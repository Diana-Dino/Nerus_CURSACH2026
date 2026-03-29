using System;
using System.Collections.Generic;
using System.Linq;
using KursT1.Data;
using KursT1.Core;

namespace KursT1.Clustering
{
    /// <summary>K-Modes алгоритм с HSV расстоянием</summary>
    public class KModesClustering
    {
        private readonly int _k;
        private readonly int _maxIterations;
        private readonly bool _useRegistrySeeds;

        public KModesClustering(int k = 10, int maxIterations = 50, bool useRegistrySeeds = true)
        {
            _k = k;
            _maxIterations = maxIterations;
            _useRegistrySeeds = useRegistrySeeds;
        }

        public ClusteringResult Cluster(byte[] pixels, int width, int height)
        {
            var result = new ClusteringResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                int stride = width * 3;
                var pixelList = new List<PixelCluster>();

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = (y * stride) + (x * 3);
                        byte b = pixels[index];
                        byte g = pixels[index + 1];
                        byte r = pixels[index + 2];

                        if (IsBackground(r, g, b))
                            continue;

                        pixelList.Add(new PixelCluster
                        {
                            X = x,
                            Y = y,
                            R = r,
                            G = g,
                            B = b
                        });
                    }
                }

                result.TotalPixels = pixelList.Count;

                if (pixelList.Count == 0)
                {
                    result.ErrorMessage = "Нет пикселей для кластеризации";
                    return result;
                }

                var modes = _useRegistrySeeds
                    ? InitializeFromRegistry(pixelList)
                    : InitializeRandom(pixelList);

                int iterations = 0;
                bool changed = true;

                while (changed && iterations < _maxIterations)
                {
                    changed = false;
                    iterations++;

                    foreach (var mode in modes)
                    {
                        mode.Pixels.Clear();
                        mode.PixelCount = 0;
                    }

                    foreach (var pixel in pixelList)
                    {
                        int closestId = 0;
                        double minDistance = double.MaxValue;

                        for (int i = 0; i < modes.Count; i++)
                        {
                            double distance = CalculateDistanceHSV(
                                pixel.R, pixel.G, pixel.B,
                                modes[i].R, modes[i].G, modes[i].B);

                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                closestId = i;
                            }
                        }

                        if (pixel.ClusterId != closestId)
                        {
                            pixel.ClusterId = closestId;
                            changed = true;
                        }

                        modes[closestId].Pixels.Add(pixel);
                        modes[closestId].PixelCount++;
                    }

                    foreach (var mode in modes)
                    {
                        if (mode.Pixels.Count > 0)
                        {
                            var newMode = CalculateNewMode(mode.Pixels);
                            mode.R = newMode.R;
                            mode.G = newMode.G;
                            mode.B = newMode.B;
                        }
                    }
                }

                result.Clusters = new List<ClusterData>();
                int clusterId = 0;

                foreach (var mode in modes)
                {
                    if (mode.PixelCount > 0)
                    {
                        mode.Id = clusterId;

                        foreach (var pixel in mode.Pixels)
                        {
                            pixel.ClusterId = clusterId;
                        }

                        result.Clusters.Add(mode);
                        clusterId++;
                    }
                }

                result.Iterations = iterations;

                int total = result.TotalPixels;
                foreach (var cluster in result.Clusters)
                {
                    if (total > 0)
                    {
                        cluster.Percentage = (double)cluster.PixelCount / total * 100;
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Ошибка: {ex.Message}";
            }

            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            return result;
        }

        private List<ClusterData> InitializeFromRegistry(List<PixelCluster> pixelList)
        {
            var modes = new List<ClusterData>();
            int id = 0;

            foreach (var colorInfo in ColorRegistry.Colors)
            {
                if (id >= _k) break;

                modes.Add(new ClusterData
                {
                    Id = id,
                    R = colorInfo.R,
                    G = colorInfo.G,
                    B = colorInfo.B,
                    Pixels = new List<PixelCluster>(),
                    PixelCount = 0
                });
                id++;
            }

            var random = new Random(42);
            var usedIndices = new HashSet<int>();

            while (modes.Count < _k && modes.Count < pixelList.Count)
            {
                int idx;
                do
                {
                    idx = random.Next(pixelList.Count);
                } while (usedIndices.Contains(idx));

                usedIndices.Add(idx);
                var p = pixelList[idx];

                modes.Add(new ClusterData
                {
                    Id = modes.Count,
                    R = p.R,
                    G = p.G,
                    B = p.B,
                    Pixels = new List<PixelCluster>(),
                    PixelCount = 0
                });
            }

            return modes;
        }

        private List<ClusterData> InitializeRandom(List<PixelCluster> pixelList)
        {
            var modes = new List<ClusterData>();
            var random = new Random(42);
            var usedIndices = new HashSet<int>();

            for (int i = 0; i < _k && i < pixelList.Count; i++)
            {
                int idx;
                do
                {
                    idx = random.Next(pixelList.Count);
                } while (usedIndices.Contains(idx));

                usedIndices.Add(idx);
                var p = pixelList[idx];

                modes.Add(new ClusterData
                {
                    Id = i,
                    R = p.R,
                    G = p.G,
                    B = p.B,
                    Pixels = new List<PixelCluster>()
                });
            }

            return modes;
        }

        private bool IsBackground(byte r, byte g, byte b)
        {
            if (r > 245 && g > 245 && b > 245) return true;
            if (r < 10 && g < 10 && b < 10) return true;
            return false;
        }

        private double CalculateDistanceHSV(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
        {
            var hsv1 = ColorUtils.RgbToHsv(r1, g1, b1);
            var hsv2 = ColorUtils.RgbToHsv(r2, g2, b2);

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

        private ClusterData CalculateNewMode(List<PixelCluster> pixels)
        {
            var colorGroups = new Dictionary<string, List<PixelCluster>>();

            foreach (var pixel in pixels)
            {
                string key = $"{pixel.R},{pixel.G},{pixel.B}";
                if (!colorGroups.ContainsKey(key))
                {
                    colorGroups[key] = new List<PixelCluster>();
                }
                colorGroups[key].Add(pixel);
            }

            string mostCommonKey = "";
            int maxCount = 0;

            foreach (var kvp in colorGroups)
            {
                if (kvp.Value.Count > maxCount)
                {
                    maxCount = kvp.Value.Count;
                    mostCommonKey = kvp.Key;
                }
            }

            var parts = mostCommonKey.Split(',');
            byte r = byte.Parse(parts[0]);
            byte g = byte.Parse(parts[1]);
            byte b = byte.Parse(parts[2]);

            return new ClusterData { R = r, G = g, B = b };
        }
    }
}