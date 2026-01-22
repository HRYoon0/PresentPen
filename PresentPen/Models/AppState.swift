import SwiftUI
import Combine

// 앱의 현재 모드
enum AppMode: Equatable {
    case none       // 비활성
    case drawing    // 그리기 모드
    case zoom       // 줌 모드
    case spotlight  // 스포트라이트 모드
    case timer      // 타이머 모드
}

// 배경 모드
enum BackgroundMode: Equatable {
    case transparent  // 투명 (기본)
    case whiteboard   // 화이트보드
    case blackboard   // 칠판
}

// 그리기 도구 종류
enum DrawingTool: String, CaseIterable {
    case pen = "펜"
    case highlighter = "형광펜"
    case line = "직선"
    case arrow = "화살표"
    case rectangle = "사각형"
    case circle = "원"
    case text = "텍스트"
}

// 하나의 그리기 요소
struct DrawingElement: Identifiable {
    let id = UUID()
    var tool: DrawingTool
    var points: [CGPoint]
    var color: Color
    var lineWidth: CGFloat
    var text: String?  // 텍스트 도구용
}

// 전역 앱 상태
class AppState: ObservableObject {
    // MARK: - 모드 상태
    @Published var currentMode: AppMode = .none
    @Published var cursorHighlightEnabled: Bool = false

    // MARK: - 그리기 상태
    @Published var drawings: [DrawingElement] = []
    @Published var currentTool: DrawingTool = .pen
    @Published var currentColor: Color = .red
    @Published var currentLineWidth: CGFloat = 3.0
    @Published var undoStack: [[DrawingElement]] = []
    @Published var backgroundMode: BackgroundMode = .transparent
    @Published var isHighlighter: Bool = false  // 형광펜 모드
    @Published var currentDrawingPath: [CGPoint] = []  // 현재 그리기 중인 경로
    @Published var currentDrawingStartPoint: CGPoint = .zero  // 현재 그리기 시작점

    // MARK: - 줌 상태
    @Published var zoomLevel: CGFloat = 2.0
    @Published var zoomCenter: CGPoint = .zero

    // MARK: - 스포트라이트 상태
    @Published var spotlightRadius: CGFloat = 150
    @Published var spotlightCenter: CGPoint = .zero
    @Published var spotlightZoomEnabled: Bool = true  // 라이브 줌 활성화 (Presentify 스타일)
    @Published var spotlightZoomLevel: CGFloat = 1.5  // 확대 배율 (1.0 ~ 5.0)

    // MARK: - 커서 하이라이트 설정
    @Published var cursorHighlightColor: Color = .yellow
    @Published var cursorHighlightRadius: CGFloat = 30
    @Published var cursorHighlightOpacity: Double = 0.5
    @Published var cursorHighlightStyleIndex: Int = 1  // 0: ring, 1: halo, 2: filled, 3: squircle
    @Published var cursorHighlightColorIndex: Int = 0  // 색상 인덱스

    // 커서 하이라이트 색상 목록
    static let cursorHighlightColors: [(Color, String)] = [
        (.yellow, "노랑"),
        (.red, "빨강"),
        (.green, "초록"),
        (.blue, "파랑"),
        (.orange, "주황"),
        (.pink, "분홍"),
        (.purple, "보라"),
        (.cyan, "청록"),
        (.white, "흰색")
    ]

    // MARK: - 커서 위치
    @Published var cursorPosition: CGPoint = .zero
    @Published var zoomCursorPosition: CGPoint = .zero  // 줌 윈도우 내 커서 위치

    // MARK: - 메서드

    /// 모드 토글
    func toggleMode(_ mode: AppMode) {
        if currentMode == mode {
            currentMode = .none
        } else {
            currentMode = mode
        }
    }

    /// 그리기 추가
    func addDrawing(_ element: DrawingElement) {
        // 실행 취소를 위해 현재 상태 저장
        undoStack.append(drawings)
        drawings.append(element)
    }

    /// 실행 취소
    func undo() {
        guard let previousState = undoStack.popLast() else { return }
        drawings = previousState
    }

    /// 전체 지우기
    func clearDrawings() {
        undoStack.append(drawings)
        drawings.removeAll()
    }

    /// 커서 하이라이트 색상 순환
    func cycleCursorHighlightColor() {
        cursorHighlightColorIndex = (cursorHighlightColorIndex + 1) % AppState.cursorHighlightColors.count
        let (color, name) = AppState.cursorHighlightColors[cursorHighlightColorIndex]
        cursorHighlightColor = color
        print("🎨 커서 하이라이트 색상: \(name)")
    }

    /// 현재 모드가 활성화되어 있는지 확인
    var isAnyModeActive: Bool {
        currentMode != .none || cursorHighlightEnabled
    }
}
