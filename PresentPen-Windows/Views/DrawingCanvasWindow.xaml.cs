using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using PresentPen.Models;

namespace PresentPen.Views
{
    public partial class DrawingCanvasWindow : Window
    {
        public DrawingCanvasWindow()
        {
            InitializeComponent();

            // 초기 색상 및 두께 설정
            UpdatePenColor(AppState.Instance.PenColor);
            ThicknessSlider.Value = AppState.Instance.PenThickness;

            KeyDown += OnKeyDown;
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string colorName)
            {
                var color = (Color)ColorConverter.ConvertFromString(colorName);
                UpdatePenColor(color);
                AppState.Instance.PenColor = color;
            }
        }

        private void UpdatePenColor(Color color)
        {
            var attributes = new DrawingAttributes
            {
                Color = color,
                Width = ThicknessSlider.Value,
                Height = ThicknessSlider.Value,
                StylusTip = StylusTip.Ellipse
            };
            DrawCanvas.DefaultDrawingAttributes = attributes;
        }

        private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DrawCanvas == null) return;

            var thickness = Math.Round(e.NewValue);
            ThicknessText.Text = thickness.ToString();

            var attributes = DrawCanvas.DefaultDrawingAttributes.Clone();
            attributes.Width = thickness;
            attributes.Height = thickness;
            DrawCanvas.DefaultDrawingAttributes = attributes;

            AppState.Instance.PenThickness = thickness;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearCanvas();
        }

        public void ClearCanvas()
        {
            DrawCanvas.Strokes.Clear();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            AppState.Instance.IsDrawingActive = false;
            AppState.Instance.CurrentMode = AppMode.None;
            base.OnClosed(e);
        }
    }
}
