using System;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using KursT1.Analyzers;
using KursT1.Clustering;
using KursT1.Data;
using KursT1.Export;

namespace KursT1
{
    public partial class MainWindow : Window
    {
        private TemplateAnalyzer _templateAnalyzer;
        private KModesClustering _clustering;
        private ColorAnalyzer _colorAnalyzer;
        private TemplateAnalysisResult _templateResult;
        private DrawingAnalysisResult _drawingResult;
        private string _currentDrawingPath;

        public MainWindow()
        {
            InitializeComponent();
            InitializeAnalyzers();
        }

        private void InitializeAnalyzers()
        {
            _templateAnalyzer = new TemplateAnalyzer(210, 360);
            _clustering = new KModesClustering(k: 10, maxIterations: 50, useRegistrySeeds: true);
        }

        private void LoadTemplate_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Изображения|*.png;*.jpg;*.bmp",
                Title = "Выберите файл шаблона"
            };

            if (dialog.ShowDialog() == true)
            {
                _templateResult = _templateAnalyzer.Process(dialog.FileName);

                if (_templateResult.IsSuccess)
                {
                    _colorAnalyzer = new ColorAnalyzer(_templateResult, _clustering);
                    StatusText.Text = $"Шаблон загружен: {_templateResult.Segments.Count} сегментов, {_templateResult.Boundaries.Count} границ";

                    int segmentsWithPixels = _templateResult.Segments.Count(s => s.PixelCount > 0);
                    MessageBox.Show(
                        $"Шаблон успешно загружен!\n\n" +
                        $"Найдено сегментов: {_templateResult.Segments.Count}\n" +
                        $"С сегментами: {segmentsWithPixels}\n" +
                        $"Граничных пикселей: {_templateResult.Boundaries.Count}",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Ошибка загрузки шаблона:\n{_templateResult.ErrorMessage}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "Ошибка загрузки шаблона";
                }
            }
        }

        private void LoadDrawing_Click(object sender, RoutedEventArgs e)
        {
            if (_colorAnalyzer == null)
            {
                MessageBox.Show("Сначала загрузите шаблон!", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = "Изображения|*.png;*.jpg;*.bmp",
                Title = "Выберите файл рисунка пользователя"
            };

            if (dialog.ShowDialog() == true)
            {
                _currentDrawingPath = dialog.FileName;

                // Отображаем рисунок
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_currentDrawingPath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    DrawingImage.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки изображения:\n{ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Анализируем
                _drawingResult = _colorAnalyzer.AnalyzeDrawing(_currentDrawingPath);

                if (_drawingResult.IsSuccess)
                {
                    ResultsGrid.ItemsSource = _drawingResult.Segments;

                    int filledSegments = _drawingResult.Segments.Count(s => s.DominantClusterId >= 0 && s.DominantClusterPercentage > 0);
                    StatusText.Text = $"Анализ завершён. Кластеров: {_drawingResult.ClusteringResult.Clusters.Count}, Заполнено: {filledSegments}/12";

                    MessageBox.Show(
                        $"Анализ завершён!\n\n" +
                        $"Кластеров найдено: {_drawingResult.ClusteringResult.Clusters.Count}\n" +
                        $"Заполнено сегментов: {filledSegments}/12\n" +
                        $"Итераций: {_drawingResult.ClusteringResult.Iterations}\n" +
                        $"Время: {_drawingResult.ClusteringResult.ExecutionTimeMs} мс",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Ошибка анализа:\n{_drawingResult.ErrorMessage}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "Ошибка анализа рисунка";
                }
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (_drawingResult == null)
            {
                MessageBox.Show("Сначала выполните анализ!", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "CSV файл|*.csv|JSON файл|*.json",
                Title = "Сохранить отчёт",
                FileName = "отчёт_анализа"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    if (dialog.FileName.EndsWith(".csv"))
                    {
                        ReportExporter.SaveAsCsv(_drawingResult, dialog.FileName);
                    }
                    else
                    {
                        ReportExporter.SaveAsJson(_drawingResult, dialog.FileName);
                    }
                    MessageBox.Show($"Отчёт сохранён:\n{dialog.FileName}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения:\n{ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            DrawingImage.Source = null;
            ResultsGrid.ItemsSource = null;
            StatusText.Text = "Готов к работе";
            _templateResult = null;
            _drawingResult = null;
            _colorAnalyzer = null;
            _currentDrawingPath = null;
            InitializeAnalyzers();
            MessageBox.Show("Все данные очищены", "Очистка",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}