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

        public ESLoadingPlayer(IniFile config, Logger logger, WallpaperManager wallpaperManager, LibVLC libVLC)
        {
            this.config = config;
            this.logger = logger;
            this.wallpaperManager = wallpaperManager;
            this.libVLC = libVLC;
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

        private Form? GetMainForm()
        {
            return Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;
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

                await Task.Run(() =>
                {
                    try
                    {
                        var mainForm = GetMainForm();
                        if (mainForm == null)
                        {
                            logger.LogError("No main form found");
                            tcs.TrySetException(new InvalidOperationException("No main form found"));
                            return;
                        }

                        mainForm.Invoke(new Action(() =>
                        {
                            try
                            {
                                if (this.libVLC == null)
                                {
                                    logger.LogError("LibVLC instance is null in PlayLoadingVideo.");
                                    tcs.TrySetResult(false);
                                    return;
                                }

                                // Créer le lecteur média
                                mediaPlayer = new MediaPlayer(this.libVLC);
                                mediaPlayer.EnableHardwareDecoding = true;

                                // Gérer l'état initial de l'audio uniquement selon les paramètres ESLoading
                                bool muteAll = config.ReadValue("Windows", "ESLoadingVideoMuteAll", "false") == "true";
                                mediaPlayer.Mute = muteAll;
                                mediaPlayer.Volume = 100;  // S'assurer que le volume est au maximum

                                // Créer la fenêtre vidéo
                                videoForm = new Form
                                {
                                    FormBorderStyle = FormBorderStyle.None,
                                    WindowState = FormWindowState.Maximized,
                                    TopMost = true,
                                    ShowInTaskbar = false,
                                    BackColor = Color.Black
                                };

                                videoView = new VideoView
                                {
                                    Dock = DockStyle.Fill,
                                    BackColor = Color.Black,
                                    MediaPlayer = mediaPlayer
                                };

                                videoForm.Controls.Add(videoView);

                                // Configurer la lecture en boucle
                                bool shouldLoop = config.ReadValue("Windows", "ESLoadingVideoLoop", "false") == "true";
                                bool muteAfterFirst = config.ReadValue("Windows", "ESLoadingVideoMuteAfterFirst", "false") == "true";

                                // Charger la vidéo avec les options appropriées
                                using (var media = new Media(this.libVLC, videoPath, FromType.FromPath))
                                {
                                    if (shouldLoop)
                                    {
                                        media.AddOption(":input-repeat=65535");  // Nombre élevé de répétitions
                                    }
                                    mediaPlayer.Media = media;
                                }

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
                                        if (!firstPlayCompleted && videoLength > 0 && e.Time >= videoLength - 500)
                                        {
                                            firstPlayCompleted = true;
                                            mediaPlayer.Mute = true;
                                            logger.LogInfo($"Muting audio at time {e.Time}ms of {videoLength}ms");
                                        }
                                    };
                                }

                                videoForm.Show();
                                mediaPlayer.Play();
                                isVideoPlaying = true;

                                // Démarrer la surveillance du contrôleur
                                controllerTask = MonitorControllerInput();

                                logger.LogInfo($"Started playing ES loading video: {videoPath}");
                                tcs.TrySetResult(true);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError($"Error in UI thread: {ex.Message}");
                                tcs.TrySetException(ex);
                            }
                        }));
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error invoking UI thread: {ex.Message}");
                        tcs.TrySetException(ex);
                    }
                });

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
                        videoForm.Invoke(new Action(() => CleanupVideoForm()));
                    }
                    else
                    {
                        CleanupVideoForm();
                    }
                }

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
                // Do not dispose the shared instance
                // libVLC.Dispose();
                libVLC = null;
            }

            if (videoForm != null && !videoForm.IsDisposed)
            {
                videoForm.Close();
                videoForm.Dispose();
                videoForm = null;
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