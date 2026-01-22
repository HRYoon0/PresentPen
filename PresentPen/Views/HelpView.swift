import SwiftUI
import AppKit

/// 도움말 윈도우 컨트롤러
class HelpWindowController {
    private var window: NSWindow?

    static let shared = HelpWindowController()

    func showHelp() {
        if let existingWindow = window {
            existingWindow.makeKeyAndOrderFront(nil)
            NSApp.activate(ignoringOtherApps: true)
            return
        }

        let helpView = HelpView()
        let hostingView = NSHostingView(rootView: helpView)

        let window = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: 600, height: 700),
            styleMask: [.titled, .closable, .miniaturizable, .resizable],
            backing: .buffered,
            defer: false
        )

        window.title = "PresentPen 사용법"
        window.contentView = hostingView
        window.center()
        window.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps: true)

        self.window = window
    }
}

/// 도움말 뷰
struct HelpView: View {
    @State private var selectedTab = 0

    var body: some View {
        VStack(spacing: 0) {
            // 헤더
            HStack {
                Image(systemName: "questionmark.circle.fill")
                    .font(.system(size: 32))
                    .foregroundColor(.blue)
                VStack(alignment: .leading) {
                    Text("PresentPen")
                        .font(.title.bold())
                    Text("프레젠테이션을 위한 화면 주석 도구")
                        .font(.subheadline)
                        .foregroundColor(.secondary)
                }
                Spacer()
            }
            .padding()
            .background(Color(NSColor.controlBackgroundColor))

            // 탭 선택
            Picker("", selection: $selectedTab) {
                Text("단축키").tag(0)
                Text("그리기").tag(1)
                Text("줌").tag(2)
                Text("커서/스포트라이트").tag(3)
                Text("타이머").tag(4)
            }
            .pickerStyle(.segmented)
            .padding()

            // 콘텐츠
            ScrollView {
                VStack(alignment: .leading, spacing: 16) {
                    switch selectedTab {
                    case 0: ShortcutsHelpView()
                    case 1: DrawingGuideView()
                    case 2: ZoomHelpView()
                    case 3: CursorSpotlightHelpView()
                    case 4: TimerHelpView()
                    default: ShortcutsHelpView()
                    }
                }
                .padding()
            }
        }
        .frame(minWidth: 500, minHeight: 500)
    }
}

/// 단축키 도움말
struct ShortcutsHelpView: View {
    var body: some View {
        VStack(alignment: .leading, spacing: 20) {
            HelpSection(title: "모드 전환", icon: "keyboard") {
                HelpRow(shortcut: "Ctrl + 1", description: "그리기 모드")
                HelpRow(shortcut: "Ctrl + 2", description: "줌 모드")
                HelpRow(shortcut: "Ctrl + 3", description: "커서 하이라이트")
                HelpRow(shortcut: "Ctrl + 4", description: "스포트라이트")
                HelpRow(shortcut: "Ctrl + 5", description: "타이머")
                HelpRow(shortcut: "ESC", description: "모든 모드 종료")
            }

            HelpSection(title: "그리기 관련", icon: "pencil") {
                HelpRow(shortcut: "Cmd + Z", description: "실행 취소")
                HelpRow(shortcut: "Ctrl + Shift + C", description: "전체 지우기")
            }

            HelpSection(title: "커서 하이라이트", icon: "cursor.rays") {
                HelpRow(shortcut: "Ctrl + Shift + 3", description: "스타일 변경")
                HelpRow(shortcut: "Ctrl + Option + 3", description: "색상 변경")
                HelpRow(shortcut: "Ctrl + 스크롤", description: "크기 조절")
            }

            HelpSection(title: "스포트라이트", icon: "light.max") {
                HelpRow(shortcut: "Ctrl + Shift + 4", description: "줌 효과 토글")
            }
        }
    }
}

/// 그리기 도움말
struct DrawingGuideView: View {
    var body: some View {
        VStack(alignment: .leading, spacing: 20) {
            HelpSection(title: "색상 변경", icon: "paintpalette") {
                HelpRow(shortcut: "R", description: "빨강")
                HelpRow(shortcut: "O", description: "주황")
                HelpRow(shortcut: "Y", description: "노랑")
                HelpRow(shortcut: "G", description: "초록")
                HelpRow(shortcut: "B", description: "파랑")
                HelpRow(shortcut: "P", description: "분홍")
            }

            HelpSection(title: "형광펜 모드", icon: "highlighter") {
                HelpRow(shortcut: "Shift + 색상키", description: "반투명 형광펜으로 전환")
            }

            HelpSection(title: "배경", icon: "rectangle.fill") {
                HelpRow(shortcut: "W", description: "화이트보드 배경 토글")
                HelpRow(shortcut: "K", description: "블랙보드 배경 토글")
            }

            HelpSection(title: "기타", icon: "slider.horizontal.3") {
                HelpRow(shortcut: "스크롤", description: "선 굵기 조절")
                HelpRow(shortcut: "E", description: "전체 지우기")
                HelpRow(shortcut: "Cmd + Z", description: "실행 취소")
                HelpRow(shortcut: "Tab", description: "원 도구")
            }
        }
    }
}

