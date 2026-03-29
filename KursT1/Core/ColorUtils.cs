using System;

namespace KursT1.Core
{
    /// <summary>Утилиты для работы с цветом</summary>
    public static class ColorUtils
    {
        /// <summary>RGB → HSV конвертация</summary>
        public static (double H, double S, double V) RgbToHsv(byte r, byte g, byte b)
        {
            double rNorm = r / 255.0;
            double gNorm = g / 255.0;
            double bNorm = b / 255.0;

            double max = Math.Max(rNorm, Math.Max(gNorm, bNorm));
            double min = Math.Min(rNorm, Math.Min(gNorm, bNorm));
            double delta = max - min;
            double h = 0;

            if (delta > 0)
            {
                if (max == rNorm)
                {
                    h = 60 * (((gNorm - bNorm) / delta) % 6);
                }
                else if (max == gNorm)
                {
                    h = 60 * (((bNorm - rNorm) / delta) + 2);
                }
                else
                {
                    h = 60 * (((rNorm - gNorm) / delta) + 4);
                }
            }

            if (h < 0) h += 360;
            double s = max > 0 ? (delta / max) * 100 : 0;
            double v = max * 100;

            return (h, s, v);
        }

        /// <summary>Чтение пикселя из массива (BGR формат)</summary>
        public static (byte R, byte G, byte B) ReadPixel(byte[] pixels, int x, int y, int width, int stride)
        {
            int index = (y * stride) + (x * 3);
            return (pixels[index + 2], pixels[index + 1], pixels[index]);
        }
    }
}