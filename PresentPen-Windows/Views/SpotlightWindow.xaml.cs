using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PresentPen.Models;
using PresentPen.Services;

namespace PresentPen.Views
{
    public partial class SpotlightWindow : Window
    {
        private readonly DispatcherTimer _updateTimer;
        private int _spotlightRadius;
        private bool _zoomEnabled;
        private double _zoomLevel;
        private BitmapSource? _baseImage;

        public SpotlightWindow()
        {
            InitializeComponent();

            _spotlightRadius = AppState.Instance.SpotlightRadius;
            _zoomEnabled = AppState.Instance.SpotlightZoomEnabled;
            _zoomLevel = AppState.Instance.SpotlightZoomLevel;

            Loaded += OnLoaded;
            MouseWheel += OnMouseWheel;
            KeyDown += OnKeyDown;

            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _updateTimer.Tick += UpdateSpotlightPosition;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            FullScreenRect.Rect = new Rect(0, 0, screenWidth, screenHeight);

            // 줌 활성화 시 화면 캡처
            if (_zoomEnabled)
            {
                CaptureBaseImage();
            }

            UpdateSpotlightGeometry();
            UpdateZoomStatus();

            _updateTimer.Start();
        }

        private void CaptureBaseImage()
        {
            Hide();
            System.Threading.Thread.Sleep(50);
            _baseImage = ScreenCaptureService.CaptureScreen();
            Show();

            if (_zoomEnabled && _baseImage != null)
            {
                ZoomedImage.Source = _baseImage;
                ZoomedImage.Visibility = Visibility.Visible;
            }
        }

        private void UpdateSpotlightPosition(object? sender, EventArgs e)
        {
            var cursorPos = ScreenCaptureService.GetCursorPosition();
            var center = new Point(cursorPos.X, cursorPos.Y);

            // 스포트라이트 마스크 위치 업데이트
            SpotlightEllipse.Center = center;

            // 줌 클립 영역 업데이트
            ZoomClip.Center = center;
            ZoomClip.RadiusX = _spotlightRadius;
            ZoomClip.RadiusY = _spotlightRadius;

            // 줌 이미지 위치 업데이트
            if (_zoomEnabled && _baseImage != null)
            {
                UpdateZoomedImagePosition(cursorPos.X, cursorPos.Y);
            }
        }

        private void UpdateZoomedImagePosition(int cursorX, int cursorY)
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            // 확대 중심점 계산
            double offsetX = cursorX - (cursorX * _zoomLevel);
            double offsetY = cursorY - (cursorY * _zoomLevel);

            var transform = new TransformGroup();
            transform.Children.Add(new ScaleTransform(_zoomLevel, _zoomLevel));
            transform.Children.Add(new TranslateTransform(offsetX, offsetY));

            ZoomedImage.RenderTransform = transform;
        }

        private void UpdateSpotlightGeometry()
        {
            SpotlightEllipse.RadiusX = _spotlightRadius;
            SpotlightEllipse.RadiusY = _spotlightRadius;
            ZoomClip.RadiusX = _spotlightRadius;
            ZoomClip.RadiusY = _spotlightRadius;
        }

        private void UpdateZoomStatus()
        {
            ZoomStatusText.Text = $"줌: {_zoomLevel:F1}x";
            ZoomEnabledText.Text = _zoomEnabled ? "줌 활성화" : "줌 비활성화";
            ZoomEnabledText.Foreground = _zoomEnabled
                ? new SolidColorBrush(Colors.LimeGreen)
                : new SolidColorBrush(Colors.Gray);

            ZoomedImage.Visibility = _zoomEnabled ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                // Shift+휠: 줌 배율 조절
                if (e.Delta > 0)
                {
                    _zoomLevel = Math.Min(_zoomLevel + 0.25, 5.0);
                }
                else
                {
                    _zoomLevel = Math.Max(_zoomLevel - 0.25, 1.0);
                }
                AppState.Instance.SpotlightZoomLevel = _zoomLevel;
                UpdateZoomStatus();
            }
            else
            {
                // 일반 휠: 스포트라이트 크기 조절
                if (e.Delta > 0)
                {
                    _spotlightRadius = Math.Min(_spotlightRadius + 20, 500);
                }
                else
                {
                    _spotlightRadius = Math.Max(_spotlightRadius - 20, 50);
                }
                UpdateSpotlightGeometry();
                AppState.Instance.SpotlightRadius = _spotlightRadius;
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            else if (e.Key == Key.Space)
            {
                // 스페이스바: 줌 토글
                _zoomEnabled = !_zoomEnabled;
                AppState.Instance.SpotlightZoomEnabled = _zoomEnabled;

                if (_zoomEnabled && _baseImage == null)
                {
                    CaptureBaseImage();
                }

                UpdateZoomStatus();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _updateTimer.Stop();
            AppState.Instance.IsSpotlightActive = false;
            AppState.Instance.CurrentMode = AppMode.None;
            base.OnClosed(e);
        }
    }
}
