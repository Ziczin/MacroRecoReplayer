using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MacroRecoReplayer
{
    public class RawEvent
    {
        public long Time;
        public string Type;
        public string Name;
        public int X, Y;
    }

    public class Recorder
    {
        [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT lpPoint);
        [StructLayout(LayoutKind.Sequential)] private struct POINT { public int x; public int y; }

        private List<RawEvent> _events = new List<RawEvent>();
        private Stopwatch _sw = new Stopwatch();
        private bool _isRecording = false;
        private bool _pending = false;
        private HashSet<string> _activeKeys = new HashSet<string>();

        public Recorder()
        {
            InputHook.OnKeyDown += (vk) => { if (_isRecording) RecordKey(vk, true); };
            InputHook.OnKeyUp += (vk) => { if (_isRecording) RecordKey(vk, false); };
            InputHook.OnMouseDown += (btn, x, y) => { if (_isRecording) RecordMouse(btn, x, y, true); };
            InputHook.OnMouseUp += (btn, x, y) => { if (_isRecording) RecordMouse(btn, x, y, false); };
        }

        public void StartRecording()
        {
            if (_isRecording || _pending) return;
            _pending = true;

            Logger.Log("Ожидание 0.5с до начала записи...");

            Task.Run(() => {
                Thread.Sleep(500);
                if (_pending)
                {
                    _pending = false;
                    _isRecording = true;
                    _events.Clear();
                    lock (_activeKeys) _activeKeys.Clear();
                    _sw.Restart();
                    Logger.Log("Запись начата.");
                }
            });
        }

        public void StopRecording()
        {
            if (!_isRecording && !_pending) return;
            _isRecording = false;
            _pending = false;
            _sw.Stop();

            var dlg = new SaveDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                string fileName = dlg.ScriptName.Replace(".txt", "").Replace(".recore", "");
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName + ".recore");

                List<string> lines = ProcessEvents();
                File.WriteAllLines(path, lines);
                Logger.Log($"Запись завершена. Файл сохранен: {fileName}.recore");
            }
        }

        private List<string> ProcessEvents()
        {
            List<string> lines = new List<string>();
            long lastTs = 0;
            bool isFirst = true;

            for (int i = 0; i < _events.Count; i++)
            {
                var e = _events[i];
                double delay = isFirst ? 0.5 : (e.Time - lastTs) / 1000.0;
                if (isFirst) isFirst = false;
                lastTs = e.Time;

                if (e.Type == "key_down" && e.Name == "ctrl" && i + 1 < _events.Count)
                {
                    var next = _events[i + 1];
                    if (next.Type == "key_up" && next.Name == "ctrl" && (next.Time - e.Time) < 200)
                    {
                        lines.Add($"{delay:F2} move {e.X} {e.Y}");
                        lastTs = next.Time;
                        i++;
                        continue;
                    }
                }

                if (e.Type == "mouse_down" && i + 1 < _events.Count)
                {
                    var next = _events[i + 1];
                    if (next.Type == "mouse_up" && next.Name == e.Name && (next.Time - e.Time) < 200)
                    {
                        lines.Add($"{delay:F2} click_{e.Name} {e.X} {e.Y}");
                        lastTs = next.Time;
                        i++;
                        continue;
                    }
                }

                if (e.Type == "key_down" && i + 1 < _events.Count)
                {
                    var next = _events[i + 1];
                    if (next.Type == "key_up" && next.Name == e.Name && (next.Time - e.Time) < 200)
                    {
                        lines.Add($"{delay:F2} key {e.Name}");
                        lastTs = next.Time;
                        i++;
                        continue;
                    }
                }

                if (e.Type.StartsWith("mouse_"))
                {
                    string act = e.Type.Split('_')[1];
                    lines.Add($"{delay:F2} mouse_{e.Name}_{act} {e.X} {e.Y}");
                }
                else
                {
                    lines.Add($"{delay:F2} {e.Type} {e.Name}");
                }
            }
            return lines;
        }

        private void RecordKey(int vk, bool down)
        {
            string name = InputHook.VkToName(vk);
            lock (_activeKeys)
            {
                if (down)
                {
                    // Если клавиша уже в списке (автоповтор), игнорируем
                    if (!_activeKeys.Add(name)) return;
                }
                else
                {
                    // Если клавиши нет в списке (лишний release), игнорируем
                    if (!_activeKeys.Remove(name)) return;
                }
            }

            int mx = 0, my = 0;
            if (name == "ctrl" && down)
            {
                GetCursorPos(out POINT p);
                mx = p.x; my = p.y;
            }

            _events.Add(new RawEvent
            {
                Time = _sw.ElapsedMilliseconds,
                Type = down ? "key_down" : "key_up",
                Name = name,
                X = mx,
                Y = my
            });
        }

        private void RecordMouse(int btn, int x, int y, bool down)
        {
            _events.Add(new RawEvent
            {
                Time = _sw.ElapsedMilliseconds,
                Type = down ? "mouse_down" : "mouse_up",
                Name = btn == 0 ? "l" : "r",
                X = x,
                Y = y
            });
        }
    }
}