using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KursT1
{
    /// <summary>
    /// Результат анализа цвета в одном сегменте
    /// </summary>
    public class SegmentColorResult
    {
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int TotalPixels { get; set; }

        // Кластеризация
        public Dictionary<int, int> ClusterCounts { get; set; } = new Dictionary<int, int>();
        public Dictionary<int, double> ClusterPercentages { get; set; } = new Dictionary<int, double>();
        public int DominantClusterId { get; set; }
        public string DominantClusterColorRGB { get; set; }
        public double DominantClusterPercentage { get; set; }

        // Интерпретация из реестра
        public string DominantClusterName { get; set; }
        public string DominantColorWithPercent => $"{DominantClusterColorRGB} ({DominantClusterPercentage:F1}%)";
    }

    /// <summary>
    /// Полный результат анализа рисунка
    /// </summary>
    public class DrawingAnalysisResult
    {
        public List<SegmentColorResult> Segments { get; set; } = new List<SegmentColorResult>();
        public ClusteringResult ClusteringResult { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
    }

    /// <summary>
    /// Анализатор рисунка пользователя
    /// </summary>
    public class ColorAnalyzer
    {
        private readonly TemplateAnalysisResult _template;
        private readonly IClusteringAlgorithm _clusteringAlgorithm;

        /// <summary>
        /// Конструктор анализатора
        /// </summary>
        public ColorAnalyzer(TemplateAnalysisResult template, IClusteringAlgorithm clusteringAlgorithm)
        {
            _template = template;
            _clusteringAlgorithm = clusteringAlgorithm;
        }



        /// <summary>
        /// Проанализировать рисунок пользователя
        /// </summary>
        public DrawingAnalysisResult AnalyzeDrawing(string filePath)
        {
            var result = new DrawingAnalysisResult();

            try
            {
                // 1. Загрузка изображения
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                // 2. Конвертация в BGR24
                var convertedBitmap = new FormatConvertedBitmap(
                    bitmap,
                    PixelFormats.Bgr24,
                    null,
                    0);

                int width = convertedBitmap.PixelWidth;
                int height = convertedBitmap.PixelHeight;

                // 3. Проверка размера
                if (width != _template.Width || height != _template.Height)
                {
                    result.ErrorMessage = $"Размер рисунка ({width}x{height}) не совпадает с шаблоном ({_template.Width}x{_template.Height})";
                    return result;
                }

                // 4. Чтение пикселей
                int stride = width * 3;
                byte[] pixels = new byte[height * stride];
                convertedBitmap.CopyPixels(pixels, stride, 0);

                // 5. Кластеризация (ГЛАВНЫЙ АНАЛИЗ!)
                result.ClusteringResult = _clusteringAlgorithm.Cluster(pixels, width, height);

                if (!result.ClusteringResult.IsSuccess)
                {
                    result.ErrorMessage = result.ClusteringResult.ErrorMessage;
                    return result;
                }

                // 6. Анализ по сегментам
                foreach (var segment in _template.Segments)
                {
                    var segmentResult = new SegmentColorResult
                    {
                        SegmentId = segment.Id,
                        SegmentName = segment.Name
                    };

                    // Инициализируем счётчики
                    foreach (var cluster in result.ClusteringResult.Clusters)
                    {
                        segmentResult.ClusterCounts[cluster.Id] = 0;
                    }

                    // Считаем пиксели по кластерам
                    foreach (var pixel in segment.Pixels)
                    {
                        int x = (int)pixel.X;
                        int y = (int)pixel.Y;

                        var clusterPixel = FindClusterPixel(result.ClusteringResult.Clusters, x, y);

                        if (clusterPixel != null)
                        {
                            segmentResult.ClusterCounts[clusterPixel.ClusterId]++;
                        }
                    }

                    segmentResult.TotalPixels = segment.Pixels.Count;

                    // 7. Вычисление процентов
                    if (segmentResult.TotalPixels > 0)
                    {
                        int dominantClusterId = -1;
                        double maxPercentage = 0;

                        foreach (int clusterId in segmentResult.ClusterCounts.Keys)
                        {
                            int count = segmentResult.ClusterCounts[clusterId];
                            double percentage = (double)count / segmentResult.TotalPixels * 100;
                            segmentResult.ClusterPercentages[clusterId] = Math.Round(percentage, 1);

                            if (percentage > maxPercentage)
                            {
                                maxPercentage = percentage;
                                dominantClusterId = clusterId;
                            }
                        }

                        segmentResult.DominantClusterId = dominantClusterId;
                        segmentResult.DominantClusterPercentage = maxPercentage;

                        // Находим доминирующий кластер
                        var dominantCluster = result.ClusteringResult.Clusters
                            .FirstOrDefault(c => c.Id == dominantClusterId);

                        if (dominantCluster != null)
                        {
                            segmentResult.DominantClusterColorRGB =
                                $"RGB({dominantCluster.R},{dominantCluster.G},{dominantCluster.B})";

                            var closestColor = ColorRegistry.FindClosestColor(
                                dominantCluster.R,
                                dominantCluster.G,
                                dominantCluster.B);
                            segmentResult.DominantClusterName = closestColor?.Name ?? "Неизвестный";
                        }
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

        /// <summary>
        /// Найти пиксель в кластерах по координатам
        /// </summary>
        private PixelCluster FindClusterPixel(List<ClusterData> clusters, int x, int y)
        {
            foreach (var cluster in clusters)
            {
                foreach (var pixel in cluster.Pixels)
                {
                    if (pixel.X == x && pixel.Y == y)
                    {
                        return pixel;
                    }
                }
            }
            return null;
        }
    }
}