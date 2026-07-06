using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BatRun.Utils;

namespace BatRun.Core
{
    /// <summary>
    /// EN: Manages the moonlight-web-stream background process and its configuration.
    /// FR: Gère le processus d'arrière-plan moonlight-web-stream et sa configuration.
    /// </summary>
    public class MoonlightManager : IDisposable
    {
        private readonly Logger _logger;
        private readonly IniFile _config;
        private Process? _process;
        private readonly string _rootPath;
        private readonly string _exePath;
        private readonly string _configPath;
        private readonly string _dataPath;
        private bool _disposed = false;
        // EN: Atomic flag to prevent concurrent web-server restarts / FR: Flag atomique pour empêcher les redémarrages concurrents
        private int _restartInProgress = 0;
        private string? _servicePassword;
        private string _serviceUser = "batrun_service";

        public int Port { get; private set; } = 8080;
        public string DataPath => _dataPath;
        public string ServiceUser => _serviceUser;

        private string ApiPrefix => _config.ReadBool("Arcade", "ProxyMoonlight", false) ? "/ml" : "";
        private string? _discoveredPublicIp;
        private DateTime _lastDiscoveryTime = DateTime.MinValue;

        public MoonlightManager(Logger logger, IniFile config)
        {
            _logger = logger;
            _config = config;

            // EN: Simplified path discovery
            // FR: Détection de chemin simplifiée
            string baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
            string localCandidate = Path.Combine(baseDir, ".moonlight-web-stream");

            // EN: If not in local folder, try to find it in common project locations (useful for dev/debug)
            if (!Directory.Exists(localCandidate))
            {
                // FR: Si pas dans le dossier local, chercher à la racine du projet (utile en dev)
                string projectRootCandidate = Path.Combine(baseDir, "..", "..", "..", "..", ".moonlight-web-stream");
                if (Directory.Exists(projectRootCandidate))
                {
                    localCandidate = projectRootCandidate;
                }
            }

            _rootPath = localCandidate;
            _exePath = Path.Combine(_rootPath, "web-server.exe");
            _configPath = Path.Combine(_rootPath, "server", "config.json");
            _dataPath = Path.Combine(_rootPath, "server", "data.json");

            _logger.LogInfo($"[Moonlight] Manager initialized. Root: {_rootPath}");
        }

