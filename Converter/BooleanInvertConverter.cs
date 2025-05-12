using System;
using System.Globalization;
using System.Windows.Data;

namespace SIASGraduate.Converter
{
    /// <summary>
    /// 布尔值反转转换器，将true转为false，false转为true
    /// </summary>
    public class BooleanInvertConverter : IValueConverter
    {
        /// <summary>
        /// 将布尔值反转
        /// </summary>
        /// <param name="value">输入值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>反转后的布尔值</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }

        /// <summary>
        /// 将布尔值反转（反向转换）
        /// </summary>
        /// <param name="value">输入值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>反转后的布尔值</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }
} 