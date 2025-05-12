using System;
using System.Globalization;
using System.Windows.Data;

namespace SIASGraduate.Converter
{
    public class WidthPercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return 0;

            try
            {
                // 获取原始宽度
                double originalWidth = System.Convert.ToDouble(value);
                
                // 获取百分比
                double percentage = System.Convert.ToDouble(parameter);
                
                // 计算结果宽度
                double result = originalWidth * percentage;
                
                // 调试输出
                System.Diagnostics.Debug.WriteLine($"原始宽度: {originalWidth}, 百分比: {percentage}, 结果宽度: {result}");
                
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WidthPercentageConverter转换失败: {ex.Message}");
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("WidthPercentageConverter不支持反向转换");
        }
    }
} 