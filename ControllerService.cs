using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BatRun.Properties;
using Newtonsoft.Json;
using SDL2;
using SharpDX.XInput;

namespace BatRun
{
    public class ControllerService : IDisposable
    {
        private readonly Logger _logger;
        private readonly IniFile _config;
        private CancellationTokenSource? _pollingCancellation;
        private readonly List<GameController> _controllers = new();
        private ButtonMapping? _directInputMapping;
        private SDLGameControllerDB? _sdlDb;

        public event EventHandler? HotkeyCombinationPressed;

        private class GameController
        {
            public IntPtr Handle { get; }
            public int Index { get; }
            public bool IsConnected { get; set; }

            public GameController(IntPtr handle, int index)
            {
                Handle = handle;
                Index = index;
                IsConnected = true;
            }
        }

        private readonly string _retrobatPath;

        public ControllerService(Logger logger, IniFile config, string retrobatPath)
        {
            _logger = logger;
            _config = config;
            _retrobatPath = retrobatPath;
        }

        public void Initialize()
        {
            InitializeControllers();
            StartPolling();
        }

        private void InitializeControllers()
        {
            string sdlDllPath = Path.Combine(AppContext.BaseDirectory, "SDL2.dll");
            if (!File.Exists(sdlDllPath))
            {
                _logger.LogError("SDL2.dll not found. Please ensure SDL2 is properly installed.");
                throw new FileNotFoundException("SDL2.dll not found.");
            }

            if (SDL.SDL_Init(SDL.SDL_INIT_GAMECONTROLLER | SDL.SDL_INIT_HAPTIC) < 0)
            {
                var error = SDL.SDL_GetError();
                _logger.LogError($"SDL initialization failed: {error}", new Exception(error));
                return;
            }

            _sdlDb = new SDLGameControllerDB(_logger);
            if (!string.IsNullOrEmpty(_retrobatPath))
            {
                _sdlDb.LoadDatabase(_retrobatPath);
            }
            else
            {
                _logger.LogError("RetroBat path is not set");
            }

            int numJoysticks = SDL.SDL_NumJoysticks();
            _logger.LogInfo($"Found {numJoysticks} joysticks");

            string exePath = AppContext.BaseDirectory;
            string parentPath = Path.GetDirectoryName(exePath) ?? exePath;
            string jsonPath = Path.Combine(parentPath, "buttonMappings.json");
            _directInputMapping = new ButtonMapping();
            if (File.Exists(jsonPath))
            {
                var jsonData = File.ReadAllText(jsonPath);
                _directInputMapping = JsonConvert.DeserializeObject<ButtonMapping>(jsonData) ?? new ButtonMapping();
            }

            for (int i = 0; i < numJoysticks; i++)
            {
                _logger.LogInfo($"Initializing controller {i}");

                if (SDL.SDL_IsGameController(i) == SDL.SDL_bool.SDL_TRUE)
                {
                    IntPtr controller = SDL.SDL_GameControllerOpen(i);
                    if (controller != IntPtr.Zero)
                    {
                        _controllers.Add(new GameController(controller, i));
                        _logger.LogInfo($"Controller {i} initialized successfully");

                        IntPtr joystick = SDL.SDL_GameControllerGetJoystick(controller);
                        if (joystick != IntPtr.Zero)
                        {
                            string controllerName = SDL.SDL_JoystickName(joystick);
                            string deviceGuid = SDL.SDL_JoystickGetGUID(joystick).ToString();

                            var existingMapping = _directInputMapping.Controllers.FirstOrDefault(c =>
                                c.JoystickName == controllerName && c.DeviceGuid == deviceGuid);

                            if (existingMapping == null)
                            {
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

                                _directInputMapping.Controllers.Add(newMapping);
                                _logger.LogInfo($"Created default mapping for XInput controller: {controllerName}");
                            }
                        }
                    }
                }
                else
                {
                    IntPtr joystick = SDL.SDL_JoystickOpen(i);
                    if (joystick != IntPtr.Zero)
                    {
                        try
                        {
                            string controllerName = SDL.SDL_JoystickName(joystick);
                            var sdlGuid = SDL.SDL_JoystickGetGUID(joystick);
                            string rawGuid = sdlGuid.ToString();

                            byte[] guidBytes = new byte[16];
                            sdlGuid.ToByteArray().CopyTo(guidBytes, 0);
                            string sdlGuidStr = "03000000" + BitConverter.ToString(guidBytes).Replace("-", "").ToLower().Substring(8);

                            _logger.LogInfo($"DirectInput controller found: {controllerName}");
                            _logger.LogInfo($"Raw GUID: {rawGuid}");
                            _logger.LogInfo($"SDL GUID: {sdlGuidStr}");

                            var existingMapping = _directInputMapping.Controllers.FirstOrDefault(c =>
                                c.JoystickName == controllerName && c.DeviceGuid == rawGuid);

                            if (existingMapping == null)
                            {
                                var sdlMapping = _sdlDb?.FindMappingByGuid(sdlGuidStr) ?? _sdlDb?.FindMappingByName(controllerName);

                                if (sdlMapping != null)
                                {
                                    _logger.LogInfo($"Found SDL mapping for {controllerName}");
                                    _logger.LogInfo($"SDL DB GUID: {sdlMapping.Guid}");
                                    if (sdlMapping.HasBackAndStartButtons)
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

                                        _directInputMapping.Controllers.Add(newMapping);
                                        _logger.LogInfo($"Created mapping from SDL DB for {controllerName}:");
                                        _logger.LogInfo($"  Back: Button {sdlMapping.ButtonMappings["back"]}");
                                        _logger.LogInfo($"  Start: Button {sdlMapping.ButtonMappings["start"]}");
                                    }
                                    else
                                    {
                                        _logger.LogInfo($"SDL mapping found but missing back/start buttons for {controllerName}");
                                    }
                                }
                                else
                                {
                                    _logger.LogInfo($"No SDL mapping found for {controllerName} with GUID {sdlGuidStr}");
                                }
                            }
                            else
                            {
                                _logger.LogInfo($"Using existing custom mapping for {controllerName}");
                            }
                        }
                        finally
                        {
                            SDL.SDL_JoystickClose(joystick);
                        }
                    }
                }
            }

