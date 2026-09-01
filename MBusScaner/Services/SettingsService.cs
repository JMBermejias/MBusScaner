using System;
using System.Collections.Generic;
using System.IO;
using MBusScaner.Models;
using Newtonsoft.Json;

namespace MBusScaner.Services
{
    /// <summary>
    /// Servicio de persistencia de configuración y dispositivos conocidos.
    /// Guarda los datos en formato JSON en la carpeta de configuración del usuario.
    /// </summary>
    public class SettingsService
    {
        private string _configDir;
        private string _settingsPath;
        private string _devicesPath;

        public AppSettings Settings { get; private set; }

        /// <summary>
        /// Reemplaza la configuración actual con una nueva instancia.
        /// </summary>
        public void ReplaceSettings(AppSettings settings)
        {
            Settings = settings;
        }

        public SettingsService()
        {
            _configDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MBusScaner");
            _settingsPath = Path.Combine(_configDir, "settings.json");
            _devicesPath = Path.Combine(_configDir, "known_devices.json");
        }

        public void Load()
        {
            try
            {
                Directory.CreateDirectory(_configDir);
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    Settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    Settings = new AppSettings();
                }
            }
            catch
            {
                Settings = new AppSettings();
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(_configDir);
                var json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
                File.WriteAllText(_settingsPath, json);
            }
            catch
            {
            }
        }

        public void SaveDevices(List<Device> devices)
        {
            try
            {
                Directory.CreateDirectory(_configDir);
                var json = JsonConvert.SerializeObject(devices, Formatting.Indented);
                File.WriteAllText(_devicesPath, json);
            }
            catch
            {
            }
        }

        public List<Device> LoadDevices()
        {
            try
            {
                if (File.Exists(_devicesPath))
                {
                    var json = File.ReadAllText(_devicesPath);
                    var devices = JsonConvert.DeserializeObject<List<Device>>(json);
                    return devices ?? new List<Device>();
                }
            }
            catch
            {
            }
            return new List<Device>();
        }
    }

    public class AppSettings
    {
        public string Language { get; set; } = "es";
        public bool AutoScanOnStartup { get; set; } = false;
        public int ScanTimeoutMs { get; set; } = 500;
        public int ModbusPort { get; set; } = 502;
        public int BacnetPort { get; set; } = 47808;
        public int PollingIntervalMs { get; set; } = 2000;
        public List<RegisterMapEntry> ModbusRegisterMap { get; set; } = new()
        {
            new RegisterMapEntry { Address = 0, Name = "Setpoint Temperatura", Type = "Temperature", Scale = 10, Unit = "°C", Direction = "ReadWrite" },
            new RegisterMapEntry { Address = 1, Name = "Modo de Operación", Type = "OperatingMode", Scale = 1, Unit = "", Direction = "ReadWrite" },
            new RegisterMapEntry { Address = 2, Name = "Velocidad Ventilador", Type = "FanSpeed", Scale = 1, Unit = "", Direction = "ReadWrite" },
            new RegisterMapEntry { Address = 4, Name = "Temperatura Actual", Type = "Numeric", Scale = 10, Unit = "°C", Direction = "ReadOnly" }
        };
        public Dictionary<string, object> UserParameterOverrides { get; set; } = new();
    }

    public class RegisterMapEntry
    {
        public int Address { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Numeric";
        public double Scale { get; set; } = 1;
        public string Unit { get; set; } = string.Empty;
        public string Direction { get; set; } = "ReadWrite";
    }
}
