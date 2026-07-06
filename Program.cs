using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Linq;
using System.Globalization;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;
namespace BatRun
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            string[] args = Environment.GetCommandLineArgs();

            // --- Handle -waitforpid argument ---
            var waitArgIndex = Array.IndexOf(args, "-waitforpid");
            if (waitArgIndex != -1 && args.Length > waitArgIndex + 1)
            {
                if (int.TryParse(args[waitArgIndex + 1], out int pid))
                {
                    try
                    {
                        var oldProcess = Process.GetProcessById(pid);
                        oldProcess.WaitForExit(5000); // Wait up to 5 seconds
                    }
                    catch (Exception) { /* Ignore */ }
                }
            }

            // --- Handle -guardian <PID> argument (Obsolete but kept for compatibility) ---
            var guardianArgIndex = Array.IndexOf(args, "-guardian");
            if (guardianArgIndex != -1)
            {
                // Internal guardian is now moved to BatRunGuardian.exe
                return;
            }

            // --- Handle -game-start / -game-end / -screensaver-* arguments (IPC) ---
            if (args.Contains("-game-start") || args.Contains("-game-end") || 
                args.Contains("-screensaver-start") || args.Contains("-screensaver-stop"))
            {
                // EN: Proxy signal to the main BatRun instance via Named Pipe
                // FR: Transmettre le signal à l'instance principale de BatRun via Named Pipe
                int retryCount = 3;
                bool success = false;
                while (retryCount > 0 && !success)
                {
                    try
                    {
                        using (var client = new System.IO.Pipes.NamedPipeClientStream(".", "BatRun_IPC", System.IO.Pipes.PipeDirection.Out))
                        {
                            // EN: Increased timeout to 2s to handle high CPU load during game launch/exit
                            // FR: Timeout augmenté à 2s pour gérer la charge CPU lors du lancement/fermeture de jeu
                            client.Connect(2000); 
                            using (var writer = new StreamWriter(client))
                            {
                                string cmd = "";
                                if (args.Contains("-game-start")) cmd = "GAME_START";
                                else if (args.Contains("-game-end")) cmd = "GAME_END";
                                else if (args.Contains("-screensaver-start")) cmd = "SCREENSAVER_START";
                                else if (args.Contains("-screensaver-stop")) cmd = "SCREENSAVER_STOP";

                                if (cmd == "GAME_START")
                                {
                                    var startIdx = Array.IndexOf(args, "-game-start");
                                    var gameArgs = args.Skip(startIdx + 1).ToArray();
                                    writer.WriteLine($"{cmd}|{string.Join("|", gameArgs)}");
                                }
                                else
                                {
                                    writer.WriteLine(cmd);
                                }
                                writer.Flush();
                                success = true;
                            }
                        }
                    }
                    catch 
                    { 
                        retryCount--;
                        if (retryCount > 0) Thread.Sleep(500);
                    }
                }
                return;
            }

            // --- Handle -ES_System_select argument ---
            if (args.Contains("-ES_System_select"))
            {
                using (var eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "BatRun_ES_System_Select"))
                {
                    eventWaitHandle.Set();
                    return;
                }
            }

            Logger? logger = null;
            try
            {
                AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
                Application.ThreadException += ApplicationOnThreadException;
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

                logger = new Logger("BatRun.log", appendToExisting: false);
                logger.ClearLogFile();
                logger.LogInfo("=== BatRun Starting - Version " + Batrun.APP_VERSION + " ===");

                CheckNetVersion(logger);

                bool createdNew;
                using (Mutex mutex = new Mutex(true, "BatRun", out createdNew))
                {
                    if (!createdNew)
                    {
                        logger.LogInfo("Another instance is already running. Exiting silently.");
                        return;
                    }

                    // FR: Nettoyer le fichier verrou d'une session précédente s'il existe
                    // EN: Clean up lock file from previous session if it exists
                    try
                    {
                        string lockFile = Path.Combine(AppContext.BaseDirectory, "BatRun.exit.lock");
                        if (File.Exists(lockFile)) File.Delete(lockFile);
                    }
                    catch { /* Ignore */ }

                    Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    VlcManager.Initialize(logger);

                    var config = new IniFile(Path.Combine(AppContext.BaseDirectory, "config.ini"));
                    bool showSplashScreen = config.ReadBool("Windows", "ShowSplashScreen", true);

                    if (showSplashScreen)
                    {
                        ShowSplashScreen();
                    }

                    bool isRestarted = args.Contains("-restarted");
                    string? sessionToken = null;
                    int tokenIdx = Array.IndexOf(args, "-sessiontoken");
                    if (tokenIdx != -1 && args.Length > tokenIdx + 1)
                    {
                        sessionToken = args[tokenIdx + 1];
                    }

                    if (!isRestarted)
                    {
                        // Launch dedicated guardian for the current process
                        try
                        {
                            var currentProcId = Process.GetCurrentProcess().Id;
                            string baseDir = AppContext.BaseDirectory;
                            string guardianPath = Path.Combine(baseDir, "BatRunGuardian.exe");
                            
                            // Check if it's in a subfolder (during development)
                            if (!File.Exists(guardianPath))
                            {
                                guardianPath = Path.Combine(baseDir, "Guardian", "bin", "Debug", "net10.0-windows7.0", "win-x64", "BatRunGuardian.exe");
                            }

                            if (File.Exists(guardianPath))
                            {
                                logger.LogInfo($"Launching dedicated guardian: {guardianPath} for PID {currentProcId}");
                                var startInfo = new ProcessStartInfo
                                {
                                    FileName = guardianPath,
                                    Arguments = $"{currentProcId} \"{baseDir}\"",
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                    WorkingDirectory = baseDir
                                };

                                var proc = Process.Start(startInfo);
                                if (proc != null)
                                {
                                    logger.LogInfo($"Guardian launched with PID {proc.Id}");
                                }
                                else
                                {
                                    logger.LogWarning("Guardian Process.Start returned null!");
                                }
                            }
                            else
                            {
                                logger.LogWarning("BatRunGuardian.exe not found. Running without guardian.");
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError("Failed to launch BatRun Guardian", ex);
                        }
                    }

                    using (var batrun = new Batrun(isRestarted, sessionToken))
                    {
                        batrun.Run();
                    }
                }
            }
            catch (Exception ex)
            {
                LogFatalError("Unhandled exception in Main", ex);
            }
            finally
            {
                VlcManager.Dispose();
                logger?.LogInfo("Application closing");
            }
        }

        private static void ShowSplashScreen()
        {
            var splash = new SplashForm();
            splash.Show();
            Application.DoEvents();

            var splashTimer = new System.Windows.Forms.Timer();
            splashTimer.Interval = 4000;
            splashTimer.Tick += (s, e) =>
            {
                splashTimer.Stop();
                splash.Close();
                splash.Dispose();
            };
            splashTimer.Start();
        }

        private static void CheckNetVersion(Logger? logger)
        {
            logger?.LogInfo("Checking for .NET 10 Desktop Runtime...");
            try
            {
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string runtimeDir = Path.Combine(programFiles, "dotnet", "shared", "Microsoft.WindowsDesktop.App");

                if (Directory.Exists(runtimeDir))
                {
                    var versions = Directory.GetDirectories(runtimeDir).Select(Path.GetFileName);
                    bool found = versions.Any(v => v != null && v.StartsWith("10."));

                    if (found)
                    {
                        logger?.LogInfo(".NET 10 Desktop Runtime found.");
                    }
                    else
                    {
                        logger?.LogInfo(".NET 10 Desktop Runtime not found in " + runtimeDir);
                        ShowNetVersionWarning(logger);
                    }
                }
                else
                {
                    logger?.LogInfo(".NET 10 Desktop Runtime directory not found at " + runtimeDir);
                    ShowNetVersionWarning(logger);
                }
            }
            catch (Exception ex)
            {
                logger?.LogInfo($"Error checking .NET version: {ex.Message}");
                LogFatalError("Error checking .NET version", ex);
            }
        }

        private static void ShowNetVersionWarning(Logger? logger)
        {
            logger?.LogInfo("Showing .NET version warning to the user.");

            string lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            string message, title, url, error_message, error_title;

            if (lang == "fr")
            {
                title = "Dépendance manquante : .NET 10";
                message = "BatRun nécessite le .NET 10 Desktop Runtime pour fonctionner. Veuillez l'installer depuis le site officiel de Microsoft.\n\nVoulez-vous ouvrir la page de téléchargement ?";
                url = "https://dotnet.microsoft.com/fr-fr/download/dotnet/10.0";
                error_title = "Erreur";
                error_message = "Impossible d'ouvrir le lien. Erreur : ";
            }
            else
            {
                title = "Missing Dependency: .NET 10";
                message = "BatRun requires the .NET 10 Desktop Runtime to run. Please install it from the official Microsoft website.\n\nDo you want to open the download page?";
                url = "https://dotnet.microsoft.com/en-us/download/dotnet/10.0";
                error_title = "Error";
                error_message = "Could not open the link. Error: ";
            }

            if (MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    logger?.LogInfo("User chose to open the download page.");
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    logger?.LogInfo($"Failed to open download page: {ex.Message}");
                    MessageBox.Show(error_message + ex.Message, error_title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                logger?.LogInfo("User chose not to open the download page. Exiting application.");
            }
            Environment.Exit(0);
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var ex = e.ExceptionObject as Exception;
                LogFatalError("Unhandled domain-level exception", ex);

                if (ex != null)
                {
                    MessageBox.Show(
                        $"A critical error occurred: {ex.Message}\n\nThe application will now close.",
                        "Critical Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            catch
            {
                File.WriteAllText(
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "BatRun_CriticalError.log"
                    ),
                    $"Unhandled exception at {DateTime.Now}: {e.ExceptionObject}"
                );
            }
        }

        private static void ApplicationOnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            LogFatalError("Unhandled thread exception", e.Exception);

            MessageBox.Show(
                $"A critical error occurred: {e.Exception.Message}\n\nThe application will now close.",
                "Critical Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }

        private static void LogFatalError(string message, Exception? ex = null)
        {
            try
            {
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                var errorLogger = new Logger("BatRun_error.log");
                errorLogger.LogCriticalError(message);

                if (ex != null)
                {
                    errorLogger.LogCriticalError($"Exception Details: {ex.Message}");
                    errorLogger.LogCriticalError($"Stack Trace: {ex.StackTrace}");

                    var innerEx = ex.InnerException;
                    while (innerEx != null)
                    {
                        errorLogger.LogCriticalError($"Inner Exception: {innerEx.Message}");
                        errorLogger.LogCriticalError($"Inner Exception Stack Trace: {innerEx.StackTrace}");
                        innerEx = innerEx.InnerException;
                    }
                }
            }
            catch
            {
                string fallbackLogPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "BatRun_LoggingError.log"
                );
                File.WriteAllText(
                    fallbackLogPath,
                    $"Logging failed at {DateTime.Now}: {message}\n{ex}"
                );
            }
        }
    }
}
