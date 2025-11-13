using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace InvoiceRobot.Admin.Converters;

/// <summary>
/// Muuntaa bool → Visibility käänteisesti (true → Collapsed, false → Visible)
/// </summary>
public class InverseBoolToVisConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Collapsed;
        }
        return false;
    }
}
