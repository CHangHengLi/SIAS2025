using System.Globalization;
using System.Windows.Data;

namespace SIASGraduate.Converter
{
    /// <summary>
    /// 布尔值转文本转换器
    /// </summary>
    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "";

            bool boolValue = (bool)value;

            if (parameter is string paramStr && paramStr.Contains("|"))
            {
                // 参数格式：falseText|trueText
                string[] parts = paramStr.Split('|');
                if (parts.Length == 2)
                {
                    return boolValue ? parts[1] : parts[0];
                }
            }

            // 默认情况
            return boolValue ? "是" : "否";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}