import SwiftUI
import AppKit

/// 타이머 서비스
class TimerService: ObservableObject {
    private var appState: AppState
    private var timerWindow: NSWindow?
    private var timerViewController: TimerViewController?
    private var isRunning = false

    init(appState: AppState) {
        self.appState = appState
    }

    /// 타이머 시작
    func startTimer() {
        guard !isRunning else { return }
        isRunning = true
        print("⏱️ 타이머 모드 시작")

        showTimerWindow()
    }

    /// 타이머 종료
    func endTimer() {
        guard isRunning else { return }
        isRunning = false
        print("⏱️ 타이머 모드 종료")

        timerViewController?.cleanup()
        timerWindow?.close()
        timerWindow = nil
        timerViewController = nil
    }

    /// 타이머 윈도우 표시
    private func showTimerWindow() {
        guard let screen = NSScreen.main else { return }

        let window = NSWindow(
            contentRect: screen.frame,
            styleMask: [.borderless],
            backing: .buffered,
            defer: false
        )

        window.level = .screenSaver
        window.backgroundColor = NSColor.black.withAlphaComponent(0.95)
        window.isOpaque = true
        window.hasShadow = false
        window.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary]

        let viewController = TimerViewController(
            screenSize: screen.frame.size,
            onExit: { [weak self] in
                self?.endTimer()
            }
        )

        window.contentViewController = viewController
        window.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps: true)

        self.timerWindow = window
        self.timerViewController = viewController
    }
}

/// 타이머 뷰 컨트롤러
class TimerViewController: NSViewController {
    private let screenSize: NSSize
    private let onExit: () -> Void

    private var timeRemaining: TimeInterval = 5 * 60  // 기본 5분
    private var timer: Timer?
    private var timeLabel: NSTextField!
    private var infoLabel: NSTextField!
    private var isPaused = false
    private var isStarted = false  // 타이머 시작 여부

    private var localMonitor: Any?

