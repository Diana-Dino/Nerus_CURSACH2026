using System.Collections.Generic;
using System.Windows;

namespace KursT1.Data
{
    /// <summary>Данные одного сегмента тела</summary>
    public class SegmentData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Point> Pixels { get; set; } = new List<Point>();
        public int PixelCount => Pixels.Count;
    }

    /// <summary>Результат анализа шаблона</summary>
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
}