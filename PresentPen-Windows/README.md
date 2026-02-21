# PresentPen for Windows

프레젠테이션을 위한 화면 확대, 그리기, 하이라이트 도구 (Windows 버전)

## 기능

- **화면 확대** (Ctrl+1): 마우스 위치 중심 줌 + 줌 내 그리기/커서/스포트라이트 동시 사용
- **그리기** (Ctrl+2): 펜, 형광펜, 직선, 화살표, 사각형, 원 + 키보드 단축키
- **타이머** (Ctrl+3): 전체 화면 카운트다운, 프리셋(1/3/5/10분), 경고 효과
- **스포트라이트** (Ctrl+4): 마우스 주변만 밝게 표시 + 줌(돋보기) 기능
- **커서 하이라이트** (Ctrl+5): 4가지 스타일(링/헤일로/채움/스퀴클) + 9가지 색상

## 단축키 (ZoomIt 호환)

### 모드 전환
| 기능 | 단축키 |
|------|--------|
| 화면 확대 | `Ctrl+1` |
| 그리기 | `Ctrl+2` |
| 타이머 | `Ctrl+3` |
| 스포트라이트 | `Ctrl+4` |
| 커서 하이라이트 | `Ctrl+5` |
| 현재 모드 종료 | `ESC` |

### 그리기 모드
| 기능 | 단축키 |
|------|--------|
| 색상 변경 | `R/G/B/Y/O/P` |
| 형광펜 | `Shift + 색상키` |
| 직선 | `Shift + 클릭` |
| 사각형 | `Ctrl + 클릭` |
| 화살표 | `Ctrl+Shift + 클릭` |
| 원 | `Tab` |
| 화이트보드 | `W` |
| 칠판(블랙보드) | `K` |
| 선 굵기 | 마우스 스크롤 |
| 전체 지우기 | `E` |
| 실행 취소 | `Ctrl+Z` |

### 커서 하이라이트
| 기능 | 단축키 |
|------|--------|
| 스타일 순환 | `Ctrl+Shift+5` |
| 색상 순환 | `Ctrl+Alt+5` |

### 스포트라이트
| 기능 | 단축키 |
|------|--------|
| 줌 효과 토글 | `Ctrl+Shift+4` |

### 타이머
| 기능 | 단축키 |
|------|--------|
| 프리셋 | `1/3/5/0` (1분/3분/5분/10분) |
| 시간 조절 | `↑↓` (1분), 스크롤 (10초) |
| 시작/일시정지 | `Space` 또는 `Enter` |

## 빌드 방법

### 요구사항
- .NET 8.0 SDK
- Windows 10/11

### 일반 빌드
```bash
dotnet build
dotnet run
```

### 포터블 빌드 (설치 없이 실행 가능)
```bash
dotnet publish -c Release
```
`bin/Release/net8.0-windows/win-x64/publish/PresentPen.exe` 파일 하나로 실행 가능

## 프로젝트 구조

```
PresentPen-Windows/
├── Models/
│   └── AppState.cs              # 앱 상태 관리
├── Services/
│   ├── HotkeyManager.cs         # 전역 핫키 관리
│   ├── MouseSpeedController.cs  # 마우스 속도 조절
│   └── ScreenCaptureService.cs  # 화면 캡처
├── Views/
│   ├── MainWindow.xaml          # 메인 윈도우
│   ├── ZoomOverlayWindow.xaml   # 화면 확대 (줌 내 기능 조합)
│   ├── DrawingCanvasWindow.xaml # 그리기 캔버스
│   ├── SpotlightWindow.xaml     # 스포트라이트
│   ├── TimerWindow.xaml         # 카운트다운 타이머
│   ├── CursorHighlightWindow.xaml # 커서 하이라이트
│   ├── HelpWindow.xaml          # 도움말
│   └── SettingsWindow.xaml      # 설정
├── App.xaml
└── PresentPen.csproj
```

## 라이선스

MIT License
