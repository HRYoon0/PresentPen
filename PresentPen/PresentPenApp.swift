import SwiftUI

@main
struct MacZoomItApp: App {
    // AppDelegate를 연결하여 메뉴바 앱으로 동작
    @NSApplicationDelegateAdaptor(AppDelegate.self) var appDelegate

    var body: some Scene {
        // 메뉴바 앱이므로 기본 윈도우 그룹 대신 Settings만 사용
        Settings {
            SettingsView()
                .environmentObject(appDelegate.appState)
        }
    }
}
