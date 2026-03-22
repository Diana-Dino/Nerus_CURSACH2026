using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;

namespace DrawingTestAnalyzer
{
    public partial class MainWindow : Window
    {
        private TemplateAnalysisResult _currentResult;
        private readonly TemplateAnalyzer _analyzer = new TemplateAnalyzer();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadTemplate_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Изображения|*.png;*.bmp;*.jpg|Все файлы|*.*",
                Title = "Выберите шаблон человека"
            };

            if (dialog.ShowDialog() == true)
            {
                // Показываем изображение
                TemplateImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri(dialog.FileName));

                // Анализируем шаблон
                _currentResult = _analyzer.Analyze(dialog.FileName);

                // Показываем результаты
                if (_currentResult.IsSuccess)
                {
                    StatusText.Text = $"✅ Шаблон загружен успешно!\n" +
                                     $"Размер: {_currentResult.Width}x{_currentResult.Height}\n" +
                                     $"Найдено сегментов: {_currentResult.Segments.Count(s => s.PixelCount > 0)}/12";
                    StatusText.Foreground = System.Windows.Media.Brushes.Green;

                    SegmentsGrid.ItemsSource = _currentResult.Segments;

                    BoundaryText.Text = $"Границы (чёрные пиксели): {_currentResult.TotalBoundaryPixels}";
                }
                else
                {
                    StatusText.Text = $"❌ Ошибка:\n{_currentResult.ErrorMessage}";
                    StatusText.Foreground = System.Windows.Media.Brushes.Red;
                    SegmentsGrid.ItemsSource = null;
                    BoundaryText.Text = "";
                }
            }
        }

        private void SaveData_Click(object sender, RoutedEventArgs e)
        {
            if (_currentResult == null || !_currentResult.IsSuccess)
            {
                MessageBox.Show("Сначала загрузите корректный шаблон!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "JSON|*.json|Текст|*.txt",
                Title = "Сохранить данные шаблона",
                FileName = "template_data"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string content;
                    if (dialog.FileName.EndsWith(".json"))
                    {
                        var options = new JsonSerializerOptions { WriteIndented = true };
                        content = JsonSerializer.Serialize(_currentResult, options);
                    }
                    else
                    {
                        content = CreateTextReport(_currentResult);
                    }

                    File.WriteAllText(dialog.FileName, content);
                    MessageBox.Show("Данные сохранены!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string CreateTextReport(TemplateAnalysisResult result)
        {
            var report = $"=== АНАЛИЗ ШАБЛОНА ===\n\n";
            report += $"Размер: {result.Width}x{result.Height}\n";
            report += $"Границы: {result.TotalBoundaryPixels} пикселей\n\n";
            report += $"СЕГМЕНТЫ:\n";
            report += $"{"№",-4} {"Зона",-20} {"Пикселей",10}\n";
            report += new string('-', 40) + "\n";

            foreach (var seg in result.Segments)
            {
                report += $"{seg.Id,-4} {seg.Name,-20} {seg.PixelCount,10}\n";
            }

            return report;
        }
    }
}
