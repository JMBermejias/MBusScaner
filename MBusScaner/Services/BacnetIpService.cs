using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MBusScaner.Models;

namespace MBusScaner.Services
{
    /// <summary>
    /// Cliente BACnet/IP para la comunicación con dispositivos de climatización.
    /// Implementa servicios BACnet básicos:
    /// - Who-Is / I-Am (descubrimiento de dispositivos)
    /// - ReadProperty (lectura de propiedades)
    /// - WriteProperty (escritura de propiedades)
    /// 
    /// Nota: Esta es una implementación ligera del protocolo BACnet/IP usando UDP
    /// (puerto 0xBAC0 = 47808). Para implementación completa usar librería BACnet.Standard.
    /// </summary>
    public class BacnetIpService : IDisposable
    {
        private const int DefaultPort = 47808; // 0xBAC0
        private const int DefaultTimeout = 3000;

        public string Host { get; private set; }
        public int Port { get; private set; }
        public bool IsConnected { get; private set; }
        public int TimeoutMs { get; set; } = DefaultTimeout;

        private UdpClient _udpClient;
        private readonly object _lock = new();
        private readonly Random _random = new();

        public BacnetIpService()
        {
        }

        public void SetHost(string host, int port = DefaultPort)
        {
            Host = host;
            Port = port;
        }

        public bool Connect()
        {
            try
            {
                _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
                IsConnected = true;
                return true;
            }
            catch
            {
                IsConnected = false;
                return false;
            }
        }

        /// <summary>
        /// Envía un broadcast Who-Is para descubrir dispositivos BACnet en la red.
        /// Devuelve los dispositivos que respondieron con I-Am.
        /// </summary>
        public async Task<List<BacnetDeviceInfo>> WhoIsAsync(string broadcastAddress, int timeoutMs = 2000)
        {
            var devices = new List<BacnetDeviceInfo>();
            if (_udpClient == null)
            {
                if (!Connect()) return devices;
            }

            var whoIs = BuildWhoIsPacket();
            var target = new IPEndPoint(IPAddress.Parse(broadcastAddress), DefaultPort);
            try
            {
                await _udpClient.SendAsync(whoIs, whoIs.Length, target);
            }
            catch
            {
                return devices;
            }

            var endTime = DateTime.Now.AddMilliseconds(timeoutMs);
            while (DateTime.Now < endTime)
            {
                try
                {
                    var remaining = (int)(endTime - DateTime.Now).TotalMilliseconds;
                    var receiveTask = _udpClient.ReceiveAsync();
                    var timeoutTask = Task.Delay(Math.Max(100, remaining));
                    var completed = await Task.WhenAny(receiveTask, timeoutTask);
                    if (completed == timeoutTask) break;

                    var result = await receiveTask;
                    var info = ParseIAmPacket(result.Buffer);
                    if (info != null)
                    {
                        devices.Add(info);
                    }
                }
                catch
                {
                    break;
                }
            }

            return devices;
        }

