using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using BatRun.Utils;

namespace BatRun.Core
{
    // EN: Holds details of a GitHub Release.
    // FR: Contient les détails d'une Release GitHub.
    public class GitHubRelease
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set; } = "";

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("body")]
        public string Body { get; set; } = "";

        [JsonProperty("assets")]
        public List<GitHubAsset> Assets { get; set; } = new List<GitHubAsset>();
    }

    // EN: Holds details of a GitHub Release Asset.
    // FR: Contient les détails d'un Asset de Release GitHub.
    public class GitHubAsset
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = "";

        [JsonProperty("size")]
        public long Size { get; set; }
    }

    // EN: Database record of an installed plugin.
    // FR: Enregistrement en base de données d'un plugin installé.
    public class InstalledPluginInfo
    {
        public string PluginName { get; set; } = "";
        public string Version { get; set; } = "";
        public List<string> InstalledFiles { get; set; } = new List<string>();
        public bool StartWithRetroBat { get; set; }
    }

    public static class PluginManager
    {
        private static readonly string DbPath = Path.Combine(AppContext.BaseDirectory, "plugins.json");

        // EN: Retrieve installed plugins list.
        // FR: Récupère la liste des plugins installés.
        public static Dictionary<string, InstalledPluginInfo> GetInstalledPlugins(Logger logger)
        {
            try
            {
                if (File.Exists(DbPath))
                {
                    string json = File.ReadAllText(DbPath);
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, InstalledPluginInfo>>(json);
                    return dict ?? new Dictionary<string, InstalledPluginInfo>();
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error loading installed plugins database", ex);
            }
            return new Dictionary<string, InstalledPluginInfo>();
        }

        // EN: Save installed plugins list.
        // FR: Sauvegarde la liste des plugins installés.
        public static void SaveInstalledPlugins(Logger logger, Dictionary<string, InstalledPluginInfo> plugins)
        {
            try
            {
                string json = JsonConvert.SerializeObject(plugins, Formatting.Indented);
                File.WriteAllText(DbPath, json);
            }
            catch (Exception ex)
            {
                logger.LogError("Error saving installed plugins database", ex);
            }
        }

        // EN: Fetch all releases from the GitHub plugins repository.
        // FR: Récupère toutes les releases du dépôt de plugins GitHub.
        public static async Task<List<GitHubRelease>> FetchReleasesAsync(Logger logger)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("BatRun-App");
                logger.LogInfo("Fetching plugins list from GitHub releases API...");
                string json = await client.GetStringAsync("https://api.github.com/repos/Aynshe/BatRun-plugins/releases");
                var releases = JsonConvert.DeserializeObject<List<GitHubRelease>>(json);
                return releases ?? new List<GitHubRelease>();
            }
        }

        // EN: Download plugin archive and return its local temporary path.
        // FR: Télécharge l'archive du plugin et retourne son chemin temporaire local.
        public static async Task<string> DownloadAssetAsync(Logger logger, string url, string fileName, Action<int>? progressReport = null)
        {
            string tempFile = Path.Combine(Path.GetTempPath(), fileName);
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("BatRun-App");
                logger.LogInfo($"Downloading plugin from: {url}");
                using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    long? totalBytes = response.Content.Headers.ContentLength;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        long totalBytesRead = 0;
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;

                            if (totalBytes.HasValue && progressReport != null)
                            {
                                int percentage = (int)((totalBytesRead * 100) / totalBytes.Value);
                                progressReport(percentage);
                            }
                        }
                    }
                }
            }
            return tempFile;
        }

        // EN: Extract ZIP or 7z archive to a temporary directory.
        // FR: Extrait une archive ZIP ou 7z vers un dossier temporaire.
        public static async Task ExtractArchiveAsync(Logger logger, string archivePath, string destinationDir)
        {
            if (Directory.Exists(destinationDir))
            {
                Directory.Delete(destinationDir, true);
            }
            Directory.CreateDirectory(destinationDir);

            if (archivePath.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
            {
                string sevenZipPath = Path.Combine(AppContext.BaseDirectory, "7za.exe");
                if (!File.Exists(sevenZipPath))
                {
                    throw new FileNotFoundException("7za.exe not found in application directory for 7z extraction.");
                }

                logger.LogInfo($"Extracting 7z archive using 7za: {archivePath}");
                var startInfo = new ProcessStartInfo
                {
                    FileName = sevenZipPath,
                    Arguments = $"x \"{archivePath}\" -o\"{destinationDir}\" -y",
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
                            throw new Exception($"7-Zip extraction failed with exit code {process.ExitCode}");
                        }
                    }
                    else
                    {
                        throw new Exception("Failed to start 7-Zip process.");
                    }
                }
            }
            else
            {
                logger.LogInfo($"Extracting Zip archive: {archivePath}");
                ZipFile.ExtractToDirectory(archivePath, destinationDir);
            }
        }

        // EN: Scan temporary folder and perform the installation to RetroBat root.
        // FR: Scanne le dossier temporaire et réalise l'installation vers la racine RetroBat.
        public static List<string> InstallExtractedFiles(Logger logger, string tempExtractDir, string retrobatRoot, string pluginName)
        {
            var installedFiles = new List<string>();

            // EN: Scan for plugins folder inside extracted content
            // FR: Recherche du dossier plugins dans le contenu extrait
            string pluginsSourceDir = Path.Combine(tempExtractDir, "plugins");
            if (Directory.Exists(pluginsSourceDir))
            {
                string specificPluginSource = Path.Combine(pluginsSourceDir, pluginName);
                if (Directory.Exists(specificPluginSource))
                {
                    string targetPluginDir = Path.Combine(retrobatRoot, "plugins", pluginName);
                    Directory.CreateDirectory(targetPluginDir);

                    // Copy files recursively
                    CopyDirectory(specificPluginSource, targetPluginDir, retrobatRoot, installedFiles);
                }
            }

            // EN: Scan for emulationstation folder inside extracted content
            // FR: Recherche du dossier emulationstation dans le contenu extrait
            string esSourceDir = Path.Combine(tempExtractDir, "emulationstation");
            if (Directory.Exists(esSourceDir))
            {
                string targetEsDir = Path.Combine(retrobatRoot, "emulationstation");
                CopyDirectory(esSourceDir, targetEsDir, retrobatRoot, installedFiles);
            }

            // EN: Read path.txt if it exists to register extra runtime files
            // FR: Lire path.txt s'il existe pour enregistrer des fichiers d'exécution supplémentaires
            string pluginDir = Path.Combine(retrobatRoot, "plugins", pluginName);
            string pathTxtFile = Path.Combine(pluginDir, "path.txt");
            if (File.Exists(pathTxtFile))
            {
                try
                {
                    var extraLines = File.ReadAllLines(pathTxtFile);
                    foreach (var line in extraLines)
                    {
                        var trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed))
                        {
                            // EN: Assume path is relative to RetroBat root (e.g. emulationstation/.emulationstation/scripts/...)
                            // FR: Suppose que le chemin est relatif à la racine RetroBat
                            string relPathClean = trimmed.Replace('/', '\\');
                            while (relPathClean.StartsWith("\\"))
                            {
                                relPathClean = relPathClean.Substring(1);
                            }

                            string resolvedPath = Path.Combine(retrobatRoot, relPathClean);
                            string relPath = GetRelativePath(retrobatRoot, resolvedPath);
                            if (!installedFiles.Contains(relPath))
                            {
                                installedFiles.Add(relPath);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError("Error reading path.txt for extra files list", ex);
                }
            }

            return installedFiles;
        }

        // EN: Recursively copy directories and track relative files.
        // FR: Copie récursivement les dossiers et suit les fichiers relatifs.
        private static void CopyDirectory(string sourceDir, string destDir, string rootDir, List<string> installedFiles)
        {
            Directory.CreateDirectory(destDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string targetFilePath = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, targetFilePath, true);

                string relativePath = GetRelativePath(rootDir, targetFilePath);
                if (!installedFiles.Contains(relativePath))
                {
                    installedFiles.Add(relativePath);
                }
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string targetSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, targetSubDir, rootDir, installedFiles);
            }
        }

        // EN: Helper to get relative path of a file from a root directory.
        // FR: Helper pour obtenir le chemin relatif d'un fichier à partir d'un dossier racine.
        public static string GetRelativePath(string rootPath, string fullPath)
        {
            if (!rootPath.EndsWith("\\")) rootPath += "\\";
            if (fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                return fullPath.Substring(rootPath.Length);
            }
            return fullPath;
        }

        // EN: Check if the plugin executable is running.
        // FR: Vérifie si l'exécutable du plugin est en cours d'exécution.
        public static bool IsPluginRunning(string pluginName)
        {
            try
            {
                var processes = Process.GetProcessesByName(pluginName);
                return processes.Length > 0 && !processes[0].HasExited;
            }
            catch
            {
                return false;
            }
        }

        // EN: Automatically terminate the plugin processes.
        // FR: Arrête automatiquement les processus du plugin.
        public static void TerminatePluginProcess(string pluginName)
        {
            try
            {
                var processes = Process.GetProcessesByName(pluginName);
                foreach (var process in processes)
                {
                    process.Kill(true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error terminating process {pluginName}: {ex.Message}");
            }
        }

        // EN: Handle startup scripts for RetroBat.
        // FR: Gère les scripts de démarrage de RetroBat.
        public static void UpdateRetroBatStartScript(Logger logger, string retrobatRoot, string pluginName, bool enable)
        {
            string startScriptsDir = Path.Combine(retrobatRoot, "emulationstation", ".emulationstation", "scripts", "start");
            string scriptPath = Path.Combine(startScriptsDir, $"{pluginName}.bat");

            if (enable)
            {
                try
                {
                    Directory.CreateDirectory(startScriptsDir);
                    string pluginExePath = Path.Combine(retrobatRoot, "plugins", pluginName, $"{pluginName}.exe");
                    string scriptContent = $@"@echo off
""{pluginExePath}""
exit /b";
                    File.WriteAllText(scriptPath, scriptContent);
                    logger.LogInfo($"Created RetroBat start script for {pluginName} at {scriptPath}");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error creating RetroBat start script for {pluginName}", ex);
                }
            }
            else
            {
                try
                {
                    if (File.Exists(scriptPath))
                    {
                        File.Delete(scriptPath);
                        logger.LogInfo($"Deleted RetroBat start script for {pluginName}");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error deleting RetroBat start script for {pluginName}", ex);
                }
            }
        }

        // EN: Launches plugins configured to start with BatRun.
        // FR: Lance les plugins configurés pour démarrer avec BatRun.
        public static void LaunchBatRunPlugins(Logger logger, IniFile config, RetroBatService retroBatService)
        {
            string exePath = retroBatService.GetRetrobatPath();
            string retrobatRoot = Path.GetDirectoryName(exePath) ?? "";
            if (string.IsNullOrEmpty(retrobatRoot)) return;

            var installed = GetInstalledPlugins(logger);
            foreach (var plugin in installed.Values)
            {
                bool startWithBatRun = config.ReadBool("Plugins", "StartWithBatRun_" + plugin.PluginName, false);
                if (startWithBatRun)
                {
                    string pluginExe = Path.Combine(retrobatRoot, "plugins", plugin.PluginName, $"{plugin.PluginName}.exe");
                    if (File.Exists(pluginExe))
                    {
                        try
                        {
                            logger.LogInfo($"Launching plugin with BatRun: {pluginExe}");
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = pluginExe,
                                WorkingDirectory = Path.GetDirectoryName(pluginExe) ?? "",
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"Failed to launch plugin {plugin.PluginName} with BatRun", ex);
                        }
                    }
                }
            }
        }
    }
}
