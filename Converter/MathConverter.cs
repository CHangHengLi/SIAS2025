using System;
using System.Globalization;
using System.Windows.Data;
using System.Data;

namespace _2025毕业设计.Converter
{
    /// <summary>
    /// 数学表达式计算转换器
    /// 用法示例: {Binding Path=Width, Converter={StaticResource MathConverter}, ConverterParameter=x*0.5}
    /// </summary>
    public class MathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return 0;
                
            try 
            {
                // 获取值和表达式
                double x = System.Convert.ToDouble(value);
                string expression = parameter.ToString().Replace("x", x.ToString(CultureInfo.InvariantCulture));
                
                // 使用DataTable的Compute方法计算表达式
                DataTable dt = new DataTable();
                var result = dt.Compute(expression, "");
                
                // 对结果进行类型转换
                if (targetType == typeof(double))
                    return System.Convert.ToDouble(result);
                if (targetType == typeof(int))
                    return System.Convert.ToInt32(result);
                if (targetType == typeof(decimal))
                    return System.Convert.ToDecimal(result);
                
                return System.Convert.ToDouble(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MathConverter error: {ex.Message}");
                return 0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("MathConverter不支持反向转换");
        }
    }
}