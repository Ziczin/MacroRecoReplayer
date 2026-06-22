using System;
using System.Collections.Generic;
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

        // Хешсет для отслеживания реально зажатых клавиш клавиатуры
        private HashSet<byte> _heldKeys = new HashSet<byte>();

        // Флаги для отслеживания зажатых кнопок мыши
        private bool _isLeftDown = false;
        private bool _isRightDown = false;
        private bool _isMiddleDown = false;

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
                ReleaseHeldKeysAndMouse();
                Logger.Log("Воспроизведение остановлено пользователем.");
            }
        }

        private void PlayLoop(string path, CancellationToken ct)
        {
            // Сбрасываем состояния перед началом проигрывания
            lock (_heldKeys) _heldKeys.Clear();
            _isLeftDown = false;
            _isRightDown = false;
            _isMiddleDown = false;

            string[] lines = File.ReadAllLines(path);
            bool loop = false;
            int repeatCount = 1;
            int startIdx = 0;

            if (lines.Length > 0)
            {
                string firstLine = lines[0].Trim();
                if (firstLine.Equals("loop", StringComparison.OrdinalIgnoreCase))
                {
                    loop = true;
                    startIdx = 1;
                }
                else if (firstLine.StartsWith("repeat", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = firstLine.Split(' ');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int r))
                    {
                        repeatCount = r <= 0 ? 1 : r;
                    }
                    startIdx = 1;
                }
            }

            if (loop) Logger.Log("Бесконечный цикл активирован.");
            else if (repeatCount > 1) Logger.Log($"Режим повтора активирован: {repeatCount} раз.");

            int currentIteration = 0;
            int totalIterations = loop ? int.MaxValue : repeatCount;

            while (currentIteration < totalIterations)
            {
                if (ct.IsCancellationRequested) break;
                currentIteration++;

                if (loop || repeatCount > 1)
                {
                    if (currentIteration == 1) Logger.Log("Начало цикла...");
                    else Logger.Log($"Перезапуск цикла #{currentIteration}...");
                }

                for (int i = startIdx; i < lines.Length; i++)
                {
                    if (ct.IsCancellationRequested) break;
                    var parts = lines[i].Trim().Split(' ');
                    if (parts.Length < 2) continue;

                    double delay = double.Parse(parts[0]) * 1000;
                    string action = parts[1];

                    if (action == "move")
                    {
                        int tx = int.Parse(parts[2]);
                        int ty = int.Parse(parts[3]);
                        MoveSmooth(tx, ty, (int)delay, ct);
                    }
                    else if (action.StartsWith("click_") || action.StartsWith("mouse_"))
                    {
                        int tx = int.Parse(parts[parts.Length - 2]);
                        int ty = int.Parse(parts[parts.Length - 1]);
                        MoveSmooth(tx, ty, (int)delay, ct);

                        // Определяем кнопку (l, r или m)
                        string btn = "r";
                        if (action.Contains("_l")) btn = "l";
                        else if (action.Contains("_m")) btn = "m";

                        uint downFlag = btn == "l" ? MOUSEEVENTF_LEFTDOWN : (btn == "m" ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_RIGHTDOWN);
                        uint upFlag = btn == "l" ? MOUSEEVENTF_LEFTUP : (btn == "m" ? MOUSEEVENTF_MIDDLEUP : MOUSEEVENTF_RIGHTUP);

                        if (action.StartsWith("click_"))
                        {
                            // Клик не меняет глобальное состояние "зажатости", так как кнопка сразу отпускается
                            mouse_event(downFlag, 0, 0, 0, IntPtr.Zero);
                            Thread.Sleep(10);
                            mouse_event(upFlag, 0, 0, 0, IntPtr.Zero);
                        }
                        else if (action.EndsWith("down"))
                        {
                            mouse_event(downFlag, 0, 0, 0, IntPtr.Zero);
                            // Запоминаем, что кнопка мыши зажата
                            if (btn == "l") _isLeftDown = true;
                            else if (btn == "r") _isRightDown = true;
                            else if (btn == "m") _isMiddleDown = true;
                        }
                        else if (action.EndsWith("up"))
                        {
                            mouse_event(upFlag, 0, 0, 0, IntPtr.Zero);
                            // Убираем из состояния "зажатости"
                            if (btn == "l") _isLeftDown = false;
                            else if (btn == "r") _isRightDown = false;
                            else if (btn == "m") _isMiddleDown = false;
                        }
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
                        else if (action.EndsWith("down"))
                        {
                            keybd_event(vk, 0, 0, UIntPtr.Zero);
                            lock (_heldKeys) _heldKeys.Add(vk);
                        }
                        else if (action.EndsWith("up"))
                        {
                            keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                            lock (_heldKeys) _heldKeys.Remove(vk);
                        }

                        if (delay > 10) Thread.Sleep((int)delay);
                    }
                }
            }

            _isPlaying = false;
            ReleaseHeldKeysAndMouse();
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

        // Отпускаем ТОЛЬКО те клавиши и кнопки мыши, которые реально зажаты в данный момент
        private void ReleaseHeldKeysAndMouse()
        {
            // 1. Отпускаем клавиши клавиатуры
            lock (_heldKeys)
            {
                foreach (byte vk in _heldKeys)
                {
                    keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                }
                _heldKeys.Clear();
            }

            // 2. Отпускаем только зажатые кнопки мыши
            if (_isLeftDown)
            {
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
                _isLeftDown = false;
            }
            if (_isRightDown)
            {
                mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, IntPtr.Zero);
                _isRightDown = false;
            }
            if (_isMiddleDown)
            {
                mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, IntPtr.Zero);
                _isMiddleDown = false;
            }

            Logger.Log("Удерживаемые клавиши и кнопки мыши принудительно отпущены.");
        }
    }
}