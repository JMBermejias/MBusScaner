using System.Net;
using CommunityToolkit.Mvvm.Input;
using MBusScaner.Models;

namespace MBusScaner.Avalonia.ViewModels
{
    public partial class AddDeviceViewModel : ViewModelBase
    {
        private readonly MainViewModel _main;
        private string _deviceName = string.Empty;
        private string _deviceIP = string.Empty;
        private int _protocolIndex;
        private string _unitId = "1";
        private string _port = "502";
        private string _validationMessage = string.Empty;

        public AddDeviceViewModel(MainViewModel main) => _main = main;

        public string DeviceName { get => _deviceName; set { if (SetProperty(ref _deviceName, value)) OnPropertyChanged(nameof(CanSave)); } }
        public string DeviceIP { get => _deviceIP; set { if (SetProperty(ref _deviceIP, value)) OnPropertyChanged(nameof(CanSave)); } }
        public int ProtocolIndex { get => _protocolIndex; set { if (SetProperty(ref _protocolIndex, value)) { Port = value == 0 ? "502" : "47808"; OnPropertyChanged(nameof(IsModbus)); OnPropertyChanged(nameof(CanSave)); } } }
        public bool IsModbus => _protocolIndex == 0;
        public string UnitId { get => _unitId; set => SetProperty(ref _unitId, value); }
        public string Port { get => _port; set => SetProperty(ref _port, value); }
        public string ValidationMessage { get => _validationMessage; set => SetProperty(ref _validationMessage, value); }
        public bool CanSave => !string.IsNullOrWhiteSpace(DeviceName) && !string.IsNullOrWhiteSpace(DeviceIP);

        [RelayCommand]
        private void Cancel() => _main.NavigateTo(_main.ScanViewModel);

        [RelayCommand]
        private void AddDevice()
        {
            ValidationMessage = string.Empty;
            if (!IPAddress.TryParse(DeviceIP.Trim(), out var ip)) { ValidationMessage = "Dirección IP no válida."; return; }
            if (!int.TryParse(Port, out int port) || port < 1 || port > 65535) { ValidationMessage = "Puerto no válido (1-65535)."; return; }

            byte unitId = 1;
            if (IsModbus && (!byte.TryParse(UnitId.Trim(), out unitId) || unitId < 1 || unitId > 247))
            { ValidationMessage = "Unit ID no válido (1-247)."; return; }

            var device = new Device
            {
                Name = DeviceName.Trim(), IPAddress = ip, Port = port,
                Protocol = IsModbus ? DeviceProtocol.ModbusTcp : DeviceProtocol.BacnetIp,
                UnitId = unitId, Status = DeviceStatus.Offline, LastSeen = System.DateTime.Now
            };

            _main.RegisterDevice(device);
            _main.AddLog($"Dispositivo añadido manualmente: {device.Name} ({ip}) - {device.ProtocolString}.");
            _main.StatusMessage = $"{device.Name} añadido a la lista.";
            _main.NavigateTo(_main.DeviceDetailViewModel);
            _ = _main.DeviceDetailViewModel.LoadDeviceAsync(device);
        }
    }
}
