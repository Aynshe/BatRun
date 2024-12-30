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

        public ShellCommandExecutor(IniFile config, Logger logger)
        {
            this.config = config;
            this.logger = logger;
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
                int commandCount = config.ReadInt("Shell", "CommandCount", 0);
                int appCount = config.ReadInt("Shell", "AppCount", 0);
                logger.LogInfo($"Found {commandCount} commands and {appCount} applications to execute");

                // Créer une liste pour stocker tous les éléments
                var shellItems = new List<(bool IsCommand, string Path, bool Enabled, int Delay, int Order, bool AutoHide, bool DoubleLaunch, int DoubleLaunchDelay, bool OnlyOneInstance)>();

                // Charger les commandes
                for (int i = 0; i < commandCount; i++)
                    {
                        string batchPath = config.ReadValue("Shell", $"Command{i}Path", "");
                        bool enabled = config.ReadBool("Shell", $"Command{i}Enabled", false);
                        int delay = config.ReadInt("Shell", $"Command{i}Delay", 0);
                        int order = config.ReadInt("Shell", $"Command{i}Order", i);
                    bool autoHide = config.ReadBool("Shell", $"Command{i}AutoHide", false);
                    bool doubleLaunch = config.ReadBool("Shell", $"Command{i}DoubleLaunch", false);
                    int doubleLaunchDelay = config.ReadInt("Shell", $"Command{i}DoubleLaunchDelay", 0);

                    shellItems.Add((true, batchPath, enabled, delay, order, autoHide, doubleLaunch, doubleLaunchDelay, false));
                }

                // Charger les applications
                for (int i = 0; i < appCount; i++)
                {
                    string path = config.ReadValue("Shell", $"App{i}Path", "");
                    bool enabled = config.ReadBool("Shell", $"App{i}Enabled", false);
                    int delay = config.ReadInt("Shell", $"App{i}Delay", 0);
                    int order = config.ReadInt("Shell", $"App{i}Order", i);
                    bool autoHide = config.ReadBool("Shell", $"App{i}AutoHide", false);
                    bool doubleLaunch = config.ReadBool("Shell", $"App{i}DoubleLaunch", false);
                    int doubleLaunchDelay = config.ReadInt("Shell", $"App{i}DoubleLaunchDelay", 0);
                    bool onlyOneInstance = config.ReadBool("Shell", $"App{i}OnlyOneInstance", false);

                    shellItems.Add((false, path, enabled, delay, order, autoHide, doubleLaunch, doubleLaunchDelay, onlyOneInstance));
                }

                // Trier par ordre global
                var sortedItems = shellItems
                    .Where(item => item.Enabled && !string.IsNullOrEmpty(item.Path))
                    .OrderBy(item => item.Order)
                    .ToList();

                // Exécuter dans l'ordre
                foreach (var item in sortedItems)
                {
                    if (item.Delay > 0)
                    {
                        logger.LogInfo($"Waiting {item.Delay} seconds before executing");
                        await Task.Delay(item.Delay * 1000);
                    }

                    try
                    {
                        Process? process = null;
                        if (item.IsCommand)
                            {
                                var startInfo = new ProcessStartInfo
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
                                // Lire le contenu du fichier batch en Windows-1252
                                string batchContent = File.ReadAllText(item.Path, Encoding.GetEncoding(1252));
                                
                                // Ajouter les commandes nécessaires au début du script
                                string modifiedBatchContent = "@echo off\r\n" +
                                                              "chcp 1252 > nul\r\n" + // Définir la page de code Windows-1252
                                                              "setlocal enabledelayedexpansion\r\n" + 
                                                              batchContent;

                                // Créer un fichier batch temporaire avec l'encodage Windows-1252
                                string tempBatchFile = Path.Combine(Path.GetTempPath(), $"batrun_temp_{Guid.NewGuid()}.bat");
                                File.WriteAllText(tempBatchFile, modifiedBatchContent, Encoding.GetEncoding(1252));

                                // Mettre à jour le chemin du fichier à exécuter
                                startInfo.Arguments = $"/c \"{tempBatchFile}\"";

                                process = Process.Start(startInfo);
                                if (process != null)
                                {
                                    string output = await process.StandardOutput.ReadToEndAsync();
                                    string error = await process.StandardError.ReadToEndAsync();

                                    await process.WaitForExitAsync();
                                    int exitCode = process.ExitCode;

                                    // Supprimer le fichier batch temporaire
                                    try 
                                    {
                                        File.Delete(tempBatchFile);
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.LogError($"Error deleting temp batch file: {ex.Message}");
                                    }

                                    if (!string.IsNullOrEmpty(output))
                                        logger.LogInfo($"Command output: {output}");
                                    if (!string.IsNullOrEmpty(error))
                                        logger.LogError($"Command error: {error}");

                                    if (exitCode != 0)
                                    {
                                        logger.LogError($"Command exited with non-zero exit code: {exitCode}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogError($"Error preparing or executing command {item.Path}: {ex.Message}", ex);
                            }
                        }
                        else
                        {
                            // Vérifier si l'application est déjà en cours d'exécution et si OnlyOneInstance est activé
                            if (item.OnlyOneInstance && IsApplicationAlreadyRunning(item.Path))
                            {
                                logger.LogInfo($"Application {item.Path} is already running and OnlyOneInstance is enabled. Skipping launch.");
                                continue;
                            }

                            var startInfo = new ProcessStartInfo
                            {
                                FileName = item.Path,
                                UseShellExecute = true,
                                WorkingDirectory = Path.GetDirectoryName(item.Path) ?? AppContext.BaseDirectory
                            };

                            logger.LogInfo($"Launching application: {item.Path}");
                            process = Process.Start(startInfo);

                            // Gérer le masquage automatique
                            if (item.AutoHide && process != null)
                            {
                                await HandleAutoHide(process, item.Path);
                            }

                            // Si DoubleLaunch est activé, lancer une deuxième instance
                            if (item.DoubleLaunch)
                            {
                                logger.LogInfo($"Waiting {item.DoubleLaunchDelay} seconds before second launch of: {item.Path}");
                                await Task.Delay(item.DoubleLaunchDelay * 1000);

                                logger.LogInfo($"Launching second instance of: {item.Path}");
                                process = Process.Start(startInfo);

                                // Gérer le masquage automatique pour la deuxième instance
                                if (item.AutoHide && process != null)
                                {
                                    await HandleAutoHide(process, item.Path);
                                }
                            }

                            // Vérifier les fenêtres persistantes après le lancement
                            applicationManager.CheckForPersistentWindows();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error executing {(item.IsCommand ? "command" : "application")} {item.Path}: {ex.Message}", ex);
                    }
                }

                // Vérifier si RetroBAT doit être lancé à la fin
                bool launchRetroBatAtEnd = config.ReadBool("Shell", "LaunchRetroBatAtEnd", false);
                if (launchRetroBatAtEnd)
                {
                    int retroBatDelay = config.ReadInt("Shell", "RetroBatDelay", 0);
                    
                    // Attendre le délai spécifié
                    if (retroBatDelay > 0)
                    {
                        logger.LogInfo($"Waiting {retroBatDelay} seconds before launching RetroBAT");
                        await Task.Delay(retroBatDelay * 1000);
                    }

                    // Lancer RetroBAT.exe en utilisant le chemin déjà récupéré
                    try
                    {
                        string retroBatExe = Program.GetRetrobatPath();
                        if (!string.IsNullOrEmpty(retroBatExe))
                        {
                            if (File.Exists(retroBatExe))
                            {
                                // Créer et afficher le splash sur le thread UI
                                HotkeySplashForm? splash = null;
                                await Task.Run(() =>
                                {
                                    Application.OpenForms[0]?.Invoke((MethodInvoker)delegate
                                    {
                                        splash = new HotkeySplashForm();
                                        splash.Show();
                                        Application.DoEvents(); // Forcer le rendu
                                    });
                                });

                                // Attendre avec le splash visible
                                await Task.Delay(3000);

                                // Vérifier si la minimisation des fenêtres est activée
                                bool minimizeWindows = config.ReadBool("Windows", "MinimizeWindows", true);
                                if (minimizeWindows)
                                {
                                    // Minimiser les fenêtres actives
                                    var processes = Process.GetProcesses();
                                    foreach (var proc in processes)
                                    {
                                        try
                                        {
                                            if (proc.MainWindowHandle != IntPtr.Zero)
                                            {
                                                NativeMethods.ShowWindow(proc.MainWindowHandle, 6); // SW_MINIMIZE = 6
                                            }
                                        }
                                        catch { }
                                    }
                                }

                                logger.LogInfo("Launching RetroBAT");
                                var startInfo = new ProcessStartInfo
                                {
                                    FileName = retroBatExe,
                                    UseShellExecute = false,
                                    WorkingDirectory = Path.GetDirectoryName(retroBatExe) ?? string.Empty
                                };
                                Process.Start(startInfo);

                                // Fermer le splash de manière sûre
                                if (splash != null)
                                {
                                    Application.OpenForms[0]?.Invoke((MethodInvoker)delegate
                                    {
                                        splash.Close();
                                        splash.Dispose();
                                    });
                                }

                                // Attendre que EmulationStation démarre
                                int maxAttempts = 10; // 10 tentatives
                                int attemptDelay = 2000; // 2 secondes entre chaque tentative
                                int currentAttempt = 0;
                                Process? esProcess = null;

                                while (currentAttempt < maxAttempts && esProcess == null)
                                {
                                    await Task.Delay(attemptDelay);
                                    var processes = Process.GetProcessesByName("emulationstation");
                                    if (processes.Length > 0 && !processes[0].HasExited)
                                    {
                                        esProcess = processes[0];
                                        break;
                                    }
                                    currentAttempt++;
                                }

                                if (esProcess != null)
                                {
                                    // Attendre que la fenêtre soit créée
                                    while (esProcess.MainWindowHandle == IntPtr.Zero && !esProcess.HasExited)
                                    {
                                        await Task.Delay(500);
                                        esProcess.Refresh();
                                    }

                                    if (!esProcess.HasExited)
                                    {
                                        // Vérifier les paramètres d'intro
                                        string retrobatIniPath = Path.Combine(Path.GetDirectoryName(retroBatExe) ?? string.Empty, "retrobat.ini");
                                        bool hasIntro = false;
                                        int introDuration = 0;

                                        if (File.Exists(retrobatIniPath))
                                        {
                                            var lines = File.ReadAllLines(retrobatIniPath);
                                            string? enableIntro = lines.FirstOrDefault(line => line.StartsWith("EnableIntro="))?.Split('=')[1];
                                            string? videoDuration = lines.FirstOrDefault(line => line.StartsWith("VideoDuration="))?.Split('=')[1];

                                            if (enableIntro == "1" && videoDuration != null && int.TryParse(videoDuration, out introDuration))
                                            {
                                                hasIntro = true;
                                                logger.LogInfo($"Waiting for intro video duration: {introDuration} ms");
                                                await Task.Delay(introDuration);
                                            }
                                        }

                                        // Mettre le focus sur EmulationStation seulement si pas de vidéo d'intro
                                        // ou après la fin de la vidéo d'intro
                                        if (!hasIntro || introDuration > 0)
                                        {
                                            if (esProcess.MainWindowHandle != IntPtr.Zero)
                                            {
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
                                            }
                                        }

                                        // Attendre la fermeture d'EmulationStation
                                        await Task.Run(() =>
                                        {
                                            esProcess.WaitForExit();
                                        });

                                        // Restaurer les fenêtres si elles ont été minimisées
                                        if (minimizeWindows)
                                        {
                                            applicationManager.ShowAllWindows();
                                        }
                                    }
                                }
                                else
                                {
                                    logger.LogError("EmulationStation failed to start");
                                }
                            }
                            else
                            {
                                logger.LogError($"RetroBAT executable not found at: {retroBatExe}");
                            }
                        }
                        else
                        {
                            logger.LogError("RetroBAT installation path not found");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error launching RetroBAT: {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error in ExecuteShellCommandsAsync", ex);
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