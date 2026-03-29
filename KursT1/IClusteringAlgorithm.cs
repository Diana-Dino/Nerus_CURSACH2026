using System;
using System.Collections.Generic;

namespace KursT1
{
    /// <summary>
    /// Интерфейс для алгоритмов кластеризации
    /// </summary>
    public interface IClusteringAlgorithm
    {
        /// <summary>
        /// Выполнить кластеризацию пикселей
        /// </summary>
        ClusteringResult Cluster(byte[] pixels, int width, int height);
    }
}