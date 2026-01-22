using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace PresentPen.Models
{
    /// <summary>
    /// 앱의 현재 활성화된 모드
    /// </summary>
    public enum AppMode
    {
        None,       // 비활성
        Zoom,       // 화면 확대
        Draw,       // 그리기
        Spotlight,  // 스포트라이트
        Timer       // 타이머
    }

    /// <summary>
    /// 배경 모드
    /// </summary>
    public enum BackgroundMode
    {
        Transparent,  // 투명 (기본)
        Whiteboard,   // 화이트보드
        Blackboard    // 칠판
    }

    /// <summary>
    /// 그리기 도구 종류
    /// </summary>
    public enum DrawingTool
    {
        Pen,         // 펜
        Highlighter, // 형광펜
        Line,        // 직선
        Arrow,       // 화살표
        Rectangle,   // 사각형
        Circle,      // 원
        Text         // 텍스트
    }

    /// <summary>
    /// 커서 하이라이트 스타일
    /// </summary>
    public enum CursorHighlightStyle
    {
        Ring,      // 링 (테두리만)
        Halo,      // 헤일로 (그라데이션)
        Filled,    // 채워진 원
        Squircle   // 둥근 사각형
    }

    /// <summary>
    /// 하나의 그리기 요소
    /// </summary>
    public class DrawingElement
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DrawingTool Tool { get; set; }
        public List<System.Windows.Point> Points { get; set; } = new();
        public Color Color { get; set; }
        public double LineWidth { get; set; }
        public string? Text { get; set; }
        public bool IsHighlighter { get; set; }
    }

    /// <summary>
    /// 앱 전역 상태 관리 (싱글톤)
    /// </summary>
    public class AppState : INotifyPropertyChanged
    {
        private static AppState? _instance;
        public static AppState Instance => _instance ??= new AppState();

        public event PropertyChangedEventHandler? PropertyChanged;

        private AppState()
        {
            // 기본값 설정
            _penColor = Colors.Red;
            _penThickness = 3.0;
            _highlightColor = Colors.Yellow;
            _highlightRadius = 30;
            _spotlightRadius = 150;
            _zoomLevel = 2.0;
            _currentTool = DrawingTool.Pen;
            _backgroundMode = BackgroundMode.Transparent;
            _cursorHighlightStyle = CursorHighlightStyle.Halo;
            _spotlightZoomEnabled = true;
            _spotlightZoomLevel = 1.5;
        }

        // === 현재 모드 ===
        private AppMode _currentMode = AppMode.None;
        public AppMode CurrentMode
        {
            get => _currentMode;
            set { _currentMode = value; OnPropertyChanged(); }
        }

        // === 줌 설정 ===
        private double _zoomLevel;
        public double ZoomLevel
        {
            get => _zoomLevel;
            set { _zoomLevel = Math.Clamp(value, 1.0, 10.0); OnPropertyChanged(); }
        }

        private bool _isZoomActive;
        public bool IsZoomActive
        {
            get => _isZoomActive;
            set { _isZoomActive = value; OnPropertyChanged(); }
        }

        // === 그리기 설정 ===
        private DrawingTool _currentTool;
        public DrawingTool CurrentTool
        {
            get => _currentTool;
            set { _currentTool = value; OnPropertyChanged(); }
        }

        private Color _penColor;
        public Color PenColor
        {
            get => _penColor;
            set { _penColor = value; OnPropertyChanged(); }
        }

        private double _penThickness;
        public double PenThickness
        {
            get => _penThickness;
            set { _penThickness = Math.Clamp(value, 1.0, 20.0); OnPropertyChanged(); }
        }

        private bool _isDrawingActive;
        public bool IsDrawingActive
        {
            get => _isDrawingActive;
            set { _isDrawingActive = value; OnPropertyChanged(); }
        }

        private BackgroundMode _backgroundMode;
        public BackgroundMode BackgroundMode
        {
            get => _backgroundMode;
            set { _backgroundMode = value; OnPropertyChanged(); }
        }

        private bool _isHighlighterMode;
        public bool IsHighlighterMode
        {
            get => _isHighlighterMode;
            set { _isHighlighterMode = value; OnPropertyChanged(); }
        }

        // === Undo 스택 ===
        private List<List<DrawingElement>> _undoStack = new();
        public List<List<DrawingElement>> UndoStack
        {
            get => _undoStack;
            set { _undoStack = value; OnPropertyChanged(); }
        }

        private List<DrawingElement> _drawings = new();
        public List<DrawingElement> Drawings
        {
            get => _drawings;
            set { _drawings = value; OnPropertyChanged(); }
        }

        // === 커서 하이라이트 설정 ===
        private bool _isCursorHighlightEnabled;
        public bool IsCursorHighlightEnabled
        {
            get => _isCursorHighlightEnabled;
            set { _isCursorHighlightEnabled = value; OnPropertyChanged(); }
        }

        private Color _highlightColor;
        public Color HighlightColor
        {
            get => _highlightColor;
            set { _highlightColor = value; OnPropertyChanged(); }
        }

        private int _highlightRadius;
        public int HighlightRadius
        {
            get => _highlightRadius;
            set { _highlightRadius = Math.Clamp(value, 10, 100); OnPropertyChanged(); }
        }

        private double _highlightOpacity = 0.5;
        public double HighlightOpacity
        {
            get => _highlightOpacity;
            set { _highlightOpacity = Math.Clamp(value, 0.1, 1.0); OnPropertyChanged(); }
        }

        private CursorHighlightStyle _cursorHighlightStyle;
        public CursorHighlightStyle CursorHighlightStyle
        {
            get => _cursorHighlightStyle;
            set { _cursorHighlightStyle = value; OnPropertyChanged(); }
        }

        private int _cursorHighlightColorIndex = 0;
        public int CursorHighlightColorIndex
        {
            get => _cursorHighlightColorIndex;
            set { _cursorHighlightColorIndex = value; OnPropertyChanged(); }
        }

        // 커서 하이라이트 색상 목록
        public static readonly (Color Color, string Name)[] CursorHighlightColors = new[]
        {
            (Colors.Yellow, "노랑"),
            (Colors.Red, "빨강"),
            (Colors.LimeGreen, "초록"),
            (Colors.DodgerBlue, "파랑"),
            (Colors.Orange, "주황"),
            (Colors.HotPink, "분홍"),
            (Colors.Purple, "보라"),
            (Colors.Cyan, "청록"),
            (Colors.White, "흰색")
        };

        // === 스포트라이트 설정 ===
        private bool _isSpotlightActive;
        public bool IsSpotlightActive
        {
            get => _isSpotlightActive;
            set { _isSpotlightActive = value; OnPropertyChanged(); }
        }

        private int _spotlightRadius;
        public int SpotlightRadius
        {
            get => _spotlightRadius;
            set { _spotlightRadius = Math.Clamp(value, 50, 500); OnPropertyChanged(); }
        }

        private bool _spotlightZoomEnabled;
        public bool SpotlightZoomEnabled
        {
            get => _spotlightZoomEnabled;
            set { _spotlightZoomEnabled = value; OnPropertyChanged(); }
        }

        private double _spotlightZoomLevel;
        public double SpotlightZoomLevel
        {
            get => _spotlightZoomLevel;
            set { _spotlightZoomLevel = Math.Clamp(value, 1.0, 5.0); OnPropertyChanged(); }
        }

        // === 타이머 설정 ===
        private bool _isTimerActive;
        public bool IsTimerActive
        {
            get => _isTimerActive;
            set { _isTimerActive = value; OnPropertyChanged(); }
        }

        private int _timerSeconds;
        public int TimerSeconds
        {
            get => _timerSeconds;
            set { _timerSeconds = Math.Max(0, value); OnPropertyChanged(); }
        }

        // === 헬퍼 메서드 ===
        public void ToggleMode(AppMode mode)
        {
            if (CurrentMode == mode)
            {
                CurrentMode = AppMode.None;
                DeactivateAll();
            }
            else
            {
                DeactivateAll();
                CurrentMode = mode;
                ActivateMode(mode);
            }
        }

        private void ActivateMode(AppMode mode)
        {
            switch (mode)
            {
                case AppMode.Zoom:
                    IsZoomActive = true;
                    break;
                case AppMode.Draw:
                    IsDrawingActive = true;
                    break;
                case AppMode.Spotlight:
                    IsSpotlightActive = true;
                    break;
                case AppMode.Timer:
                    IsTimerActive = true;
                    break;
            }
        }

        private void DeactivateAll()
        {
            IsZoomActive = false;
            IsDrawingActive = false;
            IsSpotlightActive = false;
            IsTimerActive = false;
        }

        /// <summary>
        /// 그리기 추가
        /// </summary>
        public void AddDrawing(DrawingElement element)
        {
            UndoStack.Add(new List<DrawingElement>(Drawings));
            Drawings.Add(element);
            OnPropertyChanged(nameof(Drawings));
        }

        /// <summary>
        /// 실행 취소
        /// </summary>
        public void Undo()
        {
            if (UndoStack.Count == 0) return;
            var previousState = UndoStack[^1];
            UndoStack.RemoveAt(UndoStack.Count - 1);
            Drawings = previousState;
        }

        /// <summary>
        /// 전체 지우기
        /// </summary>
        public void ClearDrawings()
        {
            UndoStack.Add(new List<DrawingElement>(Drawings));
            Drawings = new List<DrawingElement>();
            OnPropertyChanged(nameof(Drawings));
        }

        /// <summary>
        /// 커서 하이라이트 색상 순환
        /// </summary>
        public void CycleCursorHighlightColor()
        {
            CursorHighlightColorIndex = (CursorHighlightColorIndex + 1) % CursorHighlightColors.Length;
            HighlightColor = CursorHighlightColors[CursorHighlightColorIndex].Color;
        }

        /// <summary>
        /// 커서 하이라이트 스타일 순환
        /// </summary>
        public void CycleCursorHighlightStyle()
        {
            var styles = Enum.GetValues<CursorHighlightStyle>();
            int currentIndex = Array.IndexOf(styles, CursorHighlightStyle);
            CursorHighlightStyle = styles[(currentIndex + 1) % styles.Length];
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
