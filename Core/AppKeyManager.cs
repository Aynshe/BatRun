using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.Core
{
    public class AppKeyConfig
    {
        public string ExeName { get; set; } = "";
        public string PauseKey { get; set; } = "ESC";
        public string ResumeKey { get; set; } = "ESC";
        public string TimeoutKey { get; set; } = "";     // EN: Key to close game on timeout
        public int TimeoutSeconds { get; set; } = 0; // EN: Delay before auto-close (0 = disabled)
        public bool AllowAltTab { get; set; } = false; // EN: Allow Alt+Tab during active session / FR: Autoriser Alt+Tab pendant une session active
        public string AllowedForegroundWindows { get; set; } = ""; // EN: Exe names allowed in foreground (comma separated) / FR: Noms d'exécutables autorisés au premier plan (séparés par virgules)
    }

    public static class AppKeyManager
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_keys.json");
        public static List<AppKeyConfig> Configs { get; private set; } = new List<AppKeyConfig>();

        public static void Load()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigPath);
                    Configs = JsonConvert.DeserializeObject<List<AppKeyConfig>>(json) ?? new List<AppKeyConfig>();
                }
                catch { }
            }
            else
            {
                // Create defaults based on typical es_padtokey.cfg
                Configs = new List<AppKeyConfig>
                {
                    new AppKeyConfig { ExeName = "retroarch", PauseKey = "P", ResumeKey = "P" },
                    new AppKeyConfig { ExeName = "mame", PauseKey = "SUSPEND", ResumeKey = "SUSPEND" },
                    new AppKeyConfig { ExeName = "mame64", PauseKey = "SUSPEND", ResumeKey = "SUSPEND" },
                    new AppKeyConfig { ExeName = "mame32", PauseKey = "SUSPEND", ResumeKey = "SUSPEND" },
                    new AppKeyConfig { ExeName = "TeknoParrotUi", PauseKey = "SUSPEND", ResumeKey = "SUSPEND" },
                    new AppKeyConfig { ExeName = "Dolphin", PauseKey = "P", ResumeKey = "P" },
                    new AppKeyConfig { ExeName = "DolphinWX", PauseKey = "P", ResumeKey = "P" },
                    new AppKeyConfig { ExeName = "pcsx2", PauseKey = "SPACE", ResumeKey = "SPACE" },
                    new AppKeyConfig { ExeName = "pcsx2-qt", PauseKey = "SPACE", ResumeKey = "SPACE" },
                    new AppKeyConfig { ExeName = "duckstation-qt-x64-ReleaseLTCG", PauseKey = "SPACE", ResumeKey = "SPACE" },
                    new AppKeyConfig { ExeName = "Cemu", PauseKey = "SUSPEND", ResumeKey = "SUSPEND" },
                    new AppKeyConfig { ExeName = "emulator_multicpu", PauseKey = "", ResumeKey = "" },
                    new AppKeyConfig { ExeName = "[DEFAULT_PC_GAME]", PauseKey = "SUSPEND", ResumeKey = "SUSPEND" }
                };
                Save();
            }
        }

        public static void Save()
        {
            try
            {
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(Configs, Formatting.Indented));
            }
            catch { }
        }

        public static AppKeyConfig? GetConfigForExe(string exeName)
        {
            return Configs.Find(c => string.Equals(c.ExeName, exeName, StringComparison.OrdinalIgnoreCase));
        }
    }
}


