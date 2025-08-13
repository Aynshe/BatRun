using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

namespace BatRun
{
    public class WallpaperManager : IDisposable
    {
        private readonly IniFile config;
        private readonly Logger logger;
        private readonly IBatRunProgram program;
        private readonly Random random = new Random();
        private Form? wallpaperForm;
        private Button? overlayButton;
        private LibVLC? libVLC;
        private VideoView? videoView;
        private Media? media;
        private MediaPlayer? mediaPlayer;
        private PictureBox? pictureBox;
        private bool isClosing = false;
        private bool isDisposed = false;
        private bool isDragging = false;
        private Point dragStartPoint;
        private int snapDistance = 20; // Distance d'aimantation en pixels

        // Ajouter un gestionnaire d'instances statique
        private static readonly List<WallpaperManager> activeInstances = new List<WallpaperManager>();
        private static readonly object instanceLock = new object();

        // Ajouter un gestionnaire d'instance active statique pour le MediaPlayer
        private static MediaPlayer? activeMediaPlayer;
        private static readonly object mediaPlayerLock = new object();

        // Liste des extensions supportées
        public static readonly string[] SupportedExtensions = new[]
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".bmp",
            ".gif",
            ".mp4",
        };

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        private ApplicationManager? applicationManager;

        private System.Windows.Forms.Timer? emulationStationMonitorTimer;

        private int consecutiveChecksWithoutEmulationStation = 0;
        private const int REQUIRED_CHECKS_BEFORE_RESUME = 5; // Attendre 5 vérifications successives

        private bool isMediaPaused = false;
        private readonly object mediaPauseLock = new object();

        private bool isGifPaused = false;
        private Image? originalGifImage;

        private bool isPauseEnabled = true;
        private bool isBlackBackgroundEnabled = true;

        private Form? blackBackground;

        public WallpaperManager(IniFile config, Logger logger, IBatRunProgram program)
        {
            this.config = config;
            this.logger = logger;
            this.program = program;
            this.applicationManager = new ApplicationManager(config, logger);

            lock (instanceLock)
            {
                activeInstances.Add(this);
            }
        }

        public void Initialize()
        {
            try
            {
                // Initialiser LibVLC avec le chemin vers les DLLs
                string libVLCPath = Path.Combine(AppContext.BaseDirectory, "libvlc");
                if (!Directory.Exists(libVLCPath))
                {
                    logger.LogError($"LibVLC directory not found at: {libVLCPath}");
                    return;
                }

                // Vérifier la présence des DLLs essentielles
                string[] requiredDlls = { "libvlc.dll", "libvlccore.dll" };
                foreach (var dll in requiredDlls)
                {
                    string dllPath = Path.Combine(libVLCPath, dll);
                    if (!File.Exists(dllPath))
                    {
                        logger.LogError($"Required LibVLC DLL not found: {dllPath}");
                        return;
                    }
                }

                Core.Initialize(libVLCPath);
                libVLC = new LibVLC(
                    "--quiet",
                    "--no-video-title-show",
                    "--no-snapshot-preview",
                    "--no-stats",
                    "--no-sub-autodetect-file",
                    "--no-osd",
                    "--no-video-deco"
                );
                logger.LogInfo($"LibVLC instance created successfully from: {libVLCPath}");

                // Initialize the EmulationStation monitor timer
                emulationStationMonitorTimer = new System.Windows.Forms.Timer();
                emulationStationMonitorTimer.Interval = 2000; // Check every 2 seconds
                emulationStationMonitorTimer.Tick += (s, e) => CheckEmulationStationStatus();
                emulationStationMonitorTimer.Start();

                // Créer le fond noir
                blackBackground = new Form
                {
                    BackColor = Color.Black,
                    FormBorderStyle = FormBorderStyle.None,
                    ShowInTaskbar = false,
                    TopMost = true,
                    WindowState = FormWindowState.Maximized
                };
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to initialize WallpaperManager: {ex.Message}", ex);
            }
        }

