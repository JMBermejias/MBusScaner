using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MBusScaner.Helpers;
using MBusScaner.Models;
using MBusScaner.Services;

namespace MBusScaner.ViewModels
{
    /// <summary>
    /// ViewModel de la vista de detalle de un dispositivo de climatización.
    /// Muestra puertos, sensores y parámetros de control modificables.
    /// </summary>
    public class DeviceDetailViewModel : ViewModelBase
    {
        private readonly MainViewModel _main;
        private readonly DeviceDiscoveryService _discovery;

        private Device _device;
        private ControlParameter _selectedParameter;
        private SensorData _selectedSensor;
        private PortInfo _selectedPort;
        private bool _isUpdating;
        private string _deviceStatus = string.Empty;

        public ObservableCollection<ControlParameter> Parameters { get; } = new();
        public ObservableCollection<SensorData> Sensors { get; } = new();
        public ObservableCollection<PortInfo> Ports { get; } = new();

        public DeviceDetailViewModel(MainViewModel main)
        {
            _main = main;
            _discovery = new DeviceDiscoveryService();
        }

        public Device Device
        {
            get => _device;
            set => SetProperty(ref _device, value);
        }

        public string DeviceName => Device?.Name ?? "Sin dispositivo seleccionado";
        public string DeviceAddress => Device?.IPAddress?.ToString() ?? "-";
        public string DeviceProtocolDisplay => Device?.ProtocolString ?? "-";
        public string DeviceManufacturer => Device?.Manufacturer ?? "-";
        public string DeviceModel => Device?.Model ?? "-";
        public string DeviceFirmware => Device?.FirmwareVersion ?? "-";
        public string DeviceStatusText => Device?.IsOnline == true ? "En línea" : "Fuera de línea";

        public bool HasDevice => Device != null;

        public string DeviceStatus
        {
            get => _deviceStatus;
            set => SetProperty(ref _deviceStatus, value);
        }

        public ControlParameter SelectedParameter
        {
            get => _selectedParameter;
            set => SetProperty(ref _selectedParameter, value);
        }

        public SensorData SelectedSensor
        {
            get => _selectedSensor;
            set => SetProperty(ref _selectedSensor, value);
        }

        public PortInfo SelectedPort
        {
            get => _selectedPort;
            set => SetProperty(ref _selectedPort, value);
        }

        public bool IsUpdating
        {
            get => _isUpdating;
            set => SetProperty(ref _isUpdating, value);
        }

        public AsyncRelayCommand SaveParameterCommand => new(async p => await SaveParameterAsync(p), _ => SelectedParameter != null);
        public RelayCommand BackCommand => new(GoBack);
        public AsyncRelayCommand RefreshCommand => new(async () => await RefreshAsync(), () => HasDevice);
        public AsyncRelayCommand RemoveDeviceCommand => new(async () => await RemoveDeviceAsync(), () => HasDevice);

