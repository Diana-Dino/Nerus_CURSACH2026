using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace KursT1
{
    public partial class MainWindow : Window
    {
        private TemplateAnalysisResult _templateResult;
        private DrawingAnalysisResult _drawingResult;
        private readonly TemplateAnalyzer _templateAnalyzer = new TemplateAnalyzer();
        private ColorAnalyzer _colorAnalyzer;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Загрузить шаблон
        /// </summary>
        private void LoadTemplate_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Изображения|*.png;*.bmp;*.jpg|Все файлы|*.*",
                Title = "Выберите ШАБЛОН (чёрные границы + красные сегменты R=1-12)"
            };

            if (dialog.ShowDialog() == true)
            {
                TemplateImage.Source = new BitmapImage(new Uri(dialog.FileName));
                _templateResult = _templateAnalyzer.Analyze(dialog.FileName);

                if (_templateResult.IsSuccess)
                {
                    int zonesAnalyzed = 0;
                    foreach (var segment in _templateResult.Segments)
                    {
                        if (segment.PixelCount > 0)
                            zonesAnalyzed++;
                    }

                    StatusText.Text = $"✅ ШАБЛОН ЗАГРУЖЕН!\n" +
                                     $"Размер: {_templateResult.Width}x{_templateResult.Height}\n" +
                                     $"Сегментов: {zonesAnalyzed}/12\n" +
                                     $"Границ: {_templateResult.TotalBoundaryPixels} пикселей\n\n" +
                                     $"👉 Теперь загрузите РИСУНОК ПОЛЬЗОВАТЕЛЯ";
                    StatusText.Foreground = System.Windows.Media.Brushes.Green;

                    // Создаём кластеризатор с K=10
                    var clusteringAlgorithm = new KModesClustering(
                        k: 10,
                        maxIterations: 50,
                        useRegistrySeeds: true );
                    // Создаём анализатор с алгоритмом
                    _colorAnalyzer = new ColorAnalyzer(_templateResult, clusteringAlgorithm);
                }
                else
                {
                    StatusText.Text = $"❌ Ошибка шаблона:\n{_templateResult.ErrorMessage}";
                    StatusText.Foreground = System.Windows.Media.Brushes.Red;
                    _templateResult = null;
                    _colorAnalyzer = null;
                }
            }
        }

        /// <summary>
        /// Загрузить рисунок пользователя
        /// </summary>
        private void LoadDrawing_Click(object sender, RoutedEventArgs e)
        {
            if (_templateResult == null)
            {
                MessageBox.Show("Сначала загрузите ШАБЛОН!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = "Изображения|*.png;*.bmp;*.jpg|Все файлы|*.*",
                Title = "Выберите РИСУНОК ПОЛЬЗОВАТЕЛЯ"
            };

            if (dialog.ShowDialog() == true)
            {
                DrawingImage.Source = new BitmapImage(new Uri(dialog.FileName));
                _drawingResult = _colorAnalyzer.AnalyzeDrawing(dialog.FileName);

                if (_drawingResult.IsSuccess)
                {
                    // Показываем результаты кластеризации
                    if (_drawingResult.ClusteringResult != null)
                    {
                        var clusters = _drawingResult.ClusteringResult.Clusters;

                        string info = $"📊 K-Modes Кластеризация (K=10):\n\n";
                        info += $"Кластеров найдено: {clusters.Count}\n";
                        info += $"Всего пикселей: {_drawingResult.ClusteringResult.TotalPixels}\n";
                        info += $"Итераций: {_drawingResult.ClusteringResult.Iterations}\n";
                        info += $"Время: {_drawingResult.ClusteringResult.ExecutionTimeMs} мс\n\n";

                        foreach (var c in clusters)
                        {
                            info += $"• Кластер {c.Id}: {c.ColorName}\n";
                            info += $"  RGB{c.DisplayRGB} — {c.PixelCount} px ({c.Percentage:F1}%)\n\n";
                        }

                        MessageBox.Show(info, "Результаты кластеризации");
                    }

                    StatusText.Text = $"✅ РИСУНОК ПРОАНАЛИЗИРОВАН!\n" +
                                     $"Проанализировано зон: {_drawingResult.Segments.Count}";
                    StatusText.Foreground = System.Windows.Media.Brushes.Green;

                    ResultsGrid.ItemsSource = _drawingResult.Segments;
                }
                else
                {
                    StatusText.Text = $"❌ Ошибка анализа:\n{_drawingResult.ErrorMessage}";
                    StatusText.Foreground = System.Windows.Media.Brushes.Red;
                    ResultsGrid.ItemsSource = null;
                }
            }
        }

        /// <summary>
        /// Сохранить отчёт
        /// </summary>
        private void SaveReport_Click(object sender, RoutedEventArgs e)
        {
            if (_drawingResult == null)
            {
                MessageBox.Show("Сначала проанализируйте рисунок!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Текст|*.txt",
                Title = "Сохранить отчёт",
                FileName = "analysis_report"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string content = CreateTextReport(_drawingResult);
                    File.WriteAllText(dialog.FileName, content, Encoding.UTF8);
                    MessageBox.Show("Отчёт сохранён!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Очистить
        /// </summary>
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            TemplateImage.Source = null;
            DrawingImage.Source = null;
            ResultsGrid.ItemsSource = null;
            StatusText.Text = "Очищено. Загрузите шаблон заново.";
            _templateResult = null;
            _drawingResult = null;
            _colorAnalyzer = null;
        }

        /// <summary>
        /// Создать текстовый отчёт
        /// </summary>
        private string CreateTextReport(DrawingAnalysisResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== АНАЛИЗ РИСУНОЧНОГО ТЕСТА ===\n");
            sb.AppendLine($"Дата: {DateTime.Now:dd.MM.yyyy HH:mm}\n");

            sb.AppendLine("=== КЛАСТЕРИЗАЦИЯ K-MODES ===\n");
            sb.AppendLine($"Кластеров: {result.ClusteringResult.Clusters.Count}");
            sb.AppendLine($"Пикселей: {result.ClusteringResult.TotalPixels}");
            sb.AppendLine($"Итераций: {result.ClusteringResult.Iterations}");
            sb.AppendLine($"Время: {result.ClusteringResult.ExecutionTimeMs} мс\n");

            sb.AppendLine("Кластеры:\n");
            foreach (var cluster in result.ClusteringResult.Clusters)
            {
                sb.AppendLine($"  Кластер {cluster.Id}: RGB{cluster.DisplayRGB} - " +
                             $"{cluster.ColorName} - {cluster.PixelCount} пикселей ({cluster.Percentage:F1}%)");
            }

            sb.AppendLine("\n=== РЕЗУЛЬТАТЫ ПО ЗОНАМ ===\n");
            sb.AppendLine($"{"№",-4} {"Зона",-25} {"Домин. кластер",-25} {"%"}");
            sb.AppendLine(new string('-', 80));

            foreach (var seg in result.Segments)
            {
                sb.AppendLine($"{seg.SegmentId,-4} {seg.SegmentName,-25} {seg.DominantClusterColorRGB,-25} {seg.DominantClusterPercentage:F1}%");
            }

            return sb.ToString();
        }
    }
}