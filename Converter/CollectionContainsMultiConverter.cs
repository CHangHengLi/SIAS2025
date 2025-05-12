using System;
using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace SIASGraduate.Converter
{
    public class CollectionContainsMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == null || values[1] == null)
                return false;

            // 第一个参数是集合
            if (values[0] is IEnumerable collection)
            {
                // 第二个参数是要检查的值
                var valueToCheck = values[1];
                
                foreach (var item in collection)
                {
                    if (item != null && item.Equals(valueToCheck))
                        return true;
                }
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 