        private Screen GetTargetScreen()
        {
            try
            {
                string retroBatConfigPath = Path.Combine(AppContext.BaseDirectory, "retrobat.ini");
                if (File.Exists(retroBatConfigPath))
                {
                    var retroBatConfig = new IniFile(retroBatConfigPath);
                    int monitorIndex = retroBatConfig.ReadInt("RetroBat", "MonitorIndex", 0);
                    
                    if (monitorIndex >= 0 && monitorIndex < Screen.AllScreens.Length)
                    {
                        return Screen.AllScreens[monitorIndex];
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error reading monitor index: {ex.Message}", ex);
            }

            return Screen.PrimaryScreen ?? Screen.AllScreens[0];
        }

        public void ShowWallpaper(bool forceShow = false)
        {
            try
            {
                bool enableWithExplorer = config.ReadBool("Wallpaper", "EnableWithExplorer", false);
                bool shouldShow = !IsExplorerRunning() || forceShow || enableWithExplorer;

                logger.LogInfo($"ShowWallpaper called - Explorer running: {IsExplorerRunning()}, Force show: {forceShow}, Enable with Explorer: {enableWithExplorer}, Should show: {shouldShow}");

                if (shouldShow)
                {
                    CloseWallpaper();

                    string wallpaperPath;
                    string selectedWallpaper = config.ReadValue("Wallpaper", "Selected", "None");

                    if (selectedWallpaper == "None" || selectedWallpaper == "Aucun")
                    {
                        logger.LogInfo("No wallpaper selected ('None'). Skipping.");
                        return;
                    }

                    bool useRandom = selectedWallpaper == "Random Wallpaper" || 
                                   selectedWallpaper == LocalizedStrings.GetString("Random Wallpaper") ||
                                   selectedWallpaper == "Fond d'écran aléatoire";

                    if (useRandom)
                    {
                        wallpaperPath = GetRandomWallpaperPath();
                        if (string.IsNullOrEmpty(wallpaperPath))
                        {
                            logger.LogError("No wallpaper files found for random selection");
                            return;
                        }
                    }
                    else if (selectedWallpaper != "None")
                    {
                        if (selectedWallpaper.StartsWith("ES Videos"))
                        {
                            string esVideoPath = GetEmulationStationVideoPath();
                            if (!string.IsNullOrEmpty(esVideoPath))
                            {
                                wallpaperPath = Path.Combine(esVideoPath, Path.GetFileName(selectedWallpaper.Replace("ES Videos\\", "")));
                            }
                            else
                            {
                                logger.LogError("EmulationStation video path not found");
                                return;
                            }
                        }
                        else
                        {
                            wallpaperPath = Path.Combine(AppContext.BaseDirectory, "Wallpapers", selectedWallpaper);
                        }
                    }
                    else
                    {
                        return;
                    }

                    if (string.IsNullOrEmpty(wallpaperPath) || !File.Exists(wallpaperPath))
                    {
                        logger.LogError($"Wallpaper file not found: {wallpaperPath}");
                        return;
                    }

                    wallpaperForm = new Form
                    {
                        FormBorderStyle = FormBorderStyle.None,
                        ShowInTaskbar = false,
                        TopMost = false,
                        BackColor = Color.Black
                    };

                    string extension = Path.GetExtension(wallpaperPath).ToLower();
                    bool isVideo = extension == ".mp4";
                    bool isAnimatedGif = extension == ".gif";

                    if (isVideo)
                    {
                        InitializeVideoPlayer(wallpaperPath);
                    }
                    else if (isAnimatedGif)
                    {
                        // Créer un PictureBox pour le GIF animé
                        pictureBox = new PictureBox
                        {
                            Dock = DockStyle.None,
                            SizeMode = PictureBoxSizeMode.AutoSize,
                            BackColor = Color.Black
                        };
                        
                        // Charger le GIF animé
                        pictureBox.Image = Image.FromFile(wallpaperPath);

                        // Démarrer l'animation du GIF
                        AttachGifAnimation();

                        // Calculer la position pour centrer le GIF
                        Screen currentScreen = GetTargetScreen();
                        int x = (currentScreen.Bounds.Width - pictureBox.Image.Width) / 2;
                        int y = (currentScreen.Bounds.Height - pictureBox.Image.Height) / 2;
                        pictureBox.Location = new Point(x, y);

                        wallpaperForm.Controls.Add(pictureBox);
                        pictureBox.SendToBack();
                    }
                    else
                    {
                        wallpaperForm.BackgroundImageLayout = ImageLayout.Zoom;
                        using (var img = Image.FromFile(wallpaperPath))
                        {
                            wallpaperForm.BackgroundImage = new Bitmap(img);
                        }
                    }

                    // Créer le bouton overlay
                    overlayButton = new Button
                    {
                        FlatStyle = FlatStyle.Flat,
                        Size = new Size(32, 32),
                        BackColor = Color.FromArgb(45, 45, 48),
                        ForeColor = Color.White,
                        Cursor = Cursors.Hand,
                        Text = "≡"
                    };

                    overlayButton.FlatAppearance.BorderSize = 0;
                    overlayButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 65);
                    overlayButton.Font = new Font("Segoe UI", 12, FontStyle.Bold);

                    // Ajouter les gestionnaires d'événements pour le déplacement
                    overlayButton.MouseDown += (s, e) => {
                        if (e.Button == MouseButtons.Left)
                        {
                            isDragging = true;
                            dragStartPoint = e.Location;
                            overlayButton.Cursor = Cursors.SizeWE;
                        }
                    };

                    overlayButton.MouseMove += (s, e) => {
                        if (isDragging && overlayButton != null && wallpaperForm != null)
                        {
                            Screen currentScreen = GetTargetScreen();
                            int newX = overlayButton.Left + (e.X - dragStartPoint.X);
                            
                            // Limiter le déplacement à l'écran
                            newX = Math.Max(0, Math.Min(currentScreen.Bounds.Width - overlayButton.Width, newX));
                            
                            // Aimantation aux bords
                            if (newX < snapDistance)
                                newX = 0;
                            else if (newX > currentScreen.Bounds.Width - overlayButton.Width - snapDistance)
                                newX = currentScreen.Bounds.Width - overlayButton.Width;
                            
                            overlayButton.Left = newX;
                        }
                    };

                    overlayButton.MouseUp += (s, e) => {
                        if (e.Button == MouseButtons.Left)
                        {
                            isDragging = false;
                            overlayButton.Cursor = Cursors.Hand;
                        }
                    };

                    // Recharger les traductions avant de créer le menu contextuel
                    LocalizedStrings.LoadTranslations();

                    var contextMenu = new ContextMenuStrip();
                    var openMenuItem = new ToolStripMenuItem(LocalizedStrings.GetString("Open BatRun Interface"));
                    openMenuItem.Click += (s, e) =>
                    {
                        var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
                        if (mainForm == null)
                        {
                            mainForm = new MainForm(program, logger, config);
                        }
                        
                        if (!mainForm.Visible)
                        {
                            mainForm.Show();
                        }
                        mainForm.WindowState = FormWindowState.Normal;
                        mainForm.BringToFront();
                        mainForm.Activate();
                    };
                    contextMenu.Items.Add(openMenuItem);

                    // Ajouter un séparateur
                    contextMenu.Items.Add(new ToolStripSeparator());

                    // Ajouter le sous-menu des raccourcis
                    var shortcutsMenuItem = new ToolStripMenuItem(LocalizedStrings.GetString("Shortcuts"));
                    
                    // Ajouter le lien vers l'interface en première position
                    var shortcutInterfaceMenuItem = new ToolStripMenuItem(LocalizedStrings.GetString("Shortcuts Interface"));
                    shortcutInterfaceMenuItem.Click += (s, e) =>
                    {
                        using (var shortcutsForm = new ShortcutsForm(config, logger))
                        {
                            if (shortcutsForm.ShowDialog() == DialogResult.OK)
                            {
                                // Recharger les raccourcis dans le menu
                                shortcutsMenuItem.DropDownItems.Clear();
                                shortcutsMenuItem.DropDownItems.Add(shortcutInterfaceMenuItem);
                                shortcutsMenuItem.DropDownItems.Add(new ToolStripSeparator());
                                LoadShortcuts(shortcutsMenuItem);
                            }
                        }
                    };
                    shortcutsMenuItem.DropDownItems.Add(shortcutInterfaceMenuItem);

                    // Ajouter un séparateur entre l'interface et les raccourcis
                    shortcutsMenuItem.DropDownItems.Add(new ToolStripSeparator());

                    // Charger les raccourcis depuis la configuration
                    LoadShortcuts(shortcutsMenuItem);

                    contextMenu.Items.Add(shortcutsMenuItem);

                    // Ajouter l'option pour ouvrir le dossier RetroBat
                    var openRetroBatFolderMenuItem = new ToolStripMenuItem(LocalizedStrings.GetString("Open RetroBat Folder"));
                    openRetroBatFolderMenuItem.Click += (s, e) =>
                    {
                        try
                        {
                            string retroBatPath = program.GetRetrobatPath();
                            if (!string.IsNullOrEmpty(retroBatPath))
                            {
                                string retroBatFolder = Path.GetDirectoryName(retroBatPath) ?? string.Empty;
                                if (Directory.Exists(retroBatFolder))
                                {
                                    Process.Start("explorer.exe", retroBatFolder);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"Error opening RetroBat folder: {ex.Message}", ex);
                        }
                    };
                    contextMenu.Items.Add(openRetroBatFolderMenuItem);

                    // Ajouter l'option pour lancer le Task Manager
                    var openTaskManagerMenuItem = new ToolStripMenuItem(LocalizedStrings.GetString("Task Manager"));
                    openTaskManagerMenuItem.Click += (s, e) =>
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = @"C:\Windows\System32\Taskmgr.exe",
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"Error launching Task Manager: {ex.Message}", ex);
                        }
                    };
                    contextMenu.Items.Add(openTaskManagerMenuItem);

                    // Ajouter le bouton pour gérer les applications
                    var manageAppsMenuItem = new ToolStripMenuItem(LocalizedStrings.GetString("Manage Applications Hide"));
                    manageAppsMenuItem.Click += (s, e) =>
                    {
                        using (var windowsForm = new Form
                        {
                            Text = LocalizedStrings.GetString("Manage Applications Hide"),
                            Size = new Size(500, 500),
                            StartPosition = FormStartPosition.CenterScreen,
                            BackColor = Color.FromArgb(32, 32, 32),
                            ForeColor = Color.White
                        })
                        {
                            var listView = new ListView
                            {
                                Dock = DockStyle.Fill,
                                View = View.Details,
                                FullRowSelect = true,
                                MultiSelect = false,
                                BackColor = Color.FromArgb(45, 45, 48),
                                ForeColor = Color.White
                            };

                            listView.Columns.Add(LocalizedStrings.GetString("Application"), -2);
                            listView.Columns.Add(LocalizedStrings.GetString("Status"), 100);
                            listView.Columns.Add(LocalizedStrings.GetString("Persistent"), 80);

                            void RefreshWindowsList()
                            {
                                if (listView.IsDisposed) return;
                                listView.Items.Clear();

                                // Ajouter les fenêtres visibles
                                var visibleWindows = applicationManager?.GetVisibleWindows() ?? new List<(IntPtr, string)>();
                                foreach (var window in visibleWindows)
                                {
                                    var item = new ListViewItem(window.Title);
                                    item.SubItems.Add(LocalizedStrings.GetString("Visible"));
                                    item.SubItems.Add(applicationManager?.IsPersistentlyHidden(window.Title) == true ? "✓" : "");
                                    item.Tag = window.Handle;
                                    listView.Items.Add(item);
                                }

                                // Ajouter les fenêtres masquées
                                var hiddenWindows = applicationManager?.GetHiddenWindows() ?? new Dictionary<IntPtr, string>();
                                foreach (var window in hiddenWindows)
                                {
                                    var item = new ListViewItem(window.Value);
                                    item.SubItems.Add(LocalizedStrings.GetString("Hidden"));
                                    item.SubItems.Add(applicationManager?.IsPersistentlyHidden(window.Value) == true ? "✓" : "");
                                    item.Tag = window.Key;
                                    listView.Items.Add(item);
                                }
                            }

                            listView.MouseClick += (s, e) =>
                            {
                                var hitInfo = listView.HitTest(e.X, e.Y);
                                if (hitInfo.Item != null && hitInfo.SubItem != null)
                                {
                                    int columnIndex = hitInfo.Item.SubItems.IndexOf(hitInfo.SubItem);
                                    if (columnIndex == 2) // Colonne Persistent
                                    {
                                        string windowTitle = hitInfo.Item.Text;
                                        if (applicationManager?.IsPersistentlyHidden(windowTitle) == true)
                                        {
                                            applicationManager?.RemoveFromPersistentHidden(windowTitle);
                                        }
                                        else
                                        {
                                            applicationManager?.AddToPersistentHidden(windowTitle);
                                        }
                                        RefreshWindowsList();
                                    }
                                }
                            };

                            listView.MouseDoubleClick += (s, e) =>
                            {
                                var item = listView.GetItemAt(e.X, e.Y);
                                if (item != null && item.Tag is IntPtr hWnd)
                                {
                                    applicationManager?.ToggleWindowVisibility(hWnd);
                                    RefreshWindowsList();
                                }
                            };

                            using (var refreshTimer = new System.Windows.Forms.Timer { Interval = 1000 })
                            {
                                refreshTimer.Tick += (s, e) => RefreshWindowsList();
                                refreshTimer.Start();

                                windowsForm.Controls.Add(listView);
                                RefreshWindowsList();
                                windowsForm.ShowDialog();
                                refreshTimer.Stop();
                            }
                        }
                    };
                    contextMenu.Items.Add(manageAppsMenuItem);

                    // Ajouter le sous-menu pour les fenêtres masquées
                    var hiddenWindowsMenuItem = new ToolStripMenuItem(LocalizedStrings.GetString("Hidden Windows"));
                    contextMenu.Items.Add(hiddenWindowsMenuItem);

                    // Mettre à jour le sous-menu des fenêtres masquées
                    contextMenu.Opening += (s, e) =>
                    {
                        hiddenWindowsMenuItem.DropDownItems.Clear();
                        applicationManager?.ReloadHiddenWindows();
                        var hiddenWindows = applicationManager?.GetHiddenWindows() ?? new Dictionary<IntPtr, string>();
                        
                        if (hiddenWindows.Count == 0)
                        {
                            var noWindowsItem = new ToolStripMenuItem(LocalizedStrings.GetString("No Hidden Windows"))
                            {
                                Enabled = false
                            };
                            hiddenWindowsMenuItem.DropDownItems.Add(noWindowsItem);
                        }
                        else
                        {
                            // Ajouter le bouton "Tout afficher"
                            var showAllItem = new ToolStripMenuItem(LocalizedStrings.GetString("Show All"));
                            showAllItem.Click += (s, e) => applicationManager?.ShowAllWindows();
                            hiddenWindowsMenuItem.DropDownItems.Add(showAllItem);

                            // Ajouter un séparateur
                            hiddenWindowsMenuItem.DropDownItems.Add(new ToolStripSeparator());

                            // Ajouter les fenêtres masquées
                            foreach (var window in hiddenWindows)
                            {
                                var item = new ToolStripMenuItem(window.Value);
                                item.Click += (s, e) => applicationManager?.ToggleWindowVisibility(window.Key);
                                hiddenWindowsMenuItem.DropDownItems.Add(item);
                            }
                        }
                    };

                    overlayButton.Click += (s, e) => contextMenu.Show(overlayButton, new Point(0, overlayButton.Height));

                    wallpaperForm.Controls.Add(overlayButton);

                    Screen targetScreen = GetTargetScreen();
                    Rectangle screenBounds = targetScreen.Bounds;

                    wallpaperForm.StartPosition = FormStartPosition.Manual;
                    wallpaperForm.Location = new Point(screenBounds.X, screenBounds.Y);
                    wallpaperForm.Size = new Size(screenBounds.Width, screenBounds.Height);

                    wallpaperForm.Load += (s, e) =>
                    {
                        var hwnd = wallpaperForm.Handle;
                        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
                        
                        SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, 
                            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

                        const int MARGIN = 10;
                        overlayButton.Location = new Point(MARGIN, MARGIN);
                        overlayButton.BringToFront();

                        if (isVideo && mediaPlayer != null)
                        {
                            mediaPlayer.Play();
                        }
                    };

                    wallpaperForm.Activated += (s, e) =>
                    {
                        if (wallpaperForm != null)
                        {
                            SetWindowPos(wallpaperForm.Handle, HWND_BOTTOM, 0, 0, 0, 0,
                                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                        }

                        // Check if EmulationStation is still running
                        if (IsEmulationStationRunning())
                        {
                            PauseMedia();
                        }
                    };

                    wallpaperForm.FormClosing += (s, e) =>
                    {
                        // Afficher toutes les fenêtres avant la fermeture
                        applicationManager?.ShowAllWindows();
                    };

                    wallpaperForm.Show();
                    logger.LogInfo($"Wallpaper displayed: {wallpaperPath}");
                }
                else
                {
                    logger.LogInfo("Wallpaper not shown due to conditions not met");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error in ShowWallpaper: {ex.Message}", ex);
            }
        }

        private void InitializeVideoPlayer(string videoPath)
        {
            try
            {
                // S'assurer qu'il n'y a pas d'instance active
                lock (mediaPlayerLock)
                {
                    if (activeMediaPlayer != null)
                    {
                        activeMediaPlayer.Stop();
                        activeMediaPlayer.Dispose();
                        activeMediaPlayer = null;
                    }
                }

                if (libVLC == null)
                {
                    logger.LogError("LibVLC instance is not available. Cannot initialize video player.");
                    return;
                }

                videoView = new VideoView
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.Black
                };

                wallpaperForm?.Controls.Add(videoView);
                videoView.SendToBack();

                mediaPlayer = new MediaPlayer(libVLC);
                lock (mediaPlayerLock)
                {
                    activeMediaPlayer = mediaPlayer;
                }

                videoView.MediaPlayer = mediaPlayer;
                media = new Media(libVLC, videoPath, FromType.FromPath);
                
                bool loopVideo = config.ReadBool("Wallpaper", "LoopVideo", true);
                if (loopVideo)
                {
                    media.AddOption(":input-repeat=65535");
                }
                
                mediaPlayer.Media = media;
                
                // Gérer le volume en fonction du paramètre EnableAudio
                bool enableAudio = config.ReadBool("Wallpaper", "EnableAudio", false);
                mediaPlayer.Volume = enableAudio ? 100 : 0;
                
                mediaPlayer.EnableHardwareDecoding = true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error initializing video player: {ex.Message}", ex);
                // Cleanup partially created objects to prevent leaks
                mediaPlayer?.Dispose();
                mediaPlayer = null;
                videoView?.Dispose();
                videoView = null;
                media?.Dispose();
                media = null;
            }
        }

