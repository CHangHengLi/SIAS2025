using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Windows;

namespace _2025毕业设计.Converter
{
    /// <summary>
    /// 图像转换器，将图像路径转换为BitmapImage对象
    /// </summary>
    public class LegacyConVerterImage : IValueConverter
    {
        /// <summary>
        /// 将PNG文件转换为二进制数据。
        /// </summary>
        /// <param name="filePath">PNG图片的文件路径。</param>
        /// <returns>包含图片数据的字节数组。</returns>
        public static byte[] ConvertPngToByteArray(string filePath)
        {
            if (File.Exists(filePath))
            {
                return File.ReadAllBytes(filePath);
            }
            return null;
        }
        
        /// <summary>
        /// 将二进制数据转换为BitmapImage。
        /// </summary>
        /// <param name="imageData">包含图片数据的字节数组。</param>
        /// <returns>转换后的BitmapImage对象。</returns>
        public static BitmapImage ConvertByteArrayToBitmapImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
                return null;

            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }
        
        /// <summary>
        /// 将BitmapImage转换为二进制数据。
        /// </summary>
        /// <param name="bitmapImage">需要转换的BitmapImage对象。</param>
        /// <returns>包含图片数据的字节数组。</returns>
        public static byte[] ConvertBitmapImageToByteArray(BitmapImage bitmapImage)
        {
            if (bitmapImage == null)
                return null;
            using (var stream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                encoder.Save(stream);
                return stream.ToArray();
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            // 如果value是字符串（图像路径），则转换为BitmapImage
            if (value is string imagePath && !string.IsNullOrEmpty(imagePath))
            {
                try
                {
                    if (File.Exists(imagePath))
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                        bitmap.EndInit();
                        return bitmap;
                    }
                }
                catch (Exception)
                {
                    // 处理图像加载异常
                    return null;
                }
            }
            
            // 如果value是字节数组，则转换为BitmapImage
            if (value is byte[] imageData && imageData.Length > 0)
            {
                try
                {
                    return ConvertByteArrayToBitmapImage(imageData);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 将字节数组转换为图像的转换器
    /// </summary>
    public class ByteArrayToImageConverter : IValueConverter
    {
        /// <summary>
        /// 将字节数组转换为BitmapImage图像
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (value is byte[] byteArray && byteArray.Length > 0)
            {
                try
                {
                    var image = new BitmapImage();
                    using (var mem = new MemoryStream(byteArray))
                    {
                        mem.Position = 0;
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = mem;
                        image.EndInit();
                    }
                    image.Freeze();
                    return image;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ByteArrayToImageConverter转换失败: {ex.Message}");
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// 不支持反向转换
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ByteArrayToImageConverter不支持从Image转换回byte[]");
        }
    }
} 