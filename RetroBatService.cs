using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BatRun;
using Microsoft.Win32;

namespace BatRun
{
    public class RetroBatService
    {
        private readonly Logger logger;
        private readonly IniFile config;
        private string retrobatPath = "";
        private readonly Action minimizeWindows;
        private readonly Func<Task> restoreWindows;

        public RetroBatService(Logger logger, IniFile config, Action minimizeWindows, Func<Task> restoreWindows)
        {
            this.logger = logger;
            this.config = config;
            this.minimizeWindows = minimizeWindows;
            this.restoreWindows = restoreWindows;
        }

        public string GetRetrobatPath() => retrobatPath;

        public void Initialize()
        {
            logger.Log("Initializing RetroBat Service...");
            FindRetrobatPath();
        }

        private void FindRetrobatPath()
        {
            logger.Log("Searching for RetroBat path...");
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\RetroBat");
                if (key != null)
                {
                    var path = key.GetValue("LatestKnownInstallPath") as string;
                    if (!string.IsNullOrEmpty(path))
                    {
                        retrobatPath = Path.Combine(path, "retrobat.exe");
                        logger.Log($"RetroBat path found in registry: {retrobatPath}");
                        return;
                    }
                }
            }
            catch (Exception ex) { logger.LogError("Error reading registry", ex); }

            retrobatPath = @"C:\Retrobat\retrobat.exe"; // Default path
            logger.Log($"Using default path: {retrobatPath}");
        }

        public async Task LaunchRetrobat()
        {
            if (IsRetrobatRunning() || IsEmulationStationRunning())
            {
                logger.LogInfo("RetroBat or EmulationStation is already running.");
                return;
            }

            if (!File.Exists(retrobatPath))
            {
                logger.LogError($"RetroBat executable not found at: {retrobatPath}");
                MessageBox.Show($"Unable to find RetroBat at {retrobatPath}", "BatRun", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // The UI logic (splash screen) has been moved to Batrun.cs
                // This service is now only responsible for the process itself.
                minimizeWindows();

                Process.Start(new ProcessStartInfo { FileName = retrobatPath, UseShellExecute = false });

                // Launch the auto-launch handler in the background
                _ = HandleAutoLaunchAsync();

                Process? esProcess = await WaitForProcess("emulationstation", 10, 2000);

                if (esProcess != null)
                {
                    esProcess.EnableRaisingEvents = true;
                    esProcess.Exited += async (s, e) => {
                        logger.LogInfo("EmulationStation process exited.");
                        await restoreWindows();
                    };
                    await SetEmulationStationFocus();
                }
                else
                {
                    logger.LogError("EmulationStation failed to start.");
                    await restoreWindows(); // Restore windows if ES fails to start
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error launching Retrobat", ex);
            }
        }

        private async Task<Process?> WaitForProcess(string processName, int maxAttempts, int delay)
        {
            for(int i=0; i < maxAttempts; i++)
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length > 0)
                {
                    return processes[0];
                }
                await Task.Delay(delay);
            }
            return null;
        }

        private async Task SetEmulationStationFocus()
        {
            int focusDuration = config.ReadInt("Focus", "FocusDuration", 30000);
            int focusInterval = config.ReadInt("Focus", "FocusInterval", 5000);

            logger.LogInfo($"Starting focus sequence for EmulationStation (Duration: {focusDuration}ms, Interval: {focusInterval}ms)");

            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < focusDuration)
            {
                var process = Process.GetProcessesByName("emulationstation").FirstOrDefault();
                if (process?.MainWindowHandle != IntPtr.Zero)
                {
                    // This is a simplified focus attempt. The complex thread attachment logic
                    // might be better suited inside a dedicated NativeMethods helper class if needed.
                    NativeMethods.SetForegroundWindow(process.MainWindowHandle);
                    logger.LogInfo("Focus applied to EmulationStation.");
                }
                await Task.Delay(focusInterval);
            }
            logger.LogInfo("End of focus sequence for EmulationStation.");
        }

        public bool IsRetrobatRunning()
        {
            return Process.GetProcessesByName("retrobat").Length > 0;
        }

        public bool IsEmulationStationRunning()
        {
            return Process.GetProcessesByName("emulationstation").Length > 0;
        }

        private async Task HandleAutoLaunchAsync()
        {
            bool isRandom = config.ReadBool("Shell", "AutoLaunchRandom", false);
            string gamePath = config.ReadValue("Shell", "AutoLaunchGamePath", "");
            string gameName = config.ReadValue("Shell", "AutoLaunchGameName", "");

            // Only proceed if a game is configured for auto-launch
            if (!isRandom && string.IsNullOrEmpty(gamePath))
            {
                logger.LogInfo("No game configured for auto-launch.");
                return;
            }

            logger.LogInfo("Auto-launch sequence started. Waiting for EmulationStation API...");

            var esApi = new EmulationStationApi(logger);
            bool apiAvailable = false;
            for (int i = 0; i < 30; i++) // Wait up to 30 seconds
            {
                if (await esApi.IsApiAvailableAsync())
                {
                    apiAvailable = true;
                    logger.LogInfo("EmulationStation API is available.");
                    break;
                }
                await Task.Delay(1000);
            }

            if (!apiAvailable)
            {
                logger.LogError("EmulationStation API did not become available in time. Aborting auto-launch.");
                return;
            }

            Game? gameToLaunch = null;

            if (isRandom)
            {
                logger.LogInfo("Selecting a random game to launch.");
                var allGames = await esApi.GetAllGamesAsync();
                if (allGames.Count > 0)
                {
                    var random = new Random();
                    gameToLaunch = allGames[random.Next(allGames.Count)];
                }
                else
                {
                    logger.LogError("Could not find any games to select for random launch.");
                    return;
                }
            }
            else
            {
                gameToLaunch = new Game { Path = gamePath, Name = gameName, System = "Unknown" };
            }

            if (gameToLaunch != null)
            {
                await Task.Delay(2000); // Small delay to ensure ES is ready to receive commands
                await esApi.LaunchGameAsync(gameToLaunch);
            }
        }
    }
}
