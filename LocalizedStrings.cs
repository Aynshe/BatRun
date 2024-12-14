using System;
using System.Globalization;

namespace BatRun
{
    public class LocalizedStrings
    {
        public string OpenEmulationStation { get; set; } = string.Empty;
        public string LaunchBatGui { get; set; } = string.Empty;
        public string Configuration { get; set; } = string.Empty;
        public string GeneralSettings { get; set; } = string.Empty;
        public string ControllerMappings { get; set; } = string.Empty;
        public string Help { get; set; } = string.Empty;
        public string ViewLogs { get; set; } = string.Empty;
        public string ViewErrorLogs { get; set; } = string.Empty;
        public string About { get; set; } = string.Empty;
        public string Exit { get; set; } = string.Empty;
        public string OpenBatRun { get; set; } = string.Empty;
        public string HelpAndSupport { get; set; } = string.Empty;

        public static LocalizedStrings GetStrings()
        {
            var culture = CultureInfo.CurrentUICulture;
            
            // Use French if the culture starts with "fr-" (includes all French variants)
            if (culture.Name.StartsWith("fr-"))
            {
                return new LocalizedStrings
                {
                    OpenBatRun = "Ouvrir BatRun",
                    OpenEmulationStation = "Lancer RetroBat",
                    LaunchBatGui = "Lancer BatGui",
                    Configuration = "Configuration",
                    GeneralSettings = "Paramètres généraux",
                    ControllerMappings = "Configuration des manettes",
                    Help = "Aide",
                    ViewLogs = "Voir les logs",
                    ViewErrorLogs = "Voir les logs d'erreurs",
                    Exit = "Quitter",
                    About = "À propos",
                    HelpAndSupport = "Aide & Support"
                };
            }
            
            // Default to English for all other cultures
            return new LocalizedStrings
            {
                OpenBatRun = "Open BatRun",
                OpenEmulationStation = "Launch RetroBat",
                LaunchBatGui = "Launch BatGui",
                Configuration = "Configuration",
                GeneralSettings = "General Settings",
                ControllerMappings = "Controller Mappings",
                Help = "Help",
                ViewLogs = "View Logs",
                ViewErrorLogs = "View Error Logs",
                Exit = "Exit",
                About = "About",
                HelpAndSupport = "Help & Support"
            };
        }
    }
} 