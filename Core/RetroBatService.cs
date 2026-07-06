using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;

using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.Core
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
            string? registryPath = null;
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\RetroBat");
                if (key != null)
                {
                    registryPath = key.GetValue("LatestKnownInstallPath") as string;
                    if (!string.IsNullOrEmpty(registryPath) && Directory.Exists(registryPath))
                    {
                        var exePath = Directory.EnumerateFiles(registryPath, "retrobat.exe", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();
                        if (!string.IsNullOrEmpty(exePath))
                        {
                            _retrobatPath = exePath;
                            _logger.Log($"RetroBat path found in registry: {_retrobatPath}");
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error reading registry", ex);
            }

            var defaultDir = @"C:\Retrobat";
            if (Directory.Exists(defaultDir))
            {
                var defaultExePath = Directory.EnumerateFiles(defaultDir, "retrobat.exe", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();
                if (!string.IsNullOrEmpty(defaultExePath))
                {
                    _retrobatPath = defaultExePath;
                    _logger.Log($"Using default path: {_retrobatPath}");
                    return;
                }
            }

            _retrobatPath = Path.Combine(defaultDir, "retrobat.exe");
            var errorMessage = "Unable to find retrobat.exe.";
            if (!string.IsNullOrEmpty(registryPath))
            {
                errorMessage += $" Searched in registry path '{registryPath}' and default path '{defaultDir}'.";
            }
            else
            {
                errorMessage += $" Searched in default path '{defaultDir}'.";
            }
            _logger.LogError(errorMessage);
        }

        public bool IsRetroBatOrESRunning()
        {
            try
            {
                var esProcesses = System.Diagnostics.Process.GetProcessesByName("emulationstation");
                var rbProcesses = System.Diagnostics.Process.GetProcessesByName("retrobat");
                return (esProcesses.Length > 0 && !esProcesses[0].HasExited) || 
                       (rbProcesses.Length > 0 && !rbProcesses[0].HasExited);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error checking EmulationStation/RetroBat processes", ex);
                return false;
            }
        }

        public bool IsEmulationStationRunning()
        {
            try
            {
                var processes = System.Diagnostics.Process.GetProcessesByName("emulationstation");
                return processes.Any(p => {
                    try { return !p.HasExited; } catch { return false; }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error checking EmulationStation process", ex);
                return false;
            }
        }

        public async void LaunchEmulationStation()
        {
            try
            {
                _logger.LogInfo("Launching EmulationStation");

                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _retrobatPath,
                    Arguments = "-es",
                    UseShellExecute = false,
                    CreateNoWindow = false
                };

                for (int i = 0; i < 3; i++)
                {
                    if (!IsRetroBatOrESRunning())
                    {
                        System.Diagnostics.Process.Start(startInfo);
                        _logger.LogInfo("EmulationStation launched successfully");
                        return;
                    }
                    _logger.LogInfo($"RetroBat/ES is running. Waiting 2s before retry {i + 1}/3...");
                    await Task.Delay(2000);
                }
                
                _logger.LogWarning("RetroBat/ES is still running after 3 retries. Launch aborted.");
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
                Arguments = "--external-launcher",
                UseShellExecute = false
            };

            for (int i = 0; i < 3; i++)
            {
                if (!IsRetroBatOrESRunning())
                {
                    System.Diagnostics.Process.Start(startInfo);
                    _logger.LogInfo("RetroBat launched successfully");
                    return;
                }
                _logger.LogInfo($"RetroBat/ES is running. Waiting 2s before retry {i + 1}/3...");
                await Task.Delay(2000);
            }
            
            _logger.LogWarning("RetroBat/ES is still running after 3 retries. Launch aborted.");
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


