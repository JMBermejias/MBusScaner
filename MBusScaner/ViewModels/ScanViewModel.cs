using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using MBusScaner.Helpers;
using MBusScaner.Models;
using MBusScaner.Services;

namespace MBusScaner.ViewModels
{
    /// <summary>
    /// ViewModel de la vista de escaneo de red.
    /// </summary>
    public class ScanViewModel : ViewModelBase
    {
        private readonly MainViewModel _main;
        private readonly DeviceDiscoveryService _discovery;
        private readonly NetworkScannerService _scanner;

        private string _networkAddress = Helpers.NetworkHelper.GetSubnet();
        private double _progress;
        private bool _isScanning;
        private string _scanSummary = "No se ha realizado ningún escaneo.";
        private Device _selectedDevice;
        private string _filterText = string.Empty;

        public ScanViewModel(MainViewModel main)
        {
            _main = main;
            _discovery = new DeviceDiscoveryService();
            _scanner = new NetworkScannerService();

            _discovery.DiscoveryMessage += msg => _main.AddLog(msg);
            _discovery.DeviceFound += device => System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _main.RegisterDevice(device);
            });
            _discovery.ProgressChanged += (done, total) =>
            {
                var t = total == 0 ? 1 : total;
                Progress = (double)done / t * 100;
            };
        }

        public string NetworkAddress
        {
            get => _networkAddress;
            set => SetProperty(ref _networkAddress, value);
        }

        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                if (SetProperty(ref _isScanning, value))
                {
                    OnPropertyChanged(nameof(CanScan));
                    OnPropertyChanged(nameof(ScanButtonText));
                }
            }
        }

        public bool CanScan => !IsScanning;
        public string ScanButtonText => IsScanning ? "Escaneando..." : "Escanear Red RJ45";

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public string ScanSummary
        {
            get => _scanSummary;
            set => SetProperty(ref _scanSummary, value);
        }

        public string FilterText
        {
            get => _filterText;
            set
            {
                if (SetProperty(ref _filterText, value))
                {
                    ApplyFilter();
                }
            }
        }

        public Device SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (SetProperty(ref _selectedDevice, value))
                {
                    OnPropertyChanged(nameof(HasSelectedDevice));
                    if (value != null)
                    {
                        _main.NavigateTo(_main.DeviceDetailViewModel);
                        var _ = _main.DeviceDetailViewModel.LoadDeviceAsync(value);
                    }
                }
            }
        }

        public bool HasSelectedDevice => SelectedDevice != null;

        public AsyncRelayCommand ScanCommand => new(ScanAsync, () => CanScan);
        public RelayCommand DetectNetworkCommand => new(DetectNetwork);
        public RelayCommand ShowCommand => new(_ => _main.NavigateTo(this));
        public RelayCommand AddDeviceCommand => new(_ => _main.NavigateTo(_main.AddDeviceViewModel));

        public async Task ScanAsync()
        {
            IsScanning = true;
            Progress = 0;
            _main.IsScanning = true;
            _main.StatusMessage = "Escaneando red de climatización...";

            try
            {
                _main.AddLog("Iniciando escaneo de red...");
                var progress = new Progress<double>(p => Progress = p * 100);

                var devices = await _discovery.DiscoverAsync(progress);

                foreach (var dev in devices)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _main.RegisterDevice(dev));
                }

                ScanSummary = $"Escaneo completado en {devices.Count} dispositivos de climatización encontrados.";
                _main.StatusMessage = $"Escaneo completado: {devices.Count} dispositivos encontrados.";
                _main.AddLog($"Escaneo completado. Dispositivos encontrados: {devices.Count}");
            }
            catch (Exception ex)
            {
                _main.AddLog($"Error durante el escaneo: {ex.Message}");
                ScanSummary = $"Error durante el escaneo: {ex.Message}";
                _main.StatusMessage = "Error durante el escaneo.";
            }
            finally
            {
                IsScanning = false;
                _main.IsScanning = false;
            }
        }

        private void DetectNetwork()
        {
            NetworkAddress = Helpers.NetworkHelper.GetSubnet();
            _main.StatusMessage = $"Subred detectada: {NetworkAddress}";
        }

        private void ApplyFilter()
        {
            // La vista usa CollectionViewSource con filtro; este método notifica el cambio.
            OnPropertyChanged(nameof(FilterText));
        }
    }
}
