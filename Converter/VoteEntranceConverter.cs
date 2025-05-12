using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Linq;

namespace _2025毕业设计.Converter
{
    /// <summary>
    /// 投票入口转换器
    /// </summary>
    public class VoteEntranceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 根据实际需要处理转换逻辑
            if (value == null)
                return false;
                
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    /// <summary>
    /// 投票入口页专用对象到布尔值转换器
    /// </summary>
    public class VoteObjectToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 投票入口页专用字节数组到图像转换器
    /// </summary>
    public class VoteByteArrayToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(value is byte[]) || ((byte[])value).Length == 0)
            {
                return null;
            }

            try
            {
                byte[] byteArray = (byte[])value;
                var image = new BitmapImage();
                using (var stream = new MemoryStream(byteArray))
                {
                    stream.Position = 0;
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                    image.StreamSource = stream;
                    image.EndInit();
                    if (image.CanFreeze)
                    {
                        image.Freeze();
                    }
                }
                return image;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"图像转换失败: {ex.Message}");
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 