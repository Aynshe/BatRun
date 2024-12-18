using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SDL2;
using Microsoft.Win32;
using System.Text;
using BatRun; // Ajout de l'espace de noms
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX.XInput;
using SharpDX.DirectInput;

namespace BatRun
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool AllowSetForegroundWindow(int dwProcessId);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        public const int SW_RESTORE = 9;
        public const int SW_SHOW = 5;
        public const int SW_SHOWNOACTIVATE = 4;
        public const int SW_MINIMIZE = 6;

        [DllImport("kernel32.dll")]
        public static extern bool SetProcessWorkingSetSize(IntPtr process, int minimumWorkingSetSize, int maximumWorkingSetSize);
    }

    public class IniFile
    {
        private string filePath;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern uint GetPrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpDefault,
            StringBuilder lpReturnedString,
            uint nSize,
            string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool WritePrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpString,
            string lpFileName);

        public IniFile(string filePath)
        {
            this.filePath = filePath;
            EnsureConfigFileExists();
        }

        private void EnsureConfigFileExists()
        {
            if (!File.Exists(filePath))
            {
                var directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Write default values
                WriteValue("Focus", "FocusDuration", "15000");
                WriteValue("Focus", "FocusInterval", "5000");
                WriteValue("Windows", "MinimizeWindows", "true");
                WriteValue("Logging", "EnableLogging", "false");
            }
        }

        public void WriteValue(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, filePath);
        }

        public string ReadValue(string section, string key, string defaultValue)
        {
            StringBuilder result = new StringBuilder(255);
            GetPrivateProfileString(section, key, defaultValue, result, (uint)result.Capacity, filePath);
            return result.ToString();
        }

        public int ReadInt(string section, string key, int defaultValue)
        {
            string value = ReadValue(section, key, defaultValue.ToString());
            return int.TryParse(value, out int result) ? result : defaultValue;
        }

        public bool ReadBool(string section, string key, bool defaultValue)
        {
            string value = ReadValue(section, key, defaultValue.ToString()).ToLower();
            return value == "true" || value == "1";
        }
    }

    public class Program : Form, IBatRunProgram
    {
        private NotifyIcon? trayIcon;
        public IniFile config;
        private readonly string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
        private CancellationTokenSource? pollingCancellation;
        private const int BUTTON_COMBO_TIMEOUT = 1000; // 1 second
        private DateTime lastBackButtonTime = DateTime.MinValue;
        private bool isBackButtonPressed = false;
        private readonly Logger logger = new Logger("BatRun.log");
        private static string retrobatPath = "";
        private readonly LoggingConfig _loggingConfig = new LoggingConfig();
        private MainForm? mainForm;

        // Ajout de DInputHandler
        private DInputHandler dInputHandler;

        private class GameController
        {
            public IntPtr Handle { get; set; }
            public int Index { get; set; }
            public bool IsConnected { get; set; }

            public GameController(IntPtr handle, int index)
            {
                Handle = handle;
                Index = index;
                IsConnected = true;
            }
        }

        private List<GameController> controllers = new List<GameController>();

        [Flags]
        private enum GameControllerButtons
        {
            None = 0,
            Back = 1,
            Start = 2
        }

        private object launchLock = new object();
        private DateTime lastLaunchTime = DateTime.MinValue;
        private const int LAUNCH_COOLDOWN_MS = 5000; // 5 seconds between launches

        private List<IntPtr> activeWindows = new List<IntPtr>();

        private bool _emulationStationPollingLogged = false;
        private bool backStartCombinationDetected = false;
        private bool combinationDetectedLogged = false;
        private ButtonMapping? directInputMapping;
        private List<ButtonMapping> directInputMappings;

        private SDLGameControllerDB? sdlDb;

        // Constantes pour la gestion de la mémoire
        private const long TARGET_MEMORY_USAGE = 100 * 1024 * 1024; // 100 MB en bytes
        private const long MEMORY_CHECK_INTERVAL = 30000; // 30 secondes
        private System.Windows.Forms.Timer? memoryMonitorTimer;

        public const string APP_VERSION = "1.3.1";

        public Program()
        {
            if (configPath == null)
            {
                throw new InvalidOperationException("configPath cannot be null.");
            }

            // Initialize memory management
            InitializeMemoryManagement();

            // Initialize configuration
            config = new IniFile(configPath);

            // Charger l'icône de l'application
            try
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
                else
                {
                    logger.LogError($"Icon file not found at: {iconPath}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error loading icon: {ex.Message}", ex);
            }

            // Vérifier si explorer.exe est en cours d'exécution
            bool isExplorerRunning = Process.GetProcessesByName("explorer").Length > 0;

            if (isExplorerRunning)
            {
                // Comportement normal : cacher la fenêtre et afficher l'icône systray
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                this.Hide();
                InitializeTrayIcon();
            }
            else
            {
                // Explorer n'est pas en cours d'exécution : afficher la fenêtre alternative
                this.WindowState = FormWindowState.Normal;
                this.ShowInTaskbar = true;
                this.Show();
            }

            logger.Log("Starting BatRun");
            
            // Initialize RetroBat path first
            InitializeRetrobatPath();
            
            // Then initialize controllers with the correct path
            InitializeControllers();

            // Initialisation de DInputHandler
            dInputHandler = new DInputHandler(logger);

            // Start initial polling
            StartPolling();

            // Create a timer to check EmulationStation status
            var checkTimer = new System.Windows.Forms.Timer();
            checkTimer.Interval = 5000; // Check every 5 seconds
            checkTimer.Tick += (sender, e) =>
            {
                var wasRunning = IsEmulationStationRunning();
                if (wasRunning && !IsEmulationStationRunning())
                {
                    logger.Log("EmulationStation has stopped, resuming polling");
                    StartPolling();
                }
            };
            checkTimer.Start();

            directInputMappings = new List<ButtonMapping>();
            directInputMapping = new ButtonMapping();
        }

        private void InitializeMemoryManagement()
        {
            // Définir la limite de mémoire de travail
            Environment.SetEnvironmentVariable("DOTNET_GCHeapHardLimit", TARGET_MEMORY_USAGE.ToString());
            
            // Configurer le timer de surveillance de la mémoire
            memoryMonitorTimer = new System.Windows.Forms.Timer();
            memoryMonitorTimer.Interval = (int)MEMORY_CHECK_INTERVAL;
            memoryMonitorTimer.Tick += MonitorMemoryUsage;
            memoryMonitorTimer.Start();

            // Force une collection initiale
            ForceGarbageCollection();
        }

        private void MonitorMemoryUsage(object? sender, EventArgs e)
        {
            var currentMemory = Process.GetCurrentProcess().WorkingSet64;
            
            // Si l'utilisation de la mémoire dépasse la cible de 20%
            if (currentMemory > TARGET_MEMORY_USAGE * 1.2)
            {
                logger.LogInfo($"Memory usage high ({currentMemory / 1024 / 1024}MB), performing cleanup");
                ForceGarbageCollection();
                
                // Vérifier si le nettoyage a été efficace
                currentMemory = Process.GetCurrentProcess().WorkingSet64;
                if (currentMemory > TARGET_MEMORY_USAGE * 1.5)
                {
                    logger.LogWarning($"Memory usage still high after cleanup: {currentMemory / 1024 / 1024}MB");
                }
            }
        }

        private void ForceGarbageCollection()
        {
            try
            {
                GC.Collect(2, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced, true, true);

                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    NativeMethods.SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error during garbage collection: {ex.Message}");
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            if (!this.IsHandleCreated)
            {
                CreateHandle();
                InitializeAlternativeDesign();
            }
            base.SetVisibleCore(value);
        }

        private void InitializeAlternativeDesign()
        {
            // Configuration de base de la fenêtre
            this.Text = "BatRun";
            this.Size = new Size(300, 150);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(32, 32, 32);

            // Création du TableLayoutPanel principal
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.FromArgb(32, 32, 32)
            };

            // Configuration des lignes
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));

            // Label titre
            var titleLabel = new Label
            {
                Text = "BatRun",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            // Bouton pour ouvrir MainForm
            var openButton = new Button
            {
                Text = "Open BatRun Interface",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Dock = DockStyle.Fill,
                Margin = new Padding(10),
                Cursor = Cursors.Hand
            };
            openButton.FlatAppearance.BorderColor = Color.FromArgb(67, 67, 70);
            openButton.Click += (s, e) => ShowMainForm();

            // Ajout des contrôles au layout
            mainLayout.Controls.Add(titleLabel, 0, 0);
            mainLayout.Controls.Add(openButton, 0, 1);

            // Ajout du layout à la fenêtre
            this.Controls.Add(mainLayout);
        }

        private void InitializeControllers()
        {
            string sdlDllPath = Path.Combine(AppContext.BaseDirectory, "SDL2.dll");
            if (!File.Exists(sdlDllPath))
            {
                logger.LogError("SDL2.dll not found. Please ensure SDL2 is properly installed.");
                throw new FileNotFoundException("SDL2.dll not found.");
            }

            if (SDL.SDL_Init(SDL.SDL_INIT_GAMECONTROLLER | SDL.SDL_INIT_HAPTIC) < 0)
            {
                var error = SDL.SDL_GetError();
                logger.LogError($"SDL initialization failed: {error}", new Exception(error));
                return;
            }

            // Initialiser la base de données SDL
            sdlDb = new SDLGameControllerDB(logger);
            if (!string.IsNullOrEmpty(retrobatPath))
            {
                sdlDb.LoadDatabase(retrobatPath);
            }
            else
            {
                logger.LogError("RetroBat path is not set");
            }

            int numJoysticks = SDL.SDL_NumJoysticks();
            logger.LogInfo($"Found {numJoysticks} joysticks");

            // Charger les mappings existants
            string exePath = AppContext.BaseDirectory;
            string parentPath = Path.GetDirectoryName(exePath) ?? exePath;
            string jsonPath = Path.Combine(parentPath, "buttonMappings.json");
            directInputMapping = new ButtonMapping();
            if (File.Exists(jsonPath))
            {
                var jsonData = File.ReadAllText(jsonPath);
                directInputMapping = JsonConvert.DeserializeObject<ButtonMapping>(jsonData) ?? new ButtonMapping();
            }

            for (int i = 0; i < numJoysticks; i++)
            {
                logger.LogInfo($"Initializing controller {i}");
                
                if (SDL.SDL_IsGameController(i) == SDL.SDL_bool.SDL_TRUE)
                {
                    // C'est une manette XInput
                    IntPtr controller = SDL.SDL_GameControllerOpen(i);
                    if (controller != IntPtr.Zero)
                    {
                        controllers.Add(new GameController(controller, i));
                        logger.LogInfo($"Controller {i} initialized successfully");

                        IntPtr joystick = SDL.SDL_GameControllerGetJoystick(controller);
                        if (joystick != IntPtr.Zero)
                        {
                            string controllerName = SDL.SDL_JoystickName(joystick);
                            string deviceGuid = SDL.SDL_JoystickGetGUID(joystick).ToString();

                            // Vérifier si un mapping personnalisé existe déjà
                            var existingMapping = directInputMapping.Controllers.FirstOrDefault(c =>
                                c.JoystickName == controllerName && c.DeviceGuid == deviceGuid);

                            if (existingMapping == null)
                            {
                                // Créer un nouveau mapping avec les boutons par défaut pour XInput
                                var newMapping = new ControllerConfig
                                {
                                    JoystickName = controllerName,
                                    DeviceGuid = deviceGuid,
                                    Mappings = new Dictionary<string, string>
                                    {
                                        { "Hotkey", "Back" },
                                        { "StartButton", "Start" }
                                    }
                                };

                                directInputMapping.Controllers.Add(newMapping);
                                logger.LogInfo($"Created default mapping for XInput controller: {controllerName}");
                            }
                        }
                    }
                }
                else
                {
                    // C'est une manette DirectInput
                    IntPtr joystick = SDL.SDL_JoystickOpen(i);
                    if (joystick != IntPtr.Zero)
                    {
                        try
                        {
                            string controllerName = SDL.SDL_JoystickName(joystick);
                            var sdlGuid = SDL.SDL_JoystickGetGUID(joystick);
                            string rawGuid = sdlGuid.ToString();
                            
                            // Convertir le GUID au format SDL2 (32 caractères hex)
                            byte[] guidBytes = new byte[16];
                            sdlGuid.ToByteArray().CopyTo(guidBytes, 0);
                            string sdlGuidStr = "03000000" + BitConverter.ToString(guidBytes).Replace("-", "").ToLower().Substring(8);

                            logger.LogInfo($"DirectInput controller found: {controllerName}");
                            logger.LogInfo($"Raw GUID: {rawGuid}");
                            logger.LogInfo($"SDL GUID: {sdlGuidStr}");

                            // Vérifier si un mapping personnalisé existe déjà
                            var existingMapping = directInputMapping.Controllers.FirstOrDefault(c =>
                                c.JoystickName == controllerName && c.DeviceGuid == rawGuid);

                            if (existingMapping == null)
                            {
                                // Chercher d'abord par GUID dans la base de données SDL
                                var sdlMapping = sdlDb?.FindMappingByGuid(sdlGuidStr);
                                if (sdlMapping == null)
                                {
                                    // Si pas trouvé par GUID, essayer par nom
                                    sdlMapping = sdlDb?.FindMappingByName(controllerName);
                                }

                                if (sdlMapping != null)
                                {
                                    logger.LogInfo($"Found SDL mapping for {controllerName}");
                                    logger.LogInfo($"SDL DB GUID: {sdlMapping.Guid}");
                                    if (sdlMapping.HasBackAndStartButtons)
                                    {
                                        // Créer un nouveau mapping basé sur la DB SDL
                                        var newMapping = new ControllerConfig
                                        {
                                            JoystickName = controllerName,
                                            DeviceGuid = rawGuid,
                                            Mappings = new Dictionary<string, string>
                                            {
                                                { "Hotkey", $"Button {sdlMapping.ButtonMappings["back"]}" },
                                                { "StartButton", $"Button {sdlMapping.ButtonMappings["start"]}" }
                                            }
                                        };

                                        directInputMapping.Controllers.Add(newMapping);
                                        logger.LogInfo($"Created mapping from SDL DB for {controllerName}:");
                                        logger.LogInfo($"  Back: Button {sdlMapping.ButtonMappings["back"]}");
                                        logger.LogInfo($"  Start: Button {sdlMapping.ButtonMappings["start"]}");
                                    }
                                    else
                                    {
                                        logger.LogInfo($"SDL mapping found but missing back/start buttons for {controllerName}");
                                    }
                                }
                                else
                                {
                                    logger.LogInfo($"No SDL mapping found for {controllerName} with GUID {sdlGuidStr}");
                                }
                            }
                            else
                            {
                                logger.LogInfo($"Using existing custom mapping for {controllerName}");
                            }
                        }
                        finally
                        {
                            SDL.SDL_JoystickClose(joystick);
                        }
                    }
                }
            }

            // Sauvegarder les mappings mis à jour
            if (directInputMapping.Controllers.Any())
            {
                var json = JsonConvert.SerializeObject(directInputMapping, Formatting.Indented);
                File.WriteAllText(jsonPath, json);
                logger.LogInfo("Controller mappings saved to JSON");
            }
        }

        private void CleanupControllers()
        {
            foreach (var controller in controllers)
            {
                SDL.SDL_GameControllerClose(controller.Handle);
            }
            controllers.Clear();
            SDL.SDL_Quit();
        }

        private void StartPolling()
        {
            try 
            {
                // Cancel any existing polling
                if (pollingCancellation != null)
                {
                    pollingCancellation.Cancel();
                    pollingCancellation.Dispose();
                }

                // Create a new polling token
                pollingCancellation = new CancellationTokenSource();
                
                // Réinitialiser les états de la combinaison de touches
                backStartCombinationDetected = false;
                combinationDetectedLogged = false;
                isBackButtonPressed = false;
                lastBackButtonTime = DateTime.MinValue;
                
                // Charger les mappings DirectInput avant de démarrer le polling
                LoadDirectInputMappings();

                // Start polling the controllers
                Task.Run(() => PollControllersAsync(pollingCancellation.Token), pollingCancellation.Token);

                logger.LogInfo("Controller polling started");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error starting polling: {ex.Message}");
            }
        }

        private async Task PollControllersAsync(CancellationToken cancellationToken)
        {
            const int POLL_INTERVAL_MS = 100;
            int lastJoystickCount = SDL.SDL_NumJoysticks();

            SDL.SDL_JoystickEventState(SDL.SDL_ENABLE);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(POLL_INTERVAL_MS, cancellationToken);

                    SDL.SDL_PumpEvents();

                    // Vérifier si le nombre de joysticks a changé
                    int currentJoystickCount = SDL.SDL_NumJoysticks();
                    if (currentJoystickCount != lastJoystickCount)
                    {
                        logger.LogInfo($"Number of joysticks changed from {lastJoystickCount} to {currentJoystickCount}");
                        lastJoystickCount = currentJoystickCount;

                        // Parcourir tous les joysticks pour détecter les nouveaux
                        for (int i = 0; i < currentJoystickCount; i++)
                        {
                            if (SDL.SDL_IsGameController(i) == SDL.SDL_bool.SDL_TRUE)
                            {
                                // XInput controller
                                IntPtr controller = SDL.SDL_GameControllerOpen(i);
                                if (controller != IntPtr.Zero)
                                {
                                    controllers.Add(new GameController(controller, i));
                                    logger.LogInfo($"Controller {i} connected and initialized");

                                    IntPtr joystick = SDL.SDL_GameControllerGetJoystick(controller);
                                    if (joystick != IntPtr.Zero)
                                    {
                                        InitializeController(joystick, true);
                                    }
                                }
                            }
                            else
                            {
                                // DirectInput controller
                                IntPtr joystick = SDL.SDL_JoystickOpen(i);
                                if (joystick != IntPtr.Zero)
                                {
                                    try
                                    {
                                        string controllerName = SDL.SDL_JoystickName(joystick);
                                        var sdlGuid = SDL.SDL_JoystickGetGUID(joystick);
                                        string rawGuid = sdlGuid.ToString();

                                        // Vérifier si ce contrôleur est déjà initialisé
                                        var existingMapping = directInputMapping?.Controllers.FirstOrDefault(c =>
                                            c.JoystickName == controllerName && c.DeviceGuid == rawGuid);

                                        if (existingMapping == null)
                                        {
                                            InitializeController(joystick, false);
                                            SaveControllerMappings();
                                        }
                                    }
                                    finally
                                    {
                                        SDL.SDL_JoystickClose(joystick);
                                    }
                                }
                            }
                        }
                    }

                    SDL.SDL_Event sdlEvent;
                    while (SDL.SDL_PollEvent(out sdlEvent) == 1)
                    {
                        if (sdlEvent.type == SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED)
                        {
                            IntPtr removedController = (IntPtr)sdlEvent.cdevice.which;
                            var controller = controllers.FirstOrDefault(c => c.Handle == removedController);
                            if (controller != null)
                            {
                                SDL.SDL_GameControllerClose(controller.Handle);
                                controllers.Remove(controller);
                                logger.LogInfo($"Controller {controller.Index} disconnected");
                            }
                        }
                    }

                    // Vérifier si EmulationStation est en cours d'exécution
                    if (IsEmulationStationRunning())
                    {
                        continue;
                    }

                    // Vérifier tous les contrôleurs (XInput et DirectInput) en utilisant les mappings JSON
                    for (int i = 0; i < SDL.SDL_NumJoysticks(); i++)
                    {
                        IntPtr joystick = SDL.SDL_JoystickOpen(i);
                        if (joystick != IntPtr.Zero)
                        {
                            try
                            {
                                string joystickName = SDL.SDL_JoystickName(joystick);
                                string deviceGuid = SDL.SDL_JoystickGetGUID(joystick).ToString();

                                var controllerConfig = directInputMapping?.Controllers.FirstOrDefault(c =>
                                    c.JoystickName == joystickName && c.DeviceGuid == deviceGuid);

                                if (controllerConfig?.Mappings != null)
                                {
                                    if (controllerConfig.Mappings.TryGetValue("Hotkey", out string? hotkeyValue) &&
                                        controllerConfig.Mappings.TryGetValue("StartButton", out string? startValue))
                                    {
                                        bool isHotkeyPressed;
                                        bool isStartPressed;

                                        // Pour les manettes XInput/SDL
                                        if (hotkeyValue == "Back" && startValue == "Start")
                                        {
                                            IntPtr controller = SDL.SDL_GameControllerOpen(i);
                                            if (controller != IntPtr.Zero)
                                            {
                                                try
                                                {
                                                    isHotkeyPressed = SDL.SDL_GameControllerGetButton(controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK) == 1;
                                                    isStartPressed = SDL.SDL_GameControllerGetButton(controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START) == 1;
                                                }
                                                finally
                                                {
                                                    SDL.SDL_GameControllerClose(controller);
                                                }
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }
                                        // Pour les manettes DirectInput
                                        else
                                        {
                                            int hotkeyButton = int.Parse(hotkeyValue.Replace("Button ", ""));
                                            int startButton = int.Parse(startValue.Replace("Button ", ""));
                                            isHotkeyPressed = SDL.SDL_JoystickGetButton(joystick, hotkeyButton) == 1;
                                            isStartPressed = SDL.SDL_JoystickGetButton(joystick, startButton) == 1;
                                        }

                                        if (isHotkeyPressed && isStartPressed)
                                        {
                                            logger.LogInfo($"Hotkey combination detected for {joystickName}");
                                            
                                            // Vérifier si la vibration est activée
                                            bool enableVibration = config.ReadBool("Controller", "EnableVibration", true);
                                            
                                            if (enableVibration)
                                            {
                                                // Faire vibrer la manette
                                                try 
                                                {
                                                    // Pour les manettes XInput
                                                    if (hotkeyValue == "Back" && startValue == "Start")
                                                    {
                                                        // Tester chaque index de manette XInput possible
                                                        foreach (UserIndex index in Enum.GetValues(typeof(UserIndex)))
                                                        {
                                                            try
                                                            {
                                                                var xinputController = new Controller(index);
                                                                if (xinputController.IsConnected)
                                                                {
                                                                    // Vérifier si c'est la bonne manette en comparant l'état des boutons
                                                                    var state = xinputController.GetState();
                                                                    bool isCorrectController = 
                                                                        ((state.Gamepad.Buttons & GamepadButtonFlags.Back) != 0) && 
                                                                        ((state.Gamepad.Buttons & GamepadButtonFlags.Start) != 0);

                                                                    if (isCorrectController)
                                                                    {
                                                                        var vibration = new Vibration
                                                                        {
                                                                            LeftMotorSpeed = 65535,
                                                                            RightMotorSpeed = 65535
                                                                        };
                                                                        xinputController.SetVibration(vibration);
                                                                        
                                                                        await Task.Delay(500);
                                                                        xinputController.SetVibration(new Vibration());
                                                                        
                                                                        logger.LogInfo($"XInput controller rumble activated on index {index}");
                                                                        break;
                                                                    }
                                                                }
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                logger.LogError($"Error checking XInput controller at index {index}: {ex.Message}");
                                                            }
                                                        }
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    logger.LogError($"Error activating rumble: {ex.Message}");
                                                }
                                            }

                                            await LaunchRetrobat();
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                SDL.SDL_JoystickClose(joystick);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error during polling: {ex.Message}", ex);
                }
            }
        }

        private void InitializeController(IntPtr joystick, bool isXInput)
        {
            string controllerName = SDL.SDL_JoystickName(joystick);
            var sdlGuid = SDL.SDL_JoystickGetGUID(joystick);
            string rawGuid = sdlGuid.ToString();

            // Convert GUID for DirectInput controllers
            string sdlGuidStr = rawGuid;
            if (!isXInput)
            {
                byte[] guidBytes = new byte[16];
                sdlGuid.ToByteArray().CopyTo(guidBytes, 0);
                sdlGuidStr = "03000000" + BitConverter.ToString(guidBytes).Replace("-", "").ToLower().Substring(8);
            }

            logger.LogInfo($"{(isXInput ? "XInput" : "DirectInput")} controller connected: {controllerName}");
            if (!isXInput)
            {
                logger.LogInfo($"Raw GUID: {rawGuid}");
                logger.LogInfo($"SDL GUID: {sdlGuidStr}");
            }

            // Check if mapping already exists
            var existingMapping = directInputMapping?.Controllers.FirstOrDefault(c =>
                c.JoystickName == controllerName && c.DeviceGuid == rawGuid);

            if (existingMapping == null)
            {
                if (isXInput)
                {
                    // Create default XInput mapping
                    var newMapping = new ControllerConfig
                    {
                        JoystickName = controllerName,
                        DeviceGuid = rawGuid,
                        Mappings = new Dictionary<string, string>
                        {
                            { "Hotkey", "Back" },
                            { "StartButton", "Start" }
                        }
                    };
                    directInputMapping?.Controllers.Add(newMapping);
                    logger.LogInfo($"Created default mapping for XInput controller: {controllerName}");
                    SaveControllerMappings(); // Sauvegarder immédiatement après l'ajout d'une manette XInput
                }
                else
                {
                    // Try to find SDL mapping for DirectInput controller
                    var sdlMapping = sdlDb?.FindMappingByGuid(sdlGuidStr);
                    if (sdlMapping == null)
                    {
                        sdlMapping = sdlDb?.FindMappingByName(controllerName);
                    }

                    if (sdlMapping != null && sdlMapping.HasBackAndStartButtons)
                    {
                        var newMapping = new ControllerConfig
                        {
                            JoystickName = controllerName,
                            DeviceGuid = rawGuid,
                            Mappings = new Dictionary<string, string>
                            {
                                { "Hotkey", $"Button {sdlMapping.ButtonMappings["back"]}" },
                                { "StartButton", $"Button {sdlMapping.ButtonMappings["start"]}" }
                            }
                        };
                        directInputMapping?.Controllers.Add(newMapping);
                        logger.LogInfo($"Created mapping from SDL DB for {controllerName}");
                        SaveControllerMappings(); // Sauvegarder immédiatement après l'ajout d'une manette DirectInput
                    }
                }
            }
        }

        private void SaveControllerMappings()
        {
            if (directInputMapping?.Controllers.Any() == true)
            {
                string exePath = AppContext.BaseDirectory;
                string parentPath = Path.GetDirectoryName(exePath) ?? exePath;
                string jsonPath = Path.Combine(parentPath, "buttonMappings.json");
                var json = JsonConvert.SerializeObject(directInputMapping, Formatting.Indented);
                File.WriteAllText(jsonPath, json);
                logger.LogInfo("Controller mappings saved to JSON");
            }
        }

        private List<string> GetPressedButtons(IntPtr controller)
        {
            List<string> buttons = new List<string>();

            if (SDL.SDL_GameControllerGetButton(controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK) == 1)
                buttons.Add("Back");
            if (SDL.SDL_GameControllerGetButton(controller, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START) == 1)
                buttons.Add("Start");

            return buttons;
        }

        private string FormatPressedButtons(List<string> buttons)
        {
            return buttons.Count > 0 ? string.Join(", ", buttons) : "No buttons pressed";
        }

        private void HandleButtonPress(int controllerId, List<string> pressedButtons)
        {
            try 
            {
                // Vérifiez si EmulationStation est en cours d'exécution
                if (IsEmulationStationRunning())
                {
                    logger.LogInfo("Hotkey disabled: EmulationStation is already running");
                    return;
                }

                // Obtenir le nom et le GUID du contrôleur
                string? controllerName = null;
                string? controllerGuid = null;

                IntPtr controller = SDL.SDL_GameControllerFromInstanceID(controllerId);
                if (controller != IntPtr.Zero)
                {
                    IntPtr joystick = SDL.SDL_GameControllerGetJoystick(controller);
                    if (joystick != IntPtr.Zero)
                    {
                        // Attendre un court instant pour s'assurer que SDL a initialisé le contrôleur
                        for (int attempt = 0; attempt < 3; attempt++)
                        {
                            controllerName = SDL.SDL_JoystickName(joystick);
                            controllerGuid = SDL.SDL_JoystickGetGUID(joystick).ToString();

                            if (!string.IsNullOrEmpty(controllerName) && !string.IsNullOrEmpty(controllerGuid))
                            {
                                break;
                            }
                            Thread.Sleep(100); // Attendre 100ms entre chaque tentative
                        }
                    }
                }

                // Vérifier si ce contrôleur a une configuration personnalisée
                var customMapping = directInputMapping?.Controllers.FirstOrDefault(c => 
                    c.JoystickName == controllerName && 
                    c.DeviceGuid == controllerGuid);

                // Si une configuration personnalisée existe et est valide, ne pas utiliser la combinaison par défaut
                if (customMapping != null && 
                    customMapping.Mappings != null && 
                    customMapping.Mappings.ContainsKey("Hotkey") && 
                    customMapping.Mappings.ContainsKey("StartButton") &&
                    !string.IsNullOrEmpty(customMapping.Mappings["Hotkey"]) &&
                    !string.IsNullOrEmpty(customMapping.Mappings["StartButton"]))
                {
                    logger.LogInfo($"Custom mapping found for {controllerName} (GUID: {controllerGuid}), skipping default combination");
                    return;
                }

                // Si on n'a pas pu obtenir le nom ou le GUID du contrôleur, ou s'il n'y a pas de mapping personnalisé,
                // on utilise la combinaison par défaut
                bool isBackPressed = pressedButtons.Contains("Back");
                bool isStartPressed = pressedButtons.Contains("Start");

                // Reset if no buttons are pressed
                if (!isBackPressed && !isStartPressed)
                {
                    isBackButtonPressed = false;
                    backStartCombinationDetected = false;
                    return;
                }

                // Detect the Back + Start combination
                if (isBackPressed && isStartPressed && !backStartCombinationDetected)
                {
                    backStartCombinationDetected = true;
                    if (!combinationDetectedLogged)
                    {
                        logger.LogInfo("Back + Start combination detected");
                        combinationDetectedLogged = true;
                    }

                    // Check the time since the last Back button press
                    if (!isBackButtonPressed)
                    {
                        lastBackButtonTime = DateTime.Now;
                        isBackButtonPressed = true;
                        logger.LogInfo("First step of the combination reset");
                        return;
                    }

                    // Check the time between presses
                    TimeSpan timeSinceLastBackPress = DateTime.Now - lastBackButtonTime;
                    if (timeSinceLastBackPress.TotalMilliseconds <= BUTTON_COMBO_TIMEOUT)
                    {
                        logger.LogInfo($"Launching Retrobat - Time between presses: {timeSinceLastBackPress.TotalMilliseconds} ms");

                        // Launch Retrobat on the main thread
                        BeginInvoke(new Action(async () => 
                        {
                            await LaunchRetrobat();
                        }));

                        // Reset the states
                        isBackButtonPressed = false;
                        lastBackButtonTime = DateTime.MinValue;
                    }
                    else 
                    {
                        logger.LogInfo($"Time too long: {timeSinceLastBackPress.TotalMilliseconds} ms");
                        isBackButtonPressed = false;
                    }
                }
                else if (isBackPressed)
                {
                    // Reset if only the Back button is pressed
                    lastBackButtonTime = DateTime.Now;
                    isBackButtonPressed = true;
                    logger.LogInfo("Only the Back button is pressed");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error in HandleButtonPress: {ex.Message}");
            }
        }

        private async Task LaunchRetrobat()
        {
            try 
            {
                if (!IsRetrobatRunning())
                {
                    string retrobatPath = GetRetrobatPath();
                    if (!string.IsNullOrEmpty(retrobatPath))
                    {
                        // Créer et afficher le splash sur le thread UI
                        HotkeySplashForm? splash = null;
                        await Task.Run(() =>
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                splash = new HotkeySplashForm();
                                splash.Show();
                                Application.DoEvents(); // Forcer le rendu
                            });
                        });

                        // Attendre avec le splash visible
                        await Task.Delay(2500);

                        MinimizeActiveWindows();

                        // Lancer RetroBat
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = retrobatPath,
                            UseShellExecute = false
                        };
                        Process.Start(startInfo);

                        // Fermer le splash de manière sûre
                        if (splash != null)
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                splash.Close();
                                splash.Dispose();
                            });
                        }

                        // Attendre que EmulationStation démarre
                        int maxAttempts = 10; // 10 tentatives
                        int attemptDelay = 2000; // 2 secondes entre chaque tentative
                        int currentAttempt = 0;
                        Process? esProcess = null;

                        while (currentAttempt < maxAttempts && esProcess == null)
                        {
                            await Task.Delay(attemptDelay);
                            esProcess = Process.GetProcessesByName("emulationstation").FirstOrDefault();
                            currentAttempt++;

                            if (esProcess == null)
                            {
                                logger.LogInfo($"Waiting for EmulationStation to start (attempt {currentAttempt}/{maxAttempts})");
                            }
                        }

                        if (esProcess != null)
                        {
                            esProcess.EnableRaisingEvents = true;
                            esProcess.Exited += async (sender, args) =>
                            {
                                await Task.Run(() => RestoreActiveWindows().Wait());
                                StartPolling();
                            };

                            await CheckIntroSettings();
                            await SetEmulationStationFocus();
                        }
                        else
                        {
                            logger.LogError("EmulationStation failed to start");
                        }
                    }
                    else
                    {
                        logger.LogError("Retrobat path not found");
                    }
                }
                else
                {
                    logger.LogInfo("Retrobat is already running");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error launching Retrobat", ex);
            }
        }

        private void MinimizeActiveWindows()
        {
            // Read the minimize windows setting from the INI file
            bool minimizeWindows = config.ReadBool("Windows", "MinimizeWindows", true);
            
            if (!minimizeWindows)
            {
                logger.LogInfo("Window minimization disabled by configuration");
                return;
            }

            logger.LogInfo("Minimizing active windows");
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

                        // Ignore our own window and certain system windows
                        if (!string.IsNullOrEmpty(title) && 
                            !title.Contains("BatRun") && 
                            !title.Contains("Program Manager"))
                        {
                            activeWindows.Add(hWnd);
                            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_MINIMIZE);
                            logger.LogInfo($"Window minimized: {title}");
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);
        }

        private async Task RestoreActiveWindows()
        {
            // logger.LogInfo("Preparing to restore windows");

            // Reset the logging flag
            _emulationStationPollingLogged = false;

            // Wait for an initial delay to allow processes to start
            await Task.Delay(3000);

            int maxAttempts = 15;
            int currentAttempt = 0;

            while (currentAttempt < maxAttempts)
            {
                var emulationStationProcesses = Process.GetProcessesByName("emulationstation");
                
                if (emulationStationProcesses.Length == 0)
                {
                    logger.LogInfo("No EmulationStation processes found, restoring windows.");
                    
                    // Restore all windows in parallel
                    var tasks = activeWindows.Select(async hWnd =>
                    {
                        try
                        {
                            await Task.Run(() => NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE));
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"Error restoring window: {ex.Message}");
                        }
                    });

                    await Task.WhenAll(tasks); // Ensure all tasks are awaited
                    activeWindows.Clear();
                    logger.LogInfo("Window restoration complete");
                    return;
                }
                else
                {
                    LogEmulationStationPolling();
                    await Task.Delay(1000);
                    currentAttempt++;
                }
            }

            // If EmulationStation processes are still running
            // logger.LogWarning("Unable to restore windows: EmulationStation processes still active.");
        }

        private async Task RestoreWindowsAfterEmulationStation()
        {
            Process[] processes = Process.GetProcessesByName("emulationstation");
            if (processes.Length > 0)
            {
                logger.LogInfo("EmulationStation is still running, waiting for it to close...");
                while (processes.Length > 0)
                {
                    await Task.Delay(1000);
                    processes = Process.GetProcessesByName("emulationstation");
                }
                logger.LogInfo("EmulationStation has closed, proceeding to restore windows.");
            }
            else
            {
                logger.LogInfo("No EmulationStation processes found, restoring windows immediately.");
            }

            // Restore all windows in parallel
            var tasks = activeWindows.Select(async hWnd =>
            {
                try
                {
                    await Task.Run(() => NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE));
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error restoring window: {ex.Message}");
                }
            });

            await Task.WhenAll(tasks);
            activeWindows.Clear();
            
            // Réinitialiser les états de la combinaison de touches
            backStartCombinationDetected = false;
            combinationDetectedLogged = false;
            isBackButtonPressed = false;
            lastBackButtonTime = DateTime.MinValue;
            
            logger.LogInfo("Window restoration complete");
            logger.LogInfo("Controller polling started");
        }

        private async Task SetEmulationStationFocus()
        {
            // Read values from the INI file
            int focusDuration = config.ReadInt("Focus", "FocusDuration", 30000);
            int focusInterval = config.ReadInt("Focus", "FocusInterval", 5000);
            int elapsedTime = 0;

            logger.LogInfo($"Starting focus sequence for EmulationStation (Duration: {focusDuration}ms, Interval: {focusInterval}ms)");

            while (elapsedTime < focusDuration)
            {
                try 
                {
                    // Complete focus sequence
                    var process = Process.GetProcessesByName("emulationstation").FirstOrDefault();
                    IntPtr hWnd = process?.MainWindowHandle ?? IntPtr.Zero;
                    
                    if (hWnd != IntPtr.Zero)
                    {
                        // Check if process is not null before accessing its Id
                        if (process != null)
                        {
                            // Allow focus change for this process
                            NativeMethods.AllowSetForegroundWindow(process.Id);

                            // Get thread IDs
                            uint foregroundThread = NativeMethods.GetWindowThreadProcessId(
                                NativeMethods.GetForegroundWindow(), IntPtr.Zero);
                            uint appThread = NativeMethods.GetCurrentThreadId();

                            // Attach threads for focus
                            bool threadAttached = false;
                            if (foregroundThread != appThread)
                            {
                                threadAttached = NativeMethods.AttachThreadInput(foregroundThread, appThread, true);
                            }

                            try
                            {
                                NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
                                NativeMethods.ShowWindow(hWnd, NativeMethods.SW_SHOW);
                                NativeMethods.BringWindowToTop(hWnd);
                                NativeMethods.SetForegroundWindow(hWnd);
                                
                                logger.LogInfo($"Focus applied to EmulationStation (attempt {elapsedTime/focusInterval + 1}/{focusDuration/focusInterval})");
                            }
                            finally
                            {
                                // Detach threads if necessary
                                if (threadAttached)
                                {
                                    NativeMethods.AttachThreadInput(foregroundThread, appThread, false);
                                }
                            }
                        }
                        else
                        {
                            logger.LogError("Process is null, cannot set foreground window.");
                        }
                    }
                    else
                    {
                        logger.LogInfo("EmulationStation window handle not found");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError("Error setting focus to EmulationStation", ex);
                }

                await Task.Delay(focusInterval);
                elapsedTime += focusInterval;
            }

            logger.LogInfo("End of focus sequence for EmulationStation");

            await RestoreWindowsAfterEmulationStation();
        }

        private void InitializeTrayIcon()
        {
            var strings = new LocalizedStrings();
            
            try
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
                Icon? appIcon = null;

                if (File.Exists(iconPath))
                {
                    appIcon = new Icon(iconPath);
                }
                else
                {
                    logger.LogError($"Icon file not found at: {iconPath}");
                    appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                }

                trayIcon = new NotifyIcon
                {
                    Icon = appIcon,
                    Text = "BatRun",
                    Visible = true
                };
            }
            catch (Exception ex)
            {
                logger.LogError("Error initializing tray icon", ex);
                return; // Sortir de la méthode si l'initialisation de l'icône échoue
            }

            try 
            {
                var contextMenu = new ContextMenuStrip();
                
                // Groupe principal
                contextMenu.Items.Add(strings.OpenBatRun, null, (s, e) => SafeExecute(ShowMainForm));
                contextMenu.Items.Add(new ToolStripSeparator());
                contextMenu.Items.Add(strings.OpenEmulationStation, null, (s, e) => SafeExecute(LaunchEmulationStation));
                contextMenu.Items.Add(strings.LaunchBatGui, null, (s, e) => SafeExecute(LaunchBatGui));
                contextMenu.Items.Add(new ToolStripSeparator());
                
                // Groupe Configuration
                var configMenuItem = new ToolStripMenuItem(strings.Configuration);
                configMenuItem.DropDownItems.Add(strings.GeneralSettings, null, (s, e) => SafeExecute(ShowConfigWindow));
                configMenuItem.DropDownItems.Add(strings.ControllerMappings, null, (s, e) => SafeExecute(OpenMappingConfiguration));
                contextMenu.Items.Add(configMenuItem);
                
                // Groupe Aide
                var helpMenuItem = new ToolStripMenuItem(strings.Help);
                helpMenuItem.DropDownItems.Add(strings.ViewLogs, null, (s, e) => SafeExecute(OpenLogFile));
                helpMenuItem.DropDownItems.Add(strings.ViewErrorLogs, null, (s, e) => SafeExecute(OpenErrorLogFile));
                helpMenuItem.DropDownItems.Add(strings.About, null, (s, e) => SafeExecute(() => ShowAbout(null, EventArgs.Empty)));
                helpMenuItem.DropDownItems.Add(new ToolStripSeparator());
                helpMenuItem.DropDownItems.Add(LocalizedStrings.GetString("Check for Updates"), null, async (s, e) => 
                {
                    try
                    {
                        var progressForm = new Form
                        {
                            Text = LocalizedStrings.GetString("Checking for Updates"),
                            Size = new Size(300, 100),
                            FormBorderStyle = FormBorderStyle.FixedDialog,
                            StartPosition = FormStartPosition.CenterScreen,
                            MaximizeBox = false,
                            MinimizeBox = false,
                            BackColor = Color.FromArgb(32, 32, 32),
                            ForeColor = Color.White
                        };

                        var progressLabel = new Label
                        {
                            Text = LocalizedStrings.GetString("Checking for updates..."),
                            Dock = DockStyle.Fill,
                            TextAlign = ContentAlignment.MiddleCenter
                        };

                        progressForm.Controls.Add(progressLabel);
                        progressForm.Show();

                        var updateChecker = new UpdateChecker(logger, APP_VERSION);
                        var result = await updateChecker.CheckForUpdates();
                        progressForm.Close();

                        if (!result.HasInternetConnection)
                        {
                            MessageBox.Show(
                                "No internet connection",
                                "Update Check Failed",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            return;
                        }

                        if (result.UpdateAvailable)
                        {
                            var dialogResult = MessageBox.Show(
                                string.Format(LocalizedStrings.GetString("New version {0} is available. Would you like to update?"), result.LatestVersion),
                                LocalizedStrings.GetString("Update Available"),
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Information);

                            if (dialogResult == DialogResult.Yes)
                            {
                                var downloadProgress = new Progress<int>(percent =>
                                {
                                    progressLabel.Text = string.Format(LocalizedStrings.GetString("Downloading update: {0}%"), percent);
                                });

                                progressForm = new Form
                                {
                                    Text = LocalizedStrings.GetString("Downloading Update"),
                                    Size = new Size(300, 100),
                                    FormBorderStyle = FormBorderStyle.FixedDialog,
                                    StartPosition = FormStartPosition.CenterScreen,
                                    MaximizeBox = false,
                                    MinimizeBox = false,
                                    BackColor = Color.FromArgb(32, 32, 32),
                                    ForeColor = Color.White
                                };

                                progressLabel = new Label
                                {
                                    Text = LocalizedStrings.GetString("Starting download..."),
                                    Dock = DockStyle.Fill,
                                    TextAlign = ContentAlignment.MiddleCenter
                                };

                                progressForm.Controls.Add(progressLabel);
                                progressForm.Show();

                                var success = await updateChecker.DownloadAndInstallUpdate(result.DownloadUrl, downloadProgress);
                                progressForm.Close();

                                if (!success)
                                {
                                    MessageBox.Show(
                                        LocalizedStrings.GetString("Failed to install update. Please try again later."),
                                        LocalizedStrings.GetString("Update Failed"),
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show(
                                LocalizedStrings.GetString("You have the latest version."),
                                LocalizedStrings.GetString("No Updates Available"),
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error checking for updates: {ex.Message}", ex);
                        MessageBox.Show(
                            LocalizedStrings.GetString("Failed to check for updates. Please try again later."),
                            LocalizedStrings.GetString("Error"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                });
                contextMenu.Items.Add(helpMenuItem);
                
                contextMenu.Items.Add(new ToolStripSeparator());
                contextMenu.Items.Add(strings.Exit, null, (s, e) => SafeExecute(Application.Exit));

                if (trayIcon != null)
                {
                    trayIcon.ContextMenuStrip = contextMenu;
                    // Modifier le comportement du double-clic pour afficher la fenêtre principale
                    trayIcon.MouseDoubleClick += (s, e) => SafeExecute(ShowMainForm);
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error creating context menu", ex);
            }
        }

        public void SafeExecute(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                logger.LogError($"Error executing action: {ex.Message}", ex);
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void LaunchEmulationStation()
        {
            try
            {
                logger.LogInfo("Launching EmulationStation");
                
                if (IsEmulationStationRunning())
                {
                    logger.LogInfo("EmulationStation is already running");
                    return;
                }

                // Launch EmulationStation
                Process.Start(new ProcessStartInfo
                {
                    FileName = retrobatPath,
                    Arguments = "-es",
                    UseShellExecute = false,
                    CreateNoWindow = false
                });

                logger.LogInfo("EmulationStation launched successfully");
            }
            catch (Exception ex)
            {
                logger.LogError("Error launching EmulationStation", ex);
                MessageBox.Show($"Unable to launch EmulationStation: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void LaunchBatGui()
        {
            string batGuiPath = Path.Combine(Path.GetDirectoryName(retrobatPath) ?? string.Empty, "BatGui.exe");
            if (File.Exists(batGuiPath))
            {
                Process.Start(batGuiPath);
                logger.Log("Launching BatGui.exe");
            }
            else
            {
                logger.LogError("BatGui.exe not found.");
            }
        }

        public void ShowConfigWindow()
        {
            try 
            {
                var configForm = new ConfigurationForm(config, logger);
                configForm.ShowDialog();
            }
            catch (Exception ex)
            {
                logger.LogError("Error opening configuration window", ex);
                MessageBox.Show($"Unable to open configuration window: {ex.Message}", 
                    "Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }

        public void OpenMappingConfiguration()
        {
            ButtonMapping mapping = new ButtonMapping();
            string exePath = AppContext.BaseDirectory;
            string parentPath = Path.GetDirectoryName(exePath) ?? exePath;
            string jsonPath = Path.Combine(parentPath, "buttonMappings.json");
            mapping.LoadMappings(jsonPath);
            MappingConfigurationForm configForm = new MappingConfigurationForm(mapping);
            configForm.ShowDialog();

            // Recharger les mappings après la configuration
            LoadDirectInputMappings();
        }

        public void OpenLogFile()
        {
            try 
            {
                // Get the full path to the log file
                string logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
                string logFilePath = Path.Combine(logDirectory, "BatRun.log");

                // Check if the file exists
                if (File.Exists(logFilePath))
                {
                    // Open the file with the default text editor
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = logFilePath,
                        UseShellExecute = true
                    });
                    
                    logger.Log($"Opening log file: {logFilePath}");
                }
                else
                {
                    logger.LogError($"Log file not found: {logFilePath}");
                    MessageBox.Show($"Log file not found: {logFilePath}", "File not found", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error opening log file", ex);
                MessageBox.Show($"Error opening log file: {ex.Message}", "Error");
            }
        }

        public void OpenErrorLogFile()
        {
            try 
            {
                string logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
                string errorLogPath = Path.Combine(logDirectory, "BatRun_error.log");

                // Créer le fichier s'il n'existe pas
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                if (!File.Exists(errorLogPath))
                {
                    File.WriteAllText(errorLogPath, $"Error log file created on {DateTime.Now}\n");
                }

                // Ouvrir le fichier avec l'éditeur par défaut
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = errorLogPath,
                    UseShellExecute = true
                });
                
                logger.Log($"Opening error log file: {errorLogPath}");
            }
            catch (Exception ex)
            {
                logger.LogError("Error opening error log file", ex);
                MessageBox.Show($"Error opening error log file: {ex.Message}", "Error");
            }
        }

        public void ShowAbout(object? sender, EventArgs e)
        {
            var strings = new LocalizedStrings();

            var aboutForm = new Form
            {
                Text = strings.AboutBatRun,
                Size = new Size(400, 300),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.White
            };

            var mainText = new Label
            {
                Text = $"BatRun\n\n" +
                       $"{strings.VersionLabel}: {APP_VERSION}\n" +
                       $"{strings.DevelopedBy}\n\n" +
                       $"{strings.Description}",
                Dock = DockStyle.Top,
                Height = 150,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.White
            };

            var discordLink = new LinkLabel
            {
                Text = strings.JoinDiscord,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 10F),
                LinkColor = Color.FromArgb(114, 137, 218), // Discord color
                ActiveLinkColor = Color.FromArgb(134, 157, 238),
                VisitedLinkColor = Color.FromArgb(114, 137, 218)
            };
            discordLink.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://discord.com/invite/k8mg99cY6F",
                UseShellExecute = true
            });

            var githubLink = new LinkLabel
            {
                Text = strings.SourceCode,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 10F),
                LinkColor = Color.White,
                ActiveLinkColor = Color.LightGray,
                VisitedLinkColor = Color.White
            };
            githubLink.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/Aynshe/BatRun",
                UseShellExecute = true
            });

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Bottom,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };
            okButton.FlatAppearance.BorderSize = 0;

            aboutForm.Controls.AddRange(new Control[] { mainText, discordLink, githubLink, okButton });
            
            logger.Log("Showing about window");
            aboutForm.ShowDialog();
        }

        private bool IsEmulationStationRunning()
        {
            try 
            {
                var processes = Process.GetProcessesByName("emulationstation");
                return processes.Length > 0 && !processes[0].HasExited;
            }
            catch (Exception ex)
            {
                LogErrorWithException("Error checking EmulationStation", ex);
                return false;
            }
        }

        private bool IsRetrobatRunning()
        {
            try 
            {
                var processes = Process.GetProcessesByName("retrobat");
                return processes.Length > 0 && !processes[0].HasExited;
            }
            catch (Exception ex)
            {
                LogErrorWithException("Error checking Retrobat", ex);
                return false;
            }
        }

        public static string GetRetrobatPath()
        {
            return retrobatPath;
        }

        private void LogErrorWithException(string message, Exception? ex = null)
        {
            if (ex != null)
            {
                logger.LogError($"{message}: {ex.Message}", ex);
            }
            else
            {
                logger.LogError(message);
            }
        }

        private void InitializeRetrobatPath()
        {
            logger.Log("Searching for RetroBat path");
            try 
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\RetroBat");
                if (key != null)
                {
                    var path = key.GetValue("LatestKnownInstallPath") as string;
                    if (!string.IsNullOrEmpty(path))
                    {
                        retrobatPath = Path.Combine(path, "retrobat.exe");
                        logger.Log($"RetroBat path found in registry: {retrobatPath}");

                        // Lire le fichier retrobat.ini
                        string iniPath = Path.Combine(path, "retrobat.ini");
                        if (File.Exists(iniPath))
                        {
                            var lines = File.ReadAllLines(iniPath);
                            string? enableIntro = lines.FirstOrDefault(line => line.StartsWith("EnableIntro="))?.Split('=')[1];
                            string? videoDuration = lines.FirstOrDefault(line => line.StartsWith("VideoDuration="))?.Split('=')[1];

                            if (enableIntro == "1" && videoDuration != null && int.TryParse(videoDuration, out int duration))
                            {
                                logger.LogInfo($"Waiting for intro video duration: {duration} ms");
                                Task.Delay(duration).Wait(); // Attendre la durée de la vidéo
                            }
                            else
                            {
                                logger.LogInfo("Intro video not enabled, proceeding without delay.");
                            }
                        }
                        else
                        {
                            logger.LogError("Retrobat.ini file not found.");
                        }
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error reading registry", ex);
            }

            retrobatPath = @"C:\Retrobat\retrobat.exe"; // Default path if not found in the registry
            logger.Log($"Using default path: {retrobatPath}");

            if (!File.Exists(retrobatPath))
            {
                var error = $"Unable to find RetroBat at {retrobatPath}";
                logger.LogError(error);
                MessageBox.Show(error, "BatRun", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private async Task CheckIntroSettings()
        {
            string retrobatIniPath = Path.Combine(Path.GetDirectoryName(retrobatPath) ?? string.Empty, "retrobat.ini"); 
            if (File.Exists(retrobatIniPath))
            {
                var lines = File.ReadAllLines(retrobatIniPath);
                string? enableIntro = lines.FirstOrDefault(line => line.StartsWith("EnableIntro="))?.Split('=')[1];
                string? videoDuration = lines.FirstOrDefault(line => line.StartsWith("VideoDuration="))?.Split('=')[1];

                if (enableIntro == "1" && videoDuration != null && int.TryParse(videoDuration, out int duration))
                {
                    logger.LogInfo($"Waiting for intro video duration: {duration} ms");
                    await Task.Delay(duration);
                }
                else
                {
                    logger.LogInfo("Intro video not enabled, proceeding without delay.");
                }
            }
            else
            {
                logger.LogError("Retrobat.ini file not found.");
            }
        }

        private async Task StartEmulationStation()
        {
            const int maxWaitTime = 30; // Temps maximum d'attente en secondes
            int elapsedTime = 0;
            int checkInterval = 1000; // Vérification toutes les secondes

            while (elapsedTime < maxWaitTime)
            {
                if (IsEmulationStationRunning())
                {
                    // Lancer EmulationStation
                    LaunchEmulationStation();
                    return; // Sortir de la méthode si EmulationStation a été lancé
                }

                // Log de la tentative
                logger.LogInfo($"EmulationStation not found, retrying in {checkInterval / 1000} seconds...");
                await Task.Delay(checkInterval); // Attendre avant de vérifier à nouveau
                elapsedTime += checkInterval / 1000; // Convertir en secondes
            }

            logger.LogError("EmulationStation failed to start after 30 seconds.");
        }

        private void LogEmulationStationPolling()
        {
            if (!_emulationStationPollingLogged)
            {
                logger.LogInfo("EmulationStation is running, suspending polling");
                _emulationStationPollingLogged = true;
            }
        }

        private void LoadDirectInputMappings()
        {
            string exePath = AppContext.BaseDirectory;
            string parentPath = Path.GetDirectoryName(exePath) ?? exePath;
            string jsonPath = Path.Combine(parentPath, "buttonMappings.json");

            if (File.Exists(jsonPath))
            {
                var jsonData = File.ReadAllText(jsonPath);
                var mapping = JsonConvert.DeserializeObject<ButtonMapping>(jsonData);
                if (mapping != null && mapping.Controllers.Any())
                {
                    logger.LogInfo($"DirectInput mappings loaded for {mapping.Controllers.Count} controllers");
                    foreach (var controller in mapping.Controllers)
                    {
                        logger.LogInfo($"Loaded mapping for controller: {controller.JoystickName}");
                    }
                }
                directInputMapping = mapping;
            }
            else
            {
                logger.LogInfo("No DirectInput mappings found");
            }
        }

        private void ShowMainForm()
        {
            if (mainForm == null || mainForm.IsDisposed)
            {
                mainForm = new MainForm(this, logger, config);
                
                // S'assurer que l'icône est héritée du programme principal
                if (this.Icon != null)
                {
                    mainForm.Icon = (Icon)this.Icon.Clone();
                }
            }

            // Configurer l'affichage de la fenêtre
            mainForm.Show();
            mainForm.WindowState = FormWindowState.Normal;
            mainForm.ShowInTaskbar = true;
            mainForm.BringToFront();
            mainForm.Activate();
        }

        public async Task CheckForUpdates()
        {
            try
            {
                logger.Log("Checking for updates...");
                var updateChecker = new UpdateChecker(logger, APP_VERSION);
                var result = await updateChecker.CheckForUpdates();
                
                if (result.UpdateAvailable)
                {
                    logger.Log($"Update available: version {result.LatestVersion}");
                }
                else
                {
                    logger.Log("No updates available");
                }
            }
            catch (Exception ex)
            {
                LogErrorWithException("Error checking for updates", ex);
            }
        }

        public string GetAppVersion()
        {
            return APP_VERSION;
        }

        [STAThread]
        static void Main()
        {
            Logger? logger = null;
            try 
            {
                // Setup global exception handling
                AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
                Application.ThreadException += ApplicationOnThreadException;
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

                // Initialize logger as early as possible
                logger = new Logger("BatRun.log");
                logger.LogInfo("Application starting");

                // Prevent multiple instances
                bool createdNew;
                using (Mutex mutex = new Mutex(true, "BatRun", out createdNew))
                {
                    if (!createdNew)
                    {
                        logger.LogInfo("Another instance is already running");
                        MessageBox.Show("Another instance of BatRun is already running.", 
                            "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    Application.SetHighDpiMode(HighDpiMode.SystemAware);
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    // Créer et afficher le splash screen
                    var splash = new SplashForm();
                    splash.Show();
                    splash.UpdateStatus("Initializing...");

                    // Créer le programme principal
                    var program = new Program();
                    program.mainForm = new MainForm(program, logger, program.config);
                    
                    // Configurer le programme
                    program.ShowInTaskbar = false;
                    program.Visible = false;

                    // Démarrer le polling en arrière-plan
                    splash.UpdateStatus("Starting controller service...");
                    program.StartPolling();

                    // Attendre un peu pour montrer le splash
                    Thread.Sleep(2000);
                    splash.UpdateStatus("Ready!");
                    Thread.Sleep(500);

                    // Fermer le splash et démarrer l'application
                    splash.Close();
                    splash.Dispose();
                    
                    Application.Run(program);
                }
            }
            catch (Exception ex)
            {
                LogFatalError("Unhandled exception in Main", ex);
            }
            finally 
            {
                logger?.LogInfo("Application closing");
            }
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try 
            {
                var ex = e.ExceptionObject as Exception;
                LogFatalError("Unhandled domain-level exception", ex);

                // Optionally show a message box to the user
                if (ex != null)
                {
                    MessageBox.Show(
                        $"A critical error occurred: {ex.Message}\n\nThe application will now close.", 
                        "Critical Error", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Error
                    );
                }
            }
            catch 
            {
                // Absolute last resort logging
                File.WriteAllText(
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                        "BatRun_CriticalError.log"
                    ), 
                    $"Unhandled exception at {DateTime.Now}: {e.ExceptionObject}"
                );
            }
        }

        private static void ApplicationOnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            LogFatalError("Unhandled thread exception", e.Exception);
            
            MessageBox.Show(
                $"A critical error occurred: {e.Exception.Message}\n\nThe application will now close.", 
                "Critical Error", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Error
            );
        }

        private static void LogFatalError(string message, Exception? ex = null)
        {
            try 
            {
                // Ensure the Logs directory exists
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                // Use a new logger instance to ensure logging works even if the main logger fails
                var errorLogger = new Logger("BatRun_error.log");
                errorLogger.LogCriticalError(message);
                
                if (ex != null)
                {
                    errorLogger.LogCriticalError($"Exception Details: {ex.Message}");
                    errorLogger.LogCriticalError($"Stack Trace: {ex.StackTrace}");
                    
                    // Log inner exceptions
                    var innerEx = ex.InnerException;
                    while (innerEx != null)
                    {
                        errorLogger.LogCriticalError($"Inner Exception: {innerEx.Message}");
                        errorLogger.LogCriticalError($"Inner Exception Stack Trace: {innerEx.StackTrace}");
                        innerEx = innerEx.InnerException;
                    }
                }
            }
            catch 
            {
                // Absolute last resort logging
                string fallbackLogPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    "BatRun_LoggingError.log"
                );
                File.WriteAllText(
                    fallbackLogPath, 
                    $"Logging failed at {DateTime.Now}: {message}\n{ex}"
                );
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                memoryMonitorTimer?.Stop();
                memoryMonitorTimer?.Dispose();
                logger.Log("Exiting application");
                trayIcon?.Dispose();
                CleanupControllers();
                ForceGarbageCollection();
            }
            base.Dispose(disposing);
        }
    }

    public class LoggingConfig
    {
        public bool IsDetailedLoggingEnabled { get; set; } = false;
    }
}
