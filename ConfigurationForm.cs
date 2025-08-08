using System;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Win32;

namespace BatRun
{
    public partial class ConfigurationForm : Form
    {
        private readonly IniFile config;
        private readonly string startupPath;
        private readonly Logger logger;
        private readonly IBatRunProgram program;

        private const string RUN_REGISTRY_KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string APP_NAME = "BatRun";

        private ComboBox? comboBoxWallpaper;
        private GroupBox? groupBoxWallpaper;
        private CheckBox? checkBoxEnableWithExplorer;
        private ComboBox? comboBoxWallpaperFolder;
        private Button? buttonESLoadingConfig;

        private bool isInitializing = true;

        public ConfigurationForm(IniFile config, Logger logger, IBatRunProgram program)
        {
            this.config = config;
            this.logger = logger;
            this.program = program;
            
            InitializeComponent();

            // Appliquer le style sombre à la fenêtre et aux contrôles
            ApplyDarkStyle();

            // Recharger les traductions avant de mettre à jour les textes
            LocalizedStrings.LoadTranslations();
            UpdateLocalizedTexts();

            // Ajouter le gestionnaire d'événements pour HideESLoading
            this.checkBoxHideESLoading.CheckedChanged += this.CheckBoxHideESLoading_CheckedChanged;

            // Initialiser le chemin de démarrage automatique
            startupPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                "BatRun.lnk");

            // Initialiser les contrôles de fond d'écran
            InitializeWallpaperControls();

            // Initialiser les contrôles de vidéo ES
            InitializeESLoadingVideo();

            // Initialiser la combobox des méthodes de démarrage
            InitializeStartupMethodComboBox();

            LoadSettings();
        }

        private void ApplyDarkStyle()
        {
            var darkBackColor = Color.FromArgb(28, 28, 28);
            var groupBoxBackColor = Color.FromArgb(45, 45, 48);
            this.BackColor = darkBackColor;
            this.ForeColor = Color.White;

            // Définir la largeur standard pour tous les groupBox et les marges
            int padding = 12;  // Espacement depuis les bords
            int formWidth = 500;  // Largeur totale de la fenêtre
            int standardWidth = formWidth - (padding * 2);  // La largeur des groupBox sera la largeur de la fenêtre moins les marges

            // Ajuster la largeur de la fenêtre
            this.Width = formWidth;

            // Appliquer le style et la largeur aux groupBox
            groupBoxFocus.BackColor = groupBoxBackColor;
            groupBoxFocus.ForeColor = Color.White;
            groupBoxFocus.Width = standardWidth;
            groupBoxFocus.Left = padding;

            groupBoxWindows.BackColor = groupBoxBackColor;
            groupBoxWindows.ForeColor = Color.White;
            groupBoxWindows.Width = standardWidth;
            groupBoxWindows.Left = padding;

            if (groupBoxWallpaper != null)
            {
                groupBoxWallpaper.BackColor = groupBoxBackColor;
                groupBoxWallpaper.ForeColor = Color.White;
                groupBoxWallpaper.Width = standardWidth;
                groupBoxWallpaper.Left = padding;
            }

            // Ajuster la position des boutons Save et Cancel
            if (buttonSave != null && buttonCancel != null)
            {
                buttonCancel.Left = this.ClientSize.Width - buttonCancel.Width - padding;
                buttonSave.Left = buttonCancel.Left - buttonSave.Width - 10;
                
                // Ajuster la position verticale des boutons
                buttonSave.Top = (groupBoxWallpaper?.Bottom ?? groupBoxWindows.Bottom) + 10;
                buttonCancel.Top = buttonSave.Top;
            }

            // Appliquer le style aux contrôles dans les groupBox
            ApplyStyleToGroupBoxControls(groupBoxFocus, groupBoxBackColor);
            ApplyStyleToGroupBoxControls(groupBoxWindows, groupBoxBackColor);
            if (groupBoxWallpaper != null)
            {
                ApplyStyleToGroupBoxControls(groupBoxWallpaper, groupBoxBackColor);
            }

            // Ajuster la hauteur de la fenêtre
            this.Height = (buttonSave?.Bottom ?? (groupBoxWallpaper?.Bottom ?? groupBoxWindows.Bottom)) + padding + 50;
        }

        private void ApplyStyleToGroupBoxControls(GroupBox groupBox, Color backColor)
        {
            foreach (Control control in groupBox.Controls)
            {
                control.BackColor = backColor;
                control.ForeColor = Color.White;
                if (control is ComboBox combo)
                {
                    combo.FlatStyle = FlatStyle.Flat;
                }
                else if (control is Button btn)
                {
                    btn.FlatStyle = FlatStyle.Flat;
                }
            }
        }

