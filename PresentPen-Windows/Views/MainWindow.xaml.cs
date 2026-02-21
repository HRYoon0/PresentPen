using System.Windows;
using PresentPen.Models;
using PresentPen.Services;

namespace PresentPen.Views
{
    public partial class MainWindow : Window
    {
        private ZoomOverlayWindow? _zoomWindow;
        private DrawingCanvasWindow? _drawingWindow;
        private SpotlightWindow? _spotlightWindow;
        private TimerWindow? _timerWindow;
        private CursorHighlightWindow? _cursorHighlightWindow;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            AppState.Instance.PropertyChanged += OnAppStateChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var hotkeyManager = HotkeyManager.Instance;
            hotkeyManager.Initialize(this);
            hotkeyManager.HotkeyPressed += OnHotkeyPressed;
            hotkeyManager.ClearRequested += OnClearRequested;
            hotkeyManager.EscapePressed += OnEscapePressed;
        }

        private void OnAppStateChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.PropertyName == nameof(AppState.CurrentMode))
                {
                    UpdateStatusText();
                }
                else if (e.PropertyName == nameof(AppState.IsCursorHighlightEnabled))
                {
                    ToggleCursorHighlight();
                }
            });
        }

        private void UpdateStatusText()
        {
            StatusText.Text = AppState.Instance.CurrentMode switch
            {
                AppMode.Zoom => "화면 확대 모드",
                AppMode.Draw => "그리기 모드",
                AppMode.Spotlight => "스포트라이트 모드",
                AppMode.Timer => "타이머 활성화",
                _ => "대기 중"
            };
        }

        private void OnHotkeyPressed(AppMode mode)
        {
            Dispatcher.Invoke(() =>
            {
                switch (mode)
                {
                    case AppMode.Zoom:
                        ToggleZoom();
                        break;
                    case AppMode.Draw:
                        ToggleDrawing();
                        break;
                    case AppMode.Spotlight:
                        ToggleSpotlight();
                        break;
                    case AppMode.Timer:
                        ToggleTimer();
                        break;
                }
            });
        }

        private void OnClearRequested()
        {
            Dispatcher.Invoke(() =>
            {
                _drawingWindow?.ClearCanvas();
            });
        }

        private void OnEscapePressed()
        {
            Dispatcher.Invoke(() =>
            {
                CloseAllOverlays();
            });
        }

        private void ToggleZoom()
        {
            if (_zoomWindow != null)
            {
                _zoomWindow.Close();
                _zoomWindow = null;
            }
            else
            {
                CloseAllOverlays();
                _zoomWindow = new ZoomOverlayWindow();
                _zoomWindow.Closed += (s, e) => _zoomWindow = null;
                _zoomWindow.Show();
                AppState.Instance.CurrentMode = AppMode.Zoom;
            }
        }

        private void ToggleDrawing()
        {
            if (_drawingWindow != null)
            {
                _drawingWindow.Close();
                _drawingWindow = null;
            }
            else
            {
                CloseAllOverlays();
                _drawingWindow = new DrawingCanvasWindow();
                _drawingWindow.Closed += (s, e) => _drawingWindow = null;
                _drawingWindow.Show();
                AppState.Instance.CurrentMode = AppMode.Draw;
            }
        }

        private void ToggleSpotlight()
        {
            if (_spotlightWindow != null)
            {
                _spotlightWindow.Close();
                _spotlightWindow = null;
                // 마우스 속도 복원
                MouseSpeedController.Instance.Stop();
            }
            else
            {
                CloseAllOverlays();
                _spotlightWindow = new SpotlightWindow();
                _spotlightWindow.Closed += (s, e) =>
                {
                    _spotlightWindow = null;
                    MouseSpeedController.Instance.Stop();
                };
                _spotlightWindow.Show();
                AppState.Instance.CurrentMode = AppMode.Spotlight;
                // 마우스 속도 감속 (정밀 조작용)
                MouseSpeedController.Instance.Start(3);
            }
        }

        private void ToggleTimer()
        {
            if (_timerWindow != null)
            {
                _timerWindow.Close();
                _timerWindow = null;
            }
            else
            {
                _timerWindow = new TimerWindow();
                _timerWindow.Closed += (s, e) => _timerWindow = null;
                _timerWindow.Show();
                AppState.Instance.CurrentMode = AppMode.Timer;
            }
        }

        private void ToggleCursorHighlight()
        {
            if (AppState.Instance.IsCursorHighlightEnabled)
            {
                if (_cursorHighlightWindow == null)
                {
                    _cursorHighlightWindow = new CursorHighlightWindow();
                    _cursorHighlightWindow.Closed += (s, e) => _cursorHighlightWindow = null;
                    _cursorHighlightWindow.Show();
                }
            }
            else
            {
                _cursorHighlightWindow?.Close();
                _cursorHighlightWindow = null;
            }
        }

        private void CloseAllOverlays()
        {
            _zoomWindow?.Close();
            _zoomWindow = null;

            _drawingWindow?.Close();
            _drawingWindow = null;

            if (_spotlightWindow != null)
            {
                _spotlightWindow.Close();
                _spotlightWindow = null;
                MouseSpeedController.Instance.Stop();
            }

            // 타이머도 닫기
            _timerWindow?.Close();
            _timerWindow = null;

            AppState.Instance.CurrentMode = AppMode.None;
        }

        // UI 버튼 핸들러
        private void ZoomButton_Click(object sender, RoutedEventArgs e) => ToggleZoom();
        private void DrawButton_Click(object sender, RoutedEventArgs e) => ToggleDrawing();
        private void SpotlightButton_Click(object sender, RoutedEventArgs e) => ToggleSpotlight();
        private void TimerButton_Click(object sender, RoutedEventArgs e) => ToggleTimer();

        private void HighlightButton_Click(object sender, RoutedEventArgs e)
        {
            AppState.Instance.IsCursorHighlightEnabled = !AppState.Instance.IsCursorHighlightEnabled;
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            var helpWindow = new HelpWindow();
            helpWindow.Owner = this;
            helpWindow.ShowDialog();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            MouseSpeedController.Instance.Stop();
            Application.Current.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            CloseAllOverlays();
            _cursorHighlightWindow?.Close();
            MouseSpeedController.Instance.Stop();
            base.OnClosed(e);
        }
    }
}
