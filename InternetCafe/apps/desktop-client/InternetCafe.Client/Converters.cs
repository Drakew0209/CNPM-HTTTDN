using System.Globalization;
using System.Windows;
using System.Windows.Data;
namespace InternetCafe.Client;
public sealed class BooleanVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (value is true ^ parameter as string == "Inverse") ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
public sealed class EmptyVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var empty = value is null || value is string text && string.IsNullOrWhiteSpace(text) || value is int count && count == 0;
        return (empty ^ parameter as string == "Inverse") ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
