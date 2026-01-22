using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PresentPen.Models;
using PresentPen.Services;

namespace PresentPen.Views
{
    public partial class ZoomOverlayWindow : Window
    {
        private readonly DispatcherTimer _updateTimer;
        private BitmapSource? _baseImage;
        private double _zoomLevel = 2.0;

        public ZoomOverlayWindow()
        {
            InitializeComponent();

            // 초기 화면 캡처
            CaptureBaseImage();

            // 마우스 이동에 따른 업데이트 타이머
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60fps
            };
            _updateTimer.Tick += UpdateZoomPosition;
            _updateTimer.Start();

            MouseWheel += OnMouseWheel;
            KeyDown += OnKeyDown;
        }

        private void CaptureBaseImage()
        {
            // 자신을 숨기고 화면 캡처
            Hide();
            System.Threading.Thread.Sleep(50);

            _baseImage = ScreenCaptureService.CaptureScreen();
            ZoomImage.Source = _baseImage;

            Show();
            Activate();
        }

        private void UpdateZoomPosition(object? sender, EventArgs e)
        {
            var cursorPos = ScreenCaptureService.GetCursorPosition();

            // 확대 중심점을 마우스 위치로 설정
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            // 확대 후 화면 중앙이 마우스 위치가 되도록 오프셋 계산
            double offsetX = -(cursorPos.X * _zoomLevel - screenWidth / 2);
            double offsetY = -(cursorPos.Y * _zoomLevel - screenHeight / 2);

            // 이미지 위치 및 스케일 조정
            ZoomTransform.ScaleX = _zoomLevel;
            ZoomTransform.ScaleY = _zoomLevel;

            // RenderTransformOrigin을 사용하여 변환
            ZoomImage.RenderTransformOrigin = new Point(0, 0);
            ZoomImage.Margin = new Thickness(offsetX, offsetY, 0, 0);
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 휠 업: 확대, 휠 다운: 축소
            if (e.Delta > 0)
            {
                _zoomLevel = Math.Min(_zoomLevel + 0.5, 10.0);
            }
            else
            {
                _zoomLevel = Math.Max(_zoomLevel - 0.5, 1.0);
            }

            ZoomLevelText.Text = $"{_zoomLevel:F1}x";
            AppState.Instance.ZoomLevel = _zoomLevel;
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
            _updateTimer.Stop();
            AppState.Instance.IsZoomActive = false;
            AppState.Instance.CurrentMode = AppMode.None;
            base.OnClosed(e);
        }
    }
}
