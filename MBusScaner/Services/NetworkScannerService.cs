using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MBusScaner.Services
{
    /// <summary>
    /// Servicio de escaneo de red con auto-detección de la subred local.
    /// Realiza:
    /// 1. Detección de la interfaz de red activa y su subred
    /// 2. Ping sweep para encontrar hosts activos
    /// 3. Escaneo de puertos (Modbus 502, BACnet 47808) en cada host
    /// </summary>
    public class NetworkScannerService
    {
        private static readonly int[] HvacPorts = { 502, 47808, 80, 443, 2323 };

        public event Action<string> ScanStatusChanged;
        public event Action<int, int> ScanProgressChanged;

        public async Task<NetworkInfo> DetectNetworkAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                        .Where(i => i.OperationalStatus == OperationalStatus.Up)
                        .Where(i => i.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                        .Where(i => i.NetworkInterfaceType != NetworkInterfaceType.Tunnel);

                    foreach (var nic in interfaces)
                    {
                        var props = nic.GetIPProperties();
                        foreach (var unicast in props.UnicastAddresses)
                        {
                            if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                var ip = unicast.Address;
                                var mask = unicast.IPv4Mask;
                                var networkInfo = CalculateNetwork(ip, mask);
                                if (networkInfo != null)
                                {
                                    networkInfo.InterfaceName = nic.Name;
                                    networkInfo.MACAddress = nic.GetPhysicalAddress().ToString();
                                    return networkInfo;
                                }
                            }
                        }
                    }
                }
                catch
                {
                }

                return new NetworkInfo
                {
                    NetworkAddress = "192.168.1.0",
                    SubnetMask = "255.255.255.0",
                    NetworkIP = IPAddress.Parse("192.168.1.0"),
                    SubnetMaskIP = IPAddress.Parse("255.255.255.0"),
                    InterfaceName = "Unknown"
                };
            });
        }

        private NetworkInfo CalculateNetwork(IPAddress ip, IPAddress mask)
        {
            try
            {
                var ipBytes = ip.GetAddressBytes();
                var maskBytes = mask.GetAddressBytes();
                if (ipBytes.Length != 4 || maskBytes.Length != 4) return null;

                var networkBytes = new byte[4];
                var broadcastBytes = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
                    broadcastBytes[i] = (byte)(ipBytes[i] | (~maskBytes[i] & 0xFF));
                }

                // Verificar que es una IP privada/útil (no link-local 169.254)
                if (networkBytes[0] == 169 && networkBytes[1] == 254) return null;

                // Se puede escanear solo subredes razonables (/24 a /20)
                int prefixLength = GetPrefixLength(maskBytes);
                if (prefixLength < 20 || prefixLength > 30) return null;

                return new NetworkInfo
                {
                    NetworkAddress = $"{networkBytes[0]}.{networkBytes[1]}.{networkBytes[2]}.{networkBytes[3]}",
                    SubnetMask = mask.ToString(),
                    BroadcastAddress = $"{broadcastBytes[0]}.{broadcastBytes[1]}.{broadcastBytes[2]}.{broadcastBytes[3]}",
                    NetworkIP = new IPAddress(networkBytes),
                    SubnetMaskIP = mask,
                    LocalIP = ip.ToString(),
                    PrefixLength = prefixLength
                };
            }
            catch
            {
                return null;
            }
        }

        private int GetPrefixLength(byte[] mask)
        {
            int bits = 0;
            foreach (var b in mask)
            {
                byte x = b;
                while (x > 0)
                {
                    bits += x & 1;
                    x >>= 1;
                }
            }
            return bits;
        }

        public async Task<List<HostInfo>> ScanHostsAsync(NetworkInfo network, IProgress<double> progress = null, CancellationToken ct = default)
        {
            var results = new List<HostInfo>();
            var semaphore = new SemaphoreSlim(100);
            ScanStatusChanged?.Invoke($"Escaneando hosts en {network.SubnetString}...");

            string baseNetwork = network.NetworkAddress.Substring(0, network.NetworkAddress.LastIndexOf('.') + 1);

            int totalHosts = network.PrefixLength >= 24 ? 254 : 4094;
            var tasks = new List<Task>();
            int current = 0;

            // Para /24 usamos 1..254, para subredes más grandes escaneamos los primeros
            int startHost = 1;
            int endHost = network.PrefixLength >= 24 ? 254 : 254;

            for (int i = startHost; i <= endHost; i++)
            {
                int host = i;
                var task = Task.Run(async () =>
                {
                    await semaphore.WaitAsync(ct);
                    try
                    {
                        string ip = baseNetwork + host;
                        try
                        {
                            using var ping = new Ping();
                            var reply = await ping.SendPingAsync(ip, 500);
                            if (reply.Status == IPStatus.Success || reply.Status == IPStatus.TimedOut)
                            {
                                var hostInfo = new HostInfo { IPAddress = IPAddress.Parse(ip) };
                                hostInfo.IsReachable = await IsPortReachableAsync(ip, 445, 300) ||
                                                       await IsPortReachableAsync(ip, 502, 300) ||
                                                       await IsPortReachableAsync(ip, 80, 300);
                                if (hostInfo.IsReachable || reply.Status == IPStatus.Success)
                                {
                                    results.Add(hostInfo);
                                    hostInfo.OpenPorts = await ScanOpenPortsAsync(ip);
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                        int done = Interlocked.Increment(ref current);
                        ScanProgressChanged?.Invoke(done, endHost - startHost + 1);
                        progress?.Report((double)done / (endHost - startHost + 1));
                    }
                }, ct);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
            results.Sort((a, b) => BitConverter.ToInt32(a.IPAddress.GetAddressBytes(), 0).CompareTo(BitConverter.ToInt32(b.IPAddress.GetAddressBytes(), 0)));
            return results;
        }

        private async Task<bool> IsPortReachableAsync(string ip, int port, int timeoutMs)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(ip, port);
                var timeoutTask = Task.Delay(timeoutMs);
                var completed = await Task.WhenAny(connectTask, timeoutTask);
                if (completed == timeoutTask)
                {
                    return false;
                }
                return client.Connected;
            }
            catch
            {
                return false;
            }
        }

        private async Task<List<int>> ScanOpenPortsAsync(string ip)
        {
            var openPorts = new List<int>();
            var tasks = HvacPorts.Select(p => new { Port = p, Task = IsPortReachableAsync(ip, p, 300) });
            var results = await Task.WhenAll(tasks.Select(t => t.Task));
            for (int i = 0; i < tasks.Count(); i++)
            {
                if (results[i])
                {
                    openPorts.Add(tasks.ElementAt(i).Port);
                }
            }
            return openPorts;
        }

        public async Task<Dictionary<int, bool>> ScanPortsAsync(string ip, IEnumerable<int> ports, int timeoutMs = 1000)
        {
            var result = new Dictionary<int, bool>();
            foreach (var port in ports)
            {
                result[port] = await IsPortReachableAsync(ip, port, timeoutMs);
            }
            return result;
        }
    }

    public class NetworkInfo
    {
        public string NetworkAddress { get; set; }
        public string SubnetMask { get; set; }
        public string BroadcastAddress { get; set; }
        public string LocalIP { get; set; }
        public string InterfaceName { get; set; }
        public string MACAddress { get; set; }
        public IPAddress NetworkIP { get; set; }
        public IPAddress SubnetMaskIP { get; set; }
        public int PrefixLength { get; set; } = 24;

        public string SubnetString
        {
            get
            {
                var parts = NetworkAddress.Split('.');
                if (parts.Length != 4) return NetworkAddress;
                return $"{parts[0]}.{parts[1]}.{parts[2]}.0/{(PrefixLength >= 24 ? 24 : PrefixLength)}";
            }
        }
    }

    public class HostInfo
    {
        public IPAddress IPAddress { get; set; }
        public bool IsReachable { get; set; }
        public List<int> OpenPorts { get; set; } = new();
        public string HostName { get; set; } = string.Empty;

        public override string ToString() => IPAddress?.ToString() ?? "Unknown";
    }
}
