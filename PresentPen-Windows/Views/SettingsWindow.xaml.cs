using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PresentPen.Models;

namespace PresentPen.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            var state = AppState.Instance;

            PenThicknessSlider.Value = state.PenThickness;
            HighlightEnabledCheck.IsChecked = state.IsCursorHighlightEnabled;
            HighlightRadiusSlider.Value = state.HighlightRadius;
            HighlightOpacitySlider.Value = state.HighlightOpacity;
            SpotlightRadiusSlider.Value = state.SpotlightRadius;
            SpotlightZoomCheck.IsChecked = state.SpotlightZoomEnabled;
            SpotlightZoomSlider.Value = state.SpotlightZoomLevel;

            // 하이라이트 스타일 콤보박스 선택
            var styleIndex = (int)state.CursorHighlightStyle;
            if (styleIndex < HighlightStyleCombo.Items.Count)
            {
                HighlightStyleCombo.SelectedIndex = styleIndex;
            }
        }

        private void PenThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PenThicknessText == null) return;
            var value = Math.Round(e.NewValue);
            PenThicknessText.Text = value.ToString();
            AppState.Instance.PenThickness = value;
        }

        private void HighlightEnabledCheck_Changed(object sender, RoutedEventArgs e)
        {
            AppState.Instance.IsCursorHighlightEnabled = HighlightEnabledCheck.IsChecked ?? false;
        }

        private void HighlightStyleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HighlightStyleCombo.SelectedItem is ComboBoxItem item && item.Tag is string styleTag)
            {
                if (Enum.TryParse<CursorHighlightStyle>(styleTag, out var style))
                {
                    AppState.Instance.CursorHighlightStyle = style;
                }
            }
        }

        private void HighlightColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string colorName)
            {
                var color = (Color)ColorConverter.ConvertFromString(colorName);
                AppState.Instance.HighlightColor = color;
            }
        }

        private void HighlightRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (HighlightRadiusText == null) return;
            var value = (int)Math.Round(e.NewValue);
            HighlightRadiusText.Text = value.ToString();
            AppState.Instance.HighlightRadius = value;
        }

        private void HighlightOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (HighlightOpacityText == null) return;
            var value = e.NewValue;
            HighlightOpacityText.Text = $"{(int)(value * 100)}%";
            AppState.Instance.HighlightOpacity = value;
        }

        private void SpotlightZoomCheck_Changed(object sender, RoutedEventArgs e)
        {
            AppState.Instance.SpotlightZoomEnabled = SpotlightZoomCheck.IsChecked ?? true;
        }

        private void SpotlightRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SpotlightRadiusText == null) return;
            var value = (int)Math.Round(e.NewValue);
            SpotlightRadiusText.Text = value.ToString();
            AppState.Instance.SpotlightRadius = value;
        }

        private void SpotlightZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SpotlightZoomText == null) return;
            var value = Math.Round(e.NewValue, 1);
            SpotlightZoomText.Text = $"{value}x";
            AppState.Instance.SpotlightZoomLevel = value;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
