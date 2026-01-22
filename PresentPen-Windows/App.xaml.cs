using System.Windows;
using PresentPen.Services;

namespace PresentPen
{
    public partial class App : Application
    {
        private HotkeyManager? _hotkeyManager;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 전역 핫키 매니저 초기화
            _hotkeyManager = HotkeyManager.Instance;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 핫키 해제
            _hotkeyManager?.Dispose();
            base.OnExit(e);
        }
    }
}
