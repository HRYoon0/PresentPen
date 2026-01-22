import SwiftUI
import AppKit
import os.log
import ScreenCaptureKit

private let logger = Logger(subsystem: "com.maczoomit", category: "zoom")

/// 줌 오버레이 뷰 (SwiftUI 호환용)
struct ZoomOverlayView: View {
    @ObservedObject var appState: AppState

    var body: some View {
        EmptyView()
    }
}

/// 줌 모드에서 사용하는 복합 오버레이 (그리기 + 커서 하이라이트 + 스포트라이트)
struct ZoomCombinedOverlay: View {
    @ObservedObject var appState: AppState
    var showDrawing: Bool
    var showCursorHighlight: Bool
    var showSpotlight: Bool
    var cursorPosition: CGPoint

    var body: some View {
        ZStack {
            // 마우스 이벤트 통과용 투명 배경
            Color.clear
                .allowsHitTesting(false)

            // 스포트라이트 (가장 먼저 - 배경 어둡게)
            if showSpotlight {
                ZoomSpotlightLayer(cursorPosition: appState.zoomCursorPosition, radius: appState.spotlightRadius)
                    .allowsHitTesting(false)
            }

            // 저장된 그리기 요소들 표시
            if showDrawing {
                ForEach(appState.drawings) { element in
                    DrawingElementView(element: element)
                }
                .allowsHitTesting(false)

                // 현재 그리는 중인 요소 (appState에서 경로 가져옴)
                if !appState.currentDrawingPath.isEmpty {
                    CurrentDrawingView(
                        tool: appState.currentTool,
                        points: appState.currentDrawingPath,
                        startPoint: appState.currentDrawingStartPoint,
                        color: appState.currentColor,
                        lineWidth: appState.currentLineWidth,
                        isHighlighter: appState.isHighlighter
                    )
                    .allowsHitTesting(false)
                }
            }

            // 커서 하이라이트 (가장 위에)
            if showCursorHighlight {
                ZoomCursorHighlightLayer(
                    cursorPosition: appState.zoomCursorPosition,
                    radius: appState.cursorHighlightRadius,
                    color: appState.cursorHighlightColor,
                    styleIndex: appState.cursorHighlightStyleIndex
                )
                .allowsHitTesting(false)
            }

            // 도움말 표시
            if showDrawing {
                VStack {
                    ZoomDrawingHelpView()
                        .allowsHitTesting(false)
                    Spacer()
                }
            }
        }
        .allowsHitTesting(false)  // 전체 오버레이가 마우스 이벤트를 통과시킴
    }
}

/// 줌 윈도우 내 커서 하이라이트 레이어 (Presentify 스타일)
struct ZoomCursorHighlightLayer: View {
    var cursorPosition: CGPoint
    var radius: CGFloat
    var color: Color
    var styleIndex: Int

    private var style: CursorHighlightStyle {
        CursorHighlightStyle.allCases[safe: styleIndex] ?? .halo
    }

    var body: some View {
        PresentifyCursorHighlight(
            position: cursorPosition,
            radius: radius,
            color: color,
            style: style,
            isClicked: false,
            clickScale: 1.0
        )
    }
}

/// 줌 윈도우 내 스포트라이트 레이어
struct ZoomSpotlightLayer: View {
    var cursorPosition: CGPoint
    var radius: CGFloat

    var body: some View {
        GeometryReader { geometry in
            SpotlightShape(center: cursorPosition, radius: radius)
                .fill(Color.black.opacity(0.75), style: FillStyle(eoFill: true))
        }
    }
}

/// 기존 그리기 오버레이 (호환성용)
struct ZoomDrawingOverlay: View {
    @ObservedObject var appState: AppState

    var body: some View {
        ZoomCombinedOverlay(
            appState: appState,
            showDrawing: true,
            showCursorHighlight: false,
            showSpotlight: false,
            cursorPosition: .zero
        )
    }
}

/// 줌 그리기 모드 도움말
struct ZoomDrawingHelpView: View {
    @State private var isVisible = true

    var body: some View {
        if isVisible {
            VStack(spacing: 4) {
                Text("🎨 줌+그리기 모드")
                    .font(.system(size: 14, weight: .bold))
                Text("색상: R(빨강) G(초록) B(파랑) Y(노랑)")
                Text("E: 지우기 | Ctrl+Z: 실행취소 | ESC: 그리기 종료")
            }
            .font(.system(size: 12))
            .foregroundColor(.white)
            .padding(8)
            .background(Color.black.opacity(0.8))
            .cornerRadius(8)
            .padding(.top, 50)
            .onAppear {
                DispatchQueue.main.asyncAfter(deadline: .now() + 5) {
                    withAnimation { isVisible = false }
                }
            }
        }
    }
}

/// 줌 그리기 입력 레이어
struct ZoomDrawingInputLayer: NSViewRepresentable {
    @Binding var currentPath: [CGPoint]
    @Binding var startPoint: CGPoint
    var appState: AppState

    func makeNSView(context: Context) -> ZoomDrawingInputView {
        let view = ZoomDrawingInputView()
        view.delegate = context.coordinator
        view.appState = appState
        return view
    }

    func updateNSView(_ nsView: ZoomDrawingInputView, context: Context) {
        nsView.appState = appState
    }

    func makeCoordinator() -> Coordinator {
        Coordinator(self)
    }

