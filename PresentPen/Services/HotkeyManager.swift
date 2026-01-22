import AppKit
import Carbon

/// 글로벌 단축키 매니저 (Carbon API 기반)
class HotkeyManager {
    private var appState: AppState
    private weak var appDelegate: AppDelegate?
    private var hotKeyRefs: [EventHotKeyRef?] = []
    private var eventHandlerRef: EventHandlerRef?

    init(appState: AppState, appDelegate: AppDelegate) {
        self.appState = appState
        self.appDelegate = appDelegate
    }

    /// 단축키 등록
    func register() {
        print("✅ HotkeyManager: 단축키 등록 시작 (Carbon API)")

        // 이벤트 핸들러 설치
        var eventType = EventTypeSpec(eventClass: OSType(kEventClassKeyboard), eventKind: UInt32(kEventHotKeyPressed))

        let handler: EventHandlerUPP = { _, event, userData -> OSStatus in
            guard let userData = userData else { return OSStatus(eventNotHandledErr) }
            let manager = Unmanaged<HotkeyManager>.fromOpaque(userData).takeUnretainedValue()

            var hotKeyID = EventHotKeyID()
            GetEventParameter(event, EventParamName(kEventParamDirectObject), EventParamType(typeEventHotKeyID),
                            nil, MemoryLayout<EventHotKeyID>.size, nil, &hotKeyID)

            manager.handleHotKey(id: Int(hotKeyID.id))
            return noErr
        }

        let selfPtr = Unmanaged.passUnretained(self).toOpaque()
        InstallEventHandler(GetApplicationEventTarget(), handler, 1, &eventType, selfPtr, &eventHandlerRef)

        // 단축키 등록 (signature: 'MZIT')
        let signature = OSType(0x4D5A4954)  // 'MZIT'

        // Ctrl + 1: 그리기 모드
        registerHotKey(id: 1, keyCode: UInt32(kVK_ANSI_1), modifiers: UInt32(controlKey), signature: signature)

        // Ctrl + 2: 줌 모드
        registerHotKey(id: 2, keyCode: UInt32(kVK_ANSI_2), modifiers: UInt32(controlKey), signature: signature)

        // Ctrl + 3: 커서 하이라이트
        registerHotKey(id: 3, keyCode: UInt32(kVK_ANSI_3), modifiers: UInt32(controlKey), signature: signature)

        // Ctrl + 4: 스포트라이트
        registerHotKey(id: 4, keyCode: UInt32(kVK_ANSI_4), modifiers: UInt32(controlKey), signature: signature)

        // Ctrl + 5: 타이머
        registerHotKey(id: 5, keyCode: UInt32(kVK_ANSI_5), modifiers: UInt32(controlKey), signature: signature)

        // Ctrl + Shift + C: 전체 지우기
        registerHotKey(id: 6, keyCode: UInt32(kVK_ANSI_C), modifiers: UInt32(controlKey | shiftKey), signature: signature)

        // ESC: 모드 종료
        registerHotKey(id: 7, keyCode: UInt32(kVK_Escape), modifiers: 0, signature: signature)

        // Ctrl + Shift + 3: 커서 하이라이트 스타일 변경
        registerHotKey(id: 8, keyCode: UInt32(kVK_ANSI_3), modifiers: UInt32(controlKey | shiftKey), signature: signature)

        // Ctrl + Option + 3: 커서 하이라이트 색상 변경
        registerHotKey(id: 9, keyCode: UInt32(kVK_ANSI_3), modifiers: UInt32(controlKey | optionKey), signature: signature)

        // Ctrl + Shift + 4: 스포트라이트 줌 토글
        registerHotKey(id: 10, keyCode: UInt32(kVK_ANSI_4), modifiers: UInt32(controlKey | shiftKey), signature: signature)

        print("📌 단축키: Ctrl+1(그리기), Ctrl+2(줌), Ctrl+3(커서), Ctrl+4(스포트라이트), Ctrl+5(타이머)")
        print("✅ HotkeyManager: Carbon 단축키 등록 완료")
    }

    private func registerHotKey(id: Int, keyCode: UInt32, modifiers: UInt32, signature: OSType) {
        var hotKeyID = EventHotKeyID(signature: signature, id: UInt32(id))
        var hotKeyRef: EventHotKeyRef?

        let status = RegisterEventHotKey(keyCode, modifiers, hotKeyID, GetApplicationEventTarget(), 0, &hotKeyRef)

        if status == noErr {
            hotKeyRefs.append(hotKeyRef)
        } else {
            print("⚠️ 단축키 등록 실패: ID \(id), status: \(status)")
        }
    }