        private void UpdateLocalizedTexts()
        {
            this.Text = LocalizedStrings.GetString("BatRun Configuration");
            groupBoxFocus.Text = LocalizedStrings.GetString("Focus Settings");
            labelFocusDuration.Text = LocalizedStrings.GetString("Focus Duration:");
            labelFocusInterval.Text = LocalizedStrings.GetString("Focus Interval:");
            groupBoxWindows.Text = LocalizedStrings.GetString("Windows Settings");
            checkBoxMinimizeWindows.Text = LocalizedStrings.GetString("Minimize active windows on launch");
            checkBoxEnableVibration.Text = LocalizedStrings.GetString("Enable controller vibration");
            checkBoxEnableLogging.Text = LocalizedStrings.GetString("Enable logging (requires restart)");
            checkBoxHideESLoading.Text = LocalizedStrings.GetString("Hide ES during loading");
            checkBoxShowSplashScreen.Text = LocalizedStrings.GetString("Show splash screen on startup");
            checkBoxShowHotkeySplash.Text = LocalizedStrings.GetString("Show RetroBat splash screen");
            labelStartupMethod.Text = LocalizedStrings.GetString("Start with Windows:");
            buttonSave.Text = LocalizedStrings.GetString("Save");
            buttonCancel.Text = LocalizedStrings.GetString("Cancel");

            // Ajouter les nouveaux textes pour le fond d'écran
            if (groupBoxWallpaper != null)
            {
                groupBoxWallpaper.Text = LocalizedStrings.GetString("Wallpaper Settings");
            }
        }

        private void LoadSettings()
        {
            try
            {
                isInitializing = true;
                
            // Charger les paramètres de focus
            numericFocusDuration.Value = config.ReadInt("Focus", "FocusDuration", 9000);
            numericFocusInterval.Value = config.ReadInt("Focus", "FocusInterval", 3000);

                // Charger les paramètres de fenêtre
            checkBoxMinimizeWindows.Checked = config.ReadBool("Windows", "MinimizeWindows", true);
            checkBoxHideESLoading.Checked = config.ReadBool("Windows", "HideESLoading", false);
            checkBoxShowSplashScreen.Checked = config.ReadBool("Windows", "ShowSplashScreen", true);
            checkBoxShowHotkeySplash.Checked = config.ReadBool("Windows", "ShowHotkeySplash", true);

                // Charger les paramètres de vibration
                checkBoxEnableVibration.Checked = config.ReadBool("Controller", "EnableVibration", true);

                // Charger les paramètres de journalisation
                checkBoxEnableLogging.Checked = config.ReadBool("Logging", "EnableLogging", false);

            // Charger les paramètres de fond d'écran
            if (comboBoxWallpaper != null && comboBoxWallpaperFolder != null)
            {
                string selectedWallpaper = config.ReadValue("Wallpaper", "Selected", "None");
                
                // Charger d'abord les dossiers
                LoadWallpaperFolders();

                    // Restaurer le dossier sélectionné
                    string selectedFolder = config.ReadValue("Wallpaper", "SelectedFolder", "/");
                    int folderIndex = comboBoxWallpaperFolder.Items.IndexOf(selectedFolder);
                        if (folderIndex >= 0)
                        {
                            comboBoxWallpaperFolder.SelectedIndex = folderIndex;
                    }
                    else
                    {
                        comboBoxWallpaperFolder.SelectedIndex = 0; // Dossier racine par défaut
                }

                    // Charger la liste des wallpapers pour le dossier sélectionné
                LoadWallpaperList();

                    // Restaurer le wallpaper sélectionné
                if (comboBoxWallpaper.Items.Contains(selectedWallpaper))
                {
                    comboBoxWallpaper.SelectedItem = selectedWallpaper;
                }
                    else
                    {
                        comboBoxWallpaper.SelectedItem = "None";
                    }
                }

                // Charger l'état de EnableWithExplorer
                if (checkBoxEnableWithExplorer != null)
                {
                    checkBoxEnableWithExplorer.Checked = config.ReadBool("Wallpaper", "EnableWithExplorer", false);
                }

                // Charger la méthode de démarrage
                string startupMethod = "Disabled";
                if (File.Exists(startupPath))
                {
                    startupMethod = "Shortcut";
                }
                else
                {
                    using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                    if (key?.GetValue("BatRun") != null)
                    {
                        startupMethod = "Registry";
                    }
                }
                
                // Sélectionner la méthode de démarrage dans la combobox
                if (comboBoxStartupMethod.Items.Contains(startupMethod))
                {
                    comboBoxStartupMethod.SelectedItem = startupMethod;
                }

                isInitializing = false;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error loading settings: {ex.Message}", ex);
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isInitializing = false;
            }
        }

