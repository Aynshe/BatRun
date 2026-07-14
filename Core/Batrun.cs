
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SDL2;
using Microsoft.Win32;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX.XInput;
using SharpDX.DirectInput;

using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.Core
{
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
        private ArcadeManager? _arcadeManager;

        // Ajout de DInputHandler avec initialisation
        private DInputHandler dInputHandler = new DInputHandler(new Logger("BatRun.log"));

        private int _isLaunching = 0;
        private List<IntPtr> activeWindows = new List<IntPtr>();

        private bool _emulationStationPollingLogged = false;

        private Icon? _appIcon;
        private WallpaperManager? wallpaperManager;
        public ArcadeManager? ArcadeManager => _arcadeManager;

        public const string APP_VERSION = "3.1.0";

        private bool hideESLoading;
        private bool arcadeMode;
        private static int esSystemSelectCount = 0;
        private static DateTime lastESSystemSelectTime = DateTime.MinValue;
        private static bool isESStarting = false;
        private static readonly object esLockObject = new();

        private bool skipFocusSequence = false;

        private ESLoadingPlayer? esLoadingPlayer;

        private DateTime lastStartButtonTime = DateTime.MinValue;
        private const int START_BUTTON_COOLDOWN_MS = 1000; // 1 seconde de cooldown
        private readonly SynchronizationContext _syncContext;
        private bool _isRestarted;

        public Batrun(bool isRestarted = false, string? sessionToken = null)
        {
            _isRestarted = isRestarted;
            _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
            // Initialisation minimale
            config = new IniFile(Path.Combine(AppContext.BaseDirectory, "config.ini"));
            logger = new Logger("BatRun.log");
            hideESLoading = config.ReadBool("Windows", "HideESLoading", false);
            arcadeMode = config.ReadBool("Arcade", "Enabled", false);

            // EN: Write lock file on Windows shutdown/logoff so Guardian doesn't restart BatRun during shutdown
            // FR: Écrire le lock file lors d'un arrêt/déconnexion Windows pour que le Guardian ne redémarre pas BatRun
            Microsoft.Win32.SystemEvents.SessionEnding += (s, e) =>
            {
                try
                {
                    File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "BatRun.exit.lock"), "Windows Session Ending");
                    logger.LogInfo($"SessionEnding event fired (reason: {e.Reason}). Lock file written.");
                }
                catch { }
            };

            // Initialize services
            _retroBatService = new RetroBatService(logger, config);
            var retrobatPath = _retroBatService.GetRetrobatPath();
            logger.Log("Starting BatRun");

            _controllerService = new ControllerService(logger, config, retrobatPath);
            _controllerService.HotkeyCombinationPressed += OnHotkeyCombinationPressed;
            _controllerService.EmergencyStopRequested += OnEmergencyStopRequested;
            _controllerService.Initialize();

            // Démarrer l'écoute des signaux ES_System_select
            if (hideESLoading || arcadeMode)
            {
                StartESSystemSelectListener();
            }

            // Exécuter les commandes shell configurées (L'exécuteur gère les doublons en cas de restart)
            // EN: Execute shell commands (executor handles duplicates if restarted)
            Task.Run(async () =>
            {
                try
                {
                    var shellExecutor = new ShellCommandExecutor(config, logger, this, wallpaperManager);
                    await shellExecutor.ExecuteShellCommandsAsync(_isRestarted);
                }
                catch (Exception ex)
                {
                    logger.LogError("Error executing shell commands", ex);
                }
            });

            // EN: Launch plugins configured to start with BatRun
            // FR: Lancer les plugins configurés pour démarrer avec BatRun
            Task.Run(() =>
            {
                try
                {
                    PluginManager.LaunchBatRunPlugins(logger, config, _retroBatService);
                }
                catch (Exception ex)
                {
                    logger.LogError("Error starting BatRun plugins", ex);
                }
            });

            // Initialiser le WallpaperManager après l'initialisation du chemin RetroBat
            wallpaperManager = new WallpaperManager(config, logger, this);

            // Vérifier si explorer.exe est en cours d'exécution
            CheckExplorerAndInitialize();

            // Initialisation de DInputHandler
            dInputHandler = new DInputHandler(logger);

            // Create ArcadeManager
            _arcadeManager = new ArcadeManager(logger, config, _controllerService);
            _arcadeManager.InterfaceRequested += (s, e) => SafeExecute(ShowMainForm);
            
            // EN: Restore session if a valid token is provided (from Guardian restart)
            // FR: Restaurer la session si un token valide est fourni (depuis un restart Guardian)
            if (!string.IsNullOrEmpty(sessionToken))
            {
                if (_arcadeManager != null && _arcadeManager.RestoreSessionState(sessionToken))
                {
                    logger.LogInfo("Session successfully restored. Skipping initial overlay Show.");
                }
            }
            
            // Le Hook Clavier Global nécessite une boucle de message active.
            // Comme l'application tourne en arrière-plan, MainForm.Load ne s'exécute jamais tout seul !
            // On utilise donc un Timer UI qui va s'exécuter de façon asynchrone dans la boucle principale.
            var initTimer = new System.Windows.Forms.Timer();
            initTimer.Interval = 1000;
            initTimer.Tick += (s, e) =>
            {
                initTimer.Stop();
                initTimer.Dispose();
                _arcadeManager?.ActivateArcadeMode();
            };
            initTimer.Start();

            // Create a timer to check EmulationStation status
            var checkTimer = new System.Windows.Forms.Timer();
            checkTimer.Interval = 500; // EN: Check every 500ms for better arcade reactivity / FR: Vérification toutes les 500ms
            bool wasRunning = false;
            bool wasMediaPaused = false;
            checkTimer.Tick += (sender, e) =>
            {
                bool isRunning = _retroBatService.IsEmulationStationRunning();
                bool shouldPauseMedia = isRunning || (esLoadingPlayer != null && hideESLoading);

                if (isRunning && !wasRunning)
                {
                    // En mode Arcade locké, masquer le wallpaper pour qu'ES soit visible
                    if (_arcadeManager?.IsLocked == true)
                    {
                        wallpaperManager?.HideWallpaperWindow();
                    }
                    _arcadeManager?.NotifyEmulationStationState(true);
                }
                else if (!isRunning && wasRunning)
                {
                    _arcadeManager?.NotifyEmulationStationState(false);
                    // EN: Only restore wallpaper if the arcade overlay is NOT showing an ES CLOSED alert
                    // FR: Ne restaurer le wallpaper que si l'overlay arcade ne montre PAS une alerte ES CLOSED
                    if (_arcadeManager?.IsLocked == true && !_arcadeManager.IsEsClosedAlertActive)
                    {
                        wallpaperManager?.ShowWallpaperWindow();
                    }
                }

                if (wasMediaPaused && !shouldPauseMedia)
                {
                    logger.LogInfo("EmulationStation has stopped and no loading video, resuming polling and media");
                    wallpaperManager?.ResumeMedia();
                    _controllerService.StartPolling();
                }
                else if (!wasMediaPaused && shouldPauseMedia)
                {
                    logger.LogInfo("EmulationStation has started or loading video is playing, pausing media");
                    if (wallpaperManager != null)
                    {
                        wallpaperManager.EnablePauseAndBlackBackground();
                        wallpaperManager.PauseMedia();
                    }
                }
                
                wasRunning = isRunning;
                wasMediaPaused = shouldPauseMedia; // EN: Separate tracking for media pause / FR: Suivi séparé pour la pause média
            };
            checkTimer.Start();
        }

        private async void OnHotkeyCombinationPressed(object? sender, EventArgs e)
        {
            await LaunchRetrobat();
        }

        // EN: Emergency stop handler — Select+Start held 5 seconds
        // FR: Gestionnaire d'arrêt d'urgence — Select+Start maintenu 5 secondes
        private void OnEmergencyStopRequested(object? sender, EventArgs e)
        {
            logger.LogInfo("[EmergencyStop] Select+Start 5s hold detected! Forcing session and game stop...");
            _arcadeManager?.ForceStopSessionAndGame();
        }

        private async Task LaunchRetrobat()
        {
            // EN: Atomic check-and-set so two hotkey events back-to-back cannot both
            // pass the "already launching" guard and each spawn a splash + RetroBat launch.
            // FR: Vérification+positionnement atomiques pour empêcher deux events hotkey
            // rapprochés de passer tous deux le verrou et de créer chacun un splash + lancement.
            if (Interlocked.CompareExchange(ref _isLaunching, 1, 0) != 0)
            {
                logger.LogInfo("Launch already in progress.");
                return;
            }

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
                        if (_arcadeManager != null) _arcadeManager.IsLoadingVideoActive = true;
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

                    // EN: Honor the "MinimizeWindows" INI option. The splash title contains
                    // "BatRun" so the splash itself is NEVER minimized by this call.
                    // FR: Respecte l'option INI "MinimizeWindows". Le titre du splash contient
                    // "BatRun" donc le splash lui-même n'est JAMAIS minimisé par cet appel.
                    MinimizeActiveWindows();

                    if (showHotkeySplash)
                    {
                        // EN: Fire-and-forget the splash on a dedicated STA thread with its own
                        // message pump. The controller (background) thread does NOT wait for the
                        // splash: this returns immediately.
                        // FR: Fire-and-forget du splash sur un thread STA dédié avec sa propre
                        // pompe de messages. Le thread manette (background) n'attend PAS le splash :
                        // cette fonction revient immédiatement.
                        HotkeySplashHost.ShowSplashFor(TimeSpan.FromSeconds(2));
                    }

                    // EN: Always give the splash a fixed 2-second visibility window before
                    // launching RetroBat, regardless of whether the splash was shown.
                    // FR: Laisse toujours une fenêtre de visibilité fixe de 2 secondes au splash
                    // avant de lancer RetroBat, que le splash ait été affiché ou non.
                    await Task.Delay(2000);

                    await _retroBatService.Launch();

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
                        if (_arcadeManager != null) _arcadeManager.IsLoadingVideoActive = false;
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
                    if (_arcadeManager != null) _arcadeManager.IsLoadingVideoActive = false;
            }
            finally
            {
                Interlocked.Exchange(ref _isLaunching, 0);
            }
        }

        private void MinimizeActiveWindows()
        {
            // Read the minimize windows setting from the INI file
            bool minimizeWindows = config.ReadBool("Windows", "MinimizeWindows", false);

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
                    if (_arcadeManager != null) _arcadeManager.IsLoadingVideoActive = false;
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
                                NativeMethods.GetForegroundWindow(), out uint _);
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

                if (File.Exists(iconPath))
                {
                    _appIcon = new Icon(iconPath);
                }
                else
                {
                    logger.LogError($"Icon file not found at: {iconPath}");
                    _appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                }

                trayIcon = new NotifyIcon
                {
                    Icon = _appIcon,
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
                contextMenu.Opening += (s, e) =>
                {
                    if (_arcadeManager != null && _arcadeManager.IsLocked)
                    {
                        e.Cancel = true;
                    }
                };

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
                contextMenu.Items.Add(strings.Exit, null, (s, e) => {
                    if (_arcadeManager != null && _arcadeManager.IsLocked)
                    {
                        _arcadeManager.TriggerLocalOperatorPrompt();
                    }
                    else
                    {
                        SafeExecute(Application.Exit);
                    }
                });
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

        // EN: Marshal a Func onto the UI thread (used from background/hotkey threads to create Forms).
        // relays on the WindowsFormsSynchronizationContext captured at startup (when
        // Application.EnableVisualStyles installed it on the main STA thread). _syncContext.Send
        // runs the callback on the UI pump thread and blocks the caller until it returns. We do
        // NOT spin up a hidden "host" Form: such a Form leaks a taskbar entry / foreground glow on
        // some systems and can weirdly own subsequently-created Forms, hiding them off-screen.
        // FR: Marshale une Func sur le thread UI (depuis les threads background/hotkey pour créer des Forms).
        // S'appuie sur le WindowsFormsSynchronizationContext capturé au démarrage (installé par
        // Application.EnableVisualStyles sur le thread STA principal). _syncContext.Send exécute le
        // callback sur le thread de la pompe UI et bloque l'appelant jusqu'à son retour. On NE crée
        // PAS de Form hôte caché : un tel Form peut fuiter une entrée de barre des tâches / halo de
        // premier plan sur certains systèmes et peut devenir propriétaire de Forms créés ensuite,
        // les masquant hors écran.
        private T InvokeOnUIThread<T>(Func<T> func)
        {
            // EN: Best path: invoke through an existing visible form (main form or any non-overlay).
            // We prefer MainForm because its handle is created lazily by ShowMainForm() and is
            // stable; if the application has no visible form yet, fall through to SynchronizationContext.
            // FR: Meilleur chemin : invoquer via un Form visible existant (main form ou tout non-overlay).
            // On préfère MainForm car son handle est créé paresseusement par ShowMainForm() et reste
            // stable ; si l'application n'a encore aucun Form visible, on repli sur SynchronizationContext.
            Form? syncForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            if (syncForm == null && Application.OpenForms.Count > 0)
            {
                syncForm = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.GetType().Name != "OperatorMiniOverlayForm");
            }

            if (syncForm != null && syncForm.IsHandleCreated)
            {
                if (syncForm.InvokeRequired)
                {
                    return (T)syncForm.Invoke(func);
                }
                return func();
            }

            // EN: Fallback: Synchronously marshal via the UI thread's SynchronizationContext.Send().
            // Send() blocks the calling thread until the UI pump dispatches the callback, which is
            // what we want (the caller has nothing to do until the splash handle is created).
            // Unlike a hidden host Form, Send() does not create anyHWND/taskbar entry and does not
            // accidentally own subsequently-created Forms.
            // FR: Repli : Marshal synchronement via SynchronizationContext.Send() du thread UI.
            // Send() bloque le thread appelant jusqu'à ce que la pompe UI distribue le callback,
            // ce qu'on veut (l'appelant n'a rien à faire tant que le handle du splash n'est pas créé).
            // Contrairement à un Form hôte caché, Send() ne crée aucun HWND/entrée de barre des tâches
            // et ne devient pas propriétaire des Forms créés ensuite.
            if (_syncContext != null && !ReferenceEquals(SynchronizationContext.Current, _syncContext))
            {
                T result = default!;
                Exception? caught = null;
                _syncContext.Send(_ =>
                {
                    try { result = func(); }
                    catch (Exception ex) { caught = ex; }
                }, null);
                if (caught != null) throw caught;
                return result;
            }

            return func();
        }

        private void InvokeOnUIThread(Action action)
        {
            InvokeOnUIThread<object?>(() => { action(); return null; });
        }

        public void SafeExecute(Action action)
        {
            try
            {
                if (_arcadeManager != null && _arcadeManager.IsLocked)
                {
                    logger.LogInfo("BatRun interface blocked by Arcade Mode lock.");
                    return;
                }

                // EN: Ensure we are on the UI thread / FR: S'assurer d'être sur le thread UI
                // EN: Use the main form or first stable form to avoid deadlock with mini-overlay
                Form? syncForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
                if (syncForm == null && Application.OpenForms.Count > 0)
                {
                    // Fallback to first form that is NOT the unstable mini-overlay
                    syncForm = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.GetType().Name != "OperatorMiniOverlayForm");
                }

                if (syncForm != null && syncForm.IsHandleCreated)
                {
                    if (syncForm.InvokeRequired)
                    {
                        syncForm.Invoke(action);
                        return;
                    }
                }
                
                action();
            }
            catch (ObjectDisposedException) { /* EN: Form closed during execution / FR: Formulaire fermé pendant l'exécution */ }
            catch (Exception ex)
            {
                logger.LogError($"Error in SafeExecute: {ex.Message}", ex);
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
            _controllerService.LoadDirectInputMappings();
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
                if (_retroBatService.IsEmulationStationRunning())
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
            // EN: Use robust logic from mini-menu (WallpaperManager) to find existing form
            // FR: Utiliser la logique robuste du mini-menu (WallpaperManager) pour trouver la fenêtre existante
            if (mainForm == null || mainForm.IsDisposed)
            {
                mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            }

            if (mainForm == null || mainForm.IsDisposed)
            {
                mainForm = new MainForm(this, logger, config);

                // S'assurer que l'icône est héritée du programme principal
                if (_appIcon != null)
                {
                    mainForm.Icon = (Icon)_appIcon.Clone();
                }
            }

            // EN: Use proper activation sequence / FR: Utiliser la séquence d'activation appropriée
            if (!mainForm.Visible)
            {
                mainForm.Show();
            }
            
            mainForm.WindowState = FormWindowState.Normal;
            mainForm.ShowInTaskbar = true;
            mainForm.BringToFront();
            mainForm.Activate();
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

        public string GetRetrobatPath()
        {
            return _retroBatService.GetRetrobatPath();
        }

        private void CheckExplorerAndInitialize()
        {
            // Initialize the tray icon immediately
            InitializeTrayIcon();

            // Use a timer to delay the wallpaper display
            var wallpaperTimer = new System.Windows.Forms.Timer();
            wallpaperTimer.Interval = 100; // 100ms delay
            wallpaperTimer.Tick += (s, e) =>
            {
                wallpaperTimer.Stop(); // Ensure it only runs once

                bool isExplorerRunning = Process.GetProcessesByName("explorer").Length > 0;
                bool enableWithExplorer = config?.ReadBool("Wallpaper", "EnableWithExplorer", false) ?? false;
                string selectedWallpaper = config?.ReadValue("Wallpaper", "Selected", "None") ?? "None";

                if (selectedWallpaper != "None")
                {
                    config?.WriteValue("Wallpaper", "IsActive", "true");
                    if (wallpaperManager != null)
                    {
                        if (!isExplorerRunning || enableWithExplorer)
                        {
                            wallpaperManager.ShowWallpaper();
                        }
                    }
                }
                else
                {
                    config?.WriteValue("Wallpaper", "IsActive", "false");
                }
            };
            wallpaperTimer.Start();
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

                var shellConfigForm = new ShellConfigurationForm(config, logger, configForm, this);
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
                        _syncContext.Post(_ =>
                        {
                            bool isShellMode = config.ReadBool("Shell", "EnableCustomUI", false);
                            if ((hideESLoading || isShellMode || arcadeMode) && isESStarting && esSystemSelectCount < 5)
                            {
                                HandleESSystemSelect();
                            }
                        }, null);
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

                        // Notifier ArcadeManager qu'ES est pleinement chargé pour ajuster l'overlay
                        _arcadeManager?.NotifyEsReady();

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

        private async void FocusEmulationStation()
        {
            // Use the robust looping focus logic to ensure ES comes to the foreground, especially after the loading video.
            int focusDuration = config.ReadInt("Focus", "FocusDuration", 10000); // Try for 10 seconds
            int focusInterval = config.ReadInt("Focus", "FocusInterval", 500);
            int elapsedTime = 0;

            logger.LogInfo($"Starting robust focus sequence for EmulationStation (Duration: {focusDuration}ms, Interval: {focusInterval}ms)");

            while (elapsedTime < focusDuration)
            {
                try
                {
                    var process = Process.GetProcessesByName("emulationstation").FirstOrDefault();
                    IntPtr hWnd = process?.MainWindowHandle ?? IntPtr.Zero;

                    if (hWnd != IntPtr.Zero)
                    {
                        if (process != null)
                        {
                            NativeMethods.AllowSetForegroundWindow(process.Id);
                            uint foregroundThread = NativeMethods.GetWindowThreadProcessId(NativeMethods.GetForegroundWindow(), out uint _);
                            uint appThread = NativeMethods.GetCurrentThreadId();
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
                                logger.LogInfo($"Focus applied to EmulationStation (attempt {elapsedTime / focusInterval + 1})");

                                // If the window is now in the foreground, we can stop trying.
                                if (NativeMethods.GetForegroundWindow() == hWnd)
                                {
                                    logger.LogInfo("EmulationStation is now in the foreground. Stopping focus loop.");
                                    return;
                                }
                            }
                            finally
                            {
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
                    logger.LogError("Error setting focus to EmulationStation", ex);
                }

                await Task.Delay(focusInterval);
                elapsedTime += focusInterval;
            }

            logger.LogWarning("End of robust focus sequence. EmulationStation may not be in the foreground.");
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


        public async Task StartRetrobat(bool suppressFocus = false)
        {
            if (Interlocked.CompareExchange(ref _isLaunching, 1, 0) != 0)
            {
                logger.LogInfo("Launch already in progress.");
                return;
            }

            try
            {
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
                    bool minimizeWindows = config.ReadBool("Windows", "MinimizeWindows", false);
                    if (minimizeWindows)
                    {
                        MinimizeActiveWindows();
                    }
                    else
                    {
                        logger.LogInfo("Window minimization disabled by configuration");
                    }
                }

                string retroBatExe = _retroBatService.GetRetrobatPath();
                if (string.IsNullOrEmpty(retroBatExe))
                {
                    logger.LogError("RetroBAT executable not found");
                    return;
                }

                logger.LogInfo("Starting RetroBAT");
                await _retroBatService.Launch();
                isESStarting = true;

                // Pause le média en cours si nécessaire
                if (wallpaperManager != null)
                {
                    logger.LogInfo("EmulationStation has started or loading video is playing, pausing media");
                    wallpaperManager.PauseMedia();
                }

                if (!hideESLoading && !suppressFocus)
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
            finally
            {
                Interlocked.Exchange(ref _isLaunching, 0);
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
            // EN: Signal a normal exit to the guardian / FR: Signaler une fermeture normale au gardien
            try
            {
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "BatRun.exit.lock"), "Normal Exit");
                
                // EN: Clean up session state on normal exit to prevent phantom restores
                // FR: Nettoyer l'état de session sur sortie normale pour éviter les restaurations fantômes
                string statePath = Path.Combine(AppContext.BaseDirectory, "Logs", "session_state.json");
                if (File.Exists(statePath)) File.Delete(statePath);
            }
            catch { /* Ignore */ }

            logger.Log("Exiting application");
            trayIcon?.Dispose();
            _controllerService?.Dispose();
            wallpaperManager?.CloseWallpaper();
            esLoadingPlayer?.Dispose();
        }
    }

}


