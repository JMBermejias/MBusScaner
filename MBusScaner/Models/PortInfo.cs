using System;

namespace MBusScaner.Models
{
    public enum PortType
    {
        DigitalInput,
        DigitalOutput,
        AnalogInput,
        AnalogOutput,
        TemperatureSensor,
        HumiditySensor,
        PressureSensor,
        FlowSensor,
        Valve,
        Fan,
        Compressor,
        Heater,
        Unknown
    }

    public enum PortDirection
    {
        Input,
        Output,
        Bidirectional
    }

    public class PortInfo
    {
        public int Address { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public PortType Type { get; set; } = PortType.Unknown;
        public PortDirection Direction { get; set; } = PortDirection.Input;
        public string Unit { get; set; } = string.Empty;
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public double CurrentValue { get; set; }
        public bool IsEnabled { get; set; } = true;
        public DateTime LastUpdated { get; set; } = DateTime.MinValue;
        public string Function { get; set; } = string.Empty;
        public string TypeString => Type switch
        {
            PortType.DigitalInput => "Entrada Digital",
            PortType.DigitalOutput => "Salida Digital",
            PortType.AnalogInput => "Entrada Analógica",
            PortType.AnalogOutput => "Salida Analógica",
            PortType.TemperatureSensor => "Sensor de Temperatura",
            PortType.HumiditySensor => "Sensor de Humedad",
            PortType.PressureSensor => "Sensor de Presión",
            PortType.FlowSensor => "Sensor de Flujo",
            PortType.Valve => "Válvula",
            PortType.Fan => "Ventilador",
            PortType.Compressor => "Compresor",
            PortType.Heater => "Calefactor",
            _ => "Desconocido"
        };
        public string DirectionString => Direction switch
        {
            PortDirection.Input => "Entrada",
            PortDirection.Output => "Salida",
            PortDirection.Bidirectional => "Bidireccional",
            _ => "Desconocido"
        };
    }
}
