using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

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

                    Application.SetHighDpiMode(HighDpiMode.SystemAware);
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    string libvlcPath = Path.Combine(AppContext.BaseDirectory, "libvlc");
                    if (Directory.Exists(libvlcPath))
                    {
                        LibVLCSharp.Shared.Core.Initialize(libvlcPath);
                    }
                    else
                    {
                        LibVLCSharp.Shared.Core.Initialize();
                    }

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
            logger?.LogInfo("Checking for .NET 8.0 Runtime...");
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost"))
                {
                    if (key != null)
                    {
                        var names = key.GetValueNames();
                        bool found = false;
                        foreach (var name in names)
                        {
                            if (name.StartsWith("8."))
                            {
                                found = true;
                                break;
                            }
                        }

                        if (found)
                        {
                            logger?.LogInfo(".NET 8.0 Runtime found.");
                        }
                        else
                        {
                            logger?.LogInfo(".NET 8.0 Runtime not found.");
                            ShowNetVersionWarning(logger);
                        }
                    }
                    else
                    {
                        logger?.LogInfo(".NET 8.0 Runtime not found.");
                        ShowNetVersionWarning(logger);
                    }
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
            string message = "BatRun nécessite .NET 8.0 pour fonctionner. Veuillez l'installer depuis le site officiel de Microsoft.\n\nVoulez-vous ouvrir la page de téléchargement ?";
            string title = "Dépendance manquante : .NET 8.0";
            if (MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    logger?.LogInfo("User chose to open the download page.");
                    Process.Start(new ProcessStartInfo("https://dotnet.microsoft.com/download/dotnet/8.0") { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    logger?.LogInfo($"Failed to open download page: {ex.Message}");
                    MessageBox.Show($"Impossible d'ouvrir le lien. Erreur : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
