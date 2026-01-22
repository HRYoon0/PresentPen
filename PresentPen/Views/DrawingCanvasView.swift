import SwiftUI

/// 그리기 캔버스 뷰
struct DrawingCanvasView: View {
    @ObservedObject var appState: AppState
    @State private var currentPath: [CGPoint] = []
    @State private var startPoint: CGPoint = .zero

    var body: some View {
        GeometryReader { geometry in
            ZStack {
                // 배경 (화이트보드/칠판 모드)
                if appState.currentMode == .drawing {
                    backgroundView
                }

                // 저장된 그리기 요소들 표시
                ForEach(appState.drawings) { element in
                    DrawingElementView(element: element)
                }

                // 현재 그리는 중인 요소
                if appState.currentMode == .drawing && !currentPath.isEmpty {
                    CurrentDrawingView(
                        tool: appState.currentTool,
                        points: currentPath,
                        startPoint: startPoint,
                        color: appState.currentColor,
                        lineWidth: appState.currentLineWidth,
                        isHighlighter: appState.isHighlighter
                    )
                }

                // 그리기 모드일 때 입력 영역
                if appState.currentMode == .drawing {
                    DrawingInputLayer(
                        currentPath: $currentPath,
                        startPoint: $startPoint,
                        appState: appState
                    )
                }

                // 도움말 표시 (그리기 모드에서만)
                if appState.currentMode == .drawing {
                    VStack {
                        DrawingHelpView(appState: appState)
                        Spacer()
                    }
                }

                // 현재 도구 표시 (상단 중앙)
                if appState.currentMode == .drawing {
                    VStack {
                        CurrentToolIndicator(tool: appState.currentTool, color: appState.currentColor)
                            .padding(.top, 60)
                        Spacer()
                    }
                }
            }
        }
    }

    /// 배경 뷰
    @ViewBuilder
    private var backgroundView: some View {
        switch appState.backgroundMode {
        case .transparent:
            Color.clear
        case .whiteboard:
            Color.white
        case .blackboard:
            Color(red: 0.1, green: 0.2, blue: 0.15) // 칠판 녹색
        }
    }
}

/// 현재 도구 표시 (상단 중앙)
struct CurrentToolIndicator: View {
    let tool: DrawingTool
    let color: Color

    var body: some View {
        HStack(spacing: 8) {
            // 도구 아이콘
            toolIcon
                .font(.system(size: 20))

            // 도구 이름
            Text(tool.rawValue)
                .font(.system(size: 16, weight: .semibold))

            // 색상 표시
            Circle()
                .fill(color)
                .frame(width: 16, height: 16)
                .overlay(
                    Circle()
                        .stroke(Color.white.opacity(0.5), lineWidth: 1)
                )
        }
        .foregroundColor(.white)
        .padding(.horizontal, 16)
        .padding(.vertical, 10)
        .background(
            Capsule()
                .fill(Color.black.opacity(0.75))
        )
        .shadow(color: .black.opacity(0.3), radius: 5)
    }

    @ViewBuilder
    private var toolIcon: some View {
        switch tool {
        case .pen:
            Image(systemName: "pencil")
        case .highlighter:
            Image(systemName: "highlighter")
        case .line:
            Image(systemName: "line.diagonal")
        case .arrow:
            Image(systemName: "arrow.up.right")
        case .rectangle:
            Image(systemName: "rectangle")
        case .circle:
            Image(systemName: "circle")
        case .text:
            Image(systemName: "textformat")
        }
    }
}

/// 그리기 모드 도움말 뷰
struct DrawingHelpView: View {
    @ObservedObject var appState: AppState
    @State private var isVisible = true

