using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BatRun;
using Newtonsoft.Json;
using SDL2;
using SharpDX.XInput;

namespace BatRun
{
    public class ControllerService
    {
        private readonly Logger logger;
        private readonly Action onHotkeyCombination;
        private readonly IniFile config;
        private readonly string retrobatPath;
        private CancellationTokenSource? pollingCancellation;
        private List<GameController> controllers = new List<GameController>();
        private ButtonMapping? directInputMapping;
        private SDLGameControllerDB? sdlDb;

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

        public ControllerService(Logger logger, Action onHotkeyCombination, IniFile config, string retrobatPath)
        {
            this.logger = logger;
            this.onHotkeyCombination = onHotkeyCombination;
            this.config = config;
            this.retrobatPath = retrobatPath;
        }

        public void Initialize()
        {
            logger.Log("Initializing Controller Service...");
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

            sdlDb = new SDLGameControllerDB(logger);
            if (!string.IsNullOrEmpty(retrobatPath))
            {
                string retrobatDir = Path.GetDirectoryName(retrobatPath) ?? "";
                sdlDb.LoadDatabase(retrobatDir);
            }
            else
            {
                logger.LogError("RetroBat path is not set for controller DB");
            }

            int numJoysticks = SDL.SDL_NumJoysticks();
            logger.LogInfo($"Found {numJoysticks} joysticks");

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
                InitializeControllerByIndex(i);
            }

            if (directInputMapping.Controllers.Any())
            {
                var json = JsonConvert.SerializeObject(directInputMapping, Formatting.Indented);
                File.WriteAllText(jsonPath, json);
                logger.LogInfo("Controller mappings saved to JSON");
            }
        }

