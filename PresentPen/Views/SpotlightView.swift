import SwiftUI
import AppKit

/// 스포트라이트 뷰 - 특정 영역만 밝게 표시 (커서를 따라다님)
struct SpotlightView: View {
    @ObservedObject var appState: AppState

    var body: some View {
        GeometryReader { geometry in
            ZStack {
                if appState.spotlightZoomEnabled {
                    // 전체 화면 줌 (Presentify 스타일) - 화면 자체가 확대되어 커서를 따라감
                    FullScreenZoomView(appState: appState)
                } else {
                    // 기본 스포트라이트 (어두운 배경 + 구멍)
                    SpotlightShape(
                        center: appState.cursorPosition,
                        radius: appState.spotlightRadius
                    )
                    .fill(Color.black.opacity(0.75))
                }

                // 모드 인디케이터 (상단 중앙)
                VStack {
                    SpotlightModeIndicator()
                        .padding(.top, 40)
                    Spacer()
                }
            }
        }
        .allowsHitTesting(false)
    }
}

/// 스포트라이트 모드 인디케이터
struct SpotlightModeIndicator: View {
    var body: some View {
        HStack(spacing: 10) {
            Image(systemName: "light.max")
                .font(.system(size: 22))
            Text("스포트라이트")
                .font(.system(size: 16, weight: .semibold))
        }
        .foregroundColor(.white)
        .padding(.horizontal, 20)
        .padding(.vertical, 12)
        .background(
            Capsule()
                .fill(Color.orange.opacity(0.85))
        )
        .shadow(color: .black.opacity(0.3), radius: 8)
    }
}

/// 원형 돋보기 스포트라이트 뷰 (Presentify 스타일) - 커서 주변만 확대
struct FullScreenZoomView: NSViewRepresentable {
    @ObservedObject var appState: AppState

    func makeNSView(context: Context) -> MagnifierSpotlightNSView {
        let view = MagnifierSpotlightNSView()
        view.appState = appState
        view.startCapturing()
        return view
    }

    func updateNSView(_ nsView: MagnifierSpotlightNSView, context: Context) {
        nsView.appState = appState
    }

    static func dismantleNSView(_ nsView: MagnifierSpotlightNSView, coordinator: ()) {
        nsView.stopCapturing()
    }
}

/// 원형 돋보기 스포트라이트를 표시하는 NSView
class MagnifierSpotlightNSView: NSView {
    var appState: AppState?
    private var captureTimer: Timer?
    private var capturedImage: NSImage?
    private var scrollMonitor: Any?
    private var screenSize: CGSize = .zero

