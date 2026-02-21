using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using PresentPen.Models;

namespace PresentPen.Services
{
    public class HotkeyManager : IDisposable
    {
        private static HotkeyManager? _instance;
        public static HotkeyManager Instance => _instance ??= new HotkeyManager();

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOD_NONE = 0x0000;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CTRL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;

        private const uint VK_1 = 0x31;
        private const uint VK_2 = 0x32;
        private const uint VK_3 = 0x33;
        private const uint VK_4 = 0x34;
        private const uint VK_5 = 0x35;
        private const uint VK_ESCAPE = 0x1B;

        private const int HOTKEY_ZOOM = 1;
        private const int HOTKEY_DRAW = 2;
        private const int HOTKEY_TIMER = 3;
        private const int HOTKEY_SPOTLIGHT = 4;
        private const int HOTKEY_HIGHLIGHT = 5;
        private const int HOTKEY_ESCAPE = 6;
        private const int HOTKEY_STYLE_CYCLE = 7;
        private const int HOTKEY_COLOR_CYCLE = 8;
        private const int HOTKEY_SPOTLIGHT_ZOOM = 9;

        private const int WM_HOTKEY = 0x0312;

        private IntPtr _windowHandle;
        private HwndSource? _source;
        private bool _disposed;
        private bool _escapeRegistered;

        public event Action<AppMode>? HotkeyPressed;
        public event Action? EscapePressed;
        public event Action? CursorStyleCycleRequested;
        public event Action? CursorColorCycleRequested;
        public event Action? SpotlightZoomToggleRequested;

        private HotkeyManager() { }

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
            RegisterHotKey(_windowHandle, HOTKEY_ZOOM, MOD_CTRL, VK_1);
            RegisterHotKey(_windowHandle, HOTKEY_DRAW, MOD_CTRL, VK_2);
            RegisterHotKey(_windowHandle, HOTKEY_TIMER, MOD_CTRL, VK_3);
            RegisterHotKey(_windowHandle, HOTKEY_SPOTLIGHT, MOD_CTRL, VK_4);
            RegisterHotKey(_windowHandle, HOTKEY_HIGHLIGHT, MOD_CTRL, VK_5);

            RegisterHotKey(_windowHandle, HOTKEY_STYLE_CYCLE, MOD_CTRL | MOD_SHIFT, VK_5);
            RegisterHotKey(_windowHandle, HOTKEY_COLOR_CYCLE, MOD_CTRL | MOD_ALT, VK_5);
            RegisterHotKey(_windowHandle, HOTKEY_SPOTLIGHT_ZOOM, MOD_CTRL | MOD_SHIFT, VK_4);

            // ESC는 기능이 활성화될 때만 등록
        }

        /// <summary>
        /// 기능 활성화 시 ESC 핫키 등록
        /// </summary>
        public void RegisterEscapeHotkey()
        {
            if (!_escapeRegistered)
            {
                RegisterHotKey(_windowHandle, HOTKEY_ESCAPE, MOD_NONE, VK_ESCAPE);
                _escapeRegistered = true;
            }
        }

        /// <summary>
        /// 모든 기능 비활성화 시 ESC 핫키 해제
        /// </summary>
        public void UnregisterEscapeHotkey()
        {
            if (_escapeRegistered)
            {
                UnregisterHotKey(_windowHandle, HOTKEY_ESCAPE);
                _escapeRegistered = false;
            }
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
                    case HOTKEY_STYLE_CYCLE:
                        AppState.Instance.CycleCursorHighlightStyle();
                        CursorStyleCycleRequested?.Invoke();
                        break;
                    case HOTKEY_COLOR_CYCLE:
                        AppState.Instance.CycleCursorHighlightColor();
                        CursorColorCycleRequested?.Invoke();
                        break;
                    case HOTKEY_SPOTLIGHT_ZOOM:
                        AppState.Instance.SpotlightZoomEnabled = !AppState.Instance.SpotlightZoomEnabled;
                        SpotlightZoomToggleRequested?.Invoke();
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

            for (int i = 1; i <= 9; i++)
                UnregisterHotKey(_windowHandle, i);

            _source?.RemoveHook(HwndHook);
            _disposed = true;
        }
    }
}
