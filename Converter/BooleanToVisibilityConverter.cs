using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SIASGraduate.Converter
{
    /// <summary>
    /// 布尔值转可见性的转换器
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 转换方法：将布尔值转换为可见性
        /// </summary>
        /// <param name="value">要转换的值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数，可选，如果为true则反转逻辑</param>
        /// <param name="culture">区域信息</param>
        /// <returns>Visibility.Visible 或 Visibility.Collapsed</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool bValue = false;
            if (value is bool)
            {
                bValue = (bool)value;
            }
            else if (value is bool?)
            {
                bValue = ((bool?)value).GetValueOrDefault();
            }
            
            // 检查是否要反转逻辑
            bool invertLogic = false;
            if (parameter != null && parameter is bool)
            {
                invertLogic = (bool)parameter;
            }
            else if (parameter != null && parameter is string)
            {
                bool.TryParse(parameter as string, out invertLogic);
            }
            
            if (invertLogic)
            {
                return bValue ? Visibility.Collapsed : Visibility.Visible;
            }
            
            return bValue ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 反向转换方法：将可见性转换为布尔值
        /// </summary>
        /// <param name="value">要转换的值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数，可选，如果为true则反转逻辑</param>
        /// <param name="culture">区域信息</param>
        /// <returns>布尔值</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility visibility = Visibility.Collapsed;
            if (value is Visibility)
            {
                visibility = (Visibility)value;
            }
            
            // 检查是否要反转逻辑
            bool invertLogic = false;
            if (parameter != null && parameter is bool)
            {
                invertLogic = (bool)parameter;
            }
            else if (parameter != null && parameter is string)
            {
                bool.TryParse(parameter as string, out invertLogic);
            }
            
            if (invertLogic)
            {
                return visibility != Visibility.Visible;
            }
            
            return visibility == Visibility.Visible;
        }
    }
} 