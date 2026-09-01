using System;
using System.Collections.Generic;
using System.Net;

namespace MBusScaner.Models
{
    public enum DeviceProtocol
    {
        Unknown,
        ModbusTcp,
        BacnetIp
    }

    public enum DeviceStatus
    {
        Offline,
        Online,
        Error
    }

    public class Device
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Dispositivo Desconocido";
        public IPAddress IPAddress { get; set; }
        public int Port { get; set; }
        public DeviceProtocol Protocol { get; set; } = DeviceProtocol.Unknown;
        public DeviceStatus Status { get; set; } = DeviceStatus.Offline;
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public byte UnitId { get; set; } = 1;
        public DateTime LastSeen { get; set; } = DateTime.MinValue;
        public DateTime LastScanTime { get; set; } = DateTime.MinValue;
        public List<PortInfo> Ports { get; set; } = new();
        public List<SensorData> Sensors { get; set; } = new();
        public List<ControlParameter> Parameters { get; set; } = new();
        public Dictionary<string, string> Properties { get; set; } = new();
        public string MACAddress { get; set; } = string.Empty;
        public bool IsOnline => Status == DeviceStatus.Online;
        public string ProtocolString => Protocol switch
        {
            DeviceProtocol.ModbusTcp => "Modbus TCP/IP",
            DeviceProtocol.BacnetIp => "BACnet/IP",
            _ => "Desconocido"
        };
        public string StatusString => Status switch
        {
            DeviceStatus.Online => "En línea",
            DeviceStatus.Offline => "Fuera de línea",
            DeviceStatus.Error => "Error",
            _ => "Desconocido"
        };
    }
}
