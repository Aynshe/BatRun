using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SDL2;
using Microsoft.Win32;
using System.Text;
using BatRun; // Ajout de l'espace de noms
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX.XInput;
using SharpDX.DirectInput;

// line 194 version number

namespace BatRun
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool AllowSetForegroundWindow(int dwProcessId);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        public const int SW_RESTORE = 9;
        public const int SW_SHOW = 5;
        public const int SW_SHOWNOACTIVATE = 4;
        public const int SW_MINIMIZE = 6;

        [DllImport("kernel32.dll")]
        public static extern bool SetProcessWorkingSetSize(IntPtr process, int minimumWorkingSetSize, int maximumWorkingSetSize);
    }

    public class IniFile
    {
        private string filePath;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern uint GetPrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpDefault,
            StringBuilder lpReturnedString,
            uint nSize,
            string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool WritePrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpString,
            string lpFileName);

        public IniFile(string filePath)
        {
            this.filePath = filePath;
            EnsureConfigFileExists();
        }

        private void EnsureConfigFileExists()
        {
            if (!File.Exists(filePath))
            {
                var directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Write default values
                WriteValue("Focus", "FocusDuration", "9000");
                WriteValue("Focus", "FocusInterval", "3000");
                WriteValue("Windows", "MinimizeWindows", "true");
                WriteValue("Logging", "EnableLogging", "false");
            }
        }

        public void WriteValue(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, filePath);
        }

        public string ReadValue(string section, string key, string defaultValue)
        {
            StringBuilder result = new StringBuilder(255);
            GetPrivateProfileString(section, key, defaultValue, result, (uint)result.Capacity, filePath);
            return result.ToString();
        }

        public int ReadInt(string section, string key, int defaultValue)
        {
            string value = ReadValue(section, key, defaultValue.ToString());
            return int.TryParse(value, out int result) ? result : defaultValue;
        }

        public bool ReadBool(string section, string key, bool defaultValue)
        {
            string value = ReadValue(section, key, defaultValue.ToString()).ToLower();
            return value == "true" || value == "1";
        }
    }

    public class Batrun : IBatRunProgram, IDisposable
    {
        private NotifyIcon? trayIcon;
        public IniFile config;
        private readonly string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
        private readonly Logger logger;
        private readonly LoggingConfig _loggingConfig = new LoggingConfig();
        private MainForm? mainForm;
        private readonly ControllerService _controllerService;
        private readonly RetroBatService _retroBatService;

        // Ajout de DInputHandler avec initialisation
        private DInputHandler dInputHandler = new DInputHandler(new Logger("BatRun.log"));

        private object launchLock = new object();
        private DateTime lastLaunchTime = DateTime.MinValue;
        private const int LAUNCH_COOLDOWN_MS = 5000; // 5 seconds between launches

        private List<IntPtr> activeWindows = new List<IntPtr>();

        private bool _emulationStationPollingLogged = false;

        private WallpaperManager? wallpaperManager;

        public const string APP_VERSION = "2.1.2";

        private bool hideESLoading;
        private static int esSystemSelectCount = 0;
        private static DateTime lastESSystemSelectTime = DateTime.MinValue;
        private static bool isESStarting = false;
        private static readonly object esLockObject = new();

        private bool skipFocusSequence = false;

        private ESLoadingPlayer? esLoadingPlayer;

        private DateTime lastStartButtonTime = DateTime.MinValue;
        private const int START_BUTTON_COOLDOWN_MS = 1000; // 1 seconde de cooldown

        public Batrun()
        {
            // Initialisation minimale
            config = new IniFile(Path.Combine(AppContext.BaseDirectory, "config.ini"));
            logger = new Logger("BatRun.log");
            hideESLoading = config.ReadBool("Windows", "HideESLoading", false);

            // Initialize services
            _retroBatService = new RetroBatService(logger, config);
            var retrobatPath = _retroBatService.GetRetrobatPath();
            logger.Log("Starting BatRun");

            _controllerService = new ControllerService(logger, config, retrobatPath);
            _controllerService.HotkeyCombinationPressed += OnHotkeyCombinationPressed;
            _controllerService.Initialize();

            // Démarrer l'écoute des signaux ES_System_select
            if (hideESLoading)
            {
                StartESSystemSelectListener();
            }

            // Exécuter les commandes shell configurées
            Task.Run(async () =>
            {
                try
                {
                    var shellExecutor = new ShellCommandExecutor(config, logger, this, wallpaperManager);
                    await shellExecutor.ExecuteShellCommandsAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError("Error executing shell commands", ex);
                }
            });

            // Initialiser le WallpaperManager après l'initialisation du chemin RetroBat
            wallpaperManager = new WallpaperManager(config, logger, this);

            // Vérifier si explorer.exe est en cours d'exécution
            CheckExplorerAndInitialize();

            // Initialisation de DInputHandler
            dInputHandler = new DInputHandler(logger);

            // Create a timer to check EmulationStation status
            var checkTimer = new System.Windows.Forms.Timer();
            checkTimer.Interval = 5000; // Check every 5 seconds
            bool wasRunning = false;
            checkTimer.Tick += (sender, e) =>
            {
                bool isRunning = _retroBatService.IsEmulationStationRunning();
                bool shouldPause = isRunning || (esLoadingPlayer != null && hideESLoading);

                if (wasRunning && !shouldPause)
                {
                    logger.Log("EmulationStation has stopped and no loading video, resuming polling and media");
                    wallpaperManager?.ResumeMedia();
                    _controllerService.StartPolling();
                }
                else if (!wasRunning && shouldPause)
                {
                    logger.Log("EmulationStation has started or loading video is playing, pausing media");
                    if (wallpaperManager != null)
                    {
                        wallpaperManager.EnablePauseAndBlackBackground();
                        wallpaperManager.PauseMedia();
                    }
                }
                wasRunning = shouldPause;
            };
            checkTimer.Start();
        }

        private async void OnHotkeyCombinationPressed(object? sender, EventArgs e)
        {
            await LaunchRetrobat();
        }

        private async Task LaunchRetrobat()
        {
            try
            {
                if (!_retroBatService.IsEmulationStationRunning())
                {
                    // Toujours réinitialiser l'état au début d'un nouveau lancement
                    lock (esLockObject)
                    {
                        isESStarting = true;
                        esSystemSelectCount = 0;
                        lastESSystemSelectTime = DateTime.MinValue;
                        logger.LogInfo("ES state reset for new launch");
                    }

                    if (hideESLoading)
                    {
                        if (wallpaperManager != null)
                        {
                            wallpaperManager.EnablePauseAndBlackBackground();
                            wallpaperManager.PauseMedia();
                        }

                        esLoadingPlayer = new ESLoadingPlayer(config, logger, wallpaperManager!);
                        string videoPath = Path.Combine(AppContext.BaseDirectory, "ESloading", config.ReadValue("Windows", "ESLoadingVideo", "None"));
                        await esLoadingPlayer.PlayLoadingVideo(videoPath);

                        wallpaperManager?.BringToFront();
                        skipFocusSequence = true;
                    }

                    bool showHotkeySplash = config.ReadBool("Windows", "ShowHotkeySplash", true);
                    HotkeySplashForm? splash = null;

                    if (showHotkeySplash)
                    {
                        await Task.Run(() =>
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                splash = new HotkeySplashForm();
                                splash.Show();
                                Application.DoEvents();
                            });
                        });
                        await Task.Delay(2500);
                    }

                    MinimizeActiveWindows();

                    await _retroBatService.Launch();

                    if (splash != null)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            splash.Close();
                            splash.Dispose();
                        });
                    }

                    int maxAttempts = 10;
                    int attemptDelay = 2000;
                    int currentAttempt = 0;
                    Process? esProcess = null;

                    while (currentAttempt < maxAttempts && esProcess == null)
                    {
                        await Task.Delay(attemptDelay);
                        esProcess = Process.GetProcessesByName("emulationstation").FirstOrDefault();
                        currentAttempt++;

                        if (esProcess == null)
                        {
                            logger.LogInfo($"Waiting for EmulationStation to start (attempt {currentAttempt}/{maxAttempts})");
                        }
                    }

                    if (esProcess != null)
                    {
                        esProcess.EnableRaisingEvents = true;
                        esProcess.Exited += async (sender, args) =>
                        {
                            logger.LogInfo("EmulationStation process exited, cleaning up...");
                            await Task.Run(() => RestoreActiveWindows().Wait());
                            wallpaperManager?.ResumeMedia();
                            _controllerService.StartPolling();
                        };

                        await SetEmulationStationFocus();
                    }
                    else
                    {
                        logger.LogError("EmulationStation failed to start");
                        lock (esLockObject)
                        {
                            isESStarting = false;
                            esSystemSelectCount = 0;
                            lastESSystemSelectTime = DateTime.MinValue;
                        }
                    }
                }
                else
                {
                    logger.LogInfo("Retrobat is already running");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error launching Retrobat", ex);
                lock (esLockObject)
                {
                    isESStarting = false;
                    esSystemSelectCount = 0;
                    lastESSystemSelectTime = DateTime.MinValue;
                }
            }
        }

        private void MinimizeActiveWindows()
        {
            // Read the minimize windows setting from the INI file
            bool minimizeWindows = config.ReadBool("Windows", "MinimizeWindows", true);

            if (!minimizeWindows)
            {
                logger.LogInfo("Window minimization disabled by configuration");
                return;
            }

            logger.LogInfo("Minimizing active windows");
            activeWindows.Clear();

            NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                if (NativeMethods.IsWindowVisible(hWnd))
                {
                    int length = NativeMethods.GetWindowTextLength(hWnd);
                    if (length > 0)
                    {
                        StringBuilder sb = new StringBuilder(length + 1);
                        NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
                        string title = sb.ToString();

                        // Ignore our own window and certain system windows
                        if (!string.IsNullOrEmpty(title) &&
                            !title.Contains("BatRun") &&
                            !title.Contains("Program Manager"))
                        {
                            activeWindows.Add(hWnd);
                            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_MINIMIZE);
                            logger.LogInfo($"Window minimized: {title}");
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);
        }

        private async Task RestoreActiveWindows()
        {
            // logger.LogInfo("Preparing to restore windows");

            // Reset the logging flag
            _emulationStationPollingLogged = false;

            // Wait for an initial delay to allow processes to start
            await Task.Delay(3000);

            int maxAttempts = 15;
            int currentAttempt = 0;

            while (currentAttempt < maxAttempts)
            {
                var emulationStationProcesses = Process.GetProcessesByName("emulationstation");

                if (emulationStationProcesses.Length == 0)
                {
                    logger.LogInfo("No EmulationStation processes found, restoring windows.");

                    // Restore all windows in parallel
                    var tasks = activeWindows.Select(async hWnd =>
                    {
                        try
                        {
                            await Task.Run(() => NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE));
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"Error restoring window: {ex.Message}");
                        }
                    });

                    await Task.WhenAll(tasks); // Ensure all tasks are awaited
                    activeWindows.Clear();
                    logger.LogInfo("Window restoration complete");
                    return;
                }
                else
                {
                    LogEmulationStationPolling();
                    await Task.Delay(1000);
                    currentAttempt++;
                }
            }

            // If EmulationStation processes are still running
            // logger.LogWarning("Unable to restore windows: EmulationStation processes still active.");
        }

        private async Task RestoreWindowsAfterEmulationStation()
        {
            Process[] processes = Process.GetProcessesByName("emulationstation");
            if (processes.Length > 0)
            {
                logger.LogInfo("EmulationStation is still running, waiting for it to close...");
                while (processes.Length > 0)
                {
                    await Task.Delay(1000);
                    processes = Process.GetProcessesByName("emulationstation");
                }
                logger.LogInfo("EmulationStation has closed, proceeding to restore windows.");
            }
            else
            {
                logger.LogInfo("No EmulationStation processes found, restoring windows immediately.");
            }

            // Restore all windows in parallel
            var tasks = activeWindows.Select(async hWnd =>
            {
                try
                {
                    await Task.Run(() => NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE));
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error restoring window: {ex.Message}");
                }
            });

            await Task.WhenAll(tasks);
            activeWindows.Clear();

            // Réinitialiser les états d'ES et des notifications
            lock (esLockObject)
            {
                isESStarting = false;
                esSystemSelectCount = 0;
                lastESSystemSelectTime = DateTime.MinValue;

                // Nettoyer l'instance ESLoadingPlayer si elle existe encore
                if (esLoadingPlayer != null)
                {
                    esLoadingPlayer.CloseVideo();
                    esLoadingPlayer.Dispose();
                    esLoadingPlayer = null;
                }
            }

            logger.LogInfo("Window restoration complete");
            logger.LogInfo("Controller polling started");
            logger.LogInfo("ES notification system reset");
        }

        private async Task SetEmulationStationFocus()
        {
            // Si skipFocusSequence est true, ne pas exécuter la séquence de focus
            if (skipFocusSequence)
            {
                logger.LogInfo("Focus sequence skipped due to Hide ES during loading option");
                return;
            }

            // Read values from the INI file
            int focusDuration = config.ReadInt("Focus", "FocusDuration", 30000);
            int focusInterval = config.ReadInt("Focus", "FocusInterval", 5000);
            int elapsedTime = 0;

            logger.LogInfo($"Starting focus sequence for EmulationStation (Duration: {focusDuration}ms, Interval: {focusInterval}ms)");

            while (elapsedTime < focusDuration)
            {
                try
                {
                    // Complete focus sequence
                    var process = Process.GetProcessesByName("emulationstation").FirstOrDefault();
                    IntPtr hWnd = process?.MainWindowHandle ?? IntPtr.Zero;

                    if (hWnd != IntPtr.Zero)
                    {
                        // Check if process is not null before accessing its Id
                        if (process != null)
                        {
                            // Allow focus change for this process
                            NativeMethods.AllowSetForegroundWindow(process.Id);

                            // Get thread IDs
                            uint foregroundThread = NativeMethods.GetWindowThreadProcessId(
                                NativeMethods.GetForegroundWindow(), IntPtr.Zero);
                            uint appThread = NativeMethods.GetCurrentThreadId();

                            // Attach threads for focus
                            bool threadAttached = false;
                            if (foregroundThread != appThread)
                            {
                                threadAttached = NativeMethods.AttachThreadInput(foregroundThread, appThread, true);
                            }

                            try
                            {
                                NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
                                NativeMethods.ShowWindow(hWnd, NativeMethods.SW_SHOW);
                                NativeMethods.BringWindowToTop(hWnd);
                                NativeMethods.SetForegroundWindow(hWnd);

                                logger.LogInfo($"Focus applied to EmulationStation (attempt {elapsedTime/focusInterval + 1}/{focusDuration/focusInterval})");
                            }
                            finally
                            {
                                // Detach threads if necessary
                                if (threadAttached)
                                {
                                    NativeMethods.AttachThreadInput(foregroundThread, appThread, false);
                                }
                            }
                        }
                        else
                        {
                            logger.LogError("Process is null, cannot set foreground window.");
                        }
                    }
                    else
                    {
                        logger.LogInfo("EmulationStation window handle not found");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError("Error setting focus to EmulationStation", ex);
                }

                await Task.Delay(focusInterval);
                elapsedTime += focusInterval;
            }

            logger.LogInfo("End of focus sequence for EmulationStation");

            await RestoreWindowsAfterEmulationStation();
        }

        private void InitializeTrayIcon()
        {
            var strings = new LocalizedStrings();

            try
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
                Icon? appIcon = null;

                if (File.Exists(iconPath))
                {
                    appIcon = new Icon(iconPath);
                }
                else
                {
                    logger.LogError($"Icon file not found at: {iconPath}");
                    appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                }

                trayIcon = new NotifyIcon
                {
                    Icon = appIcon,
                    Text = "BatRun",
                    Visible = true
                };
            }
            catch (Exception ex)
            {
                logger.LogError("Error initializing tray icon", ex);
                return; // Sortir de la méthode si l'initialisation de l'icône échoue
            }

            try
            {
                var contextMenu = new ContextMenuStrip();

                // Groupe principal
                contextMenu.Items.Add(strings.OpenBatRun, null, (s, e) => SafeExecute(ShowMainForm));
                contextMenu.Items.Add(new ToolStripSeparator());
                contextMenu.Items.Add(strings.OpenEmulationStation, null, (s, e) => SafeExecute(LaunchEmulationStation));
                contextMenu.Items.Add(strings.LaunchBatGui, null, (s, e) => SafeExecute(LaunchBatGui));
                contextMenu.Items.Add(new ToolStripSeparator());

                // Groupe Configuration
                var configMenuItem = new ToolStripMenuItem(strings.Configuration);
                configMenuItem.DropDownItems.Add(strings.GeneralSettings, null, (s, e) => SafeExecute(ShowConfigWindow));
                configMenuItem.DropDownItems.Add(strings.ControllerMappings, null, (s, e) => SafeExecute(OpenMappingConfiguration));
                configMenuItem.DropDownItems.Add(strings.ShellLauncher, null, (s, e) => SafeExecute(() => ShowShellConfigWindow(null)));
                contextMenu.Items.Add(configMenuItem);

                // Groupe Aide
                var helpMenuItem = new ToolStripMenuItem(strings.Help);
                helpMenuItem.DropDownItems.Add(strings.ViewLogs, null, (s, e) => SafeExecute(OpenLogFile));
                helpMenuItem.DropDownItems.Add(strings.ViewErrorLogs, null, (s, e) => SafeExecute(OpenErrorLogFile));
                helpMenuItem.DropDownItems.Add(strings.About, null, (s, e) => SafeExecute(() => ShowAbout(null, EventArgs.Empty)));
                helpMenuItem.DropDownItems.Add(new ToolStripSeparator());
                helpMenuItem.DropDownItems.Add(LocalizedStrings.GetString("Check for Updates"), null, async (s, e) =>
                {
                    try
                    {
                        var progressForm = new Form
                        {
                            Text = LocalizedStrings.GetString("Checking for Updates"),
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
                            Text = LocalizedStrings.GetString("Checking for updates..."),
                            Dock = DockStyle.Fill,
                            TextAlign = ContentAlignment.MiddleCenter
                        };

                        progressForm.Controls.Add(progressLabel);
                        progressForm.Show();

                        var updateChecker = new UpdateChecker(logger, APP_VERSION);
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
                                string.Format(LocalizedStrings.GetString("New version {0} is available. Would you like to update?"), result.LatestVersion),
                                LocalizedStrings.GetString("Update Available"),
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Information);

                            if (dialogResult == DialogResult.Yes)
                            {
                                var downloadProgress = new Progress<int>(percent =>
                                {
                                    progressLabel.Text = string.Format(LocalizedStrings.GetString("Downloading update: {0}%"), percent);
                                });

                                progressForm = new Form
                                {
                                    Text = LocalizedStrings.GetString("Downloading Update"),
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
                                    Text = LocalizedStrings.GetString("Starting download..."),
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
                                        LocalizedStrings.GetString("Failed to install update. Please try again later."),
                                        LocalizedStrings.GetString("Update Failed"),
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show(
                                LocalizedStrings.GetString("You have the latest version."),
                                LocalizedStrings.GetString("No Updates Available"),
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error checking for updates: {ex.Message}", ex);
                        MessageBox.Show(
                            LocalizedStrings.GetString("Failed to check for updates. Please try again later."),
                            LocalizedStrings.GetString("Error"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                });
                contextMenu.Items.Add(helpMenuItem);

                contextMenu.Items.Add(new ToolStripSeparator());
                contextMenu.Items.Add(strings.Exit, null, (s, e) => SafeExecute(Application.Exit));

                if (trayIcon != null)
                {
                    trayIcon.ContextMenuStrip = contextMenu;
                    // Modifier le comportement du double-clic pour afficher la fenêtre principale
                    trayIcon.MouseDoubleClick += (s, e) => SafeExecute(ShowMainForm);
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error creating context menu", ex);
            }
        }

        public void SafeExecute(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                logger.LogError($"Error executing action: {ex.Message}", ex);
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void LaunchEmulationStation()
        {
            _retroBatService.LaunchEmulationStation();
        }

        public void LaunchBatGui()
        {
            string retrobatPath = _retroBatService.GetRetrobatPath();
            string batGuiPath = Path.Combine(Path.GetDirectoryName(retrobatPath) ?? string.Empty, "BatGui.exe");
            if (File.Exists(batGuiPath))
            {
                Process.Start(batGuiPath);
                logger.Log("Launching BatGui.exe");
            }
            else
            {
                logger.LogError("BatGui.exe not found.");
            }
        }

        public void ShowConfigWindow()
        {
            try
            {
                if (Application.OpenForms.OfType<ConfigurationForm>().Any())
                {
                    return;
                }

                var configForm = new ConfigurationForm(config, logger, this);
                configForm.Show();
            }
            catch (Exception ex)
            {
                logger.LogError($"Error showing configuration form: {ex.Message}", ex);
            }
        }

        public void OpenMappingConfiguration()
        {
            ButtonMapping mapping = new ButtonMapping();
            string exePath = AppContext.BaseDirectory;
            string parentPath = Path.GetDirectoryName(exePath) ?? exePath;
            string jsonPath = Path.Combine(parentPath, "buttonMappings.json");
            mapping.LoadMappings(jsonPath);
            MappingConfigurationForm configForm = new MappingConfigurationForm(mapping);
            configForm.ShowDialog();

            // Recharger les mappings après la configuration
            LoadDirectInputMappings();
        }

        public void OpenLogFile()
        {
            try
            {
                // Get the full path to the log file
                string logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
                string logFilePath = Path.Combine(logDirectory, "BatRun.log");

                // Check if the file exists
                if (File.Exists(logFilePath))
                {
                    // Open the file with the default text editor
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = logFilePath,
                        UseShellExecute = true
                    });

                    logger.Log($"Opening log file: {logFilePath}");
                }
                else
                {
                    logger.LogError($"Log file not found: {logFilePath}");
                    MessageBox.Show($"Log file not found: {logFilePath}", "File not found",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error opening log file", ex);
                MessageBox.Show($"Error opening log file: {ex.Message}", "Error");
            }
        }

        public void OpenErrorLogFile()
        {
            try
            {
                string logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
                string errorLogPath = Path.Combine(logDirectory, "BatRun_error.log");

                // Créer le fichier s'il n'existe pas
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                if (!File.Exists(errorLogPath))
                {
                    File.WriteAllText(errorLogPath, $"Error log file created on {DateTime.Now}\n");
                }

                // Ouvrir le fichier avec l'éditeur par défaut
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = errorLogPath,
                    UseShellExecute = true
                });

                logger.Log($"Opening error log file: {errorLogPath}");
            }
            catch (Exception ex)
            {
                logger.LogError("Error opening error log file", ex);
                MessageBox.Show($"Error opening error log file: {ex.Message}", "Error");
            }
        }

        public void ShowAbout(object? sender, EventArgs e)
        {
            var strings = new LocalizedStrings();

            var aboutForm = new Form
            {
                Text = strings.AboutBatRun,
                Size = new Size(400, 300),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.White
            };

            var mainText = new Label
            {
                Text = $"BatRun\n\n" +
                       $"{strings.VersionLabel}: {APP_VERSION}\n" +
                       $"{strings.DevelopedBy}\n\n" +
                       $"{strings.Description}",
                Dock = DockStyle.Top,
                Height = 150,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.White
            };

            var discordLink = new LinkLabel
            {
                Text = strings.JoinDiscord,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 10F),
                LinkColor = Color.FromArgb(114, 137, 218), // Discord color
                ActiveLinkColor = Color.FromArgb(134, 157, 238),
                VisitedLinkColor = Color.FromArgb(114, 137, 218)
            };
            discordLink.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://discord.gg/GVcPNxwzuT",
                UseShellExecute = true
            });

            var githubLink = new LinkLabel
            {
                Text = strings.SourceCode,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 10F),
                LinkColor = Color.White,
                ActiveLinkColor = Color.LightGray,
                VisitedLinkColor = Color.White
            };
            githubLink.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/Aynshe/BatRun",
                UseShellExecute = true
            });

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Bottom,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };
            okButton.FlatAppearance.BorderSize = 0;

            aboutForm.Controls.AddRange(new Control[] { mainText, discordLink, githubLink, okButton });

            logger.Log("Showing about window");
            aboutForm.ShowDialog();
        }


        private void LogErrorWithException(string message, Exception? ex = null)
        {
            if (ex != null)
            {
                logger.LogError($"{message}: {ex.Message}", ex);
            }
            else
            {
                logger.LogError(message);
            }
        }

        private async Task StartEmulationStation()
        {
            const int maxWaitTime = 30; // Temps maximum d'attente en secondes
            int elapsedTime = 0;
            int checkInterval = 1000; // Vérification toutes les secondes

            while (elapsedTime < maxWaitTime)
            {
                if (IsEmulationStationRunning())
                {
                    // Lancer EmulationStation
                    LaunchEmulationStation();
                    return; // Sortir de la méthode si EmulationStation a été lancé
                }

                // Log de la tentative
                logger.LogInfo($"EmulationStation not found, retrying in {checkInterval / 1000} seconds...");
                await Task.Delay(checkInterval); // Attendre avant de vérifier à nouveau
                elapsedTime += checkInterval / 1000; // Convertir en secondes
            }

            logger.LogError("EmulationStation failed to start after 30 seconds.");
        }

        private void LogEmulationStationPolling()
        {
            if (!_emulationStationPollingLogged)
            {
                logger.LogInfo("EmulationStation is running, suspending polling");
                _emulationStationPollingLogged = true;
            }
        }

        private void ShowMainForm()
        {
            if (mainForm == null || mainForm.IsDisposed)
            {
                mainForm = new MainForm(this, logger, config);

                // S'assurer que l'icône est héritée du programme principal
                if (this.Icon != null)
                {
                    mainForm.Icon = (Icon)this.Icon.Clone();
                }
            }

            // Configurer l'affichage de la fenêtre
            mainForm.Show();
            mainForm.WindowState = FormWindowState.Normal;
            mainForm.ShowInTaskbar = true;
            mainForm.BringToFront();
            main.Activate();
        }

        public async Task CheckForUpdates()
        {
            try
            {
                logger.Log("Checking for updates...");
                var updateChecker = new UpdateChecker(logger, APP_VERSION);
                var result = await updateChecker.CheckForUpdates();

                if (result.UpdateAvailable)
                {
                    logger.Log($"Update available: version {result.LatestVersion}");
                }
                else
                {
                    logger.Log("No updates available");
                }
            }
            catch (Exception ex)
            {
                LogErrorWithException("Error checking for updates", ex);
            }
        }

        public string GetAppVersion()
        {
            return APP_VERSION;
        }

        private void CheckExplorerAndInitialize()
        {
            bool isExplorerRunning = Process.GetProcessesByName("explorer").Length > 0;
            bool enableWithExplorer = config?.ReadBool("Wallpaper", "EnableWithExplorer", false) ?? false;
            string selectedWallpaper = config?.ReadValue("Wallpaper", "Selected", "None") ?? "None";
            bool isWallpaperActive = selectedWallpaper != "None";

            // Si un wallpaper est configuré, le marquer comme actif dans la config
            if (selectedWallpaper != "None")
            {
                config?.WriteValue("Wallpaper", "IsActive", "true");
                isWallpaperActive = true;
            }
            else
            {
                config?.WriteValue("Wallpaper", "IsActive", "false");
            }

            // Afficher le wallpaper si nécessaire
            if (wallpaperManager != null && isWallpaperActive)
            {
                if (!isExplorerRunning || (isExplorerRunning && enableWithExplorer))
                {
                    wallpaperManager.ShowWallpaper();
                }
            }

            // Dans tous les cas, minimiser dans le systray
            if (this != null)
            {
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                this.Hide();
            }
            InitializeTrayIcon();
        }

        private async Task InitializeAsync()
        {
            try
            {
                // Exécuter les commandes shell configurées
                var shellExecutor = new ShellCommandExecutor(config, logger);
                await shellExecutor.ExecuteShellCommandsAsync();
            }
            catch (Exception ex)
            {
                logger.LogError("Error during initialization", ex);
            }
        }

        public void ShowShellConfigWindow(ConfigurationForm? configForm = null)
        {
            try
            {
                if (Application.OpenForms.OfType<ShellConfigurationForm>().Any())
                {
                    return;
                }

                var shellConfigForm = new ShellConfigurationForm(config, logger, configForm);
                shellConfigForm.StartPosition = FormStartPosition.CenterScreen;
                shellConfigForm.Show();
            }
            catch (Exception ex)
            {
                logger.LogError($"Error showing shell configuration form: {ex.Message}", ex);
            }
        }

        private void StartESSystemSelectListener()
        {
            Task.Run(() =>
            {
                using (var eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "BatRun_ES_System_Select"))
                {
                    while (true)
                    {
                        // Attendre le signal
                        eventWaitHandle.WaitOne();

                        // Traiter le signal dans l'instance principale
                        this.Invoke(new Action(() =>
                        {
                            bool isShellMode = config.ReadBool("Shell", "EnableCustomUI", false);
                            if ((hideESLoading || isShellMode) && isESStarting && esSystemSelectCount < 5)
                            {
                                HandleESSystemSelect();
                            }
                        }));
                    }
                }
            });
        }

        private void HandleESSystemSelect()
        {
            try
            {
                lock (esLockObject)
                {
                    DateTime now = DateTime.Now;
                    if ((now - lastESSystemSelectTime).TotalSeconds > 10)
                    {
                        // Réinitialiser le compteur si plus de 10 secondes se sont écoulées
                        esSystemSelectCount = 0;
                        lastESSystemSelectTime = now;
                    }

                    if (esSystemSelectCount < 5)
                    {
                        esSystemSelectCount++;
                        lastESSystemSelectTime = now;
                        logger.LogInfo($"Received ES_System_select signal ({esSystemSelectCount}/5)");

                        // S'assurer que ESLoadingPlayer est fermé, même en mode shell
                        if (esLoadingPlayer != null)
                        {
                            bool isShellMode = config.ReadBool("Shell", "EnableCustomUI", false);
                            logger.LogInfo($"Closing ESLoadingPlayer from ES_System_select signal (Shell mode: {isShellMode})");

                            // En mode shell, forcer la fermeture de la vidéo
                            if (isShellMode)
                            {
                                esLoadingPlayer.CloseVideo();
                                esLoadingPlayer.Dispose();
                                esLoadingPlayer = null;
                            }
                            // En mode normal, fermer normalement
                            else
                            {
                                esLoadingPlayer.CloseVideo();
                                esLoadingPlayer.Dispose();
                                esLoadingPlayer = null;
                            }
                        }

                        // Réactiver la mise en pause du mediaplayer et le fond noir
                        if (wallpaperManager != null)
                        {
                            wallpaperManager.EnablePauseAndBlackBackground();
                            wallpaperManager.SendToBack();
                        }

                        // Appliquer le focus sur EmulationStation
                        FocusEmulationStation();

                        // Si on atteint le maximum, désactiver isESStarting
                        if (esSystemSelectCount >= 5)
                        {
                            isESStarting = false;
                            logger.LogInfo("Maximum ES_System_select signals reached, stopping listener");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error handling ES system select: {ex.Message}", ex);
            }
        }

        private void FocusEmulationStation()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName("EmulationStation");
                if (processes.Length > 0 && !processes[0].HasExited)
                {
                    IntPtr hWnd = processes[0].MainWindowHandle;
                    if (hWnd != IntPtr.Zero)
                    {
                        // Allow focus change for this process
                        NativeMethods.AllowSetForegroundWindow(processes[0].Id);

                        // Get thread IDs
                        uint foregroundThread = NativeMethods.GetWindowThreadProcessId(
                            NativeMethods.GetForegroundWindow(), IntPtr.Zero);
                        uint appThread = NativeMethods.GetCurrentThreadId();

                        // Attach threads for focus
                        bool threadAttached = false;
                        if (foregroundThread != appThread)
                        {
                            threadAttached = NativeMethods.AttachThreadInput(foregroundThread, appThread, true);
                        }

                        try
                        {
                            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
                            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_SHOW);
                            NativeMethods.BringWindowToTop(hWnd);
                            NativeMethods.SetForegroundWindow(hWnd);

                            logger.LogInfo("Focus forcefully applied to EmulationStation");
                        }
                        finally
                        {
                            // Detach threads if necessary
                            if (threadAttached)
                            {
                                NativeMethods.AttachThreadInput(foregroundThread, appThread, false);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error focusing EmulationStation: {ex.Message}");
            }
        }


        public static void SetESStartingState(bool state)
        {
            lock (esLockObject)
            {
                isESStarting = state;
                if (state)
                {
                    // Réinitialiser les compteurs au démarrage
                    esSystemSelectCount = 0;
                    lastESSystemSelectTime = DateTime.MinValue;
                }
            }
        }


        public async Task StartRetrobat()
        {
            try
            {
                lock (launchLock)
                {
                    if ((DateTime.Now - lastLaunchTime).TotalMilliseconds < LAUNCH_COOLDOWN_MS)
                    {
                        logger.LogInfo("Launch request ignored due to cooldown");
                        return;
                    }
                    lastLaunchTime = DateTime.Now;
                }

                // Nettoyer l'instance existante d'ESLoadingPlayer si elle existe
                if (esLoadingPlayer != null)
                {
                    esLoadingPlayer.Dispose();
                    esLoadingPlayer = null;
                }

                if (hideESLoading)
                {
                    if (wallpaperManager == null)
                    {
                        logger.LogError("WallpaperManager is null, cannot create ESLoadingPlayer");
                        return;
                    }
                    esLoadingPlayer = new ESLoadingPlayer(config, logger, wallpaperManager);
                    string videoPath = GetEmulationStationVideoPath();
                    if (!string.IsNullOrEmpty(videoPath))
                    {
                        await esLoadingPlayer.PlayLoadingVideo(videoPath);
                    }
                }
                else
                {
                    // Appliquer la séquence standard quand ESLoadingPlayer n'est pas utilisé
                    bool minimizeWindows = config.ReadBool("Windows", "MinimizeWindows", true);
                    if (minimizeWindows)
                    {
                        MinimizeActiveWindows();
                    }
                    else
                    {
                        logger.LogInfo("Window minimization disabled by configuration");
                    }
                }

                string retroBatExe = GetRetrobatPath();
                if (string.IsNullOrEmpty(retroBatExe))
                {
                    logger.LogError("RetroBAT executable not found");
                    return;
                }

                logger.LogInfo("Starting RetroBAT");
                var startInfo = new ProcessStartInfo
                {
                    FileName = retroBatExe,
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(retroBatExe) ?? string.Empty
                };

                Process.Start(startInfo);
                isESStarting = true;

                // Pause le média en cours si nécessaire
                if (wallpaperManager != null)
                {
                    logger.LogInfo("EmulationStation has started or loading video is playing, pausing media");
                    wallpaperManager.PauseMedia();
                }

                // Attendre la durée de l'intro si configurée
                await CheckIntroSettings();

                if (!hideESLoading)
                {
                    // Appliquer la séquence de focus
                    await SetEmulationStationFocus();
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error starting RetroBAT", ex);
                if (esLoadingPlayer != null)
                {
                    esLoadingPlayer.Dispose();
                    esLoadingPlayer = null;
                }
            }
        }

        private string GetEmulationStationVideoPath()
        {
            string selectedVideo = config.ReadValue("Windows", "ESLoadingVideo", "None");
            if (selectedVideo != "None")
            {
                string videoPath = Path.Combine(AppContext.BaseDirectory, "ESloading", selectedVideo);
                if (File.Exists(videoPath))
                {
                    return videoPath;
                }
                logger.LogError($"Selected video file not found: {videoPath}");
            }
            return string.Empty;
        }

        private void RestoreWindows()
        {
            try
            {
                // Réinitialiser les états liés à ES
                isESStarting = false;
                esSystemSelectCount = 0;
                lastESSystemSelectTime = DateTime.MinValue;

                // Nettoyer ESLoadingPlayer si nécessaire
                if (esLoadingPlayer != null)
                {
                    esLoadingPlayer.CloseVideo();
                    esLoadingPlayer.Dispose();
                    esLoadingPlayer = null;
                    logger.LogInfo("ESLoadingPlayer cleaned up during window restoration");
                }

                // Réinitialiser le signal ES_System_select
                using (var eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "BatRun_ES_System_Select"))
                {
                    eventWaitHandle.Set(); // Envoyer un dernier signal pour débloquer la boucle d'attente
                    logger.LogInfo("ES_System_select signal reset completed");
                }

                // ... existing code ...
            }
            catch (Exception ex)
            {
                logger.LogError("Error restoring windows", ex);
            }
        }

        public void Run()
        {
            Application.Run();
        }

        public void Dispose()
        {
            logger.Log("Exiting application");
            trayIcon?.Dispose();
            _controllerService?.Dispose();
            wallpaperManager?.CloseWallpaper();
            esLoadingPlayer?.Dispose();
        }
    }

    public class LoggingConfig
    {
        public bool IsDetailedLoggingEnabled { get; set; } = false;
    }
}
