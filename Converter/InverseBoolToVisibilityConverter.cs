using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SIASGraduate.Converter
{
    /// <summary>
    /// 将布尔值反转后转换为可见性的转换器
    /// </summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 将布尔值反转后转换为可见性
        /// </summary>
        /// <param name="value">输入的布尔值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">可选参数</param>
        /// <param name="culture">区域文化信息</param>
        /// <returns>反转布尔值对应的可见性</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        /// <summary>
        /// 将可见性转换回反转的布尔值（通常不使用）
        /// </summary>
        /// <param name="value">输入的可见性</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">可选参数</param>
        /// <param name="culture">区域文化信息</param>
        /// <returns>可见性对应的反转布尔值</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility != Visibility.Visible;
            }
            return true;
        }
    }
}