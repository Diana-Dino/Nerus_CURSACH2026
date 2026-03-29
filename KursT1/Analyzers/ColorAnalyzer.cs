using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KursT1.Data;
using KursT1.Clustering;

namespace KursT1.Analyzers
{
    public class ColorAnalyzer
    {
        private readonly TemplateAnalysisResult _template;
        private readonly KModesClustering _clustering;

        public ColorAnalyzer(TemplateAnalysisResult template, KModesClustering clustering)
        {
            _template = template;
            _clustering = clustering;
        }

        public DrawingAnalysisResult AnalyzeDrawing(string filePath)
        {
            var result = new DrawingAnalysisResult();

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                var convertedBitmap = new FormatConvertedBitmap(
                    bitmap, PixelFormats.Bgr24, null, 0);

                int width = convertedBitmap.PixelWidth;
                int height = convertedBitmap.PixelHeight;

                if (width != _template.Width || height != _template.Height)
                {
                    result.ErrorMessage = $"Размер рисунка ({width}x{height}) не совпадает с шаблоном ({_template.Width}x{_template.Height})";
                    return result;
                }

                int stride = width * 3;
                byte[] pixels = new byte[height * stride];
                convertedBitmap.CopyPixels(pixels, stride, 0);

                result.ClusteringResult = _clustering.Cluster(pixels, width, height);

                if (!result.ClusteringResult.IsSuccess)
                {
                    result.ErrorMessage = result.ClusteringResult.ErrorMessage;
                    return result;
                }

                foreach (var segment in _template.Segments)
                {
                    var segmentResult = new SegmentColorResult
                    {
                        SegmentId = segment.Id,
                        SegmentName = segment.Name
                    };

                    foreach (var cluster in result.ClusteringResult.Clusters)
                    {
                        segmentResult.ClusterCounts[cluster.Id] = 0;
                    }

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

                        var dominantCluster = result.ClusteringResult.Clusters
                            .FirstOrDefault(c => c.Id == dominantClusterId);

                        if (dominantCluster != null)
                        {
                            segmentResult.DominantClusterR = dominantCluster.R;
                            segmentResult.DominantClusterG = dominantCluster.G;
                            segmentResult.DominantClusterB = dominantCluster.B;
                            segmentResult.DominantClusterName =
                                ColorRegistry.FindClosestColor(
                                    dominantCluster.R,
                                    dominantCluster.G,
                                    dominantCluster.B)?.Name ?? "Неизвестный";
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