    var body: some View {
        if isVisible {
            VStack(spacing: 4) {
                Text("색상: R(빨강) G(초록) B(파랑) Y(노랑) O(주황) P(분홍)")
                Text("도구: Shift(직선) Ctrl(사각형) Tab(원) Ctrl+Shift(화살표)")
                Text("기타: E(지우기) Ctrl+Z(실행취소) W(화이트보드) K(칠판)")
            }
            .font(.system(size: 12))
            .foregroundColor(.white)
            .padding(8)
            .background(Color.black.opacity(0.7))
            .cornerRadius(8)
            .padding(.top, 10)
            .onAppear {
                // 5초 후 자동 숨김
                DispatchQueue.main.asyncAfter(deadline: .now() + 5) {
                    withAnimation {
                        isVisible = false
                    }
                }
            }
        }
    }
}

/// 개별 그리기 요소 뷰
struct DrawingElementView: View {
    let element: DrawingElement

    var body: some View {
        switch element.tool {
        case .pen, .highlighter:
            FreehandPath(points: element.points)
                .stroke(
                    element.color,
                    style: StrokeStyle(
                        lineWidth: element.lineWidth,
                        lineCap: .round,
                        lineJoin: .round
                    )
                )
                .opacity(element.tool == .highlighter ? 0.5 : 1.0)

        case .line:
            if element.points.count >= 2 {
                Path { path in
                    path.move(to: element.points.first!)
                    path.addLine(to: element.points.last!)
                }
                .stroke(element.color, lineWidth: element.lineWidth)
            }

        case .arrow:
            if element.points.count >= 2 {
                ArrowShape(from: element.points.first!, to: element.points.last!)
                    .stroke(element.color, lineWidth: element.lineWidth)
            }

        case .rectangle:
            if element.points.count >= 2 {
                let rect = CGRect(
                    origin: element.points.first!,
                    size: CGSize(
                        width: element.points.last!.x - element.points.first!.x,
                        height: element.points.last!.y - element.points.first!.y
                    )
                )
                Rectangle()
                    .stroke(element.color, lineWidth: element.lineWidth)
                    .frame(width: abs(rect.width), height: abs(rect.height))
                    .position(x: rect.midX, y: rect.midY)
            }

        case .circle:
            if element.points.count >= 2 {
                let center = element.points.first!
                let radius = hypot(
                    element.points.last!.x - center.x,
                    element.points.last!.y - center.y
                )
                Circle()
                    .stroke(element.color, lineWidth: element.lineWidth)
                    .frame(width: radius * 2, height: radius * 2)
                    .position(center)
            }

        case .text:
            if let text = element.text, let position = element.points.first {
                Text(text)
                    .font(.system(size: element.lineWidth * 8))
                    .foregroundColor(element.color)
                    .position(position)
            }
        }
    }
}

/// 현재 그리는 중인 요소 뷰
struct CurrentDrawingView: View {
    let tool: DrawingTool
    let points: [CGPoint]
    let startPoint: CGPoint
    let color: Color
    let lineWidth: CGFloat
    var isHighlighter: Bool = false

    var body: some View {
        switch tool {
        case .pen, .highlighter:
            FreehandPath(points: points)
                .stroke(
                    color,
                    style: StrokeStyle(lineWidth: isHighlighter ? lineWidth * 3 : lineWidth, lineCap: .round, lineJoin: .round)
                )
                .opacity(isHighlighter ? 0.4 : 1.0)

        case .line:
            if let last = points.last {
                Path { path in
                    path.move(to: startPoint)
                    path.addLine(to: last)
                }
                .stroke(color, lineWidth: lineWidth)
            }

        case .arrow:
            if let last = points.last {
                ArrowShape(from: startPoint, to: last)
                    .stroke(color, lineWidth: lineWidth)
            }

        case .rectangle:
            if let last = points.last {
                let rect = CGRect(
                    x: min(startPoint.x, last.x),
                    y: min(startPoint.y, last.y),
                    width: abs(last.x - startPoint.x),
                    height: abs(last.y - startPoint.y)
                )
                Rectangle()
                    .stroke(color, lineWidth: lineWidth)
                    .frame(width: rect.width, height: rect.height)
                    .position(x: rect.midX, y: rect.midY)
            }

        case .circle:
            if let last = points.last {
                let radius = hypot(last.x - startPoint.x, last.y - startPoint.y)
                Circle()
                    .stroke(color, lineWidth: lineWidth)
                    .frame(width: radius * 2, height: radius * 2)
                    .position(startPoint)
            }

        case .text:
            EmptyView()
        }
    }
}

