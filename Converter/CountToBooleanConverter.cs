using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace SIASGraduate.Converter
{
    /// <summary>
    /// 集合数量转换为布尔值的转换器
    /// 可以用于检查集合是否为空（Count为0）
    /// </summary>
    public class CountToBooleanConverter : IValueConverter
    {
        /// <summary>
        /// 将集合数量转换为布尔值
        /// 如果count为0，则返回false；否则返回true
        /// 如果设置了参数IsInverted为true，则结果取反
        /// </summary>
        /// <param name="value">集合对象或表示数量的整数</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数，如果为"Invert"则取反结果</param>
        /// <param name="culture">文化信息</param>
        /// <returns>布尔值</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isInvert = parameter?.ToString() == "Invert";
            int count = 0;

            // 根据值类型获取数量
            if (value is int intValue)
            {
                count = intValue;
            }
            else if (value is ICollection collection)
            {
                count = collection.Count;
            }

            // 根据数量返回结果（0为false，其他为true）
            bool result = count > 0;

            // 如果需要取反，则返回相反的结果
            return isInvert ? !result : result;
        }

        /// <summary>
        /// 反向转换（不支持）
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}