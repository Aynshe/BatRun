using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.UI
{
    // EN: Hotkey splash screen. Mirrors the design of the startup SplashForm so we inherit
    // the same proven, flicker-free rendering. The form is owned/created on a dedicated STA
    // thread by HotkeySplashHost; it is shown, lives for the configured duration, then
    // Close()s itself, which exits the dedicated Application.Run(form).
    // FR: Splash hotkey. Reprend le design du SplashForm de démarrage afin de bénéficier du
    // rendu éprouvé et sans scintillement. Le form est créé/possédé sur un thread STA dédié
    // par HotkeySplashHost ; il est affiché, vit pendant la durée configurée, puis se Close()
    // lui-même, ce qui quitte l'Application.Run(form) dédié.
    public class HotkeySplashForm : Form
    {
        private readonly TimeSpan _duration;
        private PictureBox? logoBox;
        private Label? titleLabel;
        private Label? messageLabel;
        private Label? versionLabel;
        private System.Windows.Forms.Timer? _closeTimer;

        // EN: Parameterless ctor kept for designer compatibility / potential reuse.
        // FR: Ctors sans paramètre conservé pour compat designer / réutilisation éventuelle.
        public HotkeySplashForm() : this(TimeSpan.FromSeconds(2)) { }

        public HotkeySplashForm(TimeSpan duration)
        {
            _duration = duration;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            logoBox = new PictureBox
            {
                BackColor = Color.FromArgb(32, 32, 32),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(64, 64),
                Location = new Point(168, 40),
                TabIndex = 0,
                TabStop = false
            };

            titleLabel = new Label
            {
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                Size = new Size(400, 40),
                Location = new Point(0, 120),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                TabIndex = 1,
                Text = LocalizedStrings.GetString("BatRun")
            };

            messageLabel = new Label
            {
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 12F, FontStyle.Regular),
                Size = new Size(400, 30),
                Location = new Point(0, 160),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                TabIndex = 2,
                Text = LocalizedStrings.GetString("Launching RetroBat...")
            };

            versionLabel = new Label
            {
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Size = new Size(400, 30),
                Location = new Point(0, 190),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                TabIndex = 3,
                Text = GetRetroBatVersion()
            };

            Controls.Add(logoBox);
            Controls.Add(titleLabel);
            Controls.Add(messageLabel);
            Controls.Add(versionLabel);

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(32, 32, 32);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F);
            ClientSize = new Size(400, 250);
            // EN: Identical set as the startup SplashForm (which works) so the splash shows the
            // same way and does not appear in the taskbar.
            // FR: Même jeu de propriétés que le SplashForm de démarrage (qui marche) afin que le
            // splash s'affiche pareil et n'apparaisse pas dans la barre des tâches.
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            // EN: Window title contains "BatRun" so MinimizeActiveWindows excludes it.
            // FR: Le titre contient "BatRun" afin que MinimizeActiveWindows l'ignore.
            Text = "BatRun - Launching RetroBat...";
            Name = "HotkeySplashForm";

            ResumeLayout(false);

            LoadLogo();

            // EN: When first shown, start a Windows.Forms.Timer that will Close() the form
            // after _duration. Windows.Forms.Timer ticks on the form's own message pump.
            // FR: Au premier affichage, démarre un Windows.Forms.Timer qui Close() le form
            // après _duration. Windows.Forms.Timer tick sur la propre pompe du form.
            Shown += OnShown;

            FormClosed += OnClosed;
        }

        private void OnShown(object? sender, EventArgs e)
        {
            // EN: Schedule the close timer. TopMost=true already makes us appear above the
            // desktop; Calling Application.Run(form) in HotkeySplashHost means a tiny pump
            // is running so the timer will fire.
            // FR: Planifie le timer de fermeture. TopMost=true nous fait déjà apparaître au-dessus
            // du bureau ; l'Application.Run(form) dans HotkeySplashHost garantit une petite pompe
            // active pour que le timer se déclenche.
            _closeTimer = new System.Windows.Forms.Timer
            {
                Interval = Math.Max(200, (int)_duration.TotalMilliseconds)
            };
            _closeTimer.Tick += (s2, e2) =>
            {
                _closeTimer.Stop();
                _closeTimer.Dispose();
                _closeTimer = null;
                if (!IsDisposed)
                {
                    Close();
                }
            };
            _closeTimer.Start();
        }

        private void OnClosed(object? sender, FormClosedEventArgs e)
        {
            // EN: Best-effort cleanup of residual state.
            // FR: Nettoyage best-effort de l'état résiduel.
            try { _closeTimer?.Stop(); _closeTimer?.Dispose(); } catch { }
        }

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
            catch { }
            return "RetroBat";
        }

        private void LoadLogo()
        {
            try
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
                if (File.Exists(iconPath))
                {
                    using var icon = new Icon(iconPath);
                    logoBox!.Image = icon.ToBitmap();
                }
            }
            catch { }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // EN: Match SplashForm's OnPaint: a thin border around the borderless window.
            // FR: Comme SplashForm.OnPaint : une fine bordure autour de la fenêtre sans bordure.
            using var pen = new Pen(Color.FromArgb(45, 45, 48), 2);
            e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, Width - 1, Height - 1));
        }
    }
}