        public void Start()
        {
            // EN: Always clean up previous instances before starting a new one
            // FR: Toujours nettoyer les instances précédentes avant d'en démarrer une nouvelle
            Stop();

            if (!_config.ReadBool("Arcade", "MoonlightStreamEnabled", false)) return;
            if (_process != null && !_process.HasExited) return;

            if (!File.Exists(_exePath))
            {
                _logger.LogWarning($"[Moonlight] Executable not found at {_exePath}");
                return;
            }

            if (string.IsNullOrEmpty(_servicePassword))
            {
                _servicePassword = Guid.NewGuid().ToString("N")[..12];
            }
            // [BATRUN-FIX]: Run config generation in a way that avoids UI deadlock
            // FR: Exécuter la génération de config d'une manière qui évite le deadlock de l'UI
            Task.Run(async () => await EnsureConfigAsync().ConfigureAwait(false)).GetAwaiter().GetResult();
            EnsureUserInDataJson();

            try
            {
                _logger.LogInfo("[Moonlight] Starting web-server service...");

                var psi = new ProcessStartInfo
                {
                    FileName = _exePath,
                    WorkingDirectory = _rootPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                _process = new Process { StartInfo = psi };

                // EN: Redirect logs to BatRun logger with a prefix, filtering out known spam
                // FR: Rediriger les logs vers le logger BatRun avec un préfixe, en filtrant les spams connus
                _process.OutputDataReceived += (s, e) => {
                    if (!string.IsNullOrWhiteSpace(e.Data) && !e.Data.Contains("Failed to send gamepad event for not registered gamepad"))
                        _logger.LogInfo($"[Moonlight-Web] {e.Data}");
                };
                _process.ErrorDataReceived += (s, e) => {
                    if (!string.IsNullOrWhiteSpace(e.Data) && !e.Data.Contains("Failed to send gamepad event for not registered gamepad"))
                        _logger.LogWarning($"[Moonlight-Web] {e.Data}");
                };

                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                _logger.LogError("[Moonlight] Failed to start process", ex);
            }
        }

        public void Stop()
        {
            // EN: Kill the tracked process if it exists
            // FR: Tuer le processus suivi s'il existe
            if (_process != null)
            {
                try
                {
                    if (!_process.HasExited)
                    {
                        _logger.LogInfo("[Moonlight] Stopping service...");
                        _process.Kill();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[Moonlight] Error while stopping tracked process: {ex.Message}");
                }
                finally
                {
                    _process.Dispose();
                    _process = null;
                }
            }

            // EN: Proactively clean up any orphaned web-server.exe processes to free the port
            // FR: Nettoyer proactivement tout processus web-server.exe orphelin pour libérer le port
            try
            {
                var orphans = Process.GetProcessesByName("web-server");
                foreach (var orphan in orphans)
                {
                    try
                    {
                        // FR: S'assurer que c'est bien notre binaire (optionnel mais plus sûr)
                        _logger.LogInfo($"[Moonlight] Cleaning up orphaned process PID {orphan.Id}");
                        orphan.Kill();
                    }
                    catch { /* Ignore errors for individual orphans / Ignorer les erreurs pour les orphelins individuels */ }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[Moonlight] Error while cleaning up orphaned processes: {ex.Message}");
            }

            // EN: Clean up any orphaned stream.exe processes (Sunshine stream encoder instances)
            // FR: Nettoyer tout processus stream.exe orphelin (instances d'encodage de flux Sunshine)
            try
            {
                var streams = Process.GetProcessesByName("stream");
                foreach (var streamProc in streams)
                {
                    try
                    {
                        _logger.LogInfo($"[Moonlight] Cleaning up orphaned Sunshine stream process PID {streamProc.Id}");
                        streamProc.Kill();
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[Moonlight] Error while cleaning up stream.exe processes: {ex.Message}");
            }
        }

        /// <summary>
        /// EN: Restarts the moonlight-web-stream (web-server.exe) process to clear all internal
        /// session state. This reproduces exactly what happens when BatRun is restarted:
        /// the web-server.exe is killed and re-launched with fresh config.
        ///
        /// WHY: moonlight-web-stream retains internal session/streamer/WebRTC state after a stream
        /// closes from an external IP. This stale state prevents subsequent connections from
        /// establishing successfully. Restarting the process is the only reliable way to
        /// clear this state without restarting BatRun itself.
        ///
        /// SAFE: This does NOT affect BatRun's TCP listener (port 4321), controller state,
        /// session credits, or any other BatRun functionality. Only the moonlight-web-stream
        /// child process is recycled.
        ///
        /// FR: Redémarre le processus moonlight-web-stream (web-server.exe) pour nettoyer tout
        /// l'état de session interne. Reproduit exactement ce qui se passe quand BatRun est
        /// redémarré : le web-server.exe est tué et relancé avec une config fraîche.
        ///
        /// POURQUOI : moonlight-web-stream conserve un état de session/streamer/WebRTC interne
        /// après la fermeture d'un stream depuis une IP externe. Cet état périmé empêche
        /// les connexions suivantes de s'établir. Redémarrer le processus est le seul moyen
        /// fiable de nettoyer cet état sans redémarrer BatRun lui-même.
        /// </summary>
        public async Task RestartWebServerAsync(int delayMs = 3000)
        {
            // EN: Prevent multiple concurrent restarts using atomic compare-and-swap
            // FR: Empêcher les redémarrages concurrents via un compare-and-swap atomique
            if (Interlocked.CompareExchange(ref _restartInProgress, 1, 0) != 0)
            {
                _logger.LogInfo("[Moonlight] ⟳ Web-server restart already in progress, skipping duplicate request.");
                return;
            }

            try
            {
                _logger.LogInfo($"[Moonlight] ⟳ Scheduling web-server restart in {delayMs}ms to clear session state...");

                // EN: Wait for the current stream to fully shut down before restarting.
                //     moonlight-web-stream needs a few seconds to cleanly close the streamer
                //     child process, release ICE agents, and send shutdown signals to Sunshine.
                // FR: Attendre que le stream courant soit complètement arrêté avant de redémarrer.
                //     moonlight-web-stream a besoin de quelques secondes pour fermer proprement
                //     le processus streamer enfant, libérer les agents ICE, et envoyer les
                //     signaux d'arrêt à Sunshine.
                if (delayMs > 0)
                {
                    await Task.Delay(delayMs).ConfigureAwait(false);
                }

                _logger.LogInfo("[Moonlight] ⟳ Restarting web-server.exe to reset session state (reproducing BatRun restart behavior)...");

                // EN: Start() internally calls Stop() first (line 79), which:
                //   1. Kills the tracked web-server.exe process
                //   2. Kills any orphaned web-server.exe processes
                //   3. Kills any orphaned stream.exe processes (Sunshine encoder instances)
                // Then Start() proceeds to:
                //   4. Generate a new service password
                //   5. Regenerate config.json (WebRTC, public IP, etc.)
                //   6. Sync the service user in data.json
                //   7. Launch a fresh web-server.exe
                //
                // FR: Start() appelle Stop() en interne (ligne 79), qui :
                //   1. Tue le processus web-server.exe suivi
                //   2. Tue tout processus web-server.exe orphelin
                //   3. Tue tout processus stream.exe orphelin (instances encodeur Sunshine)
                // Puis Start() continue avec :
                //   4. Génère un nouveau mot de passe de service
                //   5. Régénère config.json (WebRTC, IP publique, etc.)
                //   6. Synchronise l'utilisateur de service dans data.json
                //   7. Lance un web-server.exe frais
                Start();

                _logger.LogInfo("[Moonlight] ⟳ Web-server restart complete. Ready for new external sessions.");
            }
            catch (Exception ex)
            {
                _logger.LogError("[Moonlight] ⟳ Web-server restart failed", ex);
            }
            finally
            {
                // EN: Always release the restart lock, even on failure
                // FR: Toujours libérer le verrou de redémarrage, même en cas d'échec
                Interlocked.Exchange(ref _restartInProgress, 0);
            }
        }

        private void EnsureUserInDataJson()
        {
            if (string.IsNullOrEmpty(_servicePassword)) return;

            if (!File.Exists(_dataPath))
            {
                _logger.LogWarning($"[Moonlight] data.json not found at {_dataPath}. Moonlight might not have started before or we are looking at the wrong place.");
                return;
            }


            try
            {
                string json = File.ReadAllText(_dataPath);
                var data = JsonConvert.DeserializeObject<JObject>(json);
                if (data == null) return;

                var users = data["users"] as JObject;
                if (users == null)
                {
                    users = new JObject();
                    data["users"] = users;
                }

                // EN: Fix "corrupt" roles - Ensure default_settings is not null to prevent frontend crash
                // FR: Réparer les rôles "corrompus" - S'assurer que default_settings n'est pas null pour éviter le crash du frontend
                var roles = data["roles"] as JObject;
                long adminRoleId = 0;
                long batrunRoleId = 0;

                if (roles != null)
                {
                    var defaultSettingsTemplate = JObject.FromObject(new
                    {
                        audioSampleQueueSize = 20,
                        bitrate = 10000,
                        canvasRenderer = false,
                        canvasVsync = false,
                        controllerConfig = new { invertAB = false, invertXY = false, sendIntervalOverride = (object?)null },
                        dataTransport = "auto", // EN: Allow WebRTC (UDP) for better performance when ports are open (UPnP) / FR: Autoriser WebRTC (UDP) pour de meilleures performances si les ports sont ouverts
                        enterFullscreenOnStreamStart = false,
                        forceVideoElementRenderer = false,
                        fps = 60,
                        hdr = false,
                        language = "en",
                        localCursorSensitivity = 1,
                        mouseMode = "follow",
                        mouseScrollMode = "highres",
                        pageStyle = "standard",
                        playAudioLocal = false,
                        sidebarEdge = "left",
                        toggleFullscreenWithKeybind = false,
                        touchMode = "mouseRelative",
                        useSelectElementPolyfill = false,
                        videoCodec = "h264",
                        videoFrameQueueSize = 3,
                        videoSize = "custom",
                        videoSizeCustom = new { height = 1080, width = 1920 }
                    });

                    // EN: First pass: find existing roles
                    // FR: Première passe : trouver les rôles existants
                    foreach (var roleProp in roles.Properties())
                    {
                        var roleObj = roleProp.Value as JObject;
                        if (roleObj != null)
                        {
                            string roleName = roleObj["name"]?.ToString() ?? "";
                            string roleTy = roleObj["ty"]?.ToString() ?? "";

                            if (roleTy == "Admin" || roleName == "Admin")
                            {
                                if (long.TryParse(roleProp.Name, out long id)) adminRoleId = id;
                            }
                            else if (roleName == "BatRun")
                            {
                                if (long.TryParse(roleProp.Name, out long id)) batrunRoleId = id;
                            }
                        }
                    }

                    // EN: Second pass: fix null settings and ensure role validity
                    foreach (var roleProp in roles.Properties())
                    {
                        var roleObj = roleProp.Value as JObject;
                        if (roleObj != null)
                        {
                            string roleName = roleObj["name"]?.ToString() ?? "";
                            if (roleObj["default_settings"] == null || roleObj["default_settings"]!.Type == JTokenType.Null)
                            {
                                roleObj["default_settings"] = defaultSettingsTemplate.DeepClone();
                            }
                            else if (roleName == "BatRun")
                            {
                                // EN: Let user settings prevail / FR: Laisser les réglages utilisateur prévaloir
                                var settings = roleObj["default_settings"] as JObject;
                                if (settings != null)
                                {
                                    // No longer forcing values here to avoid breaking user profiles
                                }
                            }
                        }
                    }
                }

                // EN: If no Admin role found, create one (Moonlight needs at least one Admin)
                if (adminRoleId == 0)
                {
                    adminRoleId = (uint)new Random().Next(100000000, 999999999);
                    if (roles == null) { roles = new JObject(); data["roles"] = roles; }
                    roles[adminRoleId.ToString()] = JObject.FromObject(new {
                        name = "Admin",
                        ty = "Admin",
                        default_settings = new JObject(),
                        permissions = new {
                            allow_add_hosts = true,
                            maximum_bitrate_kbps = (object?)null,
                            allow_codec_h264 = true,
                            allow_codec_h265 = true,
                            allow_codec_av1 = true,
                            allow_hdr = true,
                            allow_transport_webrtc = true,
                            allow_transport_websockets = true
                        }
                    });
                    _logger.LogInfo($"[Moonlight] Created missing Admin role with ID {adminRoleId}");
                }

                // EN: If no BatRun role found, create it (User type for security)
                if (batrunRoleId == 0)
                {
                    batrunRoleId = (uint)new Random().Next(100000000, 999999999);
                    if (roles == null) { roles = new JObject(); data["roles"] = roles; }
                    roles[batrunRoleId.ToString()] = JObject.FromObject(new {
                        name = "BatRun",
                        ty = "User", // EN: Secure, non-admin role / FR: Rôle sécurisé, non-admin
                        default_settings = new JObject(),
                        permissions = new {
                            allow_add_hosts = false,
                            maximum_bitrate_kbps = (object?)null,
                            allow_codec_h264 = true,
                            allow_codec_h265 = true,
                            allow_codec_av1 = true,
                            allow_hdr = true,
                            allow_transport_webrtc = true,
                            allow_transport_websockets = true
                        }
                    });
                    _logger.LogInfo($"[Moonlight] Created BatRun (User) role with ID {batrunRoleId}");
                }

                // EN: Find or create the batrun_service user
                // FR: Trouver ou créer l'utilisateur batrun_service
                string? targetKey = users.Properties().FirstOrDefault(p => p.Value["name"]?.ToString() == _serviceUser)?.Name;

                if (targetKey == null)
                {
                    targetKey = ((uint)new Random().Next(100000000, 999999999)).ToString();
                }

                // EN: Generate PBKDF2-SHA256 hash (150k iterations) compatible with Moonlight
                // FR: Générer un hash PBKDF2-SHA256 (150k itérations) compatible avec Moonlight
                byte[] salt = new byte[16];
                using (var rng = RandomNumberGenerator.Create()) { rng.GetBytes(salt); }

                byte[] hash = Rfc2898DeriveBytes.Pbkdf2(_servicePassword, salt, 150000, HashAlgorithmName.SHA256, 32);

                users[targetKey] = JObject.FromObject(new
                {
                    role_id = batrunRoleId, // EN: Use BatRun (User) role / FR: Utiliser le rôle BatRun (User)
                    name = _serviceUser,
                    password = new
                    {
                        salt = BitConverter.ToString(salt).Replace("-", "").ToLower(),
                        hash = BitConverter.ToString(hash).Replace("-", "").ToLower()
                    },
                    client_unique_id = "BatRun-Service"
                });

                File.WriteAllText(_dataPath, data.ToString(Formatting.Indented));
                _logger.LogInfo($"[Moonlight] Service user '{_serviceUser}' synchronized with BatRun role ID {batrunRoleId}.");

                // EN: Ensure all hosts are accessible to the non-admin service user (clear owner)
                // FR: S'assurer que tous les hôtes sont accessibles au compte de service non-admin (effacer propriétaire)
                var hosts = data["hosts"] as JObject;
                bool hostUpdated = false;
                if (hosts != null)
                {
                    foreach (var hostProp in hosts.Properties())
                    {
                        var hostObj = hostProp.Value as JObject;
                        if (hostObj != null && hostObj["owner"] != null && hostObj["owner"]!.Type != JTokenType.Null)
                        {
                            hostObj["owner"] = null;
                            hostUpdated = true;
                        }
                    }
                }

                if (hostUpdated)
                {
                    File.WriteAllText(_dataPath, data.ToString(Formatting.Indented));
                    _logger.LogInfo("[Moonlight] Hosts ownership cleared to allow arcade account streaming.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("[Moonlight] Failed to update data.json", ex);
            }
        }

        private async Task EnsureConfigAsync()
        {
            // [BATRUN-FIX]: Use ConfigureAwait(false) to prevent deadlocks
            string manualIp = _config.ReadValue("Arcade", "PublicIp", "");
            string? publicIp = !string.IsNullOrEmpty(manualIp) ? manualIp : await GetPublicIpAsync().ConfigureAwait(false);

            // EN: Generate or update config.json to enable header-based authentication and WebRTC stability
            // FR: Générer ou mettre à jour config.json pour activer l'authentification par en-tête et la stabilité WebRTC
            var configContent = new
            {
                web_server = new
                {
                    bind_address = $"0.0.0.0:{Port}",
                    first_login_create_admin = true,
                    first_login_assign_global_hosts = true,
                    // EN: Use /ml prefix only if ProxyMoonlight is enabled.
                    // FR: Utiliser le préfixe /ml uniquement si ProxyMoonlight est activé.
                    url_path_prefix = _config.ReadBool("Arcade", "ProxyMoonlight", false) ? "/ml" : "",
                    forwarded_header = new
                    {
                        username_header = "X-BatRun-User",
                        auto_create_missing_user = true
                    }
                },
                webrtc = new
                {
                    // EN: Use the detected or manual PublicIp.
                    // FR: Utiliser l'IP publique détectée ou manuelle.
                    public_ip = publicIp,
                    ice_servers = new[]
                    {
                        new
                        {
                            urls = new[]
                            {
                                "stun:stun.l.google.com:19302",
                                "stun:global.stun.twilio.com:3478",
                                "stun:stun.cloudflare.com:3478",
                                "stun:stun.services.mozilla.com:3478"
                            }
                        }
                    }
                },
                log = new
                {
                    level_filter = "info"
                }
            };

            try
            {
                var dir = Path.GetDirectoryName(_configPath);
                if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string json = JsonConvert.SerializeObject(configContent, Formatting.Indented);
                File.WriteAllText(_configPath, json);
                _logger.LogInfo($"[Moonlight] Config generated at {_configPath} (WebRTC Public IP: {publicIp ?? "NONE/LOCAL"})");
            }
            catch (Exception ex)
            {
                _logger.LogError("[Moonlight] Failed to write config.json", ex);
            }
        }

        public async Task<string?> GetPublicIpAsync()
        {
            // EN: 1. Manual override from config
            // FR: 1. Saisie manuelle dans la config
            string manualIp = _config.ReadValue("Arcade", "PublicIp", "");
            if (!string.IsNullOrEmpty(manualIp)) return manualIp;

            // EN: 2. Cached discovered IP (valid for 1 hour)
            // FR: 2. IP découverte en cache (valide 1 heure)
            if (!string.IsNullOrEmpty(_discoveredPublicIp) && (DateTime.Now - _lastDiscoveryTime).TotalHours < 1)
            {
                return _discoveredPublicIp;
            }

            // EN: 3. Discover via STUN
            // FR: 3. Découverte via STUN
            _discoveredPublicIp = await DiscoverPublicIpViaStunAsync().ConfigureAwait(false);
            if (!string.IsNullOrEmpty(_discoveredPublicIp))
            {
                _lastDiscoveryTime = DateTime.Now;
            }
            return _discoveredPublicIp;
        }

        private async Task<string?> DiscoverPublicIpViaStunAsync()
        {
            string[] servers = { 
                "stun.l.google.com:19302", 
                "stun.cloudflare.com:3478", 
                "stun.voiparound.com:3478", 
                "stun.schlund.de:3478" 
            };

            foreach (var server in servers)
            {
                try
                {
                    _logger.LogInfo($"[Moonlight] Trying STUN discovery via {server}...");
                    string? ip = await QueryStunServerAsync(server).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(ip))
                    {
                        _logger.LogInfo($"[Moonlight] STUN discovery successful: {ip}");
                        return ip;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[Moonlight] STUN query to {server} failed: {ex.Message}");
                }
            }

            return null;
        }

        private async Task<string?> QueryStunServerAsync(string server)
        {
            try
            {
                var parts = server.Split(':');
                string host = parts[0];
                int port = parts.Length > 1 ? int.Parse(parts[1]) : 3478;

                using (var udp = new UdpClient())
                {
                    udp.Client.SendTimeout = 2000;
                    udp.Client.ReceiveTimeout = 2000;
                    
                    var ipEndpoints = await Dns.GetHostAddressesAsync(host);
                    var endpoint = new IPEndPoint(ipEndpoints.First(a => a.AddressFamily == AddressFamily.InterNetwork), port);

                    // STUN Binding Request
                    byte[] request = new byte[20];
                    request[0] = 0x00; request[1] = 0x01; // Type: Binding Request
                    // Length: 0
                    // Magic Cookie
                    request[4] = 0x21; request[5] = 0x12; request[6] = 0xA4; request[7] = 0x42;
                    // Transaction ID (random)
                    new Random().NextBytes(request.Skip(8).ToArray()); 

                    await udp.SendAsync(request, request.Length, endpoint).ConfigureAwait(false);
                    
                    // [BATRUN-FIX]: Add a small delay/timeout mechanism for receive
                    var receiveTask = udp.ReceiveAsync();
                    if (await Task.WhenAny(receiveTask, Task.Delay(2000)).ConfigureAwait(false) == receiveTask)
                    {
                        var result = await receiveTask.ConfigureAwait(false);
                        byte[] response = result.Buffer;
                        
                        if (response.Length >= 20)
                        {
                            // Parse attributes...
                            // (the rest of the parsing logic stays same, I will wrap it correctly)
                            return ParseStunResponse(response);
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private string? ParseStunResponse(byte[] response)
        {
            try
            {
                // Parse attributes
                int pos = 20;
                while (pos + 4 <= response.Length)
                {
                    int attrType = (response[pos] << 8) | response[pos + 1];
                    int attrLen = (response[pos + 2] << 8) | response[pos + 3];
                    pos += 4;

                    if (pos + attrLen > response.Length) break;

                    if (attrType == 0x0001) // MAPPED-ADDRESS
                    {
                        if (attrLen >= 8)
                        {
                            return $"{response[pos + 4]}.{response[pos + 5]}.{response[pos + 6]}.{response[pos + 7]}";
                        }
                    }
                    else if (attrType == 0x0020) // XOR-MAPPED-ADDRESS
                    {
                        if (attrLen >= 8)
                        {
                            return $"{(byte)(response[pos + 4] ^ 0x21)}.{(byte)(response[pos + 5] ^ 0x12)}.{(byte)(response[pos + 6] ^ 0xA4)}.{(byte)(response[pos + 7] ^ 0x42)}";
                        }
                    }
                    pos += attrLen;
                    if (attrLen % 4 != 0) pos += (4 - (attrLen % 4));
                }
            }
            catch { }
            return null;
        }

        private string? _cachedSessionCookie;
        private DateTime _sessionCookieExpiration = DateTime.MinValue;

        /// <summary>
        /// EN: Obtains a valid mlSession cookie by logging in programmatically.
        /// FR: Obtient un cookie mlSession valide en se connectant programmatiquement.
        /// </summary>
        public async Task<string?> GetSessionCookieAsync(string targetIp = "127.0.0.1")
        {
            if (string.IsNullOrEmpty(_servicePassword)) return null;

            // [BATRUN-FIX-RATE-LIMIT] EN: Cache the session cookie to prevent hitting the express-rate-limit 
            // in moonlight-web-stream, which blocks the second launch from external IPs.
            // FR: Mettre en cache le cookie de session pour éviter d'atteindre la limite de taux (express-rate-limit)
            // dans moonlight-web-stream, qui bloque le deuxième lancement depuis des IP externes.
            if (targetIp == "127.0.0.1" && !string.IsNullOrEmpty(_cachedSessionCookie) && DateTime.UtcNow < _sessionCookieExpiration)
            {
                return _cachedSessionCookie;
            }

            try
            {
                using (var handler = new HttpClientHandler { UseCookies = false })
                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var loginData = new { name = _serviceUser, password = _servicePassword };
                    var content = new StringContent(JsonConvert.SerializeObject(loginData), Encoding.UTF8, "application/json");

                    var response = await client.PostAsync($"http://{targetIp}:{Port}{ApiPrefix}/api/login", content);
                    if (response.IsSuccessStatusCode)
                    {
                        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
                        {
                            var fullCookie = cookies.FirstOrDefault(c => c.StartsWith("mlSession="));
                            if (fullCookie != null)
                            {
                                string finalCookie = fullCookie.Split(';').FirstOrDefault() ?? "";
                                if (targetIp == "127.0.0.1" && !string.IsNullOrEmpty(finalCookie))
                                {
                                    _cachedSessionCookie = finalCookie;
                                    _sessionCookieExpiration = DateTime.UtcNow.AddHours(24);
                                }
                                return finalCookie;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[Moonlight] Failed to obtain session cookie: {ex.Message}");
            }
            return null;
        }

        // [BATRUN-P-v4] — Sunshine /cancel strategy overhaul
        //
        // PROBLEM: The virtual Xbox 360 controller created by Sunshine persists ~28 seconds
        // after stream close (ICE timeout 7s + streamer timeout 20s). This causes a duplicate
        // controller on next launch that steals Player 1 input.
        //
        // PREVIOUS FAILED ATTEMPTS (all on HTTPS port 47984):
        //   v9:  HttpClientHandler → "SSL connection could not be established"
        //   v11: SocketsHttpHandler+SslOptions → same error
        //   v12: TcpClient+SslStream → "HandshakeFailure" (missing client cert)
        //   v13: Client cert loaded (cacert.pem+cakey.pem) → "platform does not support ephemeral keys"
        //        (Windows SChannel doesn't support DHE ephemeral keys)
        //
        // CONCLUSION: HTTPS approach is UNVIABLE on .NET/Windows due to SChannel limitations.
        //
        // NEW STRATEGY (P v4) — Three-tier fallback:
        //
        //   Method 1 (PRIMARY): moonlight-web-stream internal API
        //     POST http://127.0.0.1:8080/host/cancel  (plain HTTP, no SSL!)
        //     The moonlight-web-stream fork already has an IPC connection to Sunshine's streamer.
        //     This is the most reliable and zero-config approach.
        //     Requires: mlSession cookie OR X-BatRun-User header + host_id from GET /hosts
        //
        //   Method 2 (FALLBACK): curl.exe
        //     curl.exe --insecure --max-time 5 https://127.0.0.1:47984/cancel
        //     curl.exe ships with Windows 10+ and uses OpenSSL (not SChannel),
        //     so it handles self-signed certs and DHE ephemeral keys natively.
        //     Zero-config, no certificate loading needed.
        //
        //   Method 3 (LEGACY, disabled): Old SslStream code
        //     Kept as commented-out reference. Will never work on Windows/SChannel.
        //
        // FR: Nouvelle stratégie P v4 — Annulation Sunshine via API interne moonlight-web-stream
        // puis curl.exe en fallback. L'approche HTTPS directe est morte sur .NET/Windows.

        /// <summary>
        /// EN: Sends a cancel command to Sunshine to immediately terminate the streaming session
        /// and disconnect the virtual Xbox 360 controller. Uses a three-tier fallback strategy.
        ///
        /// FR: Envoie une commande d'annulation à Sunshine pour terminer immédiatement la session
        /// de streaming et déconnecter la manette virtuelle Xbox 360. Utilise une stratégie
        /// de fallback à trois niveaux.
        /// </summary>
        public async Task SendSunshineCancelAsync(string targetIp = "127.0.0.1")
        {
            _logger.LogInfo("[Moonlight] Requesting Sunshine session cancel (P v4 strategy)...");

            // ===== METHOD 1: moonlight-web-stream internal API (HTTP, port 8080) =====
            try
            {
                bool success = await CancelViaMoonlightWebStreamAsync(targetIp);
                if (success)
                {
                    _logger.LogInfo("[Moonlight] ✓ Session cancelled via moonlight-web-stream API (Method 1)");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[Moonlight] Method 1 (moonlight-web-stream API) failed: {ex.Message}");
            }

            // ===== METHOD 2: curl.exe (bypasses SChannel, uses OpenSSL) =====
            try
            {
                bool success = await CancelViaCurlExeAsync(targetIp);
                if (success)
                {
                    _logger.LogInfo("[Moonlight] ✓ Session cancelled via curl.exe (Method 2)");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[Moonlight] Method 2 (curl.exe) failed: {ex.Message}");
            }

            _logger.LogWarning("[Moonlight] ✗ All cancel methods failed. Virtual controller will persist ~28s until Sunshine timeout.");
        }

        /// <summary>
        /// EN: [Method 1] Cancels the Sunshine session via moonlight-web-stream's internal API.
        /// Uses plain HTTP on port 8080 — no SSL required. The moonlight-web-stream fork
        /// has an IPC connection to the Sunshine streamer and can relay the cancel command.
        ///
        /// FR: [Méthode 1] Annule la session Sunshine via l'API interne de moonlight-web-stream.
        /// Utilise HTTP simple sur port 8080 — pas de SSL requis. Le fork moonlight-web-stream
        /// a une connexion IPC au streamer Sunshine et peut relayer la commande d'annulation.
        /// </summary>
        private async Task<bool> CancelViaMoonlightWebStreamAsync(string targetIp)
        {
            _logger.LogInfo("[Moonlight] Method 1: Trying moonlight-web-stream /host/cancel API...");

            // EN: First, try using the X-BatRun-User forwarded header (zero-login, zero-config).
            // moonlight-web-stream is configured with forwarded_header in config.json,
            // so any request with X-BatRun-User header is auto-authenticated.
            // FR: D'abord, essayer le header forwarded X-BatRun-User (zero-login, zero-config).
            // moonlight-web-stream est configuré avec forwarded_header dans config.json,
            // donc toute requête avec le header X-BatRun-User est auto-authentifiée.
            int? hostId = await GetFirstHostIdViaHeaderAsync(targetIp);
            if (hostId == null)
            {
                // EN: Fallback: try cookie-based authentication
                // FR: Fallback : essayer l'authentification par cookie
                _logger.LogInfo("[Moonlight] X-BatRun-User header auth didn't find hosts, trying cookie auth...");
                hostId = await GetFirstHostIdViaCookieAsync(targetIp);
            }

            if (hostId == null)
            {
                _logger.LogWarning("[Moonlight] Method 1: No hosts found in moonlight-web-stream. Is Sunshine paired?");
                return false;
            }

            _logger.LogInfo($"[Moonlight] Method 1: Found host_id={hostId}, sending cancel...");

            // EN: Try cancel with header auth first, then cookie auth
            // FR: Essayer l'annulation avec header auth d'abord, puis cookie auth
            bool cancelled = await PostCancelViaHeaderAsync(targetIp, hostId.Value);
            if (!cancelled)
            {
                cancelled = await PostCancelViaCookieAsync(targetIp, hostId.Value);
            }

            return cancelled;
        }

        /// <summary>
        /// EN: Fetches the first host ID from moonlight-web-stream using the X-BatRun-User
        /// forwarded header (no login/cookie needed).
        /// FR: Récupère le premier host_id depuis moonlight-web-stream en utilisant le header
        /// forwarded X-BatRun-User (pas besoin de login/cookie).
        /// </summary>
        private async Task<int?> GetFirstHostIdViaHeaderAsync(string targetIp)
        {
            try
            {
                using (var handler = new HttpClientHandler { UseCookies = false })
                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(3);
                    var request = new HttpRequestMessage(HttpMethod.Get, $"http://{targetIp}:{Port}{ApiPrefix}/hosts");
                    request.Headers.Add("X-BatRun-User", _serviceUser);

                    var response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        string body = await response.Content.ReadAsStringAsync();
                        return ParseFirstHostId(body);
                    }
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// EN: Fetches the first host ID from moonlight-web-stream using cookie authentication.
        /// FR: Récupère le premier host_id depuis moonlight-web-stream en utilisant l'authentification par cookie.
        /// </summary>
        private async Task<int?> GetFirstHostIdViaCookieAsync(string targetIp)
        {
            try
            {
                string? cookie = await GetSessionCookieAsync(targetIp);
                if (string.IsNullOrEmpty(cookie)) return null;

                using (var handler = new HttpClientHandler { UseCookies = false })
                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(3);
                    var request = new HttpRequestMessage(HttpMethod.Get, $"http://{targetIp}:{Port}{ApiPrefix}/hosts");
                    request.Headers.Add("Cookie", cookie);

                    var response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        string body = await response.Content.ReadAsStringAsync();
                        return ParseFirstHostId(body);
                    }
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// EN: Parses the /hosts response JSON to extract the first host_id.
        /// The response is a JSON streaming response: {"data": [...], ...}
        /// Each host object has a "host_id" field.
        /// FR: Parse la réponse JSON de /hosts pour extraire le premier host_id.
        /// </summary>
        private int? ParseFirstHostId(string responseBody)
        {
            try
            {
                var json = JObject.Parse(responseBody);

                // EN: The /hosts streaming JSON response wraps hosts in "data" array
                // FR: La réponse JSON streaming de /hosts encapsule les hôtes dans le tableau "data"
                var dataArray = json["data"] as JArray;
                if (dataArray != null && dataArray.Count > 0)
                {
                    var firstHost = dataArray[0] as JObject;
                    if (firstHost != null)
                    {
                        var hostIdToken = firstHost["host_id"];
                        if (hostIdToken != null)
                        {
                            int hostId = hostIdToken.Value<int>();
                            _logger.LogInfo($"[Moonlight] Parsed host_id={hostId} from /hosts response");
                            return hostId;
                        }
                    }
                }

                // EN: Fallback: try parsing the response body directly as JArray (non-streaming format)
                // FR: Fallback : essayer de parser le corps de la réponse directement comme JArray (format non-streaming)
                JArray? topArray = null;
                try { topArray = JArray.Parse(responseBody); } catch { }
                if (topArray != null && topArray.Count > 0)
                {
                    var firstHost = topArray[0] as JObject;
                    if (firstHost != null)
                    {
                        var hostIdToken = firstHost["host_id"];
                        if (hostIdToken != null)
                        {
                            return hostIdToken.Value<int>();
                        }
                    }
                }

                _logger.LogWarning($"[Moonlight] Could not parse host_id from /hosts response: {responseBody.Truncate(200)}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[Moonlight] Error parsing /hosts response: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// EN: Sends POST /host/cancel with X-BatRun-User header authentication.
        /// FR: Envoie POST /host/cancel avec authentification par header X-BatRun-User.
        /// </summary>
        private async Task<bool> PostCancelViaHeaderAsync(string targetIp, int hostId)
        {
            try
            {
                using (var handler = new HttpClientHandler { UseCookies = false })
                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var cancelPayload = new { host_id = hostId };
                    var content = new StringContent(JsonConvert.SerializeObject(cancelPayload), Encoding.UTF8, "application/json");

                    var request = new HttpRequestMessage(HttpMethod.Post, $"http://{targetIp}:{Port}{ApiPrefix}/host/cancel");
                    request.Headers.Add("X-BatRun-User", _serviceUser);
                    request.Content = content;

                    var response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        string body = await response.Content.ReadAsStringAsync();
                        _logger.LogInfo($"[Moonlight] /host/cancel (header auth) response: {response.StatusCode} - {body}");
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning($"[Moonlight] /host/cancel (header auth) failed: HTTP {(int)response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[Moonlight] /host/cancel (header auth) error: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// EN: Sends POST /host/cancel with cookie authentication.
        /// FR: Envoie POST /host/cancel avec authentification par cookie.
        /// </summary>
        private async Task<bool> PostCancelViaCookieAsync(string targetIp, int hostId)
        {
            try
            {
                string? cookie = await GetSessionCookieAsync(targetIp);
                if (string.IsNullOrEmpty(cookie)) return false;

                using (var handler = new HttpClientHandler { UseCookies = false })
                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var cancelPayload = new { host_id = hostId };
                    var content = new StringContent(JsonConvert.SerializeObject(cancelPayload), Encoding.UTF8, "application/json");

                    var request = new HttpRequestMessage(HttpMethod.Post, $"http://{targetIp}:{Port}{ApiPrefix}/host/cancel");
                    request.Headers.Add("Cookie", cookie);
                    request.Content = content;

                    var response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        string body = await response.Content.ReadAsStringAsync();
                        _logger.LogInfo($"[Moonlight] /host/cancel (cookie auth) response: {response.StatusCode} - {body}");
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning($"[Moonlight] /host/cancel (cookie auth) failed: HTTP {(int)response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[Moonlight] /host/cancel (cookie auth) error: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// EN: [Method 2] Cancels the Sunshine session via curl.exe, which uses OpenSSL
        /// instead of Windows SChannel. curl.exe natively supports self-signed certificates
        /// (--insecure) and DHE ephemeral keys, bypassing all the SChannel limitations
        /// that made the .NET HTTPS approach fail.
        ///
        /// Windows 10+ ships curl.exe in System32. On older systems, this will gracefully fail.
        ///
        /// FR: [Méthode 2] Annule la session Sunshine via curl.exe, qui utilise OpenSSL
        /// au lieu de Windows SChannel. curl.exe supporte nativement les certificats auto-signés
        /// (--insecure) et les clés DHE éphémères, contournant toutes les limitations SChannel
        /// qui ont fait échouer l'approche HTTPS .NET.
        /// </summary>
        private async Task<bool> CancelViaCurlExeAsync(string targetIp)
        {
            int sunshineHttpsPort = _config.ReadInt("Arcade", "SunshineHttpsPort", 47984);
            string url = $"https://{targetIp}:{sunshineHttpsPort}/cancel";

            // EN: Method 2a: Try curl with forced TLS 1.2 to avoid SEC_E_ILLEGAL_MESSAGE
            // FR: Méthode 2a: Tenter curl avec TLS 1.2 forcé pour éviter l'erreur SEC_E_ILLEGAL_MESSAGE
            _logger.LogInfo($"[Moonlight] Method 2a: Trying curl.exe --insecure --tlsv1.2 {url}...");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "curl.exe",
                    // EN: Added --tlsv1.2 and increased timeout to be more robust
                    Arguments = $"--insecure --tlsv1.2 --max-time 10 --silent --show-error \"{url}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = new Process { StartInfo = psi })
                {
                    process.Start();
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await Task.Run(() => process.WaitForExit(10000));

                    if (process.ExitCode == 0)
                    {
                        _logger.LogInfo($"[Moonlight] ✓ curl.exe /cancel success: {output.Truncate(100)}");
                        return true;
                    }
                    _logger.LogWarning($"[Moonlight] curl.exe failed (code {process.ExitCode}): {error.Truncate(100)}");
                }
            }
            catch (Exception ex) { _logger.LogWarning($"[Moonlight] curl.exe error: {ex.Message}"); }

            // EN: Method 2b: PowerShell nuclear option (SkipCertificateCheck is very robust on Windows)
            // FR: Méthode 2b: Option nucléaire PowerShell (SkipCertificateCheck est très robuste sur Windows)
            _logger.LogInfo("[Moonlight] Method 2b: Trying PowerShell Invoke-WebRequest...");
            try
            {
                var psArgs = $"-NoProfile -ExecutionPolicy Bypass -Command \"[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri '{url}' -SkipCertificateCheck -TimeoutSec 10 -UseBasicParsing\"";
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = psArgs,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = new Process { StartInfo = psi })
                {
                    process.Start();
                    string output = await process.StandardOutput.ReadToEndAsync();
                    await Task.Run(() => process.WaitForExit(12000));
                    if (process.ExitCode == 0)
                    {
                        _logger.LogInfo("[Moonlight] ✓ PowerShell /cancel success");
                        return true;
                    }
                }
            }
            catch (Exception ex) { _logger.LogWarning($"[Moonlight] PowerShell fallback error: {ex.Message}"); }

            return false;
        }

        // ===== LEGACY CODE (P v3 and earlier) — DISABLED =====
        // The HTTPS approach using TcpClient+SslStream and SocketsHttpHandler is UNVIABLE
        // on .NET/Windows because Windows SChannel does not support DHE ephemeral keys,
        // which Sunshine's HTTPS server requires. This code is kept for reference only.
        //
        // FR: CODE HISTORIQUE (P v3 et antérieur) — DÉSACTIVÉ
        // L'approche HTTPS via TcpClient+SslStream et SocketsHttpHandler est INVIABLE
        // sur .NET/Windows car SChannel ne supporte pas les clés DHE éphémères,
        // que le serveur HTTPS de Sunshine exige. Ce code est gardé pour référence uniquement.

        /*
        private async Task CancelViaSslStreamAsync()
        {
            int sunshineHttpsPort = _config.ReadInt("Arcade", "SunshineHttpsPort", 47984);
            string host = "127.0.0.1";

            try
            {
                using (var tcp = new TcpClient())
                {
                    var connectTask = tcp.ConnectAsync(host, sunshineHttpsPort);
                    if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
                    {
                        _logger.LogWarning("[Moonlight] /cancel: TCP connection timed out (5s)");
                        return;
                    }
                    await connectTask;

                    using (var sslStream = new SslStream(tcp.GetStream(), false,
                        new RemoteCertificateValidationCallback((sender, cert, chain, errors) => true), null))
                    {
                        var clientCerts = LoadSunshineClientCertificate();
                        var authTask = sslStream.AuthenticateAsClientAsync(host, clientCerts,
                            SslProtocols.Tls12 | SslProtocols.Tls13, false);
                        if (await Task.WhenAny(authTask, Task.Delay(5000)) != authTask)
                        {
                            _logger.LogWarning("[Moonlight] /cancel: TLS handshake timed out (5s)");
                            return;
                        }
                        await authTask;

                        var request = $"GET /cancel HTTP/1.1\r\nHost: {host}\r\nConnection: close\r\n\r\n";
                        var requestBytes = Encoding.ASCII.GetBytes(request);
                        await sslStream.WriteAsync(requestBytes, 0, requestBytes.Length);
                        await sslStream.FlushAsync();

                        var buffer = new byte[4096];
                        var cts = new CancellationTokenSource(5000);
                        int bytesRead = await sslStream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                        if (bytesRead > 0)
                        {
                            var responseText = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                            var firstLine = responseText.Split(new[] { "\r\n" }, StringSplitOptions.None)[0];
                            _logger.LogInfo($"[Moonlight] Sunshine /cancel response: {firstLine}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[Moonlight] /cancel (TcpClient+SslStream) failed: {ex.Message}");

                // SocketsHttpHandler fallback
                try
                {
                    using (var handler = new SocketsHttpHandler
                    {
                        SslOptions = new SslClientAuthenticationOptions
                        {
                            RemoteCertificateValidationCallback = (_, _, _, _) => true,
                            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                            ClientCertificates = LoadSunshineClientCertificate()
                        }
                    })
                    using (var client = new HttpClient(handler))
                    {
                        client.Timeout = TimeSpan.FromSeconds(5);
                        var response = await client.GetAsync($"https://{host}:{sunshineHttpsPort}/cancel");
                        string body = await response.Content.ReadAsStringAsync();
                        _logger.LogInfo($"[Moonlight] Sunshine /cancel (fallback) response: {response.StatusCode} - {body}");
                    }
                }
                catch (Exception ex2)
                {
                    _logger.LogWarning($"[Moonlight] /cancel (SocketsHttpHandler fallback) also failed: {ex2.Message}");
                }
            }
        }

        private X509Certificate2Collection? LoadSunshineClientCertificate()
        {
            try
            {
                string configDir = _config.ReadValue("Arcade", "SunshineConfigDir", "");
                var searchPaths = new List<string>();

                if (!string.IsNullOrEmpty(configDir)) searchPaths.Add(configDir);

                searchPaths.Add(@"C:\Program Files\Sunshine\config");
                searchPaths.Add(@"C:\Program Files (x86)\Sunshine\config");
                searchPaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Sunshine", "config"));

                try
                {
                    foreach (var proc in Process.GetProcessesByName("sunshine"))
                    {
                        try
                        {
                            string? exePath = proc.MainModule?.FileName;
                            if (!string.IsNullOrEmpty(exePath))
                            {
                                string dir = Path.GetDirectoryName(exePath)!;
                                searchPaths.Add(Path.Combine(dir, "config"));
                            }
                        }
                        catch { }
                    }
                }
                catch { }

                foreach (string dir in searchPaths)
                {
                    string credDir = Path.Combine(dir, "credentials");
                    string certPath = Path.Combine(credDir, "cacert.pem");
                    string keyPath = Path.Combine(credDir, "cakey.pem");

                    if (File.Exists(certPath) && File.Exists(keyPath))
                    {
                        var cert = X509Certificate2.CreateFromPemFile(certPath, keyPath);
                        var certs = new X509Certificate2Collection(cert);
                        return certs;
                    }
                }
            }
            catch { }
            return null;
        }
        */

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// EN: String extension for truncating long log output.
    /// FR: Extension de chaîne pour tronquer les sorties de log longues.
    /// </summary>
    internal static class StringExtensions
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value[..maxLength] + "...";
        }
    }
}
