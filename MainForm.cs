using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading.Tasks;

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
        Task CheckForUpdates();
        string GetAppVersion();
        Task StartRetrobat();
    }

    public partial class MainForm : Form
    {
        private readonly IBatRunProgram mainProgram;
        private readonly Logger logger;
        private readonly IniFile config;
        private readonly UpdateChecker updateChecker;
        private Label? versionLabel;
        private Panel? updateStatusPanel;
        private Label? updateStatusLabel;

        public MainForm(IBatRunProgram program, Logger logger, IniFile config)
        {
            this.mainProgram = program;
            this.logger = logger;
            this.config = config;
            this.updateChecker = new UpdateChecker(logger, program.GetAppVersion());
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
                    using Icon icon = new(iconPath);
                    this.Icon = (Icon)icon.Clone();
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
            LocalizedStrings.LoadTranslations(); // Ensure translations are loaded
            var strings = new LocalizedStrings();

            // Configuration des couleurs et du style
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.ClientSize = new Size(800, 550); // Augmenté pour accommoder la box de mise à jour en bas

            // Création du panneau principal qui contiendra les boutons
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                RowCount = 2,
                ColumnCount = 2,
                BackColor = Color.FromArgb(32, 32, 32)
            };

            // Configuration des lignes et colonnes
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F)); // Colonne gauche
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F)); // Colonne droite
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 85F));      // Ligne principale
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 15F));      // Ligne de mise à jour

            // Panneau gauche (RetroBat Launcher et Configuration)
            var leftPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 10, 0)
            };

            leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            // Panneau des actions principales
            var actionPanel = CreateActionPanel(strings);
            leftPanel.Controls.Add(actionPanel, 0, 0);

            // Panneau de configuration
            var configPanel = CreateConfigPanel(strings);
            leftPanel.Controls.Add(configPanel, 0, 1);

            mainPanel.Controls.Add(leftPanel, 0, 0);

            // Panneau d'aide (même hauteur que les panneaux de gauche)
            var helpPanel = CreateHelpPanel(strings);
            helpPanel.Margin = new Padding(10, 0, 0, 0);
            mainPanel.Controls.Add(helpPanel, 1, 0);

            // Panneau de version et mise à jour (en bas sur toute la largeur)
            var versionPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Margin = new Padding(0, 10, 0, 0)
            };

            var versionLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(20, 10, 20, 10)
            };
            versionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            versionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // Label de version (à gauche)
            versionLabel = new Label
            {
                Text = $"v{mainProgram.GetAppVersion()}",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Left
            };
            versionLayout.Controls.Add(versionLabel, 0, 0);

            // Container pour le statut de mise à jour (à droite)
            var updateStatusContainer = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Dock = DockStyle.Right,
                Padding = new Padding(0, 5, 0, 0)
            };

            // Indicateur LED plus grand et plus visible
            updateStatusPanel = new Panel
            {
                Width = 20,
                Height = 20,
                BackColor = Color.Gray,
                Margin = new Padding(0, 2, 10, 0),
                Anchor = AnchorStyles.None
            };
            updateStatusPanel.Paint += (s, e) =>
            {
                using (var brush = new SolidBrush(updateStatusPanel.BackColor))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillEllipse(brush, 0, 0, 19, 19);
                }
            };

            updateStatusLabel = new Label
            {
                Text = LocalizedStrings.GetString("Checking for updates..."),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.None
            };

            updateStatusContainer.Controls.Add(updateStatusPanel, 0, 0);
            updateStatusContainer.Controls.Add(updateStatusLabel, 1, 0);
            versionLayout.Controls.Add(updateStatusContainer, 1, 0);

            versionPanel.Controls.Add(versionLayout);
            mainPanel.SetColumnSpan(versionPanel, 2); // Étendre sur les deux colonnes
            mainPanel.Controls.Add(versionPanel, 0, 1);

            this.Controls.Add(mainPanel);

            // Démarrer la vérification des mises à jour
            CheckForUpdatesOnStartup();
        }

        private async void CheckForUpdatesOnStartup()
        {
            try
            {
                if (updateStatusPanel != null && updateStatusLabel != null)
                {
                    var strings = new LocalizedStrings();
                    updateStatusPanel.BackColor = Color.Gray;
                    updateStatusLabel.Text = strings.CheckingForUpdatesProgress;
                    
                    var result = await updateChecker.CheckForUpdates();
                    
                    if (!result.HasInternetConnection)
                    {
                        updateStatusPanel.BackColor = Color.Orange;
                        updateStatusLabel.Text = "No internet connection";
                        return;
                    }
                    
                    if (result.UpdateAvailable)
                    {
                        updateStatusPanel.BackColor = Color.Red;
                        updateStatusLabel.Text = strings.UpdateAvailable;
                        updateStatusLabel.Cursor = Cursors.Hand;
                        updateStatusLabel.Click += async (s, e) => await CheckForUpdatesAsync();
                        updateStatusPanel.Cursor = Cursors.Hand;
                        updateStatusPanel.Click += async (s, e) => await CheckForUpdatesAsync();
                    }
                    else
                    {
                        updateStatusPanel.BackColor = Color.LimeGreen;
                        updateStatusLabel.Text = strings.NoUpdatesAvailable;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error checking for updates on startup: {ex.Message}", ex);
                if (updateStatusPanel != null && updateStatusLabel != null)
                {
                    var strings = new LocalizedStrings();
                    updateStatusPanel.BackColor = Color.LimeGreen;
                    updateStatusLabel.Text = strings.UpdateCheckFailed;
                }
            }
        }

        private Panel CreateActionPanel(LocalizedStrings strings)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Margin = new Padding(0, 0, 0, 5)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Padding = new Padding(10),
                RowCount = 3,
                ColumnCount = 1,
                Height = 170,
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
                Margin = new Padding(0, 5, 0, 0)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Padding = new Padding(10),
                RowCount = 3,
                ColumnCount = 1,
                Height = 170,
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
                Margin = new Padding(0)
            };

            // TableLayoutPanel principal pour organiser les deux panneaux
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.FromArgb(32, 32, 32),
                Margin = new Padding(0)
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));  // Panneau d'aide
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));  // Panneau Shell

            // Panneau d'aide
            var helpBox = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Margin = new Padding(0, 0, 0, 10)
            };

            var helpLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                RowCount = 4,
                ColumnCount = 1,
                AutoSize = false
            };

            // Configuration des lignes du panneau d'aide
            helpLayout.RowStyles.Clear();
            helpLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Titre
            helpLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));  // Espace pour centrer
            helpLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));  // Zone des boutons (3 * 60)
            helpLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));  // Espace pour centrer

            // Titre du panneau d'aide
            var helpTitleLabel = new Label
            {
                Text = strings.HelpAndSupport,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = false
            };
            helpLayout.Controls.Add(helpTitleLabel, 0, 0);

            // Panel pour contenir les boutons d'aide
            var buttonsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Height = 180,
                AutoSize = false
            };

            buttonsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            buttonsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            buttonsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

            // Boutons d'aide
            var logsButton = CreateStyledButton(strings.ViewLogs, Color.FromArgb(87, 87, 38));
            logsButton.Click += (s, e) => mainProgram.SafeExecute(mainProgram.OpenLogFile);
            buttonsPanel.Controls.Add(logsButton, 0, 0);

            var errorLogsButton = CreateStyledButton(strings.ViewErrorLogs, Color.FromArgb(87, 87, 38));
            errorLogsButton.Click += (s, e) => mainProgram.SafeExecute(mainProgram.OpenErrorLogFile);
            buttonsPanel.Controls.Add(errorLogsButton, 0, 1);

            var aboutButton = CreateStyledButton(strings.About, Color.FromArgb(87, 87, 38));
            aboutButton.Click += (s, e) => mainProgram.SafeExecute(() => mainProgram.ShowAbout(null, EventArgs.Empty));
            buttonsPanel.Controls.Add(aboutButton, 0, 2);

            helpLayout.Controls.Add(buttonsPanel, 0, 2);
            helpBox.Controls.Add(helpLayout);

            // Panneau Shell dans son propre cadre
            var shellBoxOuter = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(32, 32, 32),
                Margin = new Padding(0)
            };

            var shellBox = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Margin = new Padding(0)
            };

            var shellLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                RowCount = 3,
                ColumnCount = 1,
                AutoSize = false
            };

            // Configuration des lignes du panneau Shell
            shellLayout.RowStyles.Clear();
            shellLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));  // Espace au-dessus
            shellLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Bouton
            shellLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));  // Espace en-dessous

            // Bouton Shell avec une nouvelle couleur
            var shellButton = CreateStyledButton("Shell Launcher", Color.FromArgb(104, 33, 122));
            shellButton.Click += (s, e) => mainProgram.SafeExecute(() => {
                var shellConfigForm = new ShellConfigurationForm(config, logger);
                shellConfigForm.ShowDialog();
            });
            shellLayout.Controls.Add(shellButton, 0, 1);

            shellBox.Controls.Add(shellLayout);
            shellBoxOuter.Controls.Add(shellBox);

            // Ajouter les panneaux au layout principal
            mainLayout.Controls.Add(helpBox, 0, 0);
            mainLayout.Controls.Add(shellBoxOuter, 0, 1);

            panel.Controls.Add(mainLayout);
            return panel;
        }

        private static Button CreateStyledButton(string text, Color baseColor)
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

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                var strings = new LocalizedStrings();
                var progressForm = new Form
                {
                    Text = strings.CheckingForUpdates,
                    Size = new Size(300, 100),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterScreen,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Color.FromArgb(32, 32, 32),
                    ForeColor = Color.White
                };

                var progressLabel = new Label
                {
                    Text = strings.CheckingForUpdatesProgress,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };

                progressForm.Controls.Add(progressLabel);
                progressForm.Show();

                var result = await updateChecker.CheckForUpdates();
                progressForm.Close();

                if (!result.HasInternetConnection)
                {
                    MessageBox.Show(
                        "No internet connection",
                        "Update Check Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                if (result.UpdateAvailable)
                {
                    var dialogResult = MessageBox.Show(
                        string.Format(strings.NewVersionAvailable, result.LatestVersion),
                        strings.UpdateAvailable,
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (dialogResult == DialogResult.Yes)
                    {
                        var downloadProgress = new Progress<int>(percent =>
                        {
                            progressLabel.Text = string.Format(strings.DownloadingUpdateProgress, percent);
                        });

                        progressForm = new Form
                        {
                            Text = strings.DownloadingUpdate,
                            Size = new Size(300, 100),
                            FormBorderStyle = FormBorderStyle.FixedDialog,
                            StartPosition = FormStartPosition.CenterScreen,
                            MaximizeBox = false,
                            MinimizeBox = false,
                            BackColor = Color.FromArgb(32, 32, 32),
                            ForeColor = Color.White
                        };

                        progressLabel = new Label
                        {
                            Text = strings.StartingDownload,
                            Dock = DockStyle.Fill,
                            TextAlign = ContentAlignment.MiddleCenter
                        };

                        progressForm.Controls.Add(progressLabel);
                        progressForm.Show();

                        var success = await updateChecker.DownloadAndInstallUpdate(result.DownloadUrl, downloadProgress);
                        progressForm.Close();

                        if (!success)
                        {
                            MessageBox.Show(
                                strings.UpdateFailedMessage,
                                strings.UpdateFailed,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show(
                        strings.LatestVersion,
                        strings.NoUpdatesAvailable,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error checking for updates: {ex.Message}", ex);
                MessageBox.Show(
                    LocalizedStrings.GetString("Failed to check for updates. Please try again later."),
                    LocalizedStrings.GetString("Update check failed"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
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