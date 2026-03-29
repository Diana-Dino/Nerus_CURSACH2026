using System;
using System.Collections.Generic;
using System.Linq;

namespace KursT1
{
    /// <summary>
    /// Пиксель для кластеризации
    /// </summary>
    public class PixelCluster
    {
        public int X { get; set; }
        public int Y { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public int ClusterId { get; set; } = -1;
    }

    /// <summary>
    /// Данные одного кластера
    /// </summary>
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

    /// <summary>
    /// Результат кластеризации
    /// </summary>
    public class ClusteringResult
    {
        public List<ClusterData> Clusters { get; set; } = new List<ClusterData>();
        public int TotalPixels { get; set; }
        public int Iterations { get; set; }
        public double ExecutionTimeMs { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
    }

    /// <summary>
    /// K-Modes алгоритм с инициализацией из реестра цветов
    /// </summary>
    public class KModesClustering : IClusteringAlgorithm
    {
        private readonly int _k;
        private readonly int _maxIterations;
        private readonly bool _useRegistrySeeds;  // ← НОВОЕ: флаг использования реестра

        /// <summary>
        /// Конструктор кластеризатора
        /// </summary>
        /// <param name="k">Количество кластеров</param>
        /// <param name="maxIterations">Максимум итераций</param>
        /// <param name="useRegistrySeeds">Использовать цвета из реестра как начальные центры</param>
        public KModesClustering(int k = 10, int maxIterations = 50, bool useRegistrySeeds = true)
        {
            _k = k;
            _maxIterations = maxIterations;
            _useRegistrySeeds = useRegistrySeeds;
        }

        /// <summary>
        /// Выполнить кластеризацию пикселей изображения
        /// </summary>
        public ClusteringResult Cluster(byte[] pixels, int width, int height)
        {
            var result = new ClusteringResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // 1. Определение пикселей для работы
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

                // 2. Инициализация мод (центров кластеров)
                var modes = _useRegistrySeeds
                    ? InitializeFromRegistry(pixelList)  // ← из реестра
                    : InitializeRandom(pixelList);        // ← случайно

                // 3. Итерации кластеризации
                int iterations = 0;
                bool changed = true;

                while (changed && iterations < _maxIterations)
                {
                    changed = false;
                    iterations++;

                    // 3.1. Очищаем кластеры
                    foreach (var mode in modes)
                    {
                        mode.Pixels.Clear();
                        mode.PixelCount = 0;
                    }

                    // 3.2. Назначаем пиксели ближайшей моде
                    foreach (var pixel in pixelList)
                    {
                        int closestId = 0;
                        double minDistance = double.MaxValue;

                        for (int i = 0; i < modes.Count; i++)
                        {
                            double distance = CalculateDistance(
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

                    // 3.3. Обновляем моды (наиболее частый цвет)
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

                // 4. Сохранение непустых кластеров
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

                // 5. Пересчёт процентов
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

        /// <summary>
        /// Инициализация центров из реестра цветов
        /// </summary>
        private List<ClusterData> InitializeFromRegistry(List<PixelCluster> pixelList)
        {
            var modes = new List<ClusterData>();
            int id = 0;

            // Берём цвета из реестра как начальные центры
            foreach (var colorInfo in ColorRegistry.Colors)
            {
                if (id >= _k) break;  // Не больше K кластеров

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

            // Если в реестре меньше цветов чем K, добавляем случайные пиксели
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

        /// <summary>
        /// Случайная инициализация центров (оригинальный способ)
        /// </summary>
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

        /// <summary>
        /// Проверка на фон
        /// </summary>
        private bool IsBackground(byte r, byte g, byte b)
        {
            if (r > 245 && g > 245 && b > 245)
                return true;
            if (r < 10 && g < 10 && b < 10)
                return true;
            return false;
        }

        /// <summary>
        /// Расстояние между цветами
        /// </summary>
        private double CalculateDistance(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
        {
            double dr = r1 - r2;
            double dg = g1 - g2;
            double db = b1 - b2;
            return Math.Sqrt(dr * dr + dg * dg + db * db);
        }

        /// <summary>
        /// Вычисление новой моды (наиболее частый цвет)
        /// </summary>
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

            return new ClusterData
            {
                R = r,
                G = g,
                B = b
            };
        }
    }
}