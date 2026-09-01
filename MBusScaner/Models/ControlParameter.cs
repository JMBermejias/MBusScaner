using System;

namespace MBusScaner.Models
{
    public enum ParameterType
    {
        Numeric,
        Boolean,
        Enumeration,
        String,
        Temperature,
        FanSpeed,
        OperatingMode
    }

    public enum OperatingMode
    {
        Auto,
        Cooling,
        Heating,
        FanOnly,
        Dehumidify,
        Off
    }

    public class ControlParameter
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ParameterType Type { get; set; } = ParameterType.Numeric;
        public int RegisterAddress { get; set; }
        public object Value { get; set; }
        public object MinValue { get; set; }
        public object MaxValue { get; set; }
        public object DefaultValue { get; set; }
        public string Step { get; set; } = "1";
        public string Unit { get; set; } = string.Empty;
        public bool IsReadOnly { get; set; }
        public bool IsModified { get; set; }
        public DateTime LastWriteTime { get; set; } = DateTime.MinValue;
        public string[] Options { get; set; } = Array.Empty<string>();
        public bool IsWritable => !IsReadOnly;

        public ControlParameter()
        {
            Value = 0;
            MinValue = 0;
            MaxValue = 100;
            DefaultValue = 0;
        }

        public string DisplayValue => Value?.ToString() ?? string.Empty;
        public string TypeString => Type switch
        {
            ParameterType.Numeric => "Numérico",
            ParameterType.Boolean => "Activar/Desactivar",
            ParameterType.Enumeration => "Selección",
            ParameterType.String => "Texto",
            ParameterType.Temperature => "Temperatura",
            ParameterType.FanSpeed => "Velocidad Ventilador",
            ParameterType.OperatingMode => "Modo de Operación",
            _ => "Desconocido"
        };
    }
}
