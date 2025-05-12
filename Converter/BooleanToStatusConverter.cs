using System.Globalization;
using System.Windows.Data;

namespace _2025毕业设计.Converter
{
    public class BooleanToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "在职" : "离职";
            }
            return "未知";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status == "在职";
            }
            return false;
        }
    }
}
