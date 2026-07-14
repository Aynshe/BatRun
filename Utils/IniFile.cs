using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace BatRun.Utils
{
    public class IniFile
    {
        private string filePath;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern uint GetPrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpDefault,
            StringBuilder lpReturnedString,
            uint nSize,
            string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool WritePrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpString,
            string lpFileName);

        public IniFile(string filePath)
        {
            this.filePath = filePath;
            EnsureConfigFileExists();
            MigrateConfig();
        }

        private void EnsureConfigFileExists()
        {
            if (!File.Exists(filePath))
            {
                var directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Write default values
                WriteValue("Focus", "FocusDuration", "9000");
                WriteValue("Focus", "FocusInterval", "3000");
                WriteValue("Windows", "MinimizeWindows", "true");
                WriteValue("Logging", "EnableLogging", "false");
                WriteValue("Wallpaper", "IsActive", "true");
                WriteValue("Wallpaper", "Selected", @"16x9\video\BatRun_loading.mp4");
                WriteValue("Wallpaper", "SelectedFolder", @"16x9\video");
                WriteValue("Wallpaper", "EnableWithExplorer", "true");
                WriteValue("Wallpaper", "EnableAudio", "true");
                WriteValue("Wallpaper", "LoopVideo", "true");
                WriteValue("System", "Version", "3.1.0");
            }
        }

        private void MigrateConfig()
        {
            if (File.Exists(filePath))
            {
                string currentVersion = ReadValue("System", "Version", "3.0.0");
                
                // Si la version est 3.0.0 ou inférieure (ou qu'elle n'était pas enregistrée)
                if (currentVersion == "3.0.0" || string.IsNullOrEmpty(ReadValue("System", "Version", "")))
                {
                    // Force les nouvelles valeurs par défaut pour la transition vers la 3.1.0
                    WriteValue("Wallpaper", "IsActive", "true");
                    WriteValue("Wallpaper", "Selected", @"16x9\video\BatRun_loading.mp4");
                    WriteValue("Wallpaper", "SelectedFolder", @"16x9\video");
                    WriteValue("Wallpaper", "EnableWithExplorer", "true");
                    WriteValue("Wallpaper", "EnableAudio", "true");
                    WriteValue("Wallpaper", "LoopVideo", "true");
                    
                    // Enregistrer la version 3.1.0 pour marquer que la migration a été faite
                    WriteValue("System", "Version", "3.1.0");
                }
                else
                {
                    // Fallback générique pour les installations futures si certaines clés manquent individuellement
                    if (ReadValue("Wallpaper", "IsActive", "").Length == 0)
                    {
                        WriteValue("Wallpaper", "IsActive", "true");
                    }
                    if (ReadValue("Wallpaper", "Selected", "").Length == 0)
                    {
                        WriteValue("Wallpaper", "Selected", @"16x9\video\BatRun_loading.mp4");
                    }
                    if (ReadValue("Wallpaper", "SelectedFolder", "").Length == 0)
                    {
                        WriteValue("Wallpaper", "SelectedFolder", @"16x9\video");
                    }
                    if (ReadValue("Wallpaper", "EnableWithExplorer", "").Length == 0)
                    {
                        WriteValue("Wallpaper", "EnableWithExplorer", "true");
                    }
                    if (ReadValue("Wallpaper", "EnableAudio", "").Length == 0)
                    {
                        WriteValue("Wallpaper", "EnableAudio", "true");
                    }
                    if (ReadValue("Wallpaper", "LoopVideo", "").Length == 0)
                    {
                        WriteValue("Wallpaper", "LoopVideo", "true");
                    }
                }
            }
        }

        public void WriteValue(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, filePath);
        }

        public string ReadValue(string section, string key, string defaultValue)
        {
            StringBuilder result = new StringBuilder(2048);
            GetPrivateProfileString(section, key, defaultValue, result, (uint)result.Capacity, filePath);
            return result.ToString();
        }

        public int ReadInt(string section, string key, int defaultValue)
        {
            string value = ReadValue(section, key, defaultValue.ToString());
            return int.TryParse(value, out int result) ? result : defaultValue;
        }

        public bool ReadBool(string section, string key, bool defaultValue)
        {
            string value = ReadValue(section, key, defaultValue.ToString()).ToLower();
            return value == "true" || value == "1";
        }
    }
}