/// 줌 도움말
struct ZoomHelpView: View {
    var body: some View {
        VStack(alignment: .leading, spacing: 20) {
            HelpSection(title: "줌 조작", icon: "plus.magnifyingglass") {
                HelpRow(shortcut: "스크롤 위/아래", description: "확대/축소")
                HelpRow(shortcut: "마우스 드래그", description: "화면 이동 (패닝)")
            }

            HelpSection(title: "줌 모드 내 기능", icon: "square.stack.3d.up") {
                HelpRow(shortcut: "Ctrl + 1", description: "줌 화면 위에 그리기")
                HelpRow(shortcut: "Ctrl + 3", description: "커서 하이라이트")
                HelpRow(shortcut: "Ctrl + 4", description: "스포트라이트")
            }

            InfoBox(
                icon: "info.circle",
                text: "줌 모드가 활성화된 상태에서 다른 기능들을 함께 사용할 수 있습니다.",
                color: .blue
            )
        }
    }
}

/// 커서/스포트라이트 도움말
struct CursorSpotlightHelpView: View {
    var body: some View {
        VStack(alignment: .leading, spacing: 20) {
            HelpSection(title: "커서 하이라이트", icon: "cursor.rays") {
                HelpRow(shortcut: "Ctrl + 3", description: "켜기/끄기")
                HelpRow(shortcut: "Ctrl + Shift + 3", description: "스타일 변경")
                HelpRow(shortcut: "Ctrl + Option + 3", description: "색상 변경")
                HelpRow(shortcut: "Ctrl + 스크롤", description: "크기 조절")
            }

            Text("스타일 종류")
                .font(.headline)
            HStack(spacing: 16) {
                StylePreview(name: "링", icon: "circle")
                StylePreview(name: "헤일로", icon: "circle.circle")
                StylePreview(name: "채움", icon: "circle.fill")
                StylePreview(name: "스퀴클", icon: "app")
            }
            .padding(.bottom)

            HelpSection(title: "스포트라이트", icon: "light.max") {
                HelpRow(shortcut: "Ctrl + 4", description: "켜기/끄기")
                HelpRow(shortcut: "Ctrl + Shift + 4", description: "돋보기 모드 토글")
                HelpRow(shortcut: "스크롤", description: "배율 조절 (돋보기 모드)")
            }

            InfoBox(
                icon: "hand.raised",
                text: "스포트라이트 활성화 시 마우스 속도가 자동으로 느려져 정밀한 조작이 가능합니다.",
                color: .orange
            )
        }
    }
}

/// 타이머 도움말
struct TimerHelpView: View {
    var body: some View {
        VStack(alignment: .leading, spacing: 20) {
            HelpSection(title: "시간 설정 (시작 전)", icon: "clock") {
                HelpRow(shortcut: "1", description: "1분")
                HelpRow(shortcut: "3", description: "3분")
                HelpRow(shortcut: "5", description: "5분")
                HelpRow(shortcut: "0", description: "10분")
                HelpRow(shortcut: "↑ / ↓", description: "1분 단위 증가/감소")
                HelpRow(shortcut: "스크롤", description: "세밀한 시간 조절")
            }

            HelpSection(title: "타이머 조작", icon: "play.circle") {
                HelpRow(shortcut: "Space / Enter", description: "시작 / 일시정지 / 재개")
                HelpRow(shortcut: "ESC", description: "타이머 종료")
            }

            InfoBox(
                icon: "exclamationmark.triangle",
                text: "남은 시간이 1분 미만이면 숫자가 빨간색으로 변경됩니다. 시간 종료 시 화면이 깜빡이며 알림음이 재생됩니다.",
                color: .red
            )
        }
    }
}

// MARK: - 헬퍼 뷰

struct HelpSection<Content: View>: View {
    let title: String
    let icon: String
    @ViewBuilder let content: () -> Content

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Label(title, systemImage: icon)
                .font(.headline)
                .foregroundColor(.primary)

            VStack(alignment: .leading, spacing: 4) {
                content()
            }
            .padding(.leading, 28)
        }
    }
}

struct HelpRow: View {
    let shortcut: String
    let description: String

    var body: some View {
        HStack {
            Text(shortcut)
                .font(.system(.body, design: .monospaced))
                .foregroundColor(.white)
                .padding(.horizontal, 8)
                .padding(.vertical, 2)
                .background(Color.gray.opacity(0.6))
                .cornerRadius(4)

            Text(description)
                .foregroundColor(.secondary)

            Spacer()
        }
    }
}

struct InfoBox: View {
    let icon: String
    let text: String
    let color: Color

    var body: some View {
        HStack(alignment: .top, spacing: 12) {
            Image(systemName: icon)
                .foregroundColor(color)
                .font(.title3)

            Text(text)
                .font(.callout)
                .foregroundColor(.secondary)
        }
        .padding()
        .background(color.opacity(0.1))
        .cornerRadius(8)
    }
}

struct StylePreview: View {
    let name: String
    let icon: String

    var body: some View {
        VStack {
            Image(systemName: icon)
                .font(.title)
                .foregroundColor(.yellow)
            Text(name)
                .font(.caption)
        }
        .frame(width: 60, height: 60)
        .background(Color(NSColor.controlBackgroundColor))
        .cornerRadius(8)
    }
}
