using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using MBusScaner.Avalonia.Helpers;
using MBusScaner.Models;
using MBusScaner.Services;

using Avalonia.Threading;
using MBusScaner.Helpers;
namespace MBusScaner.Avalonia.ViewModels
{
    public partial class DeviceDetailViewModel : ViewModelBase
    {
        private readonly MainViewModel _main;
        private readonly DeviceDiscoveryService _discovery;
        private Device? _device;
        private string _deviceStatus = string.Empty;

        public ObservableCollection<ControlParameter> Parameters { get; } = new();
        public ObservableCollection<SensorData> Sensors { get; } = new();
        public ObservableCollection<PortInfo> Ports { get; } = new();

        public DeviceDetailViewModel(MainViewModel main)
        {
            _main = main;
            _discovery = new DeviceDiscoveryService();
        }

        public Device? Device { get => _device; set => SetProperty(ref _device, value); }
        public string DeviceName => Device?.Name ?? "Sin dispositivo seleccionado";
        public string DeviceAddress => Device?.IPAddress?.ToString() ?? "-";
        public string DeviceProtocolDisplay => Device?.ProtocolString ?? "-";
        public string DeviceManufacturer => Device?.Manufacturer ?? "-";
        public string DeviceModel => Device?.Model ?? "-";
        public string DeviceFirmware => Device?.FirmwareVersion ?? "-";
        public string DeviceStatusText => Device?.IsOnline == true ? "En línea" : "Fuera de línea";
        public bool HasDevice => Device != null;
        public string DeviceStatus { get => _deviceStatus; set => SetProperty(ref _deviceStatus, value); }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            if (Device != null) await LoadDeviceAsync(Device);
        }

        [RelayCommand]
        private async Task RemoveDeviceAsync()
        {
            if (Device == null) return;
            bool confirmed = await DialogHelper.ShowConfirm("Eliminar dispositivo",
                $"¿Eliminar el dispositivo '{Device.Name}' de la lista?");
            if (confirmed)
            {
                _main.Devices.Remove(Device);
                _main.AddLog($"Dispositivo '{Device.Name}' eliminado.");
                GoBack();
            }
        }

        [RelayCommand]
        private void GoBack() => _main.NavigateTo(_main.ScanViewModel);

        public async Task LoadDeviceAsync(Device device)
        {
            Device = device;
            Parameters.Clear();
            Sensors.Clear();
            Ports.Clear();
            PopulateDeviceInfo();

            if (device.Protocol == DeviceProtocol.ModbusTcp)
            {
                DeviceStatus = "Leyendo parámetros Modbus...";
                var modbusParams = await _discovery.ReadModbusParametersAsync(device.IPAddress.ToString(), device.UnitId);
                global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    foreach (var p in modbusParams) Parameters.Add(p);
                });
                DeviceStatus = "Parámetros Modbus cargados.";
                _main.AddLog($"Parámetros cargados del dispositivo {device.Name}: {modbusParams.Count}");
            }
            else if (device.Protocol == DeviceProtocol.BacnetIp)
            {
                DeviceStatus = "Dispositivo BACnet/IP. Revea objetos disponibles en la ficha.";
            }
            else
            {
                DeviceStatus = "Protocolo no soportado para lectura de parámetros.";
            }

            OnPropertyChanged(nameof(DeviceName));
            OnPropertyChanged(nameof(DeviceAddress));
            OnPropertyChanged(nameof(DeviceProtocolDisplay));
            OnPropertyChanged(nameof(DeviceManufacturer));
            OnPropertyChanged(nameof(DeviceModel));
            OnPropertyChanged(nameof(DeviceFirmware));
            OnPropertyChanged(nameof(DeviceStatusText));
            OnPropertyChanged(nameof(HasDevice));
        }

        private void PopulateDeviceInfo()
        {
            Ports.Add(new PortInfo { Address = 0, Name = "Válvula de agua fría", Type = PortType.Valve, Direction = PortDirection.Output, Unit = "%", MinValue = 0, MaxValue = 100, CurrentValue = 25, Function = "Regulación de caudal de refrigeración" });
            Ports.Add(new PortInfo { Address = 1, Name = "Ventilador de impulsión", Type = PortType.Fan, Direction = PortDirection.Output, Unit = "RPM", MinValue = 0, MaxValue = 1450, CurrentValue = 750, Function = "Impulsión de aire a la sala" });
            Ports.Add(new PortInfo { Address = 2, Name = "Bomba de circulación", Type = PortType.Compressor, Direction = PortDirection.Output, Unit = "on/off", MinValue = 0, MaxValue = 1, CurrentValue = 1, Function = "Circulación de agua" });

            Sensors.Add(new SensorData { Name = "Temperatura de retorno", Type = SensorType.Temperature, Value = 22.5, MinValue = -10, MaxValue = 50, Unit = "°C", RegisterAddress = 4 });
            Sensors.Add(new SensorData { Name = "Humedad relativa", Type = SensorType.Humidity, Value = 45, MinValue = 0, MaxValue = 100, Unit = "%", RegisterAddress = 5 });
            Sensors.Add(new SensorData { Name = "Temperatura de impulsión", Type = SensorType.Temperature, Value = 12.8, MinValue = -10, MaxValue = 50, Unit = "°C", RegisterAddress = 6 });
        }

        public async Task SaveParameterAsync(ControlParameter param)
        {
            if (Device == null || param.IsReadOnly) return;
            try
            {
                DeviceStatus = $"Escribiendo '{param.Name}'...";
                _main.AddLog($"Escribiendo '{param.Name}' en {Device.IPAddress} (valor: {param.DisplayValue})");

                bool success = await _discovery.WriteModbusParameterAsync(Device.IPAddress.ToString(), param);
                if (success)
                {
                    param.IsModified = true;
                    param.LastWriteTime = DateTime.Now;
                    DeviceStatus = $"Parámetro '{param.Name}' actualizado correctamente.";
                }
                else
                {
                    DeviceStatus = $"Error al escribir '{param.Name}'.";
                    await DialogHelper.ShowMessage("Error", $"No se pudo escribir '{param.Name}'.");
                }
            }
            catch (Exception ex)
            {
                DeviceStatus = $"Error: {ex.Message}";
                await DialogHelper.ShowMessage("Error", $"Error al guardar: {ex.Message}");
            }
        }
    }
}


