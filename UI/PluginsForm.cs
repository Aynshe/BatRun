using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.UI
{
    // EN: Form displaying various RetroBat tools and plugins from GitHub.
    // FR: Formulaire affichant divers outils et plugins RetroBat depuis GitHub.
    public partial class PluginsForm : Form
    {
        private readonly IBatRunProgram? _program;
        private readonly Logger? _logger;
        private readonly IniFile? _config;

        public PluginsForm()
        {
            InitializeComponent();
            LoadIcon();
            PopulateProjects();
        }

        public PluginsForm(IBatRunProgram program, Logger logger, IniFile config)
        {
            _program = program;
            _logger = logger;
            _config = config;
            InitializeComponent();
            LoadIcon();
            PopulateProjects();
            LoadGitHubPlugins();
        }

        private void LoadIcon()
        {
            try
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
            }
            catch { }
        }

        // EN: Populates the TableLayoutPanel with project rows.
        // FR: Remplit le TableLayoutPanel avec les lignes de projet.
        private void PopulateProjects()
        {
            var mainControls = new List<Control>();
            var expControls = new List<Control>();

            var lblMainHeader = CreateHeaderLabel("MAIN PROJECTS", mainControls, true);
            mainLayout.Controls.Add(lblMainHeader);
            
            mainControls.Add(AddProjectRow(mainLayout, "WiimoteGun (Fork for rawinput lightgun)", 
                "https://github.com/Aynshe/WiimoteGun/tree/3-removal-driver-interception-dependency",
                "https://github.com/Aynshe/WiimoteGun/releases/latest"));

            mainControls.Add(AddProjectRow(mainLayout, "RetroBat Marquee Manager", 
                "https://github.com/Aynshe/RetroBatMarqueeManager",
                "https://github.com/Aynshe/RetroBatMarqueeManager/releases/latest"));

            mainControls.Add(AddProjectRow(mainLayout, "XOrderHook (swap index xinput)", 
                "https://github.com/Aynshe/XOrderHook",
                "https://github.com/Aynshe/XOrderHook/releases/latest"));

            mainControls.Add(AddProjectRow(mainLayout, "GSLM (Game Store Library Manager)", 
                "https://github.com/Aynshe/GSLM",
                "https://github.com/Aynshe/GSLM/releases/latest"));

            // Spacer
            var mainSpacer = new Label { Height = 20, Dock = DockStyle.Top };
            mainLayout.Controls.Add(mainSpacer);
            mainControls.Add(mainSpacer);

            // Experimental Section
            var lblExpHeader = CreateHeaderLabel("EXPERIMENTAL (QUICK-RESUME DEMOS)", expControls, true);
            mainLayout.Controls.Add(lblExpHeader);

            expControls.Add(AddProjectRow(mainLayout, "EmulatorLauncher (Demo QR)", 
                "https://github.com/Aynshe/emulatorlauncher/tree/emulatorlauncher_quickresume_demo",
                "https://github.com/Aynshe/emulatorlauncher/releases"));

            expControls.Add(AddProjectRow(mainLayout, "SuspendedNTime (Demo QR)", 
                "https://github.com/Aynshe/SuspendedNTime/tree/SuspendedNTime-quickresume_RetroBat-Demo",
                "https://github.com/Aynshe/SuspendedNTime/releases"));

            // Spacer
            var expSpacer = new Label { Height = 20, Dock = DockStyle.Top };
            mainLayout.Controls.Add(expSpacer);
            expControls.Add(expSpacer);

            // Apply initial collapse state (collapsed by default)
            CollapseSection(lblMainHeader, "MAIN PROJECTS", mainControls, true);
            CollapseSection(lblExpHeader, "EXPERIMENTAL (QUICK-RESUME DEMOS)", expControls, true);
        }

        private Label CreateHeaderLabel(string text, List<Control> targetControls, bool startCollapsed)
        {
            var lbl = new Label
            {
                Text      = (startCollapsed ? "▶ " : "▼ ") + text,
                Font      = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 204),
                AutoSize  = true,
                Margin    = new Padding(0, 0, 0, 8),
                Cursor    = Cursors.Hand
            };

            lbl.Click += (s, e) =>
            {
                bool isCurrentlyCollapsed = lbl.Text.StartsWith("▶");
                CollapseSection(lbl, text, targetControls, !isCurrentlyCollapsed);
            };

            return lbl;
        }

        private Label CreateHeaderLabel(string text)
        {
            return new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 204),
                AutoSize  = true,
                Margin    = new Padding(0, 0, 0, 8)
            };
        }

        private void CollapseSection(Label headerLabel, string baseText, List<Control> targetControls, bool collapse)
        {
            headerLabel.Text = (collapse ? "▶ " : "▼ ") + baseText;
            foreach (var ctrl in targetControls)
            {
                ctrl.Visible = !collapse;
            }
            mainLayout.PerformLayout();
        }

        private Panel AddProjectRow(TableLayoutPanel panel, string name, string githubUrl, string releaseUrl)
        {
            var rowPanel = new Panel 
            { 
                Dock      = DockStyle.Top, 
                Height    = 65, 
                Margin    = new Padding(0, 0, 0, 8), 
                BackColor = Color.FromArgb(45, 45, 48) 
            };
            
            var lblName = new Label 
            { 
                Text      = name, 
                Location  = new Point(12, 8), 
                AutoSize  = true, 
                Font      = new Font("Segoe UI", 9.5F, FontStyle.Bold) 
            };
            
            var btnGithub = new Button 
            { 
                Text      = "🌐 GitHub PROJECT", 
                Location  = new Point(12, 32), 
                Size      = new Size(160, 26), 
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.FromArgb(70, 70, 70),
                Cursor    = Cursors.Hand,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 8.5F)
            };
            btnGithub.FlatAppearance.BorderSize = 0;
            btnGithub.Click += (s, e) => OpenUrl(githubUrl);

            var btnRelease = new Button 
            { 
                Text      = "📦 LATEST RELEASES", 
                Location  = new Point(185, 32), 
                Size      = new Size(160, 26), 
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.FromArgb(0, 122, 204),
                Cursor    = Cursors.Hand,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 8.5F)
            };
            btnRelease.FlatAppearance.BorderSize = 0;
            btnRelease.Click += (s, e) => OpenUrl(releaseUrl);

            rowPanel.Controls.Add(lblName);
            rowPanel.Controls.Add(btnGithub);
            rowPanel.Controls.Add(btnRelease);
            
            panel.Controls.Add(rowPanel);
            return rowPanel;
        }

        private async void LoadGitHubPlugins()
        {
            if (_logger == null) return;

            var lblLoading = new Label
            {
                Text      = "Loading plugins from GitHub...",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 9.5F, FontStyle.Italic),
                AutoSize  = true,
                Margin    = new Padding(0, 10, 0, 10)
            };
            mainLayout.Controls.Add(lblLoading);

            try
            {
                var releases = await PluginManager.FetchReleasesAsync(_logger);
                mainLayout.Controls.Remove(lblLoading);

                if (releases == null || releases.Count == 0)
                {
                    mainLayout.Controls.Add(new Label
                    {
                        Text      = "No plugins found on GitHub.",
                        ForeColor = Color.LightGray,
                        AutoSize  = true
                    });
                    return;
                }

                mainLayout.Controls.Add(CreateHeaderLabel("GITHUB PLUGINS (Aynshe/BatRun-plugins)"));

                RefreshPluginList(releases);
            }
            catch (Exception ex)
            {
                mainLayout.Controls.Remove(lblLoading);
                _logger.LogError("Failed to load GitHub plugins", ex);
                mainLayout.Controls.Add(new Label
                {
                    Text      = "Error loading plugins: " + ex.Message,
                    ForeColor = Color.Red,
                    AutoSize  = true
                });
            }
        }

        private static Version ParseVersionFromTag(string tagName)
        {
            int vIndex = tagName.LastIndexOf("-v");
            if (vIndex >= 0 && vIndex + 2 < tagName.Length)
            {
                string versionStr = tagName.Substring(vIndex + 2);
                if (Version.TryParse(versionStr, out Version? ver))
                {
                    return ver;
                }
            }
            return new Version(0, 0, 0);
        }

        private void RefreshPluginList(List<GitHubRelease> releases)
        {
            if (_logger == null) return;

            // EN: Get list of already installed plugins
            // FR: Récupère la liste des plugins déjà installés
            var installed = PluginManager.GetInstalledPlugins(_logger);

            // EN: Remove previous github controls if refreshing
            // FR: Supprime les anciens contrôles github lors du rafraîchissement
            var toRemove = mainLayout.Controls.OfType<Panel>().Where(p => p.Tag != null && p.Tag.ToString() == "github_plugin").ToList();
            foreach (var ctrl in toRemove)
            {
                mainLayout.Controls.Remove(ctrl);
            }

            // EN: Group releases by plugin name and only keep the latest version for each plugin
            // FR: Groupe les releases par nom de plugin et ne garde que la version la plus récente
            var latestReleases = releases
                .Select(r => {
                    string name = r.TagName;
                    int dash = name.IndexOf('-');
                    string pluginName = dash > 0 ? name.Substring(0, dash) : name;
                    return new { PluginName = pluginName, Release = r, Version = ParseVersionFromTag(r.TagName) };
                })
                .Where(x => x.Release.Assets.Any(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || a.Name.EndsWith(".7z", StringComparison.OrdinalIgnoreCase)))
                .GroupBy(x => x.PluginName)
                .Select(g => g.OrderByDescending(x => x.Version).First().Release)
                .ToList();

            foreach (var release in latestReleases)
            {
                // EN: Clean plugin name, e.g. "RetroBatGameMode-v1.0.0" -> "RetroBatGameMode"
                // FR: Nettoyer le nom du plugin
                string pluginName = release.TagName;
                int dashIndex = pluginName.IndexOf('-');
                if (dashIndex > 0)
                {
                    pluginName = pluginName.Substring(0, dashIndex);
                }

                var asset = release.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || a.Name.EndsWith(".7z", StringComparison.OrdinalIgnoreCase));
                if (asset == null) continue;

                var isInstalled = installed.TryGetValue(pluginName, out var installedInfo);
                
                var installedVer = isInstalled ? ParseVersionFromTag(installedInfo!.Version) : new Version(0, 0, 0);
                var latestVer = ParseVersionFromTag(release.TagName);
                var isUpdateAvailable = isInstalled && latestVer > installedVer;

                var rowPanel = new Panel
                {
                    Dock      = DockStyle.Top,
                    Height    = 130,
                    Margin    = new Padding(0, 0, 0, 8),
                    BackColor = Color.FromArgb(45, 45, 48),
                    Tag       = "github_plugin"
                };

                var lblName = new Label
                {
                    Text      = $"{pluginName} ({(isInstalled ? installedInfo!.Version : release.TagName)})",
                    Location  = new Point(12, 8),
                    AutoSize  = true,
                    Font      = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.White
                };

                var btnInfo = new Button
                {
                    Text      = "💬 INFO",
                    Location  = new Point(12, 36),
                    Size      = new Size(80, 26),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(70, 70, 70),
                    Cursor    = Cursors.Hand,
                    ForeColor = Color.White,
                    Font      = new Font("Segoe UI", 8.5F)
                };
                btnInfo.FlatAppearance.BorderSize = 0;
                btnInfo.Click += (s, e) =>
                {
                    MessageBox.Show(release.Body, $"{pluginName} - Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };

                var btnAction = new Button
                {
                    Location  = new Point(100, 36),
                    Size      = new Size(110, 26),
                    FlatStyle = FlatStyle.Flat,
                    Cursor    = Cursors.Hand,
                    ForeColor = Color.White,
                    Font      = new Font("Segoe UI", 8.5F)
                };
                btnAction.FlatAppearance.BorderSize = 0;

                var btnUninstall = new Button
                {
                    Text      = "❌ UNINSTALL",
                    Location  = new Point(220, 36),
                    Size      = new Size(100, 26),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(180, 40, 40),
                    Cursor    = Cursors.Hand,
                    ForeColor = Color.White,
                    Font      = new Font("Segoe UI", 8.5F),
                    Visible   = isInstalled
                };
                btnUninstall.FlatAppearance.BorderSize = 0;

                var btnReveal = new Button
                {
                    Text      = "📁 REVEAL",
                    Location  = new Point(330, 36),
                    Size      = new Size(90, 26),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(70, 70, 70),
                    Cursor    = Cursors.Hand,
                    ForeColor = Color.White,
                    Font      = new Font("Segoe UI", 8.5F),
                    Visible   = isInstalled
                };
                btnReveal.FlatAppearance.BorderSize = 0;
                btnReveal.Click += (s, e) =>
                {
                    try
                    {
                        string pluginDir = Path.Combine(GetRetrobatRoot(), "plugins", pluginName);
                        if (Directory.Exists(pluginDir))
                        {
                            Process.Start("explorer.exe", $"\"{pluginDir}\"");
                        }
                        else
                        {
                            string root = GetRetrobatRoot();
                            if (Directory.Exists(root))
                            {
                                Process.Start("explorer.exe", $"\"{root}\"");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Unable to open explorer: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                // EN: Detect configuration files (.ini) in the plugin folder
                // FR: Détecte les fichiers de configuration (.ini) dans le dossier du plugin
                var iniFiles = new List<string>();
                if (isInstalled)
                {
                    string pluginDir = Path.Combine(GetRetrobatRoot(), "plugins", pluginName);
                    if (Directory.Exists(pluginDir))
                    {
                        try
                        {
                            iniFiles.AddRange(Directory.GetFiles(pluginDir, "*.ini", SearchOption.AllDirectories));
                        }
                        catch { }
                    }
                }

                var btnConfig = new Button
                {
                    Text      = "⚙️ CONFIG",
                    Location  = new Point(430, 36),
                    Size      = new Size(90, 26),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(70, 70, 70),
                    Cursor    = Cursors.Hand,
                    ForeColor = Color.White,
                    Font      = new Font("Segoe UI", 8.5F),
                    Visible   = isInstalled,
                    Enabled   = isInstalled
                };
                btnConfig.FlatAppearance.BorderSize = 0;

                btnConfig.Click += async (s, e) =>
                {
                    string pluginDir = Path.Combine(GetRetrobatRoot(), "plugins", pluginName);
                    var currentIniFiles = new List<string>();
                    if (Directory.Exists(pluginDir))
                    {
                        try
                        {
                            currentIniFiles.AddRange(Directory.GetFiles(pluginDir, "*.ini", SearchOption.AllDirectories));
                        }
                        catch { }
                    }

                    if (currentIniFiles.Count == 0)
                    {
                        string exePath = Path.Combine(pluginDir, $"{pluginName}.exe");
                        if (!File.Exists(exePath))
                        {
                            MessageBox.Show("Configuration file (.ini) not found, and plugin executable could not be found to generate it.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        btnConfig.Enabled = false;
                        btnConfig.Text = "Running...";

                        try
                        {
                            var startInfo = new ProcessStartInfo
                            {
                                FileName = exePath,
                                WorkingDirectory = pluginDir,
                                UseShellExecute = true
                            };

                            using (var proc = Process.Start(startInfo))
                            {
                                if (proc != null)
                                {
                                    int elapsed = 0;
                                    while (elapsed < 8000)
                                    {
                                        await Task.Delay(250);
                                        elapsed += 250;

                                        if (Directory.Exists(pluginDir))
                                        {
                                            var detected = Directory.GetFiles(pluginDir, "*.ini", SearchOption.AllDirectories);
                                            if (detected.Length > 0)
                                            {
                                                currentIniFiles.AddRange(detected);
                                                break;
                                            }
                                        }

                                        if (proc.HasExited)
                                        {
                                            break;
                                        }
                                    }

                                    try
                                    {
                                        if (!proc.HasExited)
                                        {
                                            proc.Kill(true);
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError($"Error launching plugin {pluginName} for configuration generation", ex);
                        }
                        finally
                        {
                            btnConfig.Enabled = true;
                            btnConfig.Text = "⚙️ CONFIG";
                        }
                    }

                    if (currentIniFiles.Count > 0)
                    {
                        using (var cfgForm = new PluginConfigForm(pluginName, currentIniFiles))
                        {
                            cfgForm.ShowDialog(this);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Failed to detect or generate configuration files (.ini) for this plugin.", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };

                var chkStartWithBatRun = new CheckBox
                {
                    Text      = "Start with BatRun",
                    Location  = new Point(12, 75),
                    ForeColor = Color.White,
                    AutoSize  = true,
                    Font      = new Font("Segoe UI", 8.5F),
                    Visible   = isInstalled,
                    Checked   = _config != null && _config.ReadBool("Plugins", "StartWithBatRun_" + pluginName, false)
                };

                var chkStartWithRetroBat = new CheckBox
                {
                    Text      = "Start with RetroBat",
                    Location  = new Point(200, 75),
                    ForeColor = Color.White,
                    AutoSize  = true,
                    Font      = new Font("Segoe UI", 8.5F),
                    Visible   = isInstalled,
                    Checked   = isInstalled && installedInfo!.StartWithRetroBat
                };

                // EN: Check if the plugin manages its own startup (presence of no_start.txt)
                // FR: Vérifie si le plugin gère son propre démarrage (présence de no_start.txt)
                bool hasNoStartFile = false;
                if (isInstalled)
                {
                    string pluginDir = Path.Combine(GetRetrobatRoot(), "plugins", pluginName);
                    if (File.Exists(Path.Combine(pluginDir, "no_start.txt")))
                    {
                        hasNoStartFile = true;
                    }
                }

                if (isInstalled && hasNoStartFile)
                {
                    chkStartWithBatRun.Visible = false;
                    chkStartWithRetroBat.Visible = false;
                }

                chkStartWithBatRun.CheckedChanged += (s, e) =>
                {
                    if (_config != null)
                    {
                        _config.WriteValue("Plugins", "StartWithBatRun_" + pluginName, chkStartWithBatRun.Checked ? "true" : "false");
                    }
                    if (chkStartWithBatRun.Checked)
                    {
                        MessageBox.Show(
                            "NNote: Some plugins have their own settings for automatic startup upon first execution. If the plugin offers this feature, please modify its configuration file to enable it. If the file is missing, run the plugin for the first time to generate the config.ini file.",
                            "Startup Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                };

                chkStartWithRetroBat.CheckedChanged += (s, e) =>
                {
                    if (isInstalled && installedInfo != null)
                    {
                        installedInfo.StartWithRetroBat = chkStartWithRetroBat.Checked;
                        PluginManager.UpdateRetroBatStartScript(_logger, GetRetrobatRoot(), pluginName, chkStartWithRetroBat.Checked);
                        PluginManager.SaveInstalledPlugins(_logger, installed);
                    }
                    if (chkStartWithRetroBat.Checked)
                    {
                        MessageBox.Show(
                           "Note: Some plugins have their own settings for automatic startup upon first execution. If the plugin offers this feature, please modify its configuration file to enable it. If the file is missing, run the plugin for the first time to generate the config.ini file.",
                            "Startup Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                };

                if (!isInstalled)
                {
                    btnAction.Text = "📥 INSTALL";
                    btnAction.BackColor = Color.FromArgb(0, 122, 204);
                    btnAction.Click += async (s, e) =>
                    {
                        btnAction.Enabled = false;
                        await InstallPluginWorkflow(release, pluginName, asset, btnAction);
                        RefreshPluginList(releases);
                    };
                }
                else if (isUpdateAvailable)
                {
                    btnAction.Text = "🔄 UPDATE";
                    btnAction.BackColor = Color.FromArgb(204, 122, 0);
                    btnAction.Click += async (s, e) =>
                    {
                        btnAction.Enabled = false;
                        await InstallPluginWorkflow(release, pluginName, asset, btnAction);
                        RefreshPluginList(releases);
                    };
                }
                else
                {
                    btnAction.Text = "✅ INSTALLED";
                    btnAction.BackColor = Color.FromArgb(40, 140, 40);
                    btnAction.Enabled = false;
                }

                btnUninstall.Click += (s, e) =>
                {
                    if (UninstallPluginWorkflow(pluginName))
                    {
                        RefreshPluginList(releases);
                    }
                };

                rowPanel.Controls.Add(lblName);
                rowPanel.Controls.Add(btnInfo);
                rowPanel.Controls.Add(btnAction);
                rowPanel.Controls.Add(btnUninstall);
                rowPanel.Controls.Add(btnReveal);
                rowPanel.Controls.Add(btnConfig);
                rowPanel.Controls.Add(chkStartWithBatRun);
                rowPanel.Controls.Add(chkStartWithRetroBat);

                mainLayout.Controls.Add(rowPanel);
            }
        }

        private async Task InstallPluginWorkflow(GitHubRelease release, string pluginName, GitHubAsset asset, Button btnAction)
        {
            if (_logger == null) return;
            string retrobatRoot = GetRetrobatRoot();

            try
            {
                string archiveFile = await PluginManager.DownloadAssetAsync(_logger, asset.BrowserDownloadUrl, asset.Name, percent =>
                {
                    this.Invoke((Action)(() =>
                    {
                        btnAction.Text = $"Downloading {percent}%";
                    }));
                });

                this.Invoke((Action)(() =>
                {
                    btnAction.Text = "Extracting...";
                }));

                string tempDir = Path.Combine(Path.GetTempPath(), "BatRun_Plugin_" + pluginName);
                await PluginManager.ExtractArchiveAsync(_logger, archiveFile, tempDir);

                // Copy files
                var installedFiles = PluginManager.InstallExtractedFiles(_logger, tempDir, retrobatRoot, pluginName);

                // Update catalog
                var installed = PluginManager.GetInstalledPlugins(_logger);
                installed[pluginName] = new InstalledPluginInfo
                {
                    PluginName = pluginName,
                    Version = release.TagName,
                    InstalledFiles = installedFiles,
                    StartWithRetroBat = false
                };
                PluginManager.SaveInstalledPlugins(_logger, installed);

                try { File.Delete(archiveFile); } catch { }
                try { Directory.Delete(tempDir, true); } catch { }

                MessageBox.Show($"{pluginName} installed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to install plugin {pluginName}", ex);
                MessageBox.Show($"Failed to install {pluginName}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Invoke((Action)(() => { btnAction.Enabled = true; }));
            }
        }

        private bool UninstallPluginWorkflow(string pluginName)
        {
            if (_logger == null) return false;
            string retrobatRoot = GetRetrobatRoot();

            // Safety Process check loop
            while (PluginManager.IsPluginRunning(pluginName))
            {
                var result = MessageBox.Show(
                    "The plugin executable is currently running. Would you like to terminate the task automatically, or close it manually before retrying?",
                    "Plugin Executable Running",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    PluginManager.TerminatePluginProcess(pluginName);
                    Thread.Sleep(800); // Wait for termination
                }
                else if (result == DialogResult.No)
                {
                    Thread.Sleep(1000); // Check again in 1s
                }
                else
                {
                    return false; // User cancelled
                }
            }

            try
            {
                var installed = PluginManager.GetInstalledPlugins(_logger);
                if (installed.TryGetValue(pluginName, out var pluginInfo))
                {
                    // Clean start script if any
                    PluginManager.UpdateRetroBatStartScript(_logger, retrobatRoot, pluginName, false);

                    // Delete installed files
                    foreach (var file in pluginInfo.InstalledFiles)
                    {
                        string cleanFile = file;
                        while (cleanFile.StartsWith("\\") || cleanFile.StartsWith("/"))
                        {
                            cleanFile = cleanFile.Substring(1);
                        }

                        string fullPath = Path.Combine(retrobatRoot, cleanFile);
                        if (File.Exists(fullPath))
                        {
                            try
                            {
                                File.Delete(fullPath);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"Failed to delete file {fullPath} during uninstall", ex);
                            }
                        }
                    }

                    // Delete plugin folder if empty
                    string pluginDir = Path.Combine(retrobatRoot, "plugins", pluginName);
                    if (Directory.Exists(pluginDir) && !Directory.EnumerateFileSystemEntries(pluginDir).Any())
                    {
                        try { Directory.Delete(pluginDir, true); } catch { }
                    }

                    // Delete config ini entry if any
                    if (_config != null)
                    {
                        _config.WriteValue("Plugins", "StartWithBatRun_" + pluginName, "false");
                    }

                    installed.Remove(pluginName);
                    PluginManager.SaveInstalledPlugins(_logger, installed);

                    MessageBox.Show($"{pluginName} uninstalled successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during plugin {pluginName} uninstallation", ex);
                MessageBox.Show($"Failed to uninstall {pluginName}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return false;
        }

        private string GetRetrobatRoot()
        {
            if (_program != null)
            {
                string exePath = _program.GetRetrobatPath();
                if (!string.IsNullOrEmpty(exePath))
                {
                    return Path.GetDirectoryName(exePath) ?? "";
                }
            }
            return @"C:\Retrobat";
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open link: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClose_Click(object? sender, EventArgs e)
        {
            this.Close();
        }
    }
}
