using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MBusScaner.Models;

namespace MBusScaner.Services
{
    /// <summary>
    /// Servicio de descubrimiento de dispositivos de climatización en la red.
    /// Combina el escaneo de red con la detección del protocolo (Modbus TCP o BACnet/IP)
    /// y extrae la información de cada dispositivo encontrado.
    /// </summary>
    public class DeviceDiscoveryService
    {
        private readonly NetworkScannerService _networkScanner;
        private readonly ModbusTcpService _modbus;
        private readonly BacnetIpService _bacnet;

        public event Action<string> DiscoveryMessage;
        public event Action<Device> DeviceFound;
        public event Action<int, int> ProgressChanged;

        public DeviceDiscoveryService()
        {
            _networkScanner = new NetworkScannerService();
            _modbus = new ModbusTcpService();
            _bacnet = new BacnetIpService();
        }

        public async Task<List<Device>> DiscoverAsync(IProgress<double> progress = null)
        {
            DiscoveryMessage?.Invoke("Detectando subred local...");
            var network = await _networkScanner.DetectNetworkAsync();
            DiscoveryMessage?.Invoke($"Subred detectada: {network.SubnetString}");

            DiscoveryMessage?.Invoke("Escaneando dirección de red para hosts activos...");
            var hosts = await _networkScanner.ScanHostsAsync(network, progress);
            DiscoveryMessage?.Invoke($"Encontrados {hosts.Count} hosts activos.");

            var devices = new List<Device>();
            int current = 0;
            foreach (var host in hosts)
            {
                current++;
                ProgressChanged?.Invoke(current, hosts.Count);

                // Verificar si tiene puertos HVAC
                bool hasModbusPort = host.OpenPorts.Contains(502);
                bool hasBacnetPort = host.OpenPorts.Contains(47808);

                if (!hasModbusPort && !hasBacnetPort)
                {
                    // No tiene puertos HVAC conocidos abiertos
                    continue;
                }

                var device = await ProbeDeviceAsync(host.IPAddress, hasModbusPort, hasBacnetPort);
                if (device != null)
                {
                    devices.Add(device);
                    DeviceFound?.Invoke(device);
                    DiscoveryMessage?.Invoke($"Dispositivo encontrado: {device.Name} ({device.IPAddress}) - {device.ProtocolString}");
                }
            }

            // También intentar descubrimiento BACnet por broadcast
            if (network.BroadcastAddress != null)
            {
                DiscoveryMessage?.Invoke("Buscando dispositivos BACnet por broadcast...");
                try
                {
                    _bacnet.SetHost(network.BroadcastAddress);
                    if (_bacnet.Connect())
                    {
                        var bacnetDevices = await _bacnet.WhoIsAsync(network.BroadcastAddress, 2000);
                        foreach (var bdev in bacnetDevices)
                        {
                            // El broadcast Who-Is no nos da la IP directamente, así que solo lo reportamos
                            DiscoveryMessage?.Invoke($"Dispositivo BACnet detectado: instancia {bdev.InstanceNumber}, tipo {bdev.ObjectType}");
                        }
                        _bacnet.Disconnect();
                    }
                }
                catch (Exception ex)
                {
                    DiscoveryMessage?.Invoke($"Error en descubrimiento BACnet: {ex.Message}");
                }
            }

            return devices;
        }

