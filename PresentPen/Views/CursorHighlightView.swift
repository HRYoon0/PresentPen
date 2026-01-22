import SwiftUI
import AppKit

/// 커서 하이라이트 스타일
enum CursorHighlightStyle: String, CaseIterable {
    case ring = "링"           // 테두리만 있는 원
    case halo = "헤일로"        // 그라데이션 후광
    case filled = "채움"        // 기존 채워진 원
    case squircle = "스퀴클"    // 둥근 사각형
}

/// 커서 하이라이트 뷰 (Presentify 스타일)
struct CursorHighlightView: View {
    @ObservedObject var appState: AppState
    @State private var isClicked = false
    @State private var clickScale: CGFloat = 1.0
    @State private var scrollMonitor: Any?

    private var currentStyle: CursorHighlightStyle {
        CursorHighlightStyle.allCases[safe: appState.cursorHighlightStyleIndex] ?? .halo
    }

    var body: some View {
        GeometryReader { geometry in
            ZStack {
                PresentifyCursorHighlight(
                    position: appState.cursorPosition,
                    radius: appState.cursorHighlightRadius,
                    color: appState.cursorHighlightColor,
                    style: currentStyle,
                    isClicked: isClicked,
                    clickScale: clickScale
                )

                // 모드 인디케이터 (상단 중앙) - 다른 모드가 활성화되지 않은 경우에만 표시
                if appState.currentMode == .none {
                    VStack {
                        CursorHighlightModeIndicator(radius: appState.cursorHighlightRadius)
                            .padding(.top, 40)
                        Spacer()
                    }
                }
            }
        }
        .allowsHitTesting(false)
        .onAppear {
            setupScrollMonitor()
        }
        .onDisappear {
            removeScrollMonitor()
        }
    }

    private func setupScrollMonitor() {
        // Control + 스크롤로 크기 변경 (일반 스크롤은 앱에 전달)
        scrollMonitor = NSEvent.addGlobalMonitorForEvents(matching: .scrollWheel) { event in
            // Control 키가 눌려있을 때만 크기 변경
            guard event.modifierFlags.contains(.control) else { return }

            let delta = event.scrollingDeltaY
            if abs(delta) > 0.5 {
                let newRadius = appState.cursorHighlightRadius + (delta > 0 ? 5 : -5)
                appState.cursorHighlightRadius = max(15, min(100, newRadius))
                print("✨ 커서 하이라이트 크기: \(Int(appState.cursorHighlightRadius))")
            }
        }
    }

    private func removeScrollMonitor() {
        if let monitor = scrollMonitor {
            NSEvent.removeMonitor(monitor)
            scrollMonitor = nil
        }
    }
}

/// 커서 하이라이트 모드 인디케이터
struct CursorHighlightModeIndicator: View {
    var radius: CGFloat

    var body: some View {
        VStack(spacing: 6) {
            HStack(spacing: 10) {
                Image(systemName: "cursor.rays")
                    .font(.system(size: 22))
                Text("커서 하이라이트")
                    .font(.system(size: 16, weight: .semibold))
                Text("(\(Int(radius)))")
                    .font(.system(size: 14))
                    .opacity(0.7)
            }
            Text("⌃+스크롤: 크기 | ESC: 종료")
                .font(.system(size: 11))
                .opacity(0.6)
        }
        .foregroundColor(.black)
        .padding(.horizontal, 20)
        .padding(.vertical, 10)
        .background(
            RoundedRectangle(cornerRadius: 16)
                .fill(Color.yellow.opacity(0.9))
        )
        .shadow(color: .black.opacity(0.3), radius: 8)
    }
}

// Array 안전 접근 확장
extension Array {
    subscript(safe index: Int) -> Element? {
        return indices.contains(index) ? self[index] : nil
    }
}

/// Presentify 스타일 커서 하이라이트 컴포넌트
struct PresentifyCursorHighlight: View {
    var position: CGPoint
    var radius: CGFloat
    var color: Color
    var style: CursorHighlightStyle
    var isClicked: Bool
    var clickScale: CGFloat

