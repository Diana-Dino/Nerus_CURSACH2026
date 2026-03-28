using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KursT1
{
    // Данные одного сегмента
    public class SegmentData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Point> Pixels { get; set; } = new List<Point>();
        public int PixelCount => Pixels.Count;
    }

    // Результат анализа всего шаблона
    public class TemplateAnalysisResult
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public List<SegmentData> Segments { get; set; } = new List<SegmentData>();
        public List<Point> Boundaries { get; set; } = new List<Point>();
        public int TotalBoundaryPixels => Boundaries.Count;
        public string ErrorMessage { get; set; }
        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
    }

    // Анализатор шаблона
    public class TemplateAnalyzer
    {
        // Названия 12 сегментов 
        private static readonly string[] SegmentNames = new string[]
        {
            "Голова",               // 1
            "Шея",                  // 2
            "Левая рука (плечо)",   // 3
            "Правая рука (плечо)",  // 4
            "Левая рука (низ)",     // 5
            "Правая рука (низ)",    // 6
            "Грудь",                // 7
            "Живот",                // 8
            "Бёдра",                // 9
            "Правая нога",          // 10
            "Левая нога",           // 11
            "Стопы"                 // 12

        };

        public TemplateAnalysisResult Analyze(string filePath)
        {
            var result = new TemplateAnalysisResult();

            try
            {
                // 1. Загружаем изображение
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                // 2. Конвертируем в формат BGR24 для доступа к пикселям
                var convertedBitmap = new FormatConvertedBitmap(
                    bitmap,
                    PixelFormats.Bgr24,
                    null,
                    0);

                result.Width = convertedBitmap.PixelWidth;
                result.Height = convertedBitmap.PixelHeight;

                // 3. Проверяем размер
                if (result.Width != 210 || result.Height != 360)
                {
                    result.ErrorMessage = $"Неверный размер: {result.Width}x{result.Height}. Ожидается 210x360";
                    return result;
                }

                // 4. Инициализируем 12 сегментов
                for (int i = 0; i < 12; i++)
                {
                    result.Segments.Add(new SegmentData
                    {
                        Id = i + 1,
                        Name = SegmentNames[i]
                    });
                }

                // 5. Читаем все пиксели
                int stride = result.Width * 3; // 3 байта на пиксель (B, G, R)
                byte[] pixels = new byte[result.Height * stride];
                convertedBitmap.CopyPixels(pixels, stride, 0);

                // 6. Анализируем каждый пиксель
                for (int y = 0; y < result.Height; y++)
                {
                    for (int x = 0; x < result.Width; x++)
                    {
                        int index = (y * stride) + (x * 3);
                        byte b = pixels[index];
                        byte g = pixels[index + 1];
                        byte r = pixels[index + 2];

                        // Чёрный цвет = границы
                        if (r == 0 && g == 0 && b == 0)
                        {
                            result.Boundaries.Add(new Point(x, y));
                        }
                        // Красный R=1-12 = сегменты
                        else if (g == 0 && b == 0 && r >= 1 && r <= 12)
                        {
                            int segmentIndex = r - 1;
                            result.Segments[segmentIndex].Pixels.Add(new Point(x, y));
                        }
                        // Остальные = фон (игнорируем)
                    }
                }

                // 7. Проверка: найдены ли сегменты
                int segmentsFound = result.Segments.Count(s => s.PixelCount > 0);
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
