using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using PresentPen.Models;

namespace PresentPen.Views
{
    public partial class DrawingCanvasWindow : Window
    {
        private bool _isDrawing;
        private Point _startPoint;
        private Point _lastPoint;
        private Polyline? _currentPolyline;
        private Shape? _previewShape;
        private List<UIElement> _undoStack = new();
        private DrawingTool? _originalTool;

        private static readonly Dictionary<DrawingTool, string> ToolNames = new()
        {
            { DrawingTool.Pen, "펜" },
            { DrawingTool.Highlighter, "형광펜" },
            { DrawingTool.Line, "직선" },
            { DrawingTool.Arrow, "화살표" },
            { DrawingTool.Rectangle, "사각형" },
            { DrawingTool.Circle, "원" },
            { DrawingTool.Text, "텍스트" }
        };

        public DrawingCanvasWindow()
        {
            InitializeComponent();

            ThicknessSlider.Value = AppState.Instance.PenThickness;
            UpdateToolDisplay();

            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            MouseWheel += OnMouseWheel;

            // 5초 후 도움말 숨기기
            var hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            hideTimer.Tick += (s, e) =>
            {
                HelpBorder.Visibility = Visibility.Collapsed;
                hideTimer.Stop();
            };
            hideTimer.Start();
        }

        // === 도구 선택 ===
        private void ToolButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string toolName)
            {
                if (Enum.TryParse<DrawingTool>(toolName, out var tool))
                {
                    AppState.Instance.CurrentTool = tool;
                    AppState.Instance.IsHighlighterMode = (tool == DrawingTool.Highlighter);
                    UpdateToolDisplay();
                }
            }
        }

        private void UpdateToolDisplay()
        {
            var tool = AppState.Instance.CurrentTool;
            var highlighter = AppState.Instance.IsHighlighterMode ? " (형광)" : "";
            CurrentToolText.Text = $"도구: {ToolNames.GetValueOrDefault(tool, "펜")}{highlighter}";
        }

        // === 색상 선택 ===
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string colorName)
            {
                var color = (Color)ColorConverter.ConvertFromString(colorName);
                AppState.Instance.PenColor = color;
            }
        }

        // === 배경 모드 ===
        private void BackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string modeName)
            {
                SetBackground(modeName);
            }
        }

        private void SetBackground(string modeName)
        {
            switch (modeName)
            {
                case "Transparent":
                    AppState.Instance.BackgroundMode = BackgroundMode.Transparent;
                    BackgroundRect.Fill = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
                    break;
                case "Whiteboard":
                    AppState.Instance.BackgroundMode = BackgroundMode.Whiteboard;
                    BackgroundRect.Fill = Brushes.White;
                    break;
                case "Blackboard":
                    AppState.Instance.BackgroundMode = BackgroundMode.Blackboard;
                    BackgroundRect.Fill = new SolidColorBrush(Color.FromRgb(26, 71, 42));
                    break;
            }
        }

        // === 두께 조절 ===
        private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ThicknessText == null) return;
            var thickness = Math.Round(e.NewValue);
            ThicknessText.Text = thickness.ToString();
            AppState.Instance.PenThickness = thickness;
        }

        // === 스크롤로 선 굵기 조절 ===
        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var delta = e.Delta > 0 ? 1 : -1;
            AppState.Instance.PenThickness += delta;
            ThicknessSlider.Value = AppState.Instance.PenThickness;
        }

        // === 그리기 시작 ===
        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDrawing = true;
            _startPoint = e.GetPosition(DrawCanvas);
            _lastPoint = _startPoint;

            // 수정자 키에 따른 임시 도구 변경
            UpdateToolForModifiers();

            var tool = AppState.Instance.CurrentTool;

            if (tool == DrawingTool.Pen || tool == DrawingTool.Highlighter)
            {
                _currentPolyline = new Polyline
                {
                    Stroke = GetCurrentBrush(),
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

                _currentPolyline.Points.Add(_startPoint);
                DrawCanvas.Children.Add(_currentPolyline);
            }

            DrawCanvas.CaptureMouse();
        }

        // 수정자 키로 임시 도구 변경 (macOS 동일)
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

        // === 그리기 중 ===
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDrawing) return;

            var currentPoint = e.GetPosition(DrawCanvas);
            var tool = AppState.Instance.CurrentTool;

            if (tool == DrawingTool.Pen || tool == DrawingTool.Highlighter)
            {
                _currentPolyline?.Points.Add(currentPoint);
            }
            else
            {
                UpdatePreviewShape(currentPoint);
            }

            _lastPoint = currentPoint;
        }

        // === 그리기 종료 ===
        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDrawing) return;

            _isDrawing = false;
            DrawCanvas.ReleaseMouseCapture();

            var endPoint = e.GetPosition(DrawCanvas);
            var tool = AppState.Instance.CurrentTool;

            if (_previewShape != null)
            {
                DrawCanvas.Children.Remove(_previewShape);
                _previewShape = null;
            }

            if (tool != DrawingTool.Pen && tool != DrawingTool.Highlighter)
            {
                var shape = CreateFinalShape(_startPoint, endPoint);
                if (shape != null)
                {
                    DrawCanvas.Children.Add(shape);
                    _undoStack.Add(shape);
                }
            }
            else if (_currentPolyline != null)
            {
                _undoStack.Add(_currentPolyline);
                _currentPolyline = null;
            }

            // 임시 도구 복원
            if (_originalTool.HasValue)
            {
                AppState.Instance.CurrentTool = _originalTool.Value;
                _originalTool = null;
                UpdateToolDisplay();
            }
        }

        // === 미리보기 도형 업데이트 ===
        private void UpdatePreviewShape(Point currentPoint)
        {
            if (_previewShape != null)
            {
                DrawCanvas.Children.Remove(_previewShape);
            }

            _previewShape = CreateFinalShape(_startPoint, currentPoint);
            if (_previewShape != null)
            {
                _previewShape.Opacity = 0.5;
                DrawCanvas.Children.Add(_previewShape);
            }
        }

        // === 도형 생성 ===
        private Shape? CreateFinalShape(Point start, Point end)
        {
            var tool = AppState.Instance.CurrentTool;
            var brush = GetCurrentBrush();
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

        // === 화살표 생성 ===
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

        private Brush GetCurrentBrush()
        {
            return new SolidColorBrush(AppState.Instance.PenColor);
        }

        // === Undo ===
        private void UndoButton_Click(object sender, RoutedEventArgs e) => Undo();

        public void Undo()
        {
            if (_undoStack.Count > 0)
            {
                var lastElement = _undoStack[^1];
                _undoStack.RemoveAt(_undoStack.Count - 1);
                DrawCanvas.Children.Remove(lastElement);
            }
        }

        // === 전체 지우기 ===
        private void ClearButton_Click(object sender, RoutedEventArgs e) => ClearCanvas();

        public void ClearCanvas()
        {
            DrawCanvas.Children.Clear();
            _undoStack.Clear();
            AppState.Instance.BackgroundMode = BackgroundMode.Transparent;
            BackgroundRect.Fill = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
        }

        // === 키보드 입력 (macOS 동일 단축키) ===
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            var shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            switch (e.Key)
            {
                case Key.Escape:
                    (Application.Current.MainWindow as MainWindow)?.CloseAllFromOverlay();
                    break;

                case Key.Z:
                    if (Keyboard.Modifiers == ModifierKeys.Control) Undo();
                    break;

                // 색상 단축키 (R/G/B/Y/O/P)
                case Key.R:
                    AppState.Instance.PenColor = Colors.Red;
                    AppState.Instance.IsHighlighterMode = shift;
                    if (shift) AppState.Instance.CurrentTool = DrawingTool.Highlighter;
                    else if (AppState.Instance.CurrentTool == DrawingTool.Highlighter) AppState.Instance.CurrentTool = DrawingTool.Pen;
                    UpdateToolDisplay();
                    break;
                case Key.G:
                    AppState.Instance.PenColor = Colors.Green;
                    AppState.Instance.IsHighlighterMode = shift;
                    if (shift) AppState.Instance.CurrentTool = DrawingTool.Highlighter;
                    else if (AppState.Instance.CurrentTool == DrawingTool.Highlighter) AppState.Instance.CurrentTool = DrawingTool.Pen;
                    UpdateToolDisplay();
                    break;
                case Key.B:
                    AppState.Instance.PenColor = Colors.Blue;
                    AppState.Instance.IsHighlighterMode = shift;
                    if (shift) AppState.Instance.CurrentTool = DrawingTool.Highlighter;
                    else if (AppState.Instance.CurrentTool == DrawingTool.Highlighter) AppState.Instance.CurrentTool = DrawingTool.Pen;
                    UpdateToolDisplay();
                    break;
                case Key.Y:
                    AppState.Instance.PenColor = Colors.Yellow;
                    AppState.Instance.IsHighlighterMode = shift;
                    if (shift) AppState.Instance.CurrentTool = DrawingTool.Highlighter;
                    else if (AppState.Instance.CurrentTool == DrawingTool.Highlighter) AppState.Instance.CurrentTool = DrawingTool.Pen;
                    UpdateToolDisplay();
                    break;
                case Key.O:
                    AppState.Instance.PenColor = Colors.Orange;
                    AppState.Instance.IsHighlighterMode = shift;
                    if (shift) AppState.Instance.CurrentTool = DrawingTool.Highlighter;
                    else if (AppState.Instance.CurrentTool == DrawingTool.Highlighter) AppState.Instance.CurrentTool = DrawingTool.Pen;
                    UpdateToolDisplay();
                    break;
                case Key.P:
                    AppState.Instance.PenColor = Colors.HotPink;
                    AppState.Instance.IsHighlighterMode = shift;
                    if (shift) AppState.Instance.CurrentTool = DrawingTool.Highlighter;
                    else if (AppState.Instance.CurrentTool == DrawingTool.Highlighter) AppState.Instance.CurrentTool = DrawingTool.Pen;
                    UpdateToolDisplay();
                    break;

                // E: 전체 지우기
                case Key.E:
                    ClearCanvas();
                    break;

                // W: 화이트보드 토글
                case Key.W:
                    if (AppState.Instance.BackgroundMode == BackgroundMode.Whiteboard)
                        SetBackground("Transparent");
                    else
                        SetBackground("Whiteboard");
                    break;

                // K: 칠판 토글
                case Key.K:
                    if (AppState.Instance.BackgroundMode == BackgroundMode.Blackboard)
                        SetBackground("Transparent");
                    else
                        SetBackground("Blackboard");
                    break;

                // Tab: 원 도구 전환
                case Key.Tab:
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
                    UpdateToolDisplay();
                    e.Handled = true;
                    break;
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            // Tab 키 해제 시 원래 도구로 복원 (그리기 중이 아닐 때)
            if (e.Key == Key.Tab && !_isDrawing && _originalTool.HasValue)
            {
                AppState.Instance.CurrentTool = _originalTool.Value;
                _originalTool = null;
                UpdateToolDisplay();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            AppState.Instance.IsDrawingActive = false;
            AppState.Instance.CurrentMode = AppMode.None;
            base.OnClosed(e);
        }
    }
}
