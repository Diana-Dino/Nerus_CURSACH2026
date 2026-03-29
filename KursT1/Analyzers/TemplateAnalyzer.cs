using System;
using System.Linq;
using System.Windows;
using KursT1.Data;
using KursT1.Core;

namespace KursT1.Analyzers
{
    /// <summary>Анализатор шаблона — находит 12 сегментов по красному каналу</summary>
    public class TemplateAnalyzer : ImageAnalyzerBase
    {
        private static readonly string[] SegmentNames = {
            "Голова", "Шея", "Грудь",
            "Левая рука (плечо)", "Правая рука (плечо)",
            "Левая рука (низ)", "Правая рука (низ)",
            "Живот", "Бёдра", "Левая нога", "Правая нога", "Стопы"
        };

        private readonly int _expectedWidth;
        private readonly int _expectedHeight;

        public TemplateAnalyzer(int expectedWidth, int expectedHeight)
        {
            _expectedWidth = expectedWidth;
            _expectedHeight = expectedHeight;
        }

        /// <summary>Проанализировать шаблон</summary>
        public TemplateAnalysisResult Process(string filePath)
        {
            var result = new TemplateAnalysisResult();

            try
            {
                var (pixels, width, height, stride) = LoadImageAsBgr24(filePath);

                // Проверка размера
                if (width != _expectedWidth || height != _expectedHeight)
                {
                    result.ErrorMessage = $"Неверный размер: {width}x{height}. Ожидается {_expectedWidth}x{_expectedHeight}";
                    return result;
                }

                result.Width = width;
                result.Height = height;

                // Инициализация 12 сегментов
                for (int i = 0; i < 12; i++)
                {
                    result.Segments.Add(new SegmentData
                    {
                        Id = i + 1,
                        Name = SegmentNames[i]
                    });
                }

                // Анализ пикселей
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var (r, g, b) = ColorUtils.ReadPixel(pixels, x, y, width, stride);

                        // Чёрный = граница тела
                        if (r == 0 && g == 0 && b == 0)
                        {
                            result.Boundaries.Add(new Point(x, y));
                        }
                        // Красный маркер R=1-12 = сегмент
                        else if (g == 0 && b == 0 && r >= 1 && r <= 12)
                        {
                            result.Segments[r - 1].Pixels.Add(new Point(x, y));
                        }
                    }
                }

                // Валидация результата
                if (result.Segments.All(s => s.PixelCount == 0))
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