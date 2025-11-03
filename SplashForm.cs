using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System;

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
                Anchor = AnchorStyles.None,
                BackColor = Color.Transparent
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

            titleLabel = new Label
            {
                Text = "BatRun",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            versionLabel = new Label
            {
                Text = $"v{Batrun.APP_VERSION}",
                Font = new Font("Segoe UI", 12F, FontStyle.Regular),
                ForeColor = Color.LightGray,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            statusLabel = new Label
            {
                Text = LocalizedStrings.GetString("Initializing..."),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
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
            centerLayout.Controls.Add(versionLabel, 0, 2);
            centerLayout.Controls.Add(statusLabel, 0, 3);

            mainLayout.Controls.Add(centerLayout, 0, 1);
            mainLayout.SetRowSpan(centerLayout, 2);
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
            
            using (var pen = new Pen(Color.FromArgb(45, 45, 48), 2))
            {
                e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, Width - 1, Height - 1));
            }
        }
    }
}