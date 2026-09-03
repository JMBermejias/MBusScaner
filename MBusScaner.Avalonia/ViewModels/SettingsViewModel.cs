using CommunityToolkit.Mvvm.Input;
using MBusScaner.Models;
using MBusScaner.Services;

using Avalonia.Threading;
using MBusScaner.Helpers;
namespace MBusScaner.Avalonia.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly MainViewModel _main;
        private readonly SettingsService _settingsService;
        private AppSettings _settings;

        private string _modbusPort;
        private string _bacnetPort;
        private string _pollInterval;

        public SettingsViewModel(MainViewModel main, SettingsService settingsService)
        {
            _main = main;
            _settingsService = settingsService;
            _settings = _settingsService.Settings;
            LocalIP = MBusScaner.Helpers.NetworkHelper.GetLocalIP();
            MACAddress = MBusScaner.Helpers.NetworkHelper.GetMacAddress();
            _modbusPort = _settings.ModbusPort.ToString();
            _bacnetPort = _settings.BacnetPort.ToString();
            _pollInterval = _settings.PollingIntervalMs.ToString();
        }

        public string AppVersion =>
            System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.2";

        public string LocalIP { get; }
        public string MACAddress { get; }

        public string ModbusPort { get => _modbusPort; set { if (SetProperty(ref _modbusPort, value) && int.TryParse(value, out int p)) _settings.ModbusPort = p; } }
        public string BacnetPort { get => _bacnetPort; set { if (SetProperty(ref _bacnetPort, value) && int.TryParse(value, out int p)) _settings.BacnetPort = p; } }
        public string PollInterval { get => _pollInterval; set { if (SetProperty(ref _pollInterval, value) && int.TryParse(value, out int p)) _settings.PollingIntervalMs = p; } }

        [RelayCommand]
        private void Save()
        {
            _settingsService.Save();
            _main.AddLog("Configuración guardada.");
            _main.StatusMessage = "Configuración guardada correctamente.";
        }

        [RelayCommand]
        private void Reset()
        {
            _settings = new AppSettings();
            ModbusPort = _settings.ModbusPort.ToString();
            BacnetPort = _settings.BacnetPort.ToString();
            PollInterval = _settings.PollingIntervalMs.ToString();
            _settingsService.ReplaceSettings(_settings);
            _settingsService.Save();
            _main.AddLog("Configuración restablecida.");
        }

        [RelayCommand]
        private void ShowSettings() => _main.NavigateTo(this);
    }
}