    class Coordinator: NSObject, ZoomDrawingInputViewDelegate {
        var parent: ZoomDrawingInputLayer

        init(_ parent: ZoomDrawingInputLayer) {
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

            let element = DrawingElement(
                tool: parent.appState.currentTool,
                points: parent.currentPath,
                color: parent.appState.currentColor,
                lineWidth: parent.appState.currentLineWidth
            )
            parent.appState.addDrawing(element)
            parent.currentPath = []
        }
    }
}

protocol ZoomDrawingInputViewDelegate: AnyObject {
    func drawingBegan(at point: CGPoint)
    func drawingMoved(to point: CGPoint)
    func drawingEnded(at point: CGPoint)
}

/// 줌 그리기 마우스 입력 뷰
class ZoomDrawingInputView: NSView {
    weak var delegate: ZoomDrawingInputViewDelegate?
    var appState: AppState?
    private var originalTool: DrawingTool?

    override func mouseDown(with event: NSEvent) {
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
        restoreOriginalTool()
    }

    private func updateToolForModifiers(_ flags: NSEvent.ModifierFlags) {
        guard let appState = appState else { return }
        originalTool = appState.currentTool

        if flags.contains(.control) && flags.contains(.shift) {
            appState.currentTool = .arrow
        } else if flags.contains(.control) {
            appState.currentTool = .rectangle
        } else if flags.contains(.shift) {
            appState.currentTool = .line
        }
    }

    private func restoreOriginalTool() {
        guard let appState = appState, let original = originalTool else { return }
        appState.currentTool = original
        originalTool = nil
    }

    override var acceptsFirstResponder: Bool { true }
}

// MARK: - 커스텀 윈도우 (키 윈도우 허용 + 이벤트 처리)
/// borderless 윈도우가 키 윈도우가 될 수 있도록 오버라이드
class ZoomWindow: NSWindow {
    weak var zoomController: ZoomContentViewController?

    override var canBecomeKey: Bool { true }
    override var canBecomeMain: Bool { true }

    override func sendEvent(_ event: NSEvent) {
        // 그리기 모드 확인
        let isDrawing = zoomController?.isDrawingEnabled ?? false

        // 스크롤 이벤트 직접 처리 (그리기 모드가 아닐 때만)
        if event.type == .scrollWheel && !isDrawing {
            zoomController?.handleScrollWheel(with: event)
            return
        }

        // 핀치 제스처 직접 처리 (그리기 모드가 아닐 때만)
        if event.type == .magnify && !isDrawing {
            zoomController?.handleMagnify(with: event)
            return
        }

        // 마우스 이벤트 처리
        if event.type == .leftMouseDown {
            zoomController?.handleMouseDown(with: event)
            if isDrawing { return }
        }

        if event.type == .leftMouseDragged {
            zoomController?.handleMouseDragged(with: event)
            if isDrawing { return }
        }

        if event.type == .leftMouseUp {
            zoomController?.handleMouseUp(with: event)
            if isDrawing { return }
        }

        super.sendEvent(event)
    }
}

// MARK: - 줌 서비스
/// 줌 기능을 관리하는 서비스
class ZoomService {
    private var zoomWindow: ZoomWindow?
    private var zoomViewController: ZoomContentViewController?
    private var isActive = false
    private var scrollMonitor: Any?
    private var mouseMonitor: Any?
    private var globalScrollMonitor: Any?
    private weak var appDelegate: AppDelegate?
    private var appState: AppState

    init(appState: AppState, appDelegate: AppDelegate? = nil) {
        self.appState = appState
        self.appDelegate = appDelegate
    }

    func startZoom() {
        logger.info("🔍 startZoom() 호출됨, isActive: \(self.isActive)")
        print("🔍 startZoom() 호출됨, isActive: \(isActive)")

        // 이미 활성화되어 있으면 강제로 정리 후 다시 시작
        if isActive {
            print("⚠️ 이미 활성화 상태 - 강제 정리")
            forceCleanup()
        }

        isActive = true
        print("🔍 줌 시작")

        // 동기적으로 실행 (메인 스레드 확인)
        if Thread.isMainThread {
            createZoomWindow()
            setupEventMonitors()
        } else {
            DispatchQueue.main.sync { [weak self] in
                self?.createZoomWindow()
                self?.setupEventMonitors()
            }
        }
    }

    private func setupEventMonitors() {
        // Global 스크롤 이벤트 모니터 (앱 포커스 여부와 관계없이 감지)
        globalScrollMonitor = NSEvent.addGlobalMonitorForEvents(matching: [.scrollWheel, .magnify]) { [weak self] event in
            guard let self = self, let controller = self.zoomViewController else { return }

            if event.type == .scrollWheel {
                print("📜 [Global] 스크롤 감지: deltaY=\(event.scrollingDeltaY)")
                DispatchQueue.main.async {
                    controller.handleScrollWheel(with: event)
                }
            }

            if event.type == .magnify {
                print("🤏 [Global] 핀치 감지: magnification=\(event.magnification)")
                DispatchQueue.main.async {
                    controller.handleMagnify(with: event)
                }
            }
        }

        // Local 스크롤 이벤트 모니터 (앱이 포커스일 때)
        scrollMonitor = NSEvent.addLocalMonitorForEvents(matching: [.scrollWheel, .magnify]) { [weak self] event in
            guard let self = self, let controller = self.zoomViewController else { return event }

            if event.type == .scrollWheel {
                print("📜 [Local] 스크롤 감지: deltaY=\(event.scrollingDeltaY)")
                controller.handleScrollWheel(with: event)
                return nil
            }

            if event.type == .magnify {
                print("🤏 [Local] 핀치 감지: magnification=\(event.magnification)")
                controller.handleMagnify(with: event)
                return nil
            }

            return event
        }

        // 마우스 드래그 모니터
        mouseMonitor = NSEvent.addLocalMonitorForEvents(matching: [.leftMouseDown, .leftMouseDragged]) { [weak self] event in
            guard let self = self, let controller = self.zoomViewController else { return event }

            if event.type == .leftMouseDown {
                controller.handleMouseDown(with: event)
            } else if event.type == .leftMouseDragged {
                controller.handleMouseDragged(with: event)
            }

            return nil
        }

        print("🎮 줌 이벤트 모니터 설정 완료 (Local + Global)")
    }

