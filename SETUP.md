# MacZoomIt - Xcode 프로젝트 설정 가이드

## 1. Xcode에서 새 프로젝트 생성

1. Xcode를 열고 **File > New > Project** 선택
2. **macOS** > **App** 선택
3. 다음 설정 입력:
   - **Product Name**: `MacZoomIt`
   - **Team**: 본인 계정 선택 (또는 None)
   - **Organization Identifier**: `com.maczoomit`
   - **Interface**: `SwiftUI`
   - **Language**: `Swift`
4. 저장 위치: 이 프로젝트 폴더의 상위 디렉토리 선택

## 2. 기존 파일 추가

프로젝트 생성 후 Xcode가 자동 생성한 파일들을 **삭제**하고, 이 폴더의 파일들로 교체합니다:

### 삭제할 파일:
- `ContentView.swift`
- `MacZoomItApp.swift` (새로 만든 것으로 교체)

### 추가할 파일:
Finder에서 `MacZoomIt/MacZoomIt` 폴더의 모든 파일을 Xcode 프로젝트 네비게이터로 드래그 & 드롭

## 3. Info.plist 설정

프로젝트 설정에서:
1. 타겟 선택 > **Info** 탭
2. **Custom macOS Application Target Properties** 추가:
   - `LSUIElement` = `YES` (메뉴바 앱으로 실행)

## 4. 권한 설정

**Signing & Capabilities** 탭에서:
1. **Hardened Runtime** 비활성화 (개인 사용 시)
2. 또는 다음 권한 추가:
   - **Accessibility** (접근성 권한)

## 5. 빌드 설정

1. **Build Settings** 탭에서:
   - `MACOSX_DEPLOYMENT_TARGET` = `13.0`
   - `SWIFT_VERSION` = `5.9`

## 6. 빌드 및 실행

1. `Cmd + R`로 빌드 및 실행
2. 처음 실행 시 **접근성 권한** 요청 다이얼로그가 표시됩니다
3. **시스템 설정 > 개인정보 보호 및 보안 > 접근성**에서 MacZoomIt 허용

---

## 프로젝트 파일 구조

```
MacZoomIt/
├── MacZoomItApp.swift      # 앱 진입점
├── AppDelegate.swift       # 메뉴바 앱 설정
├── Info.plist              # 앱 설정
├── MacZoomIt.entitlements  # 권한 설정
├── Models/
│   └── AppState.swift      # 전역 상태 관리
├── Views/
│   ├── OverlayWindow.swift     # 투명 오버레이 윈도우
│   ├── CursorHighlightView.swift
│   ├── DrawingCanvasView.swift
│   ├── SpotlightView.swift
│   ├── ZoomOverlayView.swift
│   └── SettingsView.swift
├── Services/
│   └── HotkeyManager.swift     # 글로벌 단축키
└── Resources/
```

---

## 단축키

| 기능 | 단축키 |
|------|--------|
| 그리기 모드 | `Ctrl + 1` |
| 줌 모드 | `Ctrl + 2` |
| 커서 하이라이트 | `Ctrl + 3` |
| 스포트라이트 | `Ctrl + 4` |
| 전체 지우기 | `Ctrl + Shift + C` |
| 실행 취소 | `Cmd + Z` |
| 모드 종료 | `ESC` |
