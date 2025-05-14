using System.Globalization;
using System.Windows.Data;

namespace SIASGraduate.Converter
{
    /// <summary>
    /// 多值转换器，用于将多个参数打包成一个对象传递给命令
    /// </summary>
    public class MultiValueConverter : IMultiValueConverter
    {
        /// <summary>
        /// 将多个值转换为一个对象
        /// </summary>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return null;

            // 创建键值对，用于传递控件引用和事件参数
            return new System.Collections.Generic.KeyValuePair<object, object>(values[0], values[1]);
        }

        /// <summary>
        /// 不支持反向转换
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}