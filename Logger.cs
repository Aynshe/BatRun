using System;
using System.IO;
using System.Threading;

namespace BatRun
{
    public class Logger
    {
        private readonly string logFilePath;
        private readonly object _lock = new();
        private readonly bool isEnglishCulture;
        private readonly bool isLoggingEnabled;

        public Logger(string logFileName = "BatRun.log")
        {
            // Lire la configuration du logging
            var configPath = Path.Combine(AppContext.BaseDirectory, "config.ini");
            var config = new IniFile(configPath);
            isLoggingEnabled = config.ReadBool("Logging", "EnableLogging", true);
            
            // Check the system culture
            var culture = System.Globalization.CultureInfo.CurrentUICulture;
            
            // Only use French if the culture is explicitly French (fr-FR)
            isEnglishCulture = culture.Name != "fr-FR";
            
            // Use AppContext.BaseDirectory for the log path
            string logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
            
            // Create the directory if it does not exist
            Directory.CreateDirectory(logDirectory);
            
            // Full path of the log file
            logFilePath = Path.Combine(logDirectory, logFileName);
            
            // Delete the existing log file
            try 
            {
                if (File.Exists(logFilePath))
                {
                    File.Delete(logFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error deleting the log file: {ex.Message}");
            }

            // Write a header message to indicate a new startup
           // string startupMessage = isEnglishCulture 
           //     ? $"--- Application startup {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---" 
            //    : $"--- Démarrage de l'application {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---";
           // 
           // Log(startupMessage, "INIT");
        }

        public void Log(string message, string type = "INFO")
        {
            if (!isLoggingEnabled) return; // Ne pas logger si désactivé
            
            // Translate certain log types if necessary
            if (!isEnglishCulture)
            {
                type = type switch
                {
                    "INFO" => "INFO",
                    "ERROR" => "ERREUR",
                    "WARNING" => "AVERTISSEMENT",
                    "CRITICAL" => "CRITIQUE",
                    "INIT" => "INIT",
                    _ => type
                };
            }

            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{type}] {message}";

            lock (_lock)
            {
                try 
                {
                    File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
                }
                catch (IOException ioEx)
                {
                    string errorMsg = isEnglishCulture 
                        ? $"Logging failed: {ioEx.Message}" 
                        : $"Échec de l'enregistrement du log : {ioEx.Message}";
                    Console.Error.WriteLine(errorMsg);
                }
            }
        }

        public void LogInfo(string message)
        {
            Log(message, "INFO");
        }

        public void LogError(string message, Exception? ex = null)
        {
            string fullMessage = ex != null 
                ? $"{message}: {ex.Message}\nStack Trace: {ex.StackTrace}" 
                : message;
            
            Log(fullMessage, "ERROR");
        }

        public void LogCriticalError(string message)
        {
            Log(message, "CRITICAL");
        }

        public void LogWarning(string message)
        {
            Log($"[WARNING] {message}", "WARNING");
        }

        public void ClearLogFile()
        {
            try 
            {
                if (File.Exists(logFilePath))
                {
                    File.Delete(logFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error deleting the log file: {ex.Message}");
            }
        }
    }
}
