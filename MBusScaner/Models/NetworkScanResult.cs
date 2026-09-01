using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace MBusScaner.Models
{
    public class NetworkScanResult
    {
        public string Subnet { get; set; } = string.Empty;
        public string LocalIP { get; set; } = string.Empty;
        public int TotalHosts { get; set; }
        public int ReachableHosts { get; set; }
        public List<Device> Devices { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public bool IsComplete { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;

        public NetworkScanResult()
        {
            StartTime = DateTime.Now;
        }
    }
}
