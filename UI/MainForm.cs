using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;

using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.UI
{
    // EN: Interface defining the contract for the main BatRun program logic.
    // FR: Interface définissant le contrat pour la logique principale du programme BatRun.
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
        Task StartRetrobat(bool suppressFocus = false);
        string GetRetrobatPath();
        ArcadeManager? ArcadeManager { get; }
    }

    // EN: Main form of BatRun — UI skeleton lives in MainForm.Designer.cs.
    // FR: Formulaire principal de BatRun — le squelette UI est dans MainForm.Designer.cs.
    public partial class MainForm : Form
    {
        private readonly IBatRunProgram mainProgram;
        private readonly Logger         logger;
        private readonly IniFile        config;
        private readonly UpdateChecker  updateChecker;

        // ─────────────────────────────────────────────────────────────────
        // Constructor / Constructeur
        // ─────────────────────────────────────────────────────────────────
        public MainForm(IBatRunProgram program, Logger logger, IniFile config)
        {
            this.mainProgram   = program;
            this.logger        = logger;
            this.config        = config;
            this.updateChecker = new UpdateChecker(logger, program.GetAppVersion());

            // EN: Initialize all designer-managed controls.
            // FR: Initialise tous les contrôles gérés par le Designer.
            InitializeComponent();

            // EN: Fixed window behaviour.
            // FR: Comportement de fenêtre fixe.
            this.ShowInTaskbar  = true;
            this.MinimizeBox    = true;
            this.MaximizeBox    = false;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;

            LoadWindowIcon(program);
            ApplyLocalization();
            InitializeVersionBar();
            SetupUpdateStatusPaint();
            CheckForUpdatesOnStartup();
        }

        // ─────────────────────────────────────────────────────────────────
        // Icon loading / Chargement de l'icône
        // ─────────────────────────────────────────────────────────────────
        private void LoadWindowIcon(IBatRunProgram program)
        {
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
                    if (program is Form f && f.Icon != null)
                        this.Icon = (Icon)f.Icon.Clone();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error loading icon: {ex.Message}", ex);
                if (program is Form f && f.Icon != null)
                    this.Icon = (Icon)f.Icon.Clone();
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Localization — apply translated texts to designer controls
        // Localisation — applique les textes traduits aux contrôles designer
        // ─────────────────────────────────────────────────────────────────
        private void ApplyLocalization()
        {
            LocalizedStrings.LoadTranslations();
            var s = new LocalizedStrings();

            // Left panel — Launcher
            this.lblLauncherTitle.Text      = "RetroBat Launcher";
            this.btnLaunchRetroBat.Text     = s.OpenEmulationStation;
            this.btnLaunchBatGui.Text       = s.LaunchBatGui;

            // Left panel — Configuration
            this.lblConfigTitle.Text            = s.Configuration;
            this.btnGeneralSettings.Text        = s.GeneralSettings;
            this.btnControllerMappings.Text     = s.ControllerMappings;

            // Right panel — Support
            this.lblHelpTitle.Text          = s.HelpAndSupport;
            this.btnViewLogs.Text           = s.ViewLogs;
            this.btnViewErrorLogs.Text      = s.ViewErrorLogs;
            this.btnAbout.Text              = s.About;

            // Right panel — Tools / Shell (static texts, no localization needed)
            this.btnToolsPlugins.Text  = "Tools / Plugins GitHub";
            this.btnShellLauncher.Text = "Shell Launcher";
        }

        // ─────────────────────────────────────────────────────────────────
        // Version bar initialization / Initialisation de la barre de version
        // ─────────────────────────────────────────────────────────────────
        private void InitializeVersionBar()
        {
            this.lblVersion.Text = $"v{mainProgram.GetAppVersion()}";

            var s = new LocalizedStrings();
            this.lblUpdateStatus.Text = LocalizedStrings.GetString("Checking for updates...");
        }

        // ─────────────────────────────────────────────────────────────────
        // Paint the update LED as a smooth circle
        // Dessine le LED de mise à jour comme un cercle lisse
        // ─────────────────────────────────────────────────────────────────
        private void SetupUpdateStatusPaint()
        {
            this.pnlUpdateIndicator.Paint += (s, e) =>
            {
                using var brush = new SolidBrush(this.pnlUpdateIndicator.BackColor);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillEllipse(brush, 0, 0, 19, 19);
            };
        }

        // ─────────────────────────────────────────────────────────────────
        // Update check on startup / Vérification de mises à jour au démarrage
        // ─────────────────────────────────────────────────────────────────
        private async void CheckForUpdatesOnStartup()
        {
            try
            {
                var s = new LocalizedStrings();

                // [BATRUN-FIX]: Ensure AutoUpdate exists in config.ini, default to true
                // FR: S'assurer que AutoUpdate existe dans config.ini, défaut à true
                if (string.IsNullOrEmpty(config.ReadValue("Arcade", "AutoUpdate", "")))
                {
                    config.WriteValue("Arcade", "AutoUpdate", "true");
                    logger.LogInfo("Added default AutoUpdate=true to config.ini");
                }

                // [BATRUN-FIX]: Wait a bit to ensure other services are ready
                await Task.Delay(500);

                bool autoUpdateEnabled = config.ReadBool("Arcade", "AutoUpdate", false);
                if (!autoUpdateEnabled)
                {
                    this.pnlUpdateIndicator.BackColor = Color.Gray;
                    this.lblUpdateStatus.Text         = LocalizedStrings.GetString("Updates disabled");
                    return;
                }

                this.pnlUpdateIndicator.BackColor = Color.Gray;
                this.lblUpdateStatus.Text         = s.CheckingForUpdatesProgress;

                var result = await updateChecker.CheckForUpdates();

                if (!result.HasInternetConnection)
                {
                    this.pnlUpdateIndicator.BackColor = Color.Orange;
                    this.lblUpdateStatus.Text         = "No internet connection";
                    return;
                }

                if (result.UpdateAvailable)
                {
                    this.pnlUpdateIndicator.BackColor = Color.Red;
                    this.lblUpdateStatus.Text         = s.UpdateAvailable;
                    this.lblUpdateStatus.Cursor       = Cursors.Hand;
                    this.pnlUpdateIndicator.Cursor    = Cursors.Hand;
                    this.lblUpdateStatus.Click       += async (_, _) => await CheckForUpdatesAsync();
                    this.pnlUpdateIndicator.Click    += async (_, _) => await CheckForUpdatesAsync();
                }
                else
                {
                    this.pnlUpdateIndicator.BackColor = Color.LimeGreen;
                    this.lblUpdateStatus.Text         = s.NoUpdatesAvailable;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error checking for updates on startup: {ex.Message}", ex);
                var s = new LocalizedStrings();
                this.pnlUpdateIndicator.BackColor = Color.LimeGreen;
                this.lblUpdateStatus.Text         = s.UpdateCheckFailed;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Button event handlers / Gestionnaires d'événements des boutons
        // ─────────────────────────────────────────────────────────────────

        private void BtnLaunchRetroBat_Click(object sender, EventArgs e)
            => mainProgram.SafeExecute(mainProgram.LaunchEmulationStation);

        private void BtnLaunchBatGui_Click(object sender, EventArgs e)
            => mainProgram.SafeExecute(mainProgram.LaunchBatGui);

        private void BtnGeneralSettings_Click(object sender, EventArgs e)
            => mainProgram.SafeExecute(mainProgram.ShowConfigWindow);

        private void BtnControllerMappings_Click(object sender, EventArgs e)
            => mainProgram.SafeExecute(mainProgram.OpenMappingConfiguration);

        private void BtnViewLogs_Click(object sender, EventArgs e)
            => mainProgram.SafeExecute(mainProgram.OpenLogFile);

        private void BtnViewErrorLogs_Click(object sender, EventArgs e)
            => mainProgram.SafeExecute(mainProgram.OpenErrorLogFile);

        private void BtnAbout_Click(object sender, EventArgs e)
            => mainProgram.SafeExecute(() => mainProgram.ShowAbout(null, EventArgs.Empty));

        private void BtnToolsPlugins_Click(object sender, EventArgs e)
        {
            using var pluginsForm = new PluginsForm(mainProgram, logger, config);
            pluginsForm.ShowDialog(this);
        }

        private void BtnShellLauncher_Click(object sender, EventArgs e)
        {
            mainProgram.SafeExecute(() =>
            {
                var shellConfigForm = new ShellConfigurationForm(config, logger, null, mainProgram);
                shellConfigForm.ShowDialog();
            });
        }

        // ─────────────────────────────────────────────────────────────────
        // Full update check (manual) / Vérification complète (manuelle)
        // ─────────────────────────────────────────────────────────────────
        private async Task CheckForUpdatesAsync()
        {
            try
            {
                var s = new LocalizedStrings();
                var progressForm = new Form
                {
                    Text            = s.CheckingForUpdates,
                    Size            = new Size(300, 100),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition   = FormStartPosition.CenterScreen,
                    MaximizeBox     = false,
                    MinimizeBox     = false,
                    BackColor       = Color.FromArgb(32, 32, 32),
                    ForeColor       = Color.White
                };

                var progressLabel = new Label
                {
                    Text      = s.CheckingForUpdatesProgress,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                progressForm.Controls.Add(progressLabel);
                progressForm.Show();

                var result = await updateChecker.CheckForUpdates();
                progressForm.Close();

                if (!result.HasInternetConnection)
                {
                    MessageBox.Show("No internet connection", "Update Check Failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (result.UpdateAvailable)
                {
                    var dlgResult = MessageBox.Show(
                        string.Format(s.NewVersionAvailable, result.LatestVersion),
                        s.UpdateAvailable,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                    if (dlgResult == DialogResult.Yes)
                    {
                        var downloadProgress = new Progress<int>(pct =>
                            progressLabel.Text = string.Format(s.DownloadingUpdateProgress, pct));

                        progressForm = new Form
                        {
                            Text            = s.DownloadingUpdate,
                            Size            = new Size(300, 100),
                            FormBorderStyle = FormBorderStyle.FixedDialog,
                            StartPosition   = FormStartPosition.CenterScreen,
                            MaximizeBox     = false,
                            MinimizeBox     = false,
                            BackColor       = Color.FromArgb(32, 32, 32),
                            ForeColor       = Color.White
                        };
                        progressLabel = new Label
                        {
                            Text      = s.StartingDownload,
                            Dock      = DockStyle.Fill,
                            TextAlign = ContentAlignment.MiddleCenter
                        };
                        progressForm.Controls.Add(progressLabel);
                        progressForm.Show();

                        bool success = await updateChecker.DownloadAndInstallUpdate(
                            result.DownloadUrl, downloadProgress);
                        progressForm.Close();

                        if (!success)
                            MessageBox.Show(s.UpdateFailedMessage, s.UpdateFailed,
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show(s.LatestVersion, s.NoUpdatesAvailable,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error checking for updates: {ex.Message}", ex);
                MessageBox.Show(
                    LocalizedStrings.GetString("Failed to check for updates. Please try again later."),
                    LocalizedStrings.GetString("Update check failed"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Form closing / Fermeture du formulaire
        // ─────────────────────────────────────────────────────────────────
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            var am = mainProgram.ArcadeManager;
            if (am != null && am.IsLocked && !am.IsInternalClosing)
            {
                if (e.CloseReason == CloseReason.UserClosing
                    || e.CloseReason == CloseReason.TaskManagerClosing
                    || e.CloseReason == CloseReason.ApplicationExitCall)
                {
                    e.Cancel = true;
                    am.TriggerLocalOperatorPrompt();
                    return;
                }
            }

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

