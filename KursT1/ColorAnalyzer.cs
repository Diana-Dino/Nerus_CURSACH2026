using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KursT1
{
    // Результат анализа цвета в сегменте
    public class SegmentColorResult
    {
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public Dictionary<string, int> ColorCounts { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, double> ColorPercentages { get; set; } = new Dictionary<string, double>();
        public int TotalPixels { get; set; }

        // Доминирующий цвет
        public string DominantColor { get; set; }
        public double DominantPercentage { get; set; }

        // Для отображения в таблице
        public string DominantColorWithPercent => $"{DominantColor} ({DominantPercentage:F1}%)";
        
        // Остальные цвета с процентами
        public string OtherColorsText
        {
            get
            {
                // Список для результатов
                var otherColors = new List<string>();

                // Перебираем названия цветов через .Keys
                foreach (string colorName in ColorPercentages.Keys)
                {
                    // Получаем процент по ключу
                    double percent = ColorPercentages[colorName];

                    // Пропускаем доминирующий цвет и неопределенные значения
                    if (colorName == DominantColor)
                        continue;
                    if (colorName == "Неопределённый")
                        continue;
                    if (percent == 0)
                        continue;

                    // Добавляем в список в формате "Цвет: 20.5%"
                    otherColors.Add($"{colorName}: {percent:F1}%");
                }

                // Сортируем по убыванию процента (LINQ)
                otherColors = otherColors
                    .OrderByDescending(text =>
                    {
                        // Извлекаем число из строки "Цвет: 20.5%"
                        var parts = text.Split(':');
                        if (parts.Length == 2 && double.TryParse(parts[1].Trim().Replace("%", ""), out double value))
                            return value;
                        return 0;
                    })
                    .ToList();

                // Добавляем "Неопределённый" если есть в конец
                if (ColorPercentages.ContainsKey("Неопределённый"))
                {
                    double undefinedPercent = ColorPercentages["Неопределённый"];
                    if (undefinedPercent > 0)
                    {
                        otherColors.Add($"Неопр.: {undefinedPercent:F1}%");
                    }
                }

                // Возвращаем строку через запятую или "—" если пусто
                return otherColors.Count > 0 ? string.Join(", ", otherColors) : "—";
            }
        }        // Цвет для отображения квадратика
        public Color DominantColorRgb
        {
            get
            {
                // switch expression = switch-case, но короче и современнее
                return DominantColor switch
                {
                    "Красный" => Colors.Red,
                    "Зеленый" => Colors.Green,
                    "Желтый" => Colors.Yellow,
                    "Синий" => Colors.Blue,
                    "Голубой" => Colors.Cyan,
                    "Фиолетовый" => Colors.Purple,
                    "Коричневый" => Color.FromRgb(160, 80, 30),
                    "Оранжевый" => Color.FromRgb(255, 120, 0),
                    "Серый" => Colors.Gray,
                    "Розовый" => Color.FromRgb(255, 100, 200),
                    _ => Colors.LightGray // по умолчанию
                };
            }
        }

        // Полный результат анализа рисунка
        public class DrawingAnalysisResult
        {
            public List<SegmentColorResult> Segments { get; set; } = new List<SegmentColorResult>();
            public string ErrorMessage { get; set; }
            public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
        }


        // Анализатор рисунка пользователя
        public class ColorAnalyzer
        {
            private readonly TemplateAnalysisResult _template;

            public ColorAnalyzer(TemplateAnalysisResult template)
            {
                _template = template;
            }

            public DrawingAnalysisResult AnalyzeDrawing(string filePath)
            {
                var result = new DrawingAnalysisResult();

                try
                {
                    // 1. Загружаем рисунок пользователя
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(filePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    // 2. Конвертируем в BGR24
                    var convertedBitmap = new FormatConvertedBitmap(
                        bitmap,
                        PixelFormats.Bgr24,
                        null,
                        0);

                    int width = convertedBitmap.PixelWidth;
                    int height = convertedBitmap.PixelHeight;

                    // 3. Проверяем размер
                    if (width != _template.Width || height != _template.Height)
                    {
                        result.ErrorMessage = $"Размер рисунка ({width}x{height}) не совпадает с шаблоном ({_template.Width}x{_template.Height})";
                        return result;
                    }

                    // 4. Читаем пиксели рисунка
                    int stride = width * 3;
                    byte[] pixels = new byte[height * stride];
                    convertedBitmap.CopyPixels(pixels, stride, 0);

                    // 5. Для каждого сегмента анализируем цвета
                    foreach (var segment in _template.Segments)
                    {
                        var segmentResult = new SegmentColorResult
                        {
                            SegmentId = segment.Id,
                            SegmentName = segment.Name
                        };

                        // Инициализируем счётчики для всех цветов (явный доступ по ключу)
                        foreach (string colorName in ColorRegistry.Colors.Select(c => c.Name))
                        {
                            segmentResult.ColorCounts[colorName] = 0;
                        }
                        segmentResult.ColorCounts["Неопределённый"] = 0;

                        // Считаем цвета в пикселях этого сегмента
                        foreach (var pixel in segment.Pixels)
                        {
                            int x = (int)pixel.X;
                            int y = (int)pixel.Y;
                            int index = (y * stride) + (x * 3);

                            byte b = pixels[index];
                            byte g = pixels[index + 1];
                            byte r = pixels[index + 2];

                            var closestColor = ColorRegistry.FindClosestColorWithThreshold(r, g, b, 100);
                            segmentResult.ColorCounts[closestColor.Name]++;
                        }

                        segmentResult.TotalPixels = segment.Pixels.Count;

                        // Вычисляем проценты (ваш стиль: .Keys + явный доступ)
                        if (segmentResult.TotalPixels > 0)
                        {
                            string dominantColor = null;
                            double maxPercentage = 0;

                            foreach (string colorName in segmentResult.ColorCounts.Keys)
                            {
                                int count = segmentResult.ColorCounts[colorName];
                                double percentage = (double)count / segmentResult.TotalPixels * 100;
                                segmentResult.ColorPercentages[colorName] = Math.Round(percentage, 1);

                                if (percentage > maxPercentage && colorName != "Неопределённый")
                                {
                                    maxPercentage = percentage;
                                    dominantColor = colorName;
                                }
                            }

                            segmentResult.DominantColor = dominantColor ?? "Нет данных";
                            segmentResult.DominantPercentage = maxPercentage;
                        }

                        result.Segments.Add(segmentResult);
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
}