        private void OnMediaPlayerEndReached(object? sender, EventArgs e)
        {
            if (mediaPlayer != null && !isClosing)
            {
                Application.OpenForms[0]?.BeginInvoke(new Action(() =>
                {
                    if (mediaPlayer != null && !isClosing)
                    {
                        mediaPlayer.Stop();
                        mediaPlayer.Play();
                    }
                }));
            }
        }

        private string GetEmulationStationVideoPath()
        {
            try
            {
                string retroBatPath = program.GetRetrobatPath();
                if (!string.IsNullOrEmpty(retroBatPath))
                {
                    string retroBatFolder = Path.GetDirectoryName(retroBatPath) ?? string.Empty;
                    string esVideoPath = Path.Combine(retroBatFolder, "emulationstation", ".emulationstation", "video");
                    if (Directory.Exists(esVideoPath))
                    {
                        return esVideoPath;
                    }
                    else
                    {
                        logger.LogError($"EmulationStation video directory not found at: {esVideoPath}");
                    }
                }
                else
                {
                    logger.LogError("RetroBat path is empty");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error getting EmulationStation video path: {ex.Message}", ex);
            }
            return string.Empty;
        }

        private string GetRandomWallpaperPath()
        {
            try
            {
                string wallpaperFolder = Path.Combine(AppContext.BaseDirectory, "Wallpapers");
                string selectedFolder = config.ReadValue("Wallpaper", "SelectedFolder", "/");
                
                List<string> allFiles = new List<string>();
                
                if (selectedFolder == "/")
                {
                    // Si le dossier racine est sélectionné, inclure tous les fichiers de tous les dossiers
                    if (Directory.Exists(wallpaperFolder))
                    {
                        allFiles.AddRange(Directory.GetFiles(wallpaperFolder, "*.*", SearchOption.AllDirectories)
                            .Where(file => SupportedExtensions.Contains(Path.GetExtension(file).ToLower())));
                    }
                }
                else if (selectedFolder == "ES Videos")
                {
                    string esVideoPath = GetEmulationStationVideoPath();
                    if (!string.IsNullOrEmpty(esVideoPath) && Directory.Exists(esVideoPath))
                    {
                        allFiles.AddRange(Directory.GetFiles(esVideoPath, "*.*", SearchOption.TopDirectoryOnly)
                            .Where(file => SupportedExtensions.Contains(Path.GetExtension(file).ToLower())));
                    }
                }
                else
                {
                    // Sinon, inclure uniquement les fichiers du dossier sélectionné
                    string searchPath = Path.Combine(wallpaperFolder, selectedFolder);
                    if (Directory.Exists(searchPath))
                    {
                        allFiles.AddRange(Directory.GetFiles(searchPath, "*.*", SearchOption.TopDirectoryOnly)
                            .Where(file => SupportedExtensions.Contains(Path.GetExtension(file).ToLower())));
                    }
                }

                if (allFiles.Count > 0)
                {
                    string selectedFile = allFiles[random.Next(allFiles.Count)];
                    logger.LogInfo($"Selected random wallpaper: {selectedFile}");
                    return selectedFile;
                }
                else
                {
                    logger.LogError($"No files found in selected folder: {selectedFolder}");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error in GetRandomWallpaperPath: {ex.Message}", ex);
                return string.Empty;
            }
        }

        private bool IsExplorerRunning()
        {
            try
            {
                bool isRunning = Process.GetProcessesByName("explorer").Length > 0;
                logger.LogInfo($"Explorer.exe running status: {isRunning}");
                return isRunning;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error checking explorer status: {ex.Message}", ex);
                return false;
            }
        }

        public static void CleanupAllInstances()
        {
            lock (instanceLock)
            {
                foreach (var instance in activeInstances.ToList())
                {
                    try
                    {
                        instance.CloseWallpaper();
                    }
                    catch (Exception ex)
                    {
                        // Log but continue cleanup
                        instance.logger.LogError($"Error during instance cleanup: {ex.Message}", ex);
                    }
                }
                activeInstances.Clear();
            }
        }

        public void CloseWallpaper()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
            {
                try
                {
                    isClosing = true;

                    // Stop the EmulationStation monitor timer
                    emulationStationMonitorTimer?.Stop();
                    emulationStationMonitorTimer?.Dispose();
                    emulationStationMonitorTimer = null;

                    // Cleanup GIF resources
                    CleanupGifResources();

                    // Afficher toutes les fenêtres masquées et nettoyer la configuration
                    applicationManager?.CleanupOnExit();

                    // Arrêter le MediaPlayer actif
                    StopActiveMediaPlayer();

                    // Nettoyage des composants dans un ordre spécifique
                    media?.Dispose();
                    media = null;

                    if (videoView != null)
                    {
                        if (videoView.MediaPlayer != null)
                        {
                            videoView.MediaPlayer = null;
                        }
                        videoView.Dispose();
                        videoView = null;
                    }

                    mediaPlayer?.Dispose();
                    mediaPlayer = null;

                    libVLC?.Dispose();
                    libVLC = null;

                    // Nettoyer le PictureBox et l'image du GIF
                    if (pictureBox != null)
                    {
                        if (pictureBox.Image != null)
                        {
                            DetachGifAnimation();
                            pictureBox.Image.Dispose();
                            pictureBox.Image = null;
                        }
                        pictureBox.Dispose();
                        pictureBox = null;
                    }

                    // Nettoyage de l'interface utilisateur
                    overlayButton?.Dispose();
                    overlayButton = null;

                    if (wallpaperForm != null)
                    {
                        if (!wallpaperForm.IsDisposed)
                        {
                            if (wallpaperForm.InvokeRequired)
                            {
                                wallpaperForm.BeginInvoke(new Action(() =>
                                {
                                    wallpaperForm.Hide();
                                    wallpaperForm.Close();
                                }));
                            }
                            else
                            {
                                wallpaperForm.Hide();
                                wallpaperForm.Close();
                            }
                        }
                        wallpaperForm.Dispose();
                        wallpaperForm = null;
                    }

                    blackBackground?.Dispose();
                    blackBackground = null;

                    logger.LogInfo("Wallpaper closed and resources cleaned up successfully");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error closing wallpaper: {ex.Message}", ex);
                }
                finally
                {
                    isClosing = false;
                }
            }

            isDisposed = true;
        }

        public static void StopActiveMediaPlayer()
        {
            lock (mediaPlayerLock)
            {
                if (activeMediaPlayer != null)
                {
                    try
                    {
                        activeMediaPlayer.Stop();
                        activeMediaPlayer.Dispose();
                        activeMediaPlayer = null;
                        
                        // Forcer un nettoyage mémoire
                        GC.Collect(2, GCCollectionMode.Forced, true);
                        GC.WaitForPendingFinalizers();
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue
                        Debug.WriteLine($"Error stopping active media player: {ex.Message}");
                    }
                }
            }
        }

        public void PauseMedia()
        {
            lock (mediaPauseLock)
            {
                if (!isMediaPaused)
                {
                    // Pause vidéo MP4
                    if (mediaPlayer?.IsPlaying == true)
                    {
                        mediaPlayer.Pause();
                        isMediaPaused = true;
                        logger.LogInfo("Media playback paused");
                    }

                    // Pause GIF
                    if (pictureBox?.Image != null && pictureBox.Image.RawFormat.Guid == System.Drawing.Imaging.ImageFormat.Gif.Guid)
                    {
                        if (!isGifPaused)
                        {
                            // Arrêter l'animation avant de sauvegarder
                            DetachGifAnimation();
                            
                            // Sauvegarder le GIF original
                            originalGifImage = pictureBox.Image;
                            
                            // Créer un fond noir
                            var blackBackground = new Bitmap(pictureBox.Width, pictureBox.Height);
                            using (Graphics g = Graphics.FromImage(blackBackground))
                            {
                                g.Clear(Color.Black);
                            }
                            
                            // Remplacer le GIF par le fond noir
                            pictureBox.Image = blackBackground;
                            
                            isGifPaused = true;
                            isMediaPaused = true;
                            logger.LogInfo("GIF animation paused and replaced with black background");
                        }
                    }
                }
            }
        }

        public void ResumeMedia()
        {
            lock (mediaPauseLock)
            {
                if (isMediaPaused && !IsEmulationStationRunning())
                {
                    // Reprise vidéo MP4
                    if (mediaPlayer != null && !mediaPlayer.IsPlaying)
                    {
                        mediaPlayer.Play();
                        isMediaPaused = false;
                        logger.LogInfo("Media playback resumed");
                    }

                    // Reprise GIF
                    if (isGifPaused && originalGifImage != null && pictureBox != null)
                    {
                        try
                        {
                            // Nettoyer le fond noir
                            if (pictureBox.Image != originalGifImage)
                            {
                                pictureBox.Image?.Dispose();
                            }
                            
                            // Restaurer le GIF original
                            pictureBox.Image = originalGifImage;
                            
                            // Réattacher l'animation
                            frameChangedHandler = new EventHandler(OnFrameChanged);
                            ImageAnimator.Animate(pictureBox.Image, frameChangedHandler);
                            
                            isGifPaused = false;
                            isMediaPaused = false;
                            logger.LogInfo("GIF animation restored and resumed");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"Error resuming GIF animation: {ex.Message}", ex);
                        }
                    }
                }
            }
        }

