using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MacroRecoReplayer
{
    public static class InputHook
    {
        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)] private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_KEYBOARD_LL = 13, WH_MOUSE_LL = 14;
        private const int WM_KEYDOWN = 0x0100, WM_KEYUP = 0x0101, WM_SYSKEYDOWN = 0x0104, WM_SYSKEYUP = 0x0105;
        private const int WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202, WM_RBUTTONDOWN = 0x0204, WM_RBUTTONUP = 0x0205;

        [StructLayout(LayoutKind.Sequential)] private struct POINT { public int x; public int y; }
        [StructLayout(LayoutKind.Sequential)] private struct KBDLLHOOKSTRUCT { public uint vkCode; public uint scanCode; public uint flags; public uint time; public IntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Sequential)] private struct MSLLHOOKSTRUCT { public POINT pt; public uint mouseData; public uint flags; public uint time; public IntPtr dwExtraInfo; }

        private static IntPtr _kbHook, _msHook;
        private static HookProc _kbProc, _msProc;

        public delegate void KeyboardHookEvent(int vk);
        public static event KeyboardHookEvent OnKeyDown;
        public static event KeyboardHookEvent OnKeyUp;
        public static event Action<int, int, int> OnMouseDown;
        public static event Action<int, int, int> OnMouseUp;

        private static bool _suppressKey = false;
        public static void SuppressCurrentKey() => _suppressKey = true;

        public static void Initialize()
        {
            _kbProc = KbHook;
            _msProc = MsHook;
            using (Process cur = Process.GetCurrentProcess())
            using (ProcessModule mod = cur.MainModule)
            {
                IntPtr hMod = GetModuleHandle(mod.ModuleName);
                _kbHook = SetWindowsHookEx(WH_KEYBOARD_LL, _kbProc, hMod, 0);
                _msHook = SetWindowsHookEx(WH_MOUSE_LL, _msProc, hMod, 0);

                Logger.Log($"Keyboard Hook Handle: {_kbHook}");
                Logger.Log($"Mouse Hook Handle: {_msHook}");

                if (_kbHook == IntPtr.Zero || _msHook == IntPtr.Zero)
                {
                    Logger.Log("ОШИБКА: Не удалось установить хуки!");
                }
                else
                {
                    Logger.Log("Хуки успешно установлены.");
                }
            }
        }

        private static IntPtr KbHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                int vk = (int)kb.vkCode;
                bool isDown = (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN);

                if (isDown) OnKeyDown?.Invoke(vk);
                else OnKeyUp?.Invoke(vk);

                if (_suppressKey)
                {
                    _suppressKey = false;
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        private static IntPtr MsHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                MSLLHOOKSTRUCT ms = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                int btn = -1;
                bool isDown = false;

                if (wParam == (IntPtr)WM_LBUTTONDOWN) { btn = 0; isDown = true; }
                else if (wParam == (IntPtr)WM_LBUTTONUP) { btn = 0; isDown = false; }
                else if (wParam == (IntPtr)WM_RBUTTONDOWN) { btn = 1; isDown = true; }
                else if (wParam == (IntPtr)WM_RBUTTONUP) { btn = 1; isDown = false; }

                if (btn != -1)
                {
                    if (isDown) OnMouseDown?.Invoke(btn, ms.pt.x, ms.pt.y);
                    else OnMouseUp?.Invoke(btn, ms.pt.x, ms.pt.y);
                }
            }
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        public static string VkToName(int vk)
        {
            if (vk >= 65 && vk <= 90) return ((char)(vk + 32)).ToString();
            if (vk >= 48 && vk <= 57) return ((char)vk).ToString();
            switch (vk)
            {
                // Модификаторы (общие, левые и правые варианты)
                case 16: case 160: case 161: return "shift"; // Shift, LShift, RShift
                case 17: case 162: case 163: return "ctrl";   // Ctrl, LControl, RControl
                case 18: case 164: case 165: return "alt";    // Alt, LMenu, RMenu
                case 91: case 92: return "win";               // LWin, RWin

                // Остальные клавиши
                case 32: return "space";
                case 9: return "tab";
                case 13: return "enter";
                case 8: return "backspace";
                case 27: return "esc";
                case 46: return "delete";
                case 37: return "left";
                case 38: return "up";
                case 39: return "right";
                case 40: return "down";
                default:
                    if (vk >= 112 && vk <= 123) return "f" + (vk - 111);
                    return "vk_" + vk;
            }
        }
    }
}