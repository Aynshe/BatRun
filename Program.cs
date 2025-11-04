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
                        logger.LogInfo("Another instance is already running");
                        MessageBox.Show("Another instance of BatRun is already running.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

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

                    using (var batrun = new Batrun())
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
            logger?.LogInfo("Checking for .NET 8.0 Desktop Runtime...");
            try
            {
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string runtimeDir = Path.Combine(programFiles, "dotnet", "shared", "Microsoft.WindowsDesktop.App");

                if (Directory.Exists(runtimeDir))
                {
                    var versions = Directory.GetDirectories(runtimeDir).Select(Path.GetFileName);
                    bool found = versions.Any(v => v != null && v.StartsWith("8."));

                    if (found)
                    {
                        logger?.LogInfo(".NET 8.0 Desktop Runtime found.");
                    }
                    else
                    {
                        logger?.LogInfo(".NET 8.0 Desktop Runtime not found in " + runtimeDir);
                        ShowNetVersionWarning(logger);
                    }
                }
                else
                {
                    logger?.LogInfo(".NET 8.0 Desktop Runtime directory not found at " + runtimeDir);
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
                title = "Dépendance manquante : .NET 8.0";
                message = "BatRun nécessite le .NET 8.0 Desktop Runtime pour fonctionner. Veuillez l'installer depuis le site officiel de Microsoft.\n\nVoulez-vous ouvrir la page de téléchargement ?";
                url = "https://dotnet.microsoft.com/fr-fr/download/dotnet/8.0";
                error_title = "Erreur";
                error_message = "Impossible d'ouvrir le lien. Erreur : ";
            }
            else
            {
                title = "Missing Dependency: .NET 8.0";
                message = "BatRun requires the .NET 8.0 Desktop Runtime to run. Please install it from the official Microsoft website.\n\nDo you want to open the download page?";
                url = "https://dotnet.microsoft.com/en-us/download/dotnet/8.0";
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