        /// <summary>
        /// Intenta detectar si el host remoto responde como dispositivo BACnet/IP
        /// </summary>
        public async Task<bool> DetectAsync(string host, int port = DefaultPort)
        {
            try
            {
                SetHost(host, port);
                if (!Connect()) return false;

                // Enviar Who-Is dirigido a la IP del dispositivo
                var whoIs = BuildWhoIsPacket();
                var target = new IPEndPoint(IPAddress.Parse(host), port);
                try
                {
                    await _udpClient.SendAsync(whoIs, whoIs.Length, target);
                }
                catch
                {
                    return false;
                }

                var endTime = DateTime.Now.AddMilliseconds(1500);
                while (DateTime.Now < endTime)
                {
                    try
                    {
                        var remaining = (int)(endTime - DateTime.Now).TotalMilliseconds;
                        var receiveTask = _udpClient.ReceiveAsync();
                        var timeoutTask = Task.Delay(Math.Max(100, remaining));
                        var completed = await Task.WhenAny(receiveTask, timeoutTask);
                        if (completed == timeoutTask) break;

                        var result = await receiveTask;
                        var sourceAddress = result.RemoteEndPoint.Address.ToString();
                        if (sourceAddress == host)
                        {
                            var info = ParseIAmPacket(result.Buffer);
                            if (info != null)
                            {
                                return true;
                            }
                        }
                    }
                    catch
                    {
                        break;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                Disconnect();
            }
        }

        /// <summary>
        /// Lee propiedades básicas de un dispositivo BACnet (nombre, modelo, etc.)
        /// </summary>
        public async Task<Dictionary<string, string>> ReadDeviceInfoAsync(string host, uint instanceNumber)
        {
            var info = new Dictionary<string, string>();
            if (!IsConnected) return info;

            try
            {
                // En esta implementación ligera, intentamos extraer info del dispositivo
                // mediante ReadProperty de object name (propiedad 77)
                var target = new IPEndPoint(IPAddress.Parse(host), DefaultPort);

                // Read ObjectName (prop 77) del objeto Device (type 8)
                var readRequest = BuildReadPropertyPacket(instanceNumber, 8, 77, 1);
                await _udpClient.SendAsync(readRequest, readRequest.Length, target);

                var endTime = DateTime.Now.AddMilliseconds(1500);
                while (DateTime.Now < endTime)
                {
                    try
                    {
                        var remaining = (int)(endTime - DateTime.Now).TotalMilliseconds;
                        var receiveTask = _udpClient.ReceiveAsync();
                        var timeoutTask = Task.Delay(Math.Max(100, remaining));
                        var completed = await Task.WhenAny(receiveTask, timeoutTask);
                        if (completed == timeoutTask) break;

                        var result = await receiveTask;
                        if (result.RemoteEndPoint.Address.ToString() == host)
                        {
                            var parsed = ParseReadPropertyResponse(result.Buffer);
                            if (parsed != null)
                            {
                                foreach (var kv in parsed)
                                {
                                    info[kv.Key] = kv.Value;
                                }
                            }
                        }
                    }
                    catch
                    {
                        break;
                    }
                }
            }
            catch
            {
            }

            return info;
        }

        // --- Construcción de paquetes BACnet ---

        private byte[] BuildWhoIsPacket()
        {
            // BVLL Header: BACnet Virtual Link Control
            var bvlc = new byte[] { 0x81, 0x0A, 0x00, 0x0C }; // BACnet/IP, unicast, len = 12 (4 BVLL + 8 APDU)
            // BACnet APDU (solo BVLC + NPDU + APDU)
            // NPDU: Version 1, no control info
            byte npduVersion = 0x01;
            byte npduControl = 0x00;
            // APDU: Confirmed? No - unconfirmed request
            // PDU Type: 0x10 (UnconfirmedRequest), service: 0x08 (Who-Is)
            byte[] whoIs = { 0x10, 0x08 };

            var packet = new List<byte>();
            packet.AddRange(bvlc);
            packet.Add(npduVersion);
            packet.Add(npduControl);
            packet.AddRange(whoIs);

            // Actualizar longitud BVLL (bytes 2-3)
            ushort length = (ushort)packet.Count;
            packet[2] = (byte)(length >> 8);
            packet[3] = (byte)(length & 0xFF);

            return packet.ToArray();
        }

        private byte[] BuildReadPropertyPacket(uint instanceNumber, int objectType, int propertyId, int arrayIndex)
        {
            // Aquí se construye el paquete ReadProperty - implementación simplificada,
            // usando los tipos BACnet estándar codificados manualmente.
            var packet = new List<byte>();

            // BVLL Header
            packet.Add(0x81); // BVLC Type: BACnet/IP
            packet.Add(0x0B); // BVLC Function: Original-Unicast-NPDU
            packet.Add(0x00); // Len high
            packet.Add(0x00); // Len low (placeholder)

            // NPDU
            packet.Add(0x01); // Version
            packet.Add(0x20); // Control Info (expecting reply)

            // APDU Header
            packet.Add(0x00); // PDU Type: ConfirmedRequest byte low
            // Invoke ID (0x00 = ignore)

            // APDU
            packet.Add(0x00);
            byte invokeId = 0x01;
            packet.Add(invokeId);
            packet.Add(0x0C); // Service: ReadProperty

            // Object Identifier (instance number)
            // Tag 0 (Context), LVT, 4 bytes
            packet.Add(0x0C); // Tag: Context, 4 data bytes
            uint objectIdentifier = ((uint)objectType << 22) | (instanceNumber & 0x003FFFFF);
            packet.Add((byte)((objectIdentifier >> 24) & 0xFFu));
            packet.Add((byte)((objectIdentifier >> 16) & 0xFFu));
            packet.Add((byte)((objectIdentifier >> 8) & 0xFFu));
            packet.Add((byte)(objectIdentifier & 0xFFu));

            // Property Identifier (tag 1)
            packet.Add(0x09); // Tag: Context, 1 data byte
            packet.Add((byte)propertyId);

            // Actualizar longitud BVLL
            ushort length = (ushort)packet.Count;
            packet[2] = (byte)(length >> 8);
            packet[3] = (byte)(length & 0xFF);

            return packet.ToArray();
        }

        private BacnetDeviceInfo ParseIAmPacket(byte[] buffer)
        {
            try
            {
                if (buffer.Length < 16) return null;
                if (buffer[0] != 0x81) return null; // No es BACnet/IP
                if (buffer[1] == 0x0C || buffer[1] == 0x0D) return null; // TCP por defecto? no

                // Buscar servicio I-Am (PDU type 0x00 = confirmed? no, unconfirmed I-Am)
                // I-Am: PDU 0x10, service 0x00
                for (int i = 4; i < buffer.Length - 6; i++)
                {
                    if (buffer[i] == 0x10 && buffer[i + 1] == 0x00)
                    {
                        // Extraer device instance (Object Identifier del Device)
                        int pos = i + 2;
                        // Tag 0x0C = Context, 4 bytes
                        if (pos + 5 > buffer.Length) return null;
                        // skip tag byte
                        pos += 1;
                        uint objId = ((uint)buffer[pos] << 24) | ((uint)buffer[pos + 1] << 16) |
                                     ((uint)buffer[pos + 2] << 8) | buffer[pos + 3];
                        uint instance = objId & 0x003FFFFF;
                        uint type = objId >> 22;

                        var dev = new BacnetDeviceInfo
                        {
                            InstanceNumber = instance,
                            ObjectType = (int)type
                        };
                        return dev;
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private Dictionary<string, string> ParseReadPropertyResponse(byte[] buffer)
        {
            try
            {
                var result = new Dictionary<string, string>();
                if (buffer.Length < 16) return null;
                if (buffer[0] != 0x81) return null;

                // Buscar propiedad Object Name en la respuesta
                // Implementación simplificada - buscar cadenas ASCII legibles
                var text = ParseAsciiStrings(buffer);
                if (text.Count > 0)
                {
                    result["DeviceName"] = text[0];
                }
                return result.Count > 0 ? result : null;
            }
            catch
            {
                return null;
            }
        }

        private List<string> ParseAsciiStrings(byte[] buffer)
        {
            var strings = new List<string>();
            var sb = new StringBuilder();
            for (int i = 4; i < buffer.Length; i++)
            {
                byte b = buffer[i];
                if (b >= 0x20 && b <= 0x7E)
                {
                    sb.Append((char)b);
                }
                else
                {
                    if (sb.Length >= 2)
                    {
                        strings.Add(sb.ToString());
                    }
                    sb.Clear();
                }
            }
            return strings;
        }

        public void Disconnect()
        {
            IsConnected = false;
            _udpClient?.Close();
            _udpClient?.Dispose();
            _udpClient = null;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }

    public class BacnetDeviceInfo
    {
        public uint InstanceNumber { get; set; }
        public int ObjectType { get; set; }
        public IPAddress IPAddress { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
