using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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

        // 줌 내 기능 토글
        private bool _isDrawingEnabled;
        private bool _isCursorHighlightEnabled;
        private bool _isSpotlightEnabled;

        // 줌 내 그리기
        private bool _isDrawing;
        private Point _drawStartPoint;
        private Polyline? _currentPolyline;
        private List<UIElement> _drawUndoStack = new();

        public ZoomOverlayWindow()
        {
            InitializeComponent();

            CaptureBaseImage();

            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _updateTimer.Tick += UpdateZoomPosition;
            _updateTimer.Start();

            MouseWheel += OnMouseWheel;
            KeyDown += OnKeyDown;

            // 5초 후 도움말 숨기기
            var hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            hideTimer.Tick += (s, e) => { HelpBorder.Visibility = Visibility.Collapsed; hideTimer.Stop(); };
            hideTimer.Start();
        }

        // === 외부에서 호출 가능한 기능 토글 ===
        public void ToggleDrawingOverlay()
        {
            _isDrawingEnabled = !_isDrawingEnabled;
            DrawOverlay.Visibility = _isDrawingEnabled ? Visibility.Visible : Visibility.Collapsed;
            Cursor = _isDrawingEnabled ? Cursors.Pen : Cursors.Cross;
            UpdateStatusText();
        }

        public void ToggleSpotlightOverlay()
        {
            _isSpotlightEnabled = !_isSpotlightEnabled;
            SpotlightOverlay.Visibility = _isSpotlightEnabled ? Visibility.Visible : Visibility.Collapsed;
            UpdateStatusText();
        }

        public void ToggleCursorOverlay()
        {
            _isCursorHighlightEnabled = !_isCursorHighlightEnabled;
            CursorOverlay.Visibility = _isCursorHighlightEnabled ? Visibility.Visible : Visibility.Collapsed;
            UpdateStatusText();
        }

        private void CaptureBaseImage()
        {
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
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            double offsetX = -(cursorPos.X * _zoomLevel - screenWidth / 2);
            double offsetY = -(cursorPos.Y * _zoomLevel - screenHeight / 2);

            ZoomTransform.ScaleX = _zoomLevel;
            ZoomTransform.ScaleY = _zoomLevel;
            ZoomImage.RenderTransformOrigin = new Point(0, 0);
            ZoomImage.Margin = new Thickness(offsetX, offsetY, 0, 0);

            if (_isCursorHighlightEnabled)
            {
                UpdateZoomCursorHighlight(cursorPos);
            }

            if (_isSpotlightEnabled)
            {
                UpdateZoomSpotlight(cursorPos);
            }
        }

        private void UpdateZoomCursorHighlight(System.Drawing.Point cursorPos)
        {
            var state = AppState.Instance;
            double size = state.HighlightRadius * 2;
            ZoomCursorHighlight.Width = size;
            ZoomCursorHighlight.Height = size;

            var color = state.HighlightColor;
            var opacity = state.HighlightOpacity;

            switch (state.CursorHighlightStyle)
            {
                case CursorHighlightStyle.Ring:
                    ZoomCursorHighlight.Fill = Brushes.Transparent;
                    ZoomCursorHighlight.Stroke = new SolidColorBrush(Color.FromArgb((byte)(opacity * 255), color.R, color.G, color.B));
                    ZoomCursorHighlight.StrokeThickness = 3;
                    break;
                case CursorHighlightStyle.Halo:
                    var brush = new RadialGradientBrush();
                    brush.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(opacity * 180), color.R, color.G, color.B), 0));
                    brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, color.R, color.G, color.B), 1));
                    ZoomCursorHighlight.Fill = brush;
                    ZoomCursorHighlight.Stroke = null;
                    break;
                case CursorHighlightStyle.Filled:
                    ZoomCursorHighlight.Fill = new SolidColorBrush(Color.FromArgb((byte)(opacity * 150), color.R, color.G, color.B));
                    ZoomCursorHighlight.Stroke = null;
                    break;
                case CursorHighlightStyle.Squircle:
                    ZoomCursorHighlight.Fill = new SolidColorBrush(Color.FromArgb((byte)(opacity * 150), color.R, color.G, color.B));
                    ZoomCursorHighlight.Stroke = null;
                    break;
            }

            Canvas.SetLeft(ZoomCursorHighlight, cursorPos.X - size / 2);
            Canvas.SetTop(ZoomCursorHighlight, cursorPos.Y - size / 2);
        }

        private void UpdateZoomSpotlight(System.Drawing.Point cursorPos)
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var radius = AppState.Instance.SpotlightRadius;

            var outerRect = new RectangleGeometry(new Rect(0, 0, screenWidth, screenHeight));
            var innerCircle = new EllipseGeometry(new Point(cursorPos.X, cursorPos.Y), radius, radius);

            SpotlightPath.Data = new CombinedGeometry(GeometryCombineMode.Exclude, outerRect, innerCircle);
        }

        // === 줌 내 그리기 ===
        private void DrawOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDrawing = true;
            _drawStartPoint = e.GetPosition(DrawOverlay);

            _currentPolyline = new Polyline
            {
                Stroke = new SolidColorBrush(AppState.Instance.PenColor),
                StrokeThickness = AppState.Instance.PenThickness,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            _currentPolyline.Points.Add(_drawStartPoint);
            DrawOverlay.Children.Add(_currentPolyline);
            DrawOverlay.CaptureMouse();
        }

        private void DrawOverlay_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDrawing || _currentPolyline == null) return;
            _currentPolyline.Points.Add(e.GetPosition(DrawOverlay));
        }

        private void DrawOverlay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDrawing) return;
            _isDrawing = false;
            DrawOverlay.ReleaseMouseCapture();
            if (_currentPolyline != null)
            {
                _drawUndoStack.Add(_currentPolyline);
                _currentPolyline = null;
            }
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                _zoomLevel = Math.Min(_zoomLevel + 0.5, 10.0);
            else
                _zoomLevel = Math.Max(_zoomLevel - 0.5, 1.0);

            UpdateStatusText();
            AppState.Instance.ZoomLevel = _zoomLevel;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    break;

                // Ctrl+Z: 줌 내 그리기 실행 취소
                case Key.Z:
                    if (Keyboard.Modifiers == ModifierKeys.Control && _drawUndoStack.Count > 0)
                    {
                        var last = _drawUndoStack[^1];
                        _drawUndoStack.RemoveAt(_drawUndoStack.Count - 1);
                        DrawOverlay.Children.Remove(last);
                    }
                    break;

                // 색상 단축키 (그리기 모드일 때)
                case Key.R:
                    if (_isDrawingEnabled) AppState.Instance.PenColor = Colors.Red;
                    break;
                case Key.G:
                    if (_isDrawingEnabled) AppState.Instance.PenColor = Colors.Green;
                    break;
                case Key.B:
                    if (_isDrawingEnabled) AppState.Instance.PenColor = Colors.Blue;
                    break;
                case Key.Y:
                    if (_isDrawingEnabled) AppState.Instance.PenColor = Colors.Yellow;
                    break;

                // E: 그리기 전체 지우기
                case Key.E:
                    if (_isDrawingEnabled)
                    {
                        DrawOverlay.Children.Clear();
                        _drawUndoStack.Clear();
                    }
                    break;
            }
        }

        private void UpdateStatusText()
        {
            var features = new List<string> { $"{_zoomLevel:F1}x" };
            if (_isDrawingEnabled) features.Add("그리기");
            if (_isCursorHighlightEnabled) features.Add("커서");
            if (_isSpotlightEnabled) features.Add("스포트라이트");
            ZoomLevelText.Text = string.Join(" | ", features);
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
