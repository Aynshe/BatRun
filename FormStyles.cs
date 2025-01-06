using System.Drawing;
using System.Windows.Forms;

namespace BatRun
{
    public static class FormStyles
    {
        // Couleurs
        public static Color BackgroundColor = Color.FromArgb(32, 32, 32);
        public static Color ForegroundColor = Color.White;
        public static Color ButtonBackColor = Color.FromArgb(45, 45, 48);
        public static Color ButtonHoverColor = Color.FromArgb(62, 62, 66);
        public static Color GroupBoxColor = Color.FromArgb(45, 45, 48);
        public static Color TextBoxBackColor = Color.FromArgb(45, 45, 48);
        public static Color ComboBoxBackColor = Color.FromArgb(45, 45, 48);
        public static Color BorderColor = Color.FromArgb(67, 67, 70);

        // Appliquer le style sombre à un formulaire
        public static void ApplyDarkStyle(Form form)
        {
            form.BackColor = BackgroundColor;
            form.ForeColor = ForegroundColor;

            foreach (Control control in form.Controls)
            {
                ApplyDarkStyleToControl(control);
            }
        }

        // Appliquer le style aux contrôles
        private static void ApplyDarkStyleToControl(Control control)
        {
            control.ForeColor = ForegroundColor;

            switch (control)
            {
                case Button button:
                    StyleButton(button);
                    break;
                case TextBox textBox:
                    StyleTextBox(textBox);
                    break;
                case ComboBox comboBox:
                    StyleComboBox(comboBox);
                    break;
                case GroupBox groupBox:
                    StyleGroupBox(groupBox);
                    foreach (Control childControl in groupBox.Controls)
                    {
                        ApplyDarkStyleToControl(childControl);
                    }
                    break;
                case NumericUpDown numericUpDown:
                    StyleNumericUpDown(numericUpDown);
                    break;
                case CheckBox checkBox:
                    StyleCheckBox(checkBox);
                    break;
                case Label label:
                    label.ForeColor = ForegroundColor;
                    break;
            }
        }

        private static void StyleButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = ButtonBackColor;
            button.FlatAppearance.BorderColor = BorderColor;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = ButtonHoverColor;
            button.FlatAppearance.MouseDownBackColor = ButtonBackColor;
            button.UseVisualStyleBackColor = false;
        }

        private static void StyleTextBox(TextBox textBox)
        {
            textBox.BackColor = TextBoxBackColor;
            textBox.ForeColor = ForegroundColor;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }

        private static void StyleComboBox(ComboBox comboBox)
        {
            comboBox.BackColor = ComboBoxBackColor;
            comboBox.ForeColor = ForegroundColor;
            comboBox.FlatStyle = FlatStyle.Flat;
        }

        private static void StyleGroupBox(GroupBox groupBox)
        {
            groupBox.ForeColor = ForegroundColor;
            groupBox.Paint += (sender, e) =>
            {
                var g = e.Graphics;
                var pen = new Pen(BorderColor);
                var bounds = new Rectangle(0, 0, groupBox.Width - 1, groupBox.Height - 1);
                g.DrawRectangle(pen, bounds);
            };
        }

        private static void StyleNumericUpDown(NumericUpDown numericUpDown)
        {
            numericUpDown.BackColor = TextBoxBackColor;
            numericUpDown.ForeColor = ForegroundColor;
            numericUpDown.BorderStyle = BorderStyle.FixedSingle;
        }

        private static void StyleCheckBox(CheckBox checkBox)
        {
            checkBox.FlatStyle = FlatStyle.Flat;
            checkBox.FlatAppearance.BorderColor = BorderColor;
            checkBox.FlatAppearance.CheckedBackColor = ButtonBackColor;
        }
    }
} 