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
            Size = new Size(520, 400);
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
                       "Дополнительно при записи:\n" +
                       "Ctrl (одиночно) -> Движение мыши без клика (move)\n\n" +
                       "Режимы воспроизведения (первая строка скрипта):\n" +
                       "loop            -> Бесконечный цикл\n" +
                       "repeat x        -> Повторить макрос x раз\n\n" +
                       "Выбор скрипта и выход доступны в меню трея (ПКМ).",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };

            var btn = new Button
            {
                Text = "Понятно",
                Location = new Point(200, 320),
                Width = 100
            };

            btn.Click += (s, e) => Close();

            Controls.Add(lbl);
            Controls.Add(btn);
            AcceptButton = btn;
        }
    }
}