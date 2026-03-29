using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KursT1.Core
{
    /// <summary>Базовый класс для анализаторов изображений</summary>
    public abstract class ImageAnalyzerBase
    {
        protected (byte[] Pixels, int Width, int Height, int Stride) LoadImageAsBgr24(string filePath)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgr24, null, 0);
            int width = converted.PixelWidth;
            int height = converted.PixelHeight;
            int stride = width * 3;
            byte[] pixels = new byte[height * stride];
            converted.CopyPixels(pixels, stride, 0);

            return (pixels, width, height, stride);
        }
    }
}