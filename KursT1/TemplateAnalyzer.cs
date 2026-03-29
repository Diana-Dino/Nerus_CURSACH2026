using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KursT1
{
    /// <summary>
    /// Данные одного сегмента (зоны тела)
    /// </summary>
    public class SegmentData
    {
        public int Id { get; set; }// Номер сегмента (1-12)
        public string Name { get; set; } // Название сегмента 
        public List<Point> Pixels { get; set; } = new List<Point>(); // Список координат пикселей, принадлежащих этому сегменту
        public int PixelCount => Pixels.Count; // Количество пикселей в сегменте 
    }

    /// <summary>
    /// Результат анализа всего шаблона
    /// </summary>
    public class TemplateAnalysisResult
    {
        public int Width { get; set; }// Ширина изображения в пикселях (должна быть 210)
        public int Height { get; set; }// Высота изображения в пикселях (должна быть 360)
        public List<SegmentData> Segments { get; set; } = new List<SegmentData>(); // Список всех 12 сегментов тела
        public List<Point> Boundaries { get; set; } = new List<Point>();// Список координат чёрных пикселей (границы тела)
        public int TotalBoundaryPixels => Boundaries.Count;// Количество пикселей границ (вычисляется автоматически)
        public string ErrorMessage { get; set; } // Текст ошибки
        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);// Флаг успеха 
    }

    /// <summary>
    /// Анализатор шаблона — находит сегменты по красному каналу
    /// </summary>
    public class TemplateAnalyzer
    {
        // Названия 12 сегментов 
        private static readonly string[] SegmentNames = new string[]
        {
            "Голова",               // 1
            "Шея",                  // 2
            "Грудь",                // 3
            "Левая рука (плечо)",   // 4
            "Правая рука (плечо)",  // 5
            "Левая рука (низ)",     // 6
            "Правая рука (низ)",    // 7
            "Живот",                // 8
            "Бёдра",                // 9
            "Левая нога",           // 10
            "Правая нога",          // 11
            "Стопы"                 // 12
        };

        /// <summary>
        /// Анализ изображения шаблона
        /// </summary>
        /// <param name="filePath">Путь к файлу шаблона</param>
        /// <returns>Результат анализа</returns>
        public TemplateAnalysisResult Analyze(string filePath)
        {
            // Создаём пустой результат
            var result = new TemplateAnalysisResult();

            try
            {
         // 1. Загрузка изображения
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);  // Путь к файлу
                bitmap.CacheOption = BitmapCacheOption.OnLoad;  // Загрузка сразу в память
                bitmap.EndInit();
                bitmap.Freeze();  // Сделать неизменяемым 

         // 2. Конвертация в BGR24

                // BGR24 = 3 байта на пиксель (Blue, Green, Red)
                // Нужно для прямого доступа к байтам пикселей
                var convertedBitmap = new FormatConvertedBitmap(
                    bitmap,
                    PixelFormats.Bgr24,  // Формат
                    null,               
                    0                   
                );

                result.Width = convertedBitmap.PixelWidth;   
                result.Height = convertedBitmap.PixelHeight; 

         // 3. Проверка размера

                if (result.Width != 210 || result.Height != 360)
                {
                    result.ErrorMessage = $"Неверный размер: {result.Width}x{result.Height}. Ожидается 210x360";
                    return result;  // Ошибка
                }

         // 4. Инициализация 12 сегментов

                for (int i = 0; i < 12; i++)
                {
                    result.Segments.Add(new SegmentData
                    {
                        Id = i + 1,              // Номера 1-12
                        Name = SegmentNames[i]   // Название из массива
                    });
                }

         // 5. Чтение пикселей

                // stride - сколько байт в одной строке изображения
                // 210 пикселей × 3 байта = 630 байт 
                int stride = result.Width * 3;

                // Массив для хранения всех байтов изображения
                // Размер = высота × stride
                byte[] pixels = new byte[result.Height * stride];

                // Копируем пиксели в массив
                convertedBitmap.CopyPixels(pixels, stride, 0);

         // 6. Анализ пикселей по отдельности

                // Проходим по всем строкам 
                for (int y = 0; y < result.Height; y++)
                {
                    // Проходим по всем столбцам 
                    for (int x = 0; x < result.Width; x++)
                    {
                        // Вычисляем индекс в массиве байтов
                        // (y * stride) = начало строки
                        // (x * 3) = смещение пикселя в строке (3 байта на пиксель)
                        int index = (y * stride) + (x * 3);

                        // Читаем компоненты цвета
                        // В формате BGR: сначала Blue, потом Green, потом Red
                        byte b = pixels[index];      // Blue
                        byte g = pixels[index + 1];  // Green
                        byte r = pixels[index + 2];  // Red

         // 7. Классификация пикселя (фон, граница, рисунок)

                        // Чёрный цвет (R=0, G=0, B=0) = границы тела
                        if (r == 0 && g == 0 && b == 0)
                        {
                            result.Boundaries.Add(new Point(x, y));
                        }
                        // Красный цвет с R=1-12 (G=0, B=0) - сегменты
                        // Значение R определяет номер сегмента
                        else if (g == 0 && b == 0 && r >= 1 && r <= 12)
                        {
                            int segmentIndex = r - 1;  // преобразуем R=1 в индекс 
                            result.Segments[segmentIndex].Pixels.Add(new Point(x, y));
                        }
                        // Остальные цвета = фон (игнорируем)
                    }
                }

         // 8. Проверка
                // Считаем сколько сегментов содержат пиксели
                int segmentsFound = 0;
                foreach (var segment in result.Segments)
                {
                    if (segment.PixelCount > 0)
                    {
                        segmentsFound++;
                    }
                }

                if (segmentsFound == 0)
                {
                    result.ErrorMessage = "Не найдено сегментов! Проверьте шаблон (красный R=1-12).";
                }
                else if (result.Boundaries.Count == 0)
                {
                    result.ErrorMessage = "Не найдены границы! Проверьте шаблон (чёрные пиксели).";
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Ошибка: {ex.Message}";
            }

            return result;
        }
    }
}