    init(screenSize: NSSize, onExit: @escaping () -> Void) {
        self.screenSize = screenSize
        self.onExit = onExit
        super.init(nibName: nil, bundle: nil)
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) 미구현")
    }

    override func loadView() {
        let containerView = NSView(frame: NSRect(origin: .zero, size: screenSize))
        containerView.wantsLayer = true

        // 시간 라벨
        timeLabel = NSTextField(labelWithString: formatTime(timeRemaining))
        timeLabel.font = NSFont.monospacedDigitSystemFont(ofSize: 200, weight: .bold)
        timeLabel.textColor = .white
        timeLabel.alignment = .center
        timeLabel.isBordered = false
        timeLabel.isEditable = false
        timeLabel.drawsBackground = false
        timeLabel.sizeToFit()
        timeLabel.frame = NSRect(
            x: (screenSize.width - timeLabel.frame.width) / 2,
            y: (screenSize.height - timeLabel.frame.height) / 2,
            width: timeLabel.frame.width,
            height: timeLabel.frame.height
        )
        containerView.addSubview(timeLabel)

        // 프리셋 안내 라벨
        let presetLabel = NSTextField(labelWithString: "[1] 1분   [3] 3분   [5] 5분   [0] 10분")
        presetLabel.font = NSFont.monospacedSystemFont(ofSize: 18, weight: .medium)
        presetLabel.textColor = NSColor.white.withAlphaComponent(0.8)
        presetLabel.alignment = .center
        presetLabel.isBordered = false
        presetLabel.isEditable = false
        presetLabel.drawsBackground = false
        presetLabel.sizeToFit()
        presetLabel.frame = NSRect(
            x: (screenSize.width - presetLabel.frame.width) / 2,
            y: screenSize.height / 2 - 160,
            width: presetLabel.frame.width,
            height: presetLabel.frame.height
        )
        containerView.addSubview(presetLabel)

        // 안내 라벨
        infoLabel = NSTextField(labelWithString: "스크롤/↑↓: 시간 조절 | Space/Enter: 시작 | ESC: 종료")
        infoLabel.font = NSFont.systemFont(ofSize: 16, weight: .medium)
        infoLabel.textColor = NSColor.white.withAlphaComponent(0.6)
        infoLabel.alignment = .center
        infoLabel.isBordered = false
        infoLabel.isEditable = false
        infoLabel.drawsBackground = false
        infoLabel.sizeToFit()
        infoLabel.frame = NSRect(
            x: (screenSize.width - infoLabel.frame.width) / 2,
            y: screenSize.height / 2 - 210,
            width: infoLabel.frame.width,
            height: infoLabel.frame.height
        )
        containerView.addSubview(infoLabel)

        // 모드 인디케이터 (상단 중앙)
        let indicator = NSView()
        indicator.wantsLayer = true
        indicator.layer?.backgroundColor = NSColor.systemGreen.withAlphaComponent(0.85).cgColor
        indicator.layer?.cornerRadius = 20

        let indicatorLabel = NSTextField(labelWithString: "⏱️ 타이머")
        indicatorLabel.font = NSFont.systemFont(ofSize: 16, weight: .semibold)
        indicatorLabel.textColor = .white
        indicatorLabel.backgroundColor = .clear
        indicatorLabel.isBordered = false
        indicatorLabel.drawsBackground = false
        indicatorLabel.sizeToFit()

        let indicatorWidth: CGFloat = indicatorLabel.frame.width + 40
        let indicatorHeight: CGFloat = 40
        indicator.frame = NSRect(
            x: (screenSize.width - indicatorWidth) / 2,
            y: screenSize.height - 80,
            width: indicatorWidth,
            height: indicatorHeight
        )
        indicatorLabel.frame = NSRect(
            x: 20,
            y: (indicatorHeight - indicatorLabel.frame.height) / 2,
            width: indicatorLabel.frame.width,
            height: indicatorLabel.frame.height
        )
        indicator.addSubview(indicatorLabel)
        containerView.addSubview(indicator)

        self.view = containerView

        // 이벤트 모니터만 설정 (타이머는 Space/Enter로 시작)
        setupEventMonitor()
    }

    /// 카운트다운 시작
    private func startCountdown() {
        timer = Timer.scheduledTimer(withTimeInterval: 1.0, repeats: true) { [weak self] _ in
            self?.updateTimer()
        }
        RunLoop.main.add(timer!, forMode: .common)
    }

    /// 타이머 업데이트
    private func updateTimer() {
        guard !isPaused else { return }

        timeRemaining -= 1

        if timeRemaining <= 0 {
            timeRemaining = 0
            timer?.invalidate()
            // 시간 종료 효과
            flashScreen()
        }

        updateTimeLabel()
    }

    /// 시간 라벨 업데이트
    private func updateTimeLabel() {
        timeLabel.stringValue = formatTime(timeRemaining)
        timeLabel.sizeToFit()
        timeLabel.frame = NSRect(
            x: (screenSize.width - timeLabel.frame.width) / 2,
            y: (screenSize.height - timeLabel.frame.height) / 2,
            width: timeLabel.frame.width,
            height: timeLabel.frame.height
        )

        // 1분 미만이면 빨간색
        if timeRemaining < 60 {
            timeLabel.textColor = .red
        } else {
            timeLabel.textColor = .white
        }

        // 상태에 따른 안내 문구
        if !isStarted {
            infoLabel.stringValue = "스크롤/↑↓: 시간 조절 | Space/Enter: 시작 | ESC: 종료"
        } else if isPaused {
            infoLabel.stringValue = "⏸️ 일시정지 | Space: 재개 | ESC: 종료"
        } else {
            infoLabel.stringValue = "⏵ 진행 중 | Space: 일시정지 | ESC: 종료"
        }
        infoLabel.sizeToFit()
        infoLabel.frame.origin.x = (screenSize.width - infoLabel.frame.width) / 2
    }

    /// 시간 포맷
    private func formatTime(_ seconds: TimeInterval) -> String {
        let mins = Int(seconds) / 60
        let secs = Int(seconds) % 60
        return String(format: "%02d:%02d", mins, secs)
    }

    /// 화면 깜빡임 효과
    private func flashScreen() {
        NSSound.beep()

        // 깜빡임 애니메이션
        for i in 0..<3 {
            DispatchQueue.main.asyncAfter(deadline: .now() + Double(i) * 0.5) { [weak self] in
                self?.view.layer?.backgroundColor = NSColor.red.withAlphaComponent(0.3).cgColor
            }
            DispatchQueue.main.asyncAfter(deadline: .now() + Double(i) * 0.5 + 0.25) { [weak self] in
                self?.view.layer?.backgroundColor = NSColor.clear.cgColor
            }
        }
    }

    /// 이벤트 모니터 설정
    private func setupEventMonitor() {
        localMonitor = NSEvent.addLocalMonitorForEvents(
            matching: [.keyDown, .scrollWheel]
        ) { [weak self] event in
            return self?.handleEvent(event)
        }
    }

    /// 이벤트 처리
    private func handleEvent(_ event: NSEvent) -> NSEvent? {
        switch event.type {
        case .keyDown:
            switch event.keyCode {
            case 53: // ESC
                onExit()
                return nil

            case 49, 36: // Space 또는 Enter
                if !isStarted {
                    // 타이머 시작
                    isStarted = true
                    startCountdown()
                    print("⏱️ 타이머 시작: \(formatTime(timeRemaining))")
                } else {
                    // 일시정지/재개
                    isPaused.toggle()
                }
                updateTimeLabel()
                return nil

            default:
                // 시작 전에만 시간 조절 가능
                if !isStarted {
                    // 프리셋 키: 1분, 3분, 5분, 10분 (문자로 확인)
                    if let chars = event.characters {
                        switch chars {
                        case "1":
                            timeRemaining = 1 * 60
                            updateTimeLabel()
                            return nil
                        case "3":
                            timeRemaining = 3 * 60
                            updateTimeLabel()
                            return nil
                        case "5":
                            timeRemaining = 5 * 60
                            updateTimeLabel()
                            return nil
                        case "0":
                            timeRemaining = 10 * 60
                            updateTimeLabel()
                            return nil
                        default:
                            break
                        }
                    }

                    // 화살표 키
                    switch event.keyCode {
                    case 126: // 위쪽 화살표: 시간 증가
                        timeRemaining += 60
                        updateTimeLabel()
                        return nil
                    case 125: // 아래쪽 화살표: 시간 감소
                        timeRemaining = max(0, timeRemaining - 60)
                        updateTimeLabel()
                        return nil
                    default:
                        break
                    }
                }
            }

        case .scrollWheel:
            // 시작 전에만 스크롤로 시간 조절
            if !isStarted {
                let delta = event.scrollingDeltaY
                timeRemaining += delta * 2  // 스크롤 감도
                timeRemaining = max(0, timeRemaining)
                updateTimeLabel()
            }
            return nil

        default:
            break
        }

        return event
    }

    /// 정리
    func cleanup() {
        timer?.invalidate()
        timer = nil

        if let monitor = localMonitor {
            NSEvent.removeMonitor(monitor)
            localMonitor = nil
        }
    }

    deinit {
        cleanup()
    }
}