/// 자유 곡선 경로
struct FreehandPath: Shape {
    let points: [CGPoint]

    func path(in rect: CGRect) -> Path {
        var path = Path()
        guard points.count > 1 else { return path }

        path.move(to: points[0])
        for point in points.dropFirst() {
            path.addLine(to: point)
        }
        return path
    }
}

/// 화살표 모양
struct ArrowShape: Shape {
    let from: CGPoint
    let to: CGPoint

    func path(in rect: CGRect) -> Path {
        var path = Path()

        // 메인 라인
        path.move(to: from)
        path.addLine(to: to)

        // 화살표 머리 (크기 증가)
        let angle = atan2(to.y - from.y, to.x - from.x)
        let arrowLength: CGFloat = 35
        let arrowAngle: CGFloat = .pi / 6

        let arrowPoint1 = CGPoint(
            x: to.x - arrowLength * cos(angle - arrowAngle),
            y: to.y - arrowLength * sin(angle - arrowAngle)
        )
        let arrowPoint2 = CGPoint(
            x: to.x - arrowLength * cos(angle + arrowAngle),
            y: to.y - arrowLength * sin(angle + arrowAngle)
        )

        path.move(to: to)
        path.addLine(to: arrowPoint1)
        path.move(to: to)
        path.addLine(to: arrowPoint2)

        return path
    }
}

/// 그리기 입력 레이어 (NSViewRepresentable로 마우스 이벤트 처리)
struct DrawingInputLayer: NSViewRepresentable {
    @Binding var currentPath: [CGPoint]
    @Binding var startPoint: CGPoint
    var appState: AppState

    func makeNSView(context: Context) -> DrawingInputView {
        let view = DrawingInputView()
        view.delegate = context.coordinator
        view.appState = appState
        return view
    }

    func updateNSView(_ nsView: DrawingInputView, context: Context) {
        nsView.appState = appState
    }

    func makeCoordinator() -> Coordinator {
        Coordinator(self)
    }

    class Coordinator: NSObject, DrawingInputViewDelegate {
        var parent: DrawingInputLayer

        init(_ parent: DrawingInputLayer) {
            self.parent = parent
        }

        func drawingBegan(at point: CGPoint) {
            parent.startPoint = point
            parent.currentPath = [point]
        }

        func drawingMoved(to point: CGPoint) {
            parent.currentPath.append(point)
        }

        func drawingEnded(at point: CGPoint) {
            parent.currentPath.append(point)

            // 그리기 요소 저장
            let element = DrawingElement(
                tool: parent.appState.currentTool,
                points: parent.currentPath,
                color: parent.appState.currentColor,
                lineWidth: parent.appState.currentLineWidth
            )
            parent.appState.addDrawing(element)

            // 현재 경로 초기화
            parent.currentPath = []
        }
    }
}

// 그리기 입력 뷰 델리게이트
protocol DrawingInputViewDelegate: AnyObject {
    func drawingBegan(at point: CGPoint)
    func drawingMoved(to point: CGPoint)
    func drawingEnded(at point: CGPoint)
}

/// 마우스 이벤트를 처리하는 NSView
class DrawingInputView: NSView {
    weak var delegate: DrawingInputViewDelegate?
    var appState: AppState?

    // 수정자 키에 따른 임시 도구
    private var originalTool: DrawingTool?

