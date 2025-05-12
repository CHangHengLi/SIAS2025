using System.Globalization;
using System.Windows.Data;

namespace SIASGraduate.Converter
{
    public class NumberToRoleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int number)
            {
                switch (number)
                {
                    case 1:
                        return "超级管理员";
                    case 2:
                        return "管理员";
                    case 3:
                        return "员工";
                    // 你可以继续添加其他数字对应的角色描述
                    default:
                        return "未知角色";
                }
            }
            return "无效输入";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string role)
            {
                switch (role)
                {
                    case "超级管理员":
                        return 1;
                    case "管理员":
                        return 2;
                    case "员工":
                        return 3;
                    // 继续添加其他角色描述对应的数字
                    default:
                        //抛出异常
                        throw new InvalidOperationException("不存在这个角色,无法将角色描述转换为数字");
                }
            }
            return 0; // 或者抛出异常
        }
    }
}