    /// 단축키 해제
    func unregister() {
        for hotKeyRef in hotKeyRefs {
            if let ref = hotKeyRef {
                UnregisterEventHotKey(ref)
            }
        }
        hotKeyRefs.removeAll()

        if let handlerRef = eventHandlerRef {
            RemoveEventHandler(handlerRef)
            eventHandlerRef = nil
        }

        print("🛑 HotkeyManager: 단축키 해제됨")
    }

    /// 단축키 ID별 동작 처리
    private func handleHotKey(id: Int) {
        print("🔑 단축키 감지: ID \(id)")

        DispatchQueue.main.async { [weak self] in
            guard let self = self else { return }

            switch id {
            case 1:
                // 줌 모드에서는 줌 윈도우 내 그리기 토글
                if self.appState.currentMode == .zoom {
                    print("🎨 줌 모드에서 Ctrl+1 → 줌 윈도우 내 그리기 토글")
                    self.appDelegate?.toggleZoomDrawing()
                    return
                }
                print("🎨 그리기 모드 토글")
                self.appState.toggleMode(.drawing)

            case 2:
                let beforeMode = self.appState.currentMode
                print("🔍 줌 모드 토글 (전: \(beforeMode))")
                // 줌 모드 종료 시 마우스 속도 복원
                if beforeMode == .zoom {
                    MouseSpeedController.shared.stop()
                }
                self.appState.toggleMode(.zoom)
                let afterMode = self.appState.currentMode
                print("🔍 줌 모드 토글 완료 (후: \(afterMode))")

            case 3:
                // 줌 모드에서는 줌 윈도우 내 커서 하이라이트 토글
                if self.appState.currentMode == .zoom {
                    print("✨ 줌 모드에서 Ctrl+3 → 줌 윈도우 내 커서 하이라이트 토글")
                    self.appDelegate?.toggleZoomCursorHighlight()
                    return
                }
                print("✨ 커서 하이라이트 토글")
                self.appState.cursorHighlightEnabled.toggle()

            case 4:
                // 줌 모드에서는 줌 윈도우 내 스포트라이트 토글
                if self.appState.currentMode == .zoom {
                    print("💡 줌 모드에서 Ctrl+4 → 줌 윈도우 내 스포트라이트 토글")
                    self.appDelegate?.toggleZoomSpotlight()
                    // 줌 모드 스포트라이트에서도 마우스 속도 조절
                    if self.appDelegate?.isZoomSpotlightEnabled() == true {
                        MouseSpeedController.shared.start(multiplier: 0.3)
                    } else {
                        MouseSpeedController.shared.stop()
                    }
                    return
                }
                print("💡 스포트라이트 토글")
                let willBeSpotlight = self.appState.currentMode != .spotlight
                self.appState.toggleMode(.spotlight)
                // 스포트라이트 켜면 마우스 느리게, 끄면 원래대로
                if willBeSpotlight {
                    MouseSpeedController.shared.start(multiplier: 0.3)
                } else {
                    MouseSpeedController.shared.stop()
                }

            case 5:
                print("⏱️ 타이머 토글")
                self.appState.toggleMode(.timer)

            case 6:
                print("🗑️ 전체 지우기")
                self.appState.clearDrawings()

            case 7:
                // ESC: 모든 모드 및 커서 하이라이트 종료
                if self.appState.currentMode != .none || self.appState.cursorHighlightEnabled {
                    print("🚪 모드 종료")
                    self.appState.currentMode = .none
                    self.appState.cursorHighlightEnabled = false
                    // 모드 종료 시 마우스 속도 복원
                    MouseSpeedController.shared.stop()
                }

            case 8:
                // 커서 하이라이트 스타일 순환
                let styleCount = 4  // ring, halo, filled, squircle
                self.appState.cursorHighlightStyleIndex = (self.appState.cursorHighlightStyleIndex + 1) % styleCount
                let styleNames = ["링", "헤일로", "채움", "스퀴클"]
                print("✨ 커서 하이라이트 스타일: \(styleNames[self.appState.cursorHighlightStyleIndex])")

            case 9:
                // 커서 하이라이트 색상 순환
                self.appState.cycleCursorHighlightColor()

            case 10:
                // 스포트라이트 줌 토글
                self.appState.spotlightZoomEnabled.toggle()
                print("🔍 스포트라이트 줌: \(self.appState.spotlightZoomEnabled ? "ON (돋보기)" : "OFF (기본)")")

            default:
                break
            }

            self.notifyAppDelegate()
        }
    }

    /// AppDelegate에 업데이트 알림
    private func notifyAppDelegate() {
        print("📢 notifyAppDelegate 호출")
        if let delegate = appDelegate {
            print("✅ AppDelegate 찾음")
            delegate.updateOverlays()
        } else {
            print("❌ AppDelegate 참조가 없음")
        }
    }

    deinit {
        unregister()
    }
}
