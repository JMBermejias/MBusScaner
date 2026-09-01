using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MBusScaner.Helpers;
using MBusScaner.Models;
using MBusScaner.Services;

namespace MBusScaner.ViewModels
{
    /// <summary>
    /// ViewModel principal que coordina la navegación y la lógica general de la aplicación.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly DeviceDiscoveryService _discoveryService;
        private readonly SettingsService _settingsService;
        private readonly NetworkScannerService _networkScanner;

        private ViewModelBase _currentViewModel;
        private string _statusMessage = "Listo. Conecta el cable RJ45 al ordenador y pulse 'Escanear red'.";
        private bool _isScanning;
        private NetworkInfo _networkInfo;
        private string _localIP;
        private string _subnetString;

        public ObservableCollection<Device> Devices { get; } = new();
        public ObservableCollection<string> LogMessages { get; } = new();

        public ScanViewModel ScanViewModel { get; }
        public DeviceDetailViewModel DeviceDetailViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }

        public MainViewModel()
        {
            _discoveryService = new DeviceDiscoveryService();
            _settingsService = new SettingsService();
            _networkScanner = new NetworkScannerService();

            _settingsService.Load();

            ScanViewModel = new ScanViewModel(this);
            DeviceDetailViewModel = new DeviceDetailViewModel(this);
            SettingsViewModel = new SettingsViewModel(this, _settingsService);

            _discoveryService.DiscoveryMessage += msg => AddLog(msg);
            _networkScanner.ScanStatusChanged += _ => { };
        }

        public ViewModelBase CurrentViewModel
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

        public string LocalIP => _localIP ?? (_localIP = Helpers.NetworkHelper.GetLocalIP());
        public string SubnetString => _subnetString ?? (_subnetString = Helpers.NetworkHelper.GetSubnet());

        public async Task InitializeAsync()
        {
            await DetectNetworkAsync();
            CurrentViewModel = ScanViewModel;
            LoadKnownDevices();
        }

        public async Task DetectNetworkAsync()
        {
            try
            {
                StatusMessage = "Detectando interfaz de red...";
                var network = await _networkScanner.DetectNetworkAsync();
                _networkInfo = network;
                _localIP = network.LocalIP;
                _subnetString = network.NetworkAddress.Substring(0, network.NetworkAddress.LastIndexOf('.')) + ".0";
                OnPropertyChanged(nameof(LocalIP));
                OnPropertyChanged(nameof(SubnetString));
                StatusMessage = $"Listo. Interfaz: {network.InterfaceName} ({network.LocalIP}) - Subred {SubnetString}";
            }
            catch
            {
                StatusMessage = "No se pudo detectar la red automáticamente.";
            }
        }

        public void NavigateTo(ViewModelBase viewModel)
        {
            CurrentViewModel = viewModel;
        }

        public void AddLog(string message)
        {
            var line = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
            LogMessages.Insert(0, line);
            while (LogMessages.Count > 200)
            {
                LogMessages.RemoveAt(LogMessages.Count - 1);
            }
        }

        public void RegisterDevice(Device device)
        {
            var existing = System.Linq.Enumerable.FirstOrDefault(Devices, d => d.IPAddress.Equals(device.IPAddress));
            if (existing != null)
            {
                existing.Name = device.Name;
                existing.Protocol = device.Protocol;
                existing.Status = device.Status;
                existing.LastSeen = System.DateTime.Now;
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

        private void LoadKnownDevices()
        {
            var known = _settingsService.LoadDevices();
            foreach (var dev in known)
            {
                if (!System.Linq.Enumerable.Any(Devices, d => d.IPAddress.Equals(dev.IPAddress)))
                {
                    Devices.Add(dev);
                }
            }
        }

        public void ClearLog()
        {
            LogMessages.Clear();
        }
    }
}
