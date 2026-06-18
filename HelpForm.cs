using System;
using System.Drawing;
using System.Windows.Forms;

namespace MacroRecoReplayer
{
    public class HelpForm : Form
    {
        public HelpForm()
        {
            Text = "Справка по хоткеям";
            // Увеличили ширину на 20% (было 380, стало 460)
            Size = new Size(460, 260);
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var lbl = new Label
            {
                Text = "Управление:\n\n" +
                       "Alt + R         -> Начать запись\n" +
                       "Alt + Shift + R -> Остановить запись\n" +
                       "Alt + P         -> Запустить воспроизведение\n" +
                       "Alt + Shift + P -> Остановить воспроизведение\n\n" +
                       "Выбор скрипта и выход доступны в меню трея (ПКМ).",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };

            var btn = new Button
            {
                Text = "Понятно",
                Location = new Point(170, 180),
                Width = 100
            };

            btn.Click += (s, e) => Close();

            Controls.Add(lbl);
            Controls.Add(btn);
            AcceptButton = btn;
        }
    }
}