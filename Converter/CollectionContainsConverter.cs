using System;
using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace SIASGraduate.Converter
{
    public class CollectionContainsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            // 如果parameter是IEnumerable，检查它是否包含value
            if (parameter is IEnumerable collection)
            {
                foreach (var item in collection)
                {
                    if (item != null && item.Equals(value))
                        return true;
                }
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 