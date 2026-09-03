using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using MBusScaner.Models;
using MBusScaner.Services;

using Avalonia.Threading;
using MBusScaner.Helpers;
namespace MBusScaner.Avalonia.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly DeviceDiscoveryService _discoveryService;
        private readonly SettingsService _settingsService;
        private readonly NetworkScannerService _networkScanner;

        private ViewModelBase? _currentViewModel;
        private string _statusMessage = "Listo. Conecta el cable RJ45 al ordenador y pulse 'Escanear red'.";
        private bool _isScanning;
        private string? _localIP;
        private string? _subnetString;

        public ObservableCollection<Device> Devices { get; } = new();
        public ObservableCollection<string> LogMessages { get; } = new();

        public ScanViewModel ScanViewModel { get; }
        public DeviceDetailViewModel DeviceDetailViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }
        public AddDeviceViewModel AddDeviceViewModel { get; }

        public MainViewModel()
        {
            _discoveryService = new DeviceDiscoveryService();
            _settingsService = new SettingsService();
            _networkScanner = new NetworkScannerService();
            _settingsService.Load();

            ScanViewModel = new ScanViewModel(this);
            DeviceDetailViewModel = new DeviceDetailViewModel(this);
            SettingsViewModel = new SettingsViewModel(this, _settingsService);
            AddDeviceViewModel = new AddDeviceViewModel(this);

            _discoveryService.DiscoveryMessage += msg => AddLog(msg);

            DetectNetworkAsync();
            CurrentViewModel = ScanViewModel;
            LoadKnownDevices();
        }

        public ViewModelBase? CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                if (SetProperty(ref _isScanning, value))
                {
                    OnPropertyChanged(nameof(CanScan));
                }
            }
        }

        public bool CanScan => !IsScanning;
        public string LocalIP => _localIP ?? (_localIP = MBusScaner.Helpers.NetworkHelper.GetLocalIP());
        public string SubnetString => _subnetString ?? (_subnetString = MBusScaner.Helpers.NetworkHelper.GetSubnet());

        public void NavigateTo(ViewModelBase viewModel)
        {
            CurrentViewModel = viewModel;
        }

        public void AddLog(string message)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            LogMessages.Insert(0, line);
            while (LogMessages.Count > 200)
                LogMessages.RemoveAt(LogMessages.Count - 1);
        }

        public void RegisterDevice(Device device)
        {
            var existing = Devices.Count > 0
                ? System.Linq.Enumerable.FirstOrDefault(Devices, d => d.IPAddress.Equals(device.IPAddress))
                : null;

            if (existing != null)
            {
                existing.Name = device.Name;
                existing.Protocol = device.Protocol;
                existing.Status = device.Status;
                existing.LastSeen = DateTime.Now;
                existing.Manufacturer = device.Manufacturer;
                existing.Model = device.Model;
                existing.Port = device.Port;
            }
            else
            {
                Devices.Add(device);
            }
            _settingsService.SaveDevices(new System.Collections.Generic.List<Device>(Devices));
        }

        public async Task DetectNetworkAsync()
        {
            try
            {
                StatusMessage = "Detectando interfaz de red...";
                var network = await _networkScanner.DetectNetworkAsync();
                _localIP = network.LocalIP;
                _subnetString = network.NetworkAddress.Substring(0, network.NetworkAddress.LastIndexOf('.')) + ".0";
                OnPropertyChanged(nameof(LocalIP));
                OnPropertyChanged(nameof(SubnetString));
                StatusMessage = $"Listo. Interfaz: {network.InterfaceName} ({network.LocalIP}) - Subred {_subnetString}";
            }
            catch
            {
                StatusMessage = "No se pudo detectar la red automáticamente.";
            }
        }

        private void LoadKnownDevices()
        {
            var known = _settingsService.LoadDevices();
            foreach (var dev in known)
            {
                if (!System.Linq.Enumerable.Any(Devices, d => d.IPAddress.Equals(dev.IPAddress)))
                    Devices.Add(dev);
            }
        }

        public void ClearLog() => LogMessages.Clear();
    }
}