        public async Task LoadDeviceAsync(Device device)
        {
            Device = device;
            Parameters.Clear();
            Sensors.Clear();
            Ports.Clear();

            // Poblar datos de ejemplo/estáticos según el protocolo
            PopulateDeviceInfo();

            if (device.Protocol == DeviceProtocol.ModbusTcp)
            {
                DeviceStatus = "Leyendo parámetros Modbus...";
                var modbusParams = await _discovery.ReadModbusParametersAsync(device.IPAddress.ToString(), device.UnitId);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var p in modbusParams)
                    {
                        Parameters.Add(p);
                    }
                });
                DeviceStatus = "Parámetros Modbus cargados.";
                _main.AddLog($"Parámetros cargados del dispositivo {device.Name}: {modbusParams.Count}");
            }
            else if (device.Protocol == DeviceProtocol.BacnetIp)
            {
                DeviceStatus = "Dispositivo BACnet/IP. Revea objetos BACnet disponibles en la ficha.";
            }
            else
            {
                DeviceStatus = "Protocolo no soportado para lectura de parámetros.";
            }

            OnPropertiesChanged();
        }

        private void PopulateDeviceInfo()
        {
            // Añadir puertos y sensores estándar con valores de ejemplo
            if (Device.Protocol == DeviceProtocol.ModbusTcp)
            {
                Ports.Add(new PortInfo
                {
                    Address = 0,
                    Name = "Válvula de agua fría",
                    Description = "Válvula proporcional de agua refrigerada",
                    Type = PortType.Valve,
                    Direction = PortDirection.Output,
                    Unit = "%",
                    MinValue = 0,
                    MaxValue = 100,
                    CurrentValue = 25,
                    Function = "Regulación de caudal de refrigeración"
                });
                Ports.Add(new PortInfo
                {
                    Address = 1,
                    Name = "Ventilador de impulsión",
                    Description = "Motor del ventilador de aire",
                    Type = PortType.Fan,
                    Direction = PortDirection.Output,
                    Unit = "RPM",
                    MinValue = 0,
                    MaxValue = 1450,
                    CurrentValue = 750,
                    Function = "Impulsión de aire a la sala"
                });
                Ports.Add(new PortInfo
                {
                    Address = 2,
                    Name = "Bomba de circulación",
                    Description = "Bomba de agua primaria",
                    Type = PortType.Compressor,
                    Direction = PortDirection.Output,
                    Unit = "on/off",
                    MinValue = 0,
                    MaxValue = 1,
                    CurrentValue = 1,
                    Function = "Circulación de agua"
                });

                Sensors.Add(new SensorData
                {
                    Name = "Temperatura de retorno",
                    Type = SensorType.Temperature,
                    Value = 22.5,
                    MinValue = -10,
                    MaxValue = 50,
                    Unit = "°C",
                    RegisterAddress = 4
                });
                Sensors.Add(new SensorData
                {
                    Name = "Humedad relativa",
                    Type = SensorType.Humidity,
                    Value = 45,
                    MinValue = 0,
                    MaxValue = 100,
                    Unit = "%",
                    RegisterAddress = 5
                });
                Sensors.Add(new SensorData
                {
                    Name = "Temperatura de impulsión",
                    Type = SensorType.Temperature,
                    Value = 12.8,
                    MinValue = -10,
                    MaxValue = 50,
                    Unit = "°C",
                    RegisterAddress = 6
                });
            }
        }

        private async Task SaveParameterAsync(object parameterObj)
        {
            if (parameterObj is not ControlParameter param || Device == null || param.IsReadOnly)
            {
                return;
            }

            try
            {
                IsUpdating = true;
                DeviceStatus = $"Escribiendo '{param.Name}'...";
                _main.AddLog($"Escribiendo parámetro '{param.Name}' en {Device.IPAddress} (valor: {param.DisplayValue})");

                if (Device.Protocol == DeviceProtocol.ModbusTcp)
                {
                    bool success = await _discovery.WriteModbusParameterAsync(Device.IPAddress.ToString(), param);
                    if (success)
                    {
                        param.IsModified = true;
                        param.LastWriteTime = DateTime.Now;
                        DeviceStatus = $"Parámetro '{param.Name}' actualizado correctamente.";
                        _main.AddLog($"Parámetro '{param.Name}' actualizado en {Device.Name}.");
                    }
                    else
                    {
                        DeviceStatus = $"Error al escribir '{param.Name}' en el dispositivo.";
                        _main.AddLog($"Falló la escritura de '{param.Name}' en {Device.Name}.");
                        MessageBox.Show($"No se pudo escribir el parámetro '{param.Name}' en el dispositivo.",
                            "Error de escritura", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    DeviceStatus = "Escritura no soportada para este protocolo.";
                    MessageBox.Show("La escritura de parámetros solo está disponible para dispositivos Modbus TCP.",
                        "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                DeviceStatus = $"Error: {ex.Message}";
                _main.AddLog($"Error al escribir parámetro: {ex.Message}");
                MessageBox.Show($"Error al guardar el parámetro: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsUpdating = false;
            }
        }

        private void GoBack()
        {
            _main.NavigateTo(_main.ScanViewModel);
        }

        private async Task RefreshAsync()
        {
            if (Device == null) return;
            await LoadDeviceAsync(Device);
        }

        private async Task RemoveDeviceAsync()
        {
            if (Device == null) return;
            var result = MessageBox.Show($"¿Eliminar el dispositivo '{Device.Name}' de la lista?",
                "Eliminar dispositivo", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _main.Devices.Remove(Device);
                _main.AddLog($"Dispositivo '{Device.Name}' eliminado de la lista.");
                GoBack();
            }
            await Task.CompletedTask;
        }

        private void OnPropertiesChanged()
        {
            OnPropertyChanged(nameof(DeviceName));
            OnPropertyChanged(nameof(DeviceAddress));
            OnPropertyChanged(nameof(DeviceProtocolDisplay));
            OnPropertyChanged(nameof(DeviceManufacturer));
            OnPropertyChanged(nameof(DeviceModel));
            OnPropertyChanged(nameof(DeviceFirmware));
            OnPropertyChanged(nameof(DeviceStatusText));
            OnPropertyChanged(nameof(HasDevice));
        }
    }
}
