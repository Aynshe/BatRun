using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

namespace BatRun
{
    internal static class NativeMethods
    {
        // Native methods will be kept here temporarily to avoid breaking dependencies.
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        // ... other native methods from the original file ...
    }

    public class IniFile
    {
        // The IniFile class will be kept here temporarily.
        private string filePath;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public IniFile(string filePath)
        {
            this.filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
        }

        public void WriteValue(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, this.filePath);
        }

        public string ReadValue(string section, string key, string defaultValue = "")
        {
            StringBuilder retVal = new StringBuilder(255);
            GetPrivateProfileString(section, key, defaultValue, retVal, 255, this.filePath);
            return retVal.ToString();
        }
    }

    public static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // The application is now started through the Batrun class.
            using (var batrunApp = new Batrun())
            {
                batrunApp.Run();
            }
        }
    }
}
