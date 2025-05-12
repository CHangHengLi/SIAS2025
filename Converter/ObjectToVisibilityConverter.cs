using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace _2025毕业设计.Converter
{
    /// <summary>
    /// 将对象转换为可见性的转换器
    /// 如果对象为null，则返回Collapsed，否则返回Visible
    /// 特别处理byte[]类型，检查数组长度是否大于0
    /// </summary>
    public class ObjectToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Collapsed;
                
            // 特别处理字节数组类型，确保数组非空且有内容
            if (value is byte[] byteArray)
                return byteArray.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
                
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 