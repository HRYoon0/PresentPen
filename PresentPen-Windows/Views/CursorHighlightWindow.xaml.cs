using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using PresentPen.Models;
using PresentPen.Services;

namespace PresentPen.Views
{
    public partial class CursorHighlightWindow : Window
    {
        private readonly DispatcherTimer _updateTimer;

        public CursorHighlightWindow()
        {
            InitializeComponent();

            UpdateAppearance();

            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(8)
            };
            _updateTimer.Tick += UpdatePosition;
            _updateTimer.Start();

            AppState.Instance.PropertyChanged += OnAppStateChanged;
        }

        private void OnAppStateChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppState.HighlightRadius) ||
                e.PropertyName == nameof(AppState.HighlightColor) ||
                e.PropertyName == nameof(AppState.CursorHighlightStyle) ||
                e.PropertyName == nameof(AppState.HighlightOpacity))
            {
                Dispatcher.Invoke(UpdateAppearance);
            }
        }

        private void UpdateAppearance()
        {
            var radius = AppState.Instance.HighlightRadius;
            var color = AppState.Instance.HighlightColor;
            var style = AppState.Instance.CursorHighlightStyle;
            var opacity = AppState.Instance.HighlightOpacity;

            // 윈도우 크기 설정
            Width = radius * 2 + 60;
            Height = radius * 2 + 60;

            // 모든 스타일 숨기기
            RingHighlight.Visibility = Visibility.Collapsed;
            HaloHighlight.Visibility = Visibility.Collapsed;
            FilledHighlight.Visibility = Visibility.Collapsed;
            SquircleHighlight.Visibility = Visibility.Collapsed;

            // 현재 스타일에 따라 표시
            switch (style)
            {
                case CursorHighlightStyle.Ring:
                    RingHighlight.Width = radius * 2;
                    RingHighlight.Height = radius * 2;
                    RingHighlight.Stroke = new SolidColorBrush(Color.FromArgb(
                        (byte)(255 * opacity), color.R, color.G, color.B));
                    RingHighlight.Visibility = Visibility.Visible;
                    break;

                case CursorHighlightStyle.Halo:
                    HaloHighlight.Width = radius * 2;
                    HaloHighlight.Height = radius * 2;
                    HaloHighlight.Fill = CreateHaloBrush(color, opacity);
                    HaloHighlight.Visibility = Visibility.Visible;
                    break;

                case CursorHighlightStyle.Filled:
                    FilledHighlight.Width = radius * 2;
                    FilledHighlight.Height = radius * 2;
                    FilledHighlight.Fill = new SolidColorBrush(Color.FromArgb(
                        (byte)(255 * opacity), color.R, color.G, color.B));
                    FilledHighlight.Visibility = Visibility.Visible;
                    break;

                case CursorHighlightStyle.Squircle:
                    SquircleHighlight.Width = radius * 2;
                    SquircleHighlight.Height = radius * 2;
                    SquircleHighlight.CornerRadius = new CornerRadius(radius * 0.3);
                    SquircleHighlight.Background = CreateHaloBrush(color, opacity);
                    SquircleHighlight.Visibility = Visibility.Visible;
                    break;
            }
        }

        private Brush CreateHaloBrush(Color color, double opacity)
        {
            var brush = new RadialGradientBrush();
            brush.GradientStops.Add(new GradientStop(
                Color.FromArgb((byte)(200 * opacity), color.R, color.G, color.B), 0));
            brush.GradientStops.Add(new GradientStop(
                Color.FromArgb((byte)(100 * opacity), color.R, color.G, color.B), 0.6));
            brush.GradientStops.Add(new GradientStop(
                Color.FromArgb(0, color.R, color.G, color.B), 1));
            return brush;
        }

        private void UpdatePosition(object? sender, EventArgs e)
        {
            var cursorPos = ScreenCaptureService.GetCursorPosition();
            Left = cursorPos.X - Width / 2;
            Top = cursorPos.Y - Height / 2;
        }

        protected override void OnClosed(EventArgs e)
        {
            _updateTimer.Stop();
            AppState.Instance.PropertyChanged -= OnAppStateChanged;
            base.OnClosed(e);
        }
    }
}
