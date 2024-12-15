using System.Drawing;
using System.Windows.Forms;

namespace BatRun
{
    public class HotkeySplashForm : Form
    {
        private readonly Label titleLabel;
        private readonly Label messageLabel;
        private readonly PictureBox logoBox;

        public HotkeySplashForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(400, 250);
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ShowInTaskbar = false;
            this.TopMost = true;

            // Logo/Icon
            logoBox = new PictureBox
            {
                Size = new Size(64, 64),
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point((this.Width - 64) / 2, 40)
            };

            try
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
                if (File.Exists(iconPath))
                {
                    logoBox.Image = new Icon(iconPath).ToBitmap();
                }
            }
            catch { }

            // Titre
            titleLabel = new Label
            {
                Text = "BatRun",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.None,
                Size = new Size(Width, 40),
                Location = new Point(0, 120)
            };

            // Message
            messageLabel = new Label
            {
                Text = System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("fr-") 
                    ? "Lancement de RetroBat..." 
                    : "Launching RetroBat...",
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 12),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.None,
                Size = new Size(Width, 30),
                Location = new Point(0, 170)
            };

            this.Controls.AddRange([logoBox, titleLabel, messageLabel]);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle, 
                Color.FromArgb(45, 45, 48), 1, ButtonBorderStyle.Solid,
                Color.FromArgb(45, 45, 48), 1, ButtonBorderStyle.Solid,
                Color.FromArgb(45, 45, 48), 1, ButtonBorderStyle.Solid,
                Color.FromArgb(45, 45, 48), 1, ButtonBorderStyle.Solid);
        }
    }
} 