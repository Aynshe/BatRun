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

        public ConfigurationForm(IniFile config, Logger logger)
        {
            this.config = config;
            this.logger = logger;
            InitializeComponent();

            // Appliquer le style sombre
            FormStyles.ApplyDarkStyle(this);

            // Initialiser le chemin de démarrage automatique
            string executablePath = Application.ExecutablePath;
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

            // Vérifier si le raccourci de démarrage existe
            checkBoxStartWithWindows.Checked = File.Exists(startupPath);
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            try
            {
                // Sauvegarder les paramètres de focus
                config.WriteValue("Focus", "FocusDuration", numericFocusDuration.Value.ToString());
                config.WriteValue("Focus", "FocusInterval", numericFocusInterval.Value.ToString());

                // Sauvegarder le paramètre MinimizeWindows
                config.WriteValue("Windows", "MinimizeWindows", checkBoxMinimizeWindows.Checked.ToString());

                // Gérer le raccourci de démarrage automatique
                if (checkBoxStartWithWindows.Checked)
                {
                    CreateStartupShortcut();
                }
                else
                {
                    if (File.Exists(startupPath))
                    {
                        File.Delete(startupPath);
                    }
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

                using (var process = Process.Start(startInfo))
                {
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
            }
            catch (Exception ex)
            {
                logger.LogError("Error creating startup shortcut", ex);
                throw;
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
} 