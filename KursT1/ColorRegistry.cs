using KursT1;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Windows.Media;

namespace KursT1
{
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

        // Расстояние меж цветами
        public double DistanceTo(byte r, byte g, byte b)
        {
            double dr = R - r;
            double dg = G - g;
            double db = B - b;
            return Math.Sqrt(dr * dr + dg * dg + db * db);
        }
    }
}


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
            new ColorInfo(7, "Коричневый", 160, 80, 30),
            new ColorInfo(8, "Оранжевый", 255, 120, 0),
            new ColorInfo(9, "Серый", 200, 200, 200),
            new ColorInfo(10, "Розовый", 255, 100, 200)
        };

    public static ColorInfo FindClosestColor(byte r, byte g, byte b)
    {
        ColorInfo closest = null;
        double minDistance = double.MaxValue;

        foreach (var color in Colors)
        {
            double distance = color.DistanceTo(r, g, b);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = color;
            }
        }
        return closest;
    }

    public static ColorInfo FindClosestColorWithThreshold(byte r, byte g, byte b, double maxDistance = 100)
    {
        var closest = FindClosestColor(r, g, b);
        if (closest != null && closest.DistanceTo(r, g, b) <= maxDistance)
        {
            return closest;
        }
        return new ColorInfo(0, "Неопределённый", r, g, b);
    }
}

