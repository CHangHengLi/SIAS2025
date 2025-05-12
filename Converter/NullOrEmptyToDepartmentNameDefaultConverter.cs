using _2025毕业设计.Context;
using _2025毕业设计.Models;
using System.Globalization;
using System.Windows.Data;

namespace _2025毕业设计.Converter
{
    public class NullOrEmptyToDepartmentNameDefaultConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //传入部门Id
            using var context = new DataBaseContext();
            if (string.IsNullOrEmpty(value?.ToString()))
            {
                return "未分配";
            }
            Department? department = context.Departments.FirstOrDefault(d => d.DepartmentId == (int)value);
            if (string.IsNullOrEmpty(department?.DepartmentName))
            {
                return "未分配";
            }
            else
            {
                return department.DepartmentName;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
