using System.Windows;
using System.Windows.Input;
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
                    UpdateStatusText();
                }
            });
        }

        private void UpdateStatusText()
        {
            var mode = AppState.Instance.CurrentMode;
            var highlight = AppState.Instance.IsCursorHighlightEnabled;

            var modeText = mode switch
            {
                AppMode.Zoom => "화면 확대 모드",
                AppMode.Draw => "그리기 모드",
                AppMode.Spotlight => "스포트라이트 모드",
                AppMode.Timer => "타이머 활성화",
                _ => ""
            };

            if (highlight && modeText.Length > 0)
                StatusText.Text = $"{modeText} + 커서 하이라이트";
            else if (highlight)
                StatusText.Text = "커서 하이라이트 활성화";
            else if (modeText.Length > 0)
                StatusText.Text = modeText;
            else
                StatusText.Text = "대기 중";
        }

        private void OnHotkeyPressed(AppMode mode)
        {
            Dispatcher.Invoke(() =>
            {
                // 줌 활성 상태에서 다른 기능 키 → 줌 내부 기능 토글
                if (_zoomWindow != null && mode != AppMode.Zoom)
                {
                    switch (mode)
                    {
                        case AppMode.Draw:
                            _zoomWindow.ToggleDrawingOverlay();
                            return;
                        case AppMode.Spotlight:
                            _zoomWindow.ToggleSpotlightOverlay();
                            return;
                        case AppMode.Timer:
                            ToggleTimer();
                            return;
                    }
                    return;
                }

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

        /// <summary>
        /// 오버레이 윈도우에서 ESC를 눌렀을 때 호출 (모든 기능 해제)
        /// </summary>
        public void CloseAllFromOverlay()
        {
            Dispatcher.Invoke(() => CloseAllOverlays());
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
                _drawingWindow?.Close();
                _drawingWindow = null;
                if (_spotlightWindow != null)
                {
                    _spotlightWindow.Close();
                    _spotlightWindow = null;
                    MouseSpeedController.Instance.Stop();
                }

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
                _zoomWindow?.Close();
                _zoomWindow = null;
                if (_spotlightWindow != null)
                {
                    _spotlightWindow.Close();
                    _spotlightWindow = null;
                    MouseSpeedController.Instance.Stop();
                }

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
                MouseSpeedController.Instance.Stop();
            }
            else
            {
                _zoomWindow?.Close();
                _zoomWindow = null;
                _drawingWindow?.Close();
                _drawingWindow = null;

                _spotlightWindow = new SpotlightWindow();
                _spotlightWindow.Closed += (s, e) =>
                {
                    _spotlightWindow = null;
                    MouseSpeedController.Instance.Stop();
                };
                _spotlightWindow.Show();
                AppState.Instance.CurrentMode = AppMode.Spotlight;
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
                if (AppState.Instance.CurrentMode == AppMode.None)
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

            _timerWindow?.Close();
            _timerWindow = null;

            // 커서 하이라이트도 함께 해제
            if (AppState.Instance.IsCursorHighlightEnabled)
            {
                AppState.Instance.IsCursorHighlightEnabled = false;
            }
            _cursorHighlightWindow?.Close();
            _cursorHighlightWindow = null;

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

        private void ShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            var shortcutWindow = new ShortcutWindow();
            shortcutWindow.Owner = this;
            shortcutWindow.ShowDialog();
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

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
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
