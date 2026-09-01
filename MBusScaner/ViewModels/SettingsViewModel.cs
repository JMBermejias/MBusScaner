using MBusScaner.Helpers;
using MBusScaner.Models;
using MBusScaner.Services;

namespace MBusScaner.ViewModels
{
    /// <summary>
    /// ViewModel de la vista de configuración.
    /// </summary>
    public class SettingsViewModel : ViewModelBase
    {
        private readonly MainViewModel _main;
        private readonly SettingsService _settingsService;
        private AppSettings _settings;

        private string _localIP;
        private string _macAddress;
        private string _modbusPort;
        private string _bacnetPort;
        private string _pollInterval;

        public SettingsViewModel(MainViewModel main, SettingsService settingsService)
        {
            _main = main;
            _settingsService = settingsService;
            _settings = _settingsService.Settings;

            _localIP = Helpers.NetworkHelper.GetLocalIP();
            _macAddress = Helpers.NetworkHelper.GetMacAddress();
            _modbusPort = _settings.ModbusPort.ToString();
            _bacnetPort = _settings.BacnetPort.ToString();
            _pollInterval = _settings.PollingIntervalMs.ToString();
        }

        public string AppVersion =>
            System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

        public string LocalIP
        {
            get => _localIP;
            set => SetProperty(ref _localIP, value);
        }

        public string MACAddress
        {
            get => _macAddress;
            set => SetProperty(ref _macAddress, value);
        }

        public string ModbusPort
        {
            get => _modbusPort;
            set
            {
                if (SetProperty(ref _modbusPort, value) && int.TryParse(value, out int p))
                {
                    _settings.ModbusPort = p;
                }
            }
        }

        public string BacnetPort
        {
            get => _bacnetPort;
            set
            {
                if (SetProperty(ref _bacnetPort, value) && int.TryParse(value, out int p))
                {
                    _settings.BacnetPort = p;
                }
            }
        }

        public string PollInterval
        {
            get => _pollInterval;
            set
            {
                if (SetProperty(ref _pollInterval, value) && int.TryParse(value, out int p))
                {
                    _settings.PollingIntervalMs = p;
                }
            }
        }

        public RelayCommand SaveCommand => new(_ => Save(), _ => true);
        public RelayCommand ResetCommand => new(_ => Reset(), _ => true);
        public RelayCommand ShowCommand => new(_ => _main.NavigateTo(this));

        private void Save()
        {
            _settingsService.Save();
            _main.AddLog("Configuración guardada.");
            _main.StatusMessage = "Configuración guardada correctamente.";
        }

        private void Reset()
        {
            _settings = new AppSettings();
            ModbusPort = _settings.ModbusPort.ToString();
            BacnetPort = _settings.BacnetPort.ToString();
            PollInterval = _settings.PollingIntervalMs.ToString();
            _settingsService.ReplaceSettings(_settings);
            _settingsService.Save();
            _main.AddLog("Configuración restablecida a los valores por defecto.");
        }
    }
}
