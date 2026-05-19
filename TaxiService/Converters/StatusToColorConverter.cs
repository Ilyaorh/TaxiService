using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TaxiService.WPF.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string;

            return status?.ToLower() switch
            {
                "available" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                "busy" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                "offline" => new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}