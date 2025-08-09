using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Linq;

namespace BatRun
{
    public class ShellCommandExecutor
    {
        private readonly IniFile config;
        private readonly Logger logger;
        private readonly string commandsDirectory;
        private readonly ApplicationManager applicationManager;
        private readonly IBatRunProgram? program;
        private readonly WallpaperManager? wallpaperManager;

        public ShellCommandExecutor(IniFile config, Logger logger, IBatRunProgram? program = null, WallpaperManager? wallpaperManager = null)
        {
            this.config = config;
            this.logger = logger;
            this.program = program;
            this.wallpaperManager = wallpaperManager;
            this.applicationManager = new ApplicationManager(config, logger);
            
            // Enregistrer le fournisseur d'encodage pour Windows-1252
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            
            // Utiliser le même répertoire que ShellConfigurationForm
            commandsDirectory = Path.Combine(AppContext.BaseDirectory, "ShellCommands");
            if (!Directory.Exists(commandsDirectory))
            {
                Directory.CreateDirectory(commandsDirectory);
            }
        }

        public async Task ExecuteShellCommandsAsync()
        {
            try
            {
                bool launchRandomGame = config.ReadBool("Shell", "LaunchRandomGame", false);
                int commandCount = config.ReadInt("Shell", "CommandCount", 0);
                int appCount = config.ReadInt("Shell", "AppCount", 0);
                logger.LogInfo($"Found {commandCount} commands and {appCount} applications to execute");

                var shellItems = new List<ShellCommand>();

                // Charger les commandes
                for (int i = 0; i < commandCount; i++)
                {
                    shellItems.Add(new ShellCommand {
                        Type = CommandType.Command,
                        Path = config.ReadValue("Shell", $"Command{i}Path", ""),
                        IsEnabled = config.ReadBool("Shell", $"Command{i}Enabled", false),
                        DelaySeconds = config.ReadInt("Shell", $"Command{i}Delay", 0),
                        Order = config.ReadInt("Shell", $"Command{i}Order", i),
                        AutoHide = config.ReadBool("Shell", $"Command{i}AutoHide", false),
                        DoubleLaunch = config.ReadBool("Shell", $"Command{i}DoubleLaunch", false),
                        DoubleLaunchDelay = config.ReadInt("Shell", $"Command{i}DoubleLaunchDelay", 0)
                    });
                }

                // Charger les applications
                for (int i = 0; i < appCount; i++)
                {
                    shellItems.Add(new ShellCommand {
                        Type = CommandType.Application,
                        Path = config.ReadValue("Shell", $"App{i}Path", ""),
                        IsEnabled = config.ReadBool("Shell", $"App{i}Enabled", false),
                        DelaySeconds = config.ReadInt("Shell", $"App{i}Delay", 0),
                        Order = config.ReadInt("Shell", $"App{i}Order", i),
                        AutoHide = config.ReadBool("Shell", $"App{i}AutoHide", false),
                        DoubleLaunch = config.ReadBool("Shell", $"App{i}DoubleLaunch", false),
                        DoubleLaunchDelay = config.ReadInt("Shell", $"App{i}DoubleLaunchDelay", 0),
                        OnlyOneInstance = config.ReadBool("Shell", $"App{i}OnlyOneInstance", false)
                    });
                }



                // Trier par ordre global
                var sortedItems = shellItems
                    .Where(item => item.IsEnabled && !string.IsNullOrEmpty(item.Path))
                    .OrderBy(item => item.Order)
                    .ToList();

                // Exécuter dans l'ordre
                foreach (var item in sortedItems)
                {
                    if (item.DelaySeconds > 0)
                    {
                        logger.LogInfo($"Waiting {item.DelaySeconds} seconds before executing");
                        await Task.Delay(item.DelaySeconds * 1000);
                    }

                    try
                    {
                        Process? process = null;
                        switch (item.Type)
                        {
                            case CommandType.Command:
                                var cmdStartInfo = new ProcessStartInfo
                                {
                                    FileName = "cmd.exe",
                                    Arguments = $"/c \"{item.Path}\"",
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    StandardOutputEncoding = Encoding.UTF8,
                                    StandardErrorEncoding = Encoding.UTF8,
                                    WorkingDirectory = Path.GetDirectoryName(item.Path) ?? Environment.GetFolderPath(Environment.SpecialFolder.System)
                                };

                                logger.LogInfo($"Executing command: {item.Path}");
                                
                                try
                                {
                                    string batchContent = File.ReadAllText(item.Path, Encoding.GetEncoding(1252));
                                    string modifiedBatchContent = "@echo off\r\n" +
                                                                  "chcp 1252 > nul\r\n" +
                                                                  "setlocal enabledelayedexpansion\r\n" +
                                                                  batchContent;

                                    string tempBatchFile = Path.Combine(Path.GetTempPath(), $"batrun_temp_{Guid.NewGuid()}.bat");
                                    File.WriteAllText(tempBatchFile, modifiedBatchContent, Encoding.GetEncoding(1252));

                                    cmdStartInfo.Arguments = $"/c \"{tempBatchFile}\"";
                                    process = Process.Start(cmdStartInfo);
                                    if (process != null)
                                    {
                                        string output = await process.StandardOutput.ReadToEndAsync();
                                        string error = await process.StandardError.ReadToEndAsync();
                                        await process.WaitForExitAsync();
                                        try { File.Delete(tempBatchFile); } catch { }

                                        if (!string.IsNullOrEmpty(output)) logger.LogInfo($"Command output: {output}");
                                        if (!string.IsNullOrEmpty(error)) logger.LogError($"Command error: {error}");
                                        if (process.ExitCode != 0) logger.LogError($"Command exited with non-zero exit code: {process.ExitCode}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError($"Error preparing or executing command {item.Path}: {ex.Message}", ex);
                                }
                                break;

                            case CommandType.Application:
                                if (item.OnlyOneInstance && IsApplicationAlreadyRunning(item.Path))
                                {
                                    logger.LogInfo($"Application {item.Path} is already running and OnlyOneInstance is enabled. Skipping launch.");
                                    continue;
                                }

                                var appStartInfo = new ProcessStartInfo
                                {
                                    FileName = item.Path,
                                    UseShellExecute = true,
                                    WorkingDirectory = Path.GetDirectoryName(item.Path) ?? AppContext.BaseDirectory
                                };

                                logger.LogInfo($"Launching application: {item.Path}");
                                process = Process.Start(appStartInfo);

                                if (item.AutoHide && process != null) await HandleAutoHide(process, item.Path);

                                if (item.DoubleLaunch)
                                {
                                    logger.LogInfo($"Waiting {item.DoubleLaunchDelay} seconds before second launch of: {item.Path}");
                                    await Task.Delay(item.DoubleLaunchDelay * 1000);
                                    logger.LogInfo($"Launching second instance of: {item.Path}");
                                    process = Process.Start(appStartInfo);
                                    if (item.AutoHide && process != null) await HandleAutoHide(process, item.Path);
                                }
                                applicationManager.CheckForPersistentWindows();
                                break;

                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error executing item {item.Path}: {ex.Message}", ex);
                    }
                }

                // Vérifier si RetroBAT doit être lancé à la fin
                bool launchRetroBatAtEnd = config.ReadBool("Shell", "LaunchRetroBatAtEnd", false);
                if (launchRetroBatAtEnd)
                {
                    int retroBatDelay = config.ReadInt("Shell", "RetroBatDelay", 0);
                    
                    // Attendre le délai spécifié (ajout d'une seconde supplémentaire)
                    if (retroBatDelay >= 0)
                    {
                        int actualDelay = retroBatDelay + 2;
                        logger.LogInfo($"Waiting {actualDelay} seconds before launching RetroBAT");
                        await Task.Delay(actualDelay * 2000);
                    }

                    // Lancer RetroBAT en utilisant le programme principal si disponible
                    if (program != null)
                    {
                        // Determine if focus should be suppressed
                        bool willLaunchRandom = launchRandomGame && !shellItems.Any(item => item.IsEnabled);
                        string postLaunchPath = config.ReadValue("PostLaunch", "GamePath", "");
                        bool suppressFocus = willLaunchRandom || !string.IsNullOrEmpty(postLaunchPath);

                        // Start the Retrobat process but don't wait for it to complete yet.
                        Task retrobatTask = program.StartRetrobat(suppressFocus);

                        // Now, while the Retrobat process is running, handle the post-launch game.
                        await HandlePostRetroBatActionsAsync(sortedItems.Any());

                        // Finally, await the retrobat task to wait for the process to exit.
                        await retrobatTask;
                    }
                    else
                    {
                        // Fallback au lancement direct si le programme principal n'est pas disponible
                        if (program != null)
                        {
                            string retroBatExe = program.GetRetrobatPath();
                            if (!string.IsNullOrEmpty(retroBatExe) && File.Exists(retroBatExe))
                            {
                                logger.LogInfo("Launching RetroBAT");
                                var startInfo = new ProcessStartInfo
                                {
                                    FileName = retroBatExe,
                                    UseShellExecute = false,
                                    WorkingDirectory = Path.GetDirectoryName(retroBatExe) ?? string.Empty
                                };
                                Process.Start(startInfo);
                            }
                            else
                            {
                                logger.LogError($"RetroBAT executable not found at: {retroBatExe}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error in ExecuteShellCommandsAsync", ex);
            }
        }

        private async Task HandlePostRetroBatActionsAsync(bool preLaunchCommandsExist)
        {
            string gamePathToLaunch = config.ReadValue("PostLaunch", "GamePath", "");
            string displayName = config.ReadValue("PostLaunch", "DisplayName", gamePathToLaunch);
            bool isRandomLaunch = false;

            // If no specific game is set, check for random game condition
            if (string.IsNullOrEmpty(gamePathToLaunch))
            {
                bool randomOptionEnabled = config.ReadBool("Shell", "LaunchRandomGame", false);
                if (randomOptionEnabled && !preLaunchCommandsExist)
                {
                    isRandomLaunch = true;
                    displayName = "a random game";
                }
                else
                {
                    return; // No post-launch action needed
                }
            }

            logger.LogInfo($"A post-launch action is configured: launch {displayName}. Waiting for RetroBat API...");
            var scraper = new EmulationStationScraper();

            // Wait for API to be available
            const int maxAttempts = 12;
            const int delaySeconds = 5;
            bool apiReady = false;
            for (int i = 0; i < maxAttempts; i++)
            {
                logger.LogInfo($"Pinging EmulationStation API, attempt {i + 1}/{maxAttempts}...");
                if (await scraper.PingServerAsync())
                {
                    apiReady = true;
                    break;
                }
                if (i < maxAttempts - 1)
                {
                    await Task.Delay(delaySeconds * 1000);
                }
            }

            if (!apiReady)
            {
                logger.LogError($"Timed out waiting for EmulationStation API. Could not launch {displayName}.");
                return;
            }

            logger.LogInfo("API is available. Preparing to launch game.");

            // If it's a random launch, we need to pick a game now
            if (isRandomLaunch)
            {
                var allGames = await scraper.GetAllGamesAsync();
                if (allGames.Count > 0)
                {
                    var random = new Random();
                    var gameToLaunch = allGames[random.Next(allGames.Count)];
                    gamePathToLaunch = gameToLaunch.Path ?? "";
                    displayName = gameToLaunch.Name ?? "Unknown Game";
                    logger.LogInfo($"Selected random game: {displayName}");
                }
                else
                {
                    logger.LogWarning("Random launch was configured, but no games were found via the scraper.");
                    return;
                }
            }

            // Launch the game (either specific or the chosen random one)
            if (!string.IsNullOrEmpty(gamePathToLaunch))
            {
                bool success = await scraper.LaunchGameAsync(gamePathToLaunch);
                if (success)
                {
                    logger.LogInfo($"Successfully launched {displayName}.");
                }
                else
                {
                    logger.LogError($"Failed to launch {displayName} via API.");
                }
            }
        }

        private async Task HandleAutoHide(Process process, string path)
        {
            // Attendre que la fenêtre soit créée
            int attempts = 0;
            const int maxAttempts = 10;
            bool windowFound = false;

            while (attempts < maxAttempts && !windowFound && !process.HasExited)
            {
                await Task.Delay(500); // Attendre 500ms entre chaque tentative
                try
                {
                    process.Refresh();
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        applicationManager.HideWindow(process.MainWindowHandle);
                        logger.LogInfo($"Auto-hidden window for: {path}");
                        windowFound = true;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error checking window handle for {path}: {ex.Message}", ex);
                }
                attempts++;
            }

            if (!windowFound && !process.HasExited)
            {
                logger.LogError($"Could not find window to hide for: {path} after {maxAttempts} attempts");
            }
        }

        private bool IsApplicationAlreadyRunning(string applicationPath)
        {
            try
            {
                string executableName = Path.GetFileNameWithoutExtension(applicationPath);
                var runningProcesses = Process.GetProcessesByName(executableName);
                return runningProcesses.Length > 0;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error checking if application is running: {ex.Message}", ex);
                return false;
            }
        }
    }
} 