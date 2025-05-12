using System;
using System.Globalization;
using System.Windows.Data;

namespace SIASGraduate.Converter
{
    /// <summary>
    /// 简单的对象到布尔值转换器，用于判断对象是否为null
    /// </summary>
    public class SimpleObjectToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 