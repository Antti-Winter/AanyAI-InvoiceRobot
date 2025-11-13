using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace InvoiceRobot.Admin.Converters;

/// <summary>
/// Muuntaa CurrentStep-arvon Visibility:ksi vertaamalla parametriin.
/// Palauttaa Visible jos CurrentStep == parametri, muuten Collapsed.
/// </summary>
public class StepVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int currentStep && parameter is string stepString)
        {
            if (int.TryParse(stepString, out int targetStep))
            {
                return currentStep == targetStep ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
