using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace KursT1
{
    /// <summary>
    /// Информация об одном цвете в списке
    /// </summary>
    public class ColorInfo
    {
        public int Id { get; set; }// Уникальный номер цвета (1-10)
        public string Name { get; set; }// Название цвета для отображения

        // Составляющие цвета (0-255)
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        /// <summary>
        /// Конструктор цвета
        /// </summary>
        public ColorInfo(int id, string name, byte r, byte g, byte b)
        {
            Id = id;
            Name = name;
            R = r;
            G = g;
            B = b;
        }

        /// <summary>
        /// Вычислить расстояние до другого цвета (евклидово)
        /// Чем меньше расстояние - тем цвета похожее
        /// </summary>
        public double DistanceTo(byte r, byte g, byte b)
        {
            // Разница по каждому каналу
            double dr = R - r;
            double dg = G - g;
            double db = B - b;

            // Евклидово расстояние в пространстве RGB
            return Math.Sqrt(dr * dr + dg * dg + db * db);
        }
    }

    /// <summary>
    /// Статический список из 10 базовых цветов
    /// Используется для интерпретации названий кластеров
    /// </summary>
    public static class ColorRegistry
    {
        // Список всех цветов
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

        /// <summary>
        /// Найти ближайший цвет в списке
        /// </summary>
        public static ColorInfo FindClosestColor(byte r, byte g, byte b)
        {
            ColorInfo closest = null;
            double minDistance = double.MaxValue;

            // Перебираем все цвета списка
            foreach (var color in Colors)
            {
                // Вычисляем расстояние до текущего цвета
                double distance = color.DistanceTo(r, g, b);

                // Если ближе предыдущего — запоминаем
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = color;
                }
            }

            return closest;
        }

        /// <summary>
        /// Найти ближайший цвет с порогом
        /// </summary>
        public static ColorInfo FindClosestColorWithThreshold(byte r, byte g, byte b, double maxDistance = 100)
        {
            var closest = FindClosestColor(r, g, b); 

            if (closest != null && closest.DistanceTo(r, g, b) <= maxDistance)
            {
                return closest;
            }

            // Если слишком далеко от всех цветов списка
            return new ColorInfo(0, "Неопределённый", r, g, b);
        }
    }
}