using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MacroRecoReplayer
{
    public class Replayer
    {
        [DllImport("user32.dll")] private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, IntPtr dwExtraInfo);
        [DllImport("user32.dll")] private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        [DllImport("user32.dll")] private static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)] private struct POINT { public int x; public int y; }

        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002, MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008, MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020, MOUSEEVENTF_MIDDLEUP = 0x0040;

        private CancellationTokenSource _cts;
        private bool _isPlaying = false;

        public void StartPlayback()
        {
            if (_isPlaying) return;

            string script = Program.Tray.SelectedScript;
            if (string.IsNullOrEmpty(script))
            {
                MessageBox.Show("Выберите скрипт для воспроизведения в трее", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, script + ".recore");
            if (!File.Exists(path))
            {
                MessageBox.Show("Файл скрипта не найден.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _isPlaying = true;
            _cts = new CancellationTokenSource();
            Logger.Log("Воспроизведение запущено.");
            Task.Run(() => PlayLoop(path, _cts.Token));
        }

        public void StopPlayback()
        {
            if (_isPlaying)
            {
                _cts?.Cancel();
                _isPlaying = false;
                ReleaseAllKeysAndMouse(); // Отпускаем ВСЁ при ручной остановке
                Logger.Log("Воспроизведение остановлено пользователем.");
            }
        }

        private void PlayLoop(string path, CancellationToken ct)
        {
            string[] lines = File.ReadAllLines(path);
            bool loop = lines.Length > 0 && lines[0].Trim().StartsWith("loop");
            int startIdx = loop ? 1 : 0;

            if (loop) Logger.Log("Бесконечный цикл активирован.");

            int iteration = 0;
            do
            {
                if (ct.IsCancellationRequested) break;

                if (loop)
                {
                    iteration++;
                    Logger.Log($"Перезапуск цикла #{iteration}...");
                }

                for (int i = startIdx; i < lines.Length; i++)
                {
                    if (ct.IsCancellationRequested) break;
                    var parts = lines[i].Trim().Split(' ');
                    if (parts.Length < 2) continue;

                    double delay = double.Parse(parts[0]) * 1000;
                    string action = parts[1];

                    if (action.StartsWith("click_") || action.StartsWith("mouse_"))
                    {
                        int tx = int.Parse(parts[parts.Length - 2]);
                        int ty = int.Parse(parts[parts.Length - 1]);
                        MoveSmooth(tx, ty, (int)delay, ct);

                        string btn = action.Contains("_l") ? "l" : "r";
                        uint downFlag = btn == "l" ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_RIGHTDOWN;
                        uint upFlag = btn == "l" ? MOUSEEVENTF_LEFTUP : MOUSEEVENTF_RIGHTUP;

                        if (action.StartsWith("click_"))
                        {
                            mouse_event(downFlag, 0, 0, 0, IntPtr.Zero);
                            Thread.Sleep(10);
                            mouse_event(upFlag, 0, 0, 0, IntPtr.Zero);
                        }
                        else if (action.EndsWith("down")) mouse_event(downFlag, 0, 0, 0, IntPtr.Zero);
                        else if (action.EndsWith("up")) mouse_event(upFlag, 0, 0, 0, IntPtr.Zero);
                    }
                    else if (action == "key" || action.StartsWith("key_"))
                    {
                        byte vk = ResolveVk(parts[parts.Length - 1]);
                        if (action == "key")
                        {
                            keybd_event(vk, 0, 0, UIntPtr.Zero);
                            Thread.Sleep(10);
                            keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                        }
                        else if (action.EndsWith("down")) keybd_event(vk, 0, 0, UIntPtr.Zero);
                        else if (action.EndsWith("up")) keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

                        if (delay > 10) Thread.Sleep((int)delay);
                    }
                    else if (action == "move")
                    {
                        int tx = int.Parse(parts[2]);
                        int ty = int.Parse(parts[3]);
                        MoveSmooth(tx, ty, (int)delay, ct);
                    }
                }
            } while (loop && !ct.IsCancellationRequested);

            _isPlaying = false;
            ReleaseAllKeysAndMouse(); // Отпускаем ВСЁ при естественном завершении
            if (!ct.IsCancellationRequested) Logger.Log("Воспроизведение завершено.");
        }

        private void MoveSmooth(int tx, int ty, int duration, CancellationToken ct)
        {
            GetCursorPos(out POINT p);
            int startX = p.x, startY = p.y;
            int dx = tx - startX, dy = ty - startY;

            if (dx == 0 && dy == 0) return;

            if (duration <= 5)
            {
                SetCursorPos(tx, ty);
                return;
            }

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < duration)
            {
                if (ct.IsCancellationRequested) return;

                double progress = (double)sw.ElapsedMilliseconds / duration;
                int currentX = startX + (int)(dx * progress);
                int currentY = startY + (int)(dy * progress);

                SetCursorPos(currentX, currentY);

                int remaining = duration - (int)sw.ElapsedMilliseconds;
                if (remaining > 15)
                    Thread.Sleep(1);
                else
                    Thread.SpinWait(1000);
            }

            SetCursorPos(tx, ty);
        }

        private byte ResolveVk(string name)
        {
            if (name.Length == 1 && char.IsLetterOrDigit(name[0])) return (byte)char.ToUpper(name[0]);
            switch (name)
            {
                case "shift": return 16;
                case "ctrl": return 17;
                case "alt": return 18;
                case "win": return 91;
                case "space": return 32;
                case "enter": return 13;
                case "tab": return 9;
                case "esc": return 27;
                case "backspace": return 8;
                case "delete": return 46;
                case "up": return 38;
                case "down": return 40;
                case "left": return 37;
                case "right": return 39;
                default:
                    if (name.StartsWith("f") && int.TryParse(name.Substring(1), out int f)) return (byte)(111 + f);
                    return 0;
            }
        }

        // Насильно отпускаем ВСЕ возможные клавиши и кнопки мыши
        private void ReleaseAllKeysAndMouse()
        {
            // 1. Отпускаем все клавиши клавиатуры (перебираем все возможные VK-коды)
            for (byte i = 0; i < 255; i++)
            {
                keybd_event(i, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }

            // 2. Отпускаем все кнопки мыши (левую, правую, среднюю)
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, IntPtr.Zero);
            mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, IntPtr.Zero);

            Logger.Log("Все клавиши и кнопки мыши принудительно отпущены.");
        }
    }
}