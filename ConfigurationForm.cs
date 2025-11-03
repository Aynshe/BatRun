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
            ApplyDarkStyle();
            LocalizedStrings.LoadTranslations();
            UpdateLocalizedTexts();

            this.checkBoxHideESLoading.CheckedChanged += this.CheckBoxHideESLoading_CheckedChanged;
            startupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "BatRun.lnk");
            InitializeWallpaperControls();
            InitializeESLoadingVideo();
            InitializeStartupMethodComboBox();
            LoadSettings();
        }

        private void ApplyDarkStyle()
        {
            this.BackColor = Color.FromArgb(28, 28, 28);
            this.ForeColor = Color.White;

            foreach (Control control in this.Controls)
            {
                ApplyControlTheme(control);
            }
        }

        private void ApplyControlTheme(Control control)
        {
            control.ForeColor = Color.White;

            if (control is Button || control is ComboBox || control is NumericUpDown)
            {
                control.BackColor = Color.FromArgb(45, 45, 48);
                if (control is ComboBox combo) combo.FlatStyle = FlatStyle.Flat;
                if (control is Button btn) btn.FlatStyle = FlatStyle.Flat;
            }
            else if (control is GroupBox || control is Panel || control is TabControl)
            {
                control.BackColor = Color.FromArgb(45, 45, 48);
            }
             else
            {
                control.BackColor = Color.FromArgb(28, 28, 28);
            }

            if (control.HasChildren)
            {
                foreach (Control child in control.Controls)
                {
                    ApplyControlTheme(child);
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
                
                numericFocusDuration.Value = config.ReadInt("Focus", "FocusDuration", 9000);
                numericFocusInterval.Value = config.ReadInt("Focus", "FocusInterval", 3000);
                checkBoxMinimizeWindows.Checked = config.ReadBool("Windows", "MinimizeWindows", true);
                checkBoxHideESLoading.Checked = config.ReadBool("Windows", "HideESLoading", false);
                checkBoxShowSplashScreen.Checked = config.ReadBool("Windows", "ShowSplashScreen", true);
                checkBoxShowHotkeySplash.Checked = config.ReadBool("Windows", "ShowHotkeySplash", true);
                checkBoxEnableVibration.Checked = config.ReadBool("Controller", "EnableVibration", true);
                checkBoxEnableLogging.Checked = config.ReadBool("Logging", "EnableLogging", false);

                if (comboBoxWallpaper != null && comboBoxWallpaperFolder != null)
                {
                    string selectedWallpaper = config.ReadValue("Wallpaper", "Selected", "None");
                    LoadWallpaperFolders();
                    string selectedFolder = config.ReadValue("Wallpaper", "SelectedFolder", "/");
                    int folderIndex = comboBoxWallpaperFolder.Items.IndexOf(selectedFolder);
                    if (folderIndex >= 0)
                    {
                        comboBoxWallpaperFolder.SelectedIndex = folderIndex;
                    }
                    else
                    {
                        comboBoxWallpaperFolder.SelectedIndex = 0;
                    }

                    LoadWallpaperList();
                    if (comboBoxWallpaper.Items.Contains(selectedWallpaper))
                    {
                        comboBoxWallpaper.SelectedItem = selectedWallpaper;
                    }
                    else
                    {
                        comboBoxWallpaper.SelectedItem = "None";
                    }
                }

                if (checkBoxEnableWithExplorer != null)
                {
                    checkBoxEnableWithExplorer.Checked = config.ReadBool("Wallpaper", "EnableWithExplorer", false);
                }

                string startupMethod = "Disabled";
                if (File.Exists(startupPath))
                {
                    startupMethod = "Shortcut";
                }
                else if (IsInStartupRegistry())
                {
                    startupMethod = "Registry";
                }
                else if (IsStartupTaskExists())
                {
                    startupMethod = "Task Scheduler";
                }
                
                if (comboBoxStartupMethod.Items.Contains(startupMethod))
                {
                    comboBoxStartupMethod.SelectedItem = startupMethod;
                }
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
            comboBoxStartupMethod.Items.Clear();
            comboBoxStartupMethod.Items.Add("Disabled");
            comboBoxStartupMethod.Items.Add("Shortcut");
            comboBoxStartupMethod.Items.Add("Registry");
            comboBoxStartupMethod.Items.Add("Task Scheduler");
            
            var displayTexts = new Dictionary<string, string>
            {
                { "Disabled", LocalizedStrings.GetString("Disabled") },
                { "Shortcut", LocalizedStrings.GetString("Shortcut") },
                { "Registry", LocalizedStrings.GetString("Registry") },
                { "Task Scheduler", LocalizedStrings.GetString("Task Scheduler") }
            };

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
                bool previousHideESValue = config.ReadValue("Windows", "HideESLoading", "false") == "true";
                bool newHideESValue = checkBoxHideESLoading.Checked;

                logger.LogInfo($"HideESLoading - Previous value: {previousHideESValue}, New value: {newHideESValue}");

                SaveConfigurations();

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

                    DialogResult = DialogResult.OK;
                    Close();

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

                string script = $@"
                    $targetPath = '{targetPath.Replace("'", "''")}'
                    $workingDirectory = '{workingDirectory.Replace("'", "''")}'
                    $shortcutPath = '{startupPath.Replace("'", "''")}'

                    $shell = New-Object -ComObject WScript.Shell
                    $shortcut = $shell.CreateShortcut($shortcutPath)
                    $shortcut.TargetPath = $targetPath
                    $shortcut.WorkingDirectory = $workingDirectory
                    $shortcut.Description = 'BatRun Launcher'
                    $shortcut.Save()

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

        private void InitializeWallpaperControls()
        {
            var mainLayoutPanel = this.Controls.OfType<TableLayoutPanel>().First();

            groupBoxWallpaper = new GroupBox
            {
                Text = LocalizedStrings.GetString("Wallpaper Settings"),
                Dock = DockStyle.Fill,
                AutoSize = true
            };
            mainLayoutPanel.Controls.Add(groupBoxWallpaper, 0, 2);

            var wallpaperLayoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                AutoSize = true
            };
            wallpaperLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            wallpaperLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            groupBoxWallpaper.Controls.Add(wallpaperLayoutPanel);

            comboBoxWallpaperFolder = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            wallpaperLayoutPanel.Controls.Add(comboBoxWallpaperFolder, 0, 0);
            wallpaperLayoutPanel.SetColumnSpan(comboBoxWallpaperFolder, 2);
            comboBoxWallpaperFolder.SelectedIndexChanged += (s, e) => LoadWallpaperList();

            comboBoxWallpaper = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            wallpaperLayoutPanel.Controls.Add(comboBoxWallpaper, 0, 1);
            wallpaperLayoutPanel.SetColumnSpan(comboBoxWallpaper, 2);

            checkBoxEnableWithExplorer = new CheckBox { Text = LocalizedStrings.GetString("Enable wallpaper even with Explorer running"), AutoSize = true };
            wallpaperLayoutPanel.Controls.Add(checkBoxEnableWithExplorer, 0, 2);
            wallpaperLayoutPanel.SetColumnSpan(checkBoxEnableWithExplorer, 2);

            var checkBoxLoopVideo = new CheckBox { Text = LocalizedStrings.GetString("Loop video wallpapers"), AutoSize = true, Checked = config.ReadBool("Wallpaper", "LoopVideo", true) };
            wallpaperLayoutPanel.Controls.Add(checkBoxLoopVideo, 0, 3);
            wallpaperLayoutPanel.SetColumnSpan(checkBoxLoopVideo, 2);
            checkBoxLoopVideo.CheckedChanged += (s, e) => config.WriteValue("Wallpaper", "LoopVideo", checkBoxLoopVideo.Checked.ToString());

            var checkBoxEnableAudio = new CheckBox { Text = LocalizedStrings.GetString("Enable video audio"), AutoSize = true, Checked = config.ReadBool("Wallpaper", "EnableAudio", false) };
            wallpaperLayoutPanel.Controls.Add(checkBoxEnableAudio, 0, 4);
            wallpaperLayoutPanel.SetColumnSpan(checkBoxEnableAudio, 2);
            checkBoxEnableAudio.CheckedChanged += (s, e) => config.WriteValue("Wallpaper", "EnableAudio", checkBoxEnableAudio.Checked.ToString());

            var buttonTestWallpaper = new Button { Text = LocalizedStrings.GetString("Test Wallpaper"), Dock = DockStyle.Fill };
            wallpaperLayoutPanel.Controls.Add(buttonTestWallpaper, 0, 5);

            var buttonCloseWallpaper = new Button { Text = LocalizedStrings.GetString("Close Wallpaper"), Dock = DockStyle.Fill };
            wallpaperLayoutPanel.Controls.Add(buttonCloseWallpaper, 1, 5);

            checkBoxEnableWithExplorer.CheckedChanged += (s, e) =>
            {
                config.WriteValue("Wallpaper", "EnableWithExplorer", checkBoxEnableWithExplorer.Checked.ToString());
                var wallpaperManager = new WallpaperManager(config, logger, program);
                if (Process.GetProcessesByName("explorer").Length > 0)
                {
                    if (checkBoxEnableWithExplorer.Checked) wallpaperManager.ShowWallpaper(forceShow: true);
                    else wallpaperManager.CloseWallpaper();
                }
            };
            buttonTestWallpaper.Click += (s, e) =>
            {
                try
                {
                    WallpaperManager.StopActiveMediaPlayer();
                    WallpaperManager.CleanupAllInstances();
                    GC.Collect(2, GCCollectionMode.Forced, true);
                    GC.WaitForPendingFinalizers();

                    if (comboBoxWallpaper?.SelectedItem == null)
                    {
                        MessageBox.Show(LocalizedStrings.GetString("Please select a wallpaper"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    string selectedWallpaper = comboBoxWallpaper.SelectedItem.ToString() ?? "None";
                    config.WriteValue("Wallpaper", "Selected", selectedWallpaper);
                    config.WriteValue("Wallpaper", "EnableWithExplorer", checkBoxEnableWithExplorer.Checked.ToString());
                    var wallpaperManager = new WallpaperManager(config, logger, program);
                    wallpaperManager.ShowWallpaper(forceShow: true);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error testing wallpaper: {ex.Message}", ex);
                    MessageBox.Show($"Error testing wallpaper: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            buttonCloseWallpaper.Click += (s, e) =>
            {
                try
                {
                    WallpaperManager.StopActiveMediaPlayer();
                    WallpaperManager.CleanupAllInstances();
                    GC.Collect(2, GCCollectionMode.Forced, true);
                    GC.WaitForPendingFinalizers();
                    var wallpaperManager = new WallpaperManager(config, logger, program);
                    wallpaperManager.CloseWallpaper();
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error closing wallpaper: {ex.Message}", ex);
                    MessageBox.Show($"Error closing wallpaper: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            LoadWallpaperFolders();
        }

        private void LoadWallpaperFolders()
        {
            if (comboBoxWallpaperFolder == null) return;
            comboBoxWallpaperFolder.Items.Clear();
            comboBoxWallpaperFolder.Items.Add("/");
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
            checkBoxShowSplashScreen.Enabled = !isHideESChecked;
            checkBoxShowHotkeySplash.Enabled = !isHideESChecked;
            checkBoxMinimizeWindows.Enabled = !isHideESChecked;
            if (isHideESChecked)
            {
                checkBoxShowSplashScreen.Checked = false;
                checkBoxShowHotkeySplash.Checked = false;
                checkBoxMinimizeWindows.Checked = false;

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
            string retrobatPath = program.GetRetrobatPath();
            if (!string.IsNullOrEmpty(retrobatPath))
            {
                string scriptPath = Path.Combine(Path.GetDirectoryName(retrobatPath) ?? "", "emulationstation", ".emulationstation", "scripts", "system-selected");
                Directory.CreateDirectory(scriptPath);
                string scriptFile = Path.Combine(scriptPath, "notify_batrun.bat");
                if (isHideESChecked)
                {
                    string scriptContent = $@"@echo off
set Focus_BatRun_path={AppContext.BaseDirectory}
start ""BatRun_Focus_ES"" ""%Focus_BatRun_path%\BatRun.exe"" -ES_System_select";
                    File.WriteAllText(scriptFile, scriptContent);
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
                    if (File.Exists(scriptFile))
                    {
                        File.Delete(scriptFile);
                    }
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

        public void UpdateStartupState(bool isCustomUIEnabled)
        {
            try
            {
                if (isCustomUIEnabled)
                {
                    logger.LogInfo("Custom UI is enabled, disabling startup methods...");
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

                exePath = exePath.Replace("\"", "\\\"");
                workingDir = workingDir.Replace("\"", "\\\"");

                string script = $@"
                    Import-Module ScheduledTasks
                    try {{
                        $action = New-ScheduledTaskAction -Execute '{exePath}' -WorkingDirectory '{workingDir}'
                        $trigger = New-ScheduledTaskTrigger -AtLogon -User '{domainUser}'
                        $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit 0
                        $principal = New-ScheduledTaskPrincipal -GroupId 'BUILTIN\Users' -RunLevel Limited
                        Unregister-ScheduledTask -TaskName 'BatRun_Startup' -Confirm:$false -ErrorAction SilentlyContinue
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
            }
        }

        private void HandleStartupMethod()
        {
            string startupMethod = comboBoxStartupMethod.SelectedItem?.ToString() ?? "Disabled";
            if (startupMethod.Contains("Value ="))
            {
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
                        RemoveStartupTask();
                        logger.LogInfo("All startup methods disabled successfully");
                        break;

                    case "Shortcut":
                        logger.LogInfo("Creating startup shortcut...");
                        SetStartupRegistry(false);
                        RemoveStartupTask();
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
                        RemoveStartupTask();
                        SetStartupRegistry(true);
                        logger.LogInfo("Registry startup set successfully");
                        break;

                    case "Task Scheduler":
                        logger.LogInfo("Setting up task scheduler startup...");
                        if(File.Exists(startupPath))
                        {
                            logger.LogInfo("Removing existing shortcut...");
                            File.Delete(startupPath);
                        }
                        SetStartupRegistry(false);
                        CreateStartupTask();
                        logger.LogInfo("Task scheduler startup set successfully");
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

            var windowsLayoutPanel = groupBoxWindows.Controls.OfType<TableLayoutPanel>().First();

            buttonESLoadingConfig = new Button
            {
                Text = LocalizedStrings.GetString("MediaPlayer Settings"),
                Enabled = checkBoxHideESLoading.Checked,
                Dock = DockStyle.Fill
            };
            windowsLayoutPanel.Controls.Add(buttonESLoadingConfig, 0, 7);
            windowsLayoutPanel.SetColumnSpan(buttonESLoadingConfig, 2);

            buttonESLoadingConfig.Click += ButtonESLoadingConfig_Click;

            checkBoxHideESLoading.CheckedChanged += (s, e) =>
            {
                if (buttonESLoadingConfig != null)
                {
                    buttonESLoadingConfig.Enabled = checkBoxHideESLoading.Checked;
                }
            };
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

                config.WriteValue("Windows", "HideESLoading", checkBoxHideESLoading.Checked.ToString().ToLower());
                logger.LogInfo($"Saved HideESLoading: {checkBoxHideESLoading.Checked}");
                config.WriteValue("Focus", "FocusDuration", numericFocusDuration.Value.ToString());
                config.WriteValue("Focus", "FocusInterval", numericFocusInterval.Value.ToString());
                config.WriteValue("Windows", "MinimizeWindows", checkBoxMinimizeWindows.Checked.ToString().ToLower());
                config.WriteValue("Windows", "ShowSplashScreen", checkBoxShowSplashScreen.Checked.ToString().ToLower());
                config.WriteValue("Windows", "ShowHotkeySplash", checkBoxShowHotkeySplash.Checked.ToString().ToLower());
                config.WriteValue("Controller", "EnableVibration", checkBoxEnableVibration.Checked.ToString().ToLower());
                config.WriteValue("Logging", "EnableLogging", checkBoxEnableLogging.Checked.ToString().ToLower());

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
