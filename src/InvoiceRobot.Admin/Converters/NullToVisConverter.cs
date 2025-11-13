using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace InvoiceRobot.Admin.Converters;

/// <summary>
/// Muuntaa null → Collapsed, not null → Visible
/// </summary>
public class NullToVisConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
