using System.Drawing;
using System.Windows.Forms;

namespace BatRun
{
    public class HotkeySplashForm : Form
    {
        private readonly Label titleLabel;
        private readonly Label messageLabel;
        private readonly Label versionLabel;
        private readonly PictureBox logoBox;
        private readonly LocalizedStrings strings;

        private string GetRetroBatVersion()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\RetroBat");
                if (key != null)
                {
                    var path = key.GetValue("LatestKnownInstallPath") as string;
                    if (!string.IsNullOrEmpty(path))
                    {
                        string versionFile = Path.Combine(path, "system", "version.info");
                        if (File.Exists(versionFile))
                        {
                            string version = File.ReadAllText(versionFile).Trim();
                            return $"RetroBat {version}";
                        }
                    }
                }
            }
            catch
            {
                // Silently fail and return empty string
            }
            return "RetroBat";
        }

        public HotkeySplashForm()
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
                Text = LocalizedStrings.GetString("BatRun"),
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
                Text = LocalizedStrings.GetString("Launching RetroBat..."),
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 12),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.None,
                Size = new Size(Width, 30),
                Location = new Point(0, 160)
            };

            // Version de RetroBat
            versionLabel = new Label
            {
                Text = GetRetroBatVersion(),
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.None,
                Size = new Size(Width, 30),
                Location = new Point(0, 190)
            };

            this.Controls.AddRange(new Control[] { logoBox, titleLabel, versionLabel, messageLabel });
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