        private void InitializeStartupMethodComboBox()
        {
            // Modifier l'initialisation du ComboBox de méthode de démarrage
            comboBoxStartupMethod.Items.Clear();
            comboBoxStartupMethod.Items.Add("Disabled");  // Valeur simple au lieu d'un objet complexe
            comboBoxStartupMethod.Items.Add("Shortcut");
            comboBoxStartupMethod.Items.Add("Registry");
            
            // Définir les textes d'affichage via un Dictionary
            var displayTexts = new Dictionary<string, string>
            {
                { "Disabled", LocalizedStrings.GetString("Disabled") },
                { "Shortcut", LocalizedStrings.GetString("Shortcut") },
                { "Registry", LocalizedStrings.GetString("Registry") }
            };

            // Configurer l'affichage personnalisé avec vérification de null
            comboBoxStartupMethod.Format += (s, e) =>
            {
                if (e.ListItem != null)
                {
                    string itemValue = e.ListItem.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(itemValue) && displayTexts.TryGetValue(itemValue, out string? displayText))
                    {
                        e.Value = displayText;
                    }
                }
            };
        }

        private void ButtonSave_Click(object sender, EventArgs e)
        {
            try
            {
                // Lire l'état actuel avant la sauvegarde
                bool previousHideESValue = config.ReadValue("Windows", "HideESLoading", "false") == "true";
                bool newHideESValue = checkBoxHideESLoading.Checked;

                logger.LogInfo($"HideESLoading - Previous value: {previousHideESValue}, New value: {newHideESValue}");

                // Sauvegarder toutes les configurations
                SaveConfigurations();

                // Vérifier si l'état a changé (dans un sens ou dans l'autre)
                if (previousHideESValue != newHideESValue)
                {
                    logger.LogInfo($"HideESLoading state changed from {previousHideESValue} to {newHideESValue}");
                    string message = newHideESValue
                        ? LocalizedStrings.GetString("The 'Hide ES during loading' option has been enabled. BatRun will restart to apply changes.")
                        : LocalizedStrings.GetString("The 'Hide ES during loading' option has been disabled. BatRun will restart to apply changes.");

                    MessageBox.Show(
                        message,
                        LocalizedStrings.GetString("Restart Required"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    // Fermer le formulaire de configuration
                DialogResult = DialogResult.OK;
                Close();

                    // Redémarrer l'application
                    int currentPid = Process.GetCurrentProcess().Id;
                    Process.Start(Application.ExecutablePath, $"-waitforpid {currentPid}");
                    Application.Exit();
                }
                else
                {
                    logger.LogInfo("HideESLoading state unchanged");
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error saving configuration: {ex.Message}", ex);
                MessageBox.Show(
                    LocalizedStrings.GetString("An error occurred while saving the configuration."),
                    LocalizedStrings.GetString("Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void CreateStartupShortcut()
        {
            try
            {
                string targetPath = Application.ExecutablePath;
                string workingDirectory = Application.StartupPath;

                // Créer le script PowerShell pour créer le raccourci
                string script = $@"
                    $targetPath = '{targetPath.Replace("'", "''")}'
                    $workingDirectory = '{workingDirectory.Replace("'", "''")}'
                    $shortcutPath = '{startupPath.Replace("'", "''")}'

                    # Créer le raccourci directement sans élévation
                    $shell = New-Object -ComObject WScript.Shell
                    $shortcut = $shell.CreateShortcut($shortcutPath)
                    $shortcut.TargetPath = $targetPath
                    $shortcut.WorkingDirectory = $workingDirectory
                    $shortcut.Description = 'BatRun Launcher'
                    $shortcut.Save()

                    # Nettoyer
                    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($shortcut) | Out-Null
                    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($shell) | Out-Null
                    [System.GC]::Collect()
                    [System.GC]::WaitForPendingFinalizers()
                ";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"PowerShell error: {error}");
                    }
                    }

                    // Vérifier que le raccourci a bien été créé
                    if (!File.Exists(startupPath))
                    {
                        throw new Exception("Le raccourci n'a pas été créé correctement.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error creating startup shortcut", ex);
                throw;
            }
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void SetStartupRegistry(bool enable)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RUN_REGISTRY_KEY, true);
                if (key != null)
                {
                    if (enable)
                    {
                        string appPath = Application.ExecutablePath;
                        key.SetValue(APP_NAME, $"\"{appPath}\"");
                        logger.LogInfo("Added BatRun to registry startup");
                    }
                    else
                    {
                        key.DeleteValue(APP_NAME, false);
                        logger.LogInfo("Removed BatRun from registry startup");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error managing registry startup", ex);
                throw;
            }
        }

        private bool IsInStartupRegistry()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RUN_REGISTRY_KEY);
                return key?.GetValue(APP_NAME) != null;
            }
            catch (Exception ex)
            {
                logger.LogError("Error checking registry startup", ex);
                return false;
            }
        }

        private bool IsStartupTaskExists()
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "schtasks",
                        Arguments = "/query /tn \"BatRun_Startup\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output.Contains("BatRun_Startup");
            }
            catch (Exception ex)
            {
                logger.LogError("Error checking startup task", ex);
                return false;
            }
        }

        private void CreateStartupTask()
        {
            try
            {
                string exePath = Application.ExecutablePath;
                string workingDir = Path.GetDirectoryName(exePath) ?? "";
                string domainUser = Environment.UserDomainName + "\\" + Environment.UserName;

                // Échapper les guillemets dans les chemins
                exePath = exePath.Replace("\"", "\\\"");
                workingDir = workingDir.Replace("\"", "\\\"");

                string script = $@"
                    # Importer le module ScheduledTasks
                    Import-Module ScheduledTasks

                    try {{
                        # Créer l'action
                        $action = New-ScheduledTaskAction -Execute '{exePath}' -WorkingDirectory '{workingDir}'

                        # Créer le déclencheur (au démarrage de session)
                        $trigger = New-ScheduledTaskTrigger -AtLogon -User '{domainUser}'

                        # Créer les paramètres principaux
                        $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit 0

                        # Créer le principal (contexte de sécurité)
                        $principal = New-ScheduledTaskPrincipal -GroupId 'BUILTIN\Users' -RunLevel Limited

                        # Supprimer la tâche si elle existe déjà
                        Unregister-ScheduledTask -TaskName 'BatRun_Startup' -Confirm:$false -ErrorAction SilentlyContinue

                        # Créer la nouvelle tâche
                        Register-ScheduledTask -TaskName 'BatRun_Startup' `
                                             -Action $action `
                                             -Trigger $trigger `
                                             -Settings $settings `
                                             -Principal $principal `
                                             -Description 'BatRun Startup Task' `
                                             -Force
                    }}
                    catch {{
                        Write-Error $_.Exception.Message
                        exit 1
                    }}
                ";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"PowerShell error: {error}");
                    }

                    logger.LogInfo("Created startup task successfully using PowerShell");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error creating startup task: {ex.Message}", ex);
                throw;
            }
        }

        private void RemoveStartupTask()
        {
            try
            {
                string script = @"
                    # Supprimer la tâche si elle existe
                    $taskName = 'BatRun_Startup'
                    $task = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
                    if ($task) {
                        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
                    }
                ";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                    RedirectStandardError = true,
                        CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                process.WaitForExit();
                }
                logger.LogInfo("Removed startup task successfully");
            }
            catch (Exception ex)
            {
                logger.LogError("Error removing startup task", ex);
                // Ne pas relancer l'exception car la tâche peut ne pas exister
            }
        }

        private void InitializeWallpaperControls()
        {
            int padding = 12;
            int formWidth = 500;
            int standardWidth = formWidth - (padding * 2);
            int innerPadding = 15;  // Marge interne des contrôles
            int controlWidth = standardWidth - (innerPadding * 2);  // Largeur des contrôles internes

            // Groupe pour les paramètres de fond d'écran
            groupBoxWallpaper = new GroupBox
            {
                Text = LocalizedStrings.GetString("Wallpaper Settings"),
                Location = new Point(padding, groupBoxWindows.Bottom + 10),
                Size = new Size(standardWidth, 240),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            // ComboBox pour la sélection du fond d'écran
            comboBoxWallpaperFolder = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(innerPadding, 30),
                Width = controlWidth,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            comboBoxWallpaperFolder.SelectedIndexChanged += (s, e) => LoadWallpaperList();

            comboBoxWallpaper = new ComboBox
            {
                Location = new Point(innerPadding, 30),
                Size = new Size(controlWidth, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            // Checkbox pour activer le wallpaper même avec explorer.exe
            checkBoxEnableWithExplorer = new CheckBox
            {
                Text = LocalizedStrings.GetString("Enable wallpaper even with Explorer running"),
                Location = new Point(innerPadding, 60),
                AutoSize = true,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Checked = config.ReadBool("Wallpaper", "EnableWithExplorer", false)
            };

            // Ajouter une checkbox pour la lecture en boucle des vidéos
            var checkBoxLoopVideo = new CheckBox
            {
                Text = LocalizedStrings.GetString("Loop video wallpapers"),
                Location = new Point(innerPadding, 85),
                AutoSize = true,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Checked = config.ReadBool("Wallpaper", "LoopVideo", true)
            };

            // Ajouter une checkbox pour activer/désactiver l'audio des vidéos
            var checkBoxEnableAudio = new CheckBox
            {
                Text = LocalizedStrings.GetString("Enable video audio"),
                Location = new Point(innerPadding, 110),
                AutoSize = true,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Checked = config.ReadBool("Wallpaper", "EnableAudio", false)
            };

            checkBoxEnableAudio.CheckedChanged += (s, e) =>
            {
                config.WriteValue("Wallpaper", "EnableAudio", checkBoxEnableAudio.Checked.ToString());
            };

            checkBoxLoopVideo.CheckedChanged += (s, e) =>
            {
                config.WriteValue("Wallpaper", "LoopVideo", checkBoxLoopVideo.Checked.ToString());
            };

            // Ajouter un gestionnaire d'événements pour le changement d'état
            checkBoxEnableWithExplorer.CheckedChanged += (s, e) =>
            {
                config.WriteValue("Wallpaper", "EnableWithExplorer", checkBoxEnableWithExplorer.Checked.ToString());
                
                var wallpaperManager = new WallpaperManager(config, logger, program);
                if (Process.GetProcessesByName("explorer").Length > 0)
                {
                    if (checkBoxEnableWithExplorer.Checked)
                    {
                        wallpaperManager.ShowWallpaper(forceShow: true);
                    }
                    else
                    {
                        wallpaperManager.CloseWallpaper();
                    }
                }
            };

            // Bouton de test
            var buttonTestWallpaper = new Button
            {
                Text = LocalizedStrings.GetString("Test Wallpaper"),
                Location = new Point(innerPadding, 90),
                Size = new Size(controlWidth, 30),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            // Nouveau bouton pour fermer le wallpaper
            var buttonCloseWallpaper = new Button
            {
                Text = LocalizedStrings.GetString("Close Wallpaper"),
                Location = new Point(innerPadding + controlWidth - 120, 90),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            buttonTestWallpaper.Click += (s, e) =>
            {
                try
                {
                    // D'abord tuer VLC comme le fait le bouton Kill VLC
                    WallpaperManager.StopActiveMediaPlayer();
                    WallpaperManager.CleanupAllInstances();
                    GC.Collect(2, GCCollectionMode.Forced, true);
                    GC.WaitForPendingFinalizers();

                    // Ensuite, vérifier et charger le nouveau fond d'écran
                    if (comboBoxWallpaper?.SelectedItem == null)
                    {
                        MessageBox.Show(
                            LocalizedStrings.GetString("Please select a wallpaper"),
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                        return;
                    }

                    string selectedWallpaper = comboBoxWallpaper.SelectedItem.ToString() ?? "None";
                    
                    // Sauvegarder la configuration
                    config.WriteValue("Wallpaper", "Selected", selectedWallpaper);
                    config.WriteValue("Wallpaper", "EnableWithExplorer", checkBoxEnableWithExplorer.Checked.ToString());

                    // Créer une nouvelle instance et afficher le fond d'écran
                    var wallpaperManager = new WallpaperManager(config, logger, program);
                    wallpaperManager.ShowWallpaper(forceShow: true);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error testing wallpaper: {ex.Message}", ex);
                    MessageBox.Show(
                        $"Error testing wallpaper: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            };

            buttonCloseWallpaper.Click += (s, e) =>
            {
                try
                {
                    // D'abord tuer VLC comme le fait le bouton Kill VLC
                    WallpaperManager.StopActiveMediaPlayer();
                    WallpaperManager.CleanupAllInstances();
                    GC.Collect(2, GCCollectionMode.Forced, true);
                    GC.WaitForPendingFinalizers();

                    // Ensuite, fermer proprement le wallpaper
                    var wallpaperManager = new WallpaperManager(config, logger, program);
                    wallpaperManager.CloseWallpaper();
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error closing wallpaper: {ex.Message}", ex);
                    MessageBox.Show(
                        $"Error closing wallpaper: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            };

            // Ajuster la position de la comboBox des wallpapers
            if (comboBoxWallpaper != null)
            {
                comboBoxWallpaper.Location = new Point(innerPadding, 60);
            }

            // Ajuster la position des autres contrôles
            if (checkBoxEnableWithExplorer != null)
            {
                checkBoxEnableWithExplorer.Location = new Point(innerPadding, 90);
            }

            // Ajuster la position des checkboxes
            checkBoxLoopVideo.Location = new Point(innerPadding, 115);
            checkBoxEnableAudio.Location = new Point(innerPadding, 140);

            // Ajuster la position des boutons
            buttonTestWallpaper.Location = new Point(innerPadding, 170);
            buttonCloseWallpaper.Location = new Point(innerPadding + controlWidth - 120, 170);

            LoadWallpaperFolders();
            
            // Ajouter les contrôles au groupBox
            groupBoxWallpaper.Controls.Add(comboBoxWallpaperFolder);
            groupBoxWallpaper.Controls.Add(comboBoxWallpaper);
            groupBoxWallpaper.Controls.Add(checkBoxEnableWithExplorer);
            groupBoxWallpaper.Controls.Add(checkBoxLoopVideo);
            groupBoxWallpaper.Controls.Add(checkBoxEnableAudio);
            groupBoxWallpaper.Controls.Add(buttonTestWallpaper);
            groupBoxWallpaper.Controls.Add(buttonCloseWallpaper);
            this.Controls.Add(groupBoxWallpaper);

            // Ajuster la taille du groupBox pour accommoder tous les contrôles
            if (groupBoxWallpaper != null)
            {
                groupBoxWallpaper.Height = 240;  // Augmenté pour accommoder la nouvelle disposition
            }

            // Ajuster la taille de la fenêtre
            if (groupBoxWallpaper != null && buttonSave != null)
            {
                this.ClientSize = new Size(
                    Math.Max(this.ClientSize.Width, groupBoxWallpaper.Right + 20),
                    groupBoxWallpaper.Bottom + buttonSave.Height + 20
                );

                // Ajuster la position des boutons Save et Cancel
                if (buttonCancel != null)
                {
                    buttonSave.Location = new Point(
                        buttonSave.Location.X,
                        groupBoxWallpaper.Bottom + 10
                    );
                    buttonCancel.Location = new Point(
                        buttonCancel.Location.X,
                        groupBoxWallpaper.Bottom + 10
                    );

                    // S'assurer que les boutons sont visibles
                    this.MinimumSize = new Size(
                        this.Width,
                        buttonCancel.Bottom + 10
                    );
                }
            }
            else
            {
                // Taille par défaut si les contrôles ne sont pas initialisés
                this.ClientSize = new Size(350, 450);
            }
        }

        private void LoadWallpaperFolders()
        {
            if (comboBoxWallpaperFolder == null) return;

            comboBoxWallpaperFolder.Items.Clear();
            comboBoxWallpaperFolder.Items.Add("/");  // Dossier racine

            // Ajouter le dossier vidéo d'EmulationStation s'il existe
            string esVideoPath = GetEmulationStationVideoPath();
            if (!string.IsNullOrEmpty(esVideoPath))
            {
                comboBoxWallpaperFolder.Items.Add("ES Videos");
            }

            string wallpaperFolder = Path.Combine(AppContext.BaseDirectory, "Wallpapers");
            if (!Directory.Exists(wallpaperFolder))
            {
                Directory.CreateDirectory(wallpaperFolder);
                return;
            }

            var directories = Directory.GetDirectories(wallpaperFolder, "*", SearchOption.AllDirectories)
                .Select(d => Path.GetRelativePath(wallpaperFolder, d))
                .OrderBy(d => d);

            foreach (var dir in directories)
            {
                comboBoxWallpaperFolder.Items.Add(dir);
            }

            // Ne pas forcer la sélection du dossier racine ici
            if (comboBoxWallpaperFolder.Items.Count > 0 && comboBoxWallpaperFolder.SelectedIndex < 0)
            {
                comboBoxWallpaperFolder.SelectedIndex = 0;
            }
        }

        private void LoadWallpaperList()
        {
            if (comboBoxWallpaper == null || comboBoxWallpaperFolder == null) return;

            try
            {
                comboBoxWallpaper.Items.Clear();
                comboBoxWallpaper.Items.Add(LocalizedStrings.GetString("None"));
                comboBoxWallpaper.Items.Add(LocalizedStrings.GetString("Random Wallpaper"));

                string selectedFolder = comboBoxWallpaperFolder.SelectedItem?.ToString() ?? "/";
                string searchPath;

                if (selectedFolder == "ES Videos")
                {
                    searchPath = GetEmulationStationVideoPath();
                }
                else
                {
                    string wallpaperFolder = Path.Combine(AppContext.BaseDirectory, "Wallpapers");
                    searchPath = selectedFolder == "/" ? wallpaperFolder : Path.Combine(wallpaperFolder, selectedFolder);
                }

                if (Directory.Exists(searchPath))
                {
                    var imageFiles = Directory.GetFiles(searchPath, "*.*")
                        .Where(file => WallpaperManager.SupportedExtensions.Contains(Path.GetExtension(file).ToLower()))
                        .Select(f => selectedFolder == "ES Videos" ? Path.Combine("ES Videos", Path.GetFileName(f)) : Path.GetRelativePath(Path.Combine(AppContext.BaseDirectory, "Wallpapers"), f))
                        .OrderBy(f => f);

                    foreach (var file in imageFiles)
                    {
                        comboBoxWallpaper.Items.Add(file);
                    }
                }

                // Restaurer la sélection précédente si possible
                string selectedWallpaper = config.ReadValue("Wallpaper", "Selected", "None");
                if (comboBoxWallpaper.Items.Contains(selectedWallpaper))
                {
                    comboBoxWallpaper.SelectedItem = selectedWallpaper;
                }
                else
                {
                    comboBoxWallpaper.SelectedItem = "None";
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error loading wallpaper list: {ex.Message}");
            }
        }

        private void CheckBoxHideESLoading_CheckedChanged(object? sender, EventArgs e)
        {
            bool isHideESChecked = checkBoxHideESLoading.Checked;
            
            // Désactiver et décocher les options de splash screen et minimize windows si Hide ES est activé
            checkBoxShowSplashScreen.Enabled = !isHideESChecked;
            checkBoxShowHotkeySplash.Enabled = !isHideESChecked;
            checkBoxMinimizeWindows.Enabled = !isHideESChecked;
            
            if (isHideESChecked)
            {
                checkBoxShowSplashScreen.Checked = false;
                checkBoxShowHotkeySplash.Checked = false;
                checkBoxMinimizeWindows.Checked = false;

                // Afficher le message uniquement si ce n'est pas pendant l'initialisation
                if (!isInitializing)
                {
                    MessageBox.Show(
                        string.Format(
                            LocalizedStrings.GetString("The following options are automatically disabled when Hide ES during loading is enabled:") + Environment.NewLine +
                            LocalizedStrings.GetString("-Show splash screen on startup") + Environment.NewLine +
                            LocalizedStrings.GetString("-Show RetroBat splash screen") + Environment.NewLine +
                            LocalizedStrings.GetString("-Minimize active windows on launch") + Environment.NewLine +
                            LocalizedStrings.GetString("-RetroBat intro video will also be disabled.")
                        ),
                        LocalizedStrings.GetString("Information"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }

            // Créer ou supprimer le script system-selected
            string retrobatPath = program.GetRetrobatPath();
            if (!string.IsNullOrEmpty(retrobatPath))
            {
                // Gérer le script system-selected
                string scriptPath = Path.Combine(Path.GetDirectoryName(retrobatPath) ?? "", "emulationstation", ".emulationstation", "scripts", "system-selected");
                Directory.CreateDirectory(scriptPath); // Crée le dossier s'il n'existe pas

                string scriptFile = Path.Combine(scriptPath, "notify_batrun.bat");
                if (isHideESChecked)
                {
                    // Créer le script
                    string scriptContent = $@"@echo off
set Focus_BatRun_path={AppContext.BaseDirectory}
start ""BatRun_Focus_ES"" ""%Focus_BatRun_path%\BatRun.exe"" -ES_System_select";

                    File.WriteAllText(scriptFile, scriptContent);

                    // Désactiver la vidéo d'intro dans retrobat.ini
                    string retrobatIniPath = Path.Combine(Path.GetDirectoryName(retrobatPath) ?? "", "retrobat.ini");
                    if (File.Exists(retrobatIniPath))
                    {
                        var lines = File.ReadAllLines(retrobatIniPath);
                        bool foundIntroLine = false;
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (lines[i].Trim().StartsWith("EnableIntro="))
                            {
                                lines[i] = "EnableIntro=0";
                                foundIntroLine = true;
                                break;
                            }
                        }
                        if (!foundIntroLine)
                        {
                            Array.Resize(ref lines, lines.Length + 1);
                            lines[lines.Length - 1] = "EnableIntro=0";
                        }
                        File.WriteAllLines(retrobatIniPath, lines);
                        logger.LogInfo("Disabled RetroBat intro video");
                    }
                }
                else
                {
                    // Supprimer le script s'il existe
                    if (File.Exists(scriptFile))
                    {
                        File.Delete(scriptFile);
                    }

                    // Réactiver la vidéo d'intro dans retrobat.ini
                    string retrobatIniPath = Path.Combine(Path.GetDirectoryName(retrobatPath) ?? "", "retrobat.ini");
                    if (File.Exists(retrobatIniPath))
                    {
                        var lines = File.ReadAllLines(retrobatIniPath);
                        bool foundIntroLine = false;
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (lines[i].Trim().StartsWith("EnableIntro="))
                            {
                                lines[i] = "EnableIntro=1";
                                foundIntroLine = true;
                                break;
                            }
                        }
                        if (!foundIntroLine)
                        {
                            Array.Resize(ref lines, lines.Length + 1);
                            lines[lines.Length - 1] = "EnableIntro=1";
                        }
                        File.WriteAllLines(retrobatIniPath, lines);
                        logger.LogInfo("Enabled RetroBat intro video");
                    }
                }
            }
        }


        // Ajouter une méthode pour mettre à jour l'état du démarrage automatique
        public void UpdateStartupState(bool isCustomUIEnabled)
        {
            try
            {
                if (isCustomUIEnabled)
                {
                    logger.LogInfo("Custom UI is enabled, disabling startup methods...");
                    // Désactiver le démarrage automatique
                    if (File.Exists(startupPath))
                    {
                        logger.LogInfo("Removing startup shortcut...");
                        File.Delete(startupPath);
                    }
                    SetStartupRegistry(false);
                    comboBoxStartupMethod.SelectedValue = "Disabled";
                    comboBoxStartupMethod.Enabled = false;
                }
                else
                {
                    logger.LogInfo("Custom UI is disabled, enabling startup method selection...");
                    comboBoxStartupMethod.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error updating startup state: {ex.Message}", ex);
            }
        }

        private string GetEmulationStationVideoPath()
        {
            try
            {
                string? retroBatPath = program.GetRetrobatPath();
                if (!string.IsNullOrEmpty(retroBatPath))
                {
                    string? retroBatFolder = Path.GetDirectoryName(retroBatPath);
                    if (!string.IsNullOrEmpty(retroBatFolder))
                    {
                        string esVideoPath = Path.Combine(retroBatFolder, "emulationstation", ".emulationstation", "video");
                        if (Directory.Exists(esVideoPath))
                        {
                            return esVideoPath;
                        }
                        else
                        {
                            logger.LogError($"EmulationStation video directory not found at: {esVideoPath}");
                        }
                    }
                    else
                    {
                        logger.LogError("Could not get RetroBat directory from path");
                    }
                }
                else
                {
                    logger.LogError("RetroBat path is empty");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error getting EmulationStation video path: {ex.Message}", ex);
            }
            return string.Empty;
        }

        private void HandleStartupMethod()
        {
            // Récupérer la valeur réelle (pas l'objet d'affichage)
            string startupMethod = comboBoxStartupMethod.SelectedItem?.ToString() ?? "Disabled";
            if (startupMethod.Contains("Value ="))
            {
                // Si c'est un objet complexe, extraire juste la valeur
                startupMethod = startupMethod.Split('=')[1].Split(',')[0].Trim();
            }
            
            logger.LogInfo($"Applying startup method: {startupMethod}");

            try
            {
                switch (startupMethod)
                {
                    case "Disabled":
                        logger.LogInfo("Disabling all startup methods...");
                        if (File.Exists(startupPath))
                        {
                            logger.LogInfo("Removing existing shortcut...");
                            File.Delete(startupPath);
                        }
                        SetStartupRegistry(false);
                        logger.LogInfo("All startup methods disabled successfully");
                        break;

                    case "Shortcut":
                        logger.LogInfo("Creating startup shortcut...");
                        SetStartupRegistry(false);
                        CreateStartupShortcut();
                        logger.LogInfo("Startup shortcut created successfully");
                        break;

                    case "Registry":
                        logger.LogInfo("Setting up registry startup...");
                        if (File.Exists(startupPath))
                        {
                            logger.LogInfo("Removing existing shortcut...");
                            File.Delete(startupPath);
                        }
                        SetStartupRegistry(true);
                        logger.LogInfo("Registry startup set successfully");
                        break;

                    default:
                        logger.LogError($"Unknown startup method: {startupMethod}");
                        throw new Exception($"Unknown startup method: {startupMethod}");
                }
            }
            catch (Exception startupEx)
            {
                logger.LogError($"Error setting startup method {startupMethod}: {startupEx.Message}", startupEx);
                throw;
            }
        }

        private void InitializeESLoadingVideo()
        {
            if (checkBoxHideESLoading == null) return;

            // Créer le bouton de configuration
            buttonESLoadingConfig = new Button
            {
                Text = LocalizedStrings.GetString("MediaPlayer Settings"),
                Location = new Point(checkBoxHideESLoading.Right + 10, checkBoxHideESLoading.Top),
                Size = new Size(200, 23),  // Increased button width
                Enabled = checkBoxHideESLoading.Checked,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            buttonESLoadingConfig.Click += ButtonESLoadingConfig_Click;
            groupBoxWindows.Controls.Add(buttonESLoadingConfig);

            // Ajuster la largeur du groupBoxWindows si nécessaire
            int requiredWidth = buttonESLoadingConfig.Right + 10;
            if (groupBoxWindows.Width < requiredWidth)
            {
                groupBoxWindows.Width = requiredWidth;
                // Ajuster la largeur de la fenêtre principale
                this.Width = groupBoxWindows.Right + 40;
            }

            // Mettre à jour l'état du bouton quand l'option HideESLoading change
            checkBoxHideESLoading.CheckedChanged += (s, e) =>
            {
                if (buttonESLoadingConfig != null)
                {
                    buttonESLoadingConfig.Enabled = checkBoxHideESLoading.Checked;
                }
            };
        }

        private void LoadESLoadingVideos()
        {
            // Cette méthode n'est plus nécessaire car nous utilisons maintenant la fenêtre de configuration
        }

        private void ButtonESLoadingConfig_Click(object? sender, EventArgs e)
        {
            using var configForm = new ESLoadingConfigForm(config, logger);
            if (configForm.ShowDialog() == DialogResult.OK)
            {
                configForm.SaveSettings();
            }
        }

        private void SaveConfigurations()
        {
            try
            {
                logger.LogInfo("Saving configurations...");

                // Windows Settings
                config.WriteValue("Windows", "HideESLoading", checkBoxHideESLoading.Checked.ToString().ToLower());
                logger.LogInfo($"Saved HideESLoading: {checkBoxHideESLoading.Checked}");

                // Save other configurations...
                config.WriteValue("Focus", "FocusDuration", numericFocusDuration.Value.ToString());
                config.WriteValue("Focus", "FocusInterval", numericFocusInterval.Value.ToString());

                config.WriteValue("Windows", "MinimizeWindows", checkBoxMinimizeWindows.Checked.ToString().ToLower());
                config.WriteValue("Windows", "ShowSplashScreen", checkBoxShowSplashScreen.Checked.ToString().ToLower());
                config.WriteValue("Windows", "ShowHotkeySplash", checkBoxShowHotkeySplash.Checked.ToString().ToLower());

                config.WriteValue("Controller", "EnableVibration", checkBoxEnableVibration.Checked.ToString().ToLower());
                config.WriteValue("Logging", "EnableLogging", checkBoxEnableLogging.Checked.ToString().ToLower());

                // Wallpaper settings
                string selectedWallpaper = comboBoxWallpaper?.SelectedItem?.ToString() ?? "None";
                config.WriteValue("Wallpaper", "Selected", selectedWallpaper);
                
                string selectedFolder = comboBoxWallpaperFolder?.SelectedItem?.ToString() ?? "/";
                config.WriteValue("Wallpaper", "SelectedFolder", selectedFolder);
                config.WriteValue("Wallpaper", "IsActive", (selectedWallpaper != "None").ToString().ToLower());

                if (checkBoxEnableWithExplorer != null)
                {
                    config.WriteValue("Wallpaper", "EnableWithExplorer", checkBoxEnableWithExplorer.Checked.ToString().ToLower());
                }

                HandleStartupMethod();

                logger.LogInfo("Configurations saved successfully");
            }
            catch (Exception ex)
            {
                logger.LogError("Error in SaveConfigurations", ex);
                throw;
            }
        }
    }
} 