    private func cleanupEventMonitors() {
        if let monitor = globalScrollMonitor {
            NSEvent.removeMonitor(monitor)
            globalScrollMonitor = nil
        }
        if let monitor = scrollMonitor {
            NSEvent.removeMonitor(monitor)
            scrollMonitor = nil
        }
        if let monitor = mouseMonitor {
            NSEvent.removeMonitor(monitor)
            mouseMonitor = nil
        }
        print("🎮 줌 이벤트 모니터 정리 완료")
    }

    func endZoom() {
        print("🔍 endZoom() 호출됨, isActive: \(isActive)")
        guard isActive else { return }

        isActive = false
        print("🔍 줌 종료")

        cleanup()
    }

    /// 줌 모드에서 그리기 토글
    func toggleDrawing() {
        zoomViewController?.isDrawingEnabled.toggle()
        print("🎨 줌 그리기 토글: \(zoomViewController?.isDrawingEnabled ?? false)")
    }

    /// 줌 모드에서 커서 하이라이트 토글
    func toggleCursorHighlight() {
        zoomViewController?.isCursorHighlightEnabled.toggle()
        print("✨ 줌 커서 하이라이트 토글: \(zoomViewController?.isCursorHighlightEnabled ?? false)")
    }

    /// 줌 모드에서 스포트라이트 토글
    func toggleSpotlight() {
        zoomViewController?.isSpotlightEnabled.toggle()
        print("💡 줌 스포트라이트 토글: \(zoomViewController?.isSpotlightEnabled ?? false)")
    }

    /// 줌 모드 스포트라이트 활성화 상태 확인
    func isSpotlightEnabled() -> Bool {
        return zoomViewController?.isSpotlightEnabled ?? false
    }

    /// 강제 정리 (상태 불일치 시)
    private func forceCleanup() {
        isActive = false
        cleanup()
    }

    /// 리소스 정리
    private func cleanup() {
        cleanupEventMonitors()
        zoomViewController = nil
        zoomWindow?.close()
        zoomWindow = nil
    }

    private func createZoomWindow() {
        guard let screen = NSScreen.main else {
            print("❌ 메인 스크린 없음")
            isActive = false
            return
        }

        logger.info("📺 화면 캡처 시작 (ScreenCaptureKit)...")
        print("📺 화면 캡처 시작 (ScreenCaptureKit)...")

        // 캡처 전에 오버레이 윈도우들 숨기기
        appDelegate?.hideOverlaysForCapture()

        // ScreenCaptureKit으로 캡처 (비동기)
        Task { @MainActor in
            do {
                let cgImage = try await captureScreenWithSCK()

                // 캡처 완료 후 오버레이 다시 표시
                appDelegate?.showOverlaysAfterCapture()

                print("✅ ScreenCaptureKit 캡처 성공: \(cgImage.width)x\(cgImage.height)")

                let scaleFactor = screen.backingScaleFactor
                let nsImage = NSImage(
                    cgImage: cgImage,
                    size: NSSize(
                        width: CGFloat(cgImage.width) / scaleFactor,
                        height: CGFloat(cgImage.height) / scaleFactor
                    )
                )

                self.showZoomWindow(with: nsImage, screen: screen)

            } catch {
                print("❌ ScreenCaptureKit 캡처 실패: \(error.localizedDescription)")
                print("   화면 녹화 권한을 확인하세요")
                appDelegate?.showOverlaysAfterCapture()
                isActive = false
            }
        }
    }

