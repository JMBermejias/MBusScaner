using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MBusScaner.Models;

namespace MBusScaner.Converters
{
    /// <summary>
    /// Convierte el estado de un dispositivo a un color (para indicadores LED).
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DeviceStatus status)
            {
                return status switch
                {
                    DeviceStatus.Online => new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)),   // Verde
                    DeviceStatus.Offline => new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E)), // Gris
                    DeviceStatus.Error => new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)),   // Rojo
                    _ => new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E))
                };
            }
            return new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convierte un valor booleano a un color (verde/rojo o verde/gris).
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;
            if (parameter?.ToString() == "Inverse")
            {
                boolValue = !boolValue;
            }
            return boolValue
                ? new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32))
                : new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convierte el protocolo de dispositivo a una etiqueta descriptiva con abreviación.
    /// </summary>
    public class ProtocolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DeviceProtocol protocol)
            {
                return protocol switch
                {
                    DeviceProtocol.ModbusTcp => "Modbus TCP",
                    DeviceProtocol.BacnetIp => "BACnet/IP",
                    _ => "Desconocido"
                };
            }
            return "Desconocido";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Devuelve Visible si el texto no está vacío, en caso contrario Collapsed.
    /// </summary>
    public class StringNotEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !string.IsNullOrWhiteSpace(value as string)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Muestra el ComboBox si el tipo de parámetro es una enumeración (enum).
    /// </summary>
    public class EnumToVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ParameterType type)
            {
                return type is ParameterType.Enumeration or ParameterType.OperatingMode or ParameterType.FanSpeed
                    ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Muestra el TextBox solo para parámetros no enumerados (numéricos, temperatura).
    /// </summary>
    public class EnumToTextBoxVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ParameterType type)
            {
                return type is ParameterType.Enumeration or ParameterType.OperatingMode or ParameterType.FanSpeed
                    ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
