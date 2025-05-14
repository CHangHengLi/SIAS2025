using System.Globalization;
using System.Windows.Data;

namespace SIASGraduate.Converter
{
    /// <summary>
    /// 将null值转换为false，非null值转换为true的转换器。
    /// 还支持通过参数进行值比较，格式为"value|operator"。
    /// 例如"0|="表示检查值是否等于0，"10|>"表示检查值是否大于10。
    /// </summary>
    public class NullToFalseConverter : IValueConverter
    {
        /// <summary>
        /// 转换方法：将null值转换为false，非null值转换为true
        /// 如果参数不为null，则解析参数进行特定比较
        /// </summary>
        /// <param name="value">要转换的值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数，格式为"value|operator"。支持的运算符包括=,!=,>,<,>=,<=</param>
        /// <param name="culture">区域信息</param>
        /// <returns>根据比较结果返回布尔值</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 如果参数为null，则执行基本的null检查
            if (parameter == null)
            {
                return value != null;
            }

            // 如果值为null，则根据操作符决定结果
            if (value == null)
            {
                // 如果是检查null的相等，返回true
                if (parameter.ToString() == "null|=")
                    return true;
                // 如果是检查null的不等，返回false
                if (parameter.ToString() == "null|!=")
                    return false;
                // 其他情况，null不能进行数值比较，直接返回false
                return false;
            }

            // 尝试解析参数
            string paramStr = parameter.ToString();
            if (string.IsNullOrEmpty(paramStr) || !paramStr.Contains("|"))
            {
                // 如果参数无效，则执行基本的null检查
                return value != null;
            }

            // 解析参数
            string[] parts = paramStr.Split('|');
            if (parts.Length != 2)
            {
                // 如果参数格式不正确，则执行基本的null检查
                return value != null;
            }

            string compareValueStr = parts[0];
            string operatorStr = parts[1];

            // 尝试将值转换为数值进行比较
            double valueNum;
            double compareNum;

            // 特殊情况：检查字符串相等
            if (value is string valueStr && operatorStr == "=" && !double.TryParse(compareValueStr, out _))
            {
                return valueStr == compareValueStr;
            }
            else if (value is string valueStr2 && operatorStr == "!=" && !double.TryParse(compareValueStr, out _))
            {
                return valueStr2 != compareValueStr;
            }

            // 尝试将值和比较值转换为数字
            if (!double.TryParse(value.ToString(), out valueNum) ||
                !double.TryParse(compareValueStr, out compareNum))
            {
                // 如果无法转换为数字，则执行基本的对象比较
                return value.ToString() == compareValueStr;
            }

            // 根据运算符执行比较
            switch (operatorStr)
            {
                case "=":
                    return Math.Abs(valueNum - compareNum) < 0.0001; // 考虑浮点数比较的精度问题
                case "!=":
                    return Math.Abs(valueNum - compareNum) >= 0.0001;
                case ">":
                    return valueNum > compareNum;
                case "<":
                    return valueNum < compareNum;
                case ">=":
                    return valueNum >= compareNum;
                case "<=":
                    return valueNum <= compareNum;
                default:
                    // 不支持的运算符，执行基本的null检查
                    return value != null;
            }
        }

        /// <summary>
        /// 反向转换方法（不实现）
        /// </summary>
        /// <param name="value">要转换的值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">区域信息</param>
        /// <returns>始终返回null</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 不支持从布尔值反向转换到原始对象
            return null;
        }
    }
}