            if (_directInputMapping.Controllers.Any())
            {
                var json = JsonConvert.SerializeObject(_directInputMapping, Formatting.Indented);
                File.WriteAllText(jsonPath, json);
                _logger.LogInfo("Controller mappings saved to JSON");
            }
        }

        private void StartPolling()
        {
            try
            {
                if (_pollingCancellation != null)
                {
                    _pollingCancellation.Cancel();
                    _pollingCancellation.Dispose();
                }

                _pollingCancellation = new CancellationTokenSource();

                LoadDirectInputMappings();

                Task.Run(() => PollControllersAsync(_pollingCancellation.Token), _pollingCancellation.Token);

                _logger.LogInfo("Controller polling started");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error starting polling: {ex.Message}");
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

                    int currentJoystickCount = SDL.SDL_NumJoysticks();
                    if (currentJoystickCount != lastJoystickCount)
                    {
                        _logger.LogInfo($"Number of joysticks changed from {lastJoystickCount} to {currentJoystickCount}");
                        lastJoystickCount = currentJoystickCount;

                        for (int i = 0; i < currentJoystickCount; i++)
                        {
                            if (SDL.SDL_IsGameController(i) == SDL.SDL_bool.SDL_TRUE)
                            {
                                IntPtr controller = SDL.SDL_GameControllerOpen(i);
                                if (controller != IntPtr.Zero)
                                {
                                    _controllers.Add(new GameController(controller, i));
                                    _logger.LogInfo($"Controller {i} connected and initialized");

                                    IntPtr joystick = SDL.SDL_GameControllerGetJoystick(controller);
                                    if (joystick != IntPtr.Zero)
                                    {
                                        InitializeController(joystick, true);
                                    }
                                }
                            }
                            else
                            {
                                IntPtr joystick = SDL.SDL_JoystickOpen(i);
                                if (joystick != IntPtr.Zero)
                                {
                                    try
                                    {
                                        string controllerName = SDL.SDL_JoystickName(joystick);
                                        var sdlGuid = SDL.SDL_JoystickGetGUID(joystick);
                                        string rawGuid = sdlGuid.ToString();

                                        var existingMapping = _directInputMapping?.Controllers.FirstOrDefault(c =>
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
                            var controller = _controllers.FirstOrDefault(c => c.Handle == removedController);
                            if (controller != null)
                            {
                                SDL.SDL_GameControllerClose(controller.Handle);
                                _controllers.Remove(controller);
                                _logger.LogInfo($"Controller {controller.Index} disconnected");
                            }
                        }
                    }

                    if (IsEmulationStationRunning())
                    {
                        continue;
                    }

                    for (int i = 0; i < SDL.SDL_NumJoysticks(); i++)
                    {
                        IntPtr joystick = SDL.SDL_JoystickOpen(i);
                        if (joystick != IntPtr.Zero)
                        {
                            try
                            {
                                string joystickName = SDL.SDL_JoystickName(joystick);
                                string deviceGuid = SDL.SDL_JoystickGetGUID(joystick).ToString();

                                var controllerConfig = _directInputMapping?.Controllers.FirstOrDefault(c =>
                                    c.JoystickName == joystickName && c.DeviceGuid == deviceGuid);

                                if (controllerConfig?.Mappings != null)
                                {
                                    if (controllerConfig.Mappings.TryGetValue("Hotkey", out string? hotkeyValue) &&
                                        controllerConfig.Mappings.TryGetValue("StartButton", out string? startValue))
                                    {
                                        bool isHotkeyPressed;
                                        bool isStartPressed;

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
                                        else
                                        {
                                            int hotkeyButton = int.Parse(hotkeyValue.Replace("Button ", ""));
                                            int startButton = int.Parse(startValue.Replace("Button ", ""));
                                            isHotkeyPressed = SDL.SDL_JoystickGetButton(joystick, hotkeyButton) == 1;
                                            isStartPressed = SDL.SDL_JoystickGetButton(joystick, startButton) == 1;
                                        }

                                        if (isHotkeyPressed && isStartPressed)
                                        {
                                            _logger.LogInfo($"Hotkey combination detected for {joystickName}");

                                            bool enableVibration = _config.ReadBool("Controller", "EnableVibration", true);

                                            if (enableVibration)
                                            {
                                                VibrateController(hotkeyValue, startValue);
                                            }

                                            HotkeyCombinationPressed?.Invoke(this, EventArgs.Empty);
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
                    _logger.LogError($"Error during polling: {ex.Message}", ex);
                }
            }
        }

        private async void VibrateController(string hotkeyValue, string startValue)
        {
            try
            {
                if (hotkeyValue == "Back" && startValue == "Start")
                {
                    foreach (UserIndex index in Enum.GetValues(typeof(UserIndex)))
                    {
                        try
                        {
                            var xinputController = new Controller(index);
                            if (xinputController.IsConnected)
                            {
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

                                    _logger.LogInfo($"XInput controller rumble activated on index {index}");
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error checking XInput controller at index {index}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error activating rumble: {ex.Message}");
            }
        }

        private void InitializeController(IntPtr joystick, bool isXInput)
        {
            string controllerName = SDL.SDL_JoystickName(joystick);
            var sdlGuid = SDL.SDL_JoystickGetGUID(joystick);
            string rawGuid = sdlGuid.ToString();

            string sdlGuidStr = rawGuid;
            if (!isXInput)
            {
                byte[] guidBytes = new byte[16];
                sdlGuid.ToByteArray().CopyTo(guidBytes, 0);
                sdlGuidStr = "03000000" + BitConverter.ToString(guidBytes).Replace("-", "").ToLower().Substring(8);
            }

            _logger.LogInfo($"{(isXInput ? "XInput" : "DirectInput")} controller connected: {controllerName}");
            if (!isXInput)
            {
                _logger.LogInfo($"Raw GUID: {rawGuid}");
                _logger.LogInfo($"SDL GUID: {sdlGuidStr}");
            }

            var existingMapping = _directInputMapping?.Controllers.FirstOrDefault(c =>
                c.JoystickName == controllerName && c.DeviceGuid == rawGuid);

            if (existingMapping == null)
            {
                if (isXInput)
                {
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
                    _directInputMapping?.Controllers.Add(newMapping);
                    _logger.LogInfo($"Created default mapping for XInput controller: {controllerName}");
                    SaveControllerMappings();
                }
                else
                {
                    var sdlMapping = _sdlDb?.FindMappingByGuid(sdlGuidStr) ?? _sdlDb?.FindMappingByName(controllerName);

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
                        _directInputMapping?.Controllers.Add(newMapping);
                        _logger.LogInfo($"Created mapping from SDL DB for {controllerName}");
                        SaveControllerMappings();
                    }
                }
            }
        }

        private void SaveControllerMappings()
        {
            if (_directInputMapping?.Controllers.Any() == true)
            {
                string exePath = AppContext.BaseDirectory;
                string parentPath = Path.GetDirectoryName(exePath) ?? exePath;
                string jsonPath = Path.Combine(parentPath, "buttonMappings.json");
                var json = JsonConvert.SerializeObject(_directInputMapping, Formatting.Indented);
                File.WriteAllText(jsonPath, json);
                _logger.LogInfo("Controller mappings saved to JSON");
            }
        }

        public void LoadDirectInputMappings()
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
                    _logger.LogInfo($"DirectInput mappings loaded for {mapping.Controllers.Count} controllers");
                    foreach (var controller in mapping.Controllers)
                    {
                        _logger.LogInfo($"Loaded mapping for controller: {controller.JoystickName}");
                    }
                }
                _directInputMapping = mapping;
            }
            else
            {
                _logger.LogInfo("No DirectInput mappings found");
            }
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
                _logger.LogError("Error checking EmulationStation", ex);
                return false;
            }
        }

        public void Dispose()
        {
            _pollingCancellation?.Cancel();
            _pollingCancellation?.Dispose();

            foreach (var controller in _controllers)
            {
                SDL.SDL_GameControllerClose(controller.Handle);
            }
            _controllers.Clear();
            SDL.SDL_Quit();
            _logger.Log("ControllerService disposed and SDL quit.");
        }
    }
}