    override func mouseDown(with event: NSEvent) {
        // 수정자 키에 따라 임시로 도구 변경
        updateToolForModifiers(event.modifierFlags)

        let point = convert(event.locationInWindow, from: nil)
        let flippedPoint = CGPoint(x: point.x, y: bounds.height - point.y)
        delegate?.drawingBegan(at: flippedPoint)
    }

    override func mouseDragged(with event: NSEvent) {
        let point = convert(event.locationInWindow, from: nil)
        let flippedPoint = CGPoint(x: point.x, y: bounds.height - point.y)
        delegate?.drawingMoved(to: flippedPoint)
    }

    override func mouseUp(with event: NSEvent) {
        let point = convert(event.locationInWindow, from: nil)
        let flippedPoint = CGPoint(x: point.x, y: bounds.height - point.y)
        delegate?.drawingEnded(at: flippedPoint)

        // 원래 도구로 복원
        restoreOriginalTool()
    }

    override func keyDown(with event: NSEvent) {
        guard let appState = appState else {
            super.keyDown(with: event)
            return
        }

        // keyCode 사용 - 한글/영어 입력 상태와 관계없이 물리적 키 위치로 판단
        let keyCode = event.keyCode
        let hasShift = event.modifierFlags.contains(.shift)

        switch keyCode {
        // 색상 단축키 (keyCode는 물리적 키 위치)
        case 15: // R
            appState.currentColor = .red
            appState.isHighlighter = hasShift
        case 5:  // G
            appState.currentColor = .green
            appState.isHighlighter = hasShift
        case 11: // B
            appState.currentColor = .blue
            appState.isHighlighter = hasShift
        case 16: // Y
            appState.currentColor = .yellow
            appState.isHighlighter = hasShift
        case 31: // O
            appState.currentColor = .orange
            appState.isHighlighter = hasShift
        case 35: // P
            appState.currentColor = .pink
            appState.isHighlighter = hasShift

        // 배경 모드
        case 13: // W
            appState.backgroundMode = appState.backgroundMode == .whiteboard ? .transparent : .whiteboard
        case 40: // K
            appState.backgroundMode = appState.backgroundMode == .blackboard ? .transparent : .blackboard

        // 전체 지우기
        case 14: // E
            appState.clearDrawings()
            appState.backgroundMode = .transparent

        // 실행취소 (Cmd+Z 또는 Ctrl+Z)
        case 6:  // Z
            if event.modifierFlags.contains(.command) || event.modifierFlags.contains(.control) {
                appState.undo()
            }

        // Tab 키로 원 도구
        case 48: // Tab
            if appState.currentTool != .circle {
                originalTool = appState.currentTool
                appState.currentTool = .circle
            }

        default:
            super.keyDown(with: event)
        }
    }

    /// 수정자 키에 따라 도구 변경
    private func updateToolForModifiers(_ flags: NSEvent.ModifierFlags) {
        guard let appState = appState else { return }

        originalTool = appState.currentTool

        if flags.contains(.control) && flags.contains(.shift) {
            // Ctrl+Shift: 화살표
            appState.currentTool = .arrow
        } else if flags.contains(.control) {
            // Ctrl: 사각형
            appState.currentTool = .rectangle
        } else if flags.contains(.shift) {
            // Shift: 직선
            appState.currentTool = .line
        }
        // Tab은 flagsChanged에서 처리해야 하지만, 일단 기본 구현
    }

    /// 원래 도구로 복원
    private func restoreOriginalTool() {
        guard let appState = appState, let original = originalTool else { return }
        appState.currentTool = original
        originalTool = nil
    }

    override func flagsChanged(with event: NSEvent) {
        guard let appState = appState else { return }

        // Tab 키는 별도 처리 (원 도구)
        if event.modifierFlags.contains(.function) {
            originalTool = appState.currentTool
            appState.currentTool = .circle
        }
    }

    override var acceptsFirstResponder: Bool { true }

    override func becomeFirstResponder() -> Bool {
        return true
    }
}
