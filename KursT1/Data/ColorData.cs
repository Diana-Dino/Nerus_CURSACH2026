using System;
using System.Collections.Generic;
using System.Linq;
using KursT1.Core;

namespace KursT1.Data
{
    /// <summary>Информация о цвете в реестре</summary>
    public class ColorInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public ColorInfo(int id, string name, byte r, byte g, byte b)
        {
            Id = id;
            Name = name;
            R = r;
            G = g;
            B = b;
        }

        /// <summary>Расстояние между цветами в HSV пространстве</summary>
        public double DistanceToHSV(byte r, byte g, byte b)
        {
            var hsv1 = RgbToHsv(R, G, B);
            var hsv2 = RgbToHsv(r, g, b);

            double hDiff = Math.Abs(hsv1.H - hsv2.H);
            if (hDiff > 180) hDiff = 360 - hDiff;
            hDiff = hDiff / 180.0;

            double sDiff = Math.Abs(hsv1.S - hsv2.S) / 100.0;
            double vDiff = Math.Abs(hsv1.V - hsv2.V) / 100.0;

            return Math.Sqrt(
                2.0 * hDiff * hDiff +
                1.0 * sDiff * sDiff +
                1.0 * vDiff * vDiff
            );
        }

        /// <summary>Перевод RGB в HSV</summary>
        private static (double H, double S, double V) RgbToHsv(byte r, byte g, byte b)
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
    }

    /// <summary>Реестр цветов (10 базовых цветов)</summary>
    public static class ColorRegistry
    {
        public static List<ColorInfo> Colors { get; } = new List<ColorInfo>
        {
            new ColorInfo(1, "Красный", 200, 0, 0),
            new ColorInfo(2, "Зеленый", 0, 200, 0),
            new ColorInfo(3, "Желтый", 255, 255, 0),
            new ColorInfo(4, "Синий", 0, 150, 255),
            new ColorInfo(5, "Голубой", 0, 255, 255),
            new ColorInfo(6, "Фиолетовый", 150, 0, 255),
            new ColorInfo(7, "Коричневый", 180, 100, 20),
            new ColorInfo(8, "Оранжевый", 255, 120, 0),
            new ColorInfo(9, "Серый", 200, 200, 200),
            new ColorInfo(10, "Розовый", 255, 100, 200)
        };

        /// <summary>Найти ближайший цвет по HSV-расстоянию</summary>
        public static ColorInfo FindClosestColor(byte r, byte g, byte b)
        {
            ColorInfo closest = null;
            double minDistance = double.MaxValue;

            foreach (var color in Colors)
            {
                double distance = color.DistanceToHSV(r, g, b);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = color;
                }
            }
            return closest;
        }
    }

    /// <summary>Пиксель для кластеризации</summary>
    public class PixelCluster
    {
        public int X { get; set; }
        public int Y { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public int ClusterId { get; set; } = -1;
    }

    /// <summary>Данные одного кластера</summary>
    public class ClusterData
    {
        public int Id { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public int PixelCount { get; set; }
        public List<PixelCluster> Pixels { get; set; } = new List<PixelCluster>();

        public string DisplayRGB => $"({R},{G},{B})";
        public string ColorName => ColorRegistry.FindClosestColor(R, G, B)?.Name ?? "Неизвестный";
        public double Percentage { get; set; }
    }
}