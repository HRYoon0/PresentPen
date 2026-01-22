import AppKit
import CoreGraphics
import ApplicationServices

/// 마우스 속도 조절 컨트롤러 (CGEventTap 사용)
class MouseSpeedController {
    private var eventTap: CFMachPort?
    private var runLoopSource: CFRunLoopSource?
    private var isEnabled: Bool = false

    /// 마우스 속도 배율 (0.1 = 10% 속도, 0.5 = 50% 속도, 1.0 = 정상)
    var speedMultiplier: CGFloat = 0.3

    static let shared = MouseSpeedController()

    private init() {}

    /// 접근성 권한 확인
    static func checkAccessibilityPermission() -> Bool {
        let trusted = AXIsProcessTrusted()
        print("🔐 접근성 권한 상태: \(trusted ? "허용됨" : "거부됨")")
        return trusted
    }

    /// 접근성 권한 요청 (시스템 설정 열기)
    static func requestAccessibilityPermission() {
        print("🔐 접근성 권한 요청 중...")
        let options = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true] as CFDictionary
        AXIsProcessTrustedWithOptions(options)
    }

    /// 마우스 속도 조절 시작
    func start(multiplier: CGFloat = 0.3) {
        guard !isEnabled else {
            print("🐭 MouseSpeedController: 이미 활성화됨")
            return
        }

        // 접근성 권한 확인
        if !MouseSpeedController.checkAccessibilityPermission() {
            print("❌ MouseSpeedController: 접근성 권한 없음 - 권한 요청")
            MouseSpeedController.requestAccessibilityPermission()
            return
        }

        speedMultiplier = multiplier
        print("🐭 MouseSpeedController: 시작 시도 (배율: \(multiplier))")

        // CGEventTap 생성 - 마우스 이동 이벤트 가로채기
        let eventMask = (1 << CGEventType.mouseMoved.rawValue) |
                        (1 << CGEventType.leftMouseDragged.rawValue) |
                        (1 << CGEventType.rightMouseDragged.rawValue)

        // 콜백 함수
        let callback: CGEventTapCallBack = { proxy, type, event, refcon in
            guard let refcon = refcon else { return Unmanaged.passUnretained(event) }

            let controller = Unmanaged<MouseSpeedController>.fromOpaque(refcon).takeUnretainedValue()

            // 이벤트 타입 확인
            if type == .mouseMoved || type == .leftMouseDragged || type == .rightMouseDragged {
                // 델타 값 가져오기
                let deltaX = event.getDoubleValueField(.mouseEventDeltaX)
                let deltaY = event.getDoubleValueField(.mouseEventDeltaY)

                // 속도 배율 적용
                let newDeltaX = deltaX * Double(controller.speedMultiplier)
                let newDeltaY = deltaY * Double(controller.speedMultiplier)

                // 수정된 델타 값 설정
                event.setDoubleValueField(.mouseEventDeltaX, value: newDeltaX)
                event.setDoubleValueField(.mouseEventDeltaY, value: newDeltaY)
            }

            return Unmanaged.passUnretained(event)
        }

        // self를 refcon으로 전달
        let refcon = Unmanaged.passUnretained(self).toOpaque()

        eventTap = CGEvent.tapCreate(
            tap: .cghidEventTap,
            place: .headInsertEventTap,
            options: .defaultTap,
            eventsOfInterest: CGEventMask(eventMask),
            callback: callback,
            userInfo: refcon
        )

        guard let eventTap = eventTap else {
            print("❌ MouseSpeedController: CGEventTap 생성 실패")
            print("   → 시스템 설정 > 개인 정보 보호 및 보안 > 접근성에서 앱 권한 확인")
            return
        }

        // RunLoop에 추가
        runLoopSource = CFMachPortCreateRunLoopSource(kCFAllocatorDefault, eventTap, 0)
        CFRunLoopAddSource(CFRunLoopGetCurrent(), runLoopSource, .commonModes)
        CGEvent.tapEnable(tap: eventTap, enable: true)

        isEnabled = true
        print("✅ MouseSpeedController: 마우스 속도 조절 시작됨 (배율: \(String(format: "%.0f", speedMultiplier * 100))%)")
    }

    /// 마우스 속도 조절 중지
    func stop() {
        guard isEnabled else { return }

        if let eventTap = eventTap {
            CGEvent.tapEnable(tap: eventTap, enable: false)
        }

        if let runLoopSource = runLoopSource {
            CFRunLoopRemoveSource(CFRunLoopGetCurrent(), runLoopSource, .commonModes)
        }

        eventTap = nil
        runLoopSource = nil
        isEnabled = false

        print("🐭 MouseSpeedController: 마우스 속도 조절 중지됨")
    }

    /// 속도 배율 변경
    func setSpeed(_ multiplier: CGFloat) {
        speedMultiplier = max(0.1, min(1.0, multiplier))
        print("🐭 마우스 속도 배율: \(String(format: "%.0f", speedMultiplier * 100))%")
    }

    deinit {
        stop()
    }
}
