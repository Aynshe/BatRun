using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.NetworkInformation;
using System.Linq;

using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.Core
{
    public class UpdateChecker
    {
        private const string GITHUB_API_URL = "https://api.github.com/repos/Aynshe/BatRun/releases/latest";
        private const string USER_AGENT = "BatRun-UpdateChecker";
        private readonly Logger logger;
        private readonly Logger updateLogger;
        private readonly string currentVersion;
        private static readonly HttpClient client = new HttpClient();

        public class UpdateCheckResult
        {
            public bool UpdateAvailable { get; set; }
            public string LatestVersion { get; set; }
            public string DownloadUrl { get; set; }
            public bool HasInternetConnection { get; set; }

            public UpdateCheckResult(bool updateAvailable, string latestVersion, string downloadUrl, bool hasInternet)
            {
                UpdateAvailable = updateAvailable;
                LatestVersion = latestVersion;
                DownloadUrl = downloadUrl;
                HasInternetConnection = hasInternet;
            }
        }

        public UpdateChecker(Logger logger, string currentVersion)
        {
            this.logger = logger;
            this.currentVersion = currentVersion;
            
            // Create a separate logger for updates
            string updateLogPath = Path.Combine(AppContext.BaseDirectory, "Logs", "BatRun_update.log");
            this.updateLogger = new Logger(updateLogPath);
            
            try
            {
                // [BATRUN-FIX]: Ensure User-Agent is only added once and set a reasonable timeout
                // FR: S'assurer que le User-Agent n'est ajouté qu'une fois et définir un timeout raisonnable
                if (!client.DefaultRequestHeaders.Contains("User-Agent"))
                    client.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
                
                client.Timeout = TimeSpan.FromSeconds(10);
            }
            catch { }

            // Clean up old update logs
            CleanupOldUpdateLogs();
        }

        private bool CheckInternetConnection()
        {
            try
            {
                // [BATRUN-FIX]: Ping can be flaky or blocked. Using a very short timeout.
                // FR: Le Ping peut être instable ou bloqué. Utilisation d'un timeout très court.
                using (var ping = new Ping())
                {
                    var reply = ping.Send("8.8.8.8", 1000); 
                    return reply?.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        private void CleanupOldUpdateLogs()
        {
            try
            {
                string logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
                if (Directory.Exists(logDirectory))
                {
                    // Garder seulement les 5 derniers fichiers de log de mise à jour
                    var updateLogs = Directory.GetFiles(logDirectory, "BatRun_update*.log")
                        .OrderByDescending(f => new FileInfo(f).CreationTime)
                        .Skip(5);

                    foreach (var log in updateLogs)
                    {
                        try
                        {
                            File.Delete(log);
                            logger.LogInfo($"Deleted old update log: {log}");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"Failed to delete old update log {log}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error cleaning up old update logs: {ex.Message}");
            }
        }

        public async Task<UpdateCheckResult> CheckForUpdates()
        {
            logger.LogInfo($"Checking for updates... (Current version: {currentVersion})");
            try
            {
                // First check internet connection
                if (!CheckInternetConnection())
                {
                    logger.LogInfo("No internet connection detected (Ping failed)");
                    return new UpdateCheckResult(false, "", "", false);
                }

                logger.LogInfo($"Fetching latest release info from: {GITHUB_API_URL}");
                var response = await client.GetStringAsync(GITHUB_API_URL);
                using var document = JsonDocument.Parse(response);
                var root = document.RootElement;

                string latestVersion = root.GetProperty("tag_name").GetString() ?? "";
                if (string.IsNullOrEmpty(latestVersion))
                {
                    logger.LogError("Unable to get latest version from GitHub");
                    return new UpdateCheckResult(false, "", "", true);
                }

                // Extract version number from "BatRun_X.Y.Z" format
                string latestVersionNumber = latestVersion.Replace("BatRun_", "");
                string currentVersionNumber = currentVersion;

                // Ensure versions have three parts (X.Y.Z)
                latestVersionNumber = NormalizeVersion(latestVersionNumber);
                currentVersionNumber = NormalizeVersion(currentVersionNumber);

                // Compare versions
                var updateAvailable = IsNewerVersion(latestVersionNumber, currentVersionNumber);
                
                string downloadUrl = "";
                if (updateAvailable)
                {
                    var assets = root.GetProperty("assets");
                    foreach (var asset in assets.EnumerateArray())
                    {
                        var name = asset.GetProperty("name").GetString();
                        // Check for both .zip and .7z files
                        if (name != null && (name.EndsWith(".zip") || name.EndsWith(".7z")))
                        {
                            downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                            break;
                        }
                    }
                }

                return new UpdateCheckResult(updateAvailable, latestVersion, downloadUrl, true);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError($"Network error checking for updates: {ex.Message}");
                return new UpdateCheckResult(false, "", "", false);
            }
            catch (TaskCanceledException)
            {
                logger.LogWarning("Update check timed out after 10 seconds.");
                return new UpdateCheckResult(false, "", "", false);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error checking for updates: {ex.Message}", ex);
                return new UpdateCheckResult(false, "", "", true);
            }
        }

        private string NormalizeVersion(string version)
        {
            try
            {
                var parts = version.Split('.');
                if (parts.Length < 3)
                {
                    var newParts = new string[3];
                    Array.Copy(parts, newParts, parts.Length);
                    for (int i = parts.Length; i < 3; i++)
                    {
                        newParts[i] = "0";
                    }
                    return string.Join(".", newParts);
                }
                return version;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error normalizing version {version}: {ex.Message}", ex);
                return version;
            }
        }

        private bool IsNewerVersion(string latestVersion, string currentVersion)
        {
            try
            {
                var latest = Version.Parse(latestVersion);
                var current = Version.Parse(currentVersion);
                int comparison = latest.CompareTo(current);
                logger.LogInfo($"Version comparison: Latest={latestVersion} Current={currentVersion} Result={comparison}");
                return comparison > 0;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error comparing versions ({latestVersion} vs {currentVersion}): {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> DownloadAndInstallUpdate(string downloadUrl, IProgress<int> progress)
        {
            string tempDir = "";
            try
            {
                tempDir = Path.Combine(Path.GetTempPath(), "BatRunUpdate");
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                Directory.CreateDirectory(tempDir);
                updateLogger.LogInfo($"Created temporary directory: {tempDir}");

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string updateLogPath = Path.Combine(AppContext.BaseDirectory, "Logs", $"BatRun_update_{timestamp}.log");
                updateLogger.LogInfo($"Starting update process, log will be saved to: {updateLogPath}");

                string archivePath = Path.Combine(tempDir, Path.GetFileName(downloadUrl));
                updateLogger.LogInfo($"Downloading update to: {archivePath}");

                using (var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var buffer = new byte[8192];
                    var totalBytesRead = 0L;

                    using (var fileStream = File.Create(archivePath))
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        while (true)
                        {
                            var bytesRead = await contentStream.ReadAsync(buffer);
                            if (bytesRead == 0) break;
                            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                            totalBytesRead += bytesRead;
                            if (totalBytes != -1L)
                            {
                                var progressPercentage = (int)((totalBytesRead * 100) / totalBytes);
                                progress.Report(progressPercentage);
                            }
                        }
                    }
                }

                string extractPath = Path.Combine(tempDir, "extracted");
                Directory.CreateDirectory(extractPath);
                updateLogger.LogInfo($"Extracting to: {extractPath}");

                if (archivePath.EndsWith(".7z"))
                {
                    var sevenZipPath = Path.Combine(AppContext.BaseDirectory, "7za.exe");
                    if (!File.Exists(sevenZipPath))
                    {
                        updateLogger.LogError("7za.exe not found for extraction");
                        return false;
                    }

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = sevenZipPath,
                        Arguments = $"x \"{archivePath}\" -o\"{extractPath}\" -y",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            await process.WaitForExitAsync();
                            if (process.ExitCode != 0)
                            {
                                updateLogger.LogError($"7-Zip extraction failed with exit code {process.ExitCode}");
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    ZipFile.ExtractToDirectory(archivePath, extractPath);
                }

                string batrunSourcePath = Path.Combine(extractPath, "plugins", "BatRun");
                if (!Directory.Exists(batrunSourcePath))
                {
                    updateLogger.LogError("BatRun folder not found in update package");
                    return false;
                }

                string batchPath = Path.Combine(tempDir, "update.bat");
                string appDir = AppContext.BaseDirectory;
                string updateScript = $@"@echo off
chcp 65001 > nul
set ""LOG_FILE=%TEMP%\BatRun_update.log""
echo [%date% %time%] Terminating processes >> ""%LOG_FILE%""
taskkill /F /IM BatRunGuardian.exe /T 2>nul
taskkill /F /IM BatRun.exe /T 2>nul
:waitloop
tasklist /FI ""IMAGENAME eq BatRun.exe"" 2>NUL | find /I /N ""BatRun.exe"">NUL
set RUNNING1=%ERRORLEVEL%
tasklist /FI ""IMAGENAME eq BatRunGuardian.exe"" 2>NUL | find /I /N ""BatRunGuardian.exe"">NUL
set RUNNING2=%ERRORLEVEL%
if ""%RUNNING1%""==""0"" ( timeout /t 1 /nobreak >nul & goto waitloop )
if ""%RUNNING2%""==""0"" ( timeout /t 1 /nobreak >nul & goto waitloop )
timeout /t 2 /nobreak >nul
if exist ""{appDir}\BatRun.exe.bak"" del ""{appDir}\BatRun.exe.bak""
if exist ""{appDir}\BatRun.exe"" move ""{appDir}\BatRun.exe"" ""{appDir}\BatRun.exe.bak""
if exist ""{appDir}\BatRunGuardian.exe.bak"" del ""{appDir}\BatRunGuardian.exe.bak""
if exist ""{appDir}\BatRunGuardian.exe"" move ""{appDir}\BatRunGuardian.exe"" ""{appDir}\BatRunGuardian.exe.bak""
if exist ""{appDir}\BatRunGuardian.dll.bak"" del ""{appDir}\BatRunGuardian.dll.bak""
if exist ""{appDir}\BatRunGuardian.dll"" move ""{appDir}\BatRunGuardian.dll"" ""{appDir}\BatRunGuardian.dll.bak""
xcopy ""{batrunSourcePath}\*.*"" ""{appDir}"" /E /I /Y /F
if exist ""{appDir}\BatRun.exe.bak"" del ""{appDir}\BatRun.exe.bak""
if exist ""{appDir}\BatRunGuardian.exe.bak"" del ""{appDir}\BatRunGuardian.exe.bak""
if exist ""{appDir}\BatRunGuardian.dll.bak"" del ""{appDir}\BatRunGuardian.dll.bak""
start """" ""{Path.Combine(appDir, "BatRun.exe")}""
timeout /t 2 /nobreak >nul
rmdir /s /q ""{tempDir}""
del ""%~f0""
";
                File.WriteAllText(batchPath, updateScript);
                Process.Start(new ProcessStartInfo { FileName = batchPath, UseShellExecute = true, CreateNoWindow = false });
                Environment.Exit(0);
                return true;
            }
            catch (Exception ex)
            {
                updateLogger.LogError($"Error installing update: {ex.Message}", ex);
                return false;
            }
        }
    }
}
