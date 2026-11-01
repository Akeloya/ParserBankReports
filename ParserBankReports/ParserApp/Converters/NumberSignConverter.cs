using System;
using System.Globalization;
using System.Windows.Data;

namespace ParserApp.Converters
{
    public class NumberSignConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return 0;
            var type = value.GetType();
            if (type != typeof(decimal) && type != typeof(int) && type != typeof(double))
                return 0;
            var checkingVal = System.Convert.ToDecimal(value);
            if (checkingVal > 0)
                return 1;
            else
                return -1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
