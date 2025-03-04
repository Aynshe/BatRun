using System.Collections.Concurrent;
using System.Globalization;

namespace BatRun
{
    public class LocalizedStrings
    {
        private static readonly ConcurrentDictionary<string, string> _translations = new();
        private static readonly Logger _logger = new Logger("BatRun.log");
        private static string _currentLanguage = "en";

        public LocalizedStrings()
        {
            LoadTranslations();
        }

        public static void LoadTranslations()
        {
            _translations.Clear();
            
            // Add default English translations
            _translations["Random Wallpaper"] = "Random Wallpaper";
            // Add other default English translations here...

            var culture = CultureInfo.CurrentUICulture;
            _currentLanguage = culture.TwoLetterISOLanguageName.ToLower() switch
            {
                "fr" => "fr",
                "es" => "es",
                "ja" => "ja",
                "zh" => "zh-CN",
                "it" => "it",
                "pt" => culture.Name.Equals("pt-BR", StringComparison.OrdinalIgnoreCase) ? "pt-BR" : "pt-PT",
                "de" => "de",
                "no" => "no",
                "pl" => "pl",
                "ru" => "ru",
                "sv" => "sv",
                "ko" => "ko",
                _ => "en"
            };

            // Si la langue n'est pas l'anglais, charger les traductions
            if (_currentLanguage != "en")
            {
                string poPath = Path.Combine(AppContext.BaseDirectory, "Locales", _currentLanguage, "messages.po");
                if (File.Exists(poPath))
                {
                    LoadTranslationsFromFile(poPath);
                }
            }
        }

        private static void LoadTranslationsFromFile(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);
                string? msgid = null;

                foreach (var line in lines)
                {
                    if (line.StartsWith("msgid \""))
                    {
                        msgid = line.Substring(7, line.Length - 8);
                    }
                    else if (line.StartsWith("msgstr \"") && msgid != null)
                    {
                        var msgstr = line.Substring(8, line.Length - 9);
                        if (!string.IsNullOrEmpty(msgstr))
                        {
                            _translations[msgid] = msgstr;
                        }
                        msgid = null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading translations: {ex.Message}");
            }
        }

        public static string GetString(string key)
        {
            return _translations.GetValueOrDefault(key, key);
        }

        // Properties to access translated strings
        public string OpenBatRun => GetString("Open BatRun");
        public string OpenEmulationStation => GetString("Launch RetroBat");
        public string LaunchBatGui => GetString("Launch BatGui");
        public string Configuration => GetString("Configuration");
        public string GeneralSettings => GetString("General Settings");
        public string ControllerMappings => GetString("Controller Mappings");
        public string Help => GetString("Help");
        public string ViewLogs => GetString("View Logs");
        public string ViewErrorLogs => GetString("View Error Logs");
        public string About => GetString("About");
        public string Exit => GetString("Exit");
        public string HelpAndSupport => GetString("Help & Support");

        // About window properties
        public string AboutBatRun => GetString("About BatRun");
        public string VersionLabel => GetString("Version");
        public string DevelopedBy => GetString("Developed by AI for Aynshe");
        public string Description => GetString("A launcher for RetroBat with Hotkey select/back+start.");
        public string JoinDiscord => GetString("Join us on Discord");
        public string SourceCode => GetString("Source code on GitHub");

        // Update related strings
        public string CheckForUpdates => GetString("Check for Updates");
        public string CheckingForUpdates => GetString("Checking for Updates");
        public string CheckingForUpdatesProgress => GetString("Checking for updates...");
        public string UpdateAvailable => GetString("Update Available");
        public string NewVersionAvailable => GetString("New version {0} is available. Would you like to update?");
        public string DownloadingUpdate => GetString("Downloading Update");
        public string StartingDownload => GetString("Starting download...");
        public string DownloadingUpdateProgress => GetString("Downloading update: {0}%");
        public string UpdateFailed => GetString("Update Failed");
        public string UpdateFailedMessage => GetString("Failed to install update. Please try again later.");
        public string NoUpdatesAvailable => GetString("No Updates Available");
        public string LatestVersion => GetString("You have the latest version.");
        public string UpdateCheckFailed => GetString("Update check failed");
        public string UpdateCheckFailedMessage => GetString("Failed to check for updates. Please try again later.");
        public string ShellLauncher => GetString("Shell Launcher");
    }
} 