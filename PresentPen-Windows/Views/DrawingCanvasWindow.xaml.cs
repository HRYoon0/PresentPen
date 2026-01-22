using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
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
            CurrentToolText.Text = $"도구: {ToolNames.GetValueOrDefault(tool, "펜")}";
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
                switch (modeName)
                {
                    case "Transparent":
                        AppState.Instance.BackgroundMode = BackgroundMode.Transparent;
                        BackgroundRect.Fill = Brushes.Transparent;
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
        }

        // === 두께 조절 ===
        private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ThicknessText == null) return;
            var thickness = Math.Round(e.NewValue);
            ThicknessText.Text = thickness.ToString();
            AppState.Instance.PenThickness = thickness;
        }

        // === 그리기 시작 ===
        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDrawing = true;
            _startPoint = e.GetPosition(DrawCanvas);
            _lastPoint = _startPoint;

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

                if (tool == DrawingTool.Highlighter)
                {
                    _currentPolyline.Opacity = 0.4;
                    _currentPolyline.StrokeThickness = AppState.Instance.PenThickness * 3;
                }

                _currentPolyline.Points.Add(_startPoint);
                DrawCanvas.Children.Add(_currentPolyline);
            }

            DrawCanvas.CaptureMouse();
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
                // 도형 미리보기
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

            // 미리보기 제거
            if (_previewShape != null)
            {
                DrawCanvas.Children.Remove(_previewShape);
                _previewShape = null;
            }

            // 최종 도형 그리기
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

            // 선
            figure.Segments.Add(new LineSegment(end, true));
            geometry.Figures.Add(figure);

            // 화살표 머리
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
        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            Undo();
        }

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
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearCanvas();
        }

        public void ClearCanvas()
        {
            DrawCanvas.Children.Clear();
            _undoStack.Clear();
        }

        // === 키보드 입력 ===
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            else if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Undo();
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
