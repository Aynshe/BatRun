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

        private readonly string retrobatPath;

        public ShellCommandExecutor(IniFile config, Logger logger, string retrobatPath, IBatRunProgram? program = null, WallpaperManager? wallpaperManager = null)
        {
            this.config = config;
            this.logger = logger;
            this.program = program;
            this.wallpaperManager = wallpaperManager;
            this.retrobatPath = retrobatPath;
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
                        await program.StartRetrobat();
                    }
                    else
                    {
                        // Fallback au lancement direct si le programme principal n'est pas disponible
                        if (!string.IsNullOrEmpty(this.retrobatPath) && File.Exists(this.retrobatPath))
                        {
                            logger.LogInfo("Launching RetroBAT");
                            var startInfo = new ProcessStartInfo
                            {
                                FileName = this.retrobatPath,
                                UseShellExecute = false,
                                WorkingDirectory = Path.GetDirectoryName(this.retrobatPath) ?? string.Empty
                            };
                            Process.Start(startInfo);
                        }
                        else
                        {
                            logger.LogError($"RetroBAT executable not found at: {this.retrobatPath}");
                        }
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