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
            this.TopMost = true;

            // Logo/Icon
            logoBox = new PictureBox
            {
                Size = new Size(64, 64),
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point((this.Width - 64) / 2, 40),
                BackColor = Color.FromArgb(32, 32, 32)
            };

            try
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
                if (File.Exists(iconPath))
                {
                    using (var icon = new Icon(iconPath))
                    {
                        logoBox.Image = icon.ToBitmap();
                    }
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Error loading icon: {ex.Message}");
            }

            // Title Label
            titleLabel = new Label
            {
                Text = "BatRun",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Size = new Size(400, 50),
                Location = new Point(0, 110),
                BackColor = Color.FromArgb(32, 32, 32)
            };

            // Version Label
            versionLabel = new Label
            {
                Text = $"v{AppSettings.APP_VERSION}",
                Font = new Font("Segoe UI", 12F, FontStyle.Regular),
                ForeColor = Color.LightGray,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Size = new Size(400, 30),
                Location = new Point(0, 160),
                BackColor = Color.FromArgb(32, 32, 32)
            };

            // Status Label
            statusLabel = new Label
            {
                Text = LocalizedStrings.GetString("Initializing..."),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Size = new Size(400, 30),
                Location = new Point(0, 190),
                BackColor = Color.FromArgb(32, 32, 32)
            };

            this.Controls.AddRange(new Control[] { logoBox, titleLabel, versionLabel, statusLabel });
        }

        public void UpdateStatus(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStatus(message)));
                return;
            }

            statusLabel.Text = message;
            Application.DoEvents();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // Draw border
            using (var pen = new Pen(Color.FromArgb(45, 45, 48), 2))
            {
                e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, Width - 1, Height - 1));
            }
        }
    }
} 