    override init(frame frameRect: NSRect) {
        super.init(frame: frameRect)
        wantsLayer = true
        layer?.backgroundColor = .clear

        if let screen = NSScreen.main {
            screenSize = screen.frame.size
        }
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    func startCapturing() {
        // 30fps로 캡처
        captureTimer = Timer.scheduledTimer(withTimeInterval: 1.0/30.0, repeats: true) { [weak self] _ in
            self?.captureAndDraw()
        }

        // 스크롤 이벤트 모니터 (배율 조정용)
        scrollMonitor = NSEvent.addLocalMonitorForEvents(matching: .scrollWheel) { [weak self] event in
            self?.handleScroll(event)
            return nil
        }
    }

    func stopCapturing() {
        captureTimer?.invalidate()
        captureTimer = nil

        if let monitor = scrollMonitor {
            NSEvent.removeMonitor(monitor)
            scrollMonitor = nil
        }
    }

    private func handleScroll(_ event: NSEvent) {
        guard let appState = appState else { return }

        let delta = event.scrollingDeltaY
        if abs(delta) > 0.1 {
            let newZoom = appState.spotlightZoomLevel + (delta > 0 ? 0.1 : -0.1)
            appState.spotlightZoomLevel = max(1.5, min(5.0, newZoom))
            print("🔍 스포트라이트 줌 배율: \(String(format: "%.1f", appState.spotlightZoomLevel))x")
        }
    }

    private func captureAndDraw() {
        guard let appState = appState,
              let screen = NSScreen.main else { return }

        let cursorPos = appState.cursorPosition
        let radius = appState.spotlightRadius
        let zoomLevel = appState.spotlightZoomLevel

        // 캡처할 영역 계산 (줌 레벨에 따라 더 작은 영역 캡처)
        let captureRadius = radius / zoomLevel

        // 커서 위치를 CGWindowListCreateImage 좌표로 변환 (top-left origin)
        let captureRect = CGRect(
            x: cursorPos.x - captureRadius,
            y: cursorPos.y - captureRadius,
            width: captureRadius * 2,
            height: captureRadius * 2
        )

        // 자신의 윈도우 아래만 캡처
        let windowID: CGWindowID
        if let windowNumber = self.window?.windowNumber, windowNumber > 0 {
            windowID = CGWindowID(windowNumber)
        } else {
            windowID = kCGNullWindowID
        }

        if let cgImage = CGWindowListCreateImage(
            captureRect,
            .optionOnScreenBelowWindow,
            windowID,
            [.bestResolution]
        ) {
            capturedImage = NSImage(cgImage: cgImage, size: NSSize(width: cgImage.width, height: cgImage.height))
            DispatchQueue.main.async {
                self.needsDisplay = true
            }
        }
    }

    override func draw(_ dirtyRect: NSRect) {
        guard let appState = appState else { return }

        let cursorPos = appState.cursorPosition
        let radius = appState.spotlightRadius
        let zoomLevel = appState.spotlightZoomLevel

        // NSView 좌표로 변환 (bottom-left origin)
        let cursorViewY = bounds.height - cursorPos.y

        // 1. 어두운 배경 그리기 (구멍 뚫린 형태)
        let darkPath = NSBezierPath(rect: bounds)
        let holePath = NSBezierPath(ovalIn: NSRect(
            x: cursorPos.x - radius,
            y: cursorViewY - radius,
            width: radius * 2,
            height: radius * 2
        ))
        darkPath.append(holePath)
        darkPath.windingRule = .evenOdd

        NSColor.black.withAlphaComponent(0.7).setFill()
        darkPath.fill()

        // 2. 캡처된 이미지를 원형 돋보기 안에 그리기
        if let image = capturedImage {
            NSGraphicsContext.saveGraphicsState()

            // 원형 클리핑
            let clipPath = NSBezierPath(ovalIn: NSRect(
                x: cursorPos.x - radius,
                y: cursorViewY - radius,
                width: radius * 2,
                height: radius * 2
            ))
            clipPath.addClip()

            // 확대된 이미지 그리기
            let destRect = NSRect(
                x: cursorPos.x - radius,
                y: cursorViewY - radius,
                width: radius * 2,
                height: radius * 2
            )
            image.draw(in: destRect, from: NSRect(origin: .zero, size: image.size), operation: .sourceOver, fraction: 1.0)

            NSGraphicsContext.restoreGraphicsState()

            // 3. 돋보기 테두리
            NSColor.white.withAlphaComponent(0.8).setStroke()
            clipPath.lineWidth = 3
            clipPath.stroke()

            // 4. 줌 레벨 표시 (돋보기 아래)
            let zoomText = String(format: "%.1fx", zoomLevel)
            let textAttributes: [NSAttributedString.Key: Any] = [
                .font: NSFont.systemFont(ofSize: 12, weight: .medium),
                .foregroundColor: NSColor.white
            ]
            let textSize = zoomText.size(withAttributes: textAttributes)
            let textRect = NSRect(
                x: cursorPos.x - textSize.width / 2,
                y: cursorViewY - radius - textSize.height - 12,
                width: textSize.width,
                height: textSize.height
            )

            // 배경 박스
            let bgRect = textRect.insetBy(dx: -6, dy: -3)
            NSColor.black.withAlphaComponent(0.7).setFill()
            NSBezierPath(roundedRect: bgRect, xRadius: 4, yRadius: 4).fill()

            zoomText.draw(in: textRect, withAttributes: textAttributes)
        }
    }
}

/// 스포트라이트 모양 (가운데 구멍 뚫린 사각형)
struct SpotlightShape: Shape {
    var center: CGPoint
    var radius: CGFloat

    var animatableData: AnimatablePair<CGPoint.AnimatableData, CGFloat> {
        get { AnimatablePair(center.animatableData, radius) }
        set {
            center.animatableData = newValue.first
            radius = newValue.second
        }
    }

    func path(in rect: CGRect) -> Path {
        var path = Path()

        // 전체 사각형
        path.addRect(rect)

        // 원형 구멍 (even-odd fill rule로 구멍이 됨)
        let circleRect = CGRect(
            x: center.x - radius,
            y: center.y - radius,
            width: radius * 2,
            height: radius * 2
        )
        path.addEllipse(in: circleRect)

        return path
    }
}

extension SpotlightShape {
    // Even-Odd fill rule 사용
    func fill(_ color: Color) -> some View {
        self.fill(color, style: FillStyle(eoFill: true))
    }
}
