using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace BatRun
{
    public class RetroBatService
    {
        private readonly Logger _logger;
        private readonly IniFile _config;
        private string _retrobatPath = "";

        public RetroBatService(Logger logger, IniFile config)
        {
            _logger = logger;
            _config = config;
            InitializeRetrobatPath();
        }

        public string GetRetrobatPath()
        {
            return _retrobatPath;
        }

        private void InitializeRetrobatPath()
        {
            _logger.Log("Searching for RetroBat path");
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\RetroBat");
                if (key != null)
                {
                    var path = key.GetValue("LatestKnownInstallPath") as string;
                    if (!string.IsNullOrEmpty(path))
                    {
                        _retrobatPath = Path.Combine(path, "retrobat.exe");
                        _logger.Log($"RetroBat path found in registry: {_retrobatPath}");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error reading registry", ex);
            }

            _retrobatPath = @"C:\Retrobat\retrobat.exe"; // Default path
            _logger.Log($"Using default path: {_retrobatPath}");

            if (!File.Exists(_retrobatPath))
            {
                var error = $"Unable to find RetroBat at {_retrobatPath}";
                _logger.LogError(error);
            }
        }

        public bool IsEmulationStationRunning()
        {
            try
            {
                var processes = System.Diagnostics.Process.GetProcessesByName("emulationstation");
                return processes.Length > 0 && !processes[0].HasExited;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error checking EmulationStation", ex);
                return false;
            }
        }

        public void LaunchEmulationStation()
        {
            try
            {
                _logger.LogInfo("Launching EmulationStation");

                if (IsEmulationStationRunning())
                {
                    _logger.LogInfo("EmulationStation is already running");
                    return;
                }

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _retrobatPath,
                    Arguments = "-es",
                    UseShellExecute = false,
                    CreateNoWindow = false
                });

                _logger.LogInfo("EmulationStation launched successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error launching EmulationStation", ex);
            }
        }

        public async Task Launch()
        {
            if (string.IsNullOrEmpty(_retrobatPath) || !File.Exists(_retrobatPath))
            {
                _logger.LogError("Retrobat path is not valid, cannot launch.");
                return;
            }

            await CheckIntroSettings();

            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = _retrobatPath,
                UseShellExecute = false
            };
            System.Diagnostics.Process.Start(startInfo);
        }

        private async Task CheckIntroSettings()
        {
            string retrobatIniPath = Path.Combine(Path.GetDirectoryName(_retrobatPath) ?? string.Empty, "retrobat.ini");
            if (File.Exists(retrobatIniPath))
            {
                var lines = await File.ReadAllLinesAsync(retrobatIniPath);
                string? enableIntro = lines.FirstOrDefault(line => line.StartsWith("EnableIntro="))?.Split('=')[1];
                string? videoDuration = lines.FirstOrDefault(line => line.StartsWith("VideoDuration="))?.Split('=')[1];

                if (enableIntro == "1" && videoDuration != null && int.TryParse(videoDuration, out int duration))
                {
                    _logger.LogInfo($"Waiting for intro video duration: {duration} ms");
                    await Task.Delay(duration);
                }
                else
                {
                    _logger.LogInfo("Intro video not enabled, proceeding without delay.");
                }
            }
            else
            {
                _logger.LogError("Retrobat.ini file not found.");
            }
        }
    }
}
