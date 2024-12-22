using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace BatRun
{
    public partial class SplashForm : Form
    {
        private Label statusLabel;
        private Label titleLabel;
        private Label versionLabel;
        private PictureBox logoBox;
        private readonly LocalizedStrings strings;

        public SplashForm()
        {
            strings = new LocalizedStrings();
            
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

            // Title Label
            titleLabel = new Label
            {
                Text = LocalizedStrings.GetString("BatRun"),
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.None,
                AutoSize = false,
                Size = new Size(400, 50),
                Location = new Point(0, 110)
            };

            // Version Label
            versionLabel = new Label
            {
                Text = string.Format(LocalizedStrings.GetString("Version {0}"), Program.APP_VERSION),
                Font = new Font("Segoe UI", 12F, FontStyle.Regular),
                ForeColor = Color.LightGray,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.None,
                AutoSize = false,
                Size = new Size(400, 30),
                Location = new Point(0, 160)
            };

            // Status Label
            statusLabel = new Label
            {
                Text = LocalizedStrings.GetString("Initializing..."),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.None,
                AutoSize = false,
                Size = new Size(400, 30),
                Location = new Point(0, 190)
            };

            this.Controls.AddRange(new Control[] { logoBox, titleLabel, versionLabel, statusLabel });
        }

        public void UpdateStatus(string key)
        {
            if (statusLabel.InvokeRequired)
            {
                statusLabel.Invoke(new Action(() => statusLabel.Text = LocalizedStrings.GetString(key)));
            }
            else
            {
                statusLabel.Text = LocalizedStrings.GetString(key);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // Draw border
            using (Pen pen = new Pen(Color.FromArgb(45, 45, 48), 2))
            {
                e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, Width - 1, Height - 1));
            }
        }
    }
} 