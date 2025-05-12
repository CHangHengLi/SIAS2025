using System;
using System.Globalization;
using System.Windows.Data;

namespace SIASGraduate.Converter
{
    /// <summary>
    /// 将日期转换为"xxxx年-xx月-xx日"格式的转换器
    /// </summary>
    public class DateFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 如果值为null，返回空字符串
            if (value == null)
                return string.Empty;
            
            // 如果值是DateTime类型
            if (value is DateTime dateTime)
            {
                // 格式化为"xxxx年xx月xx日"
                return dateTime.ToString("yyyy年MM月dd日");
            }
            
            // 尝试解析为DateTime类型
            if (value is string dateString && DateTime.TryParse(dateString, out DateTime parsedDate))
            {
                return parsedDate.ToString("yyyy年MM月dd日");
            }
            
            // 其他情况返回原值的字符串表示
            return value.ToString();
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 通常不需要实现从格式化字符串返回到日期的转换
            // 这里只是一个基本实现
            if (value is string dateString)
            {
                // 尝试解析格式化后的日期字符串
                dateString = dateString.Replace("年", "").Replace("月", "").Replace("日", "");
                if (DateTime.TryParse(dateString, out DateTime result))
                {
                    return result;
                }
            }
            
            return value;
        }
    }
} 