        private void CleanupGifResources()
        {
            if (originalGifImage != null)
            {
                originalGifImage.Dispose();
                originalGifImage = null;
            }
        }

        private bool IsEmulationStationRunning()
        {
            try
            {
                var processes = Process.GetProcessesByName("emulationstation");
                bool isRunning = processes.Length > 0;
                if (isRunning)
                {
                    consecutiveChecksWithoutEmulationStation = 0;
                }
                return isRunning;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error checking EmulationStation status: {ex.Message}", ex);
                return false;
            }
        }

        private void CheckEmulationStationStatus()
        {
            if (IsEmulationStationRunning())
            {
                consecutiveChecksWithoutEmulationStation = 0;
                PauseMedia();
            }
            else
            {
                consecutiveChecksWithoutEmulationStation++;
                //logger.LogInfo($"EmulationStation not detected ({consecutiveChecksWithoutEmulationStation}/{REQUIRED_CHECKS_BEFORE_RESUME} checks)");
                
                if (consecutiveChecksWithoutEmulationStation >= REQUIRED_CHECKS_BEFORE_RESUME)
                {
                    // Réinitialiser le compteur
                    consecutiveChecksWithoutEmulationStation = 0;
                    
                    // Double vérification avant de reprendre
                    if (!IsEmulationStationRunning())
                    {
                        Task.Delay(1000).ContinueWith(_ => 
                        {
                            if (!IsEmulationStationRunning())
                            {
                                ResumeMedia();
                            }
                        });
                    }
                }
            }
        }

