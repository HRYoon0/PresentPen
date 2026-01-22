import SwiftUI
import AppKit
import os.log

private let appLogger = Logger(subsystem: "com.maczoomit", category: "app")

class AppDelegate: NSObject, NSApplicationDelegate {
    // 전역 상태 관리
    var appState = AppState()

    // 상태바 아이템 (메뉴바 아이콘)
    private var statusItem: NSStatusItem?

    // 오버레이 윈도우들
    private var overlayWindows: [NSWindow] = []

    // 단축키 매니저
    private var hotkeyManager: HotkeyManager?

    // 커서 추적 타이머
    private var cursorTimer: Timer?

    // 줌 서비스
    private var zoomService: ZoomService?

    // 타이머 서비스
    private var timerService: TimerService?

    // 그리기 모드 키 이벤트 모니터
    private var drawingKeyMonitor: Any?

    func applicationDidFinishLaunching(_ notification: Notification) {
        print("🚀 PresentPen 시작")

        // 메뉴바 아이콘 설정
        setupStatusBar()

        // 오버레이 윈도우 생성 (각 화면마다)
        setupOverlayWindows()

        // 글로벌 단축키 설정
        setupHotkeys()

        // 줌 서비스 초기화 (appDelegate 전달하여 오버레이 숨기기/표시 가능하도록)
        zoomService = ZoomService(appState: appState, appDelegate: self)

        // 타이머 서비스 초기화
        timerService = TimerService(appState: appState)

        // 커서 추적 시작
        startCursorTracking()

        // 권한 확인
        checkPermissions()

        print("✅ PresentPen 초기화 완료")
    }

    // MARK: - 커서 추적
    private func startCursorTracking() {
        // 60fps로 커서 위치 업데이트
        cursorTimer = Timer.scheduledTimer(withTimeInterval: 1.0/60.0, repeats: true) { [weak self] _ in
            guard let self = self else { return }
            let mouseLocation = NSEvent.mouseLocation

            // 메인 스크린 기준 좌표 변환
            if let screen = NSScreen.main {
                let flippedY = mouseLocation.y
                self.appState.cursorPosition = CGPoint(x: mouseLocation.x, y: screen.frame.height - flippedY)
            }
        }
    }

    // MARK: - 메뉴바 설정
    private func setupStatusBar() {
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)

        if let button = statusItem?.button {
            // SF Symbol 사용 (macOS 11+)
            button.image = NSImage(systemSymbolName: "pencil.and.outline", accessibilityDescription: "PresentPen")
        }

        // 메뉴 구성
        let menu = NSMenu()

