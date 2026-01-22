# PresentPen for Windows

프레젠테이션을 위한 화면 확대, 그리기, 하이라이트 도구 (Windows 버전)

## 기능

- **화면 확대** (Ctrl+Shift+Z): 마우스 위치 중심으로 화면 확대
- **그리기** (Ctrl+Shift+D): 화면 위에 자유롭게 그리기
- **스포트라이트** (Ctrl+Shift+S): 마우스 주변만 밝게 표시
- **타이머** (Ctrl+Shift+T): 프레젠테이션 시간 측정
- **커서 하이라이트** (Ctrl+Shift+H): 마우스 커서 강조 표시

## 단축키

| 기능 | 단축키 |
|------|--------|
| 화면 확대 | `Ctrl+Shift+Z` |
| 그리기 | `Ctrl+Shift+D` |
| 스포트라이트 | `Ctrl+Shift+S` |
| 타이머 | `Ctrl+Shift+T` |
| 커서 하이라이트 | `Ctrl+Shift+H` |
| 그리기 지우기 | `Ctrl+Shift+C` |
| 현재 모드 종료 | `ESC` |

## 빌드 방법

### 요구사항
- .NET 8.0 SDK
- Windows 10/11

### 빌드
```bash
dotnet build
```

### 실행
```bash
dotnet run
```

### 배포용 빌드
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## 프로젝트 구조

```
PresentPen-Windows/
├── Models/
│   └── AppState.cs          # 앱 상태 관리
├── Services/
│   ├── HotkeyManager.cs     # 전역 핫키 관리
│   └── ScreenCaptureService.cs  # 화면 캡처
├── Views/
│   ├── MainWindow.xaml      # 메인 윈도우
│   ├── ZoomOverlayWindow.xaml    # 화면 확대
│   ├── DrawingCanvasWindow.xaml  # 그리기 캔버스
│   ├── SpotlightWindow.xaml      # 스포트라이트
│   ├── TimerWindow.xaml          # 타이머
│   ├── CursorHighlightWindow.xaml # 커서 하이라이트
│   └── SettingsWindow.xaml       # 설정
├── App.xaml
└── PresentPen.csproj
```

## 라이선스

MIT License
