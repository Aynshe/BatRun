using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.NetworkInformation;

namespace BatRun
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
            
            client.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);

            // Clean up old update logs
            CleanupOldUpdateLogs();
        }

        private bool CheckInternetConnection()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send("8.8.8.8", 2000); // Ping Google DNS with 2 second timeout
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
                    var updateLogs = Directory.GetFiles(logDirectory, "BatRun_update*.log");
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
            try
            {
                // First check internet connection
                if (!CheckInternetConnection())
                {
                    logger.LogInfo("No internet connection detected");
                    return new UpdateCheckResult(false, "", "", false);
                }

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
                logger.LogError($"Network error checking for updates: {ex.Message}", ex);
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
                // Split version into parts
                var parts = version.Split('.');
                
                // If we have less than 3 parts, add ".0" for each missing part
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
                // Parse versions into Version objects
                // This will work with X.Y.Z format
                var latest = Version.Parse(latestVersion);
                var current = Version.Parse(currentVersion);

                // Compare versions
                int comparison = latest.CompareTo(current);
                
                // Log version comparison details
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
                // Create temp directory for download
                tempDir = Path.Combine(Path.GetTempPath(), "BatRunUpdate");
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
                Directory.CreateDirectory(tempDir);
                updateLogger.LogInfo($"Created temporary directory: {tempDir}");

                string archivePath = Path.Combine(tempDir, Path.GetFileName(downloadUrl));
                updateLogger.LogInfo($"Downloading update to: {archivePath}");

                // Download the update using HttpClient
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

                // Extract the update to a temporary directory
                string extractPath = Path.Combine(tempDir, "extracted");
                Directory.CreateDirectory(extractPath);
                updateLogger.LogInfo($"Extracting to: {extractPath}");

                // Extraire l'archive
                if (archivePath.EndsWith(".7z"))
                {
                    // Pour les fichiers .7z, utiliser un processus externe (7-Zip)
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
                            updateLogger.LogInfo("7-Zip extraction completed successfully");
                        }
                    }
                }
                else
                {
                    // Pour les fichiers .zip, utiliser ZipFile
                    ZipFile.ExtractToDirectory(archivePath, extractPath);
                    updateLogger.LogInfo("ZIP extraction completed successfully");
                }

                // Trouver le dossier BatRun dans l'extraction
                string batrunSourcePath = Path.Combine(extractPath, "plugins", "BatRun");
                if (!Directory.Exists(batrunSourcePath))
                {
                    updateLogger.LogError("BatRun folder not found in update package");
                    return false;
                }
                updateLogger.LogInfo($"Found BatRun folder at: {batrunSourcePath}");

                // Create update batch script
                string batchPath = Path.Combine(tempDir, "update.bat");
                string appDir = AppContext.BaseDirectory;
                string updateScript = $@"@echo off
echo Waiting for BatRun to close...
:waitloop
tasklist /FI ""IMAGENAME eq BatRun.exe"" 2>NUL | find /I /N ""BatRun.exe"">NUL
if ""%ERRORLEVEL%""==""0"" (
    timeout /t 1 /nobreak >nul
    goto waitloop
)

echo Copying new files...
xcopy ""{batrunSourcePath}\*.*"" ""{appDir}"" /E /I /Y
if errorlevel 1 (
    echo Update failed!
    pause
    exit /b 1
)

echo Starting BatRun...
start """" ""{Path.Combine(appDir, "BatRun.exe")}""
if errorlevel 1 (
    echo Failed to start BatRun!
    pause
    exit /b 1
)

echo Cleaning up...
timeout /t 2 /nobreak >nul
rmdir /s /q ""{tempDir}""
del ""%~f0""
";
                File.WriteAllText(batchPath, updateScript);
                updateLogger.LogInfo("Created update batch script");

                // Launch the update script
                Process.Start(new ProcessStartInfo
                {
                    FileName = batchPath,
                    UseShellExecute = true,
                    CreateNoWindow = false
                });
                updateLogger.LogInfo("Launched update script");

                // Exit the current instance
                Environment.Exit(0);
                return true;
            }
            catch (Exception ex)
            {
                updateLogger.LogError($"Error installing update: {ex.Message}", ex);
                logger.LogError($"Error installing update: {ex.Message}", ex);
                
                // Cleanup on error
                try
                {
                    if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                        updateLogger.LogInfo($"Cleaned up temporary directory after error: {tempDir}");
                    }
                }
                catch (Exception cleanupEx)
                {
                    updateLogger.LogError($"Error cleaning up temporary directory: {cleanupEx.Message}");
                }
                
                return false;
            }
        }
    }
} 