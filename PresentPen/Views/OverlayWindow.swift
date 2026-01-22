import SwiftUI
import AppKit

/// 화면 전체를 덮는 투명 오버레이 윈도우
class OverlayWindow: NSWindow {
    private var appState: AppState
    private var hostingView: NSHostingView<OverlayContentView>?

    init(screen: NSScreen, appState: AppState) {
        self.appState = appState

        super.init(
            contentRect: screen.frame,
            styleMask: [.borderless],
            backing: .buffered,
            defer: false
        )

        // 윈도우 설정
        self.level = .floating                    // 다른 윈도우 위에 표시
        self.backgroundColor = .clear             // 투명 배경
        self.isOpaque = false                     // 불투명하지 않음
        self.hasShadow = false                    // 그림자 없음
        self.ignoresMouseEvents = true            // 기본적으로 마우스 이벤트 무시
        self.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary]

        // SwiftUI 뷰를 윈도우에 추가
        let contentView = OverlayContentView(appState: appState)
        hostingView = NSHostingView(rootView: contentView)
        hostingView?.frame = screen.frame
        self.contentView = hostingView

        // 윈도우 표시
        self.orderFrontRegardless()
    }

    /// 콘텐츠 업데이트 (모드 변경 시 호출)
    func updateContent() {
        print("  📍 OverlayWindow.updateContent() 호출됨")

        // 모드에 따라 마우스 이벤트 처리 여부 결정
        if appState.currentMode == .drawing {
            self.ignoresMouseEvents = false
            print("  ✏️ 그리기 모드: 마우스 이벤트 활성화")
        } else {
            self.ignoresMouseEvents = true
        }

        // SwiftUI 뷰 강제 업데이트
        hostingView?.rootView = OverlayContentView(appState: appState)
        hostingView?.needsDisplay = true
    }
}

/// 오버레이 윈도우의 SwiftUI 콘텐츠
struct OverlayContentView: View {
    @ObservedObject var appState: AppState

    var body: some View {
        ZStack {
            // 커서 하이라이트
            if appState.cursorHighlightEnabled {
                CursorHighlightView(appState: appState)
            }

            // 스포트라이트 모드
            if appState.currentMode == .spotlight {
                SpotlightView(appState: appState)
            }

            // 그리기 레이어
            DrawingCanvasView(appState: appState)

            // 줌 모드
            if appState.currentMode == .zoom {
                ZoomOverlayView(appState: appState)
            }

            // 모드 인디케이터는 각 뷰에서 자체 표시 (중복 방지)
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity)
    }
}

/// 모드 인디케이터 뷰
struct ModeIndicatorView: View {
    let mode: AppMode
    let cursorHighlightEnabled: Bool

    var body: some View {
        HStack(spacing: 10) {
            // 모드 아이콘
            modeIcon
                .font(.system(size: 22))

            // 모드 이름
            Text(modeName)
                .font(.system(size: 16, weight: .semibold))

            // 커서 하이라이트가 함께 활성화된 경우 표시
            if cursorHighlightEnabled && mode != .none {
                Text("+")
                    .font(.system(size: 14))
                Image(systemName: "cursor.rays")
                    .font(.system(size: 16))
            }
        }
        .foregroundColor(.white)
        .padding(.horizontal, 20)
        .padding(.vertical, 12)
        .background(
            Capsule()
                .fill(modeColor.opacity(0.85))
        )
        .shadow(color: .black.opacity(0.3), radius: 8)
    }

    private var modeName: String {
        switch mode {
        case .none:
            return cursorHighlightEnabled ? "커서 하이라이트" : ""
        case .drawing:
            return "그리기"
        case .zoom:
            return "줌"
        case .spotlight:
            return "스포트라이트"
        case .timer:
            return "타이머"
        }
    }

    @ViewBuilder
    private var modeIcon: some View {
        switch mode {
        case .none:
            Image(systemName: "cursor.rays")
        case .drawing:
            Image(systemName: "pencil.tip")
        case .zoom:
            Image(systemName: "plus.magnifyingglass")
        case .spotlight:
            Image(systemName: "light.max")
        case .timer:
            Image(systemName: "timer")
        }
    }

    private var modeColor: Color {
        switch mode {
        case .none:
            return .yellow.opacity(0.8)
        case .drawing:
            return .blue
        case .zoom:
            return .purple
        case .spotlight:
            return .orange
        case .timer:
            return .green
        }
    }
}