        private void OnFrameChanged(object? sender, EventArgs e)
        {
            try
            {
                if (pictureBox?.InvokeRequired == true)
                {
                    pictureBox.BeginInvoke(new Action(() => pictureBox.Invalidate()));
                }
                else
                {
                    pictureBox?.Invalidate();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error in OnFrameChanged: {ex.Message}", ex);
            }
        }

        private EventHandler? frameChangedHandler;

        private void AttachGifAnimation()
        {
            try
            {
                if (pictureBox?.Image != null && pictureBox.Image.RawFormat.Guid == System.Drawing.Imaging.ImageFormat.Gif.Guid)
                {
                    if (!isGifPaused)
                    {
                        frameChangedHandler = new EventHandler(OnFrameChanged);
                        ImageAnimator.Animate(pictureBox.Image, frameChangedHandler);
                        logger.LogInfo("GIF animation started");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error attaching GIF animation: {ex.Message}", ex);
            }
        }

        private void DetachGifAnimation()
        {
            try
            {
                if (pictureBox?.Image != null && frameChangedHandler != null)
                {
                    ImageAnimator.StopAnimate(pictureBox.Image, frameChangedHandler);
                    logger.LogInfo("GIF animation detached");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error detaching GIF animation: {ex.Message}", ex);
            }
        }

        private void LoadShortcuts(ToolStripMenuItem shortcutsMenu)
        {
            try
            {
                int shortcutCount = config.ReadInt("Shortcuts", "Count", 0);
                for (int i = 0; i < shortcutCount; i++)
                {
                    string name = config.ReadValue("Shortcuts", $"Name{i}", "");
                    string path = config.ReadValue("Shortcuts", $"Path{i}", "");

                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(path))
                    {
                        var shortcutItem = new ToolStripMenuItem(name);
                        shortcutItem.Click += (s, e) =>
                        {
                            try
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = path,
                                    UseShellExecute = true
                                });
                            }
                            catch (Exception ex)
                            {
                                logger.LogError($"Error launching shortcut {name}: {ex.Message}", ex);
                                MessageBox.Show($"Error launching {name}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        };
                        shortcutsMenu.DropDownItems.Add(shortcutItem);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error loading shortcuts: {ex.Message}", ex);
            }
        }

        public void DisablePauseAndBlackBackground()
        {
            isPauseEnabled = false;
            isBlackBackgroundEnabled = false;
            if (mediaPlayer != null)
            {
                mediaPlayer.Play();
            }
            if (blackBackground != null)
            {
                blackBackground.Hide();
            }
        }

        public void EnablePauseAndBlackBackground()
        {
            isPauseEnabled = true;
            isBlackBackgroundEnabled = true;
        }

        public void BringToFront()
        {
            if (wallpaperForm != null)
            {
                wallpaperForm.BringToFront();
            }
        }

        public void SendToBack()
        {
            if (wallpaperForm != null)
            {
                wallpaperForm.SendToBack();
            }
        }

        private void PauseWallpaper()
        {
            if (!isPauseEnabled) return;

            if (mediaPlayer != null)
            {
                mediaPlayer.Pause();
            }
            if (isBlackBackgroundEnabled && blackBackground != null)
            {
                blackBackground.Show();
            }
        }
    }
} 