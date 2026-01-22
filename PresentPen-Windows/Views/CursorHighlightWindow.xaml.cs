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

            // 초기 크기 및 색상 설정
            UpdateAppearance();

            // 마우스 위치 추적 타이머
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(8) // ~120fps
            };
            _updateTimer.Tick += UpdatePosition;
            _updateTimer.Start();

            // 상태 변경 감지
            AppState.Instance.PropertyChanged += OnAppStateChanged;
        }

        private void OnAppStateChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppState.HighlightRadius) ||
                e.PropertyName == nameof(AppState.HighlightColor))
            {
                Dispatcher.Invoke(UpdateAppearance);
            }
        }

        private void UpdateAppearance()
        {
            var radius = AppState.Instance.HighlightRadius;
            var color = AppState.Instance.HighlightColor;

            Width = radius * 2 + 40;
            Height = radius * 2 + 40;
            HighlightCircle.Width = radius * 2;
            HighlightCircle.Height = radius * 2;

            // 색상 업데이트
            var brush = new RadialGradientBrush();
            brush.GradientStops.Add(new GradientStop(
                Color.FromArgb(128, color.R, color.G, color.B), 0));
            brush.GradientStops.Add(new GradientStop(
                Color.FromArgb(64, color.R, color.G, color.B), 0.7));
            brush.GradientStops.Add(new GradientStop(
                Color.FromArgb(0, color.R, color.G, color.B), 1));
            HighlightCircle.Fill = brush;
        }

        private void UpdatePosition(object? sender, EventArgs e)
        {
            var cursorPos = ScreenCaptureService.GetCursorPosition();

            // 윈도우 중앙이 커서 위치에 오도록
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
