using System;
using System.Drawing;
using System.Windows.Forms;

namespace MacroRecoReplayer
{
    public class SaveDialog : Form
    {
        public string ScriptName { get; private set; }

        public SaveDialog()
        {
            Text = "Сохранение скрипта";
            Size = new Size(300, 150);
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var lbl = new Label { Text = "Имя скрипта:", Location = new Point(10, 20), AutoSize = true };
            var txt = new TextBox { Location = new Point(10, 45), Width = 260, Text = "Без имени" };
            var btn = new Button { Text = "Сохранить", Location = new Point(10, 80), Width = 260 };

            btn.Click += (s, e) => {
                ScriptName = string.IsNullOrWhiteSpace(txt.Text) ? "Без имени" : txt.Text;
                DialogResult = DialogResult.OK;
                Close();
            };

            Controls.Add(lbl);
            Controls.Add(txt);
            Controls.Add(btn);
            AcceptButton = btn;
        }
    }
}