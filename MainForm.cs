using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace BatRun
{
    public interface IBatRunProgram
    {
        void LaunchEmulationStation();
        void LaunchBatGui();
        void ShowConfigWindow();
        void OpenMappingConfiguration();
        void OpenLogFile();
        void OpenErrorLogFile();
        void ShowAbout(object? sender, EventArgs e);
        void SafeExecute(Action action);
    }

    public partial class MainForm : Form
    {
        private readonly IBatRunProgram mainProgram;
        private readonly Logger logger;
        private readonly IniFile config;

        public MainForm(IBatRunProgram program, Logger logger, IniFile config)
        {
            this.mainProgram = program;
            this.logger = logger;
            this.config = config;
            InitializeComponent();

            // Configurer les propriétés de la fenêtre
            this.ShowInTaskbar = true;
            this.MinimizeBox = true;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;

            // Configurer l'icône de la fenêtre
            try
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
                if (File.Exists(iconPath))
                {
                    using (var icon = new Icon(iconPath))
                    {
                        this.Icon = (Icon)icon.Clone();
                    }
                }
                else
                {
                    logger.LogError($"Icon file not found at: {iconPath}");
                    // Essayer d'utiliser l'icône du programme principal
                    if (program is Form programForm && programForm.Icon != null)
                    {
                        this.Icon = (Icon)programForm.Icon.Clone();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error loading icon: {ex.Message}", ex);
                // Essayer d'utiliser l'icône du programme principal en cas d'erreur
                if (program is Form programForm && programForm.Icon != null)
                {
                    this.Icon = (Icon)programForm.Icon.Clone();
                }
            }

            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            var strings = LocalizedStrings.GetStrings();

            // Configuration des couleurs et du style
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.ClientSize = new Size(400, 800); // Augmentation de la hauteur de la fenêtre

            // Création du panneau principal qui contiendra les boutons
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                RowCount = 3,
                ColumnCount = 1,
                BackColor = Color.FromArgb(32, 32, 32)
            };

            // Configuration des lignes avec des hauteurs égales
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 230)); // Zone RetroBat Launcher
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 230)); // Zone Configuration
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 260)); // Zone Help & Support (plus haute pour 3 boutons)

            // Panneau des actions principales
            var actionPanel = CreateActionPanel(strings);
            mainPanel.Controls.Add(actionPanel, 0, 0);

            // Panneau de configuration
            var configPanel = CreateConfigPanel(strings);
            mainPanel.Controls.Add(configPanel, 0, 1);

            // Panneau d'aide
            var helpPanel = CreateHelpPanel(strings);
            mainPanel.Controls.Add(helpPanel, 0, 2);

            this.Controls.Add(mainPanel);
        }

        private Panel CreateActionPanel(LocalizedStrings strings)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Margin = new Padding(0, 0, 0, 10)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Padding = new Padding(10),
                RowCount = 3,
                ColumnCount = 1,
                Height = 170,  // Hauteur fixe : 50 (titre) + 2 * 60 (boutons)
                AutoSize = false
            };

            // Configuration des lignes avec des hauteurs fixes
            layout.RowStyles.Clear();
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Titre
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // Bouton 1
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // Bouton 2

            // Titre du panneau
            var titleLabel = new Label
            {
                Text = "RetroBat Launcher",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = false,
                Height = 50
            };
            layout.Controls.Add(titleLabel, 0, 0);

            // Boutons principaux
            var launchButton = CreateStyledButton(strings.OpenEmulationStation, Color.FromArgb(0, 122, 204));
            launchButton.Click += (s, e) => mainProgram.SafeExecute(mainProgram.LaunchEmulationStation);
            layout.Controls.Add(launchButton, 0, 1);

            var batGuiButton = CreateStyledButton(strings.LaunchBatGui, Color.FromArgb(0, 122, 204));
            batGuiButton.Click += (s, e) => mainProgram.SafeExecute(mainProgram.LaunchBatGui);
            layout.Controls.Add(batGuiButton, 0, 2);

            panel.Controls.Add(layout);
            return panel;
        }

        private Panel CreateConfigPanel(LocalizedStrings strings)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Margin = new Padding(0, 10, 0, 10)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Padding = new Padding(10),
                RowCount = 3,
                ColumnCount = 1,
                Height = 170,  // Hauteur fixe : 50 (titre) + 2 * 60 (boutons)
                AutoSize = false
            };

            // Configuration des lignes avec des hauteurs fixes
            layout.RowStyles.Clear();
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Titre
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // Bouton 1
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // Bouton 2

            // Titre du panneau
            var titleLabel = new Label
            {
                Text = strings.Configuration,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = false,
                Height = 50
            };
            layout.Controls.Add(titleLabel, 0, 0);

            // Boutons de configuration
            var settingsButton = CreateStyledButton(strings.GeneralSettings, Color.FromArgb(104, 33, 122));
            settingsButton.Click += (s, e) => mainProgram.SafeExecute(mainProgram.ShowConfigWindow);
            layout.Controls.Add(settingsButton, 0, 1);

            var mappingButton = CreateStyledButton(strings.ControllerMappings, Color.FromArgb(104, 33, 122));
            mappingButton.Click += (s, e) => mainProgram.SafeExecute(mainProgram.OpenMappingConfiguration);
            layout.Controls.Add(mappingButton, 0, 2);

            panel.Controls.Add(layout);
            return panel;
        }

        private Panel CreateHelpPanel(LocalizedStrings strings)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Margin = new Padding(0, 10, 0, 0)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Padding = new Padding(10),
                RowCount = 4,
                ColumnCount = 1,
                Height = 230,
                AutoSize = false
            };

            // Configuration des lignes avec des hauteurs fixes
            layout.RowStyles.Clear();
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Titre
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // Bouton 1
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // Bouton 2
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // Bouton 3

            // Titre du panneau
            var titleLabel = new Label
            {
                Text = strings.HelpAndSupport,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = false,
                Height = 50
            };
            layout.Controls.Add(titleLabel, 0, 0);

            // Boutons d'aide avec hauteurs fixes
            var logsButton = CreateStyledButton(strings.ViewLogs, Color.FromArgb(87, 87, 38));
            logsButton.Click += (s, e) => mainProgram.SafeExecute(mainProgram.OpenLogFile);
            layout.Controls.Add(logsButton, 0, 1);

            var errorLogsButton = CreateStyledButton(strings.ViewErrorLogs, Color.FromArgb(87, 87, 38));
            errorLogsButton.Click += (s, e) => mainProgram.SafeExecute(mainProgram.OpenErrorLogFile);
            layout.Controls.Add(errorLogsButton, 0, 2);

            var aboutButton = CreateStyledButton(strings.About, Color.FromArgb(87, 87, 38));
            aboutButton.Click += (s, e) => mainProgram.SafeExecute(() => mainProgram.ShowAbout(null, EventArgs.Empty));
            layout.Controls.Add(aboutButton, 0, 3);

            panel.Controls.Add(layout);
            return panel;
        }

        private Button CreateStyledButton(string text, Color baseColor)
        {
            var button = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = baseColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Padding = new Padding(10),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false,
                AutoSize = false,
                Height = 60,
                AutoEllipsis = true
            };

            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(baseColor);
            button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(baseColor);

            return button;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                base.OnFormClosing(e);
            }
        }
    }
} 