using System;
using System.Globalization;
using System.Windows.Data;

namespace DLab.Converters
{
    public class ShortenPathConverter : IValueConverter
    {
        private const int MaxPathLength = 90;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value.ToString();

            return str.Length > MaxPathLength
                ? $"...{str.Substring(str.Length - MaxPathLength)}"
                : str;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}