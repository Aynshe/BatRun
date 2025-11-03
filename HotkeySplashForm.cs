using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace BatRun
{
    public partial class HotkeySplashForm : Form
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
            }
            return "RetroBat";
        }

        public HotkeySplashForm()
        {
            strings = new LocalizedStrings();
            
            InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ShowInTaskbar = false;
            this.TopMost = true;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this.Controls.Add(mainLayout);

            logoBox = new PictureBox
            {
                Size = new Size(64, 64),
                SizeMode = PictureBoxSizeMode.Zoom,
                Anchor = AnchorStyles.None
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

            titleLabel = new Label
            {
                Text = LocalizedStrings.GetString("BatRun"),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            messageLabel = new Label
            {
                Text = LocalizedStrings.GetString("Launching RetroBat..."),
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 12),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            versionLabel = new Label
            {
                Text = GetRetroBatVersion(),
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            var centerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                AutoSize = true
            };
            centerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            centerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            centerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            centerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            centerLayout.Controls.Add(logoBox, 0, 0);
            centerLayout.Controls.Add(titleLabel, 0, 1);
            centerLayout.Controls.Add(messageLabel, 0, 2);
            centerLayout.Controls.Add(versionLabel, 0, 3);

            mainLayout.Controls.Add(centerLayout, 0, 1);
            mainLayout.SetRowSpan(centerLayout, 2);
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