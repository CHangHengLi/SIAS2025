using System;
using System.Globalization;
using System.Windows.Data;

namespace SIASGraduate.Converter
{
    /// <summary>
    /// 检测对象是否为null的转换器
    /// </summary>
    public class ObjectToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;
            
            if (value is bool boolValue)
                return boolValue;
                
            if (parameter != null && parameter.ToString() == "Inverse")
                return value == null;
                
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 