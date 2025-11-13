using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace InvoiceRobot.Admin.Converters;

/// <summary>
/// Muuntaa bool → Visibility (true → Visible, false → Collapsed)
/// </summary>
public class BoolToVisConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}
