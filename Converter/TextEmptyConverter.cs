using System.Globalization;
using System.Windows.Data;

namespace SIASGraduate.Converter
{
    /// <summary>
    /// 文本为空时转换为默认值的转换器
    /// 用于替代在触发器中同时使用Text作为条件和目标的情况
    /// </summary>
    public class TextEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 如果值为null或空字符串，返回参数指定的默认值
            if (value == null || (value is string text && string.IsNullOrWhiteSpace(text)))
            {
                return parameter ?? "0"; // 默认值为0
            }

            // 否则返回原值
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}