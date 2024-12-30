using System;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

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

        public ConfigurationForm(IniFile config, Logger logger, IBatRunProgram program)
        {
            this.config = config;
            this.logger = logger;
            this.program = program;
            
            InitializeComponent();
            FormStyles.ApplyDarkStyle(this);
            UpdateLocalizedTexts();

            // Initialiser le chemin de démarrage automatique
            startupPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                "BatRun.lnk");

            // Initialiser les contrôles de fond d'écran
            InitializeWallpaperControls();

            LoadSettings();
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
            // Charger les paramètres de focus
            numericFocusDuration.Value = config.ReadInt("Focus", "FocusDuration", 9000);
            numericFocusInterval.Value = config.ReadInt("Focus", "FocusInterval", 3000);

            // Charger le paramètre MinimizeWindows
            checkBoxMinimizeWindows.Checked = config.ReadBool("Windows", "MinimizeWindows", true);

            // Charger la méthode de démarrage
            if (File.Exists(startupPath))
            {
                comboBoxStartupMethod.SelectedItem = "Shortcut";
            }
            else if (IsInStartupRegistry())
            {
                comboBoxStartupMethod.SelectedItem = "Registry";
            }
            else if (IsStartupTaskExists())
            {
                comboBoxStartupMethod.SelectedItem = "Task";
            }
            else
            {
                comboBoxStartupMethod.SelectedItem = "Disabled";
            }

            // Charger l'état de la vibration
            checkBoxEnableVibration.Checked = config.ReadBool("Controller", "EnableVibration", true);

            // Charger l'état du logging
            checkBoxEnableLogging.Checked = config.ReadBool("Logging", "EnableLogging", true);

            // Charger les paramètres de fond d'écran
            if (comboBoxWallpaper != null && comboBoxWallpaperFolder != null)
            {
                string selectedWallpaper = config.ReadValue("Wallpaper", "Selected", "None");
                
                // Charger d'abord les dossiers
                LoadWallpaperFolders();

                // Si un wallpaper est sélectionné, sélectionner son dossier parent
                if (selectedWallpaper != "None" && selectedWallpaper != "Random Wallpaper")
                {
                    string? parentFolder = Path.GetDirectoryName(selectedWallpaper);
                    if (!string.IsNullOrEmpty(parentFolder) && parentFolder != ".")
                    {
                        // Sélectionner le dossier parent dans la liste
                        int folderIndex = comboBoxWallpaperFolder.Items.IndexOf(parentFolder);
                        if (folderIndex >= 0)
                        {
                            comboBoxWallpaperFolder.SelectedIndex = folderIndex;
                        }
                    }
                    else
                    {
                        comboBoxWallpaperFolder.SelectedIndex = 0; // Dossier racine
                    }
                }

                // Maintenant charger la liste des wallpapers et sélectionner le bon
                LoadWallpaperList();
                if (comboBoxWallpaper.Items.Contains(selectedWallpaper))
                {
                    comboBoxWallpaper.SelectedItem = selectedWallpaper;
                }
            }
        }

        private void ButtonSave_Click(object sender, EventArgs e)
        {
            try
            {
                // Sauvegarder les paramètres de focus
                config.WriteValue("Focus", "FocusDuration", numericFocusDuration.Value.ToString());
                config.WriteValue("Focus", "FocusInterval", numericFocusInterval.Value.ToString());

                // Sauvegarder le paramètre MinimizeWindows
                config.WriteValue("Windows", "MinimizeWindows", checkBoxMinimizeWindows.Checked.ToString());

                // Sauvegarder l'état de la vibration
                config.WriteValue("Controller", "EnableVibration", checkBoxEnableVibration.Checked.ToString());

                // Sauvegarder l'état du logging
                config.WriteValue("Logging", "EnableLogging", checkBoxEnableLogging.Checked.ToString());

                // Gérer le démarrage automatique selon la méthode choisie
                string startupMethod = comboBoxStartupMethod.SelectedItem?.ToString() ?? "Disabled";
                switch (startupMethod)
                {
                    case "Shortcut":
                        SetStartupRegistry(false);
                        RemoveStartupTask();
                        CreateStartupShortcut();
                        break;
                    case "Registry":
                        if (File.Exists(startupPath)) File.Delete(startupPath);
                        RemoveStartupTask();
                        SetStartupRegistry(true);
                        break;
                    case "Task":
                        if (File.Exists(startupPath)) File.Delete(startupPath);
                        SetStartupRegistry(false);
                        CreateStartupTask();
                        break;
                    default: // "Disabled"
                        if (File.Exists(startupPath)) File.Delete(startupPath);
                        SetStartupRegistry(false);
                        RemoveStartupTask();
                        break;
                }

                // Sauvegarder les paramètres de fond d'écran
                string selectedWallpaper = comboBoxWallpaper?.SelectedItem?.ToString() ?? "None";
                config.WriteValue("Wallpaper", "Selected", selectedWallpaper);
                
                // Mettre à jour l'état actif du wallpaper
                config.WriteValue("Wallpaper", "IsActive", (selectedWallpaper != "None").ToString().ToLower());

                // Sauvegarder le dossier sélectionné
                string selectedFolder = comboBoxWallpaperFolder?.SelectedItem?.ToString() ?? "/";
                config.WriteValue("Wallpaper", "SelectedFolder", selectedFolder);

                // S'assurer que l'option EnableWithExplorer est sauvegardée
                if (checkBoxEnableWithExplorer != null)
                {
                    config.WriteValue("Wallpaper", "EnableWithExplorer", checkBoxEnableWithExplorer.Checked.ToString());
                }

                logger.LogInfo("Configuration settings saved successfully");
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                logger.LogError("Error saving configuration settings", ex);
                MessageBox.Show(
                    LocalizedStrings.GetString("Failed to save settings"),
                    LocalizedStrings.GetString("Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void CreateStartupShortcut()
        {
            try
            {
                string targetPath = Application.ExecutablePath.Replace("\"", "\\\"");
                string workingDirectory = Application.StartupPath.Replace("\"", "\\\"");

                // Créer le script PowerShell pour créer le raccourci
                string script = $@"
                    $targetPath = '{targetPath}'
                    $workingDirectory = '{workingDirectory}'
                    $shortcutPath = '{startupPath.Replace("\"", "\\\"")}'

                    # Supprimer le raccourci existant s'il existe
                    if (Test-Path $shortcutPath) {{
                        Remove-Item $shortcutPath -Force
                    }}

                    # Créer le raccourci
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

                // Exécuter le script PowerShell avec une priorité élevée
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Verb = "runas" // Exécuter en tant qu'administrateur si nécessaire
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"PowerShell error (Exit code: {process.ExitCode}): {error}\nOutput: {output}");
                    }

                    // Vérifier que le raccourci a bien été créé
                    if (!File.Exists(startupPath))
                    {
                        throw new Exception("Le raccourci n'a pas été créé correctement.");
                    }
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
                    # Activer les commandes PowerShell
                    Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force

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
                        $principal = New-ScheduledTaskPrincipal -UserId '{domainUser}' -LogonType Interactive -RunLevel Limited

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
                        throw new Exception($"PowerShell error (Exit code: {process.ExitCode})\nError: {error}\nOutput: {output}");
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
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "schtasks",
                        Arguments = "/delete /tn \"BatRun_Startup\" /f",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
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
            // Groupe pour les paramètres de fond d'écran
            groupBoxWallpaper = new GroupBox
            {
                Text = LocalizedStrings.GetString("Wallpaper Settings"),
                Location = new Point(12, groupBoxWindows.Bottom + 10),
                Size = new Size(380, 140),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            // ComboBox pour la sélection du fond d'écran
            comboBoxWallpaper = new ComboBox
            {
                Location = new Point(15, 30),
                Size = new Size(350, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            // Checkbox pour activer le wallpaper même avec explorer.exe
            checkBoxEnableWithExplorer = new CheckBox
            {
                Text = LocalizedStrings.GetString("Enable wallpaper even with Explorer running"),
                Location = new Point(15, 60),
                AutoSize = true,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Checked = config.ReadBool("Wallpaper", "EnableWithExplorer", false)
            };

            // Ajouter une checkbox pour la lecture en boucle des vidéos
            var checkBoxLoopVideo = new CheckBox
            {
                Text = LocalizedStrings.GetString("Loop video wallpapers"),
                Location = new Point(15, 85),
                AutoSize = true,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Checked = config.ReadBool("Wallpaper", "LoopVideo", true)
            };

            // Ajouter une checkbox pour activer/désactiver l'audio des vidéos
            var checkBoxEnableAudio = new CheckBox
            {
                Text = LocalizedStrings.GetString("Enable video audio"),
                Location = new Point(15, 110),
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
                Location = new Point(15, 90),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            // Nouveau bouton pour fermer le wallpaper
            var buttonCloseWallpaper = new Button
            {
                Text = LocalizedStrings.GetString("Close Wallpaper"),
                Location = new Point(145, 90),
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

            // ComboBox pour la sélection du dossier
            comboBoxWallpaperFolder = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(15, 30),
                Width = 270,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            comboBoxWallpaperFolder.SelectedIndexChanged += (s, e) => LoadWallpaperList();

            // Ajuster la position de la comboBox des wallpapers
            if (comboBoxWallpaper != null)
            {
                comboBoxWallpaper.Location = new Point(15, 60);
            }

            // Ajuster la position des autres contrôles
            if (checkBoxEnableWithExplorer != null)
            {
                checkBoxEnableWithExplorer.Location = new Point(15, 90);
            }

            // Ajuster la position des checkboxes
            checkBoxLoopVideo.Location = new Point(15, 115);
            checkBoxEnableAudio.Location = new Point(15, 140);

            // Ajuster la position des boutons
            buttonTestWallpaper.Location = new Point(15, 170);
            buttonCloseWallpaper.Location = new Point(145, 170);

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

            comboBoxWallpaper.Items.Clear();
            comboBoxWallpaper.Items.Add(LocalizedStrings.GetString("Random Wallpaper"));
            comboBoxWallpaper.Items.Add(LocalizedStrings.GetString("None"));

            string wallpaperFolder = Path.Combine(AppContext.BaseDirectory, "Wallpapers");
            string selectedFolder = comboBoxWallpaperFolder.SelectedItem?.ToString() ?? "/";
            string searchPath = selectedFolder == "/" ? wallpaperFolder : Path.Combine(wallpaperFolder, selectedFolder);

            if (Directory.Exists(searchPath))
            {
                var imageFiles = Directory.GetFiles(searchPath, "*.*")
                    .Where(file => WallpaperManager.SupportedExtensions.Contains(Path.GetExtension(file).ToLower()))
                    .Select(f => Path.GetRelativePath(wallpaperFolder, f))
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
    }
} 