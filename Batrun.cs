using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BatRun
{
    public class Batrun : ApplicationContext
    {
        private readonly Logger logger;
        private readonly IniFile config;
        private readonly ControllerService controllerService;
        private readonly RetroBatService retroBatService;
        private NotifyIcon trayIcon;
        private List<IntPtr> activeWindows = new List<IntPtr>();
        private WallpaperManager? wallpaperManager;
        private ESLoadingPlayer? esLoadingPlayer;

        public Batrun()
        {
            logger = new Logger("BatRun.log", appendToExisting: false);
            logger.ClearLogFile();
            logger.LogInfo("=== BatRun Starting ===");

            config = new IniFile("config.ini");

            retroBatService = new RetroBatService(logger, config, MinimizeActiveWindows, RestoreActiveWindows);
            retroBatService.Initialize(); // Initialize service to get the path

            wallpaperManager = new WallpaperManager(config, logger, null, retroBatService.GetRetrobatPath()); // The form reference can be tricky here.
            controllerService = new ControllerService(logger, OnHotkeyCombination, config, retroBatService.GetRetrobatPath());
            controllerService.Initialize();

            InitializeTrayIcon();
            controllerService.StartPolling();

            // Execute shell commands from original Program.cs constructor
            Task.Run(async () =>
            {
                try
                {
                    var shellExecutor = new ShellCommandExecutor(config, logger, retroBatService.GetRetrobatPath(), null, wallpaperManager);
                    await shellExecutor.ExecuteShellCommandsAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError("Error executing shell commands", ex);
                }
            });
        }

        public void Run()
        {
            Application.Run(this);
        }

        private async void OnHotkeyCombination()
        {
            // This is the callback from ControllerService.
            // It runs on a background thread, so we need to be careful.
            bool hideESLoading = config.ReadBool("Windows", "HideESLoading", false);

            if (hideESLoading)
            {
                if (wallpaperManager != null)
                {
                    wallpaperManager.EnablePauseAndBlackBackground();
                    wallpaperManager.PauseMedia();
                }

                esLoadingPlayer = new ESLoadingPlayer(config, logger, wallpaperManager!);
                string videoPath = System.IO.Path.Combine(AppContext.BaseDirectory, "ESloading", config.ReadValue("Windows", "ESLoadingVideo", "None"));
                await esLoadingPlayer.PlayLoadingVideo(videoPath);
            }
            else
            {
                bool showHotkeySplash = config.ReadBool("Windows", "ShowHotkeySplash", true);
                if (showHotkeySplash)
                {
                    // This needs to be marshaled to the UI thread if Batrun itself doesn't have one.
                    // For ApplicationContext, we can assume it's fine for now.
                    using (var splash = new HotkeySplashForm())
                    {
                        splash.Show();
                        Application.DoEvents();
                        await Task.Delay(2500);
                    }
                }
            }

            // Now, call the clean launch service
            await retroBatService.LaunchRetrobat();
        }

        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon
            {
                Icon = new Icon(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("BatRun.Assets.icon.ico") ?? throw new InvalidOperationException()),
                Text = "BatRun",
                Visible = true
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show Config", null, (s, e) => new ConfigurationForm(config, logger, null, retroBatService.GetRetrobatPath()).Show());
            contextMenu.Items.Add("Exit", null, (s, e) => Exit());
            trayIcon.ContextMenuStrip = contextMenu;
        }

        private void MinimizeActiveWindows()
        {
            if (!config.ReadBool("Windows", "MinimizeWindows", true)) return;

            activeWindows.Clear();
            NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                if (NativeMethods.IsWindowVisible(hWnd))
                {
                    int length = NativeMethods.GetWindowTextLength(hWnd);
                    if (length > 0)
                    {
                        StringBuilder sb = new StringBuilder(length + 1);
                        NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
                        string title = sb.ToString();
                        if (!string.IsNullOrEmpty(title) && !title.Contains("BatRun") && !title.Contains("Program Manager"))
                        {
                            activeWindows.Add(hWnd);
                            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_MINIMIZE);
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);
        }

        private async Task RestoreActiveWindows()
        {
            foreach (var hWnd in activeWindows)
            {
                NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
            }
            activeWindows.Clear();
            await Task.CompletedTask;
        }

        private void Exit()
        {
            controllerService.Cleanup();
            wallpaperManager?.CloseWallpaper();
            esLoadingPlayer?.Dispose();
            trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void OnMainFormClosed(object sender, EventArgs e)
        {
            // This is called when Application.Exit() is called.
            // We override it to make sure we clean up properly.
            Exit();
            base.OnMainFormClosed(sender, e);
        }
    }
}
