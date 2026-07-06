using System.Net.NetworkInformation;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.Core
{
    public class SystemInfo
    {
        public string? name { get; set; }
        public string? fullname { get; set; }
        public int totalGames { get; set; }
    }

    public class Game
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? System { get; set; }
        public string? PhysicalSystem { get; set; } // EN: For path resolution / FR: Pour la résolution des chemins
        public string? PlayUrl { get; set; }
        public string? Path { get; set; }
        public string? Description { get; set; }
        public string? Image { get; set; }
        public string? Marquee { get; set; }
        public string? Video { get; set; }
        public string? Thumbnail { get; set; }
        public string? Fanart { get; set; }
        public string? Manual { get; set; }
        public string? Genre { get; set; }
    }

    public class EmulationStationScraper
    {
        private readonly HttpClient httpClient;
        private string baseUrl;
        private string? _cachedRetrobatRoot;

        // EN: Endpoint resolution state. ES sometimes rejects 127.0.0.1 connections
        // with a 422 + {"error":"Unexpected endpoint or method. (GET /)"} body and only
        // accepts the LAN IP of the host. We probe 127.0.0.1 first (default), then fall
        // back to the first IPv4 non-loopback address found on this machine.
        // FR: État de résolution du endpoint. ES rejette parfois 127.0.0.1 avec un 422 +
        // body {"error":"Unexpected endpoint or method. (GET /)"} et n'accepte que l'IP
        // LAN de l'hôte. On sonde 127.0.0.1 d'abord (par défaut), puis on repli sur la
        // première IPv4 non-loopback trouvée sur cette machine.
        private readonly List<string> _localIpAddresses;
        private readonly SemaphoreSlim _resolutionLock = new SemaphoreSlim(1, 1);
        private bool _endpointProbed;
        private bool _isOnline;

        // EN: Optional logger. If null we fall back to Console.WriteLine (helps early
        // constructor paths when a logger is not yet available).
        // FR: Logger optionnel. S'il est null on retombe sur Console.WriteLine (utile sur
        // les chemins constructeurs quand aucun logger n'est encore disponible).
        private readonly Logger? _logger;

        // EN: Virtual systems and internal collections to exclude from search
        // FR: Systèmes virtuels et collections internes à exclure de la recherche
        private static readonly HashSet<string> ExcludedSystems = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "retrobat", "all", "recent", "favorites", "lastplayed", "multiplayer",
            "collection", "auto-lastplayed", "auto-favorites", "screenshots"
        };

        public EmulationStationScraper(string ipAddress = "127.0.0.1") : this(ipAddress, null) { }

        public EmulationStationScraper(Logger? logger) : this("127.0.0.1", logger) { }

        public EmulationStationScraper(string ipAddress, Logger? logger)
        {
            baseUrl = $"http://{ipAddress}:1234";
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            _logger = logger ?? new Logger("BatRun_metadata.log", appendToExisting: true);
            _localIpAddresses = GetLocalIpAddresses();
            Log($"[Scraper] Constructed. Initial baseUrl='{baseUrl}', candidates=[{string.Join(", ", _localIpAddresses)}]");
        }

        // EN: Convenience helpers that route to the injected Logger when available.
        // FR: Helpers qui routent vers le Logger injecté quand il est disponible.
        private void Log(string message)
        {
            if (_logger != null) _logger.LogDebug("[Scraper] " + message);
            else Console.WriteLine("[Scraper] " + message);
        }

        private void LogWarn(string message)
        {
            if (_logger != null) _logger.LogWarning("[Scraper] " + message);
            else Console.Error.WriteLine("[Scraper] " + message);
        }

        private void LogErr(string message, Exception? ex = null)
        {
            if (_logger != null) _logger.LogError("[Scraper] " + message, ex);
            else Console.Error.WriteLine("[Scraper] " + message + (ex != null ? ": " + ex.Message : ""));
        }

        // EN: Detect this machine's IPv4 address(es) that monitor emulators like EmulationStation
        // might trust. Excludes only loopback (127/8) and link-local (169.254/16) addresses are
        // INCLUDED because some users have ES bound to such an address and it works in the browser.
        // Priority: RFC1918 private addresses (10/8, 172.16/12, 192.168/16) first, then any IPv4.
        // FR: Détecte l(es) adresse(s) IPv4 de la machine potentiellement approuvée(s) par ES.
        // Exclut uniquement loopback (127/8) ; les link-local (169.254/16) sont INCLUSES parce que
        // certains utilisateurs ont ES liées à cette adresse et ça marche dans le navigateur.
        // Priorité : adresses privées RFC1918 (10/8, 172.16/12, 192.168/16) d'abord, puis n'importe
        // quelle IPv4.
        private static List<string> GetLocalIpAddresses()
        {
            var candidates = new List<string>();

            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork
                        && !IPAddress.IsLoopback(ip))
                    {
                        candidates.Add(ip.ToString());
                    }
                }
            }
            catch { }

            if (candidates.Count == 0)
            {
                try
                {
                    foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        // Skip non-up adapters
                        if (ni.OperationalStatus != OperationalStatus.Up) continue;
                        var ipProps = ni.GetIPProperties();
                        foreach (var ip in ipProps.UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork
                                && !IPAddress.IsLoopback(ip.Address))
                            {
                                candidates.Add(ip.Address.ToString());
                            }
                        }
                    }
                }
                catch { }
            }

            // Sort: RFC1918 (private) addresses first, then APIPA (169.254/16), then everything else.
            candidates.Sort((a, b) =>
            {
                int prioA = AddressPriority(a);
                int prioB = AddressPriority(b);
                return prioA.CompareTo(prioB);
            });

            return candidates;
        }

        // EN: Lower is better. RFC1918 = 0, APIPA = 1, other = 2.
        // FR: Plus bas est meilleur. RFC1918 = 0, APIPA = 1, autre = 2.
        private static int AddressPriority(string ip)
        {
            string firstOctet = ip.Split('.')[0];
            int n;
            if (!int.TryParse(firstOctet, out n)) return 2;

            if (n == 10) return 0;                       // 10.0.0.0/8
            if (n == 192 && ip.StartsWith("192.168")) return 0; // 192.168.0.0/16
            if (n == 172) return 0;                       // 172.16.0.0/12
            if (n == 169 && ip.StartsWith("169.254")) return 1; // APIPA
            return 2;
        }

        // EN: Pull the IP portion out of "http://1.2.3.4:1234".
        // FR: Extrait l'IP de "http://1.2.3.4:1234".
        private static string ExtractIp(string url)
        {
            try
            {
                // Strip scheme, then take host before ':' (port) or '/' (path).
                string s = url;
                int scheme = s.IndexOf("://");
                if (scheme >= 0) s = s.Substring(scheme + 3);
                int colon = s.IndexOf(':');
                int slash = s.IndexOf('/');
                int cut = colon;
                if (slash >= 0 && (colon < 0 || slash < colon)) cut = slash;
                if (cut < 0) return s;
                return s.Substring(0, cut);
            }
            catch { return ""; }
        }

        // EN: Probe a single IP for the /systems endpoint. Returns true ONLY if the response is
        // a valid 200 OK carrying the expected systems shape (JSON array OR {"systems":[...]}
        // with at least 1 entry). Plain 200 OK with empty body / HTML / single-object response
        // (which is what 127.0.0.1 can serve when ES only trusts the LAN IP) is rejected.
        // FR: Sonde une IP pour /systems. Renvoie true UNIQUEMENT si la réponse est un vrai 200
        // OK portant la forme systems attendue (JSON array OU {"systems":[...]} avec au moins 1
        // entrée). Un 200 OK brut avec body vide / HTML / objet isolé (ce que 127.0.0.1 peut servir
        // quand ES n'approuve que l'IP LAN) est rejeté.
        private async Task<bool> IsEndpointAcceptedAsync(string ip)
        {
            string url = $"http://{ip}:1234/systems";
            HttpResponseMessage? response = null;
            try
            {
                response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    try
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        if (content != null && content.Contains("Unexpected endpoint"))
                        {
                            Log($"[Probe] {ip} rejected by ES (status {(int)response.StatusCode}). Body={content.Trim()}");
                            return false;
                        }
                    }
                    catch { }
                    LogWarn($"[Probe] {ip} non-200 status: {(int)response.StatusCode}");
                    return false;
                }

                // 200 OK: read the body and require a valid systems list shape.
                var body = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(body))
                {
                    LogWarn($"[Probe] {ip} returned 200 OK but empty body");
                    return false;
                }

                try
                {
                    var token = JToken.Parse(body);
                    if (token is JArray arr)
                    {
                        if (arr.Count == 0)
                        {
                            LogWarn($"[Probe] {ip} returned 200 OK but empty JSON array");
                            return false;
                        }
                        return true;
                    }
                    if (token is JObject obj && obj["systems"] is JArray sysArr && sysArr.Count > 0)
                    {
                        return true;
                    }
                    // 200 OK but body does not look like a systems array — 127.0.0.1 is most
                    // likely returning an HTML boilerplate / plain JSON.
                    LogWarn($"[Probe] {ip} returned 200 OK but body doesn't look like a systems list.");
                    LogWarn($"[Probe]   body preview: {body.Substring(0, Math.Min(160, body.Length))}");
                    return false;
                }
                catch (Exception ex)
                {
                    LogWarn($"[Probe] {ip} 200 OK but body not parseable as JSON: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogWarn($"[Probe] {ip} unreachable: {ex.Message}");
                return false;
            }
            finally
            {
                response?.Dispose();
            }
        }

        // EN: Probe the configured IP first; if it is rejected (or unreachable) and the
        // configured IP was the loopback 127.0.0.1, fall back to the detected LAN IP.
        // Caches the working baseUrl so subsequent method calls use it directly.
        // FR: Sonne d'abord l'IP configurée ; si elle est rejetée (ou inaccessible) et que
        // l'IP configurée était le loopback 127.0.0.1, repli sur l'IP LAN détectée. Cache la
        // baseUrl qui fonctionne afin que les appels suivants l'utilisent directement.
        private async Task<bool> EnsureResolvedAsync()
        {
            if (_endpointProbed) return _isOnline;

            await _resolutionLock.WaitAsync();
            try
            {
                if (_endpointProbed) return _isOnline;

                string primaryIp = ExtractIp(baseUrl);
                Log($"[Resolver] Probing primary IP={primaryIp} (from baseUrl='{baseUrl}')...");
                if (await IsEndpointAcceptedAsync(primaryIp))
                {
                    Log($"[Resolver] Primary IP {primaryIp} accepted by ES.");
                    _isOnline = true;
                }
                else if (primaryIp == "127.0.0.1" && _localIpAddresses.Count > 0)
                {
                    LogWarn($"[Resolver] 127.0.0.1 rejected by ES. Will try {_localIpAddresses.Count} candidate local IPs...");
                    bool found = false;
                    foreach (var cand in _localIpAddresses)
                    {
                        if (cand == primaryIp) continue;
                        Log($"[Resolver] Trying candidate IP={cand}...");
                        if (await IsEndpointAcceptedAsync(cand))
                        {
                            baseUrl = $"http://{cand}:1234";
                            Log($"[Resolver] Switched baseUrl to {baseUrl}");
                            _isOnline = true;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        LogErr("[Resolver] All candidate local IPs rejected. No usable endpoint found.");
                    }
                }
                else
                {
                    LogWarn($"[Resolver] Primary IP {primaryIp} not accepted and no fallback available.");
                }

                _endpointProbed = true;
                return _isOnline;
            }
            finally
            {
                _resolutionLock.Release();
            }
        }

        private string GetRetrobatRootPath()
        {
            if (_cachedRetrobatRoot != null) return _cachedRetrobatRoot;

            try
            {
                // EN: Direct registry check to avoid log spam from RetroBatService/Logger
                // FR: Vérification directe du registre pour éviter le spam de log de RetroBatService
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\RetroBat"))
                {
                    string? path = key?.GetValue("LatestKnownInstallPath") as string;
                    if (!string.IsNullOrEmpty(path))
                    {
                        _cachedRetrobatRoot = Path.GetDirectoryName(path) ?? "";
                        return _cachedRetrobatRoot;
                    }
                }
            }
            catch { }
            return "";
        }

        public async Task<List<SystemInfo>> GetSystemsAsync()
        {
            // EN: Make sure we are using a baseUrl that ES actually accepts before parsing
            // the /systems response. If 127.0.0.1 was rejected, EnsureResolvedAsync() will
            // have already swapped to the LAN IP on the first call.
            // FR: S'assurer qu'on utilise une baseUrl qu'ES accepte vraiment avant de parser
            // la réponse /systems. Si 127.0.0.1 a été rejetée, EnsureResolvedAsync() aura
            // déjà basculé vers l'IP LAN au premier appel.
            if (!await EnsureResolvedAsync()) return new List<SystemInfo>();

            try
            {
                var response = await httpClient.GetAsync($"{baseUrl}/systems");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                
                JToken token = JToken.Parse(content);
                List<SystemInfo>? systems = null;

                if (token is JArray array)
                    systems = array.ToObject<List<SystemInfo>>();
                else if (token is JObject obj && obj["systems"] is JArray sysArray)
                    systems = sysArray.ToObject<List<SystemInfo>>();

                if (systems == null || !systems.Any())
                {
                    LogWarn($"[Systems] No systems found in response from {baseUrl}/systems");
                    return new List<SystemInfo>();
                }

                return systems.Where(s => s.name != null && !ExcludedSystems.Contains(s.name!)).ToList();
            }
            catch (Exception ex)
            {
                LogErr($"[Systems] Error getting systems from {baseUrl}/systems", ex);
                return new List<SystemInfo>();
            }
        }

        public async Task<List<Game>> GetGamesForSystemAsync(string systemName)
        {
            if (!await EnsureResolvedAsync()) return new List<Game>();

            var gamesList = new List<Game>();
            try
            {
                var response = await httpClient.GetAsync($"{baseUrl}/systems/{systemName}/games");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    JToken token = JToken.Parse(content);
                    List<Game>? systemGames = null;

                    if (token is JArray array)
                        systemGames = array.ToObject<List<Game>>();
                    else if (token is JObject obj && obj["games"] is JArray gamesArray)
                        systemGames = gamesArray.ToObject<List<Game>>();

                    if (systemGames != null && systemGames.Any())
                    {
                        // EN: Deduce real system for each game if it comes from a virtual collection
                        // FR: Déduire le système réel pour chaque jeu s'il provient d'une collection virtuelle
                        foreach (var game in systemGames)
                        {
                            string candidate = !string.IsNullOrEmpty(game.System) ? game.System : systemName;
                            game.PhysicalSystem = DeduceRealSystem(candidate, game);
                            
                            // EN: Keep original system name for UI display if it's a collection, or ensure it's set
                            // FR: Garder le nom système d'origine pour l'affichage (ex: .Lya Library)
                            if (string.IsNullOrEmpty(game.System)) game.System = systemName;
                        }

                        // EN: Group games by their PHYSICAL system for correct metadata enrichment (e.g. megadrive instead of .Lya Library)
                        // FR: Grouper les jeux par leur système PHYSIQUE pour le gamelist.xml
                        var groups = systemGames.GroupBy(g => g.PhysicalSystem!);

                        foreach (var group in groups)
                        {
                            string realSystem = group.Key;
                            string realFolder = GetRomFolderName(realSystem);
                            var subList = group.ToList();

                            // EN: Enrich with gamelist.xml metadata for the real system to get accurate physical media paths
                            // FR: Enrichir avec les métadonnées de gamelist.xml pour le système réel
                            EnrichWithGamelist(realSystem, subList);

                            foreach (var game in subList)
                            {
                                // Fix media paths: prepend the correct folder name (mapped if necessary)
                                game.Image = FixMediaPath(realFolder, game.Image);
                                game.Marquee = FixMediaPath(realFolder, game.Marquee);
                                game.Video = FixMediaPath(realFolder, game.Video);
                                game.Thumbnail = FixMediaPath(realFolder, game.Thumbnail);
                                game.Fanart = FixMediaPath(realFolder, game.Fanart);
                                game.Manual = FixMediaPath(realFolder, game.Manual);

                                if (!string.IsNullOrEmpty(game.Id))
                                    game.PlayUrl = $"{baseUrl}/systems/{game.System}/games/{game.Id}/play";
                            }
                            gamesList.AddRange(subList);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogErr($"[Games] Error for system {systemName} at {baseUrl}/systems/{systemName}/games", ex);
            }

            return gamesList;
        }

        public static string GetRomFolderName(string system)
        {
            if (string.IsNullOrEmpty(system)) return system;
            
            // EN: Handle specific RetroBat/ES system name to folder mapping discrepancies
            // FR: Gérer les discordances de noms entre le système ES et le dossier de ROMS
            switch (system.ToLower())
            {
                case "gw": return "gameandwatch";
                // Add more mappings here if needed
                default: return system;
            }
        }

        private string DeduceRealSystem(string candidate, Game game)
        {
            // EN: If the candidate system doesn't have a physical folder, it's likely a virtual collection
            // FR: Si le système candidat n'a pas de dossier physique, c'est probablement une collection virtuelle
            string retrobatRoot = GetRetrobatRootPath();
            if (!string.IsNullOrEmpty(retrobatRoot))
            {
                string romsRoot = Path.Combine(retrobatRoot, "roms");
                if (Directory.Exists(Path.Combine(romsRoot, GetRomFolderName(candidate))))
                    return candidate;

                // EN: Try to extract real system from path (find folder immediately after /roms/)
                // FR: Tenter d'extraire le système réel du chemin (dossier juste après /roms/)
                string[] pathsToCheck = { game.Path ?? "", game.Image ?? "", game.Thumbnail ?? "" };
                foreach (var p in pathsToCheck)
                {
                    if (string.IsNullOrEmpty(p)) continue;
                    string normalized = p.Replace("\\", "/");
                    int romsIdx = normalized.LastIndexOf("/roms/", StringComparison.OrdinalIgnoreCase);
                    if (romsIdx != -1)
                    {
                        string afterRoms = normalized.Substring(romsIdx + 6).TrimStart('/');
                        int folderSlashIdx = afterRoms.IndexOf('/');
                        if (folderSlashIdx != -1) return afterRoms.Substring(0, folderSlashIdx);
                    }
                    
                    // EN: Fallback to systems/real_system/games/ pattern if /roms/ was not found
                    // FR: Repli sur le motif /systems/ si /roms/ n'a pas été trouvé
                    var match = Regex.Match(normalized, @"systems/([^/]+)/games/", RegexOptions.IgnoreCase);
                    if (match.Success) return match.Groups[1].Value;
                }
            }
            return candidate;
        }

        private void EnrichWithGamelist(string systemName, List<Game> games)
        {
            try
            {
                // EN: Get root path from the caller or once (avoiding spamming logs/registry)
                // FR: Récupérer le chemin racine une seule fois pour éviter de spammer les logs/registre
                string retrobatRoot = GetRetrobatRootPath();
                if (string.IsNullOrEmpty(retrobatRoot)) return;

                string folderName = GetRomFolderName(systemName);
                string gamelistPath = Path.Combine(retrobatRoot, "roms", folderName, "gamelist.xml");
                if (!File.Exists(gamelistPath))
                {
                    // EN: This is expected for virtual collections themselves, but here we are called for the PHYSICAL system
                    // FR: C'est attendu pour les collections virtuelles, mais ici on est appelé pour le système PHYSIQUE
                    return;
                }

                // [BATRUN-FIX]: Force UTF-8 encoding when reading gamelist.xml to avoid double-encoding of accented characters
                // EN: Some gamelist.xml files are UTF-8 without BOM/declaration; XDocument.Load(path) can misinterpret them.
                // FR: Certains gamelist.xml sont en UTF-8 sans BOM/déclaration; XDocument.Load(path) peut mal les interpréter.
                XDocument doc;
                using (var sr = new StreamReader(gamelistPath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    doc = XDocument.Load(sr);
                }
                var xmlGames = doc.Descendants("game").ToList();
                int enrichedCount = 0;

                foreach (var game in games)
                {
                    if (string.IsNullOrEmpty(game.Path)) continue;

                    string gameFileName = Path.GetFileName(game.Path);
                    var meta = xmlGames.FirstOrDefault(x => Path.GetFileName(x.Element("path")?.Value) == gameFileName);

                    if (meta != null)
                    {
                        // EN: Found metadata! Always overwrite with XML values for physical paths
                        // FR: Métadonnées trouvées ! Toujours écraser avec les valeurs XML
                        game.Image = FixMediaPath(folderName, meta.Element("image")?.Value);
                        game.Thumbnail = FixMediaPath(folderName, meta.Element("thumbnail")?.Value);
                        game.Video = FixMediaPath(folderName, meta.Element("video")?.Value);
                        game.Marquee = FixMediaPath(folderName, meta.Element("marquee")?.Value);
                        game.Fanart = FixMediaPath(folderName, meta.Element("fanart")?.Value);
                        game.Manual = FixMediaPath(folderName, meta.Element("manual")?.Value);
                        
                        string? desc = meta.Element("desc")?.Value;
                        if (!string.IsNullOrEmpty(desc)) game.Description = desc;
                        
                        string? genre = meta.Element("genre")?.Value;
                        if (!string.IsNullOrEmpty(genre)) game.Genre = genre;
                        
                        enrichedCount++;
                    }
                }
                
                // EN: Log success for debugging collection enrichment
                // FR: Logger la réussite pour le débogage de l'enrichissement des collections
                if (enrichedCount > 0)
                {
                    Log($"[Enrich] Enriched {enrichedCount} games for {systemName} from {gamelistPath}");
                }
            }
            catch (Exception ex)
            {
                LogErr($"[Enrich] Enrichment error for system {systemName}", ex);
            }
        }

        private string? FixMediaPath(string folderName, string? rawPath)
        {
            if (string.IsNullOrEmpty(rawPath)) return rawPath;
            
            // EN: Robust cleanup: normalize slashes, remove leading slashes and redundant ./ segments
            // FR: Nettoyage robuste : normaliser les slashes, supprimer les slashes initiaux et les ./ redondants
            string p = rawPath.Replace("\\", "/");
            while (p.StartsWith("./")) p = p.Substring(2);
            p = p.Replace("/./", "/").TrimStart('/');
            
            if (p.StartsWith(folderName + "/", StringComparison.OrdinalIgnoreCase)) return p;
            return $"{folderName}/{p}";
        }


        public async Task<List<Game>> GetAllGamesAsync(string? systemName = null)

        {
            var allGames = new List<Game>();
            List<SystemInfo> systemsToScan;

            if (!string.IsNullOrEmpty(systemName))
            {
                // If a specific system is requested, just create a list with that one
                systemsToScan = new List<SystemInfo> { new SystemInfo { name = systemName } };
            }
            else
            {
                // Otherwise, get all systems (already filtered)
                systemsToScan = await GetSystemsAsync();
            }

            foreach (var system in systemsToScan.Where(s => s.name != null))
            {
                var games = await GetGamesForSystemAsync(system.name!);
                allGames.AddRange(games);
            }

            return allGames;
        }

        public async Task<bool> ReloadGamesAsync()
        {
            if (!await EnsureResolvedAsync()) return false;
            try
            {
                var response = await httpClient.GetAsync($"{baseUrl}/reloadgames");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                LogErr($"[Reload] Error reloading games at {baseUrl}/reloadgames", ex);
                return false;
            }
        }
        
        public async Task<bool> LaunchGameAsync(string gamePath)
        {
            // EN: Make sure baseUrl points to an ES that accepts us before attempting /launch.
            // FR: S'assurer que baseUrl pointe vers un ES qui nous accepte avant de tenter /launch.
            if (!await EnsureResolvedAsync()) return false;

            try
            {
                // EN: Rebase path if the game is launched cross-network and drive letter doesn't match
                // FR: Rebaser le chemin si le jeu est lancé en réseau croisé et que la lettre de lecteur diffère
                if (!File.Exists(gamePath))
                {
                    string rootPath = GetRetrobatRootPath();
                    int romsIdx = gamePath.IndexOf(@"\roms\", StringComparison.OrdinalIgnoreCase);
                    if (romsIdx == -1) romsIdx = gamePath.IndexOf("/roms/", StringComparison.OrdinalIgnoreCase);
                    
                    if (romsIdx != -1 && !string.IsNullOrEmpty(rootPath))
                    {
                        string subPath = gamePath.Substring(romsIdx).TrimStart('\\', '/');
                        string newPath = Path.Combine(rootPath, subPath);
                        if (File.Exists(newPath))
                        {
                            Log($"[Launch] Rebased remote path for launch: {gamePath} -> {newPath}");
                            gamePath = newPath;
                        }
                    }
                }

                var launchResponse = await httpClient.PostAsync($"{baseUrl}/launch",
                    new StringContent(gamePath, Encoding.UTF8, "text/plain"));

                if (!launchResponse.IsSuccessStatusCode)
                {
                    LogWarn($"[Launch] API Launch failed with status code: {launchResponse.StatusCode}");
                }

                return launchResponse.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                LogErr("Erreur lors du lancement du jeu", ex);
                return false;
            }
        }

        public async Task<bool> PingServerAsync()
        {
            // EN: Probe 127.0.0.1 first; if ES answers 422 + "Unexpected endpoint" (or
            // otherwise fails to respond with systems), fall back to the detected LAN IP.
            // FR: Sonde 127.0.0.1 en premier ; si ES répond 422 + "Unexpected endpoint" (ou
            // ne répond pas du tout avec des systèmes), repli sur l'IP LAN détectée.
            return await EnsureResolvedAsync();
        }
    }
}