        private async Task<Device> ProbeDeviceAsync(IPAddress ip, bool hasModbus, bool hasBacnet)
        {
            string ipStr = ip.ToString();

            // Prioridad: Modbus primero
            if (hasModbus)
            {
                try
                {
                    if (await _modbus.DetectAsync(ipStr, 502))
                    {
                        var device = new Device
                        {
                            IPAddress = ip,
                            Port = 502,
                            Protocol = DeviceProtocol.ModbusTcp,
                            Status = DeviceStatus.Online,
                            LastSeen = DateTime.Now,
                            Name = GetDefaultDeviceName(ipStr, "Modbus")
                        };

                        try
                        {
                            device.Port = 502;
                            var idInfo = await _modbus.ReadDeviceIdentificationAsync();
                            if (idInfo != null)
                            {
                                foreach (var kv in idInfo)
                                {
                                    device.Properties[kv.Key] = kv.Value;
                                }
                                device.Manufacturer = idInfo.GetValueOrDefault("VendorName");
                                device.Model = idInfo.GetValueOrDefault("ProductName") ?? idInfo.GetValueOrDefault("ModelName");
                                device.FirmwareVersion = idInfo.GetValueOrDefault("MajorMinorRevision");
                                if (!string.IsNullOrEmpty(idInfo.GetValueOrDefault("UserApplicationName")))
                                {
                                    device.Name = idInfo["UserApplicationName"];
                                }
                            }
                        }
                        catch
                        {
                        }
                        _modbus.Disconnect();
                        return device;
                    }
                }
                catch
                {
                }
            }

            if (hasBacnet)
            {
                try
                {
                    if (await _bacnet.DetectAsync(ipStr, 47808))
                    {
                        var device = new Device
                        {
                            IPAddress = ip,
                            Port = 47808,
                            Protocol = DeviceProtocol.BacnetIp,
                            Status = DeviceStatus.Online,
                            LastSeen = DateTime.Now,
                            Name = GetDefaultDeviceName(ipStr, "BACnet")
                        };
                        _bacnet.Disconnect();
                        return device;
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private string GetDefaultDeviceName(string ip, string protocol)
        {
            return $"Dispositivo {protocol} ({ip})";
        }

        /// <summary>
        /// Lee los parámetros de control y sensores de un dispositivo Modbus.
        /// Usa un mapa de registros HVAC estándar (sensible a configuración).
        /// </summary>
        public async Task<List<ControlParameter>> ReadModbusParametersAsync(string ip, int unitId)
        {
            var parameters = new List<ControlParameter>();
            try
            {
                if (!await _modbus.ConnectAsync(ip, 502)) return parameters;
                _modbus.TimeoutMs = 2000;

                // Registros HVAC estándar (el usuario puede personalizar)
                // 40001: Setpoint de temperatura (x10)
                // 40002: Modo de operación
                // 40003: Velocidad del ventilador
                // 40004: Estado del equipo (on/off)
                // 40005: Temperatura actual
                // 40006: Humedad actual
                // 40007: Temperatura de agua de ida
                // 40008: Temperatura de agua de retorno

                try
                {
                    var setpointReg = await _modbus.ReadHoldingRegistersAsync(0, 1);
                    if (setpointReg != null && setpointReg.Length > 0)
                    {
                        parameters.Add(new ControlParameter
                        {
                            Name = "Setpoint de Temperatura",
                            Description = "Temperatura objetivo de consigna (°C)",
                            Type = ParameterType.Temperature,
                            RegisterAddress = 0,
                            Value = setpointReg[0] / 10.0,
                            MinValue = 10.0,
                            MaxValue = 40.0,
                            Unit = "°C",
                            Step = "0.5"
                        });
                    }
                }
                catch { }

                try
                {
                    var modeReg = await _modbus.ReadHoldingRegistersAsync(1, 1);
                    if (modeReg != null && modeReg.Length > 0)
                    {
                        parameters.Add(new ControlParameter
                        {
                            Name = "Modo de Operación",
                            Description = "Modo de funcionamiento del equipo",
                            Type = ParameterType.OperatingMode,
                            RegisterAddress = 1,
                            Value = modeReg[0],
                            MinValue = 0,
                            MaxValue = 4,
                            Unit = "",
                            Options = new[] { "Auto", "Refrigeración", "Calefacción", "Ventilación", "Off" }
                        });
                    }
                }
                catch { }

                try
                {
                    var fanReg = await _modbus.ReadHoldingRegistersAsync(2, 1);
                    if (fanReg != null && fanReg.Length > 0)
                    {
                        parameters.Add(new ControlParameter
                        {
                            Name = "Velocidad del Ventilador",
                            Description = "Velocidad del ventilador del equipo",
                            Type = ParameterType.FanSpeed,
                            RegisterAddress = 2,
                            Value = fanReg[0],
                            MinValue = 0,
                            MaxValue = 4,
                            Unit = "",
                            Options = new[] { "Off", "Baja", "Media", "Alta", "Auto" }
                        });
                    }
                }
                catch { }

                try
                {
                    var powerReg = await _modbus.ReadCoilsAsync(0, 1);
                    if (powerReg != null && powerReg.Length > 0)
                    {
                        parameters.Add(new ControlParameter
                        {
                            Name = "Encendido/Apagado",
                            Description = "Estado de alimentación del equipo",
                            Type = ParameterType.Boolean,
                            RegisterAddress = 0,
                            Value = powerReg[0],
                            MinValue = false,
                            MaxValue = true,
                            Unit = ""
                        });
                    }
                }
                catch { }

                try
                {
                    var modeReg = await _modbus.ReadHoldingRegistersAsync(4, 1);
                    if (modeReg != null && modeReg.Length > 0)
                    {
                        parameters.Add(new ControlParameter
                        {
                            Name = "Temperatura Actual",
                            Description = "Temperatura medida en la sala",
                            Type = ParameterType.Numeric,
                            RegisterAddress = 4,
                            Value = modeReg[0] / 10.0,
                            MinValue = -10,
                            MaxValue = 60,
                            Unit = "°C",
                            IsReadOnly = true
                        });
                    }
                }
                catch { }

                _modbus.Disconnect();
            }
            catch
            {
                try { _modbus.Disconnect(); } catch { }
            }
            return parameters;
        }

        /// <summary>
        /// Escribe un parámetro de control en un dispositivo Modbus.
        /// </summary>
        public async Task<bool> WriteModbusParameterAsync(string ip, ControlParameter param)
        {
            try
            {
                if (!await _modbus.ConnectAsync(ip, 502)) return false;
                _modbus.TimeoutMs = 2000;

                bool success = false;
                switch (param.Type)
                {
                    case ParameterType.Boolean:
                        await _modbus.WriteSingleCoilAsync((ushort)param.RegisterAddress, (bool)param.Value);
                        success = true;
                        break;
                    case ParameterType.OperatingMode:
                    case ParameterType.FanSpeed:
                    case ParameterType.Numeric:
                    case ParameterType.Temperature:
                    case ParameterType.Enumeration:
                        ushort value = Convert.ToUInt16(param.Value);
                        // El valor almacenado podría estar en x10, escalar según tipo
                        if (param.Type == ParameterType.Temperature && param.Value is double d && param.Unit == "°C")
                        {
                            value = (ushort)(d * 10);
                        }
                        await _modbus.WriteSingleRegisterAsync((ushort)param.RegisterAddress, value);
                        success = true;
                        break;
                    default:
                        throw new NotSupportedException($"Tipo de parámetro no soportado para escritura: {param.Type}");
                }

                _modbus.Disconnect();
                return success;
            }
            catch
            {
                try { _modbus.Disconnect(); } catch { }
                return false;
            }
        }
    }
}
