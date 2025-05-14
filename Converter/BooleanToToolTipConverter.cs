using System.Globalization;
using System.Windows.Data;

namespace SIASGraduate.Converter
{
    public class BooleanToToolTipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool hasVoted)
            {
                return hasVoted ? "您已经投过票了" : "点击投票";
            }
            return "点击投票";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}