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
        private int _elapsedSeconds;
        private bool _isRunning;

        public TimerWindow()
        {
            InitializeComponent();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;

            // 화면 우측 하단에 배치
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            Left = screenWidth - Width - 20;
            Top = screenHeight - Height - 60;

            UpdateDisplay();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _elapsedSeconds++;
            AppState.Instance.TimerSeconds = _elapsedSeconds;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            int minutes = _elapsedSeconds / 60;
            int seconds = _elapsedSeconds % 60;
            TimerDisplay.Text = $"{minutes:D2}:{seconds:D2}";

            // 5분 이상이면 경고색
            if (_elapsedSeconds >= 300)
            {
                TimerDisplay.Foreground = new SolidColorBrush(Colors.OrangeRed);
            }
            else
            {
                TimerDisplay.Foreground = new SolidColorBrush(Colors.White);
            }
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                // 정지
                _timer.Stop();
                _isRunning = false;
                StartStopBtn.Content = "▶";
                StartStopBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            }
            else
            {
                // 시작
                _timer.Start();
                _isRunning = true;
                StartStopBtn.Content = "⏸";
                StartStopBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _isRunning = false;
            _elapsedSeconds = 0;
            AppState.Instance.TimerSeconds = 0;
            StartStopBtn.Content = "▶";
            StartStopBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            UpdateDisplay();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 드래그로 이동 가능
            DragMove();
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
