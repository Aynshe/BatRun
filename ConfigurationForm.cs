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
        private readonly LocalizedStrings strings;

        private const string RUN_REGISTRY_KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string APP_NAME = "BatRun";

        public ConfigurationForm(IniFile config, Logger logger)
        {
            this.config = config;
            this.logger = logger;
            this.strings = new LocalizedStrings();
            
            InitializeComponent();
            FormStyles.ApplyDarkStyle(this);
            UpdateLocalizedTexts();

            // Appliquer le style sombre
            FormStyles.ApplyDarkStyle(this);

            // Initialiser le chemin de démarrage automatique
            startupPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                "BatRun.lnk");

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
        }

        private void LoadSettings()
        {
            // Charger les paramètres de focus
            numericFocusDuration.Value = config.ReadInt("Focus", "FocusDuration", 15000);
            numericFocusInterval.Value = config.ReadInt("Focus", "FocusInterval", 5000);

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
    }
} 