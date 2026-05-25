using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AgentApp.UI.Converters;

/// <summary>Converts a hex color string to a SolidColorBrush.</summary>
public class StringToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
            catch { }
        }
        return new SolidColorBrush(Color.FromRgb(0x5B, 0x8A, 0xF5));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