    var body: some View {
        ZStack {
            switch style {
            case .ring:
                // 링 스타일 - 테두리만 있는 원
                Circle()
                    .stroke(
                        color,
                        lineWidth: 3
                    )
                    .frame(width: radius * 2, height: radius * 2)
                    .shadow(color: color.opacity(0.5), radius: 8)

            case .halo:
                // 헤일로 스타일 - 그라데이션 후광 (Presentify 메인 스타일)
                ZStack {
                    // 외부 글로우
                    Circle()
                        .fill(
                            RadialGradient(
                                gradient: Gradient(colors: [
                                    color.opacity(0.0),
                                    color.opacity(0.1),
                                    color.opacity(0.3),
                                    color.opacity(0.0)
                                ]),
                                center: .center,
                                startRadius: radius * 0.3,
                                endRadius: radius * 1.2
                            )
                        )
                        .frame(width: radius * 2.4, height: radius * 2.4)

                    // 메인 링
                    Circle()
                        .stroke(
                            LinearGradient(
                                gradient: Gradient(colors: [
                                    color.opacity(0.9),
                                    color.opacity(0.6)
                                ]),
                                startPoint: .top,
                                endPoint: .bottom
                            ),
                            lineWidth: 2.5
                        )
                        .frame(width: radius * 2, height: radius * 2)

                    // 내부 글로우
                    Circle()
                        .fill(
                            RadialGradient(
                                gradient: Gradient(colors: [
                                    color.opacity(0.15),
                                    color.opacity(0.05),
                                    color.opacity(0.0)
                                ]),
                                center: .center,
                                startRadius: 0,
                                endRadius: radius * 0.8
                            )
                        )
                        .frame(width: radius * 1.6, height: radius * 1.6)
                }
                .shadow(color: color.opacity(0.4), radius: 10)

            case .filled:
                // 채움 스타일 - 기존 스타일
                Circle()
                    .fill(color.opacity(0.4))
                    .frame(width: radius * 2, height: radius * 2)
                    .shadow(color: color.opacity(0.3), radius: 5)

            case .squircle:
                // 스퀴클 스타일 - 둥근 사각형
                RoundedRectangle(cornerRadius: radius * 0.4)
                    .stroke(color, lineWidth: 2.5)
                    .frame(width: radius * 1.8, height: radius * 1.8)
                    .shadow(color: color.opacity(0.4), radius: 8)
            }
        }
        .scaleEffect(clickScale)
        .position(x: position.x, y: position.y)
    }
}

/// 클릭 감지를 위한 이벤트 모니터
class ClickEventMonitor: ObservableObject {
    @Published var isLeftClicked = false
    @Published var isRightClicked = false

    private var leftClickMonitor: Any?
    private var rightClickMonitor: Any?

    func start(onLeftClick: @escaping () -> Void, onRightClick: @escaping () -> Void) {
        leftClickMonitor = NSEvent.addGlobalMonitorForEvents(matching: [.leftMouseDown, .leftMouseUp]) { event in
            if event.type == .leftMouseDown {
                onLeftClick()
            }
        }

        rightClickMonitor = NSEvent.addGlobalMonitorForEvents(matching: [.rightMouseDown, .rightMouseUp]) { event in
            if event.type == .rightMouseDown {
                onRightClick()
            }
        }
    }

    func stop() {
        if let monitor = leftClickMonitor {
            NSEvent.removeMonitor(monitor)
        }
        if let monitor = rightClickMonitor {
            NSEvent.removeMonitor(monitor)
        }
    }
}

/// 커서 위치 추적 서비스
class CursorTracker {
    private var appState: AppState
    private var timer: Timer?

    init(appState: AppState) {
        self.appState = appState
    }

    func start() {
        // 60fps로 커서 위치 업데이트
        timer = Timer.scheduledTimer(withTimeInterval: 1.0/60.0, repeats: true) { [weak self] _ in
            self?.updateCursorPosition()
        }
    }

    func stop() {
        timer?.invalidate()
        timer = nil
    }

    private func updateCursorPosition() {
        let mouseLocation = NSEvent.mouseLocation

        // 스크린 좌표를 앱 좌표로 변환
        if let screen = NSScreen.main {
            let flippedY = screen.frame.height - mouseLocation.y
            DispatchQueue.main.async {
                self.appState.cursorPosition = CGPoint(x: mouseLocation.x, y: flippedY)
            }
        }
    }
}
