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
    // EN: Splash screen shown during application startup.
    // FR: Écran de démarrage affiché lors du lancement de l'application.
    public partial class SplashForm : Form
    {
        private readonly LocalizedStrings strings;

        public SplashForm()
        {
            strings = new LocalizedStrings();
            InitializeComponent();

            // EN: Set runtime values not available at design time.
            // FR: Valeurs runtime non disponibles au moment du design.
            versionLabel.Text = $"v{Batrun.APP_VERSION}";
            statusLabel.Text  = LocalizedStrings.GetString("Initializing...");

            LoadLogo();
        }

        // EN: Attempts to load the application icon as the splash logo.
        // FR: Tente de charger l'icône de l'application comme logo du splash.
        private void LoadLogo()
        {
            try
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
                if (File.Exists(iconPath))
                {
                    using var icon = new Icon(iconPath);
                    logoBox.Image = icon.ToBitmap();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading icon: {ex.Message}");
            }
        }

        // EN: Updates the status label text (thread-safe).
        // FR: Met à jour le texte du label de statut (thread-safe).
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

        // EN: Draws a border around the borderless splash window.
        // FR: Dessine une bordure autour de la fenêtre splash sans bordure.
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Color.FromArgb(45, 45, 48), 2);
            e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, Width - 1, Height - 1));
        }
    }
}

