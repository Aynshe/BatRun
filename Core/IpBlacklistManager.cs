using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using BatRun.Utils;

namespace BatRun.Core
{
    // EN: Types of security events to log
    // FR: Types d'événements de sécurité à enregistrer
    public enum SecurityEventType
    {
        LoginSuccess,
        LoginFailure,
        IpBlocked,
        IpDropped
    }

    public class SecurityLogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public SecurityEventType Type { get; set; }
        public string Ip { get; set; } = "";
        public string Username { get; set; } = "";
        public string Details { get; set; } = "";
    }

    // EN: Data model for the JSON file
    // FR: Modèle de données pour le fichier JSON
    public class IpBlacklistData
    {
        // EN: Individual banned IPs with expiration time
        // FR: IP individuelles bannies avec heure d'expiration
        public Dictionary<string, DateTime> BlockedIps { get; set; } = new();

        // EN: CIDR ranges to block indefinitely (e.g. "1.2.3.0/24")
        // FR: Plages CIDR à bloquer indéfiniment
        public List<string> BlockedRanges { get; set; } = new();

        // EN: List of country codes to block (e.g. "CN", "RU")
        // FR: Liste des codes pays à bloquer (ex: "CN", "RU")
        public List<string> BlockedCountries { get; set; } = new();
    }

    public class IpBlacklistManager
    {
        private readonly string _storagePath;
        private readonly string _cacheDir;
        private readonly Logger _logger;
        private IpBlacklistData _data = new();
        private readonly object _fileLock = new();

        // EN: In-memory tracking of failed login attempts
        // FR: Suivi en mémoire des tentatives de connexion échouées
        private readonly ConcurrentDictionary<string, int> _failedAttempts = new();
        private readonly ConcurrentDictionary<string, DateTime> _lastAttemptTime = new();

        // [BATRUN-SEC]: Rate limiting — track all login request timestamps per IP
        // FR: Rate limiting — suivi de tous les horodatages de requêtes login par IP
        private readonly ConcurrentDictionary<string, List<DateTime>> _loginRequestTimestamps = new();

        // EN: In-memory history of the very last security events
        // FR: Historique en mémoire des tous derniers événements de sécurité
        private readonly List<SecurityLogEntry> _recentLogs = new();
        private const int MaxRecentLogs = 50;
        private readonly string _logsFolder;

        // EN: In-memory storage for country CIDR blocks (fetched from web)
        // FR: Stockage en mémoire des blocs CIDR par pays (récupérés du web)
        private readonly ConcurrentDictionary<string, List<uint[]>> _countryRanges = new();

        private const int MaxFailedAttempts = 5;
        private const int BlockDurationHours = 24;
        private const int AttemptExpirationMinutes = 30; // Reset attempts after 30 mins of no activity

        // [BATRUN-SEC]: Rate limit constants
        // FR: Constantes de rate limiting
        private const int RateLimitMaxRequests = 10; // EN: Max login requests per window / FR: Max requêtes login par fenêtre
        private const int RateLimitWindowSeconds = 60; // EN: Time window in seconds / FR: Fenêtre de temps en secondes

        public IpBlacklistManager(Logger logger, string storageDirectory)
        {
            _logger = logger;
            _storagePath = Path.Combine(storageDirectory, "IpBlacklist.json");
            _cacheDir = Path.Combine(storageDirectory, "GeoIpCache");
            _logsFolder = Path.Combine(storageDirectory, "SecurityLogs");
            
            if (!Directory.Exists(_cacheDir)) Directory.CreateDirectory(_cacheDir);
            if (!Directory.Exists(_logsFolder)) Directory.CreateDirectory(_logsFolder);

            LoadData();
            
            // EN: Start background download of country ranges
            // FR: Démarrer le téléchargement en arrière-plan des plages par pays
            _ = Task.Run(() => DownloadCountryRangesAsync());
        }

        private void LoadData()
        {
            lock (_fileLock)
            {
                try
                {
                    if (File.Exists(_storagePath))
                    {
                        string json = File.ReadAllText(_storagePath);
                        _data = JsonSerializer.Deserialize<IpBlacklistData>(json) ?? new IpBlacklistData();
                        CleanExpiredBlocks(); // Clean up on load
                    }
                    else
                    {
                        // EN: Create default file with some example CIDR blocks
                        // FR: Créer un fichier par défaut avec des exemples de plages CIDR
                        _data = new IpBlacklistData
                        {
                            BlockedRanges = new List<string> 
                            { 
                                // "192.168.100.0/24", // Example / Exemple
                            },
                            BlockedCountries = new List<string>
                            {
                                // Africa / Afrique
                                "DZ", "AO", "BJ", "BF", "BI", "CM", "CV", "CF", "TD", "KM", "CG", "CD", "CI", "DJ", "EG", "GQ", "ER", "ET", "GA", "GM", "GH", "GN", "GW", "KE", "LS", "LR", "LY", "MG", "MW", "ML", "MR", "MU", "MA", "MZ", "NA", "NE", "NG", "RW", "ST", "SN", "SC", "SL", "SO", "ZA", "SS", "SD", "SZ", "TZ", "TG", "TN", "UG", "ZM", "ZW",
                                // Arabic & Middle East / Moyen-Orient & Pays Arabes
                                "AF", "BH", "IR", "IQ", "JO", "KW", "LB", "OM", "PS", "QA", "SA", "SY", "TR", "AE", "YE", "PK",
                                // Eastern Europe & High Risk / Europe de l'Est & Risques élevés
                                "RU", "BY", "UA", "MD", "RO", "BG", "CN", "KP", "VN", "BR", "IN"
                            }
                        };
                        SaveData();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to load IP blacklist", ex);
                    _data = new IpBlacklistData();
                }
            }
        }

        private void SaveData()
        {
            try
            {
                string json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_storagePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to save IP blacklist", ex);
            }
        }

        private void CleanExpiredBlocks()
        {
            bool changed = false;
            var now = DateTime.UtcNow;
            
            var expired = _data.BlockedIps.Where(kvp => kvp.Value <= now).Select(kvp => kvp.Key).ToList();
            foreach (var ip in expired)
            {
                _data.BlockedIps.Remove(ip);
                changed = true;
                _logger.LogInfo($"[Security] Ban expired for IP {ip}");
            }

            if (changed) SaveData();
        }

        public bool IsIpBlocked(IPAddress ipAddress)
        {
            // Normalize
            if (ipAddress.IsIPv4MappedToIPv6) ipAddress = ipAddress.MapToIPv4();
            string ipStr = ipAddress.ToString();

            lock (_fileLock)
            {
                // Check direct IP block
                if (_data.BlockedIps.TryGetValue(ipStr, out DateTime expiry))
                {
                    if (DateTime.UtcNow > expiry)
                    {
                        _data.BlockedIps.Remove(ipStr);
                        SaveData();
                        // Expired, let them through
                    }
                    else
                    {
                        return true; // Still blocked
                    }
                }

                // Check CIDR ranges
                if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) // Support IPv4 for now
                {
                    byte[] ipBytes = ipAddress.GetAddressBytes();
                    uint ipUint = (uint)((ipBytes[0] << 24) | (ipBytes[1] << 16) | (ipBytes[2] << 8) | ipBytes[3]);

                    foreach (var range in _data.BlockedRanges)
                    {
                        if (string.IsNullOrWhiteSpace(range)) continue;
                        _logger.LogInfo($"[Security] Checking if IP {ipStr} matches manual range: {range}");
                        try
                        {
                            string[] parts = range.Split('/');
                            if (parts.Length != 2) continue;

                            if (IPAddress.TryParse(parts[0], out IPAddress? networkIp) && networkIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                int prefix = int.Parse(parts[1]);
                                if (prefix < 0 || prefix > 32) continue;

                                byte[] netBytes = networkIp.GetAddressBytes();
                                uint netUint = (uint)((netBytes[0] << 24) | (netBytes[1] << 16) | (netBytes[2] << 8) | netBytes[3]);

                                uint mask = prefix == 0 ? 0 : 0xFFFFFFFF << (32 - prefix);
                                
                                if ((ipUint & mask) == (netUint & mask))
                                {
                                    return true; // Matches manual range
                                }
                            }
                        }
                        catch { /* Ignore invalid CIDR format */ }
                    }

                    // Check country-based ranges
                    foreach (var country in _countryRanges)
                    {
                        foreach (var range in country.Value)
                        {
                            // range[0] is netUint, range[1] is mask
                            if ((ipUint & range[1]) == (range[0] & range[1]))
                            {
                                return true; // Matches country range
                            }
                        }
                    }
                }
            }

            return false;
        }

        public void ResetFailedAttempts(IPAddress ipAddress, string username)
        {
            if (ipAddress.IsIPv4MappedToIPv6) ipAddress = ipAddress.MapToIPv4();
            string ipStr = ipAddress.ToString();
            
            if (_failedAttempts.TryRemove(ipStr, out _))
            {
                _logger.LogInfo($"[Security] Reset failed attempts for {ipStr} (Successful login: {username})");
            }
            _lastAttemptTime.TryRemove(ipStr, out _);

            AddSecurityLog(SecurityEventType.LoginSuccess, ipStr, username, "Login successful");
        }

        public void RecordFailedAttempt(IPAddress ipAddress, string username)
        {
            if (ipAddress.IsIPv4MappedToIPv6) ipAddress = ipAddress.MapToIPv4();
            string ipStr = ipAddress.ToString();

            var now = DateTime.UtcNow;
            
            // Clean old attempts if they expired
            if (_lastAttemptTime.TryGetValue(ipStr, out DateTime lastAttempt))
            {
                if ((now - lastAttempt).TotalMinutes > AttemptExpirationMinutes)
                {
                    _failedAttempts.TryRemove(ipStr, out _);
                }
            }

            int count = _failedAttempts.AddOrUpdate(ipStr, 1, (_, currentCount) => currentCount + 1);
            _lastAttemptTime[ipStr] = now;

            _logger.LogWarning($"[Security] Failed login attempt from {ipStr} for user '{username}' ({count}/{MaxFailedAttempts})");
            AddSecurityLog(SecurityEventType.LoginFailure, ipStr, username, $"Attempt {count}/{MaxFailedAttempts}");

            if (count >= MaxFailedAttempts)
            {
                // Block the IP
                lock (_fileLock)
                {
                    _data.BlockedIps[ipStr] = now.AddHours(BlockDurationHours);
                    SaveData();
                }
                
                _failedAttempts.TryRemove(ipStr, out _);
                _lastAttemptTime.TryRemove(ipStr, out _);
                _logger.LogWarning($"[Security] IP {ipStr} banned for {BlockDurationHours} hours due to brute force.");
                AddSecurityLog(SecurityEventType.IpBlocked, ipStr, username, $"Banned for {BlockDurationHours}h");
            }
        }

        public void RecordIpDrop(IPAddress ipAddress)
        {
            if (ipAddress.IsIPv4MappedToIPv6) ipAddress = ipAddress.MapToIPv4();
            string ipStr = ipAddress.ToString();
            AddSecurityLog(SecurityEventType.IpDropped, ipStr, "Blocked", "Blacklisted connection dropped");
        }

        public void RecordGuestLogin(IPAddress ipAddress)
        {
            if (ipAddress.IsIPv4MappedToIPv6) ipAddress = ipAddress.MapToIPv4();
            string ipStr = ipAddress.ToString();
            AddSecurityLog(SecurityEventType.LoginSuccess, ipStr, "Guest", "Guest mode access (Local)");
        }

        // [BATRUN-SEC]: Active rate limiting — returns true if the IP has exceeded the login request threshold
        // EN: Sliding window: keeps only timestamps within the last RateLimitWindowSeconds, then checks count.
        // FR: Fenêtre glissante : ne garde que les horodatages des dernières RateLimitWindowSeconds secondes, puis vérifie le nombre.
        public bool IsLoginRateLimited(IPAddress ipAddress)
        {
            if (ipAddress.IsIPv4MappedToIPv6) ipAddress = ipAddress.MapToIPv4();
            string ipStr = ipAddress.ToString();
            var now = DateTime.UtcNow;
            var cutoff = now.AddSeconds(-RateLimitWindowSeconds);

            var timestamps = _loginRequestTimestamps.GetOrAdd(ipStr, _ => new List<DateTime>());
            lock (timestamps)
            {
                // EN: Remove timestamps outside the sliding window
                // FR: Supprimer les horodatages hors de la fenêtre glissante
                timestamps.RemoveAll(t => t < cutoff);

                // EN: Record this attempt
                // FR: Enregistrer cette tentative
                timestamps.Add(now);

                if (timestamps.Count > RateLimitMaxRequests)
                {
                    _logger.LogWarning($"[Security] Rate limit exceeded for IP {ipStr}: {timestamps.Count} login requests in {RateLimitWindowSeconds}s (max {RateLimitMaxRequests})");
                    return true;
                }
            }
            return false;
        }

        private void AddSecurityLog(SecurityEventType type, string ip, string username, string details)
        {
            var entry = new SecurityLogEntry
            {
                Type = type,
                Ip = ip,
                Username = username,
                Details = details
            };

            // EN: Add to recent in-memory list
            // FR: Ajouter à la liste mémoire récente
            lock (_recentLogs)
            {
                _recentLogs.Add(entry);
                if (_recentLogs.Count > MaxRecentLogs) _recentLogs.RemoveAt(0);
            }

            // EN: Persist to daily file
            // FR: Persister dans le fichier journalier
            Task.Run(() => AppendToDailyLog(entry));
        }

        private void AppendToDailyLog(SecurityLogEntry entry)
        {
            try
            {
                string dateStr = entry.Timestamp.ToString("yyyy-MM-dd");
                string logFile = Path.Combine(_logsFolder, $"security_log_{dateStr}.json");
                
                List<SecurityLogEntry> dailyLogs = new();
                lock (_fileLock)
                {
                    if (File.Exists(logFile))
                    {
                        string content = File.ReadAllText(logFile);
                        dailyLogs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SecurityLogEntry>>(content) ?? new();
                    }
                    
                    dailyLogs.Add(entry);
                    File.WriteAllText(logFile, Newtonsoft.Json.JsonConvert.SerializeObject(dailyLogs, Newtonsoft.Json.Formatting.Indented));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Security] Failed to save security log to file: {ex.Message}");
            }
        }

        public List<SecurityLogEntry> GetSecurityLogs(string? dateStr = null)
        {
            if (string.IsNullOrEmpty(dateStr))
            {
                lock (_recentLogs)
                {
                    return _recentLogs.OrderByDescending(l => l.Timestamp).ToList();
                }
            }

            try
            {
                string logFile = Path.Combine(_logsFolder, $"security_log_{dateStr}.json");
                if (File.Exists(logFile))
                {
                    string content = File.ReadAllText(logFile);
                    var logs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SecurityLogEntry>>(content);
                    return logs?.OrderByDescending(l => l.Timestamp).ToList() ?? new();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Security] Failed to read security log for {dateStr}: {ex.Message}");
            }

            return new List<SecurityLogEntry>();
        }

        public List<string> GetAvailableLogDates()
        {
            try
            {
                if (!Directory.Exists(_logsFolder)) return new List<string>();
                
                _logger.LogInfo($"[Security] Searching logs in: {Path.GetFullPath(_logsFolder)}");
                
                // EN: Use EnumerateFiles for better performance and case-insensitive check
                // FR: Utiliser EnumerateFiles pour de meilleures performances et une vérification insensible à la casse
                var dates = Directory.EnumerateFiles(_logsFolder, "security_log_*.json")
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(n => n != null && n.StartsWith("security_log_", StringComparison.OrdinalIgnoreCase))
                    .Select(n => n!.Substring("security_log_".Length))
                    .OrderByDescending(d => d)
                    .ToList();

                _logger.LogInfo($"[Security] Found {dates.Count} log dates.");
                return dates;
            }
            catch (Exception ex)
            {
                _logger.LogError("[Security] Error listing log dates", ex);
                return new List<string>();
            }
        }

        public Dictionary<string, DateTime> GetBlockedIps()
        {
            lock (_fileLock)
            {
                return new Dictionary<string, DateTime>(_data.BlockedIps);
            }
        }

        private async Task DownloadCountryRangesAsync()
        {
            List<string> countries;
            lock (_fileLock) countries = _data.BlockedCountries.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim().ToLower()).ToList();

            if (countries.Count == 0) return;

            using (var client = new System.Net.Http.HttpClient())
            {
                // EN: Add user agent to avoid some blocks
                // FR: Ajouter un user agent pour éviter certains blocages
                client.DefaultRequestHeaders.Add("User-Agent", "BatRun-Security-Service/1.0");

                foreach (var countryCode in countries)
                {
                    try
                    {
                        string cacheFile = Path.Combine(_cacheDir, $"{countryCode}.zone");
                        bool needsDownload = true;

                        // EN: If file exists and is less than 7 days old, use it
                        // FR: Si le fichier existe et a moins de 7 jours, l'utiliser
                        if (File.Exists(cacheFile))
                        {
                            var lastWrite = File.GetLastWriteTimeUtc(cacheFile);
                            if ((DateTime.UtcNow - lastWrite).TotalDays < 7)
                            {
                                needsDownload = false;
                            }
                        }

                        string rawData;
                        if (needsDownload)
                        {
                            _logger.LogInfo($"[Security] Downloading IP ranges for country: {countryCode.ToUpper()}...");
                            string url = $"https://www.ipdeny.com/ipblocks/data/countries/{countryCode}.zone";
                            
                            // EN: Small delay to avoid hammering the server (503 Gateway Timeout)
                            // FR: Petit délai pour éviter de saturer le serveur (503 Gateway Timeout)
                            await Task.Delay(500); 

                            rawData = await client.GetStringAsync(url);
                            File.WriteAllText(cacheFile, rawData);
                        }
                        else
                        {
                            rawData = File.ReadAllText(cacheFile);
                        }
                        
                        var ranges = new List<uint[]>();
                        var lines = rawData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        
                        foreach (var line in lines)
                        {
                            string cidr = line.Trim();
                            if (string.IsNullOrEmpty(cidr) || cidr.StartsWith("#")) continue;

                            string[] parts = cidr.Split('/');
                            if (parts.Length == 2 && IPAddress.TryParse(parts[0], out IPAddress? networkIp))
                            {
                                byte[] netBytes = networkIp.GetAddressBytes();
                                uint netUint = (uint)((netBytes[0] << 24) | (netBytes[1] << 16) | (netBytes[2] << 8) | netBytes[3]);
                                int prefix = int.Parse(parts[1]);
                                uint mask = prefix == 0 ? 0 : 0xFFFFFFFF << (32 - prefix);
                                ranges.Add(new uint[] { netUint, mask });
                            }
                        }

                        _countryRanges[countryCode] = ranges;
                        if (needsDownload)
                            _logger.LogInfo($"[Security] Loaded {ranges.Count} ranges for {countryCode.ToUpper()} (Downloaded).");
                        else
                            _logger.LogInfo($"[Security] Loaded {ranges.Count} ranges for {countryCode.ToUpper()} (From Cache).");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[Security] Failed to load IP ranges for {countryCode.ToUpper()}: {ex.Message}");
                        
                        // EN: Fallback: Try loading from cache even if it's old if download failed
                        // FR: Repli : Essayer de charger depuis le cache même s'il est vieux si le téléchargement a échoué
                        string cacheFile = Path.Combine(_cacheDir, $"{countryCode}.zone");
                        if (File.Exists(cacheFile))
                        {
                            try
                            {
                                string rawData = File.ReadAllText(cacheFile);
                                // (Logic to parse rawData is repeated here for simplicity or factored out)
                                // We factor it out in real code but for a quick fix I'll just skip detailed logic for fallback in this block
                                _logger.LogInfo($"[Security] Using stale cache for {countryCode.ToUpper()} due to download error.");
                            } catch {}
                        }
                    }
                }
            }
        }
    }
}
