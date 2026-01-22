import SwiftUI

/// 설정 화면
struct SettingsView: View {
    @EnvironmentObject var appState: AppState

    var body: some View {
        TabView {
            // 일반 설정
            GeneralSettingsView()
                .tabItem {
                    Label("일반", systemImage: "gear")
                }

            // 그리기 설정
            DrawingSettingsView()
                .environmentObject(appState)
                .tabItem {
                    Label("그리기", systemImage: "pencil")
                }

            // 커서 하이라이트 설정
            CursorSettingsView()
                .environmentObject(appState)
                .tabItem {
                    Label("커서", systemImage: "cursorarrow")
                }

            // 단축키 설정
            HotkeySettingsView()
                .tabItem {
                    Label("단축키", systemImage: "keyboard")
                }
        }
        .frame(width: 450, height: 300)
    }
}

/// 일반 설정 뷰
struct GeneralSettingsView: View {
    @AppStorage("launchAtLogin") private var launchAtLogin = false

    var body: some View {
        Form {
            Section {
                Toggle("로그인 시 자동 시작", isOn: $launchAtLogin)
            }

            Section {
                Text("PresentPen v1.0")
                    .foregroundColor(.secondary)
                Text("프레젠테이션을 위한 화면 주석 도구")
                    .font(.caption)
                    .foregroundColor(.secondary)
            }
        }
        .padding()
    }
}

/// 그리기 설정 뷰
struct DrawingSettingsView: View {
    @EnvironmentObject var appState: AppState

    var body: some View {
        Form {
            Section("기본 도구") {
                Picker("기본 그리기 도구", selection: $appState.currentTool) {
                    ForEach(DrawingTool.allCases, id: \.self) { tool in
                        Text(tool.rawValue).tag(tool)
                    }
                }
            }

            Section("색상 및 굵기") {
                ColorPicker("기본 색상", selection: $appState.currentColor)

                HStack {
                    Text("선 굵기: \(Int(appState.currentLineWidth))")
                    Slider(value: $appState.currentLineWidth, in: 1...20, step: 1)
                }
            }
        }
        .padding()
    }
}

/// 커서 하이라이트 설정 뷰
struct CursorSettingsView: View {
    @EnvironmentObject var appState: AppState

    var body: some View {
        Form {
            Section("커서 하이라이트") {
                ColorPicker("하이라이트 색상", selection: $appState.cursorHighlightColor)

                HStack {
                    Text("크기: \(Int(appState.cursorHighlightRadius))")
                    Slider(value: $appState.cursorHighlightRadius, in: 10...100, step: 5)
                }

                HStack {
                    Text("투명도: \(Int(appState.cursorHighlightOpacity * 100))%")
                    Slider(value: $appState.cursorHighlightOpacity, in: 0.1...1.0, step: 0.1)
                }
            }

            Section("스포트라이트") {
                HStack {
                    Text("스포트라이트 크기: \(Int(appState.spotlightRadius))")
                    Slider(value: $appState.spotlightRadius, in: 50...300, step: 10)
                }
            }
        }
        .padding()
    }
}

/// 단축키 설정 뷰
struct HotkeySettingsView: View {
    var body: some View {
        Form {
            Section("현재 단축키") {
                HotkeyRow(action: "그리기 모드", shortcut: "Ctrl + 1")
                HotkeyRow(action: "줌 모드", shortcut: "Ctrl + 2")
                HotkeyRow(action: "커서 하이라이트", shortcut: "Ctrl + 3")
                HotkeyRow(action: "스포트라이트", shortcut: "Ctrl + 4")
                HotkeyRow(action: "전체 지우기", shortcut: "Ctrl + Shift + C")
                HotkeyRow(action: "실행 취소", shortcut: "Cmd + Z")
                HotkeyRow(action: "모드 종료", shortcut: "ESC")
            }
        }
        .padding()
    }
}

/// 단축키 행
struct HotkeyRow: View {
    let action: String
    let shortcut: String

    var body: some View {
        HStack {
            Text(action)
            Spacer()
            Text(shortcut)
                .foregroundColor(.secondary)
                .font(.system(.body, design: .monospaced))
        }
    }
}

// Preview 제거됨
