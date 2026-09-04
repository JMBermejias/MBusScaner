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
    public partial class ScanViewModel : ViewModelBase
    {
        private readonly MainViewModel _main;
        private readonly DeviceDiscoveryService _discovery;
        private readonly NetworkScannerService _scanner;

        private string _networkAddress = MBusScaner.Helpers.NetworkHelper.GetSubnet();
        private double _progress;
        private bool _isScanning;
        private string _scanSummary = "No se ha realizado ningún escaneo.";
        private Device? _selectedDevice;

        public ScanViewModel(MainViewModel main)
        {
            _main = main;
            _discovery = new DeviceDiscoveryService();
            _scanner = new NetworkScannerService();
            _discovery.DiscoveryMessage += msg => _main.AddLog(msg);
            _discovery.DeviceFound += device =>
                global::Avalonia.Threading.Dispatcher.UIThread.Post(() => _main.RegisterDevice(device));
            _discovery.ProgressChanged += (done, total) =>
            {
                var t = total == 0 ? 1 : total;
                Progress = (double)done / t * 100;
            };
            _main.Devices.CollectionChanged += (_, _) => OnPropertyChanged(nameof(DeviceCount));
        }

        public ObservableCollection<Device> Devices => _main.Devices;
        public int DeviceCount => _main.Devices.Count;
        public IRelayCommand AddDeviceCommand => _main.AddDeviceViewModel.AddDeviceCommand;

        public string NetworkAddress { get => _networkAddress; set => SetProperty(ref _networkAddress, value); }

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
        public double Progress { get => _progress; set => SetProperty(ref _progress, value); }
        public string ScanSummary { get => _scanSummary; set => SetProperty(ref _scanSummary, value); }

        public Device? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (SetProperty(ref _selectedDevice, value) && value != null)
                {
                    _main.NavigateTo(_main.DeviceDetailViewModel);
                    _ = _main.DeviceDetailViewModel.LoadDeviceAsync(value);
                }
            }
        }

        [RelayCommand]
        private async Task ScanAsync()
        {
            IsScanning = true;
            Progress = 0;
            _main.IsScanning = true;
            _main.StatusMessage = "Escaneando red de climatización...";

            try
            {
                _main.AddLog("Iniciando escaneo de red...");
                var devices = await _discovery.DiscoverAsync();
                foreach (var dev in devices)
                    await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => _main.RegisterDevice(dev));

                ScanSummary = $"Escaneo completado: {devices.Count} dispositivos encontrados.";
                _main.StatusMessage = $"Escaneo completado: {devices.Count} dispositivos encontrados.";
            }
            catch (Exception ex)
            {
                _main.AddLog($"Error durante el escaneo: {ex.Message}");
                ScanSummary = $"Error durante el escaneo: {ex.Message}";
            }
            finally
            {
                IsScanning = false;
                _main.IsScanning = false;
            }
        }

        [RelayCommand]
        private void DetectNetwork()
        {
            NetworkAddress = MBusScaner.Helpers.NetworkHelper.GetSubnet();
            _main.StatusMessage = $"Subred detectada: {NetworkAddress}";
        }

        [RelayCommand]
        private void ShowScan() => _main.NavigateTo(this);
    }
}


