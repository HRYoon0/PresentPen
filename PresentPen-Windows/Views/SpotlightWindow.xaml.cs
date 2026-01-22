using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PresentPen.Models;
using PresentPen.Services;

namespace PresentPen.Views
{
    public partial class SpotlightWindow : Window
    {
        private readonly DispatcherTimer _updateTimer;
        private int _spotlightRadius;

        public SpotlightWindow()
        {
            InitializeComponent();

            _spotlightRadius = AppState.Instance.SpotlightRadius;

            Loaded += OnLoaded;
            MouseWheel += OnMouseWheel;
            KeyDown += OnKeyDown;

            // 마우스 위치 추적 타이머
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _updateTimer.Tick += UpdateSpotlightPosition;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 화면 크기에 맞게 지오메트리 업데이트
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            FullScreenRect.Rect = new Rect(0, 0, screenWidth, screenHeight);
            UpdateSpotlightGeometry();

            _updateTimer.Start();
        }

        private void UpdateSpotlightPosition(object? sender, EventArgs e)
        {
            var cursorPos = ScreenCaptureService.GetCursorPosition();
            SpotlightEllipse.Center = new Point(cursorPos.X, cursorPos.Y);
        }

        private void UpdateSpotlightGeometry()
        {
            SpotlightEllipse.RadiusX = _spotlightRadius;
            SpotlightEllipse.RadiusY = _spotlightRadius;
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 휠 업: 스포트라이트 커짐, 휠 다운: 작아짐
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
            AppState.Instance.IsSpotlightActive = false;
            AppState.Instance.CurrentMode = AppMode.None;
            base.OnClosed(e);
        }
    }
}
