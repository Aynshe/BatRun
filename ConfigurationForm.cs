using System;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace BatRun
{
    public partial class ConfigurationForm : Form
    {
        private readonly IniFile config;
        private readonly string startupPath;
        private readonly Logger logger;

        private const string RUN_REGISTRY_KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string APP_NAME = "BatRun";

        public ConfigurationForm(IniFile config, Logger logger)
        {
            this.config = config;
            this.logger = logger;
            InitializeComponent();

            // Appliquer le style sombre
            FormStyles.ApplyDarkStyle(this);

            // Initialiser le chemin de démarrage automatique
            startupPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                "BatRun.lnk");

            LoadSettings();
        }

        private void LoadSettings()
        {
            // Charger les paramètres de focus
            numericFocusDuration.Value = config.ReadInt("Focus", "FocusDuration", 15000);
            numericFocusInterval.Value = config.ReadInt("Focus", "FocusInterval", 5000);

            // Charger le paramètre MinimizeWindows
            checkBoxMinimizeWindows.Checked = config.ReadBool("Windows", "MinimizeWindows", true);

            // Vérifier les deux méthodes de démarrage séparément
            checkBoxStartWithWindows.Checked = File.Exists(startupPath);
            checkBoxStartupRegistry.Checked = IsInStartupRegistry();

            // Charger l'état du logging
            checkBoxEnableLogging.Checked = config.ReadBool("Logging", "EnableLogging", true);
        }

        private void CheckBoxStartup_CheckedChanged(object sender, EventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox?.Checked == true)
            {
                try
                {
                    // Décocher l'autre option et nettoyer
                    if (checkbox == checkBoxStartWithWindows)
                    {
                        checkBoxStartupRegistry.Checked = false;
                        SetStartupRegistry(false);
                    }
                    else if (checkbox == checkBoxStartupRegistry)
                    {
                        checkBoxStartWithWindows.Checked = false;
                        if (File.Exists(startupPath))
                        {
                            File.Delete(startupPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError("Error during startup method switch", ex);
                    MessageBox.Show("Error switching startup method. Please try again.", 
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                // Sauvegarder l'état du logging
                config.WriteValue("Logging", "EnableLogging", checkBoxEnableLogging.Checked.ToString());

                // Gérer le démarrage automatique selon la méthode choisie
                if (checkBoxStartWithWindows.Checked)
                {
                    // S'assurer que la méthode registre est désactivée
                    SetStartupRegistry(false);
                    // Créer le raccourci
                    CreateStartupShortcut();
                }
                else if (checkBoxStartupRegistry.Checked)
                {
                    // S'assurer que le .lnk est supprimé
                    if (File.Exists(startupPath))
                    {
                        File.Delete(startupPath);
                    }
                    // Activer la méthode registre
                    SetStartupRegistry(true);
                }
                else
                {
                    // Désactiver les deux méthodes
                    if (File.Exists(startupPath))
                    {
                        File.Delete(startupPath);
                    }
                    SetStartupRegistry(false);
                }

                logger.LogInfo("Configuration settings saved successfully");
                MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                logger.LogError("Error saving configuration settings", ex);
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
    }
} 