        private void InitializeControllerByIndex(int i)
        {
            logger.LogInfo($"Initializing controller {i}");
            if (SDL.SDL_IsGameController(i) == SDL.SDL_bool.SDL_TRUE)
            {
                IntPtr controller = SDL.SDL_GameControllerOpen(i);
                if (controller != IntPtr.Zero)
                {
                    controllers.Add(new GameController(controller, i));
                    logger.LogInfo($"Controller {i} initialized successfully");

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
                        InitializeController(joystick, false);
                    }
                    finally
                    {
                        SDL.SDL_JoystickClose(joystick);
                    }
                }
            }
        }

        public void StartPolling()
        {
            logger.Log("Starting controller polling...");
            if (pollingCancellation != null)
            {
                pollingCancellation.Cancel();
                pollingCancellation.Dispose();
            }
            pollingCancellation = new CancellationTokenSource();
            LoadDirectInputMappings();
            Task.Run(() => PollControllersAsync(pollingCancellation.Token), pollingCancellation.Token);
        }

        public void StopPolling()
        {
            logger.Log("Stopping controller polling...");
            pollingCancellation?.Cancel();
        }

        private async Task PollControllersAsync(CancellationToken cancellationToken)
        {
            const int POLL_INTERVAL_MS = 100;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(POLL_INTERVAL_MS, cancellationToken);
                    SDL.SDL_PumpEvents();

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

                                if (controllerConfig?.Mappings != null &&
                                    controllerConfig.Mappings.TryGetValue("Hotkey", out string? hotkeyValue) &&
                                    controllerConfig.Mappings.TryGetValue("StartButton", out string? startValue))
                                {
                                    bool isHotkeyPressed = false;
                                    bool isStartPressed = false;

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
                                            finally { SDL.SDL_GameControllerClose(controller); }
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
                                        logger.LogInfo($"Hotkey combination detected for {joystickName}");
                                        VibrateController(i, hotkeyValue == "Back");
                                        onHotkeyCombination();
                                    }
                                }
                            }
                            finally { SDL.SDL_JoystickClose(joystick); }
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { logger.LogError($"Error during polling: {ex.Message}", ex); }
            }
        }

        private async void VibrateController(int joystickIndex, bool isXInput)
        {
            if (!config.ReadBool("Controller", "EnableVibration", true)) return;

            try
            {
                if (isXInput)
                {
                    // For XInput, we have to find the correct UserIndex
                    foreach (UserIndex index in Enum.GetValues(typeof(UserIndex)))
                    {
                        var xinputController = new Controller(index);
                        if (xinputController.IsConnected)
                        {
                            var state = xinputController.GetState();
                            if (((state.Gamepad.Buttons & GamepadButtonFlags.Back) != 0) &&
                                ((state.Gamepad.Buttons & GamepadButtonFlags.Start) != 0))
                            {
                                var vibration = new Vibration { LeftMotorSpeed = 65535, RightMotorSpeed = 65535 };
                                xinputController.SetVibration(vibration);
                                await Task.Delay(500);
                                xinputController.SetVibration(new Vibration());
                                logger.LogInfo($"XInput controller rumble activated on index {index}");
                                break;
                            }
                        }
                    }
                }
                else
                {
                    // For DirectInput, use SDL Haptic
                    IntPtr haptic = SDL.SDL_HapticOpen(joystickIndex);
                    if (haptic != IntPtr.Zero)
                    {
                        try
                        {
                            if (SDL.SDL_HapticRumbleInit(haptic) == 0)
                            {
                                SDL.SDL_HapticRumblePlay(haptic, 0.75f, 500);
                                logger.LogInfo($"DirectInput controller rumble activated on index {joystickIndex}");
                            }
                        }
                        finally { SDL.SDL_HapticClose(haptic); }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error activating rumble: {ex.Message}");
            }
        }

        private void InitializeController(IntPtr joystick, bool isXInput)
        {
            string controllerName = SDL.SDL_JoystickName(joystick);
            var sdlGuid = SDL.SDL_JoystickGetGUID(joystick);
            string rawGuid = sdlGuid.ToString();
            string sdlGuidStr = isXInput ? rawGuid : "03000000" + BitConverter.ToString(sdlGuid.ToByteArray()).Replace("-", "").ToLower().Substring(8);

            logger.LogInfo($"{(isXInput ? "XInput" : "DirectInput")} controller connected: {controllerName}");

            var existingMapping = directInputMapping?.Controllers.FirstOrDefault(c => c.JoystickName == controllerName && c.DeviceGuid == rawGuid);
            if (existingMapping == null)
            {
                if (isXInput)
                {
                    var newMapping = new ControllerConfig { JoystickName = controllerName, DeviceGuid = rawGuid, Mappings = new Dictionary<string, string> { { "Hotkey", "Back" }, { "StartButton", "Start" } } };
                    directInputMapping?.Controllers.Add(newMapping);
                    logger.LogInfo($"Created default mapping for XInput controller: {controllerName}");
                }
                else
                {
                    var sdlMapping = sdlDb?.FindMappingByGuid(sdlGuidStr) ?? sdlDb?.FindMappingByName(controllerName);
                    if (sdlMapping != null && sdlMapping.HasBackAndStartButtons)
                    {
                        var newMapping = new ControllerConfig { JoystickName = controllerName, DeviceGuid = rawGuid, Mappings = new Dictionary<string, string> { { "Hotkey", $"Button {sdlMapping.ButtonMappings["back"]}" }, { "StartButton", $"Button {sdlMapping.ButtonMappings["start"]}" } } };
                        directInputMapping?.Controllers.Add(newMapping);
                        logger.LogInfo($"Created mapping from SDL DB for {controllerName}");
                    }
                }
                SaveControllerMappings();
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
                directInputMapping = JsonConvert.DeserializeObject<ButtonMapping>(jsonData);
            }
            else
            {
                directInputMapping = new ButtonMapping();
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

        public void Cleanup()
        {
            logger.Log("Cleaning up Controller Service...");
            StopPolling();
            foreach (var controller in controllers)
            {
                SDL.SDL_GameControllerClose(controller.Handle);
            }
            controllers.Clear();
            SDL.SDL_Quit();
        }
    }
}
