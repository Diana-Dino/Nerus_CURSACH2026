using KursT1;
using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using static KursT1.SegmentColorResult;

namespace DrawingTestAnalyzer
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
                    StatusText.Text = $"✅ ШАБЛОН ЗАГРУЖЕН!\n" +
                                     $"Размер: {_templateResult.Width}x{_templateResult.Height}\n" +
                                     $"Сегментов: {_templateResult.Segments.Count(s => s.PixelCount > 0)}/12\n" +
                                     $"Границ: {_templateResult.TotalBoundaryPixels} пикселей\n\n" +
                                     $"👉 Теперь загрузите РИСУНОК ПОЛЬЗОВАТЕЛЯ";
                    StatusText.Foreground = System.Windows.Media.Brushes.Green;

                    // Создаём анализатор цветов с этим шаблоном
                    _colorAnalyzer = new ColorAnalyzer(_templateResult);
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
                    StatusText.Text = $"✅ РИСУНОК ПРОАНАЛИЗИРОВАН!\n" +
                                     $"Проанализировано зон: {_drawingResult.Segments.Count}";
                    StatusText.Foreground = System.Windows.Media.Brushes.Green;

                    // Показываем результаты в таблице
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

        private string CreateTextReport(DrawingAnalysisResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== АНАЛИЗ РИСУНОЧНОГО ТЕСТА ===\n");
            sb.AppendLine($"Дата: {DateTime.Now:dd.MM.yyyy HH:mm}\n");

            sb.AppendLine($"{"№",-4} {"Зона",-25} {"Домин. цвет",-20} {"Остальные цвета"}");
            sb.AppendLine(new string('-', 100));

            foreach (var seg in result.Segments)
            {
                sb.AppendLine($"{seg.SegmentId,-4} {seg.SegmentName,-25} {seg.DominantColorWithPercent,-20} {seg.OtherColorsText}");
            }

            return sb.ToString();
        }
    }
}