    /// ScreenCaptureKit을 사용한 화면 캡처
    private func captureScreenWithSCK() async throws -> CGImage {
        // 캡처 가능한 콘텐츠 가져오기
        let content = try await SCShareableContent.excludingDesktopWindows(false, onScreenWindowsOnly: true)

        // 메인 디스플레이 찾기
        guard let display = content.displays.first else {
            throw NSError(domain: "ZoomService", code: 1, userInfo: [NSLocalizedDescriptionKey: "디스플레이를 찾을 수 없습니다"])
        }

        // 스트림 설정
        let config = SCStreamConfiguration()
        config.width = display.width * 2  // Retina 해상도
        config.height = display.height * 2
        config.pixelFormat = kCVPixelFormatType_32BGRA
        config.showsCursor = false

        // 필터 설정 (전체 화면)
        let filter = SCContentFilter(display: display, excludingWindows: [])

        // 스크린샷 캡처 (macOS 14.0+)
        if #available(macOS 14.0, *) {
            let image = try await SCScreenshotManager.captureImage(
                contentFilter: filter,
                configuration: config
            )
            return image
        } else {
            // macOS 13에서는 기존 방식 fallback
            return try await withCheckedThrowingContinuation { continuation in
                let displayID = CGMainDisplayID()
                let rect = CGRect(
                    x: 0, y: 0,
                    width: CGFloat(CGDisplayPixelsWide(displayID)),
                    height: CGFloat(CGDisplayPixelsHigh(displayID))
                )

                if let cgImage = CGWindowListCreateImage(rect, .optionOnScreenOnly, kCGNullWindowID, [.bestResolution]) {
                    continuation.resume(returning: cgImage)
                } else {
                    continuation.resume(throwing: NSError(domain: "ZoomService", code: 2, userInfo: [NSLocalizedDescriptionKey: "화면 캡처 실패"]))
                }
            }
        }
    }

    /// 줌 윈도우 표시
    private func showZoomWindow(with nsImage: NSImage, screen: NSScreen) {
        // 커스텀 윈도우 생성 (canBecomeKey = true)
        let window = ZoomWindow(
            contentRect: screen.frame,
            styleMask: [.borderless],
            backing: .buffered,
            defer: false
        )
        window.level = .screenSaver
        window.backgroundColor = .black
        window.isReleasedWhenClosed = false

        // 뷰 컨트롤러 생성 (화면 크기, appState 전달)
        let viewController = ZoomContentViewController(image: nsImage, screenFrame: screen.frame, appState: appState) { [weak self] in
            self?.endZoom()
        }

        // 윈도우에 컨트롤러 연결 (이벤트 전달용)
        window.zoomController = viewController
        window.contentViewController = viewController
        window.makeKeyAndOrderFront(nil)
        window.makeFirstResponder(viewController.view)

        // 앱 활성화
        NSApp.activate(ignoringOtherApps: true)

        self.zoomViewController = viewController
        self.zoomWindow = window

        logger.info("✅ 줌 윈도우 생성 완료, frame: \(screen.frame.debugDescription)")
        print("✅ 줌 윈도우 생성 완료")
    }
}

// MARK: - 줌 콘텐츠 뷰 컨트롤러 (직접 그리기 방식)
/// 줌 화면을 표시하고 상호작용을 처리하는 뷰 컨트롤러
class ZoomContentViewController: NSViewController {
    private let image: NSImage
    private let screenFrame: NSRect
    private let onExit: () -> Void
    private var zoomView: ZoomImageView!
    private var keyMonitor: Any?
    private var mouseMovedMonitor: Any?
    private var appState: AppState?
    private var drawingHostingView: NSHostingView<ZoomCombinedOverlay>?

    // 그리기 관련
    var isDrawingEnabled: Bool = false {
        didSet {
            updateOverlays()
        }
    }

    // 커서 하이라이트 관련
    var isCursorHighlightEnabled: Bool = false {
        didSet {
            updateOverlays()
        }
    }

    // 스포트라이트 관련
    var isSpotlightEnabled: Bool = false {
        didSet {
            updateOverlays()
            // 스포트라이트 켜면 커서 따라가기도 활성화
            if isSpotlightEnabled {
                isFollowCursorEnabled = true
            }
        }
    }

    // 커서 따라가기 (자동 패닝)
    var isFollowCursorEnabled: Bool = false

    // 줌 윈도우 내 커서 위치
    private var zoomCursorPosition: CGPoint = .zero

    init(image: NSImage, screenFrame: NSRect, appState: AppState? = nil, onExit: @escaping () -> Void) {
        self.image = image
        self.screenFrame = screenFrame
        self.appState = appState
        self.onExit = onExit
        super.init(nibName: nil, bundle: nil)
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    override func loadView() {
        // 화면 크기로 zoomView 초기화
        let viewFrame = NSRect(origin: .zero, size: screenFrame.size)
        zoomView = ZoomImageView(image: image, frame: viewFrame)
        // autoresizingMask로 윈도우 크기 변경에 대응
        zoomView.autoresizingMask = [.width, .height]
        self.view = zoomView
        print("📐 loadView() - zoomView frame: \(viewFrame), image size: \(image.size)")
    }

    override func viewDidLoad() {
        super.viewDidLoad()
        setupHelpLabel()
        print("📱 ZoomContentViewController viewDidLoad")
    }

    override func viewDidAppear() {
        super.viewDidAppear()
        setupEventMonitors()
        print("📱 ZoomContentViewController viewDidAppear, bounds: \(view.bounds)")
    }

    override func viewDidLayout() {
        super.viewDidLayout()
        // 레이아웃 완료 후 뷰 다시 그리기
        zoomView.needsDisplay = true
        print("📐 viewDidLayout - zoomView.bounds: \(zoomView.bounds)")
    }

    override func viewWillDisappear() {
        super.viewWillDisappear()
        cleanupEventMonitors()
        print("📱 ZoomContentViewController viewWillDisappear")
    }

    deinit {
        cleanupEventMonitors()
        print("🧹 ZoomContentViewController 해제됨")
    }

    private func setupHelpLabel() {
        // 모드 인디케이터 (캡슐 스타일)
        let container = NSView()
        container.wantsLayer = true
        container.layer?.backgroundColor = NSColor.purple.withAlphaComponent(0.85).cgColor
        container.layer?.cornerRadius = 20
        container.translatesAutoresizingMaskIntoConstraints = false

        // 아이콘 + 텍스트
        let label = NSTextField(labelWithString: "🔍 줌")
        label.font = NSFont.systemFont(ofSize: 16, weight: .semibold)
        label.textColor = .white
        label.backgroundColor = .clear
        label.isBordered = false
        label.drawsBackground = false
        label.alignment = .center
        label.translatesAutoresizingMaskIntoConstraints = false

        container.addSubview(label)
        view.addSubview(container)

        NSLayoutConstraint.activate([
            label.centerXAnchor.constraint(equalTo: container.centerXAnchor),
            label.centerYAnchor.constraint(equalTo: container.centerYAnchor),

            container.centerXAnchor.constraint(equalTo: view.centerXAnchor),
            container.topAnchor.constraint(equalTo: view.topAnchor, constant: 40),
            container.widthAnchor.constraint(equalToConstant: 100),
            container.heightAnchor.constraint(equalToConstant: 40)
        ])

        // 그림자 효과
        container.shadow = NSShadow()
        container.layer?.shadowColor = NSColor.black.cgColor
        container.layer?.shadowOpacity = 0.3
        container.layer?.shadowOffset = CGSize(width: 0, height: 2)
        container.layer?.shadowRadius = 8
    }

    private var globalMouseMonitor: Any?

    private func setupEventMonitors() {
        keyMonitor = NSEvent.addLocalMonitorForEvents(matching: .keyDown) { [weak self] event in
            guard let self = self else { return event }
            return self.handleKeyEvent(event)
        }

        // 마우스 이동 추적 (Local)
        mouseMovedMonitor = NSEvent.addLocalMonitorForEvents(matching: [.mouseMoved, .leftMouseDragged]) { [weak self] event in
            guard let self = self else { return event }
            self.handleMouseMoved(event)
            return event
        }

        // 마우스 이동 추적 (Global - 윈도우 포커스 없어도 감지)
        globalMouseMonitor = NSEvent.addGlobalMonitorForEvents(matching: [.mouseMoved, .leftMouseDragged]) { [weak self] event in
            guard let self = self else { return }
            DispatchQueue.main.async {
                self.handleMouseMoved(event)
            }
        }
        print("⌨️ 줌 키보드/마우스 모니터 설정됨 (Local + Global)")
    }

    private func cleanupEventMonitors() {
        if let monitor = keyMonitor {
            NSEvent.removeMonitor(monitor)
            keyMonitor = nil
        }
        if let monitor = mouseMovedMonitor {
            NSEvent.removeMonitor(monitor)
            mouseMovedMonitor = nil
        }
        if let monitor = globalMouseMonitor {
            NSEvent.removeMonitor(monitor)
            globalMouseMonitor = nil
        }
        print("⌨️ 줌 키보드/마우스 모니터 정리됨")
    }

    /// 마우스 이동 처리 (커서 하이라이트, 스포트라이트, 커서 따라가기)
    private func handleMouseMoved(_ event: NSEvent) {
        // 윈도우 좌표를 뷰 좌표로 변환
        guard let window = view.window else { return }
        let windowPoint = event.locationInWindow
        let screenPoint = window.convertPoint(toScreen: windowPoint)

        // 스크린 좌표를 뷰 좌표로 변환
        let viewPoint = view.convert(view.window?.convertPoint(fromScreen: screenPoint) ?? windowPoint, from: nil)
        let flippedPoint = CGPoint(x: viewPoint.x, y: view.bounds.height - viewPoint.y)

        zoomCursorPosition = flippedPoint
        appState?.zoomCursorPosition = flippedPoint

        // 커서 따라가기 (자동 패닝)
        if isFollowCursorEnabled {
            zoomView.followCursor(at: viewPoint)
        }

        // 커서 하이라이트나 스포트라이트가 활성화되어 있으면 오버레이 업데이트
        if isCursorHighlightEnabled || isSpotlightEnabled {
            updateOverlays()
        }
    }

    private func handleKeyEvent(_ event: NSEvent) -> NSEvent? {
        // Ctrl+1: 그리기 모드 토글
        if event.keyCode == 18 && event.modifierFlags.contains(.control) {
            print("🎨 줌 모드에서 그리기 토글")
            isDrawingEnabled.toggle()
            return nil
        }

        switch event.keyCode {
        case 53: // ESC
            if isDrawingEnabled {
                // 그리기 모드 먼저 종료
                print("🎨 그리기 모드 종료")
                isDrawingEnabled = false
                return nil
            }
            print("🔑 ESC 키 감지 - 줌 종료")
            onExit()
            return nil
        case 24: // + 키
            zoomView.zoomIn(at: zoomView.lastMouseLocation)
            return nil
        case 27: // - 키
            zoomView.zoomOut(at: zoomView.lastMouseLocation)
            return nil
        case 29: // 0 키
            zoomView.resetZoom()
            return nil
        default:
            // 그리기 모드에서 색상/도구 단축키 처리
            if isDrawingEnabled, let appState = appState {
                return handleDrawingKeyEvent(event, appState: appState)
            }
            return event
        }
    }

    /// 그리기 모드 키 이벤트 처리
    private func handleDrawingKeyEvent(_ event: NSEvent, appState: AppState) -> NSEvent? {
        let keyCode = event.keyCode
        let hasShift = event.modifierFlags.contains(.shift)

        switch keyCode {
        case 15: // R - 빨강
            appState.currentColor = .red
            appState.isHighlighter = hasShift
            return nil
        case 5:  // G - 초록
            appState.currentColor = .green
            appState.isHighlighter = hasShift
            return nil
        case 11: // B - 파랑
            appState.currentColor = .blue
            appState.isHighlighter = hasShift
            return nil
        case 16: // Y - 노랑
            appState.currentColor = .yellow
            appState.isHighlighter = hasShift
            return nil
        case 31: // O - 주황
            appState.currentColor = .orange
            appState.isHighlighter = hasShift
            return nil
        case 35: // P - 분홍
            appState.currentColor = .pink
            appState.isHighlighter = hasShift
            return nil
        case 14: // E - 전체 지우기
            appState.clearDrawings()
            return nil
        case 6:  // Z - 실행취소
            if event.modifierFlags.contains(.command) || event.modifierFlags.contains(.control) {
                appState.undo()
                return nil
            }
        default:
            break
        }
        return event
    }

    /// 오버레이 업데이트 (그리기, 커서 하이라이트, 스포트라이트)
    private func updateOverlays() {
        guard let appState = appState else { return }

        // 복합 오버레이 뷰 생성/업데이트
        let needsOverlay = isDrawingEnabled || isCursorHighlightEnabled || isSpotlightEnabled

        if needsOverlay {
            if drawingHostingView == nil {
                let overlay = ZoomCombinedOverlay(
                    appState: appState,
                    showDrawing: isDrawingEnabled,
                    showCursorHighlight: isCursorHighlightEnabled,
                    showSpotlight: isSpotlightEnabled,
                    cursorPosition: zoomCursorPosition
                )
                let hostingView = NSHostingView(rootView: overlay)
                hostingView.frame = view.bounds
                hostingView.autoresizingMask = [.width, .height]
                view.addSubview(hostingView)
                drawingHostingView = hostingView
                print("🎨 복합 오버레이 추가됨 (그리기:\(isDrawingEnabled), 커서:\(isCursorHighlightEnabled), 스포트라이트:\(isSpotlightEnabled))")
            } else {
                // 기존 오버레이 업데이트
                let overlay = ZoomCombinedOverlay(
                    appState: appState,
                    showDrawing: isDrawingEnabled,
                    showCursorHighlight: isCursorHighlightEnabled,
                    showSpotlight: isSpotlightEnabled,
                    cursorPosition: zoomCursorPosition
                )
                drawingHostingView?.rootView = overlay
            }
        } else {
            // 오버레이 제거
            drawingHostingView?.removeFromSuperview()
            drawingHostingView = nil
            print("🎨 오버레이 제거됨")
        }
    }

    /// 커서 위치 업데이트
    func updateCursorPosition(_ point: CGPoint) {
        zoomCursorPosition = point
        appState?.zoomCursorPosition = point
    }

    // 외부에서 호출하는 줌 메서드 (이벤트 모니터용)
    func handleScrollWheel(with event: NSEvent) {
        // 그리기 모드일 때는 스크롤 무시
        guard !isDrawingEnabled else { return }

        // Control + 스크롤: 커서 하이라이트 크기 조절
        if event.modifierFlags.contains(.control), isCursorHighlightEnabled, let appState = appState {
            let delta = event.scrollingDeltaY
            if abs(delta) > 0.5 {
                let newRadius = appState.cursorHighlightRadius + (delta > 0 ? 5 : -5)
                appState.cursorHighlightRadius = max(15, min(100, newRadius))
                print("✨ 커서 하이라이트 크기: \(Int(appState.cursorHighlightRadius))")
            }
            return
        }

        zoomView.handleScroll(event)
    }

    func handleMagnify(with event: NSEvent) {
        // 그리기 모드일 때는 핀치 무시
        guard !isDrawingEnabled else { return }
        zoomView.handleMagnify(event)
    }

    func handleMouseDown(with event: NSEvent) {
        if isDrawingEnabled {
            // 그리기 모드: 그리기 시작
            handleDrawingMouseDown(event)
        } else {
            zoomView.handleMouseDown(event)
        }
    }

    func handleMouseDragged(with event: NSEvent) {
        if isDrawingEnabled {
            // 그리기 모드: 그리기 진행
            handleDrawingMouseDragged(event)
        } else {
            zoomView.handleMouseDragged(event)
        }
    }

    func handleMouseUp(with event: NSEvent) {
        if isDrawingEnabled {
            // 그리기 모드: 그리기 완료
            handleDrawingMouseUp(event)
        }
    }

    // MARK: - 그리기 마우스 이벤트 처리
    private func handleDrawingMouseDown(_ event: NSEvent) {
        guard let appState = appState else { return }

        // 수정자 키에 따라 임시 도구 변경
        updateToolForModifiers(event.modifierFlags)

        let point = view.convert(event.locationInWindow, from: nil)
        let flippedPoint = CGPoint(x: point.x, y: view.bounds.height - point.y)

        // appState를 통해 현재 그리기 경로 업데이트 (SwiftUI 뷰에서 표시용)
        appState.currentDrawingStartPoint = flippedPoint
        appState.currentDrawingPath = [flippedPoint]
        print("🎨 그리기 시작: \(flippedPoint)")
    }

    private func handleDrawingMouseDragged(_ event: NSEvent) {
        guard let appState = appState else { return }

        let point = view.convert(event.locationInWindow, from: nil)
        let flippedPoint = CGPoint(x: point.x, y: view.bounds.height - point.y)

        // appState를 통해 현재 그리기 경로 업데이트
        appState.currentDrawingPath.append(flippedPoint)
    }

    private func handleDrawingMouseUp(_ event: NSEvent) {
        guard let appState = appState, !appState.currentDrawingPath.isEmpty else { return }

        let point = view.convert(event.locationInWindow, from: nil)
        let flippedPoint = CGPoint(x: point.x, y: view.bounds.height - point.y)
        appState.currentDrawingPath.append(flippedPoint)

        // 그리기 요소 저장
        let element = DrawingElement(
            tool: appState.currentTool,
            points: appState.currentDrawingPath,
            color: appState.currentColor,
            lineWidth: appState.currentLineWidth
        )
        appState.addDrawing(element)
        print("🎨 그리기 완료: \(appState.currentDrawingPath.count)개 점")

        // 경로 초기화 및 도구 복원
        appState.currentDrawingPath = []
        restoreOriginalTool()
    }

    private var originalTool: DrawingTool?

    private func updateToolForModifiers(_ flags: NSEvent.ModifierFlags) {
        guard let appState = appState else { return }
        originalTool = appState.currentTool

        if flags.contains(.control) && flags.contains(.shift) {
            appState.currentTool = .arrow
        } else if flags.contains(.control) {
            appState.currentTool = .rectangle
        } else if flags.contains(.shift) {
            appState.currentTool = .line
        }
    }

    private func restoreOriginalTool() {
        guard let appState = appState, let original = originalTool else { return }
        appState.currentTool = original
        originalTool = nil
    }
}

// MARK: - 줌 이미지 뷰 (직접 그리기)
/// 이미지를 직접 그려서 줌/이동을 처리하는 커스텀 뷰
class ZoomImageView: NSView {
    private let image: NSImage
    private var scale: CGFloat = 1.0
    private var offset: CGPoint = .zero
    private var isDragging = false
    private var dragStart: CGPoint = .zero
    private var offsetAtDragStart: CGPoint = .zero

