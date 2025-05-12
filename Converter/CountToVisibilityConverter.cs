using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace _2025毕业设计.Converter
{
    /// <summary>
    /// 将数量转换为可见性的转换器
    /// 当数量为0时显示，否则隐藏
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                // 当集合为空时显示提示
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            
            // 默认隐藏
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 