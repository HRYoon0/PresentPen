using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using PresentPen.Models;

namespace PresentPen.Services
{
    /// <summary>
    /// Windows 전역 핫키 관리자
    /// Win32 RegisterHotKey API를 사용하여 전역 단축키 등록
    /// </summary>
    public class HotkeyManager : IDisposable
    {
        private static HotkeyManager? _instance;
        public static HotkeyManager Instance => _instance ??= new HotkeyManager();

        // Win32 API 선언
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // 핫키 모디파이어
        private const uint MOD_NONE = 0x0000;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CTRL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        // 가상 키 코드 (ZoomIt 호환)
        private const uint VK_1 = 0x31;  // 1
        private const uint VK_2 = 0x32;  // 2
        private const uint VK_3 = 0x33;  // 3
        private const uint VK_4 = 0x34;  // 4
        private const uint VK_5 = 0x35;  // 5
        private const uint VK_ESCAPE = 0x1B;

        // 핫키 ID (ZoomIt 호환: Ctrl+1~5)
        private const int HOTKEY_ZOOM = 1;       // Ctrl+1: 줌
        private const int HOTKEY_DRAW = 2;       // Ctrl+2: 그리기
        private const int HOTKEY_TIMER = 3;      // Ctrl+3: 타이머
        private const int HOTKEY_SPOTLIGHT = 4;  // Ctrl+4: 스포트라이트
        private const int HOTKEY_HIGHLIGHT = 5;  // Ctrl+5: 커서 하이라이트
        private const int HOTKEY_ESCAPE = 6;

        private const int WM_HOTKEY = 0x0312;

        private IntPtr _windowHandle;
        private HwndSource? _source;
        private bool _disposed;

        public event Action<AppMode>? HotkeyPressed;
        public event Action? ClearRequested;
        public event Action? EscapePressed;

        private HotkeyManager() { }

        /// <summary>
        /// 핫키 등록 (윈도우 로드 후 호출 필요)
        /// </summary>
        public void Initialize(Window window)
        {
            var helper = new WindowInteropHelper(window);
            _windowHandle = helper.Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source?.AddHook(HwndHook);

            RegisterHotkeys();
        }

        private void RegisterHotkeys()
        {
            // Ctrl+1: 줌 모드 (ZoomIt 동일)
            RegisterHotKey(_windowHandle, HOTKEY_ZOOM, MOD_CTRL, VK_1);

            // Ctrl+2: 그리기 모드 (ZoomIt 동일)
            RegisterHotKey(_windowHandle, HOTKEY_DRAW, MOD_CTRL, VK_2);

            // Ctrl+3: 타이머 (ZoomIt 동일)
            RegisterHotKey(_windowHandle, HOTKEY_TIMER, MOD_CTRL, VK_3);

            // Ctrl+4: 스포트라이트 (ZoomIt LiveZoom 위치)
            RegisterHotKey(_windowHandle, HOTKEY_SPOTLIGHT, MOD_CTRL, VK_4);

            // Ctrl+5: 커서 하이라이트
            RegisterHotKey(_windowHandle, HOTKEY_HIGHLIGHT, MOD_CTRL, VK_5);

            // ESC: 현재 모드 종료
            RegisterHotKey(_windowHandle, HOTKEY_ESCAPE, MOD_NONE, VK_ESCAPE);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int hotkeyId = wParam.ToInt32();
                handled = true;

                switch (hotkeyId)
                {
                    case HOTKEY_ZOOM:
                        HotkeyPressed?.Invoke(AppMode.Zoom);
                        break;
                    case HOTKEY_DRAW:
                        HotkeyPressed?.Invoke(AppMode.Draw);
                        break;
                    case HOTKEY_TIMER:
                        HotkeyPressed?.Invoke(AppMode.Timer);
                        break;
                    case HOTKEY_SPOTLIGHT:
                        HotkeyPressed?.Invoke(AppMode.Spotlight);
                        break;
                    case HOTKEY_HIGHLIGHT:
                        AppState.Instance.IsCursorHighlightEnabled = !AppState.Instance.IsCursorHighlightEnabled;
                        break;
                    case HOTKEY_ESCAPE:
                        EscapePressed?.Invoke();
                        break;
                }
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (_disposed) return;

            UnregisterHotKey(_windowHandle, HOTKEY_ZOOM);
            UnregisterHotKey(_windowHandle, HOTKEY_DRAW);
            UnregisterHotKey(_windowHandle, HOTKEY_TIMER);
            UnregisterHotKey(_windowHandle, HOTKEY_SPOTLIGHT);
            UnregisterHotKey(_windowHandle, HOTKEY_HIGHLIGHT);
            UnregisterHotKey(_windowHandle, HOTKEY_ESCAPE);

            _source?.RemoveHook(HwndHook);
            _disposed = true;
        }
    }
}
