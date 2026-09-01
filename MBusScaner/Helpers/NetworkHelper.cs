using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace MBusScaner.Helpers
{
    /// <summary>
    /// Utilidades adicionales de red.
    /// </summary>
    public static class NetworkHelper
    {
        public static string GetSubnet()
        {
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up) continue;
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                    foreach (var unicast in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (unicast.Address.AddressFamily != AddressFamily.InterNetwork) continue;

                        var ip = unicast.Address;
                        var mask = unicast.IPv4Mask;
                        var ipBytes = ip.GetAddressBytes();
                        var maskBytes = mask.GetAddressBytes();

                        // Omitir link-local
                        if (ipBytes[0] == 169 && ipBytes[1] == 254) continue;

                        var network = new byte[4];
                        for (int i = 0; i < 4; i++)
                        {
                            network[i] = (byte)(ipBytes[i] & maskBytes[i]);
                        }
                        return $"{network[0]}.{network[1]}.{network[2]}.0";
                    }
                }
            }
            catch
            {
            }
            return "192.168.1.0";
        }

        public static string GetLocalIP()
        {
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up) continue;
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                    foreach (var unicast in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (unicast.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        var ipBytes = unicast.Address.GetAddressBytes();
                        if (ipBytes[0] == 169 && ipBytes[1] == 254) continue;
                        return unicast.Address.ToString();
                    }
                }
            }
            catch
            {
            }
            return "127.0.0.1";
        }

        public static string GetMacAddress()
        {
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up) continue;
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    var mac = nic.GetPhysicalAddress().ToString();
                    if (!string.IsNullOrEmpty(mac))
                    {
                        return FormatMac(mac);
                    }
                }
            }
            catch
            {
            }
            return string.Empty;
        }

        private static string FormatMac(string mac)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < mac.Length; i++)
            {
                if (i > 0 && i % 2 == 0)
                {
                    sb.Append(':');
                }
                sb.Append(mac[i]);
            }
            return sb.ToString();
        }

        public static string GetHostname(string ip)
        {
            try
            {
                var entry = Dns.GetHostEntry(ip);
                return entry?.HostName ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
