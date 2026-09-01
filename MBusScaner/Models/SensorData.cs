using System;

namespace MBusScaner.Models
{
    public enum SensorType
    {
        Temperature,
        Humidity,
        Pressure,
        FlowRate,
        Power,
        Voltage,
        Current,
        CO2,
        AirQuality,
        Unknown
    }

    public class SensorData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public SensorType Type { get; set; } = SensorType.Unknown;
        public double Value { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public string Unit { get; set; } = string.Empty;
        public int RegisterAddress { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.MinValue;
        public bool IsOnline { get; set; } = true;
        public string TypeString => Type switch
        {
            SensorType.Temperature => "Temperatura",
            SensorType.Humidity => "Humedad",
            SensorType.Pressure => "Presión",
            SensorType.FlowRate => "Caudal",
            SensorType.Power => "Potencia",
            SensorType.Voltage => "Voltaje",
            SensorType.Current => "Corriente",
            SensorType.CO2 => "CO2",
            SensorType.AirQuality => "Calidad del Aire",
            _ => "Desconocido"
        };
        public string FormattedValue => $"{Value:F1} {Unit}";
    }
}
