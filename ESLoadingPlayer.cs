using System;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using System.Drawing;
using System.Threading.Tasks;
using System.Linq;
using SDL2;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace BatRun
{
    public class ESLoadingPlayer : IDisposable
    {
        private readonly IniFile config;
        private readonly Logger logger;
        private Form? videoForm;
        private LibVLC? libVLC;
        private VideoView? videoView;
        private MediaPlayer? mediaPlayer;
        private bool isDisposed;
        private bool isVideoPlaying = false;
        private Task? controllerTask;
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly object controllerLock = new object();
        private readonly WallpaperManager wallpaperManager;
        private bool firstPlayCompleted = false;
        private long videoLength = 0;
        private ButtonMapping? buttonMapping;
        private Dictionary<int, IntPtr> openJoysticks = new();
        private System.Threading.Timer? watchdogTimer;
        private long lastTimeChanged;
        private const int WATCHDOG_INTERVAL_MS = 1000;
        private const int FREEZE_TIMEOUT_MS = 5000;

        public ESLoadingPlayer(IniFile config, Logger logger, WallpaperManager wallpaperManager)
        {
            this.config = config;
            this.logger = logger;
            this.wallpaperManager = wallpaperManager;
            InitializeSDL();
            LoadButtonMappings();
        }

        private void InitializeSDL()
        {
            try
            {
                if (SDL.SDL_WasInit(SDL.SDL_INIT_JOYSTICK) == 0)
                {
                    if (SDL.SDL_Init(SDL.SDL_INIT_JOYSTICK) < 0)
                    {
                        throw new Exception($"SDL_Init failed: {SDL.SDL_GetError()}");
                    }
                }
                SDL.SDL_JoystickEventState(SDL.SDL_ENABLE);
                logger.LogInfo("SDL initialized successfully");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error initializing SDL: {ex.Message}");
            }
        }

        private void LoadButtonMappings()
        {
            try
            {
                string jsonPath = Path.Combine(AppContext.BaseDirectory, "buttonMappings.json");
                if (File.Exists(jsonPath))
                {
                    var jsonData = File.ReadAllText(jsonPath);
                    buttonMapping = JsonConvert.DeserializeObject<ButtonMapping>(jsonData);
                    logger.LogInfo("Button mappings loaded successfully");
                }
                else
                {
                    logger.LogWarning("buttonMappings.json not found");
                    buttonMapping = new ButtonMapping();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error loading button mappings: {ex.Message}");
                buttonMapping = new ButtonMapping();
            }
        }

        private async Task MonitorControllerInput()
        {
            try
            {
                bool buttonPressed = false;
                while (!cancellationTokenSource.Token.IsCancellationRequested && isVideoPlaying)
                {
                    try
                    {
                        SDL.SDL_Event evt;
                        while (SDL.SDL_PollEvent(out evt) == 1)
                        {
                            if (evt.type == SDL.SDL_EventType.SDL_JOYBUTTONDOWN && !buttonPressed)
                            {
                                var joystickId = evt.jbutton.which;
                                var buttonNumber = evt.jbutton.button;

                                if (openJoysticks.TryGetValue(joystickId, out IntPtr joystick))
                                {
                                    var joystickName = SDL.SDL_JoystickName(joystick) ?? string.Empty;
                                    var guid = SDL.SDL_JoystickGetGUID(joystick);
                                    var joystickGuid = guid.ToString() ?? string.Empty;

                                    var controllerConfig = buttonMapping?.Controllers.FirstOrDefault(c => 
                                        c.JoystickName == joystickName && c.DeviceGuid == joystickGuid);

                                    if (controllerConfig != null)
                                    {
                                        // Check if this is an XInput controller (has default mapping)
                                        bool isXInput = controllerConfig.Mappings.TryGetValue("StartButton", out string? startButtonValue) 
                                                      && startButtonValue == "Start";

                                        bool isStartButton = false;
                                        if (isXInput)
                                        {
                                            // Pour XInput, le bouton 7 est le bouton Start
                                            isStartButton = (buttonNumber == 7);
                                        }
                                        else if (controllerConfig.Mappings.TryGetValue("StartButton", out startButtonValue) 
                                                && !string.IsNullOrEmpty(startButtonValue))
                                        {
                                            // DirectInput handling
                                            if (startButtonValue.StartsWith("Button ") && 
                                                int.TryParse(startButtonValue.Substring(7), out int mappedButton))
                                            {
                                                isStartButton = (buttonNumber == mappedButton);
                                            }
                                        }

                                        if (isStartButton)
                                        {
                                            buttonPressed = true;
                                            await Task.Delay(250, cancellationTokenSource.Token); // Utiliser le token pour le délai
                                            if (!cancellationTokenSource.Token.IsCancellationRequested)
                                            {
                                                CloseVideo();
                                            }
                                            return;
                                        }
                                    }
                                }
                            }
                            else if (evt.type == SDL.SDL_EventType.SDL_JOYBUTTONUP)
                            {
                                buttonPressed = false;
                            }
                        }
                        await Task.Delay(16, cancellationTokenSource.Token); // Utiliser le token pour le délai
                    }
                    catch (OperationCanceledException)
                    {
                        // Sortir proprement si la tâche est annulée
                        return;
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Ignorer l'erreur si le CancellationTokenSource est disposé
                return;
            }
            catch (Exception ex)
            {
                if (!(ex is OperationCanceledException))
                {
                    logger.LogError($"Error in MonitorControllerInput: {ex.Message}");
                }
            }
        }

        private void OpenAllJoysticks()
        {
            try
            {
                int numJoysticks = SDL.SDL_NumJoysticks();
                for (int i = 0; i < numJoysticks; i++)
                {
                    IntPtr joystick = SDL.SDL_JoystickOpen(i);
                    if (joystick != IntPtr.Zero)
                    {
                        int instanceId = SDL.SDL_JoystickInstanceID(joystick);
                        openJoysticks[instanceId] = joystick;
                        logger.LogInfo($"Opened joystick {i}: {SDL.SDL_JoystickName(joystick)}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error opening joysticks: {ex.Message}");
            }
        }

        private void CloseAllJoysticks()
        {
            foreach (var joystick in openJoysticks.Values)
            {
                SDL.SDL_JoystickClose(joystick);
            }
            openJoysticks.Clear();
        }

        public async Task PlayLoadingVideo(string videoPath)
        {
            try
            {
                OpenAllJoysticks();
                string selectedVideo = config.ReadValue("Windows", "ESLoadingVideo", "None");
                if (selectedVideo == "None") return;

                if (!File.Exists(videoPath))
                {
                    logger.LogError($"Loading video not found: {videoPath}");
                    return;
                }

                var tcs = new TaskCompletionSource<bool>();
                var videoThread = new Thread(() =>
                {
                    try
                    {
                        logger.LogInfo("Video thread started. Initializing LibVLC...");
                        libVLC = new LibVLC(
                            "--quiet", "--no-video-title-show", "--no-snapshot-preview", "--no-stats",
                            "--no-sub-autodetect-file", "--no-osd", "--no-video-deco",
                            "--aout=directsound", "--directx-audio-device=default"
                        );
                        logger.LogInfo("LibVLC initialized. Creating MediaPlayer...");

                        mediaPlayer = new MediaPlayer(libVLC) { EnableHardwareDecoding = true };
                        logger.LogInfo("MediaPlayer created. Configuring player...");
                        bool muteAll = config.ReadValue("Windows", "ESLoadingVideoMuteAll", "false") == "true";
                        mediaPlayer.Mute = muteAll;
                        mediaPlayer.Volume = 100;

                        logger.LogInfo("Creating video form...");
                        videoForm = new Form
                        {
                            FormBorderStyle = FormBorderStyle.None,
                            WindowState = FormWindowState.Maximized,
                            TopMost = true,
                            ShowInTaskbar = false,
                            BackColor = Color.Black
                        };
                        logger.LogInfo("Video form created. Creating VideoView...");

                        videoView = new VideoView
                        {
                            Dock = DockStyle.Fill,
                            BackColor = Color.Black,
                            MediaPlayer = mediaPlayer
                        };
                        logger.LogInfo("VideoView created. Adding to form...");

                        videoView.MouseDown += (s, e) => {
                            if (e.Button == MouseButtons.Right) { /* Absorb */ }
                        };

                        videoForm.Controls.Add(videoView);
                        logger.LogInfo("VideoView added to form. Creating media object...");

                        bool shouldLoop = config.ReadValue("Windows", "ESLoadingVideoLoop", "false") == "true";
                        using (var media = new Media(libVLC, videoPath, FromType.FromPath))
                        {
                            if (shouldLoop) media.AddOption(":input-repeat=65535");
                            mediaPlayer.Media = media;
                        }
                        logger.LogInfo("Media object created and assigned. Setting up event handlers...");

                        bool muteAfterFirst = config.ReadValue("Windows", "ESLoadingVideoMuteAfterFirst", "false") == "true";
                        if (muteAfterFirst)
                        {
                            firstPlayCompleted = false;
                            videoLength = 0;

                            mediaPlayer.LengthChanged += (s, e) =>
                            {
                                if (videoLength == 0)
                                {
                                    videoLength = e.Length;
                                    logger.LogInfo($"Video length detected: {videoLength}ms");
                                }
                            };

                            mediaPlayer.TimeChanged += (s, e) =>
                            {
                                lastTimeChanged = Environment.TickCount;
                                if (!firstPlayCompleted && videoLength > 0 && e.Time >= videoLength - 500)
                                {
                                    firstPlayCompleted = true;
                                    mediaPlayer.Mute = true;
                                    logger.LogInfo($"Muting audio at time {e.Time}ms of {videoLength}ms");
                                }
                            };
                        }

                        videoForm.Load += (s, e) => {
                            logger.LogInfo("Video form Load event fired. Playing media...");
                            mediaPlayer.Play();
                            isVideoPlaying = true;
                            lastTimeChanged = Environment.TickCount;
                            logger.LogInfo("Starting watchdog and controller monitoring...");
                            watchdogTimer = new System.Threading.Timer(WatchdogCallback, null, WATCHDOG_INTERVAL_MS, WATCHDOG_INTERVAL_MS);
                            controllerTask = MonitorControllerInput();
                            logger.LogInfo($"Started playing ES loading video: {videoPath}");
                            tcs.TrySetResult(true);
                        };

                        logger.LogInfo("Starting video form message loop with Application.Run()...");
                        Application.Run(videoForm);
                        logger.LogInfo("Application.Run() has exited.");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error in video thread: {ex.Message}", ex);
                        tcs.TrySetException(ex);
                    }
                    finally
                    {
                        CleanupVideoForm();
                    }
                });

                videoThread.SetApartmentState(ApartmentState.STA);
                videoThread.IsBackground = true;
                videoThread.Start();

                var timeout = Task.Delay(TimeSpan.FromSeconds(15));
                if (await Task.WhenAny(tcs.Task, timeout) == timeout)
                {
                    // Timeout
                    logger.LogError("Video player startup timed out after 15 seconds. The video thread may be hanging.");
                    // Attempt to clean up by closing the video, which might help if the thread is stuck
                    CloseVideo();
                    throw new TimeoutException("Video player failed to start within the 15-second time limit.");
                }
                // If we get here, tcs.Task completed successfully. We can check for exceptions.
                await tcs.Task;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error playing ES loading video: {ex.Message}", ex);
                CloseAllJoysticks();
                throw;
            }
        }

        public void CloseVideo()
        {
            try
            {
                lock (controllerLock)
                {
                    isVideoPlaying = false;
                    
                    // Arrêter proprement la tâche de monitoring des contrôleurs
                    if (!cancellationTokenSource.IsCancellationRequested && !isDisposed)
                    {
                        try
                        {
                            cancellationTokenSource.Cancel();
                            
                            // Attendre que la tâche de monitoring se termine
                            if (controllerTask != null)
                            {
                                try
                                {
                                    Task.WaitAll(new[] { controllerTask }, 1000); // Attendre max 1 seconde
                                }
                                catch (AggregateException)
                                {
                                    // Ignorer les exceptions de la tâche annulée
                                }
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            // Ignorer si déjà disposé
                        }
                    }
                }

                if (videoForm != null && !videoForm.IsDisposed)
                {
                    if (videoForm.InvokeRequired)
                    {
                        videoForm.Invoke(new Action(() => videoForm.Close()));
                    }
                    else
                    {
                        videoForm.Close();
                    }
                }

                watchdogTimer?.Dispose();
                watchdogTimer = null;

                CloseAllJoysticks();
                logger.LogInfo("ES loading video closed");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error closing video: {ex.Message}");
            }
        }

        private void CleanupVideoForm()
        {
            if (mediaPlayer != null)
            {
                mediaPlayer.Stop();
                mediaPlayer.Dispose();
                mediaPlayer = null;
            }

            if (videoView != null)
            {
                videoView.Dispose();
                videoView = null;
            }

            if (libVLC != null)
            {
                libVLC.Dispose();
                libVLC = null;
            }

            // The form is disposed by Application.Run exiting.
            videoForm = null;
        }

        private void WatchdogCallback(object? state)
        {
            if (!isVideoPlaying || mediaPlayer == null) return;

            // Only check for freeze if the player state is 'Playing'
            if (mediaPlayer.IsPlaying)
            {
                if (Environment.TickCount - lastTimeChanged > FREEZE_TIMEOUT_MS)
                {
                    logger.LogWarning("Video player appears to be frozen while in playing state. Closing video.");
                    // Ensure CloseVideo is called on the UI thread if necessary
                    if (videoForm != null && videoForm.InvokeRequired)
                    {
                        videoForm.Invoke(new Action(() => CloseVideo()));
                    }
                    else
                    {
                        CloseVideo();
                    }
                    watchdogTimer?.Change(Timeout.Infinite, Timeout.Infinite); // Stop the timer
                }
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;

                try
                {
                    // Arrêter la tâche de monitoring des contrôleurs
                    if (!cancellationTokenSource.IsCancellationRequested)
                    {
                        try
                        {
                            cancellationTokenSource.Cancel();
                            // Attendre brièvement que la tâche se termine
                            if (controllerTask != null)
                            {
                                Task.WaitAll(new[] { controllerTask }, 500);
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            // Ignorer si déjà disposé
                        }
                    }

                    CloseVideo();
                    
                    try
                    {
                        cancellationTokenSource.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Ignorer si déjà disposé
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error during disposal: {ex.Message}");
                }
            }
        }
    }
} 