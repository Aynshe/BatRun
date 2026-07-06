using System;
using System.Collections.Generic;
using Microsoft.Win32;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.Core
{
    public class ArcadeManager : IDisposable
    {
        public event EventHandler? SessionStarted;
        public event EventHandler? InterfaceRequested;
        public Logger GetLogger() => _logger;

        private readonly Logger _logger;
        private readonly IniFile _config;
        public IniFile Config => _config;
        private readonly ControllerService _controllerService;

        private ArcadeOverlayForm? _overlay;
        // private OperatorMiniOverlayForm? _miniOverlay; // [DELETE]
        private RawInputHandler? _rawInputHandler;
        
        private int _totalCredits = 0;
        private int _sessionSecondsRemaining = 0;
        private bool _isSessionActive = false;
        private bool _isSessionRestored = false;
        private int _restorationGraceTicks = 0; // EN: Ticks where countdown is allowed even if ES not detected / FR: Ticks oÃ¹ le dÃ©compte est autorisÃ© mÃªme si ES non dÃ©tectÃ©
        private bool _isFreePlay = false;
        private bool _isOperatorUnlocked = false;
        private bool _isEsRunning = false;
        private bool _isOperatorPromptOpen = false;
        private System.Windows.Forms.Form? _activeModalForm;

        private void CloseActiveModal()
        {
            try
            {
                if (_activeModalForm != null && !_activeModalForm.IsDisposed)
                {
                    if (_activeModalForm.InvokeRequired)
                    {
                        _activeModalForm.BeginInvoke(new Action(() => { try { _activeModalForm?.Close(); } catch { } }));
                    }
                    else
                    {
                        try { _activeModalForm.Close(); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to close active modal dynamically", ex);
            }
        }
        public bool IsInternalClosing { get; private set; } = false;
        public bool IsGameSuspended => _lastPausedPid > 0;
        
        private System.Threading.Timer? _sessionTimer;
        private System.Threading.Timer? _operatorHoldTimer;
        private System.Threading.Timer? _guardianHoldTimer;
        private bool _isOperatorKeyHeld = false;
        private bool _isGuardianKeyHeld = false;
        private ArcadeApiService? _apiService;
        public UserManager PublicUserManager { get; private set; } = null!;
        public IpBlacklistManager BlacklistManager { get; private set; } = null!;
        private System.Threading.Timer? _heartbeatTimer;
        private string _heartbeatPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "batrun.heartbeat");
        private string _sessionStatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "session_state.json");
        private string _sessionRestartToken = Guid.NewGuid().ToString("N")[..8]; // EN: Short token generated at startup / FR: Token court gÃ©nÃ©rÃ© au dÃ©marrage

        private uint _lastPausedPid = 0;
        public uint LastPausedPid => _lastPausedPid;
        private readonly MoonlightManager _moonlightManager;
        public MoonlightManager Moonlight => _moonlightManager;
        private uint _timeoutTargetPid = 0;
        private int _timeoutSecondsRemaining = 0;
        private string? _lastTimeoutKey = null;
        private bool _isTimeoutActive = false;
        private bool _isNewGameTimeout = false;
        private bool _isGameRunning = false; // EN: True if a game process is actually active / FR: Vrai si un jeu est réellement actif
        private bool _isGameLaunching = false; // EN: True during the boot phase / FR: Vrai pendant la phase de boot
        private uint _capturedPid = 0; // EN: Captured PID for watchdog / FR: PID capturé pour le watchdog
        private int _watcherCounter = 0;
        private IntPtr _lastForegroundHwnd = IntPtr.Zero;
        private string _lastForegroundProcessName = "";
        private IntPtr _currentGameHwnd = IntPtr.Zero;
        private string _currentGameSystem = "";
        private string _currentGameName = "";
        private string _currentExecutable = "";
        private string _sessionOriginExecutable = ""; // EN: Tracks the launcher exe that started the game
        private bool _allowAltTab = false;
        private string[] _allowedForegroundWindows = Array.Empty<string>();
        private DateTime _gameStartTime = DateTime.MinValue;
        public long LastGameEndUtc { get; private set; } = 0;
            public long ForceStopTimestamp { get; private set; } = 0; // EN: Timestamp of last force-stop for web client signaling / FR: Timestamp du dernier arrêt forcé pour signaler les clients web
        private bool _isHistorySaved = false;
        private string _historyFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history.xml");
        private System.Threading.CancellationTokenSource? _ipcCts;
        
        // Custom Task Switcher Variables
        private bool _isTaskSwitcherActive = false;
        private int _taskSwitcherIndex = 0;
        private System.Collections.Generic.List<(string Name, IntPtr Hwnd)> _taskSwitcherWindows = new();


        public int TotalCredits => _totalCredits;
        public bool IsFreePlay => _isFreePlay;
        public bool IsOperatorUnlocked => _isOperatorUnlocked;
        public bool IsSessionActive => _isSessionActive;
        public bool IsTimeoutActive => _isTimeoutActive;
        public IntPtr CurrentGameHwnd => _currentGameHwnd;
        public string FormattedTimeRemaining => $"{(_sessionSecondsRemaining / 60):D2}:{(_sessionSecondsRemaining % 60):D2}";
        public string CurrentGameSystem => _currentGameSystem;
        public string CurrentGameName => _currentGameName;
        public string CurrentExecutable => _currentExecutable;
        public string CurrentGameDuration => _gameStartTime == DateTime.MinValue ? "00:00" : (DateTime.Now - _gameStartTime).ToString(@"mm\:ss");
        public string OperatorPassword => _operatorPassword;
        public int MinutesPerCredit 
        { 
            get => _minutesPerCredit; 
            set 
            { 
                _minutesPerCredit = value; 
                _config.WriteValue("Arcade", "MinutesPerCredit", value.ToString());
            } 
        }

        public bool HideOperatorButtons
        {
            get => _hideOperatorButtons;
            set
            {
                _hideOperatorButtons = value;
                _config.WriteValue("Arcade", "HideOperatorButtons", value.ToString());
                // EN: Notify overlay immediately / FR: Notifier l'overlay immÃ©diatement
                if (_overlay != null && _overlay.IsHandleCreated)
                {
                    _overlay.Invoke(new Action(() => _overlay.RefreshMiniButtonsVisibility()));
                }
            }
        }
        
        public bool IsGameInProgress => _isGameRunning || _isGameLaunching;
        public bool IsWebLaunch { get; set; } = false;
        public bool IsWebUiSession { get; set; } = false; // EN: Flag for RetroBat UI session / FR: Flag pour session interface RetroBat
        
        // Configuration
        public bool IsArcadeEnabled => _isEnabled;
        public bool IsGuardianModalActive { get; set; } = false;
        public bool AllowAltTab => _allowAltTab;
        public string[] AllowedForegroundWindows => _allowedForegroundWindows;
        private bool _isEnabled;
        private string _coinDeviceHandle = "ANY";
        private ushort _coinVirtualKey;
        private int _minutesPerCredit;
        private string _operatorPassword = "";
        private double _overlayOpacity = 0.85;
        private bool _apiEnabled = false;
        private int _apiPort = 4321;
        private string _defaultMode = "None";
        private int _initialCredits = 0;
        private bool _hideOperatorButtons = false;
        private bool _publicAccessEnabled = false;
        private bool _publicAccessRequiresLogin = false;
        private bool _publicAccessAllowRegistration = true;
        private string _adminAllowedIps = "";
        private bool _moonlightStreamEnabled = false;
        private bool _httpsEnabled = false;
        private bool _proxyMoonlight = false;
        private bool _hubMode = false;
        private string _publicIp = "";

        public string PublicIp
        {
            get => _publicIp;
            set
            {
                _publicIp = value;
                _config.WriteValue("Arcade", "PublicIp", value);
            }
        }

        public bool PublicAccessEnabled 
        { 
            get => _publicAccessEnabled; 
            set 
            { 
                _publicAccessEnabled = value; 
                _config.WriteValue("Arcade", "PublicAccessEnabled", value.ToString()); 
            } 
        }

        public bool PublicAccessRequiresLogin
        {
            get => _publicAccessRequiresLogin;
            set
            {
                bool wasEnabled = _publicAccessRequiresLogin;
                _publicAccessRequiresLogin = value;
                _config.WriteValue("Arcade", "PublicAccessRequiresLogin", value.ToString());
                // EN: When switching to requireLogin=true, purge all guest accounts immediately
                // FR: Quand on passe à requireLogin=true, purger tous les comptes guest immédiatement
                if (value && !wasEnabled)
                {
                    PublicUserManager?.PurgeAllGuests();
                }
            }
        }

        public bool MoonlightStreamEnabled
        {
            get => _moonlightStreamEnabled;
            set
            {
                _moonlightStreamEnabled = value;
                _config.WriteValue("Arcade", "MoonlightStreamEnabled", value.ToString());
                // EN: If it's disabled live, stop immediately / FR: Si on le desactive en live, arrÃªter immÃ©diatement
                if (!value) StopMoonlightService();
                else StartMoonlightService();
            }
        }

        public bool HttpsEnabled
        {
            get => _httpsEnabled;
            set
            {
                _httpsEnabled = value;
                _config.WriteValue("Arcade", "HttpsEnabled", value.ToString());
            }
        }

        public bool ProxyMoonlight
        {
            get => _proxyMoonlight;
            set
            {
                _proxyMoonlight = value;
                _config.WriteValue("Arcade", "ProxyMoonlight", value.ToString());
            }
        }

        // EN: Hub Mode — this machine is the single entry point for external access.
        // All relay targets use machine aliases instead of raw IP:port, hiding internal IPs.
        // FR: Mode Hub — cette machine est le point d'entrée unique pour l'accès externe.
        // Tous les cibles relay utilisent des alias de machine au lieu d'IP:port brutes, masquant les IPs internes.
        public bool HubMode
        {
            get => _hubMode;
            set
            {
                _hubMode = value;
                _config.WriteValue("Arcade", "HubMode", value.ToString());
            }
        }

        public bool PublicAccessAllowRegistration
        {
            get => _publicAccessAllowRegistration;
            set
            {
                _publicAccessAllowRegistration = value;
                _config.WriteValue("Arcade", "PublicAccessAllowRegistration", value.ToString());
            }
        }

        public string AdminAllowedIps
        {
            get => _adminAllowedIps;
            set
            {
                _adminAllowedIps = value;
                _config.WriteValue("Arcade", "AdminAllowedIps", value);
            }
        }

        
        private readonly string[] _standardExcludedProcesses = { 
            "emulationstation", "explorer", "batrun", "batrunguardian", 
            "taskhostw", "steam", "steamwebhelper", "galaxyclient", 
            "epicgameslauncher", "amazongames", "uplay", "origin",
            "windowsterminal", "conhost", "cmd", "powershell", "pwsh", "svchost",
            "chrome", "firefox", "msedge", "browser", "opera", "vlc", "mpc-hc",
            "emulatorlauncher", "smartscreen", "ctfmon", "startmenuexperiencehost", 
            "searchhost", "shellexperiencehost", "notepad++", "code", "devenv", 
            "sublime_text", "rider", "git-fork", "discord", "slack", "teams"
        };
        
        // Network Discovery
        // EN: Discovery UDP port can be explicit via config, or auto-derived from API port (+1)
        // FR: Le port UDP Discovery peut être explicite via config, ou dérivé automatiquement du port API (+1)
        private int _discoveryPort = 4322;
        private System.Net.Sockets.UdpClient? _udpDiscovery;
        private System.Threading.CancellationTokenSource? _discoveryCts;
        private string _networkFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "network.json");
        private System.Collections.Concurrent.ConcurrentDictionary<string, RemoteMachine> _discoveredMachines = new System.Collections.Concurrent.ConcurrentDictionary<string, RemoteMachine>();

        // EN: Cached MAC address â€” computed once, never changes at runtime
        // FR: Adresse MAC mise en cache â€” calculÃ©e une seule fois, ne change pas au runtime
        private string? _cachedMacAddress = null;

        // EN: Cached network interfaces for broadcast â€” refreshed every 30s to avoid costly NIC enumeration
        // FR: Interfaces rÃ©seau mises en cache pour le broadcast â€” rafraÃ®chies toutes les 30s
        private List<System.Net.IPEndPoint>? _cachedBroadcastEndpoints = null;
        private DateTime _broadcastEndpointExpiry = DateTime.MinValue;

        // Hook clavier
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private System.Windows.Forms.Timer? _watcherTimer;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private IntPtr _hookID = IntPtr.Zero;
        private NativeMethods.LowLevelKeyboardProc? _proc;

        public bool IsLocked => _isEnabled && !_isOperatorUnlocked;
        // EN: True when ES has just closed and the ES CLOSED alert overlay is active (wallpaper must not cover it)
        // FR: True quand ES vient de fermer et que l'alerte ES CLOSED est active (le wallpaper ne doit pas la masquer)
        public bool IsEsClosedAlertActive { get; private set; } = false;
        public bool IsLoadingVideoActive { get; set; } = false;
        private bool _isScreensaverActive = false;

        // EN: State saved when ES closes, to restore if ES restarts
        // FR: Ã‰tat sauvegardÃ© Ã  la fermeture d'ES, pour restaurer si ES redÃ©marre
        private int _savedSecondsOnEsClosed = 0;
        private int _savedCreditsOnEsClosed = 0;
        private void ShowInsertCoin(bool activate = true)
        {
            if (!_isEnabled || _isOperatorUnlocked || _isScreensaverActive || _isTimeoutActive || _isGameLaunching || _isOperatorPromptOpen || IsLoadingVideoActive) return;
            if (_isSessionActive && _sessionSecondsRemaining > 0) return;
            if (_isFreePlay) return;

            if (_overlay != null && _overlay.InvokeRequired && _overlay.IsHandleCreated)
                _overlay.Invoke(new Action(() => _overlay.ShowMessage("INSERT COIN", isAlert: true, activate: activate)));
            else
                _overlay?.ShowMessage("INSERT COIN", isAlert: true, activate: activate);
        }

        private bool _savedFreePlayOnEsClosed = false;
        private bool _hasStateToRestore = false;


        public ArcadeManager(Logger logger, IniFile config, ControllerService controllerService)
        {
            _logger = logger;
            _config = config;
            _controllerService = controllerService;
            _moonlightManager = new MoonlightManager(_logger, _config);
            
            AppKeyManager.Load();
            LoadConfig();

            string configDir = AppDomain.CurrentDomain.BaseDirectory;
            PublicUserManager = new UserManager(_logger, configDir);
            BlacklistManager = new IpBlacklistManager(_logger, configDir);
            LoadNetworkHistory();

            // EN: Always start heartbeat, regardless of arcade mode enabled/disabled
            // FR: DÃ©marrer le heartbeat toujours, que le mode arcade soit actif ou non
            string? hbDir = Path.GetDirectoryName(_heartbeatPath);
            if (hbDir != null) Directory.CreateDirectory(hbDir);
            _heartbeatTimer = new System.Threading.Timer(s => {
                try {
                    if (!File.Exists(_heartbeatPath)) File.WriteAllText(_heartbeatPath, "PULSE");
                    File.SetLastWriteTimeUtc(_heartbeatPath, DateTime.UtcNow);
                } catch { }
            }, null, 0, 2000);
            _logger.LogInfo($"Heartbeat started at {_heartbeatPath}");
            
            bool esLoadingActive = _config.ReadBool("Windows", "HideESLoading", false);
            ManageNotifyScripts(_isEnabled, esLoadingActive, _logger);
            
            if (_isEnabled)
            {
                Initialize();
            }
        }

        // EN: Save arcade session state to JSON (credits, freeplay, seconds, token) on every state change
        // FR: Sauvegarder l'Ã©tat de session arcade en JSON Ã  chaque changement d'Ã©tat
        public void SaveSessionState()
        {
            if (!_isEnabled) return;
            try
            {
                string? dir = Path.GetDirectoryName(_sessionStatePath);
                if (dir != null) Directory.CreateDirectory(dir);
                var state = new
                {
                    credits = _totalCredits,
                    freeplay = _isFreePlay,
                    secondsRemaining = _sessionSecondsRemaining,
                    sessionActive = _isSessionActive,
                    restartToken = _sessionRestartToken,
                    savedAt = DateTime.UtcNow.ToString("o")
                };
                File.WriteAllText(_sessionStatePath, System.Text.Json.JsonSerializer.Serialize(state));
            }
            catch { }
        }

        // EN: Restore session state from JSON if the restart token matches
        // FR: Restaurer l'Ã©tat de session depuis le JSON si le token de restart correspond
        public bool RestoreSessionState(string? guardianToken)
        {
            if (!_isEnabled || string.IsNullOrEmpty(guardianToken)) return false;
            _logger.LogInfo($"Attempting session restore with token: {guardianToken}");
            try
            {
                if (!File.Exists(_sessionStatePath)) 
                {
                    _logger.LogWarning("Session state file not found for restore.");
                    return false;
                }
                string json = File.ReadAllText(_sessionStatePath);
                var doc = System.Text.Json.JsonDocument.Parse(json);
                string savedToken = doc.RootElement.GetProperty("restartToken").GetString() ?? "";
                if (savedToken != guardianToken)
                {
                    _logger.LogWarning($"Session restore failed: token mismatch (got '{guardianToken}', expected '{savedToken}')");
                    return false;
                }
                int credits = doc.RootElement.GetProperty("credits").GetInt32();
                bool fp = doc.RootElement.GetProperty("freeplay").GetBoolean();
                int secs = doc.RootElement.GetProperty("secondsRemaining").GetInt32();
                bool sessActive = doc.RootElement.GetProperty("sessionActive").GetBoolean();

                _totalCredits = credits;
                _sessionSecondsRemaining = secs;
                _isSessionRestored = true;
                _restorationGraceTicks = 60; // EN: Grant 60s of "blind" countdown to start timer immediately / FR: Accorder 60s de dÃ©compte \"aveugle\" pour lancer le timer de suite
                _logger.LogInfo($"Session restored from Guardian restart: credits={credits}, freeplay={fp}, seconds={secs}");
                
                if (fp) SetFreePlay(true);
                else if (sessActive && secs > 0) 
                {
                    // EN: StartSession will set _isSessionActive = true and hide overlay
                    // FR: StartSession va mettre _isSessionActive = true et masquer l'overlay
                    StartSession(); 
                }
                else if (credits > 0) ShowInsertCoin();
                
                TriggerBroadcast();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Session restore failed", ex);
                return false;
            }
        }


        public void SyncWithConfig()
        {
            _logger.LogInfo("Syncing ArcadeManager with fresh config...");
            bool wasEnabled = _isEnabled;
            bool wasMoonlightEnabled = _moonlightStreamEnabled;
            bool wasApiEnabled = _apiEnabled;
            int oldApiPort = _apiPort;

            LoadConfig();

            // Case 1: Global Enabled toggled to False
            if (wasEnabled && !_isEnabled)
            {
                Shutdown();
                _logger.LogInfo("Arcade Mode disabled live.");
            }
            // Case 2: Global Enabled toggled to True (was False)
            else if (!wasEnabled && _isEnabled)
            {
                Initialize();
                ActivateArcadeMode();
                _logger.LogInfo("Arcade Mode enabled live.");
            }
            // Case 3: Still enabled but settings changed
            else if (_isEnabled)
            {
                // EN: Handle Moonlight toggle
                // FR: GÃ©rer la bascule Moonlight
                if (_moonlightStreamEnabled != wasMoonlightEnabled)
                {
                    if (_moonlightStreamEnabled) 
                    {
                        _logger.LogInfo("[Moonlight] Enabling service live...");
                        StartMoonlightService();
                    }
                    else 
                    {
                        _logger.LogInfo("[Moonlight] Disabling service live...");
                        StopMoonlightService();
                    }
                }

                // Handle API toggle/port change
                if (_apiEnabled != wasApiEnabled || _apiPort != oldApiPort)
                {
                    _apiService?.Stop();
                    _apiService?.Dispose();
                    _apiService = null;
                    
                    if (_apiEnabled)
                    {
                        _apiService = new ArcadeApiService(_logger, this, _apiPort);
                        _apiService.Start();
                        _logger.LogInfo($"Web API restarted on port {_apiPort}");
                    }
                    else
                    {
                        StopDiscoveryService();
                        _logger.LogInfo("Web API stopped live.");
                    }
                }

                // Update opacity
                if (_overlay != null) _overlay.Opacity = _overlayOpacity;
                _overlay?.RefreshMiniButtonsVisibility();
            }
        }

        private void LoadConfig()
        {
            AppKeyManager.Load();
            _isEnabled = _config.ReadBool("Arcade", "Enabled", false);
            _coinDeviceHandle = _config.ReadValue("Arcade", "CoinDevice", "ANY");
            _coinVirtualKey = ushort.TryParse(_config.ReadValue("Arcade", "CoinVirtualKey", "53"), out var kv) ? kv : (ushort)53; // Default '5'
            _minutesPerCredit = int.TryParse(_config.ReadValue("Arcade", "MinutesPerCredit", "5"), out var m) ? m : 5;
            _operatorPassword = _config.ReadValue("Arcade", "OperatorPassword", "");
            _hideOperatorButtons = _config.ReadBool("Arcade", "HideOperatorButtons", false);
            
            if (int.TryParse(_config.ReadValue("Arcade", "OverlayOpacity", "85"), out var op))
            {
                _overlayOpacity = Math.Max(10, Math.Min(100, op)) / 100.0;
            }

            _apiEnabled = _config.ReadBool("Arcade", "ApiEnabled", false);
            _apiPort = int.TryParse(_config.ReadValue("Arcade", "ApiPort", "4321"), out var p) ? p : 4321;
            int configuredDiscoveryPort = _config.ReadInt("Arcade", "DiscoveryPort", 0);
            _discoveryPort = configuredDiscoveryPort > 0 ? configuredDiscoveryPort : 4322; // [BATRUN-FIX]: Stable discovery port even if ApiPort changes

            // [BATRUN-FIX]: Prevent API port from colliding with the Discovery port
            if (_apiPort == _discoveryPort)
            {
                _apiPort++; // Jump to next port
                _logger.LogWarning($"API Port collision detected with Discovery Port ({_discoveryPort}). Automatically jumped to port {_apiPort}.");
            }
            
            _defaultMode = _config.ReadValue("Arcade", "DefaultMode", "None");
            _initialCredits = _config.ReadInt("Arcade", "InitialCredits", 0);

            _publicAccessEnabled = _config.ReadBool("Arcade", "PublicAccessEnabled", false);
            ReloadPublicSettings();
            _moonlightStreamEnabled = _config.ReadBool("Arcade", "MoonlightStreamEnabled", false);
            _httpsEnabled = _config.ReadBool("Arcade", "HttpsEnabled", false);
            _proxyMoonlight = _config.ReadBool("Arcade", "ProxyMoonlight", false);
            _hubMode = _config.ReadBool("Arcade", "HubMode", false);
            _publicIp = _config.ReadValue("Arcade", "PublicIp", "");
        }

        public void Shutdown()
        {
            _logger.LogInfo("Shutting down ArcadeManager services...");
            _moonlightManager.Stop();
            
            // Stop timers
            _watcherTimer?.Stop();
            _sessionTimer?.Dispose();
            _sessionTimer = null;
            _operatorHoldTimer?.Dispose();
            _operatorHoldTimer = null;
            _guardianHoldTimer?.Dispose();
            _guardianHoldTimer = null;
            // EN: Heartbeat timer is NOT stopped here - it lives for the full application lifetime
            // FR: Le timer heartbeat n'est PAS arrÃªtÃ© ici - il vit toute la durÃ©e de l'application
            if (IsInternalClosing)
            {
                _heartbeatTimer?.Dispose();
                _heartbeatTimer = null;
                try { if (File.Exists(_heartbeatPath)) File.Delete(_heartbeatPath); } catch { }
            }

            // Stop API and Services
            _apiService?.Stop();
            _apiService?.Dispose();
            _apiService = null;
            
            StopDiscoveryService();
            StopIpcServer();

            // Unhook
            if (_hookID != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }

            // Hide and Dispose Overlays
            if (_overlay != null)
            {
                if (_overlay.InvokeRequired) _overlay.Invoke(new Action(() => { _overlay.Hide(); _overlay.Dispose(); }));
                else { _overlay.Hide(); _overlay.Dispose(); }
                _overlay = null;
            }

/*
            if (_miniOverlay != null)
            {
                if (_miniOverlay.InvokeRequired) _miniOverlay.Invoke(new Action(() => { _miniOverlay.Hide(); _miniOverlay.Dispose(); }));
                else { _miniOverlay.Hide(); _miniOverlay.Dispose(); }
                _miniOverlay = null;
            }
*/

            _rawInputHandler?.Dispose();
            _rawInputHandler = null;

            // EN: We DO NOT reset _isSessionActive or _isOperatorUnlocked here, as they must survive 
            // the Shutdown/Initialize cycle during startup restoration.
            // FR: On ne RESET PAS _isSessionActive ni _isOperatorUnlocked ici, car ils doivent survivre
            // au cycle Shutdown/Initialize lors de la restauration au dÃ©marrage.
        }

        private void StopDiscoveryService()
        {
            _discoveryCts?.Cancel();
            _discoveryCts?.Dispose();
            _discoveryCts = null;
            _udpDiscovery?.Dispose();
            _udpDiscovery = null;
        }

        private void StopIpcServer()
        {
            _ipcCts?.Cancel();
            _ipcCts?.Dispose();
            _ipcCts = null;
        }


        public void StartMoonlightService()
        {
            _moonlightManager.Start();
        }

        public void StopMoonlightService()
        {
            _moonlightManager.Stop();
        }

        /// <summary>
        /// EN: Restarts BatRun API services (ArcadeApiService, DiscoveryService, and IpcServer) asynchronously with a delay.
        /// FR: Redémarre les services d'API BatRun (ArcadeApiService, DiscoveryService et IpcServer) de manière asynchrone avec un délai.
        /// </summary>
        public async Task RestartArcadeApiServicesAsync(int delayMs = 3000)
        {
            try
            {
                _logger.LogInfo($"[API-Restart] Scheduling BatRun API services restart in {delayMs}ms to clear stale sockets and states...");
                
                if (delayMs > 0)
                {
                    await Task.Delay(delayMs).ConfigureAwait(false);
                }

                _logger.LogInfo("[API-Restart] Restarting BatRun API services...");

                // EN: Stop current services
                // FR: Arrêter les services actuels
                _apiService?.Stop();
                _apiService?.Dispose();
                _apiService = null;

                StopDiscoveryService();
                StopIpcServer();

                // EN: Re-initialize and start services if API is enabled
                // FR: Réinitialiser et démarrer les services si l'API est activée
                if (_apiEnabled)
                {
                    _apiService = new ArcadeApiService(_logger, this, _apiPort);
                    _apiService.Start();
                    StartDiscoveryService();
                    StartIpcServer();
                    _logger.LogInfo("[API-Restart] BatRun API services successfully restarted.");
                }
                else
                {
                    _logger.LogWarning("[API-Restart] API is not enabled in config, skipping restart.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("[API-Restart] Error during asynchronous API services restart", ex);
            }
        }



        public static void ManageNotifyScripts(bool arcadeEnabled, bool esLoadingEnabled, Logger? logger = null)
        {
            try
            {
                string? retrobatDir = null;
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\RetroBat"))
                {
                    retrobatDir = key?.GetValue("LatestKnownInstallPath") as string;
                }
                if (string.IsNullOrEmpty(retrobatDir)) return;

                string scriptFolder = System.IO.Path.Combine(retrobatDir, "emulationstation", ".emulationstation", "scripts", "system-selected");
                string startFolder = System.IO.Path.Combine(retrobatDir, "emulationstation", ".emulationstation", "scripts", "game-start");
                string endFolder = System.IO.Path.Combine(retrobatDir, "emulationstation", ".emulationstation", "scripts", "game-end");
                string ssStartFolder = System.IO.Path.Combine(retrobatDir, "emulationstation", ".emulationstation", "scripts", "screensaver-start");
                string ssStopFolder = System.IO.Path.Combine(retrobatDir, "emulationstation", ".emulationstation", "scripts", "screensaver-stop");
                
                System.IO.Directory.CreateDirectory(scriptFolder);
                System.IO.Directory.CreateDirectory(startFolder);
                System.IO.Directory.CreateDirectory(endFolder);
                System.IO.Directory.CreateDirectory(ssStartFolder);
                System.IO.Directory.CreateDirectory(ssStopFolder);
                
                string scriptFile = System.IO.Path.Combine(scriptFolder, "notify_batrun.bat");
                string startFile = System.IO.Path.Combine(startFolder, "notify_batrun.bat");
                string endFile = System.IO.Path.Combine(endFolder, "notify_batrun.bat");
                string ssStartFile = System.IO.Path.Combine(ssStartFolder, "notify_batrun.bat");
                string ssStopFile = System.IO.Path.Combine(ssStopFolder, "notify_batrun.bat");

                if (arcadeEnabled)
                {
                    // CrÃ©er le script system-selected si absent
                    if (!System.IO.File.Exists(scriptFile))
                    {
                        string basePath = AppContext.BaseDirectory;
                        string content = $"@echo off{System.Environment.NewLine}" +
                                         $"set Focus_BatRun_path={basePath}{System.Environment.NewLine}" +
                                         $"start \"BatRun_Focus_ES\" \"%Focus_BatRun_path%\\BatRun.exe\" -ES_System_select";
                        System.IO.File.WriteAllText(scriptFile, content);
                    }
                    
                    // Toujours crÃ©er/Ã©craser game-start et game-end en mode arcade
                    string startContent = $"@echo off{System.Environment.NewLine}start \"\" \"{AppContext.BaseDirectory}BatRun.exe\" -game-start %*";
                    System.IO.File.WriteAllText(startFile, startContent);
                    
                    string endContent = $"@echo off{System.Environment.NewLine}start \"\" \"{AppContext.BaseDirectory}BatRun.exe\" -game-end";
                    System.IO.File.WriteAllText(endFile, endContent);

                    // CrÃ©er/Ã©craser les scripts screensaver
                    string ssStartContent = $"@echo off{System.Environment.NewLine}start \"\" \"{AppContext.BaseDirectory}BatRun.exe\" -screensaver-start";
                    System.IO.File.WriteAllText(ssStartFile, ssStartContent);

                    string ssStopContent = $"@echo off{System.Environment.NewLine}start \"\" \"{AppContext.BaseDirectory}BatRun.exe\" -screensaver-stop";
                    System.IO.File.WriteAllText(ssStopFile, ssStopContent);
                }
                else
                {
                    // DÃ©sactivÃ© : Supprimer game-start, game-end, screensaver-start, screensaver-stop
                    if (System.IO.File.Exists(startFile)) System.IO.File.Delete(startFile);
                    if (System.IO.File.Exists(endFile)) System.IO.File.Delete(endFile);
                    if (System.IO.File.Exists(ssStartFile)) System.IO.File.Delete(ssStartFile);
                    if (System.IO.File.Exists(ssStopFile)) System.IO.File.Delete(ssStopFile);

                    // Supprimer system-selected uniquement si ESLoading est aussi dÃ©sactivÃ©
                    if (!esLoadingEnabled && System.IO.File.Exists(scriptFile))
                        System.IO.File.Delete(scriptFile);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError("Error managing notify_batrun.bat scripts", ex);
            }
        }

        private void Initialize()
        {
            _logger.LogInfo("Initializing Arcade Mode...");
            
            bool esLoadingActive = _config.ReadBool("Windows", "HideESLoading", false);
            ManageNotifyScripts(_isEnabled, esLoadingActive, _logger);

            Shutdown(); // Cleanup first just in case

            if (SynchronizationContext.Current == null)
            {
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            }

            try
            {
                _overlay = new ArcadeOverlayForm(_overlayOpacity, this);
                var _Handle = _overlay.Handle; // Force creation
                _overlay.OperatorRequested += OnOperatorRequested;
                _overlay.FreePlayToggleRequested += OnFreePlayToggleRequested;
                _overlay.AddCreditsRequested += OnAddCreditsRequested;
            }
            catch (Exception ex)
            {
                _logger.LogError("CRITICAL ERROR: Failed to create Arcade Overlay handle. " + ex.Message, ex);
                // Fallback: try one more time after a small delay if handle already exists error
                if (ex.Message.Contains("handle", StringComparison.OrdinalIgnoreCase))
                {
                    System.Threading.Thread.Sleep(500);
                    _overlay = new ArcadeOverlayForm(_overlayOpacity, this);
                }
            }

            if (_apiEnabled)
            {
                _apiService = new ArcadeApiService(_logger, this, _apiPort);
                _apiService.Start();
                StartDiscoveryService();
                StartIpcServer();
            }

            StartMoonlightService();
            _ = Task.Run(async () => await _moonlightManager.GetPublicIpAsync());

            if (_overlay != null)
            {
                _overlay.LockRequested += OnLockRequested;
                _overlay.InterfaceRequested += (s, e) => InterfaceRequested?.Invoke(this, EventArgs.Empty);
            }
            
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += (s, e) => RefreshAllOverlays();

            _rawInputHandler = new RawInputHandler(_logger);
            _rawInputHandler.KeyPressed += OnRawInputKeyPressed;

            // EN: 200ms instead of 100ms - imperceptible delay but saves ~50% CPU on old hardware (i7-3770K etc.)
            // FR: 200ms au lieu de 100ms - dÃ©lai imperceptible mais Ã©conomise ~50% CPU sur ancien matÃ©riel
            _watcherTimer = new System.Windows.Forms.Timer { Interval = 200 };
            _watcherTimer.Tick += (s, e) => WatcherTick();
            _watcherTimer.Start();

            // EN: Start session timer globally to monitor sessions and timeouts
            _sessionTimer = new System.Threading.Timer(OnSessionTick, null, 1000, 1000);

            if (_hookID == IntPtr.Zero)
            {
                _proc = HookCallback;
                _hookID = SetHook(_proc);
            }

            // EN: If a session was restored and is active, ensure we hide the overlay now that it's created
            // FR: Si une session a Ã©tÃ© restaurÃ©e et est active, s'assurer de masquer l'overlay maintenant qu'il est crÃ©Ã©
            if (_isSessionActive && _sessionSecondsRemaining > 0)
            {
                _logger.LogInfo($"Initialize: Active session detected ({_sessionSecondsRemaining}s). Hiding overlay.");
                _overlay?.HideOverlay();
            }
        }

        private void ApplyDefaultMode()
        {
            if (_isSessionRestored)
            {
                _logger.LogInfo("Skipping DefaultMode: Session was restored.");
                return;
            }

            _logger.LogInfo($"Applying default arcade mode: {_defaultMode}");
            switch (_defaultMode)
            {
                case "Credit":
                    if (_initialCredits > 0)
                    {
                        for (int i = 0; i < _initialCredits; i++)
                        {
                            AddCredit();
                        }
                    }
                    else
                    {
                        ShowInsertCoin();
                    }
                    break;
                case "Freeplay":
                    SetFreePlay(true);
                    break;
                case "Operator":
                    UnlockOperatorMode();
                    break;
                case "None":
                default:
                    ShowInsertCoin();
                    break;
            }
        }

        private void WatcherTick()
        {
            _watcherCounter++;

            if (_isEnabled && !_isOperatorUnlocked)
            {
                // EN: Periodic focus enforcement when system is locked (Every 100ms)
                // FR: ForÃ§age pÃ©riodique du focus quand le systÃ¨me est verrouillÃ© (Toutes les 100ms)
                // EN: Respect screensaver and game boot delay / FR: Respecter le screensaver et le dÃ©lai de boot du jeu
                bool isGameBooting = (DateTime.Now - _gameStartTime < TimeSpan.FromSeconds(25));
                if (IsLocked && !_isSessionActive && !IsInternalClosing && _overlay != null && !IsGameSuspended && !_isScreensaverActive && !isGameBooting && !_isOperatorPromptOpen)
                {
                    if (NativeMethods.GetForegroundWindow() != _overlay.Handle)
                    {
                        ShowInsertCoin();
                    }
                }
                else if (IsLocked && _isSessionActive && _allowAltTab && _currentGameHwnd != IntPtr.Zero && _allowedForegroundWindows.Length > 0 && !isGameBooting && !_isOperatorPromptOpen)
                {
                    // EN: Check if the current foreground window is the game or an allowed window
                    // FR: VÃ©rifier si la fenÃªtre au premier plan est le jeu ou une fenÃªtre autorisÃ©e
                    IntPtr fg = NativeMethods.GetForegroundWindow();
                    if (fg != IntPtr.Zero && fg != _currentGameHwnd && fg != (_overlay?.Handle ?? IntPtr.Zero))
                    {
                        if (fg != _lastForegroundHwnd)
                        {
                            _lastForegroundHwnd = fg;
                            try {
                                NativeMethods.GetWindowThreadProcessId(fg, out uint fgPid);
                                var p = System.Diagnostics.Process.GetProcessById((int)fgPid);
                                _lastForegroundProcessName = p.ProcessName.ToLower();
                            } catch { _lastForegroundProcessName = ""; }
                        }

                        if (!string.IsNullOrEmpty(_lastForegroundProcessName))
                        {
                            bool isAllowed = _allowedForegroundWindows.Contains(_lastForegroundProcessName) || 
                                             _lastForegroundProcessName == _currentExecutable.ToLower() ||
                                             (!string.IsNullOrEmpty(_sessionOriginExecutable) && _lastForegroundProcessName == _sessionOriginExecutable.ToLower());

                            if (!isAllowed)
                            {
                                // _logger.LogInfo($"Window '{_lastForegroundProcessName}' is not in AllowedForegroundWindows. Forcing focus back to game.");
                                NativeMethods.SetForegroundWindow(_currentGameHwnd);
                            }
                        }
                    }
                }

                // EN: Heavier checks performed every 5 seconds (25 ticks at 200ms)
                // FR: VÃ©rifications plus lourdes toutes les 5 secondes (25 ticks Ã  200ms)
                // EN: Process.GetProcessesByName() enumerates ALL system processes â€” very costly, keep it rare
                // FR: Process.GetProcessesByName() Ã©numÃ¨re TOUS les processus â€” coÃ»teux, le garder rare
                if (_watcherCounter % 25 == 0)
                {
                    // EN: Watchdog: If a game is running but the captured process has exited, force a GAME_END
                    // FR: Watchdog : Si un jeu est en cours mais que le processus capturé est fermé, forcer GAME_END
                    if (_isGameRunning && _capturedPid != 0)
                    {
                        try
                        {
                            var p = System.Diagnostics.Process.GetProcessById((int)_capturedPid);
                            if (p.HasExited)
                            {
                                _logger.LogInfo($"[Watchdog] Captured process {_capturedPid} has exited. Forcing GAME_END.");
                                HandleIpcCommand("GAME_END");
                            }
                        }
                        catch (ArgumentException)
                        {
                            // EN: Process no longer exists / FR : Le processus n'existe plus
                            _logger.LogInfo($"[Watchdog] Captured process {_capturedPid} is no longer found. Forcing GAME_END.");
                            HandleIpcCommand("GAME_END");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"[Watchdog] Error checking PID {_capturedPid}: {ex.Message}");
                        }
                    }

                    try
                    {
                        var processes = System.Diagnostics.Process.GetProcessesByName("taskmgr");
                        foreach (var p in processes)
                        {
                            try { p.Kill(); } catch { }
                            finally { p.Dispose(); }
                        }
                    }
                    catch { }
                }
            }
            else if (_isEnabled && _isOperatorUnlocked && _watcherCounter % 50 == 0)
            {
                // EN: Very rare Z-order refresh in operator mode (every 10s at 200ms = 50 ticks)
                // FR: RafraÃ®chissement du Z-order trÃ¨s rare en mode opÃ©rateur (toutes les 10s)
                if (_overlay != null && _overlay.Visible)
                {
                    NativeMethods.SetWindowPos(_overlay.Handle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
                        NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
                }
            }
        }



        public void ActivateArcadeMode()
        {
            if (!_isEnabled) return;
            
            // EN: Only reset session state if not restored from Guardian
            // FR: Ne rÃ©initialiser l'Ã©tat de session que si non restaurÃ© par le Guardian
            if (!_isSessionRestored)
            {
                _isSessionActive = false;
                _totalCredits = 0;
                _sessionSecondsRemaining = 0;
            }
            
            Shutdown();
            Initialize();

            // EN: Catch-up check for ES state after initialization
            // FR: ContrÃ´le de rattrapage de l'Ã©tat ES aprÃ¨s initialisation
            var esRunning = Process.GetProcessesByName("emulationstation").Any(p => {
                try { return !p.HasExited; } catch { return false; }
            });
            NotifyEmulationStationState(esRunning);
            
            ApplyDefaultMode();
            
            // EN: Reset restoration flag after activation sequence is complete
            // FR: RÃ©initialiser le flag de restauration une fois la sÃ©quence d'activation terminÃ©e
            _isSessionRestored = false; 
        }

        /// <summary>
        /// EN: Start a virtual RetroBat UI session (streaming the interface itself, not a specific game).
        /// FR: Démarrer une session d'interface RetroBat virtuelle (streaming de l'interface elle-même).
        /// </summary>
        public void StartWebUiSession()
        {
            _logger.LogInfo("[Manager] Starting Web UI Session ([RETROBAT_UI])");
            IsWebLaunch = true;
            IsWebUiSession = true;
            _currentGameName = "[RETROBAT_UI]";
            _currentGameSystem = "RetroBat";
            _isGameRunning = true; // EN: Mark as running to avoid immediate timeout / FR: Marquer comme actif pour éviter le timeout
            _gameStartTime = DateTime.Now;
            TriggerBroadcast();
        }

        private void EnsureNotifyScriptInstalled()
        {
            try
            {
                string? retrobatDir = null;

                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\RetroBat"))
                {
                    retrobatDir = key?.GetValue("LatestKnownInstallPath") as string;
                }

                if (string.IsNullOrEmpty(retrobatDir))
                {
                    _logger.LogInfo("Cannot find RetroBat path to install notify script.");
                    return;
                }

                string scriptFolder = Path.Combine(retrobatDir, "emulationstation", ".emulationstation", "scripts", "system-selected");
                Directory.CreateDirectory(scriptFolder);

                string scriptFile = Path.Combine(scriptFolder, "notify_batrun.bat");
                if (!File.Exists(scriptFile))
                {
                    string basePath = AppContext.BaseDirectory;
                    string content = $"@echo off{Environment.NewLine}set Focus_BatRun_path={basePath}{Environment.NewLine}start \"BatRun_Focus_ES\" \"%Focus_BatRun_path%\\BatRun.exe\" -ES_System_select";
                    File.WriteAllText(scriptFile, content);
                    _logger.LogInfo($"notify_batrun.bat installed at: {scriptFile}");
                }
                else
                {
                    _logger.LogInfo("notify_batrun.bat already present, skipping install.");
                }

                // --- NEW: game-start script ---
                string startFolder = Path.Combine(retrobatDir, "emulationstation", ".emulationstation", "scripts", "game-start");
                Directory.CreateDirectory(startFolder);
                string startFile = Path.Combine(startFolder, "notify_batrun.bat");
                string startContent = $"@echo off{Environment.NewLine}start \"\" \"{AppContext.BaseDirectory}BatRun.exe\" -game-start %*";
                File.WriteAllText(startFile, startContent);
                _logger.LogInfo($"game-start script installed at: {startFile}");

                // --- NEW: game-end script ---
                string endFolder = Path.Combine(retrobatDir, "emulationstation", ".emulationstation", "scripts", "game-end");
                Directory.CreateDirectory(endFolder);
                string endFile = Path.Combine(endFolder, "notify_batrun.bat");
                string endContent = $"@echo off{Environment.NewLine}start \"\" \"{AppContext.BaseDirectory}BatRun.exe\" -game-end";
                File.WriteAllText(endFile, endContent);
                _logger.LogInfo($"game-end script installed at: {endFile}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to install notify_batrun.bat", ex);
            }
        }

        public void NotifyEmulationStationState(bool isRunning)
        {
            bool wasRunning = _isEsRunning;
            _isEsRunning = isRunning;

            if (!_isEnabled) return;
            
            if (isRunning && !wasRunning)
            {
                _logger.LogInfo("EmulationStation process detected, showing Arcade Overlay.");
                // _isEsRunning already set at top of method
                IsEsClosedAlertActive = false; // EN: ES is back, clear alert / FR: ES est de retour, effacer l'alerte
                
                // EN: Always restore state when ES restarts (regardless of operator mode)
                // FR: Toujours restaurer l'Ã©tat au redÃ©marrage d'ES (peu importe le mode opÃ©rateur)
                if (_hasStateToRestore)
                {
                    _logger.LogInfo($"Restoring state after ES restart: {_savedSecondsOnEsClosed}s, FreePlay={_savedFreePlayOnEsClosed}, Credits={_savedCreditsOnEsClosed}");
                    _isFreePlay = _savedFreePlayOnEsClosed;
                    _sessionSecondsRemaining = _savedSecondsOnEsClosed;
                    _totalCredits = _savedCreditsOnEsClosed;
                    _hasStateToRestore = false;
                    
                    // EN: Only start session timer if not in operator mode
                    // FR: Ne dÃ©marrer le timer de session que si pas en mode opÃ©rateur
                    if (!_isOperatorUnlocked && (_isFreePlay || _sessionSecondsRemaining > 0))
                    {
                        StartSession();
                    }
                }
                
                // EN: Show overlay based on current state (only if player mode)
                // FR: Afficher l'overlay selon l'Ã©tat actuel (seulement en mode joueur)
                if (!_isOperatorUnlocked)
                {
                    if (_isFreePlay || (_isSessionActive && _sessionSecondsRemaining > 0))
                    {
                        // Session active ou Free Play : on n'affiche rien
                        _overlay?.HideOverlay();
                    }
                    else
                    {
                        // Afficher INSERT COIN par-dessus ES via la mÃ©thode sÃ©curisÃ©e
                        ShowInsertCoin();
                    }
                }
                else
                {
                    // EN: In operator mode, ensure the mini overlay is visible when ES is detected
                    // FR: En mode opÃ©rateur, s'assurer que le mini overlay est visible quand ES est dÃ©tectÃ©
                    if (_overlay != null && _overlay.IsHandleCreated)
                        _overlay.Invoke(new Action(() => _overlay.SetMiniMode(true)));
                }
            }
            else if (!isRunning && wasRunning)
            {
                _logger.LogInfo("EmulationStation stopped.");
                _isEsRunning = false;
                
                // EN: Save current state BEFORE EndSession resets counters
                // FR: Sauvegarder l'Ã©tat courant AVANT qu'EndSession rÃ©initialise les compteurs
                bool wasFreePlay = _isFreePlay;
                bool wasSessionActive = _isSessionActive;
                int savedSeconds = _sessionSecondsRemaining;
                int savedCredits = _totalCredits;
                
                string timeInfo = wasFreePlay ? "FREE PLAY ACTIVE" : $"{(savedSeconds / 60):D2}:{(savedSeconds % 60):D2} LEFT";

                // EN: Persist state for restoration when ES comes back
                // FR: Persister l'Ã©tat pour restauration au retour d'ES
                if (wasFreePlay || (wasSessionActive && savedSeconds > 0))
                {
                    _savedFreePlayOnEsClosed = wasFreePlay;
                    _savedSecondsOnEsClosed = savedSeconds;
                    _savedCreditsOnEsClosed = savedCredits;
                    _hasStateToRestore = true;
                    _logger.LogInfo($"State saved on ES close: FreePlay={wasFreePlay}, Seconds={savedSeconds}, Credits={savedCredits}");
                }
                else
                {
                    _hasStateToRestore = false;
                }
                
                // EN: End session without showing GAME OVER (ES is closed, we'll show ES CLOSED instead)
                // FR: Terminer la session sans afficher GAME OVER (ES est fermÃ©, on affichera ES CLOSED)
                EndSession();

                if (!_isOperatorUnlocked)
                {
                    // EN: Show ES CLOSED with context (FreePlay or session time)
                    // FR: Afficher ES CLOSED avec contexte (FreePlay ou temps de session)
                    string crashMsg = (wasSessionActive || wasFreePlay)
                        ? $"ES CLOSED!\n{timeInfo}\nPRESS SELECT+START OR CALL OPERATOR"
                        : "ES CLOSED!\nPRESS SELECT+START OR CALL OPERATOR";
                    IsEsClosedAlertActive = true;
                    _overlay?.ShowMessage(crashMsg, isAlert: true);
                }
            }
        }

        /// <summary>
        /// Called from Batrun.cs when the BatRun_ES_System_Select signal is received.
        /// This means ES is fully loaded. NOW we apply overlay / focus.
        /// </summary>
        public void NotifyEsReady()
        {
            if (!_isEnabled || _isOperatorUnlocked) return;

            _logger.LogInfo("ArcadeManager: ES ready (System_Select received). Applying overlay.");

            // EN: If state was restored (credits or freeplay), hide overlay, ES is accessible
            // FR: Si l'Ã©tat a Ã©tÃ© restaurÃ© (crÃ©dits ou freeplay), masquer l'overlay
            if (_isFreePlay || (_isSessionActive && _sessionSecondsRemaining > 0))
            {
                // Session active ou Free Play : on laisse ES visible
                _overlay?.HideOverlay();
            }
            else
            {
                ShowInsertCoin();
            }
        }

        public void DeactivateArcadeMode()
        {
            if (!_isEnabled) return;

            if (_hookID != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
                _logger.LogInfo("Low-level keyboard hook removed.");
            }

            _sessionTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _overlay?.HideOverlay();
            _overlay?.SetMiniMode(false);
            _isSessionActive = false;
            _ipcCts?.Cancel();
            _ipcCts = null;
            CleanupNotifyScripts();
        }

        private void CleanupNotifyScripts()
        {
            try
            {
                string? retrobatDir = null;
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\RetroBat"))
                {
                    retrobatDir = key?.GetValue("LatestKnownInstallPath") as string;
                }

                if (string.IsNullOrEmpty(retrobatDir)) return;

                string[] scriptPaths = {
                    Path.Combine(retrobatDir, "emulationstation", ".emulationstation", "scripts", "system-selected", "notify_batrun.bat"),
                    Path.Combine(retrobatDir, "emulationstation", ".emulationstation", "scripts", "game-start", "notify_batrun.bat"),
                    Path.Combine(retrobatDir, "emulationstation", ".emulationstation", "scripts", "game-end", "notify_batrun.bat")
                };

                foreach (var path in scriptPaths)
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        _logger.LogInfo($"Deleted script: {path}");
                    }
                }
            }
            catch (Exception ex) { _logger.LogWarning("Failed to cleanup ES scripts: " + ex.Message); }
        }

        private void OnRawInputKeyPressed(object? sender, RawInputEventArgs e)
        {
            if (_isOperatorUnlocked) return; // Do nothing if already unlocked

            // EN: If a specific device is selected, compare its RawInput name (VID/PID) with the current event source
            // FR: Si un pÃ©riphÃ©rique spÃ©cifique est sÃ©lectionnÃ©, comparer son nom RawInput (VID/PID) avec la source actuelle
            bool isCorrectDevice = _coinDeviceHandle == "ANY";
            if (!isCorrectDevice)
            {
                string deviceName = RawInputHandler.GetDeviceName(e.DeviceHandle);
                isCorrectDevice = deviceName == _coinDeviceHandle;
            }

            if (isCorrectDevice)
            {
                if (e.VirtualKey == _coinVirtualKey && e.IsKeyDown)
                {
                    AddCredit();
                }
            }

            // _logger.LogInfo($"RawKey: {e.VirtualKey} (Down: {e.IsKeyDown})");
            // Raccourci OpÃ©rateur (Touche 9 ou NumPad9 ou 'Ã§' = D9) - Maintenir 3 secondes
            if (e.VirtualKey == (ushort)Keys.D9 || e.VirtualKey == (ushort)Keys.NumPad9)
            {
                if (e.IsKeyDown)
                {
                    if (!_isOperatorKeyHeld)
                    {
                        _isOperatorKeyHeld = true;
                        _operatorHoldTimer?.Dispose();
                        _operatorHoldTimer = new System.Threading.Timer(OnOperatorHoldCompleted, null, 3000, Timeout.Infinite);
                    }
                }
                else
                {
                    _isOperatorKeyHeld = false;
                    _operatorHoldTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }

            // Raccourci Guardian (Touche 0 ou NumPad0 ou 'Ã ' = D0) - Maintenir 3 secondes
            if (e.VirtualKey == (ushort)Keys.D0 || e.VirtualKey == (ushort)Keys.NumPad0)
            {
                if (e.IsKeyDown)
                {
                    // EN: Only start Guardian timer if system is locked or session is active (avoid trigger while typing in dev tools)
                    // FR: Ne dÃ©marrer le timer Guardian que si systÃ¨me verrouillÃ© ou session active (Ã©vite trigger en tapant dans outils dev)
                    if (!_isGuardianKeyHeld && (IsLocked || _isSessionActive))
                    {
                        _isGuardianKeyHeld = true;
                        _guardianHoldTimer?.Dispose();
                        _guardianHoldTimer = new System.Threading.Timer(OnGuardianHoldCompleted, null, 3000, Timeout.Infinite);
                    }
                }
                else
                {
                    _isGuardianKeyHeld = false;
                    _guardianHoldTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }

        private void OnGuardianHoldCompleted(object? state)
        {
            if (!_isGuardianKeyHeld) return;

            // EN: Hardware check â€” is the key ACTUALLY still pressed? (Fixes Ghost Trigger if KeyUp event was missed)
            // FR: VÃ©rification matÃ©rielle â€” la touche est-elle RÃ‰ELLEMENT encore enfoncÃ©e ? (Corrige trigger fantÃ´me si KeyUp manquÃ©)
            bool isPhysicallyPressed = (NativeMethods.GetAsyncKeyState((int)Keys.D0) & 0x8000) != 0 || 
                                     (NativeMethods.GetAsyncKeyState((int)Keys.NumPad0) & 0x8000) != 0;
            
            if (!isPhysicallyPressed)
            {
                _isGuardianKeyHeld = false;
                return;
            }
            
            _logger.LogInfo("Guardian key (0) maintained for 3 seconds. Waking up Guardian via IPC...");
            string wakePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wake_guardian.lock");
            try { System.IO.File.WriteAllText(wakePath, "WAKE"); } catch { }
        }

        private void OnOperatorHoldCompleted(object? state)
        {
            if (!_isOperatorKeyHeld || IsInternalClosing) return;
            // Ne pas mettre Ã  false ici ! Le flag doit rester true jusqu'au KEY_UP 
            // interceptÃ© par OnRawInputKeyPressed pour que le Hook puisse bloquer
            // la rÃ©pÃ©tition de touche.

            if (_isFreePlay)
            {
                _logger.LogInfo("Operator Hold triggered in FreePlay mode. Resetting to Insert Coin.");
                SetFreePlay(false);
            }

            if (_overlay?.InvokeRequired == true)
            {
                _overlay.BeginInvoke(new Action(() => OnOperatorRequested(this, EventArgs.Empty)));
            }
            else
            {
                OnOperatorRequested(this, EventArgs.Empty);
            }
        }

        public void AddManualCredits(int count)
        {
            for (int i = 0; i < count; i++)
            {
                AddCredit();
            }
        }

        public void ShowOperatorMessage(string message, int durationSeconds)
        {
            _overlay?.ShowOperatorMessage(message, durationSeconds);
        }

        public void RemoveCredit()
        {
            if (_totalCredits > 0)
            {
                if (_sessionSecondsRemaining <= 60 && !_isFreePlay)
                {
                    _logger.LogInfo($"API -1 Credit refusÃ© : seulement {_sessionSecondsRemaining}s restantes (<= 1 minute).");
                    return;
                }

                _sessionSecondsRemaining = Math.Max(0, _sessionSecondsRemaining - (_minutesPerCredit * 60));
                UpdateCreditsFromTime(); // EN: Ensure credits are sync after removal / FR: Sync crÃ©dits aprÃ¨s retrait
                _logger.LogInfo($"Credit removed via API. Total: {_totalCredits}, Time adjusted.");
                SaveSessionState();
                if (_sessionSecondsRemaining <= 0 && !_isFreePlay) EndSession();
            }
        }

        public void SetSessionDuration(int minutes)
        {
            _sessionSecondsRemaining = minutes * 60;
            UpdateCreditsFromTime();
            _logger.LogInfo($"Session duration set to {minutes} minutes via API. Total Credits: {_totalCredits}");
            
            if (_sessionSecondsRemaining > 0)
            {
                if (!_isSessionActive)
                {
                    _logger.LogInfo($"SetSessionDuration: Starting session because duration set to {minutes}m.");
                    StartSession(); // Starts session and hides overlay initially
                }
                
                // Show message for 2 seconds (non-alert to avoid focus theft / minimization)
                _overlay?.ShowMessage($"TIME UPDATED\n{minutes} MINUTES LEFT", isAlert: false);
                Task.Delay(2000).ContinueWith(_ => _overlay?.HideOverlay());
            }
            else if (_sessionSecondsRemaining <= 0 && _isSessionActive && !_isFreePlay)
            {
                EndSession();
            }
            
            SaveSessionState();
        }

        private void UpdateCreditsFromTime()
        {
            if (_minutesPerCredit > 0)
            {
                int secPerCredit = _minutesPerCredit * 60;
                if (_sessionSecondsRemaining <= 0)
                    _totalCredits = 0;
                else
                    _totalCredits = (_sessionSecondsRemaining + secPerCredit - 1) / secPerCredit;
            }
            else if (_sessionSecondsRemaining > 0)
            {
                _totalCredits = 1;
            }
            else
            {
                _totalCredits = 0;
            }
        }
        public void SetFreePlay(bool enabled)
        {
            _isFreePlay = enabled;
            SaveSessionState(); // EN: Save state on freeplay change / FR: Sauvegarder l'Ã©tat lors du changement freeplay
            if (_isFreePlay)
            {
                if (!_isSessionActive) StartSession();
                TriggerBroadcast();
                _overlay?.ShowMessage("FREE PLAY ACTIVE", false);
                Task.Delay(2000).ContinueWith(_ => _overlay?.HideOverlay());
            }
            else
            {
                TriggerBroadcast();
                if (_sessionSecondsRemaining <= 0) EndSession();
            }
        }
        private void AddCredit()
        {
            int addedSeconds = _minutesPerCredit * 60;
            _sessionSecondsRemaining += addedSeconds;
            UpdateCreditsFromTime(); // EN: Update credits count to match new time / FR: Mettre Ã  jour les crÃ©dits pour correspondre au nouveau temps
            
            if (!_isEsRunning)
            {
                _logger.LogInfo("AddCredit: EmulationStation is not running yet. Showing alert but keeping credits.");
                string timeInfo = _isFreePlay ? "FREE PLAY ACTIVE" : $"{(_sessionSecondsRemaining / 60):D2}:{(_sessionSecondsRemaining % 60):D2} LEFT";
                if (!_isOperatorUnlocked && !IsLoadingVideoActive)
                {
                    _overlay?.ShowMessage($"ES CLOSED!\n{timeInfo}\nPRESS SELECT+START OR CALL OPERATOR", isAlert: true);
                }
                TriggerBroadcast();
                // EN: We don't return here anymore, we allow adding the credit and starting the session timer
                // FR: On ne quitte plus ici, on autorise l'ajout du crÃ©dit et le dÃ©marrage du timer de session
            }

            _logger.LogInfo($"Credit added. Total: {_totalCredits}, Session Time: {_sessionSecondsRemaining}s");
            SaveSessionState(); // EN: Save state after credit add / FR: Sauvegarder l'Ã©tat aprÃ¨s ajout de crÃ©dit

            if (!_isSessionActive)
            {
                StartSession();
            }

            TriggerBroadcast(); // EN: Force immediate network update

            _overlay?.ShowMessage($"CREDIT INSERTED\n{_sessionSecondsRemaining / 60} MINUTES LEFT", isAlert: false);
            Task.Delay(2000).ContinueWith(_ => {
                _overlay?.HideOverlay();
                if (_currentGameHwnd == IntPtr.Zero) FocusEmulationStation();
            });
        }

        public void StartSession()
        {
            if (_isSessionActive) return; 
            
            _isSessionActive = true;
            _isGameLaunching = false; // EN: Session started, clear launch flag / FR: Session dÃ©marrÃ©e, effacer le flag de lancement
            TriggerBroadcast(); // EN: Force immediate network update
            _controllerService.IsInputBlocked = false;
            
            _isTimeoutActive = false; // EN: Cancel auto-close timeout on resume
            _timeoutSecondsRemaining = 0;
            IsInternalClosing = false;

            uint pid = 0;
            try
            {
                pid = _lastPausedPid;
                if (pid == 0)
                {
                    IntPtr fg = NativeMethods.GetForegroundWindow();
                    if (fg != IntPtr.Zero) NativeMethods.GetWindowThreadProcessId(fg, out pid);
                }

                if (pid > 0)
                {
                    var proc = System.Diagnostics.Process.GetProcessById((int)pid);
                    string exeName = proc.ProcessName;
                    var config = AppKeyManager.GetConfigForExe(exeName);
                    
                    if (config == null)
                    {
                        if (IsTeknoParrotLoaderRunning())
                        {
                            _logger.LogInfo($"TeknoParrot loader detected! Using TeknoParrotUi config for resume of unknown process: {exeName}");
                            config = AppKeyManager.GetConfigForExe("TeknoParrotUi") ?? new AppKeyConfig { ExeName = "TP_Fallback", PauseKey = "SUSPEND", ResumeKey = "SUSPEND" };
                        }
                        else
                        {
                            config = AppKeyManager.GetConfigForExe("[DEFAULT_PC_GAME]");
                        }
                    }

                    if (config != null && !string.IsNullOrWhiteSpace(config.ResumeKey))
                    {
                        // EN: Ensure process is resume-able and find its handle BEFORE sending keys
                        // FR: S'assurer que le processus est prÃªt et trouver son handle AVANT d'envoyer les touches
                        if (config.ResumeKey.Equals("SUSPEND", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInfo($"StartSession: Resuming process {exeName} (PID: {proc.Id}) via NtResumeProcess");
                            NativeMethods.NtResumeProcess(proc.Handle);
                            System.Threading.Thread.Sleep(200);
                        }

                        // EN: Find window handle FIRST
                        IntPtr gameHwnd = proc.MainWindowHandle;
                        if (gameHwnd == IntPtr.Zero)
                        {
                            uint targetPid = (uint)proc.Id;
                            NativeMethods.EnumWindows((hWnd, lParam) => {
                                NativeMethods.GetWindowThreadProcessId(hWnd, out uint pidRow);
                                if (pidRow == targetPid && NativeMethods.IsWindowVisible(hWnd)) {
                                    gameHwnd = hWnd;
                                    return false;
                                }
                                return true;
                            }, IntPtr.Zero);
                        }

                        // EN: Release input lock and focus game BEFORE sending key
                        NativeMethods.BlockInput(false);
                        if (gameHwnd != IntPtr.Zero)
                        {
                            _logger.LogInfo($"StartSession: Focusing HWND {gameHwnd} BEFORE sending resume key {config.ResumeKey}");
                            NativeMethods.ShowWindowAsync(gameHwnd, NativeMethods.SW_RESTORE);
                            NativeMethods.ShowWindowAsync(gameHwnd, NativeMethods.SW_SHOW);
                            NativeMethods.SetForegroundWindow(gameHwnd);
                            System.Threading.Thread.Sleep(300);
                        }

                        if (!config.ResumeKey.Equals("SUSPEND", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInfo($"StartSession: Sending ResumeKey {config.ResumeKey} to {exeName}");
                            KeyboardSimulator.SendKeyStroke(config.ResumeKey);
                        }

                        if (gameHwnd != IntPtr.Zero)
                        {
                            _logger.LogInfo($"StartSession: Ensuring foreground focus for HWND {gameHwnd}");
                            NativeMethods.SetForegroundWindow(gameHwnd);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during session resume: " + ex.Message);
                _logger.LogError($"StartSession: Failed to resume game (PID: {pid})", ex);
            }
            finally
            {
                // EN: Always refresh layout after resume in case resolution changed back
                RefreshAllOverlays();
            }

            if (pid > 0) _lastPausedPid = 0; // Reset only if we actually tried to resume something

            _overlay?.HideOverlay();
            if (pid == 0) FocusEmulationStation();
            
            // On dÃ©bloque pour permettre le jeu
            NativeMethods.BlockInput(false);
            if (!IsLoadingVideoActive) _overlay?.ShowMessage("SESSION STARTED", isAlert: false, activate: false);
            // EN: Timer is now started globally in Initialize
            // _sessionTimer?.Dispose();
            // _sessionTimer = new System.Threading.Timer(OnSessionTick, null, 1000, 1000);

            // EN: Notify Batrun that session has started so it can apply robust focus
            // FR: Notifier Batrun du dÃ©marrage de session pour appliquer un focus robuste
            SessionStarted?.Invoke(this, EventArgs.Empty);
        }
        
        private void UpdateCurrentGameHwnd()
        {
            // EN: If we already have a valid visible window, keep it
            // FR : Si on a dÃ©jÃ  une fenÃªtre visible valide, on la garde
            if (_currentGameHwnd != IntPtr.Zero && NativeMethods.IsWindow(_currentGameHwnd) && NativeMethods.IsWindowVisible(_currentGameHwnd))
            {
                return;
            }

            // EN: Try to find the window from current foreground ONLY IF we are expecting a game or it's currently launching
            // FR : Tenter de trouver la fenÃªtre via le foreground UNIQUEMENT SI on attend un jeu ou en cours de lancement
            IntPtr fg = NativeMethods.GetForegroundWindow();
            if (fg != IntPtr.Zero && fg != (_overlay?.Handle ?? IntPtr.Zero) && (_isGameRunning || _isGameLaunching))
            {
                NativeMethods.GetWindowThreadProcessId(fg, out uint pid);
                if (pid != (uint)System.Diagnostics.Process.GetCurrentProcess().Id)
                {
                    try {
                        var p = System.Diagnostics.Process.GetProcessById((int)pid);
                        string procName = p.ProcessName.ToLower();

                        // EN: If this is an allowed utility OR a standard excluded process, don't capture it as the main game window
                        // FR : Si c'est un utilitaire autorisÃ© OU un processus exclu standard, ne pas le capturer comme fenÃªtre de jeu principale
                        if (_allowedForegroundWindows.Contains(procName) || _standardExcludedProcesses.Contains(procName))
                        {
                            return;
                        }

                        _currentGameHwnd = fg;
                        UpdateAltTabConfig(pid);
                        return;
                    } catch { }
                }
            }

            // EN: Fallback to enumeration if we have a paused PID
            if (_lastPausedPid > 0)
            {
                uint targetPid = _lastPausedPid;
                NativeMethods.EnumWindows((hWnd, lParam) => {
                    NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);
                    if (pid == targetPid && NativeMethods.IsWindowVisible(hWnd))
                    {
                        _currentGameHwnd = hWnd;
                        UpdateAltTabConfig(targetPid);
                        return false;
                    }
                    return true;
                }, IntPtr.Zero);
            }
            if (!_isSessionActive && !_isTimeoutActive)
            {
                _currentGameHwnd = IntPtr.Zero;
                _allowAltTab = false;
                _allowedForegroundWindows = Array.Empty<string>();
            }
        }

        private void UpdateAltTabConfig(uint pid)
        {
            try
            {
                // EN: Hot-reload JSON config to apply user changes in real-time
                // FR: Recharger Ã  chaud le JSON pour appliquer les modifs utilisateur en temps rÃ©el
                AppKeyManager.Load();

                var proc = Process.GetProcessById((int)pid);
                string exeName = proc.ProcessName;
                if (exeName == _currentExecutable && _isSessionActive) return;

                _currentExecutable = exeName;
                var config = AppKeyManager.GetConfigForExe(exeName);
                
                // EN: If no specific config for this window, fallback to the session's origin launcher config
                // FR: Si pas de config spÃ©cifique, utiliser celle du lanceur d'origine (ex: fjordlauncher => javaw)
                if (config == null && !string.IsNullOrEmpty(_sessionOriginExecutable))
                {
                    config = AppKeyManager.GetConfigForExe(_sessionOriginExecutable);
                }

                if (config == null)
                {
                    config = AppKeyManager.GetConfigForExe("[DEFAULT_PC_GAME]");
                }

                if (config != null)
                {
                    _allowAltTab = config.AllowAltTab;
                    _allowedForegroundWindows = (config.AllowedForegroundWindows ?? "")
                        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim().ToLower())
                        .ToArray();
                    
                    _logger.LogInfo($"Alt-Tab Config for {exeName}: Allow={_allowAltTab}, AllowedCount={_allowedForegroundWindows.Length}");
                }
            }
            catch { }
        }

        private void OnSessionTick(object? state)
        {
            if (_isSessionActive || _isTimeoutActive)
            {
                UpdateCurrentGameHwnd();
            }

            if (_isOperatorUnlocked || _isFreePlay) return;

            if (_isSessionActive)
            {
                HandleSessionTick();
            }
            else if (_isTimeoutActive)
            {
                HandleTimeoutTick();
            }
        }

        private void HandleSessionTick()
        {
            if (_restorationGraceTicks > 0) _restorationGraceTicks--;

            if (!_isEsRunning && _restorationGraceTicks <= 0)
            {
                int minutes = _sessionSecondsRemaining / 60;
                int seconds = _sessionSecondsRemaining % 60;
                string crashMsg = $"ES CLOSED!\n{minutes:D2}:{seconds:D2} LEFT\nPRESS SELECT+START OR CALL OPERATOR";
                _overlay?.UpdateAlertText(crashMsg);
                return;
            }

            if (_restorationGraceTicks > 0)
            {
                _logger.LogInfo($"Restored Session Tick: {_sessionSecondsRemaining}s left (Grace: {_restorationGraceTicks})");
            }

            _sessionSecondsRemaining--;
            UpdateCreditsFromTime(); // EN: Keep credits in sync during countdown / FR: Garder les crÃ©dits synchronisÃ©s pendant le dÃ©compte
            SaveSessionState(); // EN: Save state every second / FR: Sauvegarder l'Ã©tat chaque seconde
            
            if (_sessionSecondsRemaining <= 0)
            {
                EndSession();
                return;
            }
            
            if (_sessionSecondsRemaining <= 60)
            {
                // Moins de 1 minute, on affiche le compte Ã  rebours dynamique
                // EN: Respect screensaver / FR: Respecter le screensaver
                if (!_isScreensaverActive && !IsLoadingVideoActive)
                {
                    _overlay?.ShowCountdown("GAME OVER IN", _sessionSecondsRemaining);
                }
            }
            else if (_sessionSecondsRemaining <= 300) // Moins de 5 minutes
            {
                // Affichage Ã  chaque minute pleine
                if (_sessionSecondsRemaining % 60 == 0 && !_isScreensaverActive && !IsLoadingVideoActive)
                {
                    _overlay?.ShowMessage($"{_sessionSecondsRemaining / 60} MINUTES LEFT", isAlert: false);
                    Task.Delay(3000).ContinueWith(_ => _overlay?.HideOverlay());
                }
            }
        }

        private void HandleTimeoutTick()
        {
            if (!_isTimeoutActive || _isSessionActive || _isOperatorUnlocked) return;

            _timeoutSecondsRemaining--;
            
            if (_timeoutSecondsRemaining % 5 == 0 || _timeoutSecondsRemaining <= 5)
            {
                _logger.LogInfo($"Timeout countdown: {_timeoutSecondsRemaining}s remaining...");
            }

            if (_timeoutSecondsRemaining <= 0)
            {
                _isTimeoutActive = false;
                ExecuteTimeoutAction();
            }
            else if (!_isScreensaverActive)
            {
                string msg = _isNewGameTimeout 
                    ? "CREDIT REQUIRED\nPLEASE ADD COIN\n\nAUTO-CLOSE IN" 
                    : "GAME OVER\nINSERT COIN\n\nAUTO-CLOSE IN";
                
                // EN: Display countdown. isAlert: false for new game to avoid any focus issues.
                _overlay?.ShowCountdown(msg, _timeoutSecondsRemaining, isAlert: !_isNewGameTimeout, activate: false);
            }
        }

        private void ExecuteTimeoutAction()
        {
            if (string.IsNullOrWhiteSpace(_lastTimeoutKey)) return;
            if (!_isEsRunning)
            {
                _logger.LogInfo("ExecuteTimeoutAction: EmulationStation is not running. Aborting auto-close.");
                _isTimeoutActive = false;
                return;
            }

            _isTimeoutActive = false; // EN: Disarm immediately to prevent re-trigger
            string keyToSend = _lastTimeoutKey;
            uint pidToResume = _lastPausedPid;
            uint targetPid = _timeoutTargetPid;

            _logger.LogInfo($"Timeout reached! Executing auto-close command: {keyToSend}");
            IsInternalClosing = true;

            // EN: Run on background thread to avoid blocking the timer infrastructure
            // FR: ExÃ©cution sur thread sÃ©parÃ© pour ne pas bloquer le timer
            Task.Run(() =>
            {
                try
                {
                    // EN: Immediately block physical input to shield ES (Safe while suspended)
                    // FR: Bloquer les entrÃ©es physiques immÃ©diatement pour protÃ©ger ES (SÃ»r mÃªme si suspendu)
                    NativeMethods.BlockInput(true);

                    Process? targetProc = null;

                    // EN: Always retrieve the process first using the dedicated tracking variable
                    // FR: Toujours rÃ©cupÃ©rer en premier le processus via la variable de suivi dÃ©diÃ©e
                    if (targetPid > 0)
                    {
                        try
                        {
                            targetProc = Process.GetProcessById((int)targetPid);
                        }
                        catch { }
                    }

                    // EN: If it's suspended, we must resume it before closing
                    // FR: S'il est suspendu, on doit le relancer avant de le fermer
                    if (pidToResume > 0 && targetProc != null && !targetProc.HasExited)
                    {
                        try
                        {
                            _logger.LogInfo($"Resuming suspended process PID {pidToResume} before focus/timeout command...");
                            NativeMethods.NtResumeProcess(targetProc.Handle);
                            System.Threading.Thread.Sleep(300); 
                            _lastPausedPid = 0;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInfo($"Could not resume PID {pidToResume}: {ex.Message}");
                        }
                    }

                    // EN: REMOVED premature ShowMessage("INSERT COIN") which stole focus and broke keystroke injection!


                    // EN: Find the best window handle to target (MainWindow or first visible window)
                    // FR: Trouver la meilleure fenÃªtre du processus (MainWindow ou premiÃ¨re fenÃªtre visible)
                    IntPtr targetHwnd = IntPtr.Zero;
                    if (targetProc != null && !targetProc.HasExited)
                    {
                        try { targetProc.Refresh(); } catch { }
                        targetHwnd = targetProc.MainWindowHandle;

                        if (targetHwnd == IntPtr.Zero)
                        {
                            // EN: Enumerate all windows to find a visible one belonging to this process
                            uint targetPid = (uint)targetProc.Id;
                            NativeMethods.EnumWindows((hWnd, lParam) =>
                            {
                                NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);
                                if (pid == targetPid && NativeMethods.IsWindowVisible(hWnd))
                                {
                                    targetHwnd = hWnd;
                                    return false; // Stop enumeration
                                }
                                return true;
                            }, IntPtr.Zero);
                        }

                    _logger.LogInfo($"Target window found HWND: {targetHwnd} for PID {targetProc.Id}");
                }

                bool isCloseCommand = keyToSend.Equals("CLOSE", StringComparison.OrdinalIgnoreCase) || 
                                      keyToSend.Equals("EXIT", StringComparison.OrdinalIgnoreCase);

                bool isEscCommand = keyToSend.Equals("ESC", StringComparison.OrdinalIgnoreCase) || 
                                    keyToSend.Equals("ESCAPE", StringComparison.OrdinalIgnoreCase);

                // EN: ALWAYS unblock input during the auto-close process to allow emulators to receive keys 
                // and to allow the user to manually intervene if needed.
                // FR: TOUJOURS dÃ©bloquer l'input pendant l'auto-close pour permettre aux Ã©mulateurs de recevoir les touches
                // et permettre Ã  l'utilisateur d'intervenir manuellement si besoin.
                NativeMethods.BlockInput(false);

                if (targetHwnd != IntPtr.Zero)
                {
                    _logger.LogInfo($"Focusing game window HWND: {targetHwnd} for command {keyToSend}");
                    NativeMethods.ShowWindowAsync(targetHwnd, NativeMethods.SW_SHOW);
                    NativeMethods.SetForegroundWindow(targetHwnd);
                    System.Threading.Thread.Sleep(300);
                }

                _logger.LogInfo($"Executing auto-close command: {keyToSend}");

                // EN: FIRST ATTEMPT
                if (isCloseCommand && targetHwnd != IntPtr.Zero)
                {
                    NativeMethods.PostMessage(targetHwnd, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }
                else if (isEscCommand && targetHwnd != IntPtr.Zero)
                {
                    NativeMethods.PostMessage(targetHwnd, NativeMethods.WM_KEYDOWN, (IntPtr)0x1B, unchecked((IntPtr)0x00010001));
                    System.Threading.Thread.Sleep(100);
                    NativeMethods.PostMessage(targetHwnd, NativeMethods.WM_KEYUP, (IntPtr)0x1B, unchecked((IntPtr)0xC0010001));
                }
                else
                {
                    KeyboardSimulator.SendKeyStroke(keyToSend);
                }

                // EN: WAIT AND RETRY LOGIC (Consistent for all commands)
                if (targetProc != null)
                {
                    int firstWait = 30; // 1.5s
                    while (firstWait > 0 && !targetProc.HasExited)
                    {
                        System.Threading.Thread.Sleep(50);
                        firstWait--;
                    }

                    if (!targetProc.HasExited)
                    {
                        _logger.LogInfo($"Process PID {targetProc.Id} still running after first attempt. Firing {keyToSend} again as fallback!");
                        
                        // SECOND ATTEMPT
                        if (isCloseCommand && targetHwnd != IntPtr.Zero)
                        {
                            // If WM_CLOSE failed, try a physical Alt+F4 as fallback
                            KeyboardSimulator.SendKeyStroke("ALT+F4");
                        }
                        else if (isEscCommand && targetHwnd != IntPtr.Zero)
                        {
                            // If PostMessage ESC failed, try physical ESC
                            KeyboardSimulator.SendKeyStroke("ESC");
                        }
                        else
                        {
                            KeyboardSimulator.SendKeyStroke(keyToSend);
                        }

                        int finalWait = 30; // 1.5s
                        while (finalWait > 0 && !targetProc.HasExited)
                        {
                            System.Threading.Thread.Sleep(50);
                            finalWait--;
                        }
                    }

                    if (!targetProc.HasExited)
                    {
                        _logger.LogInfo($"Process PID {targetProc.Id} still running after double attempt. Final Kill forced (tree).");
                        try { 
                            targetProc.Kill(true); 
                        } 
                        catch 
                        { 
                            try { targetProc.Kill(); } catch { } 
                        }
                    }
                }
                
                // EN: Re-block input only AFTER the entire process is over
                NativeMethods.BlockInput(true);
                        
                _logger.LogInfo($"Timeout command executed: {keyToSend}");
                _isTimeoutActive = false;
                _isGameRunning = false;
                _timeoutTargetPid = 0;
                    
                    // EN: Immediately ensure focus and shield ES after process exit
                    // FR: SÃ©curiser immÃ©diatement le focus et le bouclier ES aprÃ¨s la sortie du processus
                    if (!_isScreensaverActive)
                    {
                        _overlay?.ShowMessage("INSERT COIN", isAlert: true, activate: true);
                    }
                    NativeMethods.BlockInput(true);
                }
                catch (Exception ex)
                {
                    _logger.LogError("ExecuteTimeoutAction error: " + ex.Message);
                }
                finally
                {
                    IsInternalClosing = false;
                    _currentGameHwnd = IntPtr.Zero; // Session strictly over
                }
            });
        }

        private void RefreshAllOverlays()
        {
            _logger.LogInfo("Display settings changed. Refreshing overlays layout...");
            _overlay?.RefreshLayout();
            if (_isOperatorUnlocked) _overlay?.RefreshLayout();
        }

        private bool IsTeknoParrotLoaderRunning()
        {
            string[] loaders = { "TeknoParrotUi", "BudgieLoader", "OpenParrotLoader64", "OpenParrotKonamiLoader", "OpenParrotLoader" };
            return loaders.Any(l => Process.GetProcessesByName(l).Length > 0);
        }

        private void ArmAutoCloseForRunningGame(Process p)
        {
            if (p == null || p.HasExited) return;

            uint activePid = (uint)p.Id;
            string exeName = p.ProcessName;
            AppKeyManager.Load();
            var targetConfig = AppKeyManager.GetConfigForExe(exeName) ?? AppKeyManager.GetConfigForExe("[DEFAULT_PC_GAME]");

            _logger.LogInfo($"ArmAutoCloseForRunningGame: Locking game {exeName} (PID: {activePid})");

            // EN: Suspend the process based on config
            // FR: Suspendre le processus selon la config
            if (targetConfig != null && !string.IsNullOrWhiteSpace(targetConfig.PauseKey))
            {
                if (targetConfig.PauseKey.Equals("SUSPEND", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInfo($"Auto-Close: Suspending {exeName} (PID: {activePid}) via NT API");
                    IntPtr processHandle = NativeMethods.OpenProcess(0x1F0FFF, false, activePid);
                    if (processHandle != IntPtr.Zero)
                    {
                        NativeMethods.NtSuspendProcess(processHandle);
                        NativeMethods.CloseHandle(processHandle);
                        _lastPausedPid = activePid;
                    }
                }
                else
                {
                    // EN: For non-suspend keys (like 'P' in MAME), we must focus first
                    // FR: Pour les touches non-suspend (comme 'P' dans MAME), on doit d'abord focuser
                    NativeMethods.BlockInput(false);
                    IntPtr hWnd = p.MainWindowHandle;
                    if (hWnd != IntPtr.Zero)
                    {
                        NativeMethods.ShowWindowAsync(hWnd, NativeMethods.SW_SHOW);
                        NativeMethods.SetForegroundWindow(hWnd);
                        System.Threading.Thread.Sleep(300);
                        KeyboardSimulator.SendKeyStroke(targetConfig.PauseKey);
                    }
                    NativeMethods.BlockInput(true);
                    _lastPausedPid = 0; 
                }
            }

            // EN: Arm the timeout countdown only if > 0
            // FR: Armer le compte Ã  rebours du timeout seulement si > 0
            _timeoutSecondsRemaining = targetConfig?.TimeoutSeconds ?? 0;
            if (_timeoutSecondsRemaining > 0)
            {
                _lastTimeoutKey = targetConfig?.TimeoutKey ?? "ESC";
                _timeoutTargetPid = activePid;
                _isTimeoutActive = true;
                _isNewGameTimeout = true; // EN: It's a new game boot timeout / FR: C'est un timeout de boot de nouveau jeu
                IsInternalClosing = true;

                // EN: Force immediate display of the countdown overlay WITHOUT "INSERT COIN" text to avoid confusion
                // FR: Forcer l'affichage immÃ©diat de l'overlay de compte Ã  rebours SANS le texte "INSERT COIN" pour Ã©viter la confusion
                string msg = "CREDIT REQUIRED\nPLEASE ADD COIN\n\nAUTO-CLOSE IN";
                if (_overlay != null && _overlay.InvokeRequired && _overlay.IsHandleCreated)
                    _overlay.BeginInvoke(new Action(() => _overlay.ShowCountdown(msg, _timeoutSecondsRemaining, isAlert: false, activate: false)));
                else
                    _overlay?.ShowCountdown(msg, _timeoutSecondsRemaining, isAlert: false, activate: false);

                _isGameRunning = true;
                _logger.LogInfo($"Timeout Auto-Close ARMED for new game: {_timeoutSecondsRemaining}s (Key: {_lastTimeoutKey})");
            }
            else
            {
                _isTimeoutActive = false;
                _isGameRunning = true; // Still running, just not timed
                _logger.LogInfo("Timeout Auto-Close DISABLED for new game (0s). Displaying static message.");

                // EN: Display static message since there is no countdown
                // FR: Afficher un message statique puisqu'il n'y a pas de compte Ã  rebours
                string staticMsg = "CREDIT REQUIRED\nPLEASE ADD COIN";
                if (_overlay != null && _overlay.InvokeRequired && _overlay.IsHandleCreated)
                    _overlay.BeginInvoke(new Action(() => _overlay.ShowMessage(staticMsg, isAlert: false)));
                else
                    _overlay?.ShowMessage(staticMsg, isAlert: false);
            }
        }

        public void EndSession()
        {
            _isGameLaunching = false; // Clear on session end
            if (!_isSessionActive) return; // Prevent double trigger

            _logger.LogInfo("EndSession: Session ending...");
            
            // EN: Save current game to history before everything is cleaned up
            // FR: Sauvegarder la partie actuelle dans l'historique avant le nettoyage
            SaveToHistory();

            _isFreePlay = false; // Force disable freeplay on session end

            // 1. Capture the game window BEFORE overlay takes focus (ONLY if a game is supposed to be running)
            IntPtr fg = NativeMethods.GetForegroundWindow();
            uint activePid = 0;
            if (_isGameRunning && fg != IntPtr.Zero) 
            {
                NativeMethods.GetWindowThreadProcessId(fg, out activePid);
                // EN: Store it immediately for the overlay to monitor
                _currentGameHwnd = fg;
            }
            else
            {
                _currentGameHwnd = IntPtr.Zero;
            }

            // EN: Identify configuration EARLIER to arm timeout immediately
            AppKeyConfig? targetConfig = null;
            string exeName = "";
            try
            {
                if (activePid > 0)
                {
                    var proc = System.Diagnostics.Process.GetProcessById((int)activePid);
                    exeName = proc.ProcessName;
                    if (!exeName.Equals(System.Diagnostics.Process.GetCurrentProcess().ProcessName, StringComparison.OrdinalIgnoreCase) && 
                        !exeName.Equals("emulationstation", StringComparison.OrdinalIgnoreCase))
                    {
                        AppKeyManager.Load();
                        targetConfig = AppKeyManager.GetConfigForExe(exeName);
                    }
                    else if (exeName.Equals("emulationstation", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInfo("EndSession: Foreground is EmulationStation. Ignoring for Auto-Close.");
                        activePid = 0; // EN: Don't target ES for pause/timeout / FR: Ne pas cibler ES pour pause/timeout
                    }
                }
            } catch { }

            if (targetConfig == null)
            {
                if (IsTeknoParrotLoaderRunning())
                {
                    targetConfig = AppKeyManager.GetConfigForExe("TeknoParrotUi") ?? new AppKeyConfig { ExeName = "TP_Fallback", PauseKey = "SUSPEND", ResumeKey = "SUSPEND" };
                }
                else
                {
                    targetConfig = AppKeyManager.GetConfigForExe("[DEFAULT_PC_GAME]");
                }
            }

            if (!_isEsRunning)
            {
                _logger.LogInfo("EndSession: EmulationStation is not running. Aborting session end actions.");
                string timeInfo = _isFreePlay ? "FREE PLAY ACTIVE" : $"{(_sessionSecondsRemaining / 60):D2}:{(_sessionSecondsRemaining % 60):D2} LEFT";
                if (!_isOperatorUnlocked)
                {
                    _overlay?.ShowMessage($"ES CLOSED!\n{timeInfo}\nPRESS SELECT+START OR CALL OPERATOR", isAlert: true);
                }
                _isSessionActive = false;
                _sessionSecondsRemaining = 0;
                TriggerBroadcast();
                return;
            }

            // EN: ARM TIMEOUT NOW so it can start counting even during the 1s sleep
            _logger.LogInfo($"EndSession: Session ending for game: {exeName} (PID: {activePid})");
            
            if (_isGameRunning && activePid > 0 && targetConfig != null && targetConfig.TimeoutSeconds > 0)
            {
                _timeoutSecondsRemaining = targetConfig.TimeoutSeconds;
                _lastTimeoutKey = !string.IsNullOrWhiteSpace(targetConfig.TimeoutKey) ? targetConfig.TimeoutKey : "ESC";
                _timeoutTargetPid = activePid;
                _isTimeoutActive = true;
                _isNewGameTimeout = false; // EN: Normal session end / FR: Fin de session normale
                IsInternalClosing = true;
                _logger.LogInfo($"Timeout Auto-Close ARMED: {_timeoutSecondsRemaining}s (Key: {_lastTimeoutKey})");
            }
            else if (_isGameRunning)
            {
                // EN: NO FALLBACK - Default to 0 (disabled)
                _timeoutSecondsRemaining = 0; 
                _isTimeoutActive = false;
                IsInternalClosing = false;
                _logger.LogInfo("Timeout Auto-Close DISABLED (No config or 0s).");
            }
            else
            {
                _logger.LogInfo("EndSession: No game running. Skipping Auto-Close timeout.");
                _isTimeoutActive = false;
                IsInternalClosing = false;
            }

            _isSessionActive = false;
            _controllerService.IsInputBlocked = true;
            _totalCredits = 0;
            _sessionSecondsRemaining = 0;
            
            // 2. Optional Pause/Suspend based on config
            // EN: Suspend the game BEFORE our UI captures the focus to ensure emulator receives key events
            // FR: Suspendre/Mettre en pause le jeu AVANT que l'UI ne prenne le focus
            try
            {
                if (_isGameRunning && activePid > 0 && targetConfig != null && !string.IsNullOrWhiteSpace(targetConfig.PauseKey))
                {
                    var proc = System.Diagnostics.Process.GetProcessById((int)activePid);
                    if (proc != null && !proc.HasExited)
                    {
                        if (targetConfig.PauseKey.Equals("SUSPEND", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInfo($"EndSession: Suspending {exeName} (PID: {activePid})");
                            IntPtr processHandle = NativeMethods.OpenProcess(0x1F0FFF, false, activePid);
                            if (processHandle != IntPtr.Zero)
                            {
                                NativeMethods.NtSuspendProcess(processHandle);
                                NativeMethods.CloseHandle(processHandle);
                            }
                        }
                        else
                        {
                            NativeMethods.BlockInput(false);
                            _logger.LogInfo($"EndSession: Sending PauseKey {targetConfig.PauseKey} to {exeName} BEFORE overlay captures focus");
                            KeyboardSimulator.SendKeyStroke(targetConfig.PauseKey);
                            System.Threading.Thread.Sleep(150);
                        }
                        _lastPausedPid = activePid; // Set it in ALL cases
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during EndSession Pause/Suspend: " + ex.Message);
            }

            // EN: Show GAME OVER or COUNTDOWN overlay IF ES is running and no screensaver
            // CRITICAL: We do this AFTER pausing the game
            if (_isEsRunning && !_isScreensaverActive)
            {
                _logger.LogInfo("EndSession: Forcing overlay to foreground.");
                Action showUiAction;
                
                if (_isTimeoutActive && _timeoutSecondsRemaining > 0)
                {
                    string msg = "CONTINUE?\nINSERT COIN\n\nAUTO-CLOSE IN";
                    showUiAction = () => _overlay?.ShowCountdown(msg, _timeoutSecondsRemaining, isAlert: true, activate: true);
                }
                else
                {
                    showUiAction = () => _overlay?.ShowMessage("GAME OVER\nINSERT COIN", isAlert: true, activate: true);
                }

                if (_overlay != null && _overlay.InvokeRequired && _overlay.IsHandleCreated)
                    _overlay.Invoke(showUiAction);
                else
                    showUiAction();
            }

            _logger.LogInfo("Session ended. System locked and focus captured.");
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        const uint KEYEVENTF_KEYUP = 0x0002;

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr PostMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        const uint WM_CLOSE = 0x0010;

        private void ForceReleaseModifiers()
        {
            NativeMethods.BlockInput(false);
            keybd_event((byte)Keys.LWin, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event((byte)Keys.RWin, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event((byte)Keys.Menu, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Alt
            keybd_event((byte)Keys.ControlKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event((byte)Keys.ShiftKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        public void TriggerLocalOperatorPrompt()
        {
            if (System.Windows.Forms.Application.OpenForms.Count > 0)
            {
                var mainForm = System.Windows.Forms.Application.OpenForms[0];
                if (mainForm!.InvokeRequired)
                {
                    mainForm.BeginInvoke(new Action(() => OnOperatorRequested(this, EventArgs.Empty)));
                }
                else
                {
                    OnOperatorRequested(this, EventArgs.Empty);
                }
            }
            else
            {
                OnOperatorRequested(this, EventArgs.Empty);
            }
        }

        private void OnOperatorRequested(object? sender, EventArgs e)
        {
            if (_overlay != null && _overlay.InvokeRequired)
            {
                _overlay.Invoke(new Action(() => OnOperatorRequested(sender, e)));
                return;
            }

            if (_isOperatorUnlocked || _isOperatorPromptOpen || IsInternalClosing) return;

            _isOperatorPromptOpen = true;
            _logger.LogInfo("Operator Password Prompt requested.");
            ForceReleaseModifiers();

            using (var form = new System.Windows.Forms.Form())
            {
                form.TopMost = true;
                form.Owner = _overlay;
                form.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
                form.ClientSize = new System.Drawing.Size(660, 160); 
                form.Text = "OPERATOR MODE";
                form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.BackColor = System.Drawing.Color.FromArgb(20, 20, 20);
                form.ForeColor = System.Drawing.Color.White;
                _activeModalForm = form;

                Label lbl = new Label() { Text = "ENTER OPERATOR PASSWORD :", Location = new System.Drawing.Point(20, 25), AutoSize = true, Font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold) };
                var tb = new System.Windows.Forms.TextBox() { Location = new System.Drawing.Point(300, 22), PasswordChar = '*', Width = 280, Font = new System.Drawing.Font("Arial", 12), BackColor = System.Drawing.Color.FromArgb(40, 40, 40), ForeColor = System.Drawing.Color.White, BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle };
                
                AddVirtualKeyboard(form, tb, 160, 520);

                var autoCloseTimer = new System.Windows.Forms.Timer { Interval = 15000 };
                autoCloseTimer.Tick += (s, args) =>
                {
                    autoCloseTimer.Stop();
                    form.Close();
                };

                tb.TextChanged += (s, args) =>
                {
                    autoCloseTimer.Stop();
                    autoCloseTimer.Start();

                    if (tb.Text == _operatorPassword)
                    {
                        autoCloseTimer.Stop();
                        UnlockOperatorMode();
                        form.Close();
                    }
                };

                form.Controls.Add(lbl);
                form.Controls.Add(tb);
                form.FormClosed += (s, args) => 
                {
                    _isOperatorPromptOpen = false;
                    if (_activeModalForm == form) _activeModalForm = null;
                    autoCloseTimer.Dispose();
                };
                
                form.Shown += (s, args) =>
                {
                    form.Activate();
                    form.BringToFront();
                    tb.Focus();
                    autoCloseTimer.Start();

                    Task.Delay(200).ContinueWith(_ => {
                        if (tb.IsHandleCreated) {
                            tb.BeginInvoke(new Action(() => tb.Clear()));
                        }
                    });
                };

                if (_overlay != null) _overlay.CanStealFocus = false;
                try { form.ShowDialog(); } finally { if (_overlay != null) _overlay.CanStealFocus = true; }
            }
        }


        private void OnFreePlayToggleRequested(object? sender, EventArgs e)
        {
            ShowOperatorActionForm(true);
        }

        private void OnAddCreditsRequested(object? sender, EventArgs e)
        {
            ShowOperatorActionForm(false);
        }


        private void ShowOperatorActionForm(bool isFreePlayToggle)
        {
            if (_overlay != null && _overlay.InvokeRequired)
            {
                _overlay.Invoke(new Action(() => ShowOperatorActionForm(isFreePlayToggle)));
                return;
            }

            if (_isOperatorPromptOpen) return;
            _isOperatorPromptOpen = true;
            _logger.LogInfo($"Operator Action Prompt requested: {(isFreePlayToggle ? "FreePlay" : "AddCredits")}");
            ForceReleaseModifiers();

            using (var form = new System.Windows.Forms.Form())
            {
                form.TopMost = true;
                form.Owner = _overlay;
                form.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
                form.ClientSize = new System.Drawing.Size(660, 180);
                form.Text = isFreePlayToggle ? "FREE PLAY MODE" : "ADD CREDITS MODE";
                form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.BackColor = System.Drawing.Color.FromArgb(20, 20, 20);
                form.ForeColor = System.Drawing.Color.White;
                _activeModalForm = form;

                var lblAction = new System.Windows.Forms.Label() { 
                    Text = isFreePlayToggle ? $"CURRENT: {(_isFreePlay ? "ON" : "OFF")}\nENTER PASSWORD TO TOGGLE:" : "CREDITS TO ADD:",
                    Location = new System.Drawing.Point(20, 20),
                    Size = new System.Drawing.Size(260, 45),
                    Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold)
                };
                
                System.Windows.Forms.Control inputField;
                if (isFreePlayToggle)
                {
                    inputField = new System.Windows.Forms.TextBox() { Location = new System.Drawing.Point(300, 25), PasswordChar = '*', Width = 280, Font = new System.Drawing.Font("Arial", 12), BackColor = System.Drawing.Color.FromArgb(40, 40, 40), ForeColor = System.Drawing.Color.White, BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle };
                }
                else
                {
                    inputField = new System.Windows.Forms.NumericUpDown() { 
                        Location = new System.Drawing.Point(300, 20), 
                        Width = 280, 
                        Minimum = 1, 
                        Maximum = 100, 
                        Value = 10,
                        Font = new System.Drawing.Font("Arial", 12),
                        BackColor = System.Drawing.Color.FromArgb(40, 40, 40),
                        ForeColor = System.Drawing.Color.White
                    };
                }

                var lblPass = new System.Windows.Forms.Label() { 
                    Text = isFreePlayToggle ? "" : "PASSWORD:", 
                    Location = new System.Drawing.Point(20, 65), 
                    Size = new System.Drawing.Size(200, 20),
                    Visible = !isFreePlayToggle,
                    Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold)
                };

                var tbPass = new System.Windows.Forms.TextBox() { 
                    Location = new System.Drawing.Point(300, 62), 
                    PasswordChar = '*', 
                    Width = 280,
                    Visible = !isFreePlayToggle,
                    Font = new System.Drawing.Font("Arial", 12),
                    BackColor = System.Drawing.Color.FromArgb(40, 40, 40),
                    ForeColor = System.Drawing.Color.White,
                    BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
                };


                // EN: Add Virtual Keyboard targeting the password field
                // FR: Ajouter le clavier virtuel ciblant le champ de mot de passe
                if (isFreePlayToggle)
                    AddVirtualKeyboard(form, (System.Windows.Forms.TextBox)inputField, 160, 520);
                else
                    AddVirtualKeyboard(form, tbPass, 180, 540);

                if (isFreePlayToggle)
                {
                    var tb = (System.Windows.Forms.TextBox)inputField;
                    tb.TextChanged += (s, args) => {
                        if (tb.Text == _operatorPassword)
                        {
                            SetFreePlay(!_isFreePlay);
                            form.Close();
                        }
                    };
                }
                else
                {
                    tbPass.TextChanged += (s, args) => {
                        if (tbPass.Text == _operatorPassword)
                        {
                            AddManualCredits((int)((System.Windows.Forms.NumericUpDown)inputField).Value);
                            form.Close();
                        }
                    };
                }

                form.Controls.Add(lblAction);
                form.Controls.Add(inputField);
                form.Controls.Add(lblPass);
                form.Controls.Add(tbPass);

                form.FormClosed += (s, args) => {
                    _isOperatorPromptOpen = false;
                    if (_activeModalForm == form) _activeModalForm = null;
                };
                
                form.Shown += (s, args) =>
                {
                    form.Activate();
                    form.BringToFront();
                    inputField.Focus();
                };

                if (_overlay != null) _overlay.CanStealFocus = false;
                try { form.ShowDialog(); } finally { if (_overlay != null) _overlay.CanStealFocus = true; }
            }
        }


        public void UnlockOperatorMode()
        {
            try
            {
                _logger.LogInfo("Operator mode UNLOCKED.");
                _isOperatorUnlocked = true;
                TriggerBroadcast();
                
                if (_overlay != null && _overlay.InvokeRequired && _overlay.IsHandleCreated) 
                    _overlay.Invoke(new Action(() => _overlay.SetMiniMode(true)));
                else if (_overlay != null)
                {
                    // EN: Small delay at startup to ensure the UI is ready for mini-mode
                    // FR: LÃ©ger dÃ©lai au dÃ©marrage pour s'assurer que l'UI est prÃªte pour le mode mini
                    Task.Delay(500).ContinueWith(_ => {
                        if (_overlay != null && _overlay.IsHandleCreated)
                            _overlay.Invoke(new Action(() => _overlay.SetMiniMode(true)));
                        else if (_overlay != null)
                             _overlay.SetMiniMode(true);
                    });
                }

                FocusEmulationStation();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in UnlockOperatorMode", ex);
            }
        }

        private void AddVirtualKeyboard(System.Windows.Forms.Form form, System.Windows.Forms.TextBox targetTb, int compactHeight, int expandedHeight)
        {
            // EN: State for the Shift/Caps mode
            // FR: Ã‰tat pour le mode Shift/Caps
            bool isShifted = false;
            System.Windows.Forms.Panel kbPanel = new System.Windows.Forms.Panel(); // Define first to reference it

            System.Windows.Forms.Button kbToggle = new System.Windows.Forms.Button()
            {
                Text = "VIRTUAL KEYBOARD",
                Size = new System.Drawing.Size(280, 35),
                Location = new System.Drawing.Point(targetTb.Left, targetTb.Bottom + 10),
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                BackColor = System.Drawing.Color.FromArgb(60, 60, 60),
                Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.White
            };

            kbPanel.Location = new System.Drawing.Point(0, compactHeight - 10);
            kbPanel.Size = new System.Drawing.Size(form.ClientSize.Width, 350);
            kbPanel.Visible = false;

            kbToggle.Click += (s, args) =>
            {
                kbPanel.Visible = !kbPanel.Visible;
                form.ClientSize = new System.Drawing.Size(form.ClientSize.Width, kbPanel.Visible ? expandedHeight : compactHeight);
            };

            // EN: Function to generate/refresh keys based on case
            // FR: Fonction pour gÃ©nÃ©rer/rafraÃ®chir les touches selon la casse
            Action? refreshKeys = null; 
            refreshKeys = () =>
            {
                kbPanel.Controls.Clear();
                string[] kbRows = {
                    "1 2 3 4 5 6 7 8 9 0 - _",
                    isShifted ? "Q W E R T Y U I O P" : "q w e r t y u i o p",
                    isShifted ? "A S D F G H J K L M" : "a s d f g h j k l m",
                    isShifted ? "Z X C V B N ! ? @ #" : "z x c v b n ! ? @ #"
                };

                int startYkb = 10;
                foreach (string row in kbRows)
                {
                    int startX = 30;
                    string[] rowKeys = row.Split(' ');
                    foreach (string key in rowKeys)
                    {
                        System.Windows.Forms.Button kBtn = new System.Windows.Forms.Button() { 
                            Text = key, Left = startX, Top = startYkb, Width = 42, Height = 42, 
                            BackColor = System.Drawing.Color.FromArgb(40, 40, 40), ForeColor = System.Drawing.Color.White, 
                            FlatStyle = System.Windows.Forms.FlatStyle.Flat, Font = new System.Drawing.Font("Arial", 11, System.Drawing.FontStyle.Bold) 
                        };
                        kBtn.Click += (ks, ke) => { targetTb.Text += key; targetTb.SelectionStart = targetTb.Text.Length; targetTb.Focus(); };
                        kbPanel.Controls.Add(kBtn);
                        startX += 47;
                    }
                    startYkb += 47;
                }

                // BACKSPACE
                System.Windows.Forms.Button bkspBtn = new System.Windows.Forms.Button() { 
                    Text = "BACKSPACE", Left = 30, Top = startYkb, Width = 160, Height = 45, 
                    BackColor = System.Drawing.Color.DarkRed, ForeColor = System.Drawing.Color.White, 
                    FlatStyle = System.Windows.Forms.FlatStyle.Flat, Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold) 
                };
                bkspBtn.Click += (s, e) => { if (targetTb.Text.Length > 0) targetTb.Text = targetTb.Text.Substring(0, targetTb.Text.Length - 1); targetTb.Focus(); targetTb.SelectionStart = targetTb.Text.Length; };
                kbPanel.Controls.Add(bkspBtn);

                // CLEAR
                System.Windows.Forms.Button clearBtn = new System.Windows.Forms.Button() { 
                    Text = "CLEAR", Left = 200, Top = startYkb, Width = 100, Height = 45, 
                    BackColor = System.Drawing.Color.Orange, ForeColor = System.Drawing.Color.Black, 
                    FlatStyle = System.Windows.Forms.FlatStyle.Flat, Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold) 
                };
                clearBtn.Click += (s, e) => { targetTb.Text = ""; targetTb.Focus(); };
                kbPanel.Controls.Add(clearBtn);

                // SHIFT
                System.Windows.Forms.Button shiftBtn = new System.Windows.Forms.Button() { 
                    Text = "SHIFT", Left = 310, Top = startYkb, Width = 100, Height = 45, 
                    BackColor = isShifted ? System.Drawing.Color.White : System.Drawing.Color.FromArgb(60, 60, 60), 
                    ForeColor = isShifted ? System.Drawing.Color.Black : System.Drawing.Color.White, 
                    FlatStyle = System.Windows.Forms.FlatStyle.Flat, Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold) 
                };
                shiftBtn.Click += (s, e) => { isShifted = !isShifted; refreshKeys?.Invoke(); };
                kbPanel.Controls.Add(shiftBtn);
            };

            refreshKeys();
            form.Controls.Add(kbToggle);
            form.Controls.Add(kbPanel);
        }

        #region IPC Server (Named Pipe)

        private void StartIpcServer()
        {
            if (_ipcCts != null) return;
            _ipcCts = new System.Threading.CancellationTokenSource();
            System.Threading.Tasks.Task.Run(() => IpcServerLoop(_ipcCts.Token));
            _logger.LogInfo("IPC Server (NamedPipe) started for ES integration.");
        }

        private async System.Threading.Tasks.Task IpcServerLoop(System.Threading.CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // EN: Increased instances to allow multiple simultaneous connections if needed
                    // FR: Augmenté les instances pour permettre des connexions simultanées si besoin
                    using (var server = new System.IO.Pipes.NamedPipeServerStream("BatRun_IPC", System.IO.Pipes.PipeDirection.In, 10, System.IO.Pipes.PipeTransmissionMode.Byte, System.IO.Pipes.PipeOptions.Asynchronous))
                    {
                        await server.WaitForConnectionAsync(token);
                        using (var reader = new StreamReader(server))
                        {
                            string? line = await reader.ReadLineAsync();
                            if (!string.IsNullOrEmpty(line))
                            {
                                HandleIpcCommand(line);
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested) _logger.LogWarning("IPC Server error: " + ex.Message);
                    await System.Threading.Tasks.Task.Delay(1000, token);
                }
            }
        }

        private void FocusEmulationStation()
        {
            try
            {
                var processes = Process.GetProcessesByName("emulationstation");
                foreach (var proc in processes)
                {
                    if (proc.HasExited) continue;
                    IntPtr hWnd = proc.MainWindowHandle;
                    if (hWnd == IntPtr.Zero)
                    {
                        uint targetPid = (uint)proc.Id;
                        NativeMethods.EnumWindows((h, lParam) => {
                            NativeMethods.GetWindowThreadProcessId(h, out uint pidRow);
                            if (pidRow == targetPid && NativeMethods.IsWindowVisible(h)) {
                                hWnd = h;
                                return false;
                            }
                            return true;
                        }, IntPtr.Zero);
                    }

                    if (hWnd != IntPtr.Zero)
                    {
                        NativeMethods.AllowSetForegroundWindow((int)proc.Id);
                        NativeMethods.ShowWindowAsync(hWnd, NativeMethods.SW_RESTORE);
                        NativeMethods.ShowWindowAsync(hWnd, NativeMethods.SW_SHOW);
                        NativeMethods.BringWindowToTop(hWnd);
                        NativeMethods.SetForegroundWindow(hWnd);
                        _logger.LogInfo($"Focused EmulationStation (HWND: {hWnd}) for screensaver.");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to focus EmulationStation: " + ex.Message);
            }
        }

        private void HandleIpcCommand(string data)
        {
            try
            {
                _logger.LogInfo($"IPC Command received: {data}");
                var parts = data.Split('|');
                string cmd = parts[0];

                if (cmd == "GAME_START" && parts.Length >= 2)
                {
                    _isGameRunning = true;
                    CloseActiveModal();
                    // EN: If a game starts, we MUST ensure the overlay is hidden immediately,
                    // especially if it was just re-shown by a SCREENSAVER_STOP or NotifyEsReady.
                    // FR: Si un jeu dÃ©marre, on DOIT s'assurer que l'overlay est masquÃ© immÃ©diatement,
                    // surtout s'il vient d'Ãªtre rÃ©-affichÃ© par un SCREENSAVER_STOP ou NotifyEsReady.
                    if (IsLocked && !_isSessionActive)
                    {
                        if (_overlay != null && _overlay.InvokeRequired && _overlay.IsHandleCreated) 
                            _overlay.Invoke(new Action(() => _overlay.HideOverlay()));
                        else 
                            _overlay?.HideOverlay();
                    }

                    if (_isScreensaverActive)
                    {
                        _logger.LogInfo("Game started during screensaver. Postponing lock until process is captured.");
                        _isScreensaverActive = false;
                    }
                    _gameStartTime = DateTime.Now;
                    _isGameLaunching = true; // Block INSERT COIN everywhere
                    IsWebUiSession = false; // EN: Clear UI flag when a real game starts / FR: Effacer le flag UI quand un vrai jeu démarre
                    _controllerService.IsInputBlocked = true; // EN: [BUG R FIX] Block controller hotkey/emergency inputs during launch phase to prevent spurious virtual controller inputs from triggering EmergencyStop
                    _isHistorySaved = false;

                    // EN: args[1] = ROM Path, args[2] = ROM Name, args[3] = Display Name
                    // FR: args[1] = Chemin ROM, args[2] = Nom ROM, args[3] = Nom AffichÃ©
                    string romPath = parts[1].Trim('\"');
                    _currentGameName = parts.Length >= 4 ? parts[3].Trim('\"') : (parts.Length >= 3 ? parts[2].Trim('\"') : Path.GetFileNameWithoutExtension(romPath));
                    
                    // EN: Extract system from roms folder
                    _currentGameSystem = ExtractSystemFromPath(romPath);
                    _gameStartTime = DateTime.Now;
                    _isHistorySaved = false;
                    _sessionOriginExecutable = ""; // EN: Reset origin for new session
                    
                    _logger.LogInfo($"Game Started: {_currentGameName} (System: {_currentGameSystem})");

                    Task.Run(async () => {
                        _logger.LogInfo("Starting game executable capture (60s delay with stability check)...");
                        
                        uint lastCapturedPid = 0;
                        int stabilityCounter = 0;
                        const int RequiredStability = 20; // EN: Must be stable for 10 seconds (20 * 500ms) / FR: Doit Ãªtre stable pendant 10 secondes

                        for (int i = 0; i < 120; i++) // 120 * 500ms = 60s capture
                        {
                            if (!_isGameRunning)
                            {
                                _logger.LogInfo("Executable capture aborted (GAME_END received).");
                                break;
                            }

                            uint currentPid = 0;
                            IntPtr currentHwnd = IntPtr.Zero;
                            IntPtr fg = NativeMethods.GetForegroundWindow();
                            
                            if (fg != IntPtr.Zero)
                            {
                                NativeMethods.GetWindowThreadProcessId(fg, out uint pid);
                                if (pid != (uint)Process.GetCurrentProcess().Id)
                                {
                                    try {
                                        var p = Process.GetProcessById((int)pid);
                                        string name = p.ProcessName.ToLower();
                                        
                                        if (!_standardExcludedProcesses.Contains(name))
                                        {
                                            currentPid = pid;
                                            currentHwnd = fg;
                                        }
                                    } catch { }
                                }
                            }

                            if (currentPid > 0)
                            {
                                if (currentPid == lastCapturedPid)
                                {
                                    stabilityCounter++;
                                    if (stabilityCounter >= RequiredStability)
                                    {
                                        try {
                                            var p = Process.GetProcessById((int)currentPid);

                                            if (string.IsNullOrEmpty(_sessionOriginExecutable))
                                                _sessionOriginExecutable = p.ProcessName;

                                            UpdateAltTabConfig(currentPid);
                                            _logger.LogInfo($"Captured game executable (STABLE): {_currentExecutable} (PID: {currentPid}) [Origin: {_sessionOriginExecutable}]");

                                            // FORCER LE FOCUS - Fix Attract Mode
                                            NativeMethods.AllowSetForegroundWindow((int)currentPid);
                                            NativeMethods.ShowWindowAsync(currentHwnd, NativeMethods.SW_RESTORE);
                                            NativeMethods.ShowWindowAsync(currentHwnd, NativeMethods.SW_SHOW);
                                            NativeMethods.BringWindowToTop(currentHwnd);
                                            NativeMethods.SetForegroundWindow(currentHwnd);
                                            _logger.LogInfo($"Focus forced on captured game: {p.ProcessName}");

                                            _capturedPid = currentPid; // EN: Set for watchdog / FR: Définir pour le watchdog
                                            TriggerBroadcast();

                                            // EN: If no credits/session, lock the game AFTER a safe delay
                                            // FR: Si pas de crÃ©dits/session, verrouiller le jeu APRÃˆS un dÃ©lai de sÃ©curitÃ©
                                            if (IsLocked && !_isSessionActive)
                                            {
                                                _logger.LogInfo("Game started without credits. Waiting 30s for boot...");
                                                await Task.Delay(30000); 
                                                
                                                if (!_isSessionActive && !p.HasExited)
                                                {
                                                    ArmAutoCloseForRunningGame(p);
                                                }
                                                _isGameLaunching = false;
                                                _controllerService.IsInputBlocked = !_isSessionActive; // EN: [BUG R FIX] Restore input state after launch phase ends
                                                }
                                            return;
                                        } catch { }
                                    }
                                }
                                else
                                {
                                    lastCapturedPid = currentPid;
                                    stabilityCounter = 0;
                                    _logger.LogInfo($"Potential game detected: {lastCapturedPid}, waiting for stability...");
                                }
                            }
                            else
                            {
                                lastCapturedPid = 0;
                                stabilityCounter = 0;
                            }
                            await Task.Delay(500);
                        }
                        _isGameLaunching = false; // Timeout on capture
                        _isGameRunning = false; // EN: Reset if nothing captured / FR: Réinitialiser si rien capturé
                        _controllerService.IsInputBlocked = !_isSessionActive; // EN: [BUG R FIX] Restore input state after launch phase ends
                        _currentExecutable = "Unknown Executable";
                        _capturedPid = 0;
                        IsWebLaunch = false;
                        TriggerBroadcast();
                    });
                }
                else if (cmd == "GAME_END")
                {
                _isGameRunning = false;
                _isGameLaunching = false;
                _capturedPid = 0;
                IsWebLaunch = false;
                IsWebUiSession = false; // EN: Clear UI flag on game end / FR: Effacer le flag UI à la fin du jeu
                _controllerService.IsInputBlocked = !_isSessionActive; // EN: [BUG R FIX] Restore input state after launch phase ends
                _isTimeoutActive = false;
                _logger.LogInfo("Game Ended signal received. Saving history before cleanup.");
                SaveToHistory();
                
                // [BATRUN-FORK-v9]: Send /cancel to Sunshine to immediately disconnect the virtual controller.
                // Without this, the controller persists ~27s after stream end, causing input issues on next launch.
                // FR: Envoyer /cancel à Sunshine pour déconnecter immédiatement la manette virtuelle.
                // Sans cela, la manette persiste ~27s après la fin du stream, causant des problèmes d'input au prochain lancement.
                if (_moonlightStreamEnabled)
                {
                    try { _ = _moonlightManager.SendSunshineCancelAsync(); } catch { }
                    
                    // EN: Restart moonlight-web-stream web server on game end to clear stale session and WebRTC state for subsequent connections.
                    // FR: Redémarrer le serveur web moonlight-web-stream à la fin du jeu pour nettoyer l'état de session et WebRTC pour les connexions suivantes.
                    try { _ = _moonlightManager.RestartWebServerAsync(3000); } catch { }
                }
                
                // EN: Restart BatRun API services on game end to clear stale sockets and states.
                // FR: Redémarrer les services d'API BatRun à la fin du jeu pour nettoyer les sockets et états obsolètes.
                try { _ = RestartArcadeApiServicesAsync(3000); } catch { }
                    
                    _currentGameSystem = "RetroBat";
                    _currentGameName = "Idle / Menu";
                    _currentExecutable = "";
                    _sessionOriginExecutable = "";
                    _currentGameHwnd = IntPtr.Zero; // EN: Clear focus anchor / FR : Effacer l'ancrage de focus
                    _gameStartTime = DateTime.MinValue;
                    LastGameEndUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    TriggerBroadcast();
                }
                else if (cmd == "SCREENSAVER_START")
                {
                    _logger.LogInfo("Screensaver started");
                    _isScreensaverActive = true;
                    CloseActiveModal();
                    // EN: Hide overlay and focus ES so screensaver is visible even if Arcade is locked
                    // FR: Masquer l'overlay et focuser ES pour que l'Ã©cran de veille soit visible mÃªme si Arcade est verrouillÃ©
                    if (IsLocked && !_isSessionActive)
                    {
                        if (_overlay != null && _overlay.InvokeRequired && _overlay.IsHandleCreated) 
                            _overlay.Invoke(new Action(() => _overlay.HideOverlay()));
                        else 
                            _overlay?.HideOverlay();
                    }
                    FocusEmulationStation();
                }
                else if (cmd == "SCREENSAVER_STOP")
                {
                    _logger.LogInfo("Screensaver stopped. Waiting for potential game start...");
                    _isScreensaverActive = false;
                    CloseActiveModal();

                    // EN: Use a delayed task to avoid showing INSERT COIN if a GAME_START follows immediately
                    // FR: Utiliser une tÃ¢che diffÃ©rÃ©e pour Ã©viter d'afficher INSERT COIN si un GAME_START suit immÃ©diatement
                    Task.Run(async () => {
                        await Task.Delay(1500); // 1.5s delay
                        
                        // EN: Re-check conditions after delay via secure method
                        // FR: Re-vÃ©rifier les conditions aprÃ¨s le dÃ©lai via la mÃ©thode sÃ©curisÃ©e
                        if (!_isScreensaverActive)
                        {
                             if (!_isOperatorUnlocked)
                             {
                                 ShowInsertCoin();
                             }
                             else
                             {
                                 // EN: Ensure operator mini overlay is visible
                                 // FR: S'assurer que le mini overlay opÃ©rateur est visible
                                 if (_overlay != null && _overlay.IsHandleCreated)
                                     _overlay.Invoke(new Action(() => _overlay.SetMiniMode(true)));
                             }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error handling IPC command", ex);
            }
        }
        #endregion

        private string ExtractSystemFromPath(string path)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(directory)) return "Unknown System";

                // EN: Folder structure is usually .../roms/system/game.zip
                // FR: La structure est gÃ©nÃ©ralement .../roms/systÃ¨me/jeu.zip
                var dirInfo = new DirectoryInfo(directory);
                if (dirInfo.Parent?.Name.Equals("roms", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return dirInfo.Name;
                }
                
                // Fallback: search for "roms" in the path segments
                var segments = path.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < segments.Length - 1; i++)
                {
                    if (segments[i].Equals("roms", StringComparison.OrdinalIgnoreCase))
                    {
                        return segments[i + 1];
                    }
                }
            }
            catch { }
            return "PC / Windows";
        }

        private void SaveToHistory()
        {
            if (_isHistorySaved || _gameStartTime == DateTime.MinValue || _currentGameName == "Idle / Menu") return;
            _isHistorySaved = true;

            try
            {
                var duration = DateTime.Now - _gameStartTime;
                if (duration.TotalSeconds < 2) return; // FR: Seuil rÃ©duit Ã  2s pour les tests

                var entry = new GameHistoryEntry
                {
                    System = _currentGameSystem,
                    GameTitle = _currentGameName,
                    Executable = _currentExecutable,
                    StartTime = _gameStartTime,
                    EndTime = DateTime.Now,
                    Duration = duration.ToString(@"hh\:mm\:ss")
                };

                List<GameHistoryEntry> history;
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(List<GameHistoryEntry>));

                if (File.Exists(_historyFilePath))
                {
                    using (var reader = new StreamReader(_historyFilePath))
                    {
                        history = (List<GameHistoryEntry>?)serializer.Deserialize(reader) ?? new List<GameHistoryEntry>();
                    }
                }
                else
                {
                    history = new List<GameHistoryEntry>();
                }

                history.Insert(0, entry); // EN: Newest first
                if (history.Count > 100) history.RemoveAt(history.Count - 1); // EN: Keep last 100

                using (var writer = new StreamWriter(_historyFilePath))
                {
                    serializer.Serialize(writer, history);
                }
                _logger.LogInfo($"Game history saved: {entry.GameTitle}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to save game history", ex);
            }
        }

        public List<GameHistoryEntry> GetHistory()
        {
            try
            {
                if (!File.Exists(_historyFilePath)) return new List<GameHistoryEntry>();
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(List<GameHistoryEntry>));
                using (var reader = new StreamReader(_historyFilePath))
                {
                    return (List<GameHistoryEntry>?)serializer.Deserialize(reader) ?? new List<GameHistoryEntry>();
                }
            }
            catch { return new List<GameHistoryEntry>(); }
        }

        private void OnLockRequested(object? sender, EventArgs e)
        {
            if (_overlay != null && _overlay.InvokeRequired)
            {
                _overlay.Invoke(new Action(() => OnLockRequested(sender, e)));
                return;
            }

            _logger.LogInfo("Operator Lock requested. Showing password modal.");
            ForceReleaseModifiers();

            if (_overlay != null) _overlay.CanStealFocus = false;
            _isOperatorPromptOpen = true; // EN: Block overlay updates / FR: Bloquer les mises Ã  jour d'overlay

            try
            {
                using (var form = new System.Windows.Forms.Form())
                {
                    form.TopMost = true;
                    form.Owner = _overlay;
                    form.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
                    form.ClientSize = new System.Drawing.Size(660, 160);
                    form.Text = "SYSTEM LOCK - ENTER PASSWORD";
                    form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
                    form.MaximizeBox = false;
                    form.MinimizeBox = false;
                    form.BackColor = System.Drawing.Color.FromArgb(20, 20, 20);
                    form.ForeColor = System.Drawing.Color.White;
                    _activeModalForm = form;

                    Label lbl = new Label() { Text = "ENTER PASSWORD :", Location = new System.Drawing.Point(20, 25), AutoSize = true, Font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold) };
                    var tb = new System.Windows.Forms.TextBox() { Location = new System.Drawing.Point(300, 22), PasswordChar = '*', Width = 280, Font = new System.Drawing.Font("Arial", 12), BackColor = System.Drawing.Color.FromArgb(40, 40, 40), ForeColor = System.Drawing.Color.White, BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle };
                    
                    AddVirtualKeyboard(form, tb, 160, 520);

                    tb.TextChanged += (s, args) =>
                    {
                        if (tb.Text == _operatorPassword)
                        {
                            _logger.LogInfo("Password correct. Locking system.");
                            LockOperatorMode();
                            form.DialogResult = System.Windows.Forms.DialogResult.OK;
                            form.Close();
                        }
                    };

                    form.Controls.Add(lbl);
                    form.Controls.Add(tb);
                    
                    form.Shown += (s, args) =>
                    {
                        form.Activate();
                        form.BringToFront();
                        tb.Focus();
                    };

                    form.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error showing Lock Modal", ex);
            }
            finally
            {
                _isOperatorPromptOpen = false;
                _activeModalForm = null;
                if (_overlay != null) _overlay.CanStealFocus = true;
            }
        }
        public void LockOperatorMode()
        {
            try
            {
                _logger.LogInfo("Operator mode LOCKED. Restoring Arcade mode.");
                _isOperatorUnlocked = false;
                TriggerBroadcast();
            
                if (_overlay != null && _overlay.InvokeRequired && _overlay.IsHandleCreated) 
                    _overlay.Invoke(new Action(() => _overlay.SetMiniMode(false)));
                else 
                    _overlay?.SetMiniMode(false);

                if (_isFreePlay || (_isSessionActive && _sessionSecondsRemaining > 0))
                {
                    // EN: Active session - hide the now full-screen expanded overlay
                    // FR: Session active - masquer l'overlay qui vient d'Ãªtre repassÃ© en plein Ã©cran
                    _overlay?.HideOverlay();
                }
                else
                {
                    // Pas de session, on remet l'Ã©cran "INSERT COIN" via mÃ©thode sÃ©curisÃ©e
                    // EN: Sync ES state just in case it was lost during operator session
                    // FR: Synchroniser l'Ã©tat ES au cas oÃ¹ il aurait Ã©tÃ© perdu pendant la session opÃ©rateur
                    var esRunning = Process.GetProcessesByName("emulationstation").Any(p => {
                        try { return !p.HasExited; } catch { return false; }
                    });
                    NotifyEmulationStationState(esRunning);

                    if (_isEsRunning)
                    {
                        ShowInsertCoin();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in LockOperatorMode", ex);
            }
        }

        #region Network Discovery Service

        private void StartDiscoveryService()
        {
            try
            {
                LoadNetworkHistory();
                _discoveryCts = new System.Threading.CancellationTokenSource();
                
                _udpDiscovery = new System.Net.Sockets.UdpClient();
                _udpDiscovery.Client.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.ReuseAddress, true);
                _udpDiscovery.ExclusiveAddressUse = false;
                _udpDiscovery.Client.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, _discoveryPort));

                System.Threading.Tasks.Task.Run(() => DiscoveryListenLoop(_discoveryCts.Token));
                System.Threading.Tasks.Task.Run(() => DiscoveryBroadcastLoop(_discoveryCts.Token));
                System.Threading.Tasks.Task.Run(() => RemoteStatusPollingLoop(_discoveryCts.Token));
                
                _logger.LogInfo($"Network Discovery Service started on UDP {_discoveryPort}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to start Discovery Service", ex);
            }
        }

        private async System.Threading.Tasks.Task DiscoveryListenLoop(System.Threading.CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpDiscovery!.ReceiveAsync();
                    string message = System.Text.Encoding.UTF8.GetString(result.Buffer);
                    
                    if (message.StartsWith("BATRUN|"))
                    {
                        var parts = message.Split('|');
                        if (parts.Length >= 3)
                        {
                            string name = parts[1];
                            int port = int.TryParse(parts[2], out var p) ? p : 4321;
                            string timeRemaining = parts.Length >= 4 ? parts[3] : "--:--";
                            bool isFp = parts.Length >= 5 && parts[4] == "1";
                            bool isOp = parts.Length >= 6 && parts[5] == "1";
                            string currentGameSystem = parts.Length >= 7 ? parts[6] : "";
                            string currentGameName = parts.Length >= 8 ? parts[7] : "";

                            string ip = result.RemoteEndPoint.Address.ToString();
                            string mac = parts.Length >= 9 ? parts[8] : "";
                            
                            // EN: Use MAC as primary ID if available, fallback to Name (less reliable but better than IP)
                            // FR: Utiliser la MAC comme ID principal si dispo, sinon le Nom (moins fiable mais mieux que l'IP)
                            string id = !string.IsNullOrEmpty(mac) && mac != "UNKNOWN_MAC" ? mac : name;

                            var device = _discoveredMachines.GetOrAdd(id, k => new RemoteMachine { Name = name, IP = ip, Port = port, MacAddress = mac });
                            device.Port = port; // [BATRUN-FIX]: Ensure port updates if changed on the remote machine
                            
                            // EN: Always update latest IP and add to history
                            // FR: Toujours mettre Ã  jour la derniÃ¨re IP et l'ajouter Ã  l'historique
                            device.IP = ip;
                            if (device.IpHistory == null) device.IpHistory = new System.Collections.Generic.HashSet<string>();
                            device.IpHistory.Add(ip);
                            
                            if (!string.IsNullOrEmpty(mac) && mac != "UNKNOWN_MAC") device.MacAddress = mac;
                            
                            device.Name = name;
                            device.TimeRemaining = timeRemaining;
                            device.StatusDisplay = timeRemaining; // [BATRUN-FIX]: Ensure status is visible in Admin UI
                            device.IsFreePlay = isFp;
                            device.IsOperatorUnlocked = isOp;
                            device.RequiresLogin = parts.Length >= 10 ? parts[9] == "1" : false;
                            device.IsMoonlightEnabled = parts.Length >= 11 ? parts[10] == "1" : false;
                            device.CurrentGameSystem = currentGameSystem;
                            device.CurrentGameName = currentGameName;
                            device.LastSeen = DateTime.Now;
                            
                            // [BATRUN-FIX]: Determine if device is local or remote
                            // EN: Only override RequiresLogin/IsMoonlightEnabled for the LOCAL machine.
                            //     Remote machines send their own values via UDP broadcast (parts[9], parts[10]).
                            // FR: Ne surcharger RequiresLogin/IsMoonlightEnabled que pour la machine LOCALE.
                            //     Les machines distantes envoient leurs propres valeurs via broadcast UDP.
                            // [BATRUN-FIX]: Check all local IPs to correctly identify secondary network adapters (VMs, etc.)
                            var localIps = GetAllLocalIPAddresses();
                            if (ip == "127.0.0.1" || localIps.Contains(ip))
                            {
                                device.IsLocal = true;
                                device.RequiresLogin = _publicAccessRequiresLogin;
                                device.IsMoonlightEnabled = _moonlightStreamEnabled;
                            }
                            else
                            {
                                device.IsLocal = false;
                            }

                            SaveNetworkHistory();
                        }
                    }
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested) _logger.LogWarning("Discovery listener error: " + ex.Message);
                }
            }
        }

        private bool _lastLoggedRequiresLogin = false;
        public void ReloadPublicSettings()
        {
            _publicAccessRequiresLogin = _config.ReadBool("Arcade", "PublicAccessRequiresLogin", false);
            _publicAccessAllowRegistration = _config.ReadBool("Arcade", "PublicAccessAllowRegistration", true);
            _adminAllowedIps = _config.ReadValue("Arcade", "AdminAllowedIps", "");
            _operatorPassword = _config.ReadValue("Arcade", "OperatorPassword", "");
            _publicIp = _config.ReadValue("Arcade", "PublicIp", "");
            // [BATRUN-FORK-v4]: Only log when the value actually changes to reduce log spam
            // FR: Ne logger que quand la valeur change réellement pour réduire le spam de logs
            if (_publicAccessRequiresLogin != _lastLoggedRequiresLogin)
            {
                bool becameEnabled = _publicAccessRequiresLogin && !_lastLoggedRequiresLogin;
                _lastLoggedRequiresLogin = _publicAccessRequiresLogin;
                _logger.LogInfo($"[Manager] Public settings reloaded: RequiresLogin={_publicAccessRequiresLogin}");
                
                // EN: If requireLogin was just enabled, purge all guest accounts immediately
                // FR: Si requireLogin vient d'être activé, purger tous les comptes guest immédiatement
                if (becameEnabled)
                {
                    PublicUserManager?.PurgeAllGuests();
                }
            }
            TriggerBroadcast(); // [BATRUN-MOD]: Immediate notify other nodes
        }

        private void TriggerBroadcast()
        {
            try
                {
                    string timeStr = _isFreePlay ? "FREE" : (_isSessionActive ? FormattedTimeRemaining : "INSERT COIN");
                    int fp = _isFreePlay ? 1 : 0;
                    int op = _isOperatorUnlocked ? 1 : 0;
                    int rl = _publicAccessRequiresLogin ? 1 : 0;
                    int ml = _moonlightStreamEnabled ? 1 : 0;
                    byte[] data = System.Text.Encoding.UTF8.GetBytes($"BATRUN|{Environment.MachineName}|{_apiPort}|{timeStr}|{fp}|{op}|{_currentGameSystem}|{_currentGameName}|{GetMacAddress()}|{rl}|{ml}");
                    var endpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Broadcast, _discoveryPort);
                    _ = _udpDiscovery?.SendAsync(data, data.Length, endpoint); // Fire and forget
                }
                catch { }
        }

        private async System.Threading.Tasks.Task DiscoveryBroadcastLoop(System.Threading.CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    string statusStr = _isFreePlay ? "FREE" : (_totalCredits > 0 ? FormattedTimeRemaining : "INSERT COIN");
                    int fp = _isFreePlay ? 1 : 0;
                    int op = _isOperatorUnlocked ? 1 : 0;
                    int rl = _publicAccessRequiresLogin ? 1 : 0;
                    int ml = _moonlightStreamEnabled ? 1 : 0;
                    string myMac = GetMacAddress();
                    byte[] data = System.Text.Encoding.UTF8.GetBytes($"BATRUN|{Environment.MachineName}|{_apiPort}|{statusStr}|{fp}|{op}|{_currentGameSystem}|{_currentGameName}|{myMac}|{rl}|{ml}");

                    // EN: Update local machine status in our own list using the same ID logic as ListenLoop
                    // FR: Mettre Ã  jour la machine locale dans notre liste avec la mÃªme logique d'ID que le ListenLoop
                    string localId = (!string.IsNullOrEmpty(myMac) && myMac != "UNKNOWN_MAC") ? myMac : Environment.MachineName;
                    
                    var local = _discoveredMachines.GetOrAdd(localId, k => new RemoteMachine { Name = Environment.MachineName, IsLocal = true, MacAddress = myMac });
                    local.Name = Environment.MachineName;
                    local.IP = GetLocalIPAddress();
                    local.MacAddress = myMac;
                    local.Port = _apiPort; // [BATRUN-FIX]: Ensure local port is updated in registry
                    local.StatusDisplay = statusStr;
                    local.TimeRemaining = FormattedTimeRemaining;
                    local.IsFreePlay = _isFreePlay;
                    local.IsOperatorUnlocked = _isOperatorUnlocked;
                    local.CurrentGameSystem = _currentGameSystem;
                    local.CurrentGameName = _currentGameName;
                    local.LastSeen = DateTime.Now;
                    if (local.IpHistory == null) local.IpHistory = new System.Collections.Generic.HashSet<string>();
                    local.IpHistory.Add(GetLocalIPAddress());

                    // EN: Use cached broadcast endpoints â€” GetAllNetworkInterfaces() is very expensive, cache for 30s
                    // FR: Utiliser les endpoints de broadcast mis en cache â€” GetAllNetworkInterfaces() est trÃ¨s coÃ»teux
                    if (_cachedBroadcastEndpoints == null || DateTime.Now > _broadcastEndpointExpiry)
                    {
                        var endpoints = new List<System.Net.IPEndPoint>();
                        try
                        {
                            foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                                .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                                            n.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback))
                            {
                                var ipProps = nic.GetIPProperties();
                                foreach (var addr in ipProps.UnicastAddresses)
                                {
                                    if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                    {
                                        // EN: Skip APIPA (169.254.x.x) addresses
                                        // FR: Ignorer les adresses APIPA (169.254.x.x)
                                        if (addr.Address.ToString().StartsWith("169.254")) continue;

                                        // EN: Calculate subnet broadcast if possible, or use Generic Broadcast
                                        // FR: Calculer le broadcast du sous-rÃ©seau si possible, sinon utiliser le Broadcast gÃ©nÃ©rique
                                        var mask = addr.IPv4Mask;
                                        if (mask != null && !mask.Equals(System.Net.IPAddress.Any))
                                        {
                                            byte[] ipBytes = addr.Address.GetAddressBytes();
                                            byte[] maskBytes = mask.GetAddressBytes();
                                            byte[] broadcastBytes = new byte[ipBytes.Length];
                                            for (int i = 0; i < ipBytes.Length; i++)
                                                broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                                            
                                            endpoints.Add(new System.Net.IPEndPoint(new System.Net.IPAddress(broadcastBytes), _discoveryPort));
                                        }
                                        
                                        // EN: Always include the generic broadcast for this card's segment
                                        // FR: Toujours inclure le broadcast gÃ©nÃ©rique pour le segment de cette carte
                                        endpoints.Add(new System.Net.IPEndPoint(System.Net.IPAddress.Broadcast, _discoveryPort));
                                    }
                                }
                            }
                        }
                        catch { }
                        
                        _cachedBroadcastEndpoints = endpoints.Distinct().ToList();
                        _broadcastEndpointExpiry = DateTime.Now.AddSeconds(30);
                    }

                    foreach (var endpoint in _cachedBroadcastEndpoints)
                    {
                        try { await _udpDiscovery!.SendAsync(data, data.Length, endpoint); } catch { }
                    }
                }
                catch { }
                // EN: Broadcast every 5 seconds â€” real-time was triggering expensive GetAllNetworkInterfaces() 60x/minute
                // FR: Broadcast toutes les 5 secondes â€” le temps rÃ©el dÃ©clenchait GetAllNetworkInterfaces() 60x/minute
                await System.Threading.Tasks.Task.Delay(5000, token);
            }
        }

        private async System.Threading.Tasks.Task RemoteStatusPollingLoop(System.Threading.CancellationToken token)
        {
            // EN: Wait a bit after startup
            // FR: Attendre un peu aprÃ¨s le dÃ©marrage
            await System.Threading.Tasks.Task.Delay(5000, token);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var machines = _discoveredMachines.Values.Where(m => !m.IsLocal).ToList();
                    foreach (var m in machines)
                    {
                        if (token.IsCancellationRequested) break;

                        // EN: If machine is offline or barely seen, try direct HTTP update
                        // FR: Si la machine est offline ou Ã  peine vue, tenter une mise Ã  jour HTTP directe
                        bool needsUpdate = (DateTime.Now - m.LastSeen).TotalSeconds > 1;
                        if (needsUpdate)
                        {
                            _ = FetchRemoteStatusAsync(m); // Fire and forget
                        }
                    }
                }
                catch { }
                // EN: Remote polling every 5 seconds â€” HTTP + NIC enumeration is expensive on old CPUs
                // FR: Polling distant toutes les 5 secondes â€” HTTP + Ã©numÃ©ration NICs est coÃ»teux sur vieux CPU
                await System.Threading.Tasks.Task.Delay(5000, token);
            }
        }

        private async System.Threading.Tasks.Task FetchRemoteStatusAsync(RemoteMachine m)
        {
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(3);
                    string url = $"http://{m.IP}:{m.Port}/api/status";
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);
                        if (data != null)
                        {
                            // EN: Update machine data with live values from remote HTTP status
                            // FR: Mettre Ã  jour les donnÃ©es de la machine avec les valeurs rÃ©elles du statut HTTP
                            m.Name = (string)data.machineName ?? m.Name;
                            m.StatusDisplay = (string)data.statusDisplay ?? (string)data.timeRemaining ?? m.StatusDisplay;
                            m.TimeRemaining = (string)data.timeRemaining ?? m.TimeRemaining;
                            m.IsFreePlay = (bool)(data.isFreePlay ?? false);
                            m.IsOperatorUnlocked = (bool)(data.isOperatorUnlocked ?? false);
                            m.CurrentGameSystem = (string)data.currentGameSystem ?? m.CurrentGameSystem;
                            m.CurrentGameName = (string)data.currentGameName ?? m.CurrentGameName;
                            m.MacAddress = (string)data.macAddress ?? m.MacAddress;
                            m.LastSeen = DateTime.Now; // Update LastSeen so it shows as Online
                            
                            // EN: Also add any new IPs to history
                            if (m.IpHistory == null) m.IpHistory = new System.Collections.Generic.HashSet<string>();
                            m.IpHistory.Add(m.IP);
                        }
                    }
                }
            }
            catch { }
        }

        public string GetLocalIPAddress()
        {
            try
            {
                var preferred = GetAllLocalIPAddresses()
                    .Where(ip => !ip.StartsWith("169.254") && !ip.StartsWith("127."))
                    .OrderBy(ip => ip.StartsWith("192.168") ? 0 : (ip.StartsWith("10.") ? 1 : (ip.StartsWith("172.") ? 2 : 3)))
                    .FirstOrDefault();

                if (preferred != null) return preferred;
                
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !ip.ToString().StartsWith("169.254")) 
                        return ip.ToString();
                }
            }
            catch { }
            return "127.0.0.1";
        }

        public System.Collections.Generic.List<string> GetAllLocalIPAddresses()
        {
            var list = new System.Collections.Generic.List<string>();
            try
            {
                foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up))
                {
                    var props = nic.GetIPProperties();
                    foreach (var addr in props.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            list.Add(addr.Address.ToString());
                    }
                }
            }
            catch { }
            return list;
        }

        private void LoadNetworkHistory()
        {
            try
            {
                if (System.IO.File.Exists(_networkFilePath))
                {
                    string json = System.IO.File.ReadAllText(_networkFilePath);
                    var list = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.List<RemoteMachine>>(json);
                    if (list != null)
                    {
                        // EN: Group by MAC address first if available, otherwise by Name
                        // FR: Regrouper d'abord par adresse MAC si disponible, sinon par Nom
                        var groupedByMac = list
                            .Where(m => !string.IsNullOrEmpty(m.MacAddress) && m.MacAddress != "UNKNOWN_MAC")
                            .GroupBy(m => m.MacAddress);

                        foreach (var g in groupedByMac)
                        {
                            var best = g.OrderByDescending(m => m.LastSeen).First();
                            if (best.IpHistory == null) best.IpHistory = new System.Collections.Generic.HashSet<string>();
                            foreach (var m in g)
                            {
                                if (m.IpHistory != null)
                                    foreach (var ip in m.IpHistory) best.IpHistory.Add(ip);
                                best.IpHistory.Add(m.IP);
                            }
                            _discoveredMachines[g.Key] = best;
                        }

                        var groupedByName = list
                            .Where(m => (string.IsNullOrEmpty(m.MacAddress) || m.MacAddress == "UNKNOWN_MAC") 
                                       && !string.IsNullOrEmpty(m.Name) && m.Name != "Unknown")
                            .GroupBy(m => m.Name);

                        foreach (var g in groupedByName)
                        {
                            if (_discoveredMachines.ContainsKey(g.Key)) continue; // Already added by MAC probably

                            var best = g.OrderByDescending(m => m.LastSeen).First();
                            if (best.IpHistory == null) best.IpHistory = new System.Collections.Generic.HashSet<string>();
                            foreach (var m in g)
                            {
                                if (m.IpHistory != null)
                                    foreach (var ip in m.IpHistory) best.IpHistory.Add(ip);
                                best.IpHistory.Add(m.IP);
                            }
                            _discoveredMachines[g.Key] = best;
                        }
                    }
                }
            }
            catch { }
        }

        private void SaveNetworkHistory()
        {
            try
            {
                var list = new System.Collections.Generic.List<RemoteMachine>(_discoveredMachines.Values);
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(list, Newtonsoft.Json.Formatting.Indented);
                System.IO.File.WriteAllText(_networkFilePath, json);
            }
            catch { }
        }

        public System.Collections.Generic.List<RemoteMachine> GetNetworkMachines()
        {
            return new System.Collections.Generic.List<RemoteMachine>(_discoveredMachines.Values);
        }

        /// <summary>
        /// EN: Resolve a machine alias (name or MAC) to its IP:port string for relay.
        /// In HubMode, ONLY known machines from _discoveredMachines are accepted (anti-SSRF).
        /// In non-HubMode, raw IP:port targets are still allowed for backward compatibility.
        /// Returns null if the alias cannot be resolved or is rejected.
        /// FR: Résout un alias de machine (nom ou MAC) en chaîne IP:port pour le relay.
        /// En HubMode, SEULEMENT les machines connues de _discoveredMachines sont acceptées (anti-SSRF).
        /// En mode non-Hub, les cibles IP:port brutes sont toujours autorisées pour compatibilité.
        /// Retourne null si l'alias ne peut pas être résolu ou est rejeté.
        /// </summary>
        public string? ResolveMachineAlias(string target, out string? resolvedMoonlightHost)
        {
            resolvedMoonlightHost = null;

            if (string.IsNullOrEmpty(target))
                return null;

            // EN: Check if target looks like a raw IP:port (contains digits and dots)
            // FR: Vérifier si le target ressemble à une IP:port brute
            bool isRawIpPort = System.Text.RegularExpressions.Regex.IsMatch(
                target, @"^\d{1,3}(\.\d{1,3}){3}(:\d+)?$");

            // EN: Helper to normalize host:port and prevent accidental use of discovery UDP port (4322)
            // FR: Helper pour normaliser host:port et éviter l'utilisation accidentelle du port UDP de découverte (4322)
            (string host, int port) NormalizeHostPort(string hostCandidate, int fallbackPort)
            {
                string host = (hostCandidate ?? "").Trim();
                int port = fallbackPort > 0 ? fallbackPort : 4321;

                if (host.Contains(':'))
                {
                    var parts = host.Split(':');
                    if (parts.Length >= 2)
                    {
                        host = parts[0].Trim();
                        if (int.TryParse(parts[1], out int parsedPort))
                            port = parsedPort;
                    }
                }

                // EN: Relay/API must target HTTP(S) API port, never UDP discovery port
                // FR: Le relay/API doit cibler le port API HTTP(S), jamais le port UDP de découverte
                if (port == _discoveryPort) port = 4321;

                return (host, port);
            }

            // EN: In HubMode, reject raw IP:port targets — only machine aliases allowed
            // FR: En HubMode, rejeter les cibles IP:port brutes — seuls les alias de machine sont autorisés
            if (_hubMode && isRawIpPort)
            {
                _logger.LogWarning($"[HubMode] Rejected raw IP target '{target}' — use machine alias instead");
                return null;
            }

            // EN: Try to find machine by Name (case-insensitive) or MAC address
            // FR: Essayer de trouver la machine par Nom (insensible à la casse) ou adresse MAC
            RemoteMachine? machine = null;

            // Search by Name first
            foreach (var m in _discoveredMachines.Values)
            {
                if (string.Equals(m.Name, target, StringComparison.OrdinalIgnoreCase))
                {
                    machine = m;
                    break;
                }
            }

            // Search by MAC if not found by name
            if (machine == null)
            {
                foreach (var m in _discoveredMachines.Values)
                {
                    if (!string.IsNullOrEmpty(m.MacAddress) &&
                        string.Equals(m.MacAddress, target, StringComparison.OrdinalIgnoreCase))
                    {
                        machine = m;
                        break;
                    }
                }
            }

            if (machine != null)
            {
                // EN: Found known machine — resolve to IP:port
                // FR: Machine connue trouvée — résoudre en IP:port
                string ip = !string.IsNullOrEmpty(machine.IP) ? machine.IP : "";
                int storedPort = machine.Port > 0 ? machine.Port : 4321;
                var normalized = NormalizeHostPort(ip, storedPort);
                ip = normalized.host;
                int port = normalized.port;
                resolvedMoonlightHost = ip; // EN: For Moonlight WS relay, use same IP, port 8080
                string resolved = $"{ip}:{port}";
                _logger.LogInfo($"[Relay] Alias '{target}' resolved to {resolved}");
                return resolved;
            }

            // EN: In non-HubMode, allow raw IP:port for backward compatibility
            // FR: En mode non-Hub, autoriser les IP:port brutes pour compatibilité ascendante
            if (!_hubMode && isRawIpPort)
            {
                // EN: Extract just the IP part for Moonlight host resolution
                // FR: Extraire juste la partie IP pour la résolution de l'hôte Moonlight
                var normalized = NormalizeHostPort(target, 4321);
                resolvedMoonlightHost = normalized.host;
                return $"{normalized.host}:{normalized.port}";
            }

            _logger.LogWarning($"[Relay] Cannot resolve alias '{target}' — machine not found in discovered list");
            return null;
        }


        public void RemoveDiscoveredMachine(string name, string? macAddress = null)
        {
            bool removed = false;
            if (!string.IsNullOrEmpty(macAddress) && macAddress != "UNKNOWN_MAC")
            {
                removed = _discoveredMachines.TryRemove(macAddress, out _);
            }
            
            if (!removed)
            {
                removed = _discoveredMachines.TryRemove(name, out _);
            }

            if (removed)
            {
                SaveNetworkHistory();
            }
        }

        public void AddManualMachine(string ip)
        {
            try
            {
                // EN: Use IP as ID for manual entries if we don't know the name/MAC yet
                // FR: Utiliser l'IP comme ID pour les entrÃ©es manuelles si on ne connaÃ®t pas encore le nom/MAC
                string id = ip; 
                
                if (!_discoveredMachines.ContainsKey(id))
                {
                    var machine = new RemoteMachine { 
                        Name = ip, // Use IP as name until discovered
                        IP = ip, 
                        LastSeen = DateTime.Now.AddSeconds(-25), // Mark as "barely" online
                        IpHistory = new System.Collections.Generic.HashSet<string> { ip }
                    };
                    _discoveredMachines[id] = machine;
                    SaveNetworkHistory();
                    
                    // EN: Trigger an immediate HTTP poll to get real info right away
                    // FR: DÃ©clencher un polling HTTP immÃ©diat pour avoir les vraies infos tout de suite
                    _ = FetchRemoteStatusAsync(machine); 
                }

                // EN: Trigger an immediate broadcast to try to get real info from this IP
                // FR: DÃ©clencher un broadcast immÃ©diat pour tenter d'obtenir les vraies infos
                TriggerBroadcast();
            }
            catch { }
        }

        public string GetMacAddress()
        {
            // EN: Return cached MAC â€” NIC enumeration is expensive, do it only once
            // FR: Retourner la MAC mise en cache â€” l'Ã©numÃ©ration des NICs est coÃ»teuse, ne le faire qu'une fois
            if (_cachedMacAddress != null) return _cachedMacAddress;
            try
            {
                // EN: Priority order: Ethernet > WiFi. Exclude loopback and virtual/VPN adapters (empty MAC)
                // FR: PrioritÃ©: Ethernet > WiFi. Exclure loopback et adaptateurs virtuels/VPN (MAC vide)
                var candidates = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                    .Where(nic => nic.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up
                               && nic.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback
                               && nic.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Tunnel)
                    .Select(nic => new { nic, bytes = nic.GetPhysicalAddress().GetAddressBytes() })
                    .Where(x => x.bytes.Length == 6 && x.bytes.Any(b => b != 0))
                    .OrderBy(x => x.nic.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Ethernet ? 0 : 1);

                var best = candidates.FirstOrDefault();
                if (best != null)
                {
                    _cachedMacAddress = string.Join(":", best.bytes.Select(b => b.ToString("X2")));
                    return _cachedMacAddress;
                }
            }
            catch { }
            _cachedMacAddress = "UNKNOWN_MAC";
            return _cachedMacAddress;
        }

        #endregion

        private IntPtr SetHook(NativeMethods.LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule!)
            {
                IntPtr hModule = NativeMethods.GetModuleHandle(curModule.ModuleName);
                if (hModule == IntPtr.Zero)
                {
                    // Fallback
                    hModule = NativeMethods.GetModuleHandle(null!);
                }
                IntPtr hook = NativeMethods.SetWindowsHookEx(WH_KEYBOARD_LL, proc, hModule, 0);
                if (hook == IntPtr.Zero)
                {
                    _logger.LogError("Failed to set keyboard hook! Error: " + Marshal.GetLastWin32Error());
                }
                return hook;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN || wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP))
            {
                NativeMethods.KBDLLHOOKSTRUCT kbdStruct = (NativeMethods.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeMethods.KBDLLHOOKSTRUCT))!;
                int vkCode = (int)kbdStruct.vkCode;
                bool isAltDown = (kbdStruct.flags & 32) != 0; // LLKHF_ALTDOWN = 0x20
                bool isInjected = (kbdStruct.flags & 0x10) != 0; // EN: Key was injected by SendInput (likely BatRun itself)

                // EN: ALWAYS allow injected keys to reach their target (e.g. PauseKey sent by BatRun)
                // FR: TOUJOURS laisser passer les touches injectÃ©es (ex: touche Pause envoyÃ©e par BatRun)
                if (isInjected)
                {
                    return NativeMethods.CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                if (!_isOperatorUnlocked)
                {
                    bool isCtrl = (NativeMethods.GetAsyncKeyState((int)Keys.ControlKey) & 0x8000) != 0;

                    // Interdire les raccourcis systÃ¨me dangereux
                    bool isSystemShortcut = vkCode == (int)Keys.LWin || vkCode == (int)Keys.RWin ||
                                           (isAltDown && vkCode == (int)Keys.Tab) ||
                                           (isAltDown && vkCode == (int)Keys.Escape) ||
                                           (isCtrl && vkCode == (int)Keys.Escape) ||
                                           (isAltDown && vkCode == (int)Keys.F4);

                    if (isSystemShortcut)
                    {
                        // EN: If Alt-Tab or Alt-F4 is handled for an ACTIVE game session (isGameRunning is critical to avoid dev tools capture)
                        // FR: Si Alt-Tab ou Alt-F4 est gÃ©rÃ© pour une session de jeu ACTIVE (isGameRunning est critique pour Ã©viter la capture des outils dev)
                        if (_isGameRunning && (_isSessionActive || _isFreePlay) && _currentGameHwnd != IntPtr.Zero && (vkCode == (int)Keys.Tab || vkCode == (int)Keys.Escape || vkCode == (int)Keys.F4))
                        {
                            if (vkCode == (int)Keys.Tab && _allowAltTab && (wParam == (IntPtr)WM_SYSKEYDOWN || wParam == (IntPtr)WM_KEYDOWN))
                            {
                                HandleCustomAltTab(true);
                            }
                            else if (vkCode == (int)Keys.F4 && isAltDown && (wParam == (IntPtr)WM_SYSKEYDOWN || wParam == (IntPtr)WM_KEYDOWN))
                            {
                                HandleCustomAltF4();
                            }
                            return (IntPtr)1; // Blocked from Windows Shell
                        }
                        
                        return (IntPtr)1; // Blocked everywhere else
                    }

                    // EN: Check if the user is releasing ALT while the Custom Task Switcher is active
                    // FR: VÃ©rifier si l'utilisateur relÃ¢che ALT pendant que le Task Switcher est actif
                    if (_isTaskSwitcherActive && (vkCode == 0x12 /* VK_MENU / ALT */ || vkCode == 0xA4 || vkCode == 0xA5) && (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP))
                    {
                        HandleCustomAltTab(false); // Execute switch
                    }

                    // EN: Block '9' key while it's being held to avoid typing it into everything (game, then password modal)
                    // FR: Bloquer la touche '9' tant qu'elle est maintenue pour Ã©viter de l'Ã©crire partout (jeu, puis password)
                    if (_isOperatorKeyHeld && (vkCode == (int)Keys.D9 || vkCode == (int)Keys.NumPad9))
                    {
                        return (IntPtr)1;
                    }

                    if (_isGuardianKeyHeld && (vkCode == (int)Keys.D0 || vkCode == (int)Keys.NumPad0))
                    {
                        return (IntPtr)1;
                    }

                    // Si la session n'est pas active (INSERT COIN) et le prompt fermÃ©, bloquer toutes les touches sauf Coins/OpÃ©rateur
                    if (!_isSessionActive && !_isOperatorPromptOpen)
                    {
                        // EN: Allow all keys if the Guardian emergency modal is the active focused window
                        // FR: Autoriser toutes les touches si la modale d'urgence du Guardian est active au premier plan
                        IntPtr fg = NativeMethods.GetForegroundWindow();
                        if (fg != IntPtr.Zero)
                        {
                            System.Text.StringBuilder title = new System.Text.StringBuilder(256);
                            if (NativeMethods.GetWindowText(fg, title, 256) > 0)
                            {
                                if (title.ToString() == "BatRun Guardian Emergency")
                                {
                                    return NativeMethods.CallNextHookEx(_hookID, nCode, wParam, lParam);
                                }
                            }
                        }

                        if (vkCode != _coinVirtualKey && vkCode != (int)Keys.D9 && vkCode != (int)Keys.NumPad9 &&
                            vkCode != (int)Keys.D0 && vkCode != (int)Keys.NumPad0)
                        {
                            return (IntPtr)1; // Blocked
                        }
                    }
                }
            }
            return NativeMethods.CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_hookID != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
            _apiService?.Dispose();
            _sessionTimer?.Dispose();
            _operatorHoldTimer?.Dispose();
            _guardianHoldTimer?.Dispose();
            _overlay?.Dispose();
            _rawInputHandler?.Dispose();
            _discoveryCts?.Cancel();
            _udpDiscovery?.Dispose();
            _moonlightManager.Dispose();
        }

        private void HandleCustomAltTab(bool isKeyDown)
        {
            if (isKeyDown)
            {
                // EN: User pressed Tab while holding Alt
                // FR: L'utilisateur a appuyÃ© sur Tab en maintenant Alt
                if (!_isTaskSwitcherActive)
                {
                    // Initialization / Scan windows
                    _taskSwitcherWindows.Clear();

                    // FR: Mettre Ã  jour Hwnd si le jeu a fermÃ© son splash screen et ouvert sa vraie fenÃªtre
                    if (!string.IsNullOrEmpty(_currentExecutable))
                    {
                        try {
                            var procs = Process.GetProcessesByName(_currentExecutable);
                            if (procs.Length > 0 && procs[0].MainWindowHandle != IntPtr.Zero && procs[0].MainWindowHandle != _currentGameHwnd)
                            {
                                _currentGameHwnd = procs[0].MainWindowHandle;
                            }
                        } catch { }
                    }

                    _taskSwitcherWindows.Add(("Jeu: " + (!string.IsNullOrEmpty(_currentGameName) ? _currentGameName : "En cours"), _currentGameHwnd));

                    // Scan allowed background windows
                    NativeMethods.EnumWindows((hWnd, lParam) =>
                    {
                        if (hWnd != _currentGameHwnd && hWnd != (_overlay?.Handle ?? IntPtr.Zero))
                        {
                            NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);
                            try
                            {
                                var p = Process.GetProcessById((int)pid);
                                string pNameLower = p.ProcessName.ToLower();

                                if (_allowedForegroundWindows.Contains(pNameLower))
                                {
                                    // FR: N'accepter STRICTEMENT que la fenÃªtre principale du processus pour Ã©viter les ghost-windows
                                    // qui ne rÃ©agissent pas au SetForegroundWindow.
                                    if (hWnd == p.MainWindowHandle && hWnd != IntPtr.Zero)
                                    {
                                        string title = p.MainWindowTitle;
                                        if (string.IsNullOrWhiteSpace(title)) title = p.ProcessName;
                                        
                                        // PREVENT DUPLICATES PER PROCESS!
                                        if (!_taskSwitcherWindows.Any(w => w.Name == title || w.Hwnd == hWnd))
                                            _taskSwitcherWindows.Add((title, hWnd));
                                    }
                                }
                            }
                            catch { }
                        }
                        return true;
                    }, IntPtr.Zero);

                    // Ensure minimum 2 windows to switch otherwise no point
                    if (_taskSwitcherWindows.Count > 1)
                    {
                        _isTaskSwitcherActive = true;
                        _taskSwitcherIndex = 1; // Start on the first background app

                        var names = _taskSwitcherWindows.Select(w => w.Name).ToList();
                        _overlay?.ShowTaskSwitcher(names, _taskSwitcherIndex);
                    }
                }
                else
                {
                    // Cycle next
                    if (_taskSwitcherWindows.Count > 1)
                    {
                        _taskSwitcherIndex = (_taskSwitcherIndex + 1) % _taskSwitcherWindows.Count;
                        var names = _taskSwitcherWindows.Select(w => w.Name).ToList();
                        _overlay?.ShowTaskSwitcher(names, _taskSwitcherIndex);
                    }
                }
            }
            else
            {
                // EN: User released Alt
                // FR: L'utilisateur a relÃ¢chÃ© Alt
                if (_isTaskSwitcherActive)
                {
                    _isTaskSwitcherActive = false;
                    _overlay?.HideTaskSwitcher();

                    if (_taskSwitcherWindows.Count > 0 && _taskSwitcherIndex < _taskSwitcherWindows.Count)
                    {
                        IntPtr targetHwnd = _taskSwitcherWindows[_taskSwitcherIndex].Hwnd;
                        if (NativeMethods.IsWindow(targetHwnd))
                        {
                            // EN: Use the aggressive focus theft bypass from the overlay
                            // FR: Utiliser le contournement agressif de vol de focus de l'overlay
                            _overlay?.ForceForegroundWindow(targetHwnd, true);
                        }
                    }
                }
            }
        }

        private async void HandleCustomAltF4()
        {
            if (!_isEnabled || (!_isSessionActive && !_isFreePlay)) return;

            IntPtr targetHwnd = _currentGameHwnd;
            if (targetHwnd == IntPtr.Zero || !NativeMethods.IsWindow(targetHwnd))
            {
                _logger.LogInfo("Alt+F4: No valid game window found, ignoring.");
                return;
            }

            NativeMethods.GetWindowThreadProcessId(targetHwnd, out uint pid);
            if (pid == 0) return;

            try
            {
                var p = Process.GetProcessById((int)pid);
                string procName = p.ProcessName;
                _logger.LogInfo($"Alt+F4 Intercepted: Requesting closure of {procName} (PID: {pid})");

                // SELECTION DU JEU - S'assurer que le focus est bon pour l'envoi de touche
                NativeMethods.SetForegroundWindow(targetHwnd);
                System.Threading.Thread.Sleep(50);

                // 1. ESC Sequence
                _logger.LogInfo("Alt+F4 [Step 1/3]: Sending ESC...");
                KeyboardSimulator.SendKeyStroke("ESC");
                
                await Task.Delay(500);
                if (p.HasExited) {
                    _logger.LogInfo($"Alt+F4 Success: Process {procName} closed with ESC.");
                    return;
                }

                // 2. WM_CLOSE (Standard Alt+F4 behavior emulation via message)
                _logger.LogInfo("Alt+F4 [Step 2/3]: Sending WM_CLOSE...");
                NativeMethods.PostMessage(targetHwnd, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                
                await Task.Delay(1000);
                if (p.HasExited) {
                    _logger.LogInfo($"Alt+F4 Success: Process {procName} closed with WM_CLOSE.");
                    return;
                }

                // 3. Process.Kill (Brutal force)
                _logger.LogInfo("Alt+F4 [Step 3/3]: Process still active. Executing KILL...");
                try {
                    p.Kill(true); // Kill tree
                    _logger.LogInfo($"Alt+F4 Success: Process {procName} killed violently.");
                } catch (Exception kex) {
                    _logger.LogError($"Alt+F4 ERROR: Failed to kill process: {kex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during custom Alt+F4 execution: " + ex.Message);
            }
        }

        /// <summary>
        /// EN: Emergency force stop — kills the running game process using the captured executable name,
        /// stops the Moonlight stream session for all connected players, resets game/session state,
        /// and refreshes the game list. Called from Select+Start 5s hold or from the web API.
        /// FR: Arrêt forcé d'urgence — tue le processus de jeu via l'exécutable capturé,
        /// arrête la session stream Moonlight pour tous les joueurs, réinitialise l'état jeu/session,
        /// et rafraîchit la liste des jeux. Appelé par Select+Start 5s ou l'API web.
        /// </summary>
        public void ForceStopSessionAndGame()
        {
            _logger.LogInfo("[ForceStopSessionAndGame] === EMERGENCY STOP INITIATED ===");

            // [BATRUN-FIX]: Check if we were in a RetroBat UI session BEFORE clearing state
            // FR: Vérifier si on était dans une session interface RetroBat AVANT de réinitialiser l'état
            bool isRbUiSession = IsWebUiSession || (_currentGameName == "[RETROBAT_UI]");
            // FR: Étape 1 — Tuer le processus de jeu via le nom d'exécutable capturé
            try
            {
                if (!string.IsNullOrEmpty(_currentExecutable) && _currentExecutable != "Unknown Executable")
                {
                    string exeName = _currentExecutable.ToLower();
                    _logger.LogInfo($"[ForceStopSessionAndGame] Killing process tree for: {exeName}");

                    var processes = Process.GetProcessesByName(exeName);
                    foreach (var p in processes)
                    {
                        try
                        {
                            if (!p.HasExited)
                            {
                                _logger.LogInfo($"[ForceStopSessionAndGame] Killing PID {p.Id} ({p.ProcessName})");
                                p.Kill(true); // Kill entire process tree
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"[ForceStopSessionAndGame] Failed to kill PID {p.Id}: {ex.Message}");
                            try { p.Kill(); } catch { }
                        }
                        finally { p.Dispose(); }
                    }
                }

                // EN: Also kill via session origin executable if different (e.g. launcher)
                // FR: Aussi tuer via l'exécutable d'origine de session si différent (ex: lanceur)
                if (!string.IsNullOrEmpty(_sessionOriginExecutable) &&
                    _sessionOriginExecutable.ToLower() != _currentExecutable.ToLower())
                {
                    var originProcesses = Process.GetProcessesByName(_sessionOriginExecutable);
                    foreach (var p in originProcesses)
                    {
                        try
                        {
                            if (!p.HasExited)
                            {
                                _logger.LogInfo($"[ForceStopSessionAndGame] Killing origin process PID {p.Id} ({p.ProcessName})");
                                p.Kill(true);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"[ForceStopSessionAndGame] Failed to kill origin PID {p.Id}: {ex.Message}");
                            try { p.Kill(); } catch { }
                        }
                        finally { p.Dispose(); }
                    }
                }

                // EN: Also handle paused PID if game was suspended
                // FR: Gérer aussi le PID en pause si le jeu était suspendu
                if (_lastPausedPid > 0)
                {
                    try
                    {
                        var pausedProc = Process.GetProcessById((int)_lastPausedPid);
                        if (!pausedProc.HasExited)
                        {
                            _logger.LogInfo($"[ForceStopSessionAndGame] Resuming and killing paused PID {_lastPausedPid}");
                            try { NativeMethods.NtResumeProcess(pausedProc.Handle); } catch { }
                            pausedProc.Kill(true);
                        }
                    }
                    catch { }
                    _lastPausedPid = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ForceStopSessionAndGame] Error during game process kill: {ex.Message}");
            }

            // EN: Step 2 — Send /cancel to Sunshine after a 3s delay to disconnect the virtual controller
            // FR: Étape 2 — Envoyer /cancel à Sunshine après un délai de 3s pour déconnecter la manette virtuelle
            if (_moonlightStreamEnabled)
            {
                _logger.LogInfo("[ForceStopSessionAndGame] Delaying Sunshine cancel by 3s...");
                Task.Run(async () => {
                    await Task.Delay(3000);
                    try { await _moonlightManager.SendSunshineCancelAsync(); } catch { }
                });
            }
            
            // EN: Step 3 — Reset all game tracking state
            // FR: Étape 3 — Réinitialiser tout l'état de suivi du jeu
            _isGameRunning = false;
            _isGameLaunching = false;
            _capturedPid = 0;
            IsWebLaunch = false;
            _controllerService.IsInputBlocked = !_isSessionActive; // EN: [BUG R FIX] Restore input state after launch phase ends
            _isTimeoutActive = false;
            _timeoutTargetPid = 0;
            _timeoutSecondsRemaining = 0;
            IsInternalClosing = false;
            _currentGameHwnd = IntPtr.Zero;
            _currentGameSystem = "";
            _currentGameName = "";
            _currentExecutable = "";
            _sessionOriginExecutable = "";
            IsWebUiSession = false;
            _gameStartTime = DateTime.MinValue;
                    LastGameEndUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    ForceStopTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // EN: Signal web clients to close stream / FR: Signaler les clients web de fermer le stream
            
                    // EN: Step 3 — Stop any active session and reset credits
            // FR: Étape 3 — Arrêter toute session active et réinitialiser les crédits
            if (_isSessionActive)
            {
                _isSessionActive = false;
                _isFreePlay = false;
                _totalCredits = 0;
                _sessionSecondsRemaining = 0;
                SaveSessionState();
            }

            // EN: Step 4 — Disarm any timeout that was armed
            // FR: Étape 4 — Désarmer tout timeout armé
            _isTimeoutActive = false;
            _isNewGameTimeout = false;

            // EN: Step 5 — Reset lobby state (stop all players)
            // FR: Étape 5 — Réinitialiser le lobby (arrêter tous les joueurs)
            // The lobby is reset via API, but we also clear local state

            // EN: Step 6 — Unblock input and refresh UI
            // FR: Étape 6 — Débloquer l'input et rafraîchir l'UI
            _controllerService.IsInputBlocked = false;
            NativeMethods.BlockInput(false);

            // EN: Show INSERT COIN overlay if ES is running and not in operator mode
            // FR: Afficher INSERT COIN si ES tourne et pas en mode opérateur
            if (_isEsRunning && !_isOperatorUnlocked)
            {
                _overlay?.ShowMessage("GAME STOPPED\nINSERT COIN", isAlert: true, activate: true);
            }

            // EN: Step 7 — Broadcast the new state to all connected web clients
            // FR: Étape 7 — Diffuser le nouvel état à tous les clients web connectés
            TriggerBroadcast();
            
            // [BATRUN-FORK-v7]: Step 8 — Reload ES interface after force-stop.
            // The SELECT+START hotkey inputs reach ES and open a menu. Reloading ES
            // closes any menu and restores the clean gamelist view.
            // [BATRUN-FIX]: ONLY reload if it was a RetroBat UI session to avoid random crashes on games.
            // FR: Étape 8 — Recharger l'interface ES après un arrêt forcé.
            // UNIQUEMENT s'il s'agissait d'une session interface RetroBat pour éviter les crashs sur les jeux.
            if (isRbUiSession)
            {
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        await System.Threading.Tasks.Task.Delay(2000); // Wait for ES to settle after game process is killed
                        var scraper = new EmulationStationScraper();
                        bool ok = await scraper.ReloadGamesAsync();
                        _logger.LogInfo($"[ForceStopSessionAndGame] ES reload after force-stop: {(ok ? "success" : "failed")}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"[ForceStopSessionAndGame] ES reload error: {ex.Message}");
                    }
                });
            }
            
            _logger.LogInfo("[ForceStopSessionAndGame] === EMERGENCY STOP COMPLETE ===");
            }

        public async void CancelSunshineSessionFromWeb()
        {
            if (_moonlightStreamEnabled && _moonlightManager != null)
            {
                _logger.LogInfo("[ArcadeManager] Canceling Sunshine session requested from web (delaying 3s)...");
                await Task.Delay(3000);
                try { await _moonlightManager.SendSunshineCancelAsync(); } catch { }
            }
        }

        public async void ForceStopGameFromWeb()
        {
            try
            {
                IntPtr targetHwnd = _currentGameHwnd;
                uint pid = 0;
                
                if (targetHwnd != IntPtr.Zero && NativeMethods.IsWindow(targetHwnd))
                {
                    NativeMethods.GetWindowThreadProcessId(targetHwnd, out pid);
                }
                
                // Fallback: Si pas de HWND mais un process identifiÃ©
                if (pid == 0 && !string.IsNullOrEmpty(_currentExecutable) && _currentExecutable != "Unknown Executable")
                {
                    var name = Path.GetFileNameWithoutExtension(_currentExecutable);
                    if (!string.IsNullOrEmpty(name))
                    {
                        var procs = Process.GetProcessesByName(name);
                        if (procs.Length > 0 && !procs[0].HasExited)
                        {
                            pid = (uint)procs[0].Id;
                            targetHwnd = procs[0].MainWindowHandle;
                        }
                    }
                }

                if (pid == 0)
                {
                    _logger.LogInfo("WebStop: No valid game process or window found.");
                    return;
                }

                var p = Process.GetProcessById((int)pid);
                string procName = p.ProcessName;
                _logger.LogInfo($"WebStop: Requesting closure of {procName} (PID: {pid})");

                if (targetHwnd != IntPtr.Zero)
                {
                    NativeMethods.SetForegroundWindow(targetHwnd);
                    System.Threading.Thread.Sleep(50);

                    // 1. ESC Sequence
                    _logger.LogInfo("WebStop [Step 1/3]: Sending ESC...");
                    KeyboardSimulator.SendKeyStroke("ESC");
                    
                    await Task.Delay(500);
                    if (p.HasExited) { _logger.LogInfo("WebStop Success via ESC."); return; }

                    // 2. WM_CLOSE
                    _logger.LogInfo("WebStop [Step 2/3]: Sending WM_CLOSE...");
                    NativeMethods.PostMessage(targetHwnd, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    
                    await Task.Delay(1000);
                    if (p.HasExited) { _logger.LogInfo("WebStop Success via WM_CLOSE."); return; }
                }

                // 3. Process.Kill (Brutal force)
                _logger.LogInfo("WebStop [Step 3/3]: Process still active. Executing KILL...");
                try {
                    p.Kill(true); // Kill tree
                    _logger.LogInfo($"WebStop Success: Process {procName} killed violently.");
                } catch (Exception kex) {
                    _logger.LogError($"WebStop ERROR: Failed to kill process: {kex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during WebStop execution: " + ex.Message);
            }
        }
    }

    public class RemoteMachine
    {
        public string Name { get; set; } = "Unknown";
        public string IP { get; set; } = "";
        public int Port { get; set; } = 4321;
        public string TimeRemaining { get; set; } = "";
        public string StatusDisplay { get; set; } = "";
        public bool IsFreePlay { get; set; } = false;
        public bool IsOperatorUnlocked { get; set; } = false;
        public DateTime LastSeen { get; set; }
        public bool IsLocal { get; set; } = false;
        public string CurrentGameSystem { get; set; } = "";
        public string CurrentGameName { get; set; } = "";
        // EN: MAC address for machine identification across IP changes
        // FR: Adresse MAC pour identifier la machine malgrÃ© les changements d'IP
        public string MacAddress { get; set; } = "";
        public bool RequiresLogin { get; set; } = false;
        public bool IsMoonlightEnabled { get; set; } = false;
        // EN: History of all observed IP addresses for this machine
        // FR: Historique de toutes les IPs observÃ©es pour cette machine
        public System.Collections.Generic.HashSet<string> IpHistory { get; set; } = new System.Collections.Generic.HashSet<string>();

        public bool IsOnline => IsLocal || (DateTime.Now - LastSeen).TotalSeconds < 10; // More aggressive timeout for 1s polling
    }

    public class GameHistoryEntry
    {
        public string System { get; set; } = "";
        public string GameTitle { get; set; } = "";
        public string Executable { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Duration { get; set; } = "";
    }
}


