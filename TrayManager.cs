using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MacroRecoReplayer
{
    public class TrayManager
    {
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _menu;
        public string SelectedScript { get; private set; }

        public TrayManager()
        {
            _menu = new ContextMenuStrip();
            _menu.Opening += (s, e) => RefreshMenu();

            _trayIcon = new NotifyIcon
            {
                Icon = CreateIcon(),
                Text = "Macro Recorder",
                Visible = true,
                ContextMenuStrip = _menu
            };
        }

        private Icon CreateIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.FillEllipse(Brushes.Red, 2, 2, 12, 12);
            }
            return Icon.FromHandle(bmp.GetHicon());
        }

        private void RefreshMenu()
        {
            _menu.Items.Clear();
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            // Ищем только файлы с новым расширением
            string[] files = Directory.GetFiles(dir, "*.recore");

            if (files.Length == 0)
            {
                _menu.Items.Add(new ToolStripMenuItem("Скрипты не найдены") { Enabled = false });
            }
            else
            {
                foreach (string file in files)
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    var item = new ToolStripMenuItem(name);
                    if (name == SelectedScript) item.Font = new Font(item.Font, FontStyle.Bold);
                    item.Click += (s, e) => { SelectedScript = name; };
                    _menu.Items.Add(item);
                }
            }

            _menu.Items.Add(new ToolStripSeparator());

            var showFolderItem = new ToolStripMenuItem("> Показать скрипты");
            showFolderItem.Click += (s, e) =>
            {
                Process.Start("explorer.exe", dir);
            };
            _menu.Items.Add(showFolderItem);

            var helpItem = new ToolStripMenuItem("Помощь");
            helpItem.Click += (s, e) => new HelpForm().ShowDialog();
            _menu.Items.Add(helpItem);

            var exitItem = new ToolStripMenuItem("Выход");
            exitItem.Click += (s, e) => Application.Exit();
            _menu.Items.Add(exitItem);
        }
    }
}