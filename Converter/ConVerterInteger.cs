using System;
using System.Globalization;
using System.Windows.Data;

namespace _2025毕业设计.Converter
{
    /// <summary>
    /// 整数转换器，用于在不同格式的整数之间进行转换
    /// </summary>
    public class ConVerterInteger : IValueConverter
    {
        /// <summary>
        /// 将对象转换为整数
        /// </summary>
        /// <param name="value">要转换的值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数，可以指定默认值</param>
        /// <param name="culture">区域信息</param>
        /// <returns>转换后的整数值</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                // 如果参数指定了默认值，则返回默认值
                if (parameter != null && int.TryParse(parameter.ToString(), out int defaultValue))
                {
                    return defaultValue;
                }
                return 0;
            }

            // 尝试将值转换为整数
            if (int.TryParse(value.ToString(), out int result))
            {
                return result;
            }

            // 如果是浮点数，则转换为整数（四舍五入）
            if (double.TryParse(value.ToString(), out double doubleValue))
            {
                return (int)Math.Round(doubleValue);
            }

            // 如果都失败，返回0或参数指定的默认值
            if (parameter != null && int.TryParse(parameter.ToString(), out int fallbackValue))
            {
                return fallbackValue;
            }
            
            return 0;
        }

        /// <summary>
        /// 将整数转换回原始类型
        /// </summary>
        /// <param name="value">要转换的整数值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">区域信息</param>
        /// <returns>转换后的值</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return 0;

            if (!int.TryParse(value.ToString(), out int intValue))
                return 0;

            // 根据目标类型返回适当的值
            if (targetType == typeof(string))
                return intValue.ToString();
            
            if (targetType == typeof(double))
                return (double)intValue;
            
            if (targetType == typeof(float))
                return (float)intValue;
            
            if (targetType == typeof(decimal))
                return (decimal)intValue;
            
            if (targetType == typeof(long))
                return (long)intValue;
            
            return intValue;
        }
    }
}