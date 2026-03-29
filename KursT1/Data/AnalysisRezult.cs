using System.Collections.Generic;

namespace KursT1.Data
{
    /// <summary>Результат кластеризации</summary>
    public class ClusteringResult
    {
        public List<ClusterData> Clusters { get; set; } = new List<ClusterData>();
        public int TotalPixels { get; set; }
        public int Iterations { get; set; }
        public double ExecutionTimeMs { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
    }

    /// <summary>Результат анализа цвета в одном сегменте</summary>
    public class SegmentColorResult
    {
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int TotalPixels { get; set; }

        public Dictionary<int, int> ClusterCounts { get; set; } = new Dictionary<int, int>();
        public Dictionary<int, double> ClusterPercentages { get; set; } = new Dictionary<int, double>();
        public int DominantClusterId { get; set; }

        public byte DominantClusterR { get; set; }
        public byte DominantClusterG { get; set; }
        public byte DominantClusterB { get; set; }
        public double DominantClusterPercentage { get; set; }
        public string DominantClusterName { get; set; }

        public string DominantColorWithPercent =>
            $"{DominantClusterName} ({DominantClusterPercentage:F1}%)";
    }

    /// <summary>Полный результат анализа рисунка</summary>
    public class DrawingAnalysisResult
    {
        public List<SegmentColorResult> Segments { get; set; } = new List<SegmentColorResult>();
        public ClusteringResult ClusteringResult { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
    }
}