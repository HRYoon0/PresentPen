using System.Windows;
using PresentPen.Models;

namespace PresentPen.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            // 현재 설정값 로드
            LoadSettings();
        }

        private void LoadSettings()
        {
            var state = AppState.Instance;

            PenThicknessSlider.Value = state.PenThickness;
            HighlightEnabledCheck.IsChecked = state.IsCursorHighlightEnabled;
            HighlightRadiusSlider.Value = state.HighlightRadius;
            SpotlightRadiusSlider.Value = state.SpotlightRadius;
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

        private void HighlightRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (HighlightRadiusText == null) return;

            var value = (int)Math.Round(e.NewValue);
            HighlightRadiusText.Text = value.ToString();
            AppState.Instance.HighlightRadius = value;
        }

        private void SpotlightRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SpotlightRadiusText == null) return;

            var value = (int)Math.Round(e.NewValue);
            SpotlightRadiusText.Text = value.ToString();
            AppState.Instance.SpotlightRadius = value;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
