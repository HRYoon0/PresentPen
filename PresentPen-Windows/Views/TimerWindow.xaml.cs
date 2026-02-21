using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PresentPen.Models;

namespace PresentPen.Views
{
    public partial class TimerWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private int _timeRemaining = 5 * 60; // 기본값: 5분 (초 단위)
        private bool _isStarted;
        private bool _isPaused;

        public TimerWindow()
        {
            InitializeComponent();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;

            KeyDown += OnKeyDown;
            MouseWheel += OnMouseWheel;

            // 포커스 받기
            Focusable = true;
            Loaded += (s, e) => Focus();

            UpdateDisplay();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_isPaused) return;

            _timeRemaining--;

            if (_timeRemaining <= 0)
            {
                _timeRemaining = 0;
                _timer.Stop();
                FlashScreen();
            }

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            int mins = _timeRemaining / 60;
            int secs = _timeRemaining % 60;
            TimerDisplay.Text = $"{mins:D2}:{secs:D2}";

            // 1분 미만 경고: 빨간색
            TimerDisplay.Foreground = _timeRemaining < 60 && _isStarted
                ? new SolidColorBrush(Colors.Red)
                : new SolidColorBrush(Colors.White);

            // 상태 안내 문구
            if (!_isStarted)
            {
                InfoLabel.Text = "스크롤/↑↓: 시간 조절 | Space/Enter: 시작 | ESC: 종료";
                PresetLabel.Visibility = Visibility.Visible;
            }
            else if (_isPaused)
            {
                InfoLabel.Text = "⏸ 일시정지 | Space: 재개 | ESC: 종료";
                PresetLabel.Visibility = Visibility.Collapsed;
            }
            else
            {
                InfoLabel.Text = "⏵ 진행 중 | Space: 일시정지 | ESC: 종료";
                PresetLabel.Visibility = Visibility.Collapsed;
            }
        }

        private void FlashScreen()
        {
            // 비프음
            SystemSounds.Beep.Play();

            // 3번 빨간색 깜빡임
            var flashTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            int flashCount = 0;
            flashTimer.Tick += (s, e) =>
            {
                flashCount++;
                if (flashCount > 6)
                {
                    flashTimer.Stop();
                    BackgroundRect.Fill = new SolidColorBrush(Color.FromArgb(0xF0, 0, 0, 0));
                    return;
                }
                BackgroundRect.Fill = flashCount % 2 == 1
                    ? new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0, 0))
                    : new SolidColorBrush(Color.FromArgb(0xF0, 0, 0, 0));
            };
            flashTimer.Start();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    e.Handled = true;
                    CloseAfterKeyRelease();
                    break;

                case Key.Space:
                case Key.Enter:
                    if (!_isStarted)
                    {
                        // 타이머 시작
                        _isStarted = true;
                        _timer.Start();
                    }
                    else
                    {
                        // 일시정지/재개
                        _isPaused = !_isPaused;
                    }
                    UpdateDisplay();
                    break;

                // 프리셋 (시작 전에만)
                case Key.D1:
                case Key.NumPad1:
                    if (!_isStarted) { _timeRemaining = 1 * 60; UpdateDisplay(); }
                    break;
                case Key.D3:
                case Key.NumPad3:
                    if (!_isStarted) { _timeRemaining = 3 * 60; UpdateDisplay(); }
                    break;
                case Key.D5:
                case Key.NumPad5:
                    if (!_isStarted) { _timeRemaining = 5 * 60; UpdateDisplay(); }
                    break;
                case Key.D0:
                case Key.NumPad0:
                    if (!_isStarted) { _timeRemaining = 10 * 60; UpdateDisplay(); }
                    break;

                // 화살표로 1분 단위 조절 (시작 전에만)
                case Key.Up:
                    if (!_isStarted) { _timeRemaining += 60; UpdateDisplay(); }
                    break;
                case Key.Down:
                    if (!_isStarted) { _timeRemaining = Math.Max(0, _timeRemaining - 60); UpdateDisplay(); }
                    break;
            }

            e.Handled = true;
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 시작 전에만 스크롤로 시간 조절
            if (!_isStarted)
            {
                _timeRemaining += e.Delta > 0 ? 10 : -10;
                _timeRemaining = Math.Max(0, _timeRemaining);
                UpdateDisplay();
            }
        }

        private void CloseAfterKeyRelease()
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            timer.Tick += (s, args) =>
            {
                if (!Keyboard.IsKeyDown(Key.Escape))
                {
                    timer.Stop();
                    (Application.Current.MainWindow as MainWindow)?.CloseAllFromOverlay();
                }
            };
            timer.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            AppState.Instance.IsTimerActive = false;
            AppState.Instance.CurrentMode = AppMode.None;
            base.OnClosed(e);
        }
    }
}
