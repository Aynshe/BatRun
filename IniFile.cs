using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace BatRun
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
            }
        }

        public void WriteValue(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, filePath);
        }

        public string ReadValue(string section, string key, string defaultValue)
        {
            StringBuilder result = new StringBuilder(255);
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
