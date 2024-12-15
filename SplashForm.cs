using System.Drawing;
using System.Windows.Forms;

namespace BatRun
{
    public class SplashForm : Form
    {
        private readonly Label titleLabel;
        private readonly Label versionLabel;
        private readonly PictureBox logoBox;

        public SplashForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(400, 250);
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ShowInTaskbar = false;

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

            // Version
            versionLabel = new Label
            {
                Text = "Initializing...",
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.None,
                Size = new Size(Width, 30),
                Location = new Point(0, 170)
            };

            this.Controls.AddRange([logoBox, titleLabel, versionLabel]);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Dessiner une bordure
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle, 
                Color.FromArgb(45, 45, 48), 1, ButtonBorderStyle.Solid,
                Color.FromArgb(45, 45, 48), 1, ButtonBorderStyle.Solid,
                Color.FromArgb(45, 45, 48), 1, ButtonBorderStyle.Solid,
                Color.FromArgb(45, 45, 48), 1, ButtonBorderStyle.Solid);
        }

        public void UpdateStatus(string status)
        {
            if (versionLabel.InvokeRequired)
            {
                versionLabel.Invoke(new Action(() => versionLabel.Text = status));
            }
            else
            {
                versionLabel.Text = status;
            }
        }
    }
} 