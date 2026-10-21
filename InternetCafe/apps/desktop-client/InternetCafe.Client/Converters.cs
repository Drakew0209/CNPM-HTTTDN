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

public sealed class ImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url)) return null;

        try
        {
            var fileName = System.IO.Path.GetFileName(url.Split('?')[0]);
            if (!string.IsNullOrEmpty(fileName))
            {
                var candidates = new[]
                {
                    System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Food", fileName),
                    System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Games", fileName),
                    System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Banners", fileName),
                    System.IO.Path.Combine(Environment.CurrentDirectory, "InternetCafe", "apps", "desktop-client", "InternetCafe.Client", "Assets", "Food", fileName),
                    System.IO.Path.Combine(Environment.CurrentDirectory, "InternetCafe", "apps", "desktop-client", "InternetCafe.Client", "Assets", "Games", fileName),
                    System.IO.Path.Combine(Environment.CurrentDirectory, "InternetCafe", "apps", "desktop-client", "InternetCafe.Client", "Assets", "Banners", fileName),
                    System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "Food", fileName),
                    System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "Games", fileName),
                    System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "Banners", fileName)
                };

                foreach (var path in candidates)
                {
                    if (System.IO.File.Exists(path))
                    {
                        var full = System.IO.Path.GetFullPath(path);
                        var bmp = new System.Windows.Media.Imaging.BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bmp.UriSource = new Uri(full, UriKind.Absolute);
                        bmp.EndInit();
                        bmp.Freeze();
                        return bmp;
                    }
                }
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                var bmp = new System.Windows.Media.Imaging.BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bmp.UriSource = uri;
                bmp.EndInit();
                return bmp;
            }
        }
        catch
        {
            // fallback
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
