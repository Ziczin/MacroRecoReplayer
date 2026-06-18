using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MacroRecoReplayer
{
    static class Program
    {
        public static TrayManager Tray;
        public static Recorder Recorder;
        public static Replayer Replayer;

        private static HashSet<int> _pressedKeys = new HashSet<int>();

        [STAThread]
        static void Main()
        {
            Logger.InitConsole();
            Logger.Log("Приложение запущено.");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            new HelpForm().ShowDialog();

            Tray = new TrayManager();
            Recorder = new Recorder();
            Replayer = new Replayer();

            InputHook.OnKeyDown += OnKeyDown;
            InputHook.OnKeyUp += OnKeyUp;
            InputHook.Initialize();

            var hiddenForm = new Form
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized,
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                Opacity = 0,
                Width = 0,
                Height = 0
            };

            Logger.Log("Запуск главного цикла сообщений...");
            Application.Run(hiddenForm);
        }

        private static void OnKeyDown(int vk)
        {
            lock (_pressedKeys) _pressedKeys.Add(vk);
            CheckHotkeys(vk);
        }

        private static void OnKeyUp(int vk)
        {
            lock (_pressedKeys) _pressedKeys.Remove(vk);
        }

        private static void CheckHotkeys(int currentVk)
        {
            // Проверяем ЛЮБОЙ Alt (общий 18, левый 164, правый 165)
            bool isAlt = _pressedKeys.Contains(18) || _pressedKeys.Contains(164) || _pressedKeys.Contains(165);

            // Проверяем ЛЮБОЙ Shift (общий 16, левый 160, правый 161)
            bool isShift = _pressedKeys.Contains(16) || _pressedKeys.Contains(160) || _pressedKeys.Contains(161);

            if (!isAlt) return;

            int targetKey = currentVk;
            if (targetKey == 16 || targetKey == 18 || targetKey == 160 || targetKey == 161 || targetKey == 164 || targetKey == 165) return;

            if (targetKey == 82) // R
            {
                InputHook.SuppressCurrentKey();
                if (isShift)
                {
                    Logger.Log("Хоткей: Остановить запись (Alt+Shift+R)");
                    Recorder.StopRecording();
                }
                else
                {
                    Logger.Log("Хоткей: Начать запись (Alt+R)");
                    Recorder.StartRecording();
                }
            }
            else if (targetKey == 80) // P
            {
                InputHook.SuppressCurrentKey();
                if (isShift)
                {
                    Logger.Log("Хоткей: Остановить воспроизведение (Alt+Shift+P)");
                    Replayer.StopPlayback();
                }
                else
                {
                    Logger.Log("Хоткей: Начать воспроизведение (Alt+P)");
                    Replayer.StartPlayback();
                }
            }
        }
    }
}