    var lastMouseLocation: CGPoint = .zero

    // 부드러운 커서 따라가기를 위한 애니메이션 변수
    private var targetOffset: CGPoint = .zero
    private var animationTimer: Timer?
    private var isAnimating: Bool = false

    init(image: NSImage, frame: NSRect = .zero) {
        self.image = image
        super.init(frame: frame)
        wantsLayer = true
        layer?.backgroundColor = NSColor.black.cgColor
        print("🖼️ ZoomImageView 생성 - frame: \(frame), image: \(image.size)")
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    override func draw(_ dirtyRect: NSRect) {
        // bounds가 유효한지 확인
        guard bounds.width > 0 && bounds.height > 0 else {
            logger.warning("⚠️ draw() 호출됨 - bounds가 아직 0임")
            return
        }

        // NSGraphicsContext 사용
        guard NSGraphicsContext.current != nil else {
            logger.error("❌ NSGraphicsContext.current가 nil")
            return
        }

        // 배경 - 검은색
        NSColor.black.setFill()
        bounds.fill()

        // 이미지 그리기
        let viewSize = bounds.size

        // 확대된 이미지 크기
        let scaledWidth = viewSize.width * scale
        let scaledHeight = viewSize.height * scale

        // 이미지 위치 (중앙 기준 + offset)
        let x = (viewSize.width - scaledWidth) / 2 + offset.x
        let y = (viewSize.height - scaledHeight) / 2 + offset.y

        let destRect = NSRect(x: x, y: y, width: scaledWidth, height: scaledHeight)

        // 이미지 그리기
        image.draw(in: destRect,
                   from: NSRect(origin: .zero, size: image.size),
                   operation: .sourceOver,
                   fraction: 1.0,
                   respectFlipped: true,
                   hints: [.interpolation: NSImageInterpolation.high])

        // 디버그: 빨간 테두리 그리기 (뷰가 그려지는지 확인용)
        NSColor.red.setStroke()
        let borderPath = NSBezierPath(rect: bounds.insetBy(dx: 5, dy: 5))
        borderPath.lineWidth = 10
        borderPath.stroke()

        // 디버그 로그 (최초 1회만)
        if !hasLoggedFirstDraw {
            logger.info("🎨 draw() 성공 - bounds: \(self.bounds.debugDescription), scale: \(self.scale)")
            hasLoggedFirstDraw = true
        }
    }

    private var hasLoggedFirstDraw = false

    // 마우스 위치 추적
    override func mouseMoved(with event: NSEvent) {
        lastMouseLocation = convert(event.locationInWindow, from: nil)
    }

    override func updateTrackingAreas() {
        super.updateTrackingAreas()
        // 기존 tracking area 제거
        for area in trackingAreas {
            removeTrackingArea(area)
        }
        // 새 tracking area 추가
        let options: NSTrackingArea.Options = [.mouseMoved, .activeAlways, .inVisibleRect]
        let trackingArea = NSTrackingArea(rect: bounds, options: options, owner: self, userInfo: nil)
        addTrackingArea(trackingArea)
    }

    func handleScroll(_ event: NSEvent) {
        let delta = event.scrollingDeltaY
        if abs(delta) > 0.1 {
            let mouseLocation = convert(event.locationInWindow, from: nil)
            lastMouseLocation = mouseLocation

            if delta > 0 {
                zoomIn(at: mouseLocation)
            } else {
                zoomOut(at: mouseLocation)
            }
        }
    }

    func handleMagnify(_ event: NSEvent) {
        let mouseLocation = convert(event.locationInWindow, from: nil)
        lastMouseLocation = mouseLocation

        let oldScale = scale
        scale = max(1.0, min(10.0, scale * (1 + event.magnification)))

        // 마우스 위치 기준으로 줌
        adjustOffsetForZoom(at: mouseLocation, oldScale: oldScale, newScale: scale)
        needsDisplay = true
        print("🔍 핀치 줌: \(String(format: "%.1f", scale))x")
    }

    func handleMouseDown(_ event: NSEvent) {
        isDragging = true
        dragStart = convert(event.locationInWindow, from: nil)
        offsetAtDragStart = offset
    }

    func handleMouseDragged(_ event: NSEvent) {
        guard isDragging else { return }
        let currentPoint = convert(event.locationInWindow, from: nil)
        offset = CGPoint(
            x: offsetAtDragStart.x + (currentPoint.x - dragStart.x),
            y: offsetAtDragStart.y + (currentPoint.y - dragStart.y)
        )
        needsDisplay = true
    }

    override func mouseUp(with event: NSEvent) {
        isDragging = false
    }

    func zoomIn(at point: CGPoint) {
        let oldScale = scale
        scale = min(10.0, scale * 1.15)
        adjustOffsetForZoom(at: point, oldScale: oldScale, newScale: scale)
        needsDisplay = true
        print("🔍 줌 인: \(String(format: "%.1f", scale))x")
    }

    func zoomOut(at point: CGPoint) {
        let oldScale = scale
        scale = max(1.0, scale / 1.15)
        adjustOffsetForZoom(at: point, oldScale: oldScale, newScale: scale)
        needsDisplay = true
        print("🔍 줌 아웃: \(String(format: "%.1f", scale))x")
    }

    func resetZoom() {
        scale = 1.0
        offset = .zero
        needsDisplay = true
        print("🔍 줌 리셋")
    }

    /// 커서를 따라 자동 패닝 (마우스가 가리키는 원본 위치가 화면 중앙에 오도록)
    func followCursor(at point: CGPoint) {
        // 스케일이 1.0이면 패닝 불필요
        guard scale > 1.0 else { return }

        let centerX = bounds.width / 2
        let centerY = bounds.height / 2

        // 마우스 위치를 원본 이미지 좌표로 변환
        // 현재 이미지 시작 위치
        let imageOriginX = (bounds.width - bounds.width * scale) / 2 + offset.x
        let imageOriginY = (bounds.height - bounds.height * scale) / 2 + offset.y

        // 마우스가 가리키는 원본 이미지의 위치 (0 ~ bounds.width/height)
        let originalX = (point.x - imageOriginX) / scale
        let originalY = (point.y - imageOriginY) / scale

        // 이 원본 위치가 화면 중앙에 오도록 offset 계산
        let newTargetX = centerX - (bounds.width - bounds.width * scale) / 2 - originalX * scale
        let newTargetY = centerY - (bounds.height - bounds.height * scale) / 2 - originalY * scale

        // 경계 제한 적용
        let maxOffsetX = bounds.width * (scale - 1) / 2
        let maxOffsetY = bounds.height * (scale - 1) / 2
        targetOffset.x = max(-maxOffsetX, min(maxOffsetX, newTargetX))
        targetOffset.y = max(-maxOffsetY, min(maxOffsetY, newTargetY))

        // 애니메이션 시작
        startFollowAnimation()
    }

    /// 애니메이션 타이머 시작
    private func startFollowAnimation() {
        guard !isAnimating else { return }
        isAnimating = true

        // 60fps 타이머로 부드러운 애니메이션
        animationTimer = Timer.scheduledTimer(withTimeInterval: 1.0/60.0, repeats: true) { [weak self] _ in
            self?.updateFollowAnimation()
        }
    }

    /// 애니메이션 프레임 업데이트
    private func updateFollowAnimation() {
        // 부드러운 이동 (lerp) - 값이 작을수록 느리고 부드러움
        let smoothFactor: CGFloat = 0.08

        let dx = targetOffset.x - offset.x
        let dy = targetOffset.y - offset.y

        // 목표에 충분히 가까우면 애니메이션 중지
        if abs(dx) < 0.5 && abs(dy) < 0.5 {
            offset = targetOffset
            stopFollowAnimation()
            needsDisplay = true
            return
        }

        offset.x += dx * smoothFactor
        offset.y += dy * smoothFactor

        needsDisplay = true
    }

    /// 애니메이션 타이머 중지
    func stopFollowAnimation() {
        animationTimer?.invalidate()
        animationTimer = nil
        isAnimating = false
    }

    /// 마우스 위치를 기준으로 줌할 때 offset 조정
    private func adjustOffsetForZoom(at point: CGPoint, oldScale: CGFloat, newScale: CGFloat) {
        // 마우스 위치를 뷰 중심 기준으로 변환
        let centerX = bounds.width / 2
        let centerY = bounds.height / 2

        // 마우스와 중심 사이의 거리
        let dx = point.x - centerX - offset.x
        let dy = point.y - centerY - offset.y

        // 스케일 변화에 따른 offset 조정
        let scaleRatio = newScale / oldScale
        offset.x -= dx * (scaleRatio - 1)
        offset.y -= dy * (scaleRatio - 1)
    }
}