        menu.addItem(NSMenuItem(title: "그리기 모드 (Ctrl+1)", action: #selector(toggleDrawingMode), keyEquivalent: ""))
        menu.addItem(NSMenuItem(title: "줌 모드 (Ctrl+2)", action: #selector(toggleZoomMode), keyEquivalent: ""))
        menu.addItem(NSMenuItem(title: "커서 하이라이트 (Ctrl+3)", action: #selector(toggleCursorHighlight), keyEquivalent: ""))
        menu.addItem(NSMenuItem(title: "스포트라이트 (Ctrl+4)", action: #selector(toggleSpotlight), keyEquivalent: ""))

        menu.addItem(NSMenuItem.separator())

        menu.addItem(NSMenuItem(title: "전체 지우기", action: #selector(clearAll), keyEquivalent: ""))

        menu.addItem(NSMenuItem.separator())

        menu.addItem(NSMenuItem(title: "사용법", action: #selector(openHelp), keyEquivalent: "?"))
        menu.addItem(NSMenuItem(title: "설정...", action: #selector(openSettings), keyEquivalent: ","))
        menu.addItem(NSMenuItem(title: "종료", action: #selector(quitApp), keyEquivalent: "q"))

        statusItem?.menu = menu
    }

    // MARK: - 오버레이 윈도우 설정
    private func setupOverlayWindows() {
        // 모든 화면에 대해 오버레이 윈도우 생성
        for screen in NSScreen.screens {
            let window = OverlayWindow(screen: screen, appState: appState)
            overlayWindows.append(window)
        }
    }

    // MARK: - 단축키 설정
    private func setupHotkeys() {
        hotkeyManager = HotkeyManager(appState: appState, appDelegate: self)
        hotkeyManager?.register()
    }

    // MARK: - 권한 확인
    private func checkPermissions() {
        // 접근성 권한 확인2 (글로벌 단축키에 필요)
        let trusted = AXIsProcessTrusted()
        if !trusted {
            // 접근성 권한 요청 다이얼로그 표시
            let options: NSDictionary = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true]
            AXIsProcessTrustedWithOptions(options)
        }

        // 화면 녹화 권한 확인 (줌 기능에 필요)
        checkScreenCapturePermission()
    }

    /// 화면 녹화 권한 확인
    private func checkScreenCapturePermission() {
        // CGWindowListCreateImage를 테스트로 호출하여 권한 확인
        let testImage = CGWindowListCreateImage(
            CGRect(x: 0, y: 0, width: 1, height: 1),
            .optionOnScreenOnly,
            kCGNullWindowID,
            .bestResolution
        )

        if testImage == nil {
            print("⚠️ 화면 녹화 권한이 필요합니다")
            // 시스템 환경설정 열기
            DispatchQueue.main.async {
                let alert = NSAlert()
                alert.messageText = "화면 녹화 권한 필요"
                alert.informativeText = "줌 기능을 사용하려면 시스템 환경설정 > 개인정보 보호 및 보안 > 화면 녹화에서 PresentPen을 허용해주세요."
                alert.alertStyle = .warning
                alert.addButton(withTitle: "설정 열기")
                alert.addButton(withTitle: "나중에")

                if alert.runModal() == .alertFirstButtonReturn {
                    if let url = URL(string: "x-apple.systempreferences:com.apple.preference.security?Privacy_ScreenCapture") {
                        NSWorkspace.shared.open(url)
                    }
                }
            }
        } else {
            print("✅ 화면 녹화 권한 확인됨")
        }
    }

    // MARK: - 메뉴 액션
    @objc func toggleDrawingMode() {
        appState.toggleMode(.drawing)
        updateOverlays()
    }

    @objc func toggleZoomMode() {
        appState.toggleMode(.zoom)
        updateOverlays()
    }

    @objc func toggleCursorHighlight() {
        appState.cursorHighlightEnabled.toggle()
        updateOverlays()
    }

    @objc func toggleSpotlight() {
        appState.toggleMode(.spotlight)
        updateOverlays()
    }

    @objc func clearAll() {
        appState.clearDrawings()
        updateOverlays()
    }

    @objc func openHelp() {
        HelpWindowController.shared.showHelp()
    }

    @objc func openSettings() {
        NSApp.sendAction(Selector(("showSettingsWindow:")), to: nil, from: nil)
        NSApp.activate(ignoringOtherApps: true)
    }

    @objc func quitApp() {
        NSApp.terminate(nil)
    }

    // MARK: - 줌 모드에서 그리기 토글
    func toggleZoomDrawing() {
        zoomService?.toggleDrawing()
    }

    // MARK: - 줌 모드에서 커서 하이라이트 토글
    func toggleZoomCursorHighlight() {
        zoomService?.toggleCursorHighlight()
    }

    // MARK: - 줌 모드에서 스포트라이트 토글
    func toggleZoomSpotlight() {
        zoomService?.toggleSpotlight()
    }

    /// 줌 모드 스포트라이트 활성화 상태 확인
    func isZoomSpotlightEnabled() -> Bool {
        return zoomService?.isSpotlightEnabled() ?? false
    }

    // MARK: - 오버레이 윈도우 숨기기/표시 (화면 캡처용)
    func hideOverlaysForCapture() {
        for window in overlayWindows {
            window.orderOut(nil)
        }
        print("👁️ 오버레이 윈도우 숨김 (캡처용)")
    }

    func showOverlaysAfterCapture() {
        for window in overlayWindows {
            window.orderFrontRegardless()
        }
        print("👁️ 오버레이 윈도우 다시 표시")
    }

    // MARK: - 오버레이 업데이트
    func updateOverlays() {
        let mode = appState.currentMode
        appLogger.info("🔄 오버레이 업데이트 - 모드: \(String(describing: mode))")
        print("🔄 오버레이 업데이트")
        print("   → 현재 모드: \(mode)")
        print("   → 커서하이라이트: \(appState.cursorHighlightEnabled)")

        // 줌 모드 처리
        if mode == .zoom {
            appLogger.info("   → 줌 모드 활성화 → startZoom() 호출, zoomService nil? \(self.zoomService == nil)")
            print("   → 줌 모드 활성화 → startZoom() 호출")
            zoomService?.startZoom()
        } else {
            print("   → 줌 모드 비활성화 → endZoom() 호출")
            zoomService?.endZoom()
        }

        // 타이머 모드 처리
        if appState.currentMode == .timer {
            timerService?.startTimer()
        } else {
            timerService?.endTimer()
        }

        // 그리기 모드 키 모니터 처리
        if appState.currentMode == .drawing {
            setupDrawingKeyMonitor()
        } else {
            removeDrawingKeyMonitor()
            // 그리기 모드 종료 시 그림 및 배경 초기화
            if !appState.drawings.isEmpty || appState.backgroundMode != .transparent {
                appState.clearDrawings()
                appState.backgroundMode = .transparent
                print("🗑️ 그리기 모드 종료 - 그림 초기화")
            }
        }

        for window in overlayWindows {
            if let overlayWindow = window as? OverlayWindow {
                overlayWindow.updateContent()
            }
        }
    }

    // MARK: - 그리기 모드 키 모니터
    private func setupDrawingKeyMonitor() {
        guard drawingKeyMonitor == nil else { return }

        drawingKeyMonitor = NSEvent.addLocalMonitorForEvents(matching: .keyDown) { [weak self] event in
            guard let self = self else { return event }
            guard self.appState.currentMode == .drawing else { return event }

            if self.handleDrawingKeyEvent(event) {
                return nil
            }
            return event
        }
        print("⌨️ 그리기 키 모니터 설정됨")
    }

    private func removeDrawingKeyMonitor() {
        if let monitor = drawingKeyMonitor {
            NSEvent.removeMonitor(monitor)
            drawingKeyMonitor = nil
            print("⌨️ 그리기 키 모니터 제거됨")
        }
    }

    private func handleDrawingKeyEvent(_ event: NSEvent) -> Bool {
        let keyCode = event.keyCode
        let hasShift = event.modifierFlags.contains(.shift)

        switch keyCode {
        // 색상 단축키 (keyCode는 물리적 키 위치 - 한글/영어 무관)
        case 15: // R
            appState.currentColor = .red
            appState.isHighlighter = hasShift
            print("🎨 색상: 빨강")
            return true

        case 5:  // G
            appState.currentColor = .green
            appState.isHighlighter = hasShift
            print("🎨 색상: 초록")
            return true

        case 11: // B
            appState.currentColor = .blue
            appState.isHighlighter = hasShift
            print("🎨 색상: 파랑")
            return true

        case 16: // Y
            appState.currentColor = .yellow
            appState.isHighlighter = hasShift
            print("🎨 색상: 노랑")
            return true

        case 31: // O
            appState.currentColor = .orange
            appState.isHighlighter = hasShift
            print("🎨 색상: 주황")
            return true

        case 35: // P
            appState.currentColor = .pink
            appState.isHighlighter = hasShift
            print("🎨 색상: 분홍")
            return true

        // 배경 모드
        case 13: // W
            appState.backgroundMode = appState.backgroundMode == .whiteboard ? .transparent : .whiteboard
            print("📋 배경: \(appState.backgroundMode)")
            return true

        case 40: // K
            appState.backgroundMode = appState.backgroundMode == .blackboard ? .transparent : .blackboard
            print("📋 배경: \(appState.backgroundMode)")
            return true

        // 전체 지우기
        case 14: // E
            appState.clearDrawings()
            appState.backgroundMode = .transparent
            print("🗑️ 전체 지우기")
            return true

        // 실행취소 (Cmd+Z 또는 Ctrl+Z)
        case 6:  // Z
            if event.modifierFlags.contains(.command) || event.modifierFlags.contains(.control) {
                appState.undo()
                print("↩️ 실행취소")
                return true
            }
            return false

        // 도구 변경
        case 48: // Tab - 원
            appState.currentTool = .circle
            print("🔧 도구: 원")
            return true

        default:
            return false
        }
    }

    // MARK: - 타이머 액션
    @objc func toggleTimerMode() {
        appState.toggleMode(.timer)
        updateOverlays()
    }
}
