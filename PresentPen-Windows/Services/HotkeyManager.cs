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

        // 가상 키 코드
        private const uint VK_Z = 0x5A;  // Z
        private const uint VK_D = 0x44;  // D
        private const uint VK_S = 0x53;  // S
        private const uint VK_T = 0x54;  // T
        private const uint VK_H = 0x48;  // H
        private const uint VK_C = 0x43;  // C
        private const uint VK_ESCAPE = 0x1B;

        // 핫키 ID
        private const int HOTKEY_ZOOM = 1;
        private const int HOTKEY_DRAW = 2;
        private const int HOTKEY_SPOTLIGHT = 3;
        private const int HOTKEY_TIMER = 4;
        private const int HOTKEY_HIGHLIGHT = 5;
        private const int HOTKEY_CLEAR = 6;
        private const int HOTKEY_ESCAPE = 7;

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
            // Ctrl+Shift+Z: 줌 토글
            RegisterHotKey(_windowHandle, HOTKEY_ZOOM, MOD_CTRL | MOD_SHIFT, VK_Z);

            // Ctrl+Shift+D: 그리기 토글
            RegisterHotKey(_windowHandle, HOTKEY_DRAW, MOD_CTRL | MOD_SHIFT, VK_D);

            // Ctrl+Shift+S: 스포트라이트 토글
            RegisterHotKey(_windowHandle, HOTKEY_SPOTLIGHT, MOD_CTRL | MOD_SHIFT, VK_S);

            // Ctrl+Shift+T: 타이머 토글
            RegisterHotKey(_windowHandle, HOTKEY_TIMER, MOD_CTRL | MOD_SHIFT, VK_T);

            // Ctrl+Shift+H: 커서 하이라이트 토글
            RegisterHotKey(_windowHandle, HOTKEY_HIGHLIGHT, MOD_CTRL | MOD_SHIFT, VK_H);

            // Ctrl+Shift+C: 그리기 지우기
            RegisterHotKey(_windowHandle, HOTKEY_CLEAR, MOD_CTRL | MOD_SHIFT, VK_C);

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
                    case HOTKEY_SPOTLIGHT:
                        HotkeyPressed?.Invoke(AppMode.Spotlight);
                        break;
                    case HOTKEY_TIMER:
                        HotkeyPressed?.Invoke(AppMode.Timer);
                        break;
                    case HOTKEY_HIGHLIGHT:
                        AppState.Instance.IsCursorHighlightEnabled = !AppState.Instance.IsCursorHighlightEnabled;
                        break;
                    case HOTKEY_CLEAR:
                        ClearRequested?.Invoke();
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
            UnregisterHotKey(_windowHandle, HOTKEY_SPOTLIGHT);
            UnregisterHotKey(_windowHandle, HOTKEY_TIMER);
            UnregisterHotKey(_windowHandle, HOTKEY_HIGHLIGHT);
            UnregisterHotKey(_windowHandle, HOTKEY_CLEAR);
            UnregisterHotKey(_windowHandle, HOTKEY_ESCAPE);

            _source?.RemoveHook(HwndHook);
            _disposed = true;
        }
    }
}
