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

        // 줌 내 그리기 (전체 도구 지원)
        private bool _isDrawing;
        private Point _drawStartPoint;
        private Point _drawLastPoint;
        private Polyline? _currentPolyline;
        private Shape? _previewShape;
        private DrawingTool? _originalTool;
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
            KeyUp += OnKeyUp;

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

            // 그리기 활성화 시 줌 위치 고정 (화면 멈춤)
            if (_isDrawingEnabled)
                _updateTimer.Stop();
            else
                _updateTimer.Start();

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
                UpdateZoomCursorHighlight(cursorPos);

            if (_isSpotlightEnabled)
                UpdateZoomSpotlight(cursorPos);
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

        // === 줌 내 그리기: 마우스 다운 ===
        private void DrawOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDrawing = true;
            _drawStartPoint = e.GetPosition(DrawOverlay);
            _drawLastPoint = _drawStartPoint;

            // 수정자 키에 따른 임시 도구 변경
            UpdateToolForModifiers();

            var tool = AppState.Instance.CurrentTool;

            if (tool == DrawingTool.Pen || tool == DrawingTool.Highlighter)
            {
                _currentPolyline = new Polyline
                {
                    Stroke = new SolidColorBrush(AppState.Instance.PenColor),
                    StrokeThickness = AppState.Instance.PenThickness,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };

                if (tool == DrawingTool.Highlighter || AppState.Instance.IsHighlighterMode)
                {
                    _currentPolyline.Opacity = 0.4;
                    _currentPolyline.StrokeThickness = AppState.Instance.PenThickness * 3;
                }

                _currentPolyline.Points.Add(_drawStartPoint);
                DrawOverlay.Children.Add(_currentPolyline);
            }

            DrawOverlay.CaptureMouse();
        }

        private void UpdateToolForModifiers()
        {
            var ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            var shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            if (ctrl && shift)
            {
                _originalTool = AppState.Instance.CurrentTool;
                AppState.Instance.CurrentTool = DrawingTool.Arrow;
            }
            else if (ctrl)
            {
                _originalTool = AppState.Instance.CurrentTool;
                AppState.Instance.CurrentTool = DrawingTool.Rectangle;
            }
            else if (shift)
            {
                _originalTool = AppState.Instance.CurrentTool;
                AppState.Instance.CurrentTool = DrawingTool.Line;
            }
        }

        // === 줌 내 그리기: 마우스 이동 ===
        private void DrawOverlay_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDrawing) return;

            var currentPoint = e.GetPosition(DrawOverlay);
            var tool = AppState.Instance.CurrentTool;

            if (tool == DrawingTool.Pen || tool == DrawingTool.Highlighter)
            {
                _currentPolyline?.Points.Add(currentPoint);
            }
            else
            {
                // 도형 미리보기
                if (_previewShape != null)
                    DrawOverlay.Children.Remove(_previewShape);

                _previewShape = CreateShape(_drawStartPoint, currentPoint);
                if (_previewShape != null)
                {
                    _previewShape.Opacity = 0.5;
                    DrawOverlay.Children.Add(_previewShape);
                }
            }

            _drawLastPoint = currentPoint;
        }

        // === 줌 내 그리기: 마우스 업 ===
        private void DrawOverlay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDrawing) return;

            _isDrawing = false;
            DrawOverlay.ReleaseMouseCapture();

            var endPoint = e.GetPosition(DrawOverlay);
            var tool = AppState.Instance.CurrentTool;

            // 미리보기 제거
            if (_previewShape != null)
            {
                DrawOverlay.Children.Remove(_previewShape);
                _previewShape = null;
            }

            if (tool != DrawingTool.Pen && tool != DrawingTool.Highlighter)
            {
                // 도형 확정
                var shape = CreateShape(_drawStartPoint, endPoint);
                if (shape != null)
                {
                    DrawOverlay.Children.Add(shape);
                    _drawUndoStack.Add(shape);
                }
            }
            else if (_currentPolyline != null)
            {
                _drawUndoStack.Add(_currentPolyline);
                _currentPolyline = null;
            }

            // 임시 도구 복원
            if (_originalTool.HasValue)
            {
                AppState.Instance.CurrentTool = _originalTool.Value;
                _originalTool = null;
            }
        }

        // === 도형 생성 ===
        private Shape? CreateShape(Point start, Point end)
        {
            var tool = AppState.Instance.CurrentTool;
            var brush = new SolidColorBrush(AppState.Instance.PenColor);
            var thickness = AppState.Instance.PenThickness;

            switch (tool)
            {
                case DrawingTool.Line:
                    return new Line
                    {
                        X1 = start.X, Y1 = start.Y,
                        X2 = end.X, Y2 = end.Y,
                        Stroke = brush,
                        StrokeThickness = thickness,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round
                    };

                case DrawingTool.Arrow:
                    return CreateArrow(start, end, brush, thickness);

                case DrawingTool.Rectangle:
                    var rect = new Rectangle
                    {
                        Stroke = brush,
                        StrokeThickness = thickness,
                        Width = Math.Abs(end.X - start.X),
                        Height = Math.Abs(end.Y - start.Y)
                    };
                    Canvas.SetLeft(rect, Math.Min(start.X, end.X));
                    Canvas.SetTop(rect, Math.Min(start.Y, end.Y));
                    return rect;

                case DrawingTool.Circle:
                    var ellipse = new Ellipse
                    {
                        Stroke = brush,
                        StrokeThickness = thickness,
                        Width = Math.Abs(end.X - start.X),
                        Height = Math.Abs(end.Y - start.Y)
                    };
                    Canvas.SetLeft(ellipse, Math.Min(start.X, end.X));
                    Canvas.SetTop(ellipse, Math.Min(start.Y, end.Y));
                    return ellipse;

                default:
                    return null;
            }
        }

        private Path CreateArrow(Point start, Point end, Brush brush, double thickness)
        {
            var path = new Path
            {
                Stroke = brush,
                StrokeThickness = thickness,
                Fill = brush,
                StrokeLineJoin = PenLineJoin.Round
            };

            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = start };
            figure.Segments.Add(new LineSegment(end, true));
            geometry.Figures.Add(figure);

            double angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
            double arrowSize = thickness * 4;

            var arrowHead = new PathFigure { StartPoint = end };
            arrowHead.Segments.Add(new LineSegment(
                new Point(
                    end.X - arrowSize * Math.Cos(angle - Math.PI / 6),
                    end.Y - arrowSize * Math.Sin(angle - Math.PI / 6)), true));
            arrowHead.Segments.Add(new LineSegment(
                new Point(
                    end.X - arrowSize * Math.Cos(angle + Math.PI / 6),
                    end.Y - arrowSize * Math.Sin(angle + Math.PI / 6)), true));
            arrowHead.Segments.Add(new LineSegment(end, true));
            arrowHead.IsClosed = true;
            geometry.Figures.Add(arrowHead);

            path.Data = geometry;
            return path;
        }

        // === 마우스 휠 ===
        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_isDrawingEnabled)
            {
                // 그리기 모드: 선 굵기 조절
                var delta = e.Delta > 0 ? 1 : -1;
                AppState.Instance.PenThickness += delta;
            }
            else
            {
                // 줌 모드: 확대/축소
                if (e.Delta > 0)
                    _zoomLevel = Math.Min(_zoomLevel + 0.5, 10.0);
                else
                    _zoomLevel = Math.Max(_zoomLevel - 0.5, 1.0);

                AppState.Instance.ZoomLevel = _zoomLevel;
            }

            UpdateStatusText();
        }

        // === 키보드 입력 ===
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            var shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            switch (e.Key)
            {
                case Key.Escape:
                    e.Handled = true;
                    CloseAfterKeyRelease();
                    break;

                // Ctrl+Z: 실행 취소
                case Key.Z:
                    if (Keyboard.Modifiers == ModifierKeys.Control && _drawUndoStack.Count > 0)
                    {
                        var last = _drawUndoStack[^1];
                        _drawUndoStack.RemoveAt(_drawUndoStack.Count - 1);
                        DrawOverlay.Children.Remove(last);
                    }
                    break;

                // 색상 단축키 (그리기 활성 시)
                case Key.R:
                    if (_isDrawingEnabled)
                    {
                        AppState.Instance.PenColor = Colors.Red;
                        AppState.Instance.IsHighlighterMode = shift;
                        if (shift) AppState.Instance.CurrentTool = DrawingTool.Highlighter;
                        else if (AppState.Instance.CurrentTool == DrawingTool.Highlighter) AppState.Instance.CurrentTool = DrawingTool.Pen;
                    }
                    break;
                case Key.G:
                    if (_isDrawingEnabled)
                    {
                        AppState.Instance.PenColor = Colors.Green;
                        AppState.Instance.IsHighlighterMode = shift;
                        if (shift) AppState.Instance.CurrentTool = DrawingTool.Highlighter;
                        else if (AppState.Instance.CurrentTool == DrawingTool.Highlighter) AppState.Instance.CurrentTool = DrawingTool.Pen;
                    }
                    break;
                case Key.B:
                    if (_isDrawingEnabled)
                    {
                        AppState.Instance.PenColor = Colors.Blue;
                        AppState.Instance.IsHighlighterMode = shift;
                        if (shift) AppState.Instance.CurrentTool = DrawingTool.Highlighter;
                        else if (AppState.Instance.CurrentTool == DrawingTool.Highlighter) AppState.Instance.CurrentTool = DrawingTool.Pen;
                    }
                    break;
                case Key.Y:
                    if (_isDrawingEnabled)
                    {
                        AppState.Instance.PenColor = Colors.Yellow;
                        AppState.Instance.IsHighlighterMode = shift;
                        if (shift) AppState.Instance.CurrentTool = DrawingTool.Highlighter;
                        else if (AppState.Instance.CurrentTool == DrawingTool.Highlighter) AppState.Instance.CurrentTool = DrawingTool.Pen;
                    }
                    break;
                case Key.O:
                    if (_isDrawingEnabled)
                    {
                        AppState.Instance.PenColor = Colors.Orange;
                        AppState.Instance.IsHighlighterMode = shift;
                        if (shift) AppState.Instance.CurrentTool = DrawingTool.Highlighter;
                        else if (AppState.Instance.CurrentTool == DrawingTool.Highlighter) AppState.Instance.CurrentTool = DrawingTool.Pen;
                    }
                    break;
                case Key.P:
                    if (_isDrawingEnabled)
                    {
                        AppState.Instance.PenColor = Colors.HotPink;
                        AppState.Instance.IsHighlighterMode = shift;
                        if (shift) AppState.Instance.CurrentTool = DrawingTool.Highlighter;
                        else if (AppState.Instance.CurrentTool == DrawingTool.Highlighter) AppState.Instance.CurrentTool = DrawingTool.Pen;
                    }
                    break;

                // E: 전체 지우기
                case Key.E:
                    if (_isDrawingEnabled)
                    {
                        DrawOverlay.Children.Clear();
                        _drawUndoStack.Clear();
                    }
                    break;

                // Tab: 원 도구 전환
                case Key.Tab:
                    if (_isDrawingEnabled)
                    {
                        if (AppState.Instance.CurrentTool != DrawingTool.Circle)
                        {
                            _originalTool = AppState.Instance.CurrentTool;
                            AppState.Instance.CurrentTool = DrawingTool.Circle;
                        }
                        else
                        {
                            AppState.Instance.CurrentTool = _originalTool ?? DrawingTool.Pen;
                            _originalTool = null;
                        }
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab && _isDrawingEnabled && !_isDrawing && _originalTool.HasValue)
            {
                AppState.Instance.CurrentTool = _originalTool.Value;
                _originalTool = null;
            }
        }

        private void UpdateStatusText()
        {
            var features = new List<string> { $"{_zoomLevel:F1}x" };
            if (_isDrawingEnabled)
            {
                features.Add("그리기");
                features.Add($"두께:{AppState.Instance.PenThickness:F0}");
            }
            if (_isCursorHighlightEnabled) features.Add("커서");
            if (_isSpotlightEnabled) features.Add("스포트라이트");
            ZoomLevelText.Text = string.Join(" | ", features);
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
            _updateTimer.Stop();
            AppState.Instance.IsZoomActive = false;
            AppState.Instance.CurrentMode = AppMode.None;
            base.OnClosed(e);
        }
    }
}
