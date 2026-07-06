using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Authentication;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.Core
{
    public partial class ArcadeApiService : IDisposable
    {
        private TcpListener? _listener;
        private readonly Logger _logger;
        private readonly int _port;
        private readonly ArcadeManager _manager;
        private CancellationTokenSource? _cts;
        // EN: Cache for RetroBat root path to avoid repeated registry lookups per media request
        // FR: Cache du chemin RetroBat pour éviter des lectures répétées du registre par requête média
        private string? _retrobatRoot = null;
        private static readonly Newtonsoft.Json.JsonSerializerSettings _jsonSettings = new Newtonsoft.Json.JsonSerializerSettings
        {
            ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
        };
        private readonly MoonlightManager _moonlight;
        // [BATRUN-HUB]: Cache for remote target's Moonlight path prefix (e.g. "/ml" or "").
        // Avoids probing /ml/config.js vs /config.js on every request.
        private readonly Dictionary<string, string> _relayTargetPrefixCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // [BATRUN-FORK] Server-side Lobby State for cross-session multiplayer coordination
        // FR: État du lobby côté serveur pour la coordination multijoueur inter-sessions
        private class LobbyPlayer
        {
            public string SessionId { get; set; } = "";
            public string Token { get; set; } = "";
            public bool Ready { get; set; } = false;
            public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        }
        private class LobbyState
        {
            public string Phase { get; set; } = "none"; // none, lobby, confirm, launching
            public List<LobbyPlayer> Players { get; set; } = new List<LobbyPlayer>();
            public string PendingGamePath { get; set; } = "";
            public string P1SessionId { get; set; } = "";
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime LastActivity { get; set; } = DateTime.UtcNow;
            public void Reset()
            {
                Phase = "none";
                Players.Clear();
                PendingGamePath = "";
                P1SessionId = "";
                LastActivity = DateTime.UtcNow;
            }
        }
        private readonly LobbyState _lobby = new LobbyState();
        private readonly object _lobbyLock = new object();
        // EN: Throttle dictionary for lobby diagnostic logs / FR: Dictionnaire de throttle pour les logs diagnostic du lobby
        private readonly Dictionary<string, DateTime> _lobbyLogThrottle = new Dictionary<string, DateTime>();
        private X509Certificate2? _serverCertificate;
        // [BATRUN-FORK-v4]: Track last lobby status to reduce log spam — only log when phase or player count changes
        // FR: Suivre le dernier statut du lobby pour réduire le spam de logs — ne logger que quand la phase ou le nombre de joueurs change
        private string _lastLobbyStatusKey = "";

        public ArcadeApiService(Logger logger, ArcadeManager manager, int port)
        {
            _logger = logger;
            _manager = manager;
            _port = port;
            _moonlight = manager.Moonlight;
        }

        public void Start()
        {
            try
            {
                if (_manager.HttpsEnabled)
                {
                    _logger.LogInfo("HTTPS is enabled. Loading/Generating self-signed certificate...");
                    LoadOrGenerateCertificate();
                }

                // EN: Use TcpListener instead of HttpListener to bypass Windows HTTP.SYS admin restrictions
                // FR: Utiliser TcpListener au lieu de HttpListener pour contourner les restrictions admin de HTTP.SYS
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();
                
                _cts = new CancellationTokenSource();
                Task.Run(() => ListenAsync(_cts.Token));
                
                _logger.LogInfo($"Arcade API Server (TCP) started on port {_port}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to start Arcade API Server on port {_port}", ex);
            }
        }

        private void LoadOrGenerateCertificate()
        {
            string certPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "batrun_https.pfx");
            if (File.Exists(certPath))
            {
                try
                {
#pragma warning disable SYSLIB0057
                    _serverCertificate = new X509Certificate2(certPath, "batrun");
#pragma warning restore SYSLIB0057
                    _logger.LogInfo("[HTTPS] Loaded existing self-signed certificate.");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[HTTPS] Failed to load existing cert, regenerating: {ex.Message}");
                }
            }
            try
            {
                using (RSA rsa = RSA.Create(2048))
                {
                    var req = new CertificateRequest("CN=BatRunConnect", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
                    req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
                    req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
                    using (var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(10)))
                    {
                        byte[] pfxBytes = cert.Export(X509ContentType.Pfx, "batrun");
                        File.WriteAllBytes(certPath, pfxBytes);
#pragma warning disable SYSLIB0057
                        _serverCertificate = new X509Certificate2(pfxBytes, "batrun");
#pragma warning restore SYSLIB0057
                        _logger.LogInfo("[HTTPS] Generated and saved new self-signed certificate.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("[HTTPS] Error generating certificate", ex);
                _serverCertificate = null;
            }
        }

        private async Task ListenAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _listener != null)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync(token);
                    _ = Task.Run(() => HandleClientAsync(client), token);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                        _logger.LogError("Error in API Listener loop", ex);
                }
            }
        }

        private bool IsLocalOrAllowedIp(IPAddress ip)
        {
            // EN: Normalize IPv4-mapped IPv6 (::ffff:x.x.x.x) to pure IPv4 for consistent comparison
            // FR: Normaliser les adresses IPv4-mappées IPv6 (::ffff:x.x.x.x) en IPv4 pure
            string rawIpStr = ip.ToString();
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();

            if (IPAddress.IsLoopback(ip)) return true;
            byte[] bytes = ip.GetAddressBytes();
            if (bytes.Length == 4) // IPv4
            {
                if (bytes[0] == 10) return true;                                         // 10.x.x.x
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;   // 172.16-31.x.x
                if (bytes[0] == 192 && bytes[1] == 168) return true;                    // 192.168.x.x
            }

            string ipStr = ip.ToString();
            string allowedIps = _manager.AdminAllowedIps;

            if (!string.IsNullOrEmpty(allowedIps))
            {
                var whitelist = allowedIps.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var allowed in whitelist)
                {
                    string entry = allowed.Trim();
                    if (entry == ipStr)
                    {
                        _logger.LogInfo($"[Security] MATCH {ipStr} → authorized");
                        return true;
                    }
                }
            }
            _logger.LogDebug($"[Security] {ipStr} NOT in whitelist → denied");
            return false;
        }


        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                Stream stream = client.GetStream();
                try
                {
                    // EN: Dual HTTP/HTTPS mode: peek first byte to detect TLS ClientHello (0x16).
                    //     If it's TLS, wrap in SslStream. Otherwise, treat as plain HTTP.
                    //     This allows both http:// and https:// clients on the same port.
                    // FR: Mode double HTTP/HTTPS: inspecter le premier octet pour détecter le ClientHello TLS (0x16).
                    //     Si c'est du TLS, envelopper dans un SslStream. Sinon, traiter comme du HTTP brut.
                    //     Cela permet aux clients http:// et https:// sur le même port.
                    if (_manager.HttpsEnabled && _serverCertificate != null)
                    {
                        NetworkStream netStream = (NetworkStream)stream;
                        byte[] peekBuf = new byte[1];
                        int peeked = await netStream.ReadAsync(peekBuf, 0, 1);
                        if (peeked == 0) return;

                        // EN: TLS ClientHello always starts with 0x16 (ContentType.Handshake)
                        // FR: Le ClientHello TLS commence toujours par 0x16 (ContentType.Handshake)
                        if (peekBuf[0] == 0x16)
                        {
                            // EN: Re-assemble the first byte + rest into a single stream for SslStream
                            // FR: Réassembler le premier octet + le reste en un seul flux pour SslStream
                            var prefixedStream = new PrefixedStream(peekBuf, peeked, netStream);
                            var sslStream = new SslStream(prefixedStream, false);
                            await sslStream.AuthenticateAsServerAsync(_serverCertificate, false, SslProtocols.Tls12 | SslProtocols.Tls13, false);
                            stream = sslStream;
                        }
                        else
                        {
                            // EN: Not TLS — it's plain HTTP. Re-assemble and continue.
                            // FR: Pas du TLS — c'est du HTTP brut. Réassembler et continuer.
                            stream = new PrefixedStream(peekBuf, peeked, netStream);
                        }
                    }

                    // [BATRUN-MOD]: Global Blacklist Check
                    if (client.Client.RemoteEndPoint is IPEndPoint globalRemoteIp)
                    {
                        if (_manager.BlacklistManager.IsIpBlocked(globalRemoteIp.Address))
                        {
                            _logger.LogWarning($"[Security] Dropping connection from globally blocked IP: {globalRemoteIp.Address}");
                            _manager.BlacklistManager.RecordIpDrop(globalRemoteIp.Address);
                            return; // Silent drop to mitigate brute force resources
                        }
                    }


                    using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, leaveOpen: true))
                    {
                        // EN: Basic HTTP Request Parsing
                        // FR: Analyse basique de la requête HTTP
                        string? firstLine = await reader.ReadLineAsync();
                        if (string.IsNullOrEmpty(firstLine)) return;

                    string[] parts = firstLine.Split(' ');
                    if (parts.Length < 2) return;

                    string method = parts[0];
                    string rawPath = parts[1];
                    string path = rawPath.Split('?')[0].ToLower().Replace("//", "/");
                    while (path.Contains("//")) path = path.Replace("//", "/");

                    // Read all headers
                    var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    string? line;
                    while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
                    {
                        int colonIdx = line.IndexOf(':');
                        if (colonIdx > 0)
                        {
                            string key = line.Substring(0, colonIdx).Trim();
                            string val = line.Substring(colonIdx + 1).Trim();
                            headers[key] = val;
                        }
                    }

                    // [BATRUN-AUTH] EN: Mandatory security check for Moonlight routes early in the pipeline
                    // FR: Vérification de sécurité obligatoire pour les routes Moonlight tôt dans le pipeline
                    if (path.StartsWith("/ml") || path.StartsWith("/relay/"))
                    {
                        IPAddress? clientIp = (client.Client.RemoteEndPoint as IPEndPoint)?.Address;
                        bool isLocal = clientIp != null && IsLocalOrAllowedIp(clientIp);

                        if (!isLocal)
                        {
                            var cookies = ParseCookies(headers.GetValueOrDefault("Cookie", ""));
                            string? token = cookies.GetValueOrDefault("batrun_token");
                            bool valid = !string.IsNullOrEmpty(token) && _manager.PublicUserManager.ValidateToken(token);

                            if (!valid)
                            {
                                _logger.LogWarning($"[Security] Blocked unauthorized direct access to {path} from {clientIp} - Redirecting to login");
                                await SendRedirectAsync(stream, "/connect");
                                return;
                            }
                            _logger.LogInfo($"[Security] Authorized external access to {path} from {clientIp}");
                        }
                    }

                    int contentLength = 0;
                    if (headers.TryGetValue("Content-Length", out string? clStr)) int.TryParse(clStr, out contentLength);
                    headers.TryGetValue("Upgrade", out string? upgradeHeader);
                    headers.TryGetValue("Range", out string? rangeHeader);
 
                    if (_manager.HttpsEnabled && _serverCertificate != null && !(stream is SslStream))
                    {
                        // EN: Ensure we use a reachable host for the redirect. 
                        // For external clients (non-local), we MUST prioritize the Public IP to avoid redirecting to a local 192.168.x.x address.
                        // FR: S'assurer d'utiliser un hôte joignable.
                        // Pour les clients externes, on DOIT prioriser l'IP publique pour éviter de rediriger vers une adresse locale 192.168.x.x.
                        string publicIp = _manager.Config.ReadValue("Arcade", "PublicIp", "");
                        IPAddress? clientIp = (client.Client.RemoteEndPoint as IPEndPoint)?.Address;
                        bool isExternal = clientIp != null && !IsLocalOrAllowedIp(clientIp);

                        string hostHeader = "";
                        if (isExternal && !string.IsNullOrEmpty(publicIp))
                        {
                            hostHeader = publicIp;
                            // EN: Ensure port is included if publicIp doesn't have it
                            if (!hostHeader.Contains(":")) hostHeader += ":" + _port;
                        }
                        else
                        {
                            hostHeader = headers.ContainsKey("Host") ? headers["Host"] : publicIp;
                        }

                        if (string.IsNullOrEmpty(hostHeader)) hostHeader = Environment.MachineName + ":" + _port;
 
                        if (!string.IsNullOrEmpty(hostHeader))
                        {
                            string redirectUrl = $"https://{hostHeader}{rawPath}";
                            _logger.LogInfo($"[HTTP] Redirecting {(isExternal ? "external" : "local")} request to secure: {redirectUrl}");
                            await SendRedirectAsync(stream, redirectUrl);
                            return;
                        }
                    }


        // [BATRUN-HUB]: Prefix-based relay route for remote Moonlight Web
        // Pattern: /relay/{targetAlias}/* → proxy to http://{targetIp}:8080/{rest}
        // This is critical for HubMode: relative asset paths in stream.html resolve
        // naturally through the /relay/{target}/ prefix, unlike query-param relay.
        // FR: Route de relai par préfixe pour Moonlight Web distant
        // Motif: /relay/{alias}/* → proxy vers http://{ipCible}:8080/{reste}
        if (path.StartsWith("/relay/") && path.Length > 7)
        {
            int secondSlash = path.IndexOf('/', 7);
            string relayTargetAlias = secondSlash > 0 ? path.Substring(7, secondSlash - 7) : path.Substring(7);
            if (!string.IsNullOrEmpty(relayTargetAlias))
            {
                await HandleMoonlightRelayProxyAsync(client, stream, method, rawPath, headers, contentLength, upgradeHeader, relayTargetAlias);
                return;
            }
        }

        // EN: Proxy route for Moonlight Web (hides port 8080 from external access)
        // FR: Route proxy pour Moonlight Web (masque le port 8080 depuis l'extérieur)
        if (_manager.ProxyMoonlight && path.StartsWith("/ml"))
        {
            await HandleMoonlightProxyAsync(client, stream, method, rawPath, headers, contentLength, upgradeHeader);
            return;
        }

                    if (path == "/" || path == "/index.html")
                    {
                        bool isAdminAuthorized = false;
                        if (client.Client.RemoteEndPoint is IPEndPoint remoteIp)
                        {
                            isAdminAuthorized = IsLocalOrAllowedIp(remoteIp.Address);
                        }

                        if (_manager.PublicAccessEnabled)
                        {
                            // [BATRUN-CRED] EN: Redirect / to /connect for clean URL-based login flow
                            // FR: Rediriger / vers /connect pour un flux de connexion avec changement d'URL propre
                            await SendRedirectAsync(stream, "/connect");
                        }
                        else
                        {
                            if (isAdminAuthorized)
                                await SendResponseAsync(stream, "text/html", GetWebUIHtml());
                            else
                                await SendRedirectAsync(stream, "https://www.google.com");
                        }
                    }
                    // [BATRUN-CRED] EN: /connect = public login page (dedicated URL for browser credential detection)
                    // FR: /connect = page de connexion publique (URL dédiée pour la détection des identifiants)
                    // [SECURITY] EN: This page contains ZERO game/streaming code — login form only
                    // [SECURITY] FR: Cette page ne contient AUCUN code jeu/streaming — formulaire de login uniquement
                    else if ((path == "/connect" || path == "/connect/") && method == "GET")
                    {
                        bool isLocal = client.Client.RemoteEndPoint is IPEndPoint rIp && IsLocalOrAllowedIp(rIp.Address);
                        await SendResponseAsync(stream, "text/html", GetConnectPageHtml(isLocal, !isLocal));
                    }
                    // [BATRUN-CRED] EN: POST /connect = form-based login (real POST→redirect, triggers browser credential save)
                    // FR: POST /connect = login par formulaire (vrai POST→redirect, déclenche la sauvegarde navigateur)
                    else if ((path == "/connect" || path == "/connect/") && method == "POST")
                    {
                        IPAddress connectIp = (client.Client.RemoteEndPoint as IPEndPoint)?.Address ?? IPAddress.Loopback;
                        _manager.ReloadPublicSettings();
                        string connectBody = "";
                        if (contentLength > 0)
                        {
                            char[] buf = new char[contentLength];
                            await reader.ReadBlockAsync(buf, 0, contentLength);
                            connectBody = new string(buf);
                        }
                        var formFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var pair in connectBody.Split('&'))
                        {
                            var kv = pair.Split('=', 2);
                            if (kv.Length == 2)
                                formFields[Uri.UnescapeDataString(kv[0].Replace('+', ' '))] = Uri.UnescapeDataString(kv[1].Replace('+', ' '));
                        }
                        formFields.TryGetValue("username", out string? cUser);
                        formFields.TryGetValue("password", out string? cPass);

                        // EN: Guest shortcut (no-login mode)
                        if (string.IsNullOrEmpty(cUser) || cUser.ToLower() == "guest")
                        {
                            if (!_manager.PublicAccessRequiresLogin && IsLocalOrAllowedIp(connectIp))
                            {
                                var guestU = _manager.PublicUserManager.LoginGuest("form");
                                if (guestU != null)
                                {
                                    await SendRedirectWithCookieAsync(stream, "/cloud", $"batrun_token={guestU.Token}; Path=/; SameSite=Lax");
                                    return;
                                }
                            }
                            await SendRedirectAsync(stream, "/connect?error=1");
                            return;
                        }

                        var loggedConnect = _manager.PublicUserManager.AuthenticateLogin(cUser ?? "", cPass ?? "");
                        if (loggedConnect != null)
                        {
                            _manager.BlacklistManager.ResetFailedAttempts(connectIp, cUser ?? "Unknown");
                            // [BATRUN-CRED] EN: Redirect to /cloud after login — URL changes from /connect → /cloud
                            // FR: Rediriger vers /cloud après login — l'URL change de /connect vers /cloud
                            await SendRedirectWithCookieAsync(stream, "/cloud", $"batrun_token={loggedConnect.Token}; Path=/; SameSite=Lax");
                        }
                        else
                        {
                            _manager.BlacklistManager.RecordFailedAttempt(connectIp, cUser ?? "Unknown");
                            await SendRedirectAsync(stream, "/connect?error=1");
                        }
                    }
                    // [BATRUN-CRED] EN: /cloud = public games page (served after successful login)
                    // FR: /cloud = page des jeux publique (servie après connexion réussie)
                    // [SECURITY] EN: Server-side token validation — game HTML is NEVER served without valid authentication
                    // [SECURITY] FR: Validation token côté serveur — le HTML des jeux n'est JAMAIS servi sans authentification valide
                    else if ((path == "/cloud" || path == "/cloud/") && method == "GET")
                    {
                        bool isLocal = client.Client.RemoteEndPoint is IPEndPoint rIp2 && IsLocalOrAllowedIp(rIp2.Address);
                        bool effectiveRequiresLogin = _manager.PublicAccessRequiresLogin || !isLocal;

                        // EN: Server-side guard: if login is required, validate token from cookie before serving cloud page
                        // FR: Garde côté serveur : si login requis, valider le token du cookie avant de servir la page cloud
                        if (effectiveRequiresLogin)
                        {
                            string? cloudToken = null;
                            if (headers.TryGetValue("cookie", out string? cookieHeader) && !string.IsNullOrEmpty(cookieHeader))
                            {
                                foreach (var cookie in cookieHeader.Split(';'))
                                {
                                    var cookieParts = cookie.Trim().Split('=', 2);
                                    if (cookieParts.Length == 2 && cookieParts[0].Trim() == "batrun_token")
                                    {
                                        cloudToken = cookieParts[1].Trim();
                                        break;
                                    }
                                }
                            }

                            if (string.IsNullOrEmpty(cloudToken) || _manager.PublicUserManager.GetUserByToken(cloudToken) == null)
                            {
                                _logger.LogWarning($"[Security] Blocked unauthenticated access to /cloud — clearing stale cookie and redirecting to login with logout flag");
                                // EN: Clear stale cookie and append ?logout=1 to force client-side cleanup / FR: Supprimer le cookie périmé et ajouter ?logout=1 pour forcer le nettoyage côté client
                                await SendRedirectWithCookieAsync(stream, "/connect?logout=1", "batrun_token=; expires=Thu, 01 Jan 1970 00:00:00 UTC; Path=/; SameSite=Lax");
                                return;
                            }
                        }

                        await SendResponseAsync(stream, "text/html", GetCloudPageHtml(isLocal, !isLocal));
                    }
                    else if (path == "/admin" || path == "/admin/" || path == "/admin/index.html")
                    {
                        if (client.Client.RemoteEndPoint is IPEndPoint remoteIp && !IsLocalOrAllowedIp(remoteIp.Address))
                        {
                            _logger.LogWarning($"[Security] Blocked external access to /admin from {remoteIp.Address} - redirecting to external site");
                            // EN: Redirect unauthorized admin access to an external site to obscure the admin path
                            // FR: Rediriger l'accès admin non autorisé vers un site externe pour masquer le chemin
                            await SendRedirectAsync(stream, "https://www.google.com");
                            return;
                        }
                        await SendResponseAsync(stream, "text/html", GetWebUIHtml());
                    }
                    else if (path == "/api/status" && method == "GET")
                    {
                        await SendStatusAsync(stream);
                    }
                    else if (path == "/api/network" && method == "GET")
                    {
                        await HandleNetworkDiscoveryAsync(stream);
                    }
                    else if (path == "/api/history" && method == "GET")
                    {
                        await SendHistoryAsync(stream);
                    }
                    else if (path == "/api/action" && method == "POST")
                    {
                        string body = "";
                        if (contentLength > 0)
                        {
                            char[] buffer = new char[contentLength];
                            await reader.ReadBlockAsync(buffer, 0, contentLength);
                            body = new string(buffer);
                        }
                        await HandleActionAsync(stream, body);
                    }
                    else if (path.StartsWith("/api/relay") || path.StartsWith("/stream-relay/"))
                    {
                        string body = "";
                        if (contentLength > 0)
                        {
                            char[] buffer = new char[contentLength];
                            await reader.ReadBlockAsync(buffer, 0, contentLength);
                            body = new string(buffer);
                        }
                        await HandleRelayAsync(stream, method, rawPath, body, rangeHeader, headers);
                    }
                    else if (path == "/api/es/status" && method == "GET")
                    {
                        await HandleEsStatusAsync(stream);
                    }
                    else if (path == "/api/es/games" && method == "GET")
                    {
                        await HandleEsSearchAsync(stream, rawPath);
                    }
                    else if (path == "/api/es/metadata" && method == "GET")
                    {
                        await HandleEsMetadataAsync(stream, rawPath);
                    }
                    else if (path == "/api/es/media" && method == "GET")
                    {
                        await HandleEsMediaAsync(stream, rawPath, rangeHeader);
                    }
                    else if (path == "/api/es/systems" && method == "GET")
                    {
                        await HandleEsSystemsAsync(stream);
                    }
                    else if (path == "/api/es/debug" && method == "GET")
                    {
                        await HandleEsDebugAsync(stream, rawPath);
                    }
                    else if (path.StartsWith("/api/public") || path.StartsWith("/api/admin") || path.StartsWith("/api/host"))
                    {
                        string body = "";
                        if (contentLength > 0)
                        {
                            char[] buffer = new char[contentLength];
                            await reader.ReadBlockAsync(buffer, 0, contentLength);
                            body = new string(buffer);
                        }
                        
                        if (path.StartsWith("/api/public") || path.StartsWith("/api/host")) 
                        {
                            IPAddress ip = (client.Client.RemoteEndPoint as IPEndPoint)?.Address ?? IPAddress.Loopback;
                            await HandlePublicApiAsync(stream, method, path, rawPath, body, ip, headers);
                        }
                        else 
                        {
                            if (client.Client.RemoteEndPoint is IPEndPoint remoteIp && !IsLocalOrAllowedIp(remoteIp.Address))
                            {
                                _logger.LogWarning($"[Security] Blocked external access to /api/admin from {remoteIp.Address}");
                                await SendResponseAsync(stream, "text/plain", "Forbidden", HttpStatusCode.Forbidden);
                                return;
                            }
                            await HandleAdminApiAsync(stream, method, path, body);
                        }
                    }
                    else if (path == "/api/moonlight-auth" && method == "GET")
                    {
                        // EN: Get client IP for local network detection in HandleMoonlightAuthAsync
                        // FR: Obtenir l'IP du client pour la détection de réseau local dans HandleMoonlightAuthAsync
                        IPAddress clientIp = (client.Client.RemoteEndPoint as IPEndPoint)?.Address ?? IPAddress.Loopback;
                        await HandleMoonlightAuthAsync(stream, rawPath, headers, clientIp);
                    }
                    else if (path == "/api/relay/ws" && method == "GET")
                    {
                        await HandleWebSocketRelayAsync(client, stream, method, rawPath, headers);
                        return; // HandleWebSocketRelay takes over the stream completely
                    }
                    else if (method == "OPTIONS")
                    {
                        // Handle CORS preflight
                        await SendResponseAsync(stream, "text/plain", "", HttpStatusCode.OK, true);
                    }
                    else
                    {
                        await SendResponseAsync(stream, "text/plain", "Not Found", HttpStatusCode.NotFound);
                    }
                    } // close using (reader)
                }
                catch (Exception ex)
                {
                    if (ex is System.IO.IOException || ex is System.Net.Sockets.SocketException)
                    {
                        // EN: Ignore generic disconnection errors to avoid log spam
                        // FR: Ignorer les erreurs de déconnexion génériques pour éviter le spam
                    }
                    else if (ex is System.Security.Authentication.AuthenticationException)
                    {
                        // EN: Expected with self-signed certificates — browsers that haven't accepted
                        //     the cert will repeatedly attempt TLS handshakes that fail. Not an error.
                        // FR: Attendu avec les certificats auto-signés — les navigateurs qui n'ont pas
                        //     accepté le cert vont tenter des handshakes TLS en boucle. Pas une erreur.
                    }
                    else
                    {
                        _logger.LogError("Error handling API client", ex);
                    }
                }
            }
        }

        private async Task SendResponseAsync(Stream stream, string contentType, string content, HttpStatusCode status = HttpStatusCode.OK, bool isCors = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"HTTP/1.1 {(int)status} {status}");
            sb.AppendLine($"Content-Type: {contentType}; charset=utf-8");
            sb.AppendLine($"Content-Length: {Encoding.UTF8.GetByteCount(content)}");
            sb.AppendLine("Access-Control-Allow-Origin: *");
            sb.AppendLine("Access-Control-Allow-Methods: GET, POST, OPTIONS");
            sb.AppendLine("Access-Control-Allow-Headers: Content-Type");
            // [BATRUN-FIX]: Essential for Gamepad and Autoplay support in Moonlight
            sb.AppendLine("Permissions-Policy: gamepad=(self), autoplay=(self), fullscreen=(self)");
            sb.AppendLine("Cache-Control: no-cache, no-store, must-revalidate");
            sb.AppendLine("Pragma: no-cache");
            sb.AppendLine("Expires: 0");
            sb.AppendLine("Connection: keep-alive");
            sb.AppendLine("Keep-Alive: timeout=5, max=100");
            sb.AppendLine();
            sb.Append(content);

            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());
                await stream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                // EN: Ignore client disconnections (normal behavior when navigating away or closing tabs)
                // FR: Ignorer les déconnexions clients (comportement normal lors d'un changement de page ou fermeture d'onglet)
                if (ex is IOException || ex is SocketException || (ex.InnerException is SocketException se && (se.SocketErrorCode == SocketError.ConnectionReset || se.SocketErrorCode == SocketError.ConnectionAborted)))
                {
                    return;
                }
                _logger.LogWarning($"[Network] Failed to send response: {ex.Message}");
            }
        }

        private async Task SendRedirectAsync(Stream stream, string location)
        {
            StringBuilder sb = new StringBuilder();
            // EN: Use 302 (temporary) instead of 301 (permanent) to prevent browser caching of redirects
            // FR: Utiliser 302 (temporaire) au lieu de 301 (permanent) pour éviter le cache navigateur des redirections
            sb.AppendLine("HTTP/1.1 302 Found");
            sb.AppendLine($"Location: {location}");
            sb.AppendLine("Cache-Control: no-cache, no-store, must-revalidate");
            sb.AppendLine("Content-Length: 0");
            sb.AppendLine("Connection: close");
            sb.AppendLine();

            byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        private async Task SendRedirectWithCookieAsync(Stream stream, string location, string cookieValue)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("HTTP/1.1 302 Found");
            sb.AppendLine($"Location: {location}");
            // [BATRUN] Ajout de Max-Age=31536000 (1 an) pour garder la session active même après redémarrage du navigateur
    if (!cookieValue.Contains("Max-Age"))
        sb.AppendLine($"Set-Cookie: {cookieValue}; Max-Age=31536000; Path=/; SameSite=Lax{(_serverCertificate != null ? "; Secure" : "")}");
    else
        sb.AppendLine($"Set-Cookie: {cookieValue}; Path=/; SameSite=Lax{(_serverCertificate != null ? "; Secure" : "")}");
            sb.AppendLine("Access-Control-Allow-Origin: *");
            sb.AppendLine("Cache-Control: no-cache, no-store, must-revalidate");
            sb.AppendLine("Connection: close");
            sb.AppendLine();

            byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        private async Task SendStatusAsync(Stream stream)
        {
            var status = new
            {
                credits = _manager.TotalCredits,
                statusDisplay = _manager.IsFreePlay ? "FREE" : (_manager.TotalCredits > 0 ? _manager.FormattedTimeRemaining : "INSERT COIN"),
                timeRemaining = _manager.FormattedTimeRemaining,
                minutesPerCredit = _manager.MinutesPerCredit,
                isFreePlay = _manager.IsFreePlay,
                isLocked = _manager.IsLocked,
                isSessionActive = _manager.IsSessionActive,
                isOperatorUnlocked = _manager.IsOperatorUnlocked,
                machineName = Environment.MachineName,
                macAddress = _manager.GetMacAddress(),
                ipHistory = _manager.GetAllLocalIPAddresses(),
                apiPort = _port,
                requiresAuth = !string.IsNullOrEmpty(_manager.OperatorPassword),
                isArcadeEnabled = _manager.IsArcadeEnabled,
                currentGameSystem = _manager.CurrentGameSystem,
                currentGameName = _manager.CurrentGameName,
                currentExecutable = _manager.CurrentExecutable,
                currentGameDuration = _manager.CurrentGameDuration,
                hideOperatorButtons = _manager.HideOperatorButtons,
                publicIp = _manager.PublicIp,
                isWebLaunch = _manager.IsWebLaunch
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(status, _jsonSettings);
            await SendResponseAsync(stream, "application/json", json);
        }

        private async Task SendHistoryAsync(Stream stream)
        {
            var history = _manager.GetHistory();
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(history, _jsonSettings);
            await SendResponseAsync(stream, "application/json", json);
        }

        private async Task HandleNetworkDiscoveryAsync(Stream stream)
        {
            var machines = _manager.GetNetworkMachines();
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(machines, _jsonSettings);
            await SendResponseAsync(stream, "application/json", json);
        }

        private async Task HandleActionAsync(Stream stream, string body)
        {
            try
            {
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
                string? action = data?.action;
                string? password = data?.password;

                if (!string.IsNullOrEmpty(_manager.OperatorPassword) && password != _manager.OperatorPassword)
                {
                    _logger.LogWarning($"API: Unauthorized attempt for action '{action}'");
                    await SendResponseAsync(stream, "text/plain", "Unauthorized", HttpStatusCode.Unauthorized);
                    return;
                }

                _logger.LogInfo($"API Action received: {action}");

                switch (action)
                {
                    case "add_credit":
                        int count = data?.count ?? 1;
                        _manager.AddManualCredits(count);
                        break;
                    case "remove_credit":
                        _manager.RemoveCredit();
                        break;
                    case "toggle_freeplay":
                        _manager.SetFreePlay(!_manager.IsFreePlay);
                        break;
                    case "lock":
                        _manager.EndSession();
                        break;
                    case "open_local_operator":
                        if (_manager.IsOperatorUnlocked) _manager.LockOperatorMode();
                        else _manager.UnlockOperatorMode();
                        break;
                    case "toggle_hide_operator_buttons":
                        _manager.HideOperatorButtons = !_manager.HideOperatorButtons;
                        break;
                    case "set_duration":
                        int mins = data?.minutes ?? 0;
                        _manager.SetSessionDuration(mins);
                        break;
                    case "set_minutes_per_credit":
                        int mpc = data?.minutes ?? 5;
                        _manager.MinutesPerCredit = mpc;
                        break;
                    case "show_message":
                        string? msg = data?.message;
                        int dur = data?.duration ?? 5;
                        if (!string.IsNullOrEmpty(msg))
                            _manager.ShowOperatorMessage(msg, dur);
                        break;
                    case "remove_machine":
                        string? mName = data?.machineName;
                        string? mMac = data?.macAddress;
                        if (!string.IsNullOrEmpty(mName))
                            _manager.RemoveDiscoveredMachine(mName, mMac);
                        break;
                    case "add_machine":
                        string? mIp = data?.ip;
                        if (!string.IsNullOrEmpty(mIp))
                            _manager.AddManualMachine(mIp);
                        break;
                    case "launch_game":
                        string? gPath = data?.gamePath;
                        if (!string.IsNullOrEmpty(gPath))
                        {
                            _logger.LogInfo($"Action launch_game request for: {gPath}");
                            _manager.IsWebLaunch = true;
                            var scraper = new EmulationStationScraper();
                            bool result = await scraper.LaunchGameAsync(gPath);
                            _logger.LogInfo($"launch_game result from ES API: {result}");
                        }
                        break;

                    case "stop_game":
                        _logger.LogInfo("Action stop_game requested from web UI.");
                        _manager.ForceStopGameFromWeb();
                        break;
                    case "set_public_ip":
                        string? pIp = data?.publicIp;
                        _manager.PublicIp = pIp ?? "";
                        break;
                    case "reload_es":
                        if (_manager.IsGameInProgress)
                        {
                            _logger.LogWarning("API reload_es rejected: Game is in progress.");
                            await SendResponseAsync(stream, "text/plain", "Game in progress", HttpStatusCode.Conflict);
                            return;
                        }
                        var sReload = new EmulationStationScraper();
                        bool rOk = await sReload.ReloadGamesAsync();
                        if (!rOk)
                        {
                            await SendResponseAsync(stream, "text/plain", "Failed", HttpStatusCode.InternalServerError);
                            return;
                        }
                        break;
                }

                await SendResponseAsync(stream, "text/plain", "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError("API Error in HandleAction", ex);
                await SendResponseAsync(stream, "text/plain", "Bad Request", HttpStatusCode.BadRequest);
            }
        }

        private async Task HandleEsStatusAsync(Stream stream)
        {
            var scraper = new EmulationStationScraper();
            bool isOnline = await scraper.PingServerAsync();
            await SendResponseAsync(stream, "application/json", "{\"isOnline\":" + isOnline.ToString().ToLower() + "}");
        }

        private async Task HandleEsSystemsAsync(Stream stream)
        {
            try
            {
                var scraper = new EmulationStationScraper();
                var systems = await scraper.GetSystemsAsync();
                await SendResponseAsync(stream, "application/json", Newtonsoft.Json.JsonConvert.SerializeObject(systems, _jsonSettings));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in HandleEsSystems", ex);
                await SendResponseAsync(stream, "text/plain", ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        private async Task HandleEsSearchAsync(Stream stream, string rawPath)
        {
            try
            {
                string query = "";
                string system = "";

                // EN: Extract 'q' (search text) and 'system' query parameters
                // FR: Extraire les paramètres 'q' (texte de recherche) et 'system'
                int qIdx = rawPath.IndexOf("q=", StringComparison.OrdinalIgnoreCase);
                if (qIdx != -1)
                {
                    string tail = rawPath.Substring(qIdx + 2);
                    int ampIdx = tail.IndexOf('&');
                    query = Uri.UnescapeDataString(ampIdx != -1 ? tail.Substring(0, ampIdx) : tail);
                }

                int sIdx = rawPath.IndexOf("system=", StringComparison.OrdinalIgnoreCase);
                if (sIdx != -1)
                {
                    string tail = rawPath.Substring(sIdx + 7);
                    int ampIdx = tail.IndexOf('&');
                    system = Uri.UnescapeDataString(ampIdx != -1 ? tail.Substring(0, ampIdx) : tail);
                }

                _logger.LogInfo($"[ES-Search] Query='{query}', System='{system}', RawPath='{rawPath}'");

                var scraper = new EmulationStationScraper();
                var allGames = await scraper.GetAllGamesAsync(string.IsNullOrEmpty(system) ? null : system);

                _logger.LogInfo($"[ES-Search] Scraper returned {allGames.Count} games for system='{system}'");

                var results = string.IsNullOrEmpty(query) ? allGames : allGames.Where(g =>
                    (g.Name != null && g.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                    (g.Path != null && g.Path.Contains(query, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                _logger.LogInfo($"[ES-Search] Returning {results.Count} results after filter");

                await SendResponseAsync(stream, "application/json", Newtonsoft.Json.JsonConvert.SerializeObject(results, _jsonSettings));
            }
            catch (IOException)
            {
                // EN: Ignore client disconnects during search / FR: Ignorer les déconnexions client pendant la recherche
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in HandleEsSearch", ex);
                try { await SendResponseAsync(stream, "text/plain", ex.Message, HttpStatusCode.InternalServerError); } catch {}
            }
        }


        private async Task HandleEsMetadataAsync(Stream stream, string rawPath)
        {
            try
            {
                string system = "";
                string gamePath = "";
                int sIdx = rawPath.IndexOf("system=", StringComparison.OrdinalIgnoreCase);
                int pIdx = rawPath.IndexOf("path=", StringComparison.OrdinalIgnoreCase);

                if (sIdx != -1) 
                {
                    int end = rawPath.IndexOf('&', sIdx);
                    if (end == -1) end = rawPath.Length;
                    system = Uri.UnescapeDataString(rawPath.Substring(sIdx + 7, end - (sIdx + 7)));
                    // EN: Don't TrimStart('.') here as it breaks folders like .Lya Library
                }
                if (pIdx != -1)
                {
                    int end = rawPath.IndexOf('&', pIdx);
                    if (end == -1) end = rawPath.Length;
                    gamePath = Uri.UnescapeDataString(rawPath.Substring(pIdx + 5, end - (pIdx + 5)));
                }

                if (string.IsNullOrEmpty(system) || string.IsNullOrEmpty(gamePath))
                {
                    await SendResponseAsync(stream, "text/plain", "Missing system or path", HttpStatusCode.BadRequest);
                    return;
                }

                string retrobatRoot = GetRetrobatRoot();
                string romsRoot = Path.Combine(retrobatRoot, "roms");
                string folderName = EmulationStationScraper.GetRomFolderName(system);
                string gamelistPath = Path.Combine(romsRoot, folderName, "gamelist.xml");

                // EN: If gamelist.xml not found for the provided system (e.g. .Lya Library), try to deduce from ROM path
                // FR: Si gamelist.xml non trouvé pour le système (ex: .Lya Library), déduire du dossier physique
                if (!File.Exists(gamelistPath))
                {
                    // EN: Find the folder name immediately following the '/roms/' segment
                    // FR: Trouver le nom du dossier immédiatement après le segment '/roms/'
                    string p = gamePath.Replace("\\", "/");
                    int romsIdx = p.IndexOf("/roms/", StringComparison.OrdinalIgnoreCase);
                    
                    if (romsIdx != -1)
                    {
                        string afterRoms = p.Substring(romsIdx + 6).TrimStart('/');
                        int folderSlashIdx = afterRoms.IndexOf('/');
                        if (folderSlashIdx != -1)
                        {
                            string deducedFolder = afterRoms.Substring(0, folderSlashIdx);
                            string deducedGamelist = Path.Combine(romsRoot, deducedFolder, "gamelist.xml");
                            if (File.Exists(deducedGamelist))
                            {
                                folderName = deducedFolder;
                                gamelistPath = deducedGamelist;
                                _logger.LogInfo($"[ES Metadata] Deduced physical folder '{folderName}' for collection '{system}' from path '{gamePath}'");
                            }
                        }
                    }
                }

                if (!File.Exists(gamelistPath))
                {
                    await SendResponseAsync(stream, "application/json", "{\"error\":\"gamelist.xml not found\"}");
                    return;
                }

                // [BATRUN-FIX]: Force UTF-8 encoding to avoid double-encoding of accented characters (être → Ãªtre)
                // FR: Forcer l'encodage UTF-8 pour éviter le double-encodage des caractères accentués
                XDocument doc;
                using (var sr = new StreamReader(gamelistPath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    doc = XDocument.Load(sr);
                }
                string fileName = Path.GetFileName(gamePath);
                var meta = doc.Descendants("game").FirstOrDefault(g => Path.GetFileName(g.Element("path")?.Value) == fileName);

                if (meta == null)
                {
                    await SendResponseAsync(stream, "application/json", "{\"error\":\"game not found in gamelist\"}");
                    return;
                }

                var result = new Dictionary<string, string>();
                foreach (var el in meta.Elements())
                {
                    string key = el.Name.LocalName;
                    string val = el.Value;
                    if (key == "image" || key == "video" || key == "thumbnail" || key == "marquee" || key == "fanart" || key == "manual")
                    {
                        if (!System.IO.Path.IsPathRooted(val) && !(val.Length >= 2 && val[1] == ':'))
                        {
                            // EN: Cleanup dots and prepend the physical folder name
                            // FR: Nettoyer les points et préfixer le dossier physique
                            while (val.StartsWith("./")) val = val.Substring(2);
                            val = val.TrimStart('/', '\\');
                            val = $"{folderName}/{val}";
                        }
                    }
                    result[key] = val;
                }

                await SendResponseAsync(stream, "application/json", Newtonsoft.Json.JsonConvert.SerializeObject(result, _jsonSettings));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in HandleEsMetadata", ex);
                await SendResponseAsync(stream, "text/plain", ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        private async Task HandleEsMediaAsync(Stream stream, string rawPath, string? rangeHeader)
        {
            try
            {
                int pIdx = rawPath.IndexOf("path=", StringComparison.OrdinalIgnoreCase);
                if (pIdx == -1)
                {
                    await SendResponseAsync(stream, "text/plain", "Missing path", HttpStatusCode.BadRequest);
                    return;
                }

                string relativePath = Uri.UnescapeDataString(rawPath.Substring(pIdx + 5)).TrimStart('/', '\\');
                string retrobatRoot = GetRetrobatRoot();
                
                string fullPath;
                if (System.IO.Path.IsPathRooted(relativePath) || (relativePath.Length >= 2 && relativePath[1] == ':')) 
                {
                    fullPath = relativePath;
                }
                else 
                {
                    // Convert back slashes if necessary
                    relativePath = relativePath.Replace("/", "\\");
                    fullPath = Path.Combine(retrobatRoot, "roms", relativePath);
                }

                if (!File.Exists(fullPath))
                {
                    // EN: If not found physically, always try to proxy to ES API (localhost:1234)
                    // FR: Si non trouvé physiquement, toujours tenter de proxier vers l'API ES
                    try
                    {
                        string esPath = relativePath.Replace("\\", "/");
                        _logger.LogInfo($"[ES Media Proxy] File not found physically, trying ES API: {esPath}");

                        using (var hc = new HttpClient())
                        {
                            hc.Timeout = TimeSpan.FromSeconds(30);
                            
                            // EN: Forward Range header if present / FR: Transférer l'en-tête Range si présent
                            if (!string.IsNullOrEmpty(rangeHeader))
                                hc.DefaultRequestHeaders.TryAddWithoutValidation("Range", rangeHeader);

                            var esResponse = await hc.GetAsync($"http://127.0.0.1:1234/media?path={Uri.EscapeDataString(esPath)}", HttpCompletionOption.ResponseHeadersRead);
                            
                            string esContentType = esResponse.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
                            byte[] esData = await esResponse.Content.ReadAsByteArrayAsync();

                            if (esResponse.StatusCode == HttpStatusCode.PartialContent)
                            {
                                string? esCR = esResponse.Content.Headers.Contains("Content-Range") ? esResponse.Content.Headers.GetValues("Content-Range").FirstOrDefault() : null;
                                await SendRawResponseAsync(stream, esContentType, esData, HttpStatusCode.PartialContent, esCR);
                                return;
                            }
                            else if (esResponse.IsSuccessStatusCode)
                            {
                                await SendRawResponseAsync(stream, esContentType, esData);
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[ES Media Proxy] Failed for {relativePath}", ex);
                    }

                    _logger.LogInfo($"[ES Media 404] Not found: {fullPath}");
                    await SendResponseAsync(stream, "text/plain", "File not found", HttpStatusCode.NotFound);
                    return;
                }

                string ext = Path.GetExtension(fullPath).ToLower();
                string contentType = "application/octet-stream";
                if (ext == ".png") contentType = "image/png";
                else if (ext == ".jpg" || ext == ".jpeg") contentType = "image/jpeg";
                else if (ext == ".mp4") contentType = "video/mp4";
                else if (ext == ".pdf") contentType = "application/pdf";

                byte[] data = await File.ReadAllBytesAsync(fullPath);
                await SendRawResponseAsync(stream, contentType, data, HttpStatusCode.OK, null, rangeHeader);
            }
            catch (Exception ex)
            {
                await SendResponseAsync(stream, "text/plain", ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        private string GetRetrobatRoot()
        {
            if (_retrobatRoot != null) return _retrobatRoot;
            var config = new IniFile(Path.Combine(AppContext.BaseDirectory, "config.ini"));
            var service = new RetroBatService(_logger, config);
            string exePath = service.GetRetrobatPath();
            _retrobatRoot = Path.GetDirectoryName(exePath) ?? "";
            return _retrobatRoot;
        }

        // EN: Debug endpoint - call /api/es/debug?system=mastersystem to inspect gamelist paths
        // FR: Endpoint de diagnostic - appeler /api/es/debug?system=mastersystem pour inspecter les chemins
        private async Task HandleEsDebugAsync(Stream stream, string rawPath)
        {
            try
            {
                int sIdx = rawPath.IndexOf("system=", StringComparison.OrdinalIgnoreCase);
                string system = sIdx != -1 ? Uri.UnescapeDataString(rawPath.Substring(sIdx + 7).Split('&')[0]) : "mastersystem";
                string folderName = EmulationStationScraper.GetRomFolderName(system);

                string romsRoot = Path.Combine(GetRetrobatRoot(), "roms");
                string gamelistPath = Path.Combine(romsRoot, folderName, "gamelist.xml");

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"=== ES Debug for system: {system} ===");
                sb.AppendLine($"Folder    : {folderName}");
                sb.AppendLine($"Roms root : {romsRoot}");
                sb.AppendLine($"Gamelist  : {gamelistPath}");
                sb.AppendLine($"Exists    : {File.Exists(gamelistPath)}");
                sb.AppendLine("");

                if (File.Exists(gamelistPath))
                {
                    // [BATRUN-FIX]: Force UTF-8 encoding
                    XDocument doc;
                    using (var sr = new StreamReader(gamelistPath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                    {
                        doc = XDocument.Load(sr);
                    }
                    var games = doc.Descendants("game").Take(10);
                    foreach (var g in games)
                    {
                        string rawImg = g.Element("image")?.Value ?? "";
                        string img = rawImg.Replace("\\", "/").TrimStart('.', '/');
                        string imgFull = Path.Combine(romsRoot, folderName, img.Replace("/", "\\"));

                        sb.AppendLine($"Game  : {g.Element("name")?.Value}");
                        sb.AppendLine($"  Path   : {g.Element("path")?.Value}");
                        sb.AppendLine($"  Image  : {rawImg}  =>  {imgFull}  [exists={File.Exists(imgFull)}]");
                        sb.AppendLine($"  Thumb  : {g.Element("thumbnail")?.Value}");
                        sb.AppendLine($"  Video  : {g.Element("video")?.Value}");
                        sb.AppendLine($"  Manual : {g.Element("manual")?.Value}");
                        sb.AppendLine("");
                    }
                }

                await SendResponseAsync(stream, "text/plain; charset=utf-8", sb.ToString());
            }
            catch (Exception ex)
            {
                await SendResponseAsync(stream, "text/plain", $"Debug error: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        private async Task SendRawResponseAsync(Stream stream, string contentType, byte[] data, HttpStatusCode statusCode = HttpStatusCode.OK, string? contentRange = null, string? rangeHeader = null)
        {
            try
            {
                // EN: Handle Range Requests if specified and not already handled by upstream
                // FR: Gérer les Range Requests si spécifiées et non déjà gérées en amont
                if (statusCode == HttpStatusCode.OK && !string.IsNullOrEmpty(rangeHeader) && rangeHeader.StartsWith("bytes="))
                {
                    string range = rangeHeader.Substring(6);
                    string[] parts = range.Split('-');
                    if (parts.Length == 2)
                    {
                        long total = data.Length;
                        long start = 0;
                        long end = total - 1;

                        if (long.TryParse(parts[0], out long s)) start = s;
                        if (long.TryParse(parts[1], out long e)) end = e;

                        if (start < 0) start = 0;
                        if (end >= total) end = total - 1;

                        if (start <= end)
                        {
                            long length = end - start + 1;
                            byte[] slice = new byte[length];
                            Array.Copy(data, (int)start, slice, 0, (int)length);
                            
                            data = slice;
                            statusCode = HttpStatusCode.PartialContent;
                            contentRange = $"bytes {start}-{end}/{total}";
                        }
                    }
                }

                string header = $"HTTP/1.1 {(int)statusCode} {statusCode}\r\n" +
                                $"Content-Type: {contentType}\r\n" +
                                $"Content-Length: {data.Length}\r\n" +
                                $"Date: {DateTime.UtcNow:R}\r\n" +
                                $"Access-Control-Allow-Origin: *\r\n" +
                                "Permissions-Policy: gamepad=(self), autoplay=(self), fullscreen=(self)\r\n" +
                                "Accept-Ranges: bytes\r\n";

                // EN: Enable browser caching for media files (boxart, video) to speed up list loading
                // FR: Activer le cache navigateur pour les médias (boxart, vidéo) pour accélérer le chargement
                if (contentType.StartsWith("image/") || contentType.StartsWith("video/"))
                {
                    header += "Cache-Control: public, max-age=3600\r\n";
                }
                else
                {
                    header += "Cache-Control: no-cache, no-store, must-revalidate\r\n";
                }

                if (!string.IsNullOrEmpty(contentRange))
                    header += $"Content-Range: {contentRange}\r\n";

                header += "Connection: keep-alive\r\n" +
                          "Keep-Alive: timeout=5, max=100\r\n\r\n";

                byte[] headerBytes = Encoding.UTF8.GetBytes(header);
                await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
            }
            catch (Exception ex)
            {
                // EN: Ignore client disconnections (normal behavior when navigating away or closing tabs)
                // FR: Ignorer les déconnexions clients (comportement normal lors d'un changement de page ou fermeture d'onglet)
                if (ex is IOException || ex is SocketException || (ex.InnerException is SocketException se && (se.SocketErrorCode == SocketError.ConnectionReset || se.SocketErrorCode == SocketError.ConnectionAborted)))
                {
                    return;
                }
                _logger.LogError("Error in SendRawResponseAsync", ex);
            }
        }

        /// <summary>
        /// EN: Proxy all /ml/* requests to the local Moonlight Web instance on port 8080.
        ///     HTTP requests are forwarded via HttpClient.
        ///     WebSocket upgrade requests are bridged via a raw TCP tunnel.
        /// FR: Proxifie toutes les requêtes /ml/* vers l'instance Moonlight Web locale sur le port 8080.
        ///     Les requêtes HTTP sont transmises via HttpClient.
        ///     Les upgrades WebSocket sont pontés via un tunnel TCP brut.
        /// </summary>
        private async Task HandleMoonlightProxyAsync(TcpClient client, Stream clientStream, string method, string rawPath, Dictionary<string, string> headers, int contentLength, string? upgradeHeader)
        {
            const int moonlightPort = 8080;
            string moonlightBase = $"http://127.0.0.1:{moonlightPort}";

            // EN: Keep the /ml prefix as Moonlight Web is configured with url_path_prefix = "/ml"
            // FR: Conserver le préfixe /ml car Moonlight Web est configuré avec url_path_prefix = "/ml"
            string mlPath = rawPath;

            try
            {
                bool isWebSocket = string.Equals(upgradeHeader, "websocket", StringComparison.OrdinalIgnoreCase);

                if (isWebSocket)
                {
                    // EN: WebSocket upgrade: open raw TCP connection to Moonlight and bridge
                    // FR: Upgrade WebSocket: ouvrir connexion TCP brute vers Moonlight et ponter
                    _logger.LogInfo($"[ML-Proxy] WS bridge → 127.0.0.1:{moonlightPort}{mlPath}");
                    using (TcpClient moonlightClient = new TcpClient())
                    {
                        moonlightClient.NoDelay = true;
                        client.NoDelay = true;
                        
                        // EN: Use large buffers to support video traffic if WebRTC fails and falls back to WebSocket
                        // FR: Utiliser de larges buffers pour supporter le flux vidéo si le WebRTC échoue et repasse en WebSocket
                        moonlightClient.SendBufferSize = 1024 * 1024; 
                        moonlightClient.ReceiveBufferSize = 1024 * 1024;
                        client.SendBufferSize = 1024 * 1024;
                        client.ReceiveBufferSize = 1024 * 1024;

                        await moonlightClient.ConnectAsync("127.0.0.1", moonlightPort);
                        using (Stream moonlightStream = moonlightClient.GetStream())
                        {
                            // EN: Rebuild the original request with ALL headers
                            // FR: Reconstruire la requête originale avec TOUS les en-têtes
                            var sb = new StringBuilder();
                            sb.Append($"{method} {mlPath} HTTP/1.1\r\n");
                            sb.Append($"Host: 127.0.0.1:{moonlightPort}\r\n");
                            foreach (var h in headers)
                            {
                                if (h.Key.Equals("Host", StringComparison.OrdinalIgnoreCase)) continue;
                                // EN: Prevent client from overriding our injected auth header
                                if (h.Key.Equals("X-BatRun-User", StringComparison.OrdinalIgnoreCase)) continue;
                                sb.Append($"{h.Key}: {h.Value}\r\n");
                            }
                            // EN: Inject auth header for reverse proxy bypass
                            sb.Append($"X-BatRun-User: {_moonlight.ServiceUser}\r\n");
                            sb.Append("\r\n");
                            
                            byte[] reqBytes = Encoding.ASCII.GetBytes(sb.ToString());
                            await moonlightStream.WriteAsync(reqBytes, 0, reqBytes.Length);

                            // EN: Bridge bidirectional traffic using optimized buffer size for signaling (64KB)
                            // FR: Ponter le trafic bidirectionnel avec une taille de buffer optimisée pour la signalisation (64KB)
                            _logger.LogInfo($"[ML-Proxy] Bridge started for {mlPath}. Buffer: 64KB");

                            using (var cts = new CancellationTokenSource())
                            {
                                // EN: Use 1MB buffers to support high-bitrate video traffic in WebSocket mode
                                // FR: Utiliser des buffers de 1Mo pour supporter le flux vidéo haut débit en mode WebSocket
                                var t1 = moonlightStream.CopyToAsync(clientStream, 1048576, cts.Token);
                                var t2 = clientStream.CopyToAsync(moonlightStream, 1048576, cts.Token);

                                Task winner = await Task.WhenAny(t1, t2);
                                cts.Cancel(); 
                                
                                if (winner == t1)
                                    _logger.LogInfo("[ML-Proxy] Bridge closed: Moonlight (Server) finished first.");
                                else
                                    _logger.LogInfo("[ML-Proxy] Bridge closed: Client (Browser) finished first.");

                                if (winner.IsFaulted) 
                                    _logger.LogWarning($"[ML-Proxy] Bridge error: {winner.Exception?.InnerException?.Message ?? winner.Exception?.Message ?? "Unknown"}");
                            }
                        }
                    }
                }
                else
                {
                    // EN: Regular HTTP proxy
                    // FR: Proxy HTTP classique
                    string targetUrl = moonlightBase + mlPath;
                    _logger.LogInfo($"[ML-Proxy] {method} {targetUrl}");

                    using (var httpClient = new HttpClient())
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(30);
                        HttpRequestMessage req = new HttpRequestMessage(new HttpMethod(method), targetUrl);

                        // Forward headers
                        foreach (var h in headers)
                        {
                            if (h.Key.Equals("Host", StringComparison.OrdinalIgnoreCase)) continue;
                            if (h.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) continue;
                            if (h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)) continue;
                            if (h.Key.Equals("X-BatRun-User", StringComparison.OrdinalIgnoreCase)) continue;
                            if (h.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase)) continue;
                            
                            req.Headers.TryAddWithoutValidation(h.Key, h.Value);
                        }

                        // EN: Keep connection warm to reduce latency on external links
                        // FR: Garder la connexion active pour réduire la latence sur les liens externes
                        req.Headers.TryAddWithoutValidation("Connection", "keep-alive");
                        req.Headers.TryAddWithoutValidation("Keep-Alive", "timeout=20, max=1000");

                        // EN: Inject auth header for reverse proxy bypass
                        req.Headers.TryAddWithoutValidation("X-BatRun-User", _moonlight.ServiceUser);

                        if (contentLength > 0)
                        {
                            // EN: Note: If StreamReader buffered some body bytes, they are lost here.
                            // For Moonlight Web static files (GET), this is usually not an issue.
                            byte[] buf = new byte[contentLength];
                            int totalRead = 0;
                            while (totalRead < contentLength)
                            {
                                int r = await clientStream.ReadAsync(buf, totalRead, contentLength - totalRead);
                                if (r == 0) break;
                                totalRead += r;
                            }
                            req.Content = new ByteArrayContent(buf);
                            if (headers.TryGetValue("Content-Type", out string? ct))
                                req.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(ct);
                        }

                        HttpResponseMessage resp = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
                        string contentType = resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                        bool isText = contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase) || 
                                     contentType.Contains("text/", StringComparison.OrdinalIgnoreCase);

                        if (!isText)
                        {
                            byte[] respBytes = await resp.Content.ReadAsByteArrayAsync();
                            await SendRawResponseAsync(clientStream, contentType, respBytes, resp.StatusCode);
                        }
                        else
                        {
                            string remoteContent = await resp.Content.ReadAsStringAsync();
                            await SendResponseAsync(clientStream, contentType, remoteContent, resp.StatusCode);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ML-Proxy] Error proxying {rawPath}", ex);
                try { await SendResponseAsync(clientStream, "text/plain", "Proxy Error", System.Net.HttpStatusCode.BadGateway); } catch { }
            }
    }

    /// <summary>
    /// [BATRUN-HUB]: Prefix-based reverse proxy for remote Moonlight Web streaming.
    /// Pattern: /relay/{targetAlias}/* → http://{targetIp}:8080[/ml]/*
    /// Unlike the query-param relay (/api/relay?target=X&path=Y), this prefix-based approach
    /// ensures that relative asset paths in stream.html and its JS modules resolve naturally
    /// through the /relay/{alias}/ prefix, preventing 404s on static assets.
    /// The WebSocket upgrade is also handled: /relay/{alias}/api/host/stream → WS bridge.
    /// The config.js path_prefix is overridden from "/ml" to "/relay/{alias}" so that
    /// the moonlight-web-stream JS client constructs all URLs (including WS) through the relay.
    /// FR: Proxy inverse par préfixe pour le streaming Moonlight Web distant.
    /// </summary>
    private async Task HandleMoonlightRelayProxyAsync(TcpClient client, Stream clientStream, string method, string rawPath, Dictionary<string, string> headers, int contentLength, string? upgradeHeader, string targetAlias)
    {
        int moonlightPort = _moonlight.Port;

        string? resolvedTarget = _manager.ResolveMachineAlias(targetAlias, out string? moonlightHost);
        if (resolvedTarget == null || string.IsNullOrEmpty(moonlightHost))
        {
            _logger.LogWarning($"[Relay-ML] Cannot resolve target alias '{targetAlias}'");
            await SendResponseAsync(clientStream, "text/plain", $"Unknown target: {targetAlias}", HttpStatusCode.NotFound);
            return;
        }

        string targetIp = moonlightHost;

        // Strip the /relay/{targetAlias}/ prefix to get the path on the remote Moonlight server
        // e.g. /relay/RETROBAT/stream.html?foo=bar → /stream.html?foo=bar
        // e.g. /relay/RETROBAT/api/host/stream → /api/host/stream
        // e.g. /relay/RETROBAT/config.js → /config.js
        string prefix = $"/relay/{targetAlias}";
        string remotePath = rawPath;
        int prefixIdx = rawPath.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (prefixIdx >= 0)
        {
            remotePath = rawPath.Substring(prefixIdx + prefix.Length);
            if (string.IsNullOrEmpty(remotePath) || remotePath == "") remotePath = "/";
        }

        // [BATRUN-HUB-FIX]: Detect the remote target's Moonlight path prefix.
        // If the remote BatRun has ProxyMoonlight enabled, its Moonlight Web routes are
        // under /ml/ (e.g. /ml/stream.html, /ml/config.js). We must prepend /ml when
        // forwarding. We probe once and cache the result per target.
        // FR: Détecter le préfixe de chemin Moonlight de la cible distante.
        string? remoteMlPrefix;
        lock (_relayTargetPrefixCache)
        {
            if (!_relayTargetPrefixCache.TryGetValue(targetAlias, out var cachedPrefix))
                remoteMlPrefix = null;
            else
                remoteMlPrefix = cachedPrefix;
        }

        if (remoteMlPrefix == null)
        {
            remoteMlPrefix = await DetectRemoteMoonlightPrefixAsync(targetIp, moonlightPort);
            lock (_relayTargetPrefixCache)
            {
                _relayTargetPrefixCache[targetAlias] = remoteMlPrefix;
            }
        }

        // Build the actual remote path with the /ml prefix if needed
        string effectiveRemotePath = remoteMlPrefix + remotePath;

        _logger.LogInfo($"[Relay-ML] {method} /relay/{targetAlias}{remotePath} → http://{targetIp}:{moonlightPort}{effectiveRemotePath}");

        try
        {
            bool isWebSocket = string.Equals(upgradeHeader, "websocket", StringComparison.OrdinalIgnoreCase);

            if (isWebSocket)
            {
                string wsPath = effectiveRemotePath;
                if (string.IsNullOrEmpty(wsPath) || wsPath == "/") wsPath = remoteMlPrefix + "/api/host/stream";

                _logger.LogInfo($"[Relay-ML] WS bridge → {targetIp}:{moonlightPort}{wsPath}");
                using (TcpClient targetClient = new TcpClient())
                {
                    targetClient.NoDelay = true;
                    client.NoDelay = true;
                    targetClient.SendBufferSize = 1024 * 1024;
                    targetClient.ReceiveBufferSize = 1024 * 1024;
                    client.SendBufferSize = 1024 * 1024;
                    client.ReceiveBufferSize = 1024 * 1024;
                    targetClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                    await targetClient.ConnectAsync(targetIp, moonlightPort);
                    using (NetworkStream targetStream = targetClient.GetStream())
                    {
                        var sb = new StringBuilder();
                        sb.Append($"{method} {wsPath} HTTP/1.1\r\n");
                        sb.Append($"Host: {targetIp}:{moonlightPort}\r\n");
                        sb.Append($"X-BatRun-User: {_moonlight.ServiceUser}\r\n");
                        foreach (var h in headers)
                        {
                            if (h.Key.Equals("Host", StringComparison.OrdinalIgnoreCase)) continue;
                            if (h.Key.Equals("X-BatRun-User", StringComparison.OrdinalIgnoreCase)) continue;
                            if (h.Key.Equals("Origin", StringComparison.OrdinalIgnoreCase))
                            {
                                sb.Append($"Origin: http://{targetIp}:{moonlightPort}\r\n");
                                continue;
                            }
                            sb.Append($"{h.Key}: {h.Value}\r\n");
                        }
                        sb.Append("\r\n");

                        byte[] reqBytes = Encoding.ASCII.GetBytes(sb.ToString());
                        await targetStream.WriteAsync(reqBytes, 0, reqBytes.Length);

                        _logger.LogInfo($"[Relay-ML] WS bridge started for {wsPath} (target={targetAlias})");

                        var t1 = Task.Run(async () => {
                            byte[] buffer = new byte[65536];
                            try { while (true) { int read = await targetStream.ReadAsync(buffer, 0, buffer.Length); if (read <= 0) break; await clientStream.WriteAsync(buffer, 0, read); } } catch { }
                        });
                        var t2 = Task.Run(async () => {
                            byte[] buffer = new byte[65536];
                            try { while (true) { int read = await clientStream.ReadAsync(buffer, 0, buffer.Length); if (read <= 0) break; await targetStream.WriteAsync(buffer, 0, read); } } catch { }
                        });

                        Task winner = await Task.WhenAny(t1, t2);
                        if (winner == t1)
                            _logger.LogInfo($"[Relay-ML] Bridge closed: Target (Moonlight {targetAlias}) finished first.");
                        else
                            _logger.LogInfo("[Relay-ML] Bridge closed: Client (Browser) finished first.");

                        if (t1.IsFaulted) _logger.LogWarning($"[Relay-ML] TargetToClient error: {t1.Exception?.InnerException?.Message ?? "Unknown"}");
                        if (t2.IsFaulted) _logger.LogWarning($"[Relay-ML] ClientToTarget error: {t2.Exception?.InnerException?.Message ?? "Unknown"}");

                        _logger.LogInfo("[Relay-ML] WebSocket bridge closed.");
                        // EN: [BATRUN-FIX-SESSION] Try cancel on remote Sunshine, then conditionally restart local web-server
                        //     to clear stale session state if no active game or lobby is running.
                        // FR: [BATRUN-FIX-SESSION] Tenter l'annulation sur Sunshine distant, puis redémarrer le web-server
                        //     local sous condition si aucun jeu ou lobby n'est en cours.
                        _ = Task.Run(async () => {
                            try { await _manager.Moonlight.SendSunshineCancelAsync(); } catch { }
                            
                            bool isLobbyActive = false;
                            lock (_lobbyLock)
                            {
                                isLobbyActive = _lobby.Phase != "none";
                            }
                            
                            if (!_manager.IsGameInProgress && !isLobbyActive)
                            {
                                // EN: Restart both local Moonlight web-server and BatRun API services
                                // FR: Redémarrer à la fois le serveur web Moonlight local et les services d'API BatRun
                                await _manager.Moonlight.RestartWebServerAsync();
                                try { _ = _manager.RestartArcadeApiServicesAsync(3000); } catch { }
                            }
                            else
                            {
                                _logger.LogInfo("[Relay-ML] WebSocket closed but game/lobby is still active. Postponing web-server restart to allow reconnection.");
                            }
                        });
                    }
                }
            }
            else
            {
                string targetUrl = $"http://{targetIp}:{moonlightPort}{effectiveRemotePath}";

                var relayHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                    UseCookies = false,
                    AllowAutoRedirect = false,
                    AutomaticDecompression = System.Net.DecompressionMethods.None
                };
                using (var httpClient = new HttpClient(relayHandler))
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(30);
            var req = new HttpRequestMessage(method == "POST" ? HttpMethod.Post : HttpMethod.Get, targetUrl);
            req.Headers.Add("X-BatRun-User", _moonlight.ServiceUser);

            // [BATRUN-HUB-FIX]: Forward Cookie header from browser to target Moonlight.
            // Without the session cookie, Moonlight endpoints like /api/authenticate and
            // /api/role return "unauthenticated", causing stream.html to bail out.
            // FR: Transmettre le cookie du navigateur vers le Moonlight cible.
            if (headers.TryGetValue("Cookie", out string? cookieVal) && !string.IsNullOrEmpty(cookieVal))
            {
                req.Headers.Add("Cookie", cookieVal);
                _logger.LogInfo($"[Relay-ML] Forwarded Cookie to {effectiveRemotePath} (len={cookieVal.Length})");
            }
            else
            {
                _logger.LogWarning($"[Relay-ML] No Cookie header on request for {effectiveRemotePath} — target may reject auth");
            }

            if (headers.TryGetValue("Range", out string? rangeVal))
                req.Headers.Add("Range", rangeVal);

                    if (method == "POST" && contentLength > 0)
                    {
                        req.Content = new StringContent("", Encoding.UTF8, "application/json");
                    }

                    using (var resp = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead))
                    {
                        string contentType = resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                        bool isText = contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
                                      contentType.Contains("text/", StringComparison.OrdinalIgnoreCase) ||
                                      contentType.Contains("javascript", StringComparison.OrdinalIgnoreCase);

                        if (!isText)
                        {
                            byte[] respBytes = await resp.Content.ReadAsByteArrayAsync();
                            string? contentRange = resp.Content.Headers.Contains("Content-Range") ? resp.Content.Headers.GetValues("Content-Range").FirstOrDefault() : null;
                            await SendRawResponseAsync(clientStream, contentType, respBytes, resp.StatusCode, contentRange);
                        }
                        else
                        {
                            string remoteContent = await resp.Content.ReadAsStringAsync();

                            // [BATRUN-HUB-FIX]: Override config.js path_prefix to route through relay.
                            // The moonlight-web-stream JS constructs all URLs as:
                            //   window.location.origin + CONFIG.path_prefix + path
                            // By replacing path_prefix with "/relay/{alias}", all asset loads
                            // and WebSocket connections route through the Hub's relay prefix.
                            // This is the ONLY robust way to make the Moonlight web client work
                            // through a reverse proxy without modifying its source code.
                            // FR: Remplacer path_prefix dans config.js pour router via le relay.
                            bool isConfigJs = remotePath.Equals("/config.js", StringComparison.OrdinalIgnoreCase) ||
                                              remotePath.StartsWith("/config.js?", StringComparison.OrdinalIgnoreCase);

                            if (isConfigJs)
                            {
                                string relayPrefix = $"/relay/{targetAlias}";
                                int ppIdx = remoteContent.IndexOf("\"path_prefix\"");
                                if (ppIdx >= 0)
                                {
                                    int colonIdx = remoteContent.IndexOf(':', ppIdx);
                                    if (colonIdx >= 0)
                                    {
                                        int valueStart = remoteContent.IndexOf('"', colonIdx) + 1;
                                        int valueEnd = remoteContent.IndexOf('"', valueStart);
                                        if (valueStart > 0 && valueEnd > valueStart)
                                        {
                                            remoteContent = remoteContent.Substring(0, valueStart) + relayPrefix + remoteContent.Substring(valueEnd);
                                        }
                                    }
                                }
                                _logger.LogInfo($"[Relay-ML] Overrode config.js path_prefix → '{relayPrefix}'");
                            }

                            await SendResponseAsync(clientStream, contentType, remoteContent, resp.StatusCode);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Relay-ML] Error proxying /relay/{targetAlias}{remotePath}", ex);
            try { await SendResponseAsync(clientStream, "text/plain", "Relay Proxy Error", System.Net.HttpStatusCode.BadGateway); } catch { }
        }
    }

    /// <summary>
    /// [BATRUN-HUB]: Probe the remote Moonlight Web server to detect if it uses /ml prefix.
    /// Tries /ml/config.js first, then /config.js. Returns "/ml" or "".
    /// FR: Sonder le serveur Moonlight Web distant pour détecter le préfixe /ml.
    /// </summary>
    private async Task<string> DetectRemoteMoonlightPrefixAsync(string targetIp, int moonlightPort)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
            UseCookies = false,
            AllowAutoRedirect = false,
            AutomaticDecompression = System.Net.DecompressionMethods.None
        };
        using (var probeClient = new HttpClient(handler))
        {
            probeClient.Timeout = TimeSpan.FromSeconds(3);
            try
            {
                var resp = await probeClient.GetAsync($"http://{targetIp}:{moonlightPort}/ml/config.js");
                if (resp.IsSuccessStatusCode)
                {
                    _logger.LogInfo($"[Relay-ML] Detected /ml prefix for {targetIp}:{moonlightPort}");
                    return "/ml";
                }
            }
            catch { }
            try
            {
                var resp = await probeClient.GetAsync($"http://{targetIp}:{moonlightPort}/config.js");
                if (resp.IsSuccessStatusCode)
                {
                    _logger.LogInfo($"[Relay-ML] Detected no prefix for {targetIp}:{moonlightPort}");
                    return "";
                }
            }
            catch { }
            _logger.LogWarning($"[Relay-ML] Could not probe Moonlight prefix for {targetIp}:{moonlightPort}, defaulting to no prefix");
            return "";
        }
    }

    private async Task HandleMoonlightAuthAsync(Stream stream, string rawPath, Dictionary<string, string> headers, IPAddress clientIp)
        {
            try
            {
                var queryParams = ParseQueryString(rawPath);
                string? token = queryParams.GetValueOrDefault("token");
                string? appId = queryParams.GetValueOrDefault("appId");
                string? hostId = queryParams.GetValueOrDefault("hostId", "1");
                string? targetIp = queryParams.GetValueOrDefault("targetIp");
        
                // [BATRUN-HUB]: Resolve machine alias for Moonlight auth relay
                // EN: targetIp may now be a machine name alias — resolve it to IP:port
                // FR: targetIp peut maintenant être un alias de machine — le résoudre en IP:port
                string? resolvedTargetIp = targetIp;
                if (!string.IsNullOrEmpty(targetIp))
                {
                    string? resolved = _manager.ResolveMachineAlias(targetIp, out string? _);
                    if (resolved != null)
                        resolvedTargetIp = resolved;
                    else
                        _logger.LogWarning($"[Moonlight-Auth] Cannot resolve targetIp alias '{targetIp}'");
                }
        
            // [BATRUN-MOD]: Remote Token Validation (Fix 401 in Relay Mode)
            // [BATRUN-HUB-FIX]: Use GET with query param instead of POST with JSON body.
            // The target's /api/public/status reads token from query string or cookie,
            // NOT from a JSON POST body. Using POST always sent an empty token → 401.
            // FR: Utiliser GET avec paramètre de requête au lieu de POST avec corps JSON.
            bool isTokenValid = false;
            if (!string.IsNullOrEmpty(resolvedTargetIp) && !resolvedTargetIp.Contains("127.0.0.1"))
            {
                _logger.LogInfo($"[Moonlight] Relay: Verifying token with remote target {resolvedTargetIp} (alias={targetIp})...");
                try {
                var relayHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                    UseCookies = false,
                    AllowAutoRedirect = false,
                    AutomaticDecompression = System.Net.DecompressionMethods.None
                };
                string relayProtocol = (_serverCertificate != null) ? "https" : "http";
                using (var httpClient = new HttpClient(relayHandler)) {
                    httpClient.Timeout = TimeSpan.FromSeconds(5);
                    var response = await httpClient.GetAsync($"{relayProtocol}://{resolvedTargetIp}/api/public/status?token={Uri.EscapeDataString(token ?? "")}");
                    isTokenValid = response.IsSuccessStatusCode;
                    }
                } catch (Exception ex) {
                    _logger.LogWarning($"[Moonlight] Token verification failed for {resolvedTargetIp}: {ex.Message}");
                }
            }
            else
            {
                isTokenValid = _manager.PublicUserManager.ValidateToken(token ?? "");
            }

            if (!isTokenValid)
            {
                _logger.LogWarning("[Moonlight] Auth attempt with invalid token (potentially from wrong machine).");
                await SendResponseAsync(stream, "text/plain", "Unauthorized", HttpStatusCode.Unauthorized);
                return;
            }

            _logger.LogInfo($"[Moonlight] Bridging session with system user '{_moonlight.ServiceUser}' (Target hostId='{hostId}', appId='{appId}')...");

            // [BATRUN-HUB-FIX]: Obtain a valid session cookie from the TARGET's Moonlight instance,
            // not the local one. The local cookie is meaningless on the remote target.
            // FR: Obtenir un cookie de session valide depuis l'instance Moonlight de la CIBLE,
            // pas la locale. Le cookie local n'a pas de sens sur la cible distante.
string? mlCookie = null;
            string? remoteRedirectLocation = null;
            if (!string.IsNullOrEmpty(resolvedTargetIp) && !resolvedTargetIp.Contains("127.0.0.1"))
            {
                try {
                    _logger.LogInfo($"[Moonlight] Getting session cookie from remote target {resolvedTargetIp}...");
                    var remoteHandler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                        UseCookies = false,
                        AllowAutoRedirect = false,
                        AutomaticDecompression = System.Net.DecompressionMethods.None
                    };
                    string relayProtocol = (_serverCertificate != null) ? "https" : "http";
                    using (var remoteClient = new HttpClient(remoteHandler))
                    {
                        remoteClient.Timeout = TimeSpan.FromSeconds(10);
                        var cookieRequest = new HttpRequestMessage(HttpMethod.Get, $"{relayProtocol}://{resolvedTargetIp}/api/moonlight-auth?token={Uri.EscapeDataString(token ?? "")}&hostId={hostId}&appId={appId}");
                        using (var cookieResponse = await remoteClient.SendAsync(cookieRequest, HttpCompletionOption.ResponseHeadersRead))
                        {
                            if (cookieResponse.Headers.TryGetValues("Set-Cookie", out var cookies))
                            {
                                mlCookie = cookies.FirstOrDefault();
                            }
                            if (string.IsNullOrEmpty(mlCookie) && cookieResponse.Content.Headers.TryGetValues("Set-Cookie", out var contentCookies))
                            {
                                mlCookie = contentCookies.FirstOrDefault();
                            }
                            if (cookieResponse.StatusCode == System.Net.HttpStatusCode.Redirect || cookieResponse.StatusCode == System.Net.HttpStatusCode.Found)
                            {
                                var location = cookieResponse.Headers.Location;
                                remoteRedirectLocation = location?.OriginalString;
                                _logger.LogInfo($"[Moonlight] Remote target redirected to: {remoteRedirectLocation}");
                                if (location != null)
                                {
                                    var remoteQuery = ParseQueryString(location.Query);
                                    if (remoteQuery.TryGetValue("hostId", out string? rHostId) && !string.IsNullOrEmpty(rHostId))
                                    {
                                        hostId = rHostId;
                                        _logger.LogInfo($"[Moonlight] Extracted hostId='{hostId}' from remote redirect.");
                                    }
                                    if (remoteQuery.TryGetValue("appId", out string? rAppId) && !string.IsNullOrEmpty(rAppId))
                                    {
                                        appId = rAppId;
                                        _logger.LogInfo($"[Moonlight] Extracted appId='{appId}' from remote redirect.");
                                    }
                                }
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(mlCookie))
                    {
                        _logger.LogWarning($"[Moonlight] Could not obtain session cookie from remote target {resolvedTargetIp}. Falling back to local cookie.");
                        mlCookie = await _moonlight.GetSessionCookieAsync();
                    }
                } catch (Exception ex) {
                    _logger.LogWarning($"[Moonlight] Failed to get remote session cookie: {ex.Message}. Falling back to local cookie.");
                    mlCookie = await _moonlight.GetSessionCookieAsync();
                }
            }
            else
            {
                mlCookie = await _moonlight.GetSessionCookieAsync();
            }

            if (string.IsNullOrEmpty(mlCookie))
                {
                    _logger.LogWarning("[Moonlight] FAILED to obtain session cookie. Auto-login will not work. Please check if 'batrun_service' exists in data.json.");
                    // EN: If we can't get a cookie, the bridge will fail. Send an error page to explain.
                    await SendResponseAsync(stream, "text/html", $@"
                        <html><body style='background:#1a1a1a;color:#ff4444;text-align:center;font-family:sans-serif;padding-top:50px;'>
                        <h2>Moonlight Session Error</h2>
                        <p>Impossible de r&eacute;cup&eacute;rer le cookie de session (Utilisateur non trouv&eacute; ou serveur non pr&ecirc;t).</p>
                        <p style='color:#888;'>V&eacute;rifiez les logs de BatRun et le dossier .moonlight-web-stream.</p>
                        <button onclick='window.location.reload()' style='padding:10px 20px;'>R&eacute;essayer</button>
                        </body></html>", HttpStatusCode.ServiceUnavailable);
                    return;
                }
                else
                {
                    _logger.LogInfo("[Moonlight] Session cookie obtained successfully via internal login.");
                }

        // [BATRUN-HUB-FIX]: In relay mode, hostId/appId were already extracted from the
        // remote target's 302 redirect above. Only read local config for local Moonlight.
        // FR: En mode relay, hostId/appId ont déjà été extraits de la redirection 302
        // de la cible distante ci-dessus. Ne lire la config locale que pour Moonlight local.
        bool isRelayMode = !string.IsNullOrEmpty(resolvedTargetIp) && !resolvedTargetIp.Contains("127.0.0.1") && remoteRedirectLocation != null;
        if (!isRelayMode)
        {
            hostId = _manager.Config.ReadValue("Arcade", "MoonlightHostId", "auto").Trim();
            if (string.IsNullOrEmpty(hostId) || hostId == "auto") hostId = "1";
            _logger.LogInfo($"[Moonlight] Using manual hostId='{hostId}' from config.");

            appId = _manager.Config.ReadValue("Arcade", "MoonlightAppId", "1").Trim();
            if (string.IsNullOrEmpty(appId) || appId == "0") appId = "1";
            _logger.LogInfo($"[Moonlight] Using manual appId='{appId}' from config.");
        }
        else
        {
            _logger.LogInfo($"[Moonlight] Relay mode: using hostId='{hostId}', appId='{appId}' from remote target redirect.");
        }


                // EN: Determine redirect host - use local IP if client is on local network, otherwise use incoming Host header
                // FR: Déterminer l'hôte de redirection - utiliser l'IP locale si le client est sur le réseau local, sinon utiliser le header Host
                string host = Environment.MachineName;
                
                // EN: Check if client is on local network - if so, use local IP instead of external Host header
                // FR: Vérifier si le client est sur le réseau local - si oui, utiliser l'IP locale au lieu du header Host externe
                bool isClientLocal = IsLocalOrAllowedIp(clientIp);
                
                if (isClientLocal)
                {
                    // EN: Client is local, use local IP of this hub machine
                    // FR: Le client est local, utiliser l'IP locale de cette machine hub
                    string localIp = _manager.GetLocalIPAddress();
                    _logger.LogInfo($"[Moonlight] Client {clientIp} is local, using local IP {localIp} for redirect.");
                    host = localIp;
                }
                else
                {
                    // EN: Client is external, use incoming Host header (may be public IP)
                    // FR: Le client est externe, utiliser le header Host (peut être l'IP publique)
                    if (headers.TryGetValue("Host", out string? incomingHost) && !string.IsNullOrEmpty(incomingHost))
                    {
                        // EN: Host header often includes the port (e.g., ip:4321), extract just the IP/name
                        int colonIdx = incomingHost.IndexOf(':');
                        if (colonIdx > 0)
                            host = incomingHost.Substring(0, colonIdx);
                        else
                            host = incomingHost;
                    }
                    _logger.LogInfo($"[Moonlight] Client {clientIp} is external, using Host header {host} for redirect.");
                }
                
                string protocol = (_serverCertificate != null) ? "https" : "http";
                
            // [BATRUN-MOD]: Handle Relay Mode for Moonlight Web
            // [BATRUN-HUB-FIX]: Use prefix-based relay (/stream-relay/{alias}/stream.html) instead of
            // query-param relay (/api/relay?target=X&path=/stream.html). The prefix-based approach
            // ensures that all relative asset paths in stream.html and its JS modules resolve
            // through the /stream-relay/{alias}/ prefix naturally, preventing 404s on static assets.
            // FR: Utiliser le relay par préfixe au lieu du relay par paramètre de requête.
            string targetUrl;
            var qParams = ParseQueryString(rawPath);

            if (qParams.TryGetValue("targetIp", out string? tIp) && !string.IsNullOrEmpty(tIp))
            {
                string relayTarget = tIp;
                targetUrl = $"{protocol}://{host}:{_port}/stream-relay/{Uri.EscapeDataString(relayTarget)}/stream.html?arcadeMode=1&hostId={hostId}&appId={appId}";
            }
            else
                {
                    if (_manager.ProxyMoonlight)
                    {
                        // EN: Proxy mode: serve Moonlight via /ml/ to avoid exposing port 8080
                        // FR: Mode proxy: servir Moonlight via /ml/ pour éviter d'exposer le port 8080
                        targetUrl = $"{protocol}://{host}:{_port}/ml/stream.html?hostId={hostId}&appId={appId}&arcadeMode=1";
                        _logger.LogInfo("[Moonlight] ProxyMoonlight ON: redirecting through /ml/ instead of direct port 8080.");
                    }
                    else
                    {
                        // Standard direct access (local) on port 8080 (Always HTTP for direct access)
                        targetUrl = $"http://{host}:{_moonlight.Port}/stream.html?hostId={hostId}&appId={appId}&arcadeMode=1";
                    }
                }

                _logger.LogInfo($"[Moonlight] Bridge: hostId={hostId}, appId={appId}. URL: {targetUrl}");

                _logger.LogInfo($"[Moonlight] Auth success. Redirecting to: {targetUrl}");
                string cleanCookie = mlCookie?.Split(';').FirstOrDefault() ?? mlCookie ?? "";
                await SendRedirectWithCookieAsync(stream, targetUrl, cleanCookie);
            }
            catch (Exception ex)
            {
                _logger.LogError("Moonlight Auth Error", ex);
                await SendResponseAsync(stream, "text/plain", "Auth Error", HttpStatusCode.InternalServerError);
            }
        }

        private async Task HandleRelayAsync(Stream stream, string method, string rawPath, string body, string? rangeHeader = null, Dictionary<string, string>? requestHeaders = null)
        {
            string target = "";
            string remotePath = "";
            // EN: Use incoming Host header so external clients get a reachable WS URL (critical for hub mode)
            // FR: Utiliser le header Host entrant pour que les clients externes obtiennent une URL WS accessible (critique pour le mode hub)
            string targetHostForWs = Environment.MachineName;
            if (requestHeaders != null && requestHeaders.TryGetValue("Host", out string? hostVal) && !string.IsNullOrEmpty(hostVal))
            {
                int colonIdx = hostVal.IndexOf(':');
                targetHostForWs = colonIdx > 0 ? hostVal.Substring(0, colonIdx) : hostVal;
            }

            try
            {
                bool isPrefixRelay = rawPath.StartsWith("/stream-relay/");
                if (isPrefixRelay)
                {
                    // Expected format: /stream-relay/{alias}/{path}
                    string[] parts = rawPath.Split(new[] { '/' }, 4);
                    if (parts.Length >= 4)
                    {
                        target = Uri.UnescapeDataString(parts[2]);
                        remotePath = "/" + parts[3];
                    }
                }
                else
                {
                    // EN: Robust parsing of query parameters
                    // FR: Analyse robuste des paramètres de requête
                    var queryParams = ParseQueryString(rawPath);
                    
                    if (queryParams.TryGetValue("target", out string? t)) target = t;
                    if (queryParams.TryGetValue("path", out string? p)) remotePath = p;
                }
    
                if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(remotePath))
                {
                    _logger.LogWarning($"Relay: Missing target or path in '{rawPath}'");
                    await SendResponseAsync(stream, "text/plain", "Missing target or path", HttpStatusCode.BadRequest);
                    return;
                }
    
                // [BATRUN-MOD]: Remote Request Guard (Fix 500 Internal Error)
                // EN: Prevent nested relay or invalid hosts that cause Uri constructor to crash.
                if (target.Contains("/") || target.Contains("?") || target.Contains("api/relay"))
                {
                    _logger.LogWarning($"[Relay] Blocked invalid/nested target: {target}");
                    await SendResponseAsync(stream, "text/plain", "Bad Request: Invalid or nested target host", HttpStatusCode.BadRequest);
                    return;
                }
    
                // [BATRUN-HUB]: Resolve machine alias to IP:port
                // EN: In HubMode, only known machine aliases are accepted (anti-SSRF).
                // In non-HubMode, raw IP:port is still allowed for backward compatibility.
                // FR: En HubMode, seuls les alias de machines connus sont acceptés (anti-SSRF).
                // En mode non-Hub, les IP:port brutes sont toujours autorisées pour compatibilité.
                string? resolvedTarget = _manager.ResolveMachineAlias(target, out string? moonlightHost);
                if (resolvedTarget == null)
                {
                    _logger.LogWarning($"[Relay] Cannot resolve target '{target}' — rejected or unknown machine");
                    await SendResponseAsync(stream, "text/plain", "Forbidden: Unknown machine alias", HttpStatusCode.Forbidden);
                    return;
                }
    
                // EN: Store the original alias for WS URL injection (stream.html rewrite)
                // FR: Conserver l'alias original pour l'injection d'URL WS (réécriture stream.html)
                string targetAlias = target;
    
                _logger.LogInfo($"Relay {method} -> {resolvedTarget}{remotePath} (alias={targetAlias}, Body size: {body.Length})");

                // [BATRUN-HUB]: Use HTTPS with self-signed cert acceptance for inter-machine relay
                // FR: Utiliser HTTPS avec acceptation des certificats auto-signés pour le relay inter-machines
                var relayHandler = new HttpClientHandler
                {
                	ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
                using (var client = new HttpClient(relayHandler))
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    // [BATRUN-HUB]: Detect Moonlight paths and use Moonlight port (8080) instead of BatRun API port (4321)
                    // EN: If the path starts with /stream.html, /ws, /api/server, /assets, or /static,
                    // it's a Moonlight Web request — use the Moonlight host + port instead of BatRun API port.
                    // FR: Si le chemin commence par /stream.html, /ws, /api/server, /assets, ou /static,
                    // c'est une requête Moonlight Web — utiliser l'hôte Moonlight + port au lieu du port API BatRun.
                    bool isMoonlightPath = remotePath.StartsWith("/stream.html", StringComparison.OrdinalIgnoreCase) ||
                                           remotePath.StartsWith("/ws", StringComparison.OrdinalIgnoreCase) ||
                                           remotePath.StartsWith("/api/server", StringComparison.OrdinalIgnoreCase) ||
                                           remotePath.StartsWith("/assets", StringComparison.OrdinalIgnoreCase) ||
                                           remotePath.StartsWith("/static", StringComparison.OrdinalIgnoreCase);
                    string urlHost = isMoonlightPath && !string.IsNullOrEmpty(moonlightHost)
                        ? $"{moonlightHost}:{_moonlight.Port}"  // EN: Moonlight Web port (8080)
                        : resolvedTarget;                        // EN: BatRun API port (4321)
                    
                    // [BATRUN-DEBUG]: Log Moonlight relay details
                    _logger.LogInfo($"[Relay] isMoonlightPath={isMoonlightPath}, moonlightHost='{moonlightHost}', _moonlight.Port={_moonlight.Port}, urlHost={urlHost}");
                    // [BATRUN-HUB-FIX]: Moonlight Web ALWAYS listens on plain HTTP (port 8080), even if BatRun API uses HTTPS.
                    // EN: Force HTTP for Moonlight paths to avoid SSL handshake failure on remote Moonlight servers.
                    // FR: Forcer HTTP pour les chemins Moonlight pour éviter l'échec de handshake SSL sur les serveurs Moonlight distants.
            // [BATRUN-HUB-FIX]: Redirect stream.html requests to the prefix-based relay
            // (/stream-relay/{alias}/stream.html) BEFORE fetching. The prefix-based relay
            // handles all static assets correctly; the query-param relay cannot.
            // FR: Rediriger les requêtes stream.html vers le relay par préfixe AVANT la récupération.
            if (!isPrefixRelay && (remotePath.EndsWith("stream.html", StringComparison.OrdinalIgnoreCase) ||
                remotePath.StartsWith("/stream.html?", StringComparison.OrdinalIgnoreCase)))
            {
                string relayPrefixUrl = $"{(_serverCertificate != null ? "https" : "http")}://{targetHostForWs}:{_port}/stream-relay/{targetAlias}{remotePath}";
                _logger.LogInfo($"[Relay] Redirecting stream.html to prefix-based relay: {relayPrefixUrl}");
                await SendRedirectAsync(stream, relayPrefixUrl);
                return;
            }

            string relayProtocol = isMoonlightPath ? "http" : ((_serverCertificate != null) ? "https" : "http");
            string url = remotePath.StartsWith("http") ? remotePath : $"{relayProtocol}://{urlHost}{remotePath}";
                    
            // EN: Forward Range header if present
            // FR: Transférer l'en-tête Range si présent
            if (!string.IsNullOrEmpty(rangeHeader))
                client.DefaultRequestHeaders.Add("Range", rangeHeader);

            // [BATRUN-HUB-FIX]: Forward auth cookie to remote target for API relay calls.
            // The client's batrun_token cookie must be sent to the target so that
            // /api/public/status and other authenticated endpoints work through the relay.
            // FR: Transférer le cookie d'auth au target distant pour les appels API relay.
            if (requestHeaders != null && requestHeaders.TryGetValue("Cookie", out string? cookieHeader))
            {
                client.DefaultRequestHeaders.Add("Cookie", cookieHeader);
            }

            // [BATRUN-HUB-FIX]: Inject X-BatRun-User header for Moonlight API relay calls
            // so the remote Moonlight-Web auto-authenticates the relay request.
            // FR: Injecter le header X-BatRun-User pour les appels relay Moonlight API.
            if (isMoonlightPath)
            {
                client.DefaultRequestHeaders.Add("X-BatRun-User", _moonlight.ServiceUser);
            }

                    HttpResponseMessage response;
                    try {
                        if (method == "POST")
                        {
                            var content = new StringContent(body, Encoding.UTF8, "application/json");
                            response = await client.PostAsync(url, content);
                        }
                        else
                        {
                            response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                        }
                    } catch (Exception ex) {
                        _logger.LogWarning($"[Relay] Target unreachable or invalid: {url} - {ex.Message}");
                        await SendResponseAsync(stream, "text/plain", $"Relay Failed: {ex.Message}", HttpStatusCode.BadGateway);
                        return;
                    }

                    string contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
                    
                    // EN: Use binary relay for everything EXCEPT known text/json types
                    // FR: Utiliser le relais binaire pour TOUT sauf les types texte/json connus
                    bool isText = contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase) || 
                                 contentType.Contains("text/", StringComparison.OrdinalIgnoreCase);

                    if (!isText)
                    {
                        byte[] remoteBytes = await response.Content.ReadAsByteArrayAsync();
                        string? contentRange = response.Content.Headers.Contains("Content-Range") ? response.Content.Headers.GetValues("Content-Range").FirstOrDefault() : null;
                        await SendRawResponseAsync(stream, contentType, remoteBytes, response.StatusCode, contentRange);
                    }
                    else
                    {
                        string remoteContent = await response.Content.ReadAsStringAsync();
                        
            // [BATRUN-MOD]: Moonlight Web Relay Injections
            // stream.html requests are now redirected to prefix-based relay above,
            // before this fetch. No additional injection needed here.
            // FR: Les requêtes stream.html sont redirigées vers le relay par préfixe ci-dessus.

            await SendResponseAsync(stream, contentType, remoteContent, response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Relay Error for {target}{remotePath}", ex);
                await SendResponseAsync(stream, "text/plain", $"Relay Error: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        private Dictionary<string, string> ParseCookies(string cookieHeader)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(cookieHeader)) return dict;
            
            var pairs = cookieHeader.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var parts = pair.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                    dict[parts[0].Trim()] = parts[1].Trim();
            }
            return dict;
        }

        private Dictionary<string, string> ParseQueryString(string url)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int qIdx = url.IndexOf('?');
            if (qIdx == -1) return result;

            string query = url.Substring(qIdx + 1);
            
            // EN: Handle nested parameters in 'path=' for relaying
            // FR: Gérer les paramètres imbriqués dans 'path=' pour le relai
            int pathStart = query.IndexOf("path=", StringComparison.OrdinalIgnoreCase);
            string otherParams = query;

            if (pathStart != -1)
            {
                // Everything after 'path=' is part of the remote path (including its own query string)
                string pVal = query.Substring(pathStart + 5);
                result["path"] = Uri.UnescapeDataString(pVal);
                
                // Truncate query to parse other params (like target=)
                otherParams = query.Substring(0, pathStart);
            }

            foreach (var part in otherParams.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = part.Split('=');
                if (kv.Length >= 2)
                {
                    result[kv[0]] = Uri.UnescapeDataString(kv[1]);
                }
            }
            return result;
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
            _logger.LogInfo("Arcade API Server stopped");
        }
        public void Dispose()
        {
            Stop();
        }

        // EN: GetStaticNodeHtml() moved to Pages/ConnectCloudPage.cs
        // FR: GetStaticNodeHtml() deplace vers Pages/ConnectCloudPage.cs

        private async Task HandlePublicApiAsync(Stream stream, string method, string path, string rawPath, string body, IPAddress clientIp, Dictionary<string, string> headers)
        {
            bool isLocal = IsLocalOrAllowedIp(clientIp);
            try
            {
                if (!_manager.PublicAccessEnabled)
                {
                    await SendResponseAsync(stream, "text/plain", "Public access disabled", HttpStatusCode.Forbidden);
                    return;
                }

                if (path == "/api/public/nodes" && method == "GET")
                {
                    _manager.ReloadPublicSettings(); // [BATRUN-IRON]: Force sync from disk before returning list
                    var machinesList = _manager.GetNetworkMachines()
                        .Select(m => new {
                            name = m.Name,
                            ip = m.IP, // [BATRUN-IRON]: Always send real IP for direct links/redirection
                            port = m.Port,
                            isLocal = m.IsLocal, 
                            isOnline = m.IsOnline, 
                            requiresLogin = m.IsLocal ? _manager.PublicAccessRequiresLogin : m.RequiresLogin,
                            isMoonlightEnabled = m.IsLocal ? _manager.MoonlightStreamEnabled : m.IsMoonlightEnabled
                        })
                        .ToList();
                    
                    // [BATRUN-FIX]: Deduplicate by machine name to handle multi-homed hosts (multiple NICs)
                    machinesList = machinesList
                        .GroupBy(m => m.name.ToLower())
                        .Select(g => g.OrderByDescending(m => m.isLocal).First())
                        .ToList();

                    // EN: Ensure local machine is always present / FR: S'assurer que la machine locale est toujours présente
                    if (!machinesList.Any(m => m.isLocal))
                    {
                        machinesList.Add(new { 
                            name = Environment.MachineName, 
                            ip = "127.0.0.1", 
                            port = _port, 
                            isLocal = true, 
                            isOnline = true,
                            requiresLogin = _manager.PublicAccessRequiresLogin,
                            isMoonlightEnabled = _manager.MoonlightStreamEnabled
                        });
                    }

                    bool isLocalAccess = IsLocalOrAllowedIp(clientIp);
                    var final = machinesList.OrderByDescending(m => m.isLocal).ThenBy(m => m.name)
                        .Select(m => new {
                            m.name,
                            m.ip,
                            m.port,
                            m.isLocal,
                            m.isOnline,
                            m.requiresLogin,
                            m.isMoonlightEnabled,
                            displayIp = m.isLocal ? "LOCAL" : (isLocalAccess ? m.ip : "REMOTE")
                        }).ToList();

                    string jsonNodes = Newtonsoft.Json.JsonConvert.SerializeObject(final, _jsonSettings);
                    await SendResponseAsync(stream, "application/json", jsonNodes);
                }

                if (path == "/api/public/status" && method == "GET")
                {
                    // [BATRUN-AUTH] Check token if registration is required
                    if (_manager.PublicAccessRequiresLogin && !isLocal)
                    {
                        var queryParams = ParseQueryString(rawPath);
                        string? token = queryParams.GetValueOrDefault("token");
                        
                        // Try cookie if not in query
                        if (string.IsNullOrEmpty(token))
                        {
                            var cookies = ParseCookies(headers.GetValueOrDefault("Cookie", ""));
                            token = cookies.GetValueOrDefault("batrun_token");
                        }

                        if (!_manager.PublicUserManager.ValidateToken(token ?? "") && !isLocal)
                        {
                            await SendResponseAsync(stream, "text/plain", "Unauthorized", HttpStatusCode.Unauthorized);
                            return;
                        }
                    }

                    object status;
                    lock (_lobbyLock)
                    {
                        status = new
                        {
                            apiPort = _port,
                            machineName = Environment.MachineName,
                            isMoonlightEnabled = _manager.MoonlightStreamEnabled,
                            isGameInProgress = _manager.IsGameInProgress,
                            isWebLaunch = _manager.IsWebLaunch,
                            lastGameEndTime = _manager.LastGameEndUtc,
                            forceStopTime = _manager.ForceStopTimestamp, // EN: Timestamp of last force-stop for web clients / FR: Timestamp du dernier arrêt forcé pour les clients web
                            requiresLogin = _manager.PublicAccessRequiresLogin, // [BATRUN-IRON]: Expose current security state
                            hubMode = _manager.HubMode, // [BATRUN-HUB]: Expose hub mode state
                            isLobbyActive = _lobby.Phase != "none",
                            isLobbyWaiting = _lobby.Phase == "lobby" || _lobby.Phase == "confirm"
                        };
                    }
                    await SendResponseAsync(stream, "application/json", Newtonsoft.Json.JsonConvert.SerializeObject(status, _jsonSettings));
                }
                else if (path == "/api/public/register" && method == "POST")
                {
                    if (!_manager.PublicAccessAllowRegistration)
                    {
                        await SendResponseAsync(stream, "application/json", "{\"error\":\"Registration is currently disabled\"}", HttpStatusCode.Forbidden);
                        return;
                    }

                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
                    string? user = data?.username;
                    string? pass = data?.password;

                    var newUser = _manager.PublicUserManager.RegisterUser(user ?? "", pass ?? "");
                    if (newUser != null)
                        await SendResponseAsync(stream, "application/json", "{\"status\":\"pending\"}");
                    else
                        await SendResponseAsync(stream, "application/json", "{\"error\":\"Registration failed\"}", HttpStatusCode.BadRequest);
                }
                else if (path == "/api/public/login" && method == "POST")
                {
                    // [BATRUN-IRON]: Re-sync settings from disk before login to prevent stale configuration bypass
                    _manager.ReloadPublicSettings();

                    // [BATRUN-SEC]: Rate limiting — reject if too many login requests from this IP
                    // FR: Rate limiting — rejeter si trop de requêtes de login depuis cette IP
                    if (_manager.BlacklistManager.IsLoginRateLimited(clientIp))
                    {
                        await SendResponseAsync(stream, "application/json", "{\"error\":\"Too many requests. Please try again later.\"}", HttpStatusCode.TooManyRequests);
                        return;
                    }

                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
                    string? user = data?.username;
                    string? pass = data?.password;

                    bool isGuestAttempt = string.IsNullOrEmpty(user) || user.ToLower() == "guest";

                    // EN: Check if the manager is in "No Login" mode
                    // FR: Vérifier si le manager est en mode "No Login"
                    // [BATRUN-IRON]: Strict Guest login denial if security is enabled OR if IP is external
                    if (!_manager.PublicAccessRequiresLogin && isGuestAttempt)
                    {
                        // [BATRUN-MOD]: Even if login is not required globally, EXTERNAL IPs must always login
                        // [BATRUN-MOD]: Même si le login n'est pas requis globalement, les IPs EXTERNES doivent toujours se connecter
                        if (!IsLocalOrAllowedIp(clientIp))
                        {
                            _logger.LogWarning($"[Security] Blocked external Guest login attempt from {clientIp} (External IPs must have an account)");
                            await SendResponseAsync(stream, "application/json", "{\"error\":\"External access requires a registered account.\"}", HttpStatusCode.Unauthorized);
                            return;
                        }

                        string deviceId = data?.deviceId ?? data?.sessionId ?? "unknown";
                        var guestUser = _manager.PublicUserManager.LoginGuest(deviceId);
                        if (guestUser != null)
                        {
                            _manager.BlacklistManager.RecordGuestLogin(clientIp);
                            _manager.BlacklistManager.ResetFailedAttempts(clientIp, "Guest");
                            await SendResponseAsync(stream, "application/json", "{\"token\":\"" + guestUser.Token + "\"}");
                            return;
                        }
                    }
                    else if (user?.ToLower() == "guest" && _manager.PublicAccessRequiresLogin)
                    {
                        // FR: Refuser explicitement l'accès invité si le login est requis
                        _logger.LogWarning($"[Security] Blocked unauthorized Guest login attempt (RequiresLogin=True)");
                        await SendResponseAsync(stream, "application/json", "{\"error\":\"Guest access disabled. Please login.\"}", HttpStatusCode.Unauthorized);
                        return;
                    }

                    var logged = _manager.PublicUserManager.AuthenticateLogin(user ?? "", pass ?? "");
                    if (logged != null)
                    {
                        _manager.BlacklistManager.ResetFailedAttempts(clientIp, user ?? "Unknown");
                        await SendResponseAsync(stream, "application/json", "{\"token\":\"" + logged.Token + "\"}");
                    }
                    else
                    {
                        _manager.BlacklistManager.RecordFailedAttempt(clientIp, user ?? "Unknown");
                        // [BATRUN-SEC]: Unified error message to prevent user enumeration
                        // FR: Message d'erreur unifié pour empêcher l'énumération des utilisateurs
                        await SendResponseAsync(stream, "application/json", "{\"error\":\"Invalid credentials\"}", HttpStatusCode.Unauthorized);
                    }
                }
                // [BATRUN-CRED] EN: Firefox-compatible form login endpoint — accepts application/x-www-form-urlencoded
                // FR: Endpoint de login compatible Firefox — accepte application/x-www-form-urlencoded pour déclencher le gestionnaire de mots de passe
                else if (path == "/api/public/login-form" && method == "POST")
                {
                    _manager.ReloadPublicSettings();

                    // [BATRUN-SEC]: Rate limiting — reject if too many login requests from this IP
                    // FR: Rate limiting — rejeter si trop de requêtes de login depuis cette IP
                    if (_manager.BlacklistManager.IsLoginRateLimited(clientIp))
                    {
                        await SendRedirectAsync(stream, "/?login_error=rate_limited");
                        return;
                    }

                    // EN: Parse form-encoded body (username=foo&password=bar)
                    // FR: Analyser le corps encodé en formulaire
                    var formFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var pair in body.Split('&'))
                    {
                        var kv = pair.Split('=', 2);
                        if (kv.Length == 2)
                            formFields[Uri.UnescapeDataString(kv[0].Replace('+', ' '))] = Uri.UnescapeDataString(kv[1].Replace('+', ' '));
                    }
                    formFields.TryGetValue("username", out string? fUser);
                    formFields.TryGetValue("password", out string? fPass);

                    // EN: Guest shortcut
                    if (string.IsNullOrEmpty(fUser) || fUser.ToLower() == "guest")
                    {
                        if (!_manager.PublicAccessRequiresLogin && IsLocalOrAllowedIp(clientIp))
                        {
                            string deviceId = formFields.TryGetValue("deviceId", out string? did) ? did : "form";
                            var guestUser = _manager.PublicUserManager.LoginGuest(deviceId);
                            if (guestUser != null)
                            {
                                await SendRedirectWithCookieAsync(stream, "/?token=" + guestUser.Token, $"batrun_token={guestUser.Token}; Path=/; SameSite=Lax");
                                return;
                            }
                        }
                        await SendRedirectAsync(stream, "/?login_error=1");
                        return;
                    }

                    var loggedForm = _manager.PublicUserManager.AuthenticateLogin(fUser ?? "", fPass ?? "");
                    if (loggedForm != null)
                    {
                        _manager.BlacklistManager.ResetFailedAttempts(clientIp, fUser ?? "Unknown");
                        await SendRedirectWithCookieAsync(stream, "/?token=" + loggedForm.Token, $"batrun_token={loggedForm.Token}; Path=/; SameSite=Lax");
                    }
                    else
                    {
                        _manager.BlacklistManager.RecordFailedAttempt(clientIp, fUser ?? "Unknown");
                        await SendRedirectAsync(stream, "/?login_error=1");
                    }
                }
                // [BATRUN-GUEST] EN: Logout endpoint — invalidates token and auto-deletes guest accounts
                // FR: Endpoint de deconnexion — invalide le token et supprime automatiquement les comptes guest
                else if (path == "/api/public/logout" && method == "POST")
                {
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
                    string? token = data?.token;

                    if (!string.IsNullOrEmpty(token))
                    {
                        bool result = _manager.PublicUserManager.LogoutUser(token);
                        _logger.LogInfo($"[Security] Logout request processed (success={result})");
                    }

                    await SendResponseAsync(stream, "application/json", "{\"ok\":true}");
                }
                else if (path == "/api/public/launch" && method == "POST")
                {
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
                    string? token = data?.token;
                    string? gamePath = data?.gamePath;

                    if (!_manager.PublicUserManager.ValidateToken(token ?? "") && !isLocal)
                    {
                        await SendResponseAsync(stream, "text/plain", "Unauthorized", HttpStatusCode.Unauthorized);
                        return;
                    }

                    // EN: [RETROBAT_UI] is a virtual path for the RetroBat interface stream.
                    //     It does NOT represent a real game file — skip ES launcher entirely.
                    // FR: [RETROBAT_UI] est un chemin virtuel pour le stream de l'interface RetroBat.
                    //     Pas de fichier réel — passer le lanceur ES entièrement.
                    if (gamePath == "[RETROBAT_UI]")
                    {
                        _manager.StartWebUiSession();
                        await SendResponseAsync(stream, "application/json", "{\"success\":true}");
                        return;
                    }

                    if (_manager.IsGameInProgress)
                    {
                        // EN: Game already running — this is not an error for the client.
                        //     Return 200 with a special flag so JS can treat it as "join existing session".
                        // FR: Jeu déjà en cours — ce n'est pas une erreur pour le client.
                        //     Retourner 200 avec un flag spécial pour que le JS rejoigne la session existante.
                        await SendResponseAsync(stream, "application/json", "{\"success\":true,\"alreadyRunning\":true}");
                        return;
                    }

                    if (!string.IsNullOrEmpty(gamePath))
                    {
                        _logger.LogInfo($"[Public] Launch requested for: {gamePath}");
                        _manager.IsWebLaunch = true;
                        var scraper = new EmulationStationScraper();
                        bool result = await scraper.LaunchGameAsync(gamePath);
                        await SendResponseAsync(stream, "application/json", "{\"success\":" + result.ToString().ToLower() + "}");
                    }
                    else
                    {
                        await SendResponseAsync(stream, "text/plain", "Missing gamePath", HttpStatusCode.BadRequest);
                    }
                }
                else if (path == "/api/public/status" && method == "POST")
                {
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
                    string? token = data?.token;

                    if (!_manager.PublicUserManager.ValidateToken(token ?? "") && !isLocal)
                    {
                        await SendResponseAsync(stream, "text/plain", "Unauthorized", HttpStatusCode.Unauthorized);
                        return;
                    }
                    object status;
                    lock (_lobbyLock)
                    {
                        status = new
                        {
                            isGameInProgress = _manager.IsGameInProgress,
                            isWebLaunch = _manager.IsWebLaunch,
                            isMoonlightEnabled = _manager.MoonlightStreamEnabled,
                            lastGameEndTime = _manager.LastGameEndUtc,
                            forceStopTime = _manager.ForceStopTimestamp,
                            // [BATRUN-FORK-v4]: Expose current game name so P2 can see what's running
                            // FR: Exposer le nom du jeu en cours pour que P2 puisse voir ce qui tourne
                            currentGameName = _manager.CurrentGameName ?? "",
                            isLobbyActive = _lobby.Phase != "none",
                            isLobbyWaiting = _lobby.Phase == "lobby" || _lobby.Phase == "confirm"
                        };
                    }
                    await SendResponseAsync(stream, "application/json", Newtonsoft.Json.JsonConvert.SerializeObject(status, _jsonSettings));
                }
                else if (path == "/api/public/games" && method == "POST")
                {
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
                    string? token = data?.token;

                    if (!_manager.PublicUserManager.ValidateToken(token ?? "") && !isLocal)
                    {
                        await SendResponseAsync(stream, "text/plain", "Unauthorized", HttpStatusCode.Unauthorized);
                        return;
                    }

                    var scraper = new EmulationStationScraper();
                    var games = await scraper.GetAllGamesAsync();
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(games, _jsonSettings);
                    await SendResponseAsync(stream, "application/json", json);
                }
                else if (path == "/api/public/stop" && method == "POST")
                {
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
                    string? token = data?.token;
    
                    if (!_manager.PublicUserManager.ValidateToken(token ?? "") && !isLocal)
                    {
                        await SendResponseAsync(stream, "text/plain", "Unauthorized", HttpStatusCode.Unauthorized);
                        return;
                    }
    
                    _logger.LogInfo("[Public] Stop game requested.");
                    _manager.ForceStopGameFromWeb();
                    // [BATRUN-FORK]: Reset server-side lobby on game stop
                    // FR: Réinitialiser le lobby côté serveur à l'arrêt du jeu
                    lock (_lobbyLock) { _lobby.Reset(); }
                    await SendResponseAsync(stream, "application/json", "{\"success\":true}");
                }
                else if (path == "/api/public/start-rb" && method == "POST")
                {
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
                    string? token = data?.token;
    
                    if (!_manager.PublicUserManager.ValidateToken(token ?? "") && !isLocal)
                    {
                        await SendResponseAsync(stream, "text/plain", "Unauthorized", HttpStatusCode.Unauthorized);
                        return;
                    }
    
                    _logger.LogInfo("[Public] Manual RetroBat launch requested via Emergency UI.");
                    var rbConfig = new IniFile(Path.Combine(AppContext.BaseDirectory, "config.ini"));
                    var rbService = new RetroBatService(_logger, rbConfig);
                    rbService.LaunchEmulationStation();
                    await SendResponseAsync(stream, "application/json", "{\"success\":true}");
                }
                // EN: Emergency force-stop — kills game process + stops session + stops Moonlight stream for all players
                // FR: Arrêt forcé d'urgence — tue le processus jeu + arrête la session + arrête le stream Moonlight pour tous les joueurs
                else if (path == "/api/public/force-stop-session" && method == "POST")
                {
                    _logger.LogInfo($"[API] Force stop session requested via {path}");
                    string? token = null;
                    if (!string.IsNullOrEmpty(body) && body.Trim().StartsWith("{"))
                    {
                        var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
                        token = data?.token;
                    }

                    // EN: Fallback to query string for GET-like POSTs
                    // FR: Fallback vers la query string pour les POST typés GET
                    if (string.IsNullOrEmpty(token))
                    {
                        var queryParams = ParseQueryString(rawPath);
                        token = queryParams.GetValueOrDefault("token");
                    }
    
                    if (!_manager.PublicUserManager.ValidateToken(token ?? "") && !isLocal)
                    {
                        await SendResponseAsync(stream, "text/plain", "Unauthorized", HttpStatusCode.Unauthorized);
                        return;
                    }
    
                    _logger.LogInfo("[Public] Force-stop-session requested (emergency stop).");
                    _manager.ForceStopSessionAndGame();
                    // EN: Reset lobby on emergency stop / FR: Réinitialiser le lobby à l'arrêt d'urgence
                    lock (_lobbyLock) { _lobby.Reset(); }
                    await SendResponseAsync(stream, "application/json", "{\"success\":true}");
                }
                // [BATRUN-FORK]: Wild Exit / Cancel from Web UI. Cleanly disconnects Moonlight and resets lobby.
                // FR: Sortie brutale / Annulation web. Déconnecte proprement Moonlight et réinitialise le lobby.
                else if (path == "/api/host/cancel" && method == "POST")
                {
                    _logger.LogInfo($"[API] Session cancellation requested via {path}");
                    string? token = null;
                    if (!string.IsNullOrEmpty(body) && body.Trim().StartsWith("{"))
                    {
                        var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
                        token = data?.token;
                    }

                    if (string.IsNullOrEmpty(token))
                    {
                        var queryParams = ParseQueryString(rawPath);
                        token = queryParams.GetValueOrDefault("token");
                    }

                    if (!_manager.PublicUserManager.ValidateToken(token ?? "") && !isLocal)
                    {
                        await SendResponseAsync(stream, "text/plain", "Unauthorized", HttpStatusCode.Unauthorized);
                        return;
                    }

                    _logger.LogInfo("[Public] Moonlight cancel requested (Wild Exit).");
                    lock (_lobbyLock) { _lobby.Reset(); }
                    
                    // [BATRUN-FIX]: Only cancel Sunshine if a game is actually running.
                    // If we just cancel from the lobby, we shouldn't kill the Moonlight service state.
                    if (_manager.IsGameInProgress) 
                    {
                        await _manager.Moonlight.SendSunshineCancelAsync();
                    }
                    
                    await SendResponseAsync(stream, "application/json", "{\"success\":true}");
                }
                // [BATRUN-FORK] Lobby API endpoints for cross-session multiplayer
                // FR: Points d'API du lobby pour le multijoueur inter-sessions
                else if (path == "/api/public/lobby/create" && method == "POST")
                {
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
                    string? token = data?.token;
                    string? gamePath = data?.gamePath;
                    string? sessionId = data?.sessionId;
                    if (!_manager.PublicUserManager.ValidateToken(token ?? "") && !isLocal)
                    {
                        await SendResponseAsync(stream, "text/plain", "Unauthorized", HttpStatusCode.Unauthorized);
                        return;
                    }
                    lock (_lobbyLock)
                    {
                    // EN: Auto-expire stale lobby (>5 min inactive)
                    // FR: Expiration automatique du lobby inactif (>5 min)
                    if (_lobby.Phase != "none" && (DateTime.UtcNow - _lobby.LastActivity).TotalMinutes > 5)
                    {
                    _logger.LogInfo("[Lobby] Expired stale lobby, resetting.");
                    _lobby.Reset();
                    }
                    // [BATRUN-FORK-v9]: Also reset if lobby is in 'launching' or 'confirm' phase
                    // but no game is actually running on the host. This prevents a stale lobby
                    // from causing the next launch to skip the lobby overlay.
                    // FR: Aussi reset si le lobby est en phase 'launching' ou 'confirm'
                    // mais qu'aucun jeu ne tourne sur l'hôte. Cela empêche un lobby périmé
                    // de faire sauter l'overlay lobby au prochain lancement.
                    if ((_lobby.Phase == "launching" || _lobby.Phase == "confirm") && !_manager.IsGameInProgress)
                    {
                    _logger.LogInfo($"[Lobby] Resetting stale lobby in phase '{_lobby.Phase}' (no game running).");
                    _lobby.Reset();
                    }
                    if (_lobby.Phase != "none")
                        {
                            // EN: Lobby already active — join it instead
                            // FR: Lobby déjà actif — le rejoindre à la place
                            var existing = _lobby.Players.Find(p => p.SessionId == sessionId);
                            if (existing == null && _lobby.Players.Count < 4)
                            {
                                _lobby.Players.Add(new LobbyPlayer { SessionId = sessionId ?? "", Token = token ?? "", Ready = false });
                                _lobby.LastActivity = DateTime.UtcNow;
                                _logger.LogInfo($"[Lobby] Player joined existing lobby (session={sessionId}, total={_lobby.Players.Count})");
                            }
                        }
                        else
                        {
                            // EN: Create new lobby directly in 'lobby' phase (waiting by default)
                            // FR: Créer un nouveau lobby directement en phase 'lobby' (attente par défaut)
                            _lobby.Phase = "lobby";
                            _lobby.PendingGamePath = gamePath ?? "";
                            _lobby.P1SessionId = sessionId ?? ""; // kept for logging/debug only
                            _lobby.Players.Clear();
                            _lobby.Players.Add(new LobbyPlayer { SessionId = sessionId ?? "", Token = token ?? "", Ready = false });
                            _lobby.CreatedAt = DateTime.UtcNow;
                            _lobby.LastActivity = DateTime.UtcNow;
                            _logger.LogInfo($"[Lobby-DBG] Created by P1 (session={sessionId}, game={gamePath})");
                        }
                    }
                    _logger.LogInfo($"[Lobby-DBG] /create SUCCESS: session={sessionId}");
                    await SendResponseAsync(stream, "application/json", "{\"success\":true}");
                }
                else if (path == "/api/public/lobby/status" && method == "POST")
                {
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
                    string? token = data?.token;
                    string? sessionId = data?.sessionId;
                    if (!_manager.PublicUserManager.ValidateToken(token ?? "") && !isLocal)
                    {
                        _logger.LogWarning($"[Lobby-DBG] /status REJECTED: invalid token for session={sessionId}");
                        await SendResponseAsync(stream, "text/plain", "Unauthorized", HttpStatusCode.Unauthorized);
                        return;
                    }
                    string lobbyJson;
                    lock (_lobbyLock)
                    {
                        var lobbyInfo = new
                        {
                            phase = _lobby.Phase,
                            playerCount = _lobby.Players.Count,
                            players = _lobby.Players.Select((p, i) => new
                            {
                                index = i,
                                ready = p.Ready,
                                isP1 = i == 0
                            }).ToList(),
                            isP1 = _lobby.Players.Count > 0 && _lobby.Players[0].SessionId == sessionId,
                            gamePath = _lobby.PendingGamePath
                        };
                        lobbyJson = Newtonsoft.Json.JsonConvert.SerializeObject(lobbyInfo, _jsonSettings);
                        // EN: Diagnostic log - throttled to every 5s per session / FR: Log diagnostic - limité à 5s par session
                        if (_lobby.Phase != "none")
                        {
                            var now = DateTime.UtcNow;
                            string logKey = "status_" + (sessionId ?? "?");
                            if (!_lobbyLogThrottle.TryGetValue(logKey, out var lastLog) || (now - lastLog).TotalSeconds >= 5)
                            {
                                _lobbyLogThrottle[logKey] = now;
                                bool callerIsP1 = _lobby.Players.Count > 0 && _lobby.Players[0].SessionId == sessionId;
                                // [BATRUN-FORK-v4]: Reduced lobby/status logging — only log when phase changes or player count changes
                                // FR: Réduction des logs lobby/status — ne logger que quand la phase ou le nombre de joueurs change
                                string lobbyStatusKey = $"{_lobby.Phase}:{_lobby.Players.Count}";
                                if (lobbyStatusKey != _lastLobbyStatusKey)
                                {
                                    _lastLobbyStatusKey = lobbyStatusKey;
                                    _logger.LogInfo($"[Lobby] Status: session={sessionId}, phase={_lobby.Phase}, players={_lobby.Players.Count}, isP1={callerIsP1}");
                                }
                            }
                        }
                    }
                    await SendResponseAsync(stream, "application/json", lobbyJson);
                }
                else if (path == "/api/public/lobby/ready" && method == "POST")
                {
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
                    string? token = data?.token;
                    string? sessionId = data?.sessionId;
                    _logger.LogInfo($"[Lobby-DBG] /ready CALLED: session={sessionId}");
                    if (!_manager.PublicUserManager.ValidateToken(token ?? "") && !isLocal)
                    {
                        _logger.LogWarning($"[Lobby-DBG] /ready REJECTED: invalid token for session={sessionId}");
                        await SendResponseAsync(stream, "text/plain", "Unauthorized", HttpStatusCode.Unauthorized);
                        return;
                    }
                    lock (_lobbyLock)
                    {
                        var player = _lobby.Players.Find(p => p.SessionId == sessionId);
                        _logger.LogInfo($"[Lobby-DBG] /ready LOCK: session={sessionId}, playerFound={player != null}, phase={_lobby.Phase}, totalPlayers={_lobby.Players.Count}");
                        if (player != null)
                        {
                            player.Ready = !player.Ready; // EN: Toggle ready state / FR: Basculer l'état prêt
                            _lobby.LastActivity = DateTime.UtcNow;
                            _logger.LogInfo($"[Lobby] Player toggled ready (session={sessionId}, state={player.Ready}, ready={_lobby.Players.Count(p => p.Ready)}/{_lobby.Players.Count})");
                        }
                        // EN: Auto-advance to confirm only when at least 2 players are present and all are ready
                        // FR: Passer automatiquement en confirmation uniquement quand au moins 2 joueurs sont présents et tous prêts
                        if (_lobby.Phase == "lobby" && _lobby.Players.Count > 1 && _lobby.Players.All(p => p.Ready))
                        {
                            _lobby.Phase = "confirm";
                            _logger.LogInfo("[Lobby] All players ready → confirm phase.");
                        }
                    }
                    await SendResponseAsync(stream, "application/json", "{\"success\":true}");
                }
                else if (path == "/api/public/lobby/solo" && method == "POST")
                {
                    // EN: Solo launch — player doesn't want to wait, launch immediately
                    // FR: Lancement solo — le joueur ne veut pas attendre, lancer immédiatement
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
                    string? token = data?.token;
                    string? sessionId = data?.sessionId;
                    if (!_manager.PublicUserManager.ValidateToken(token ?? "") && !isLocal)
                    {
                        await SendResponseAsync(stream, "text/plain", "Unauthorized", HttpStatusCode.Unauthorized);
                        return;
                    }
                    lock (_lobbyLock)
                    {
                        if (_lobby.Phase == "lobby")
                        {
                            var player = _lobby.Players.Find(p => p.SessionId == sessionId);
                            if (player != null) player.Ready = true;
                            _lobby.Phase = "launching";
                            _lobby.LastActivity = DateTime.UtcNow;
                            _logger.LogInfo($"[Lobby] Solo launch by session={sessionId}");
                        }
                    }
                    await SendResponseAsync(stream, "application/json", "{\"success\":true}");
                }
                else if (path == "/api/public/lobby/join" && method == "POST")
                {
                    // EN: Join existing lobby — does NOT create one if none exists
                    // FR: Rejoindre un lobby existant — ne crée PAS de lobby si aucun n'existe
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
                    string? token = data?.token;
                    string? sessionId = data?.sessionId;
                    _logger.LogInfo($"[Lobby-DBG] /join CALLED: session={sessionId}");
                    if (!_manager.PublicUserManager.ValidateToken(token ?? "") && !isLocal)
                    {
                        _logger.LogWarning($"[Lobby-DBG] /join REJECTED: invalid token for session={sessionId}");
                        await SendResponseAsync(stream, "text/plain", "Unauthorized", HttpStatusCode.Unauthorized);
                        return;
                    }
                    lock (_lobbyLock)
                    {
                        _logger.LogInfo($"[Lobby-DBG] /join LOCK: phase={_lobby.Phase}, currentPlayers={_lobby.Players.Count}");
                        if (_lobby.Phase != "none")
                        {
                            var existing = _lobby.Players.Find(p => p.SessionId == sessionId);
                            if (existing == null && _lobby.Players.Count < 4)
                            {
                                _lobby.Players.Add(new LobbyPlayer { SessionId = sessionId ?? "", Token = token ?? "", Ready = false });
                                _lobby.LastActivity = DateTime.UtcNow;
                                _logger.LogInfo($"[Lobby] Player joined via /join (session={sessionId}, total={_lobby.Players.Count})");
                            }
                        }
                    }
                    await SendResponseAsync(stream, "application/json", "{\"success\":true}");
                }
                else if (path == "/api/public/lobby/launch" && method == "POST")
                {
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
                    string? token = data?.token;
                    string? sessionId = data?.sessionId;
                    if (!_manager.PublicUserManager.ValidateToken(token ?? "") && !isLocal)
                    {
                        await SendResponseAsync(stream, "text/plain", "Unauthorized", HttpStatusCode.Unauthorized);
                        return;
                    }
                    lock (_lobbyLock)
                    {
                        // EN: Only P1 (first player, index 0) can confirm launch
                        // FR: Seul P1 (premier joueur, index 0) peut confirmer le lancement
                        bool isP1 = _lobby.Players.Count > 0 && _lobby.Players[0].SessionId == sessionId;
                        if (_lobby.Phase == "confirm" && isP1)
                        {
                            _lobby.Phase = "launching";
                            _lobby.LastActivity = DateTime.UtcNow;
                            _logger.LogInfo("[Lobby] P1 confirmed launch!");
                        }
                    }
                    await SendResponseAsync(stream, "application/json", "{\"success\":true}");
                }
                else if (path == "/api/public/lobby/leave" && method == "POST")
                {
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
                    string? token = data?.token;
                    string? sessionId = data?.sessionId;
                    if (!_manager.PublicUserManager.ValidateToken(token ?? "") && !isLocal)
                    {
                        await SendResponseAsync(stream, "text/plain", "Unauthorized", HttpStatusCode.Unauthorized);
                        return;
                    }
                    lock (_lobbyLock)
                    {
                        // EN: Check if leaving player is P1 (index 0) before removing
                        // FR: Vérifier si le joueur qui part est P1 (index 0) avant de le retirer
                        bool wasP1 = _lobby.Players.Count > 0 && _lobby.Players[0].SessionId == sessionId;
                        _lobby.Players.RemoveAll(p => p.SessionId == sessionId);
                        if (_lobby.Players.Count == 0 || wasP1)
                        {
                            _lobby.Reset();
                            _logger.LogInfo($"[Lobby] Reset (P1 left or no players, session={sessionId})");
                        }
                        _lobby.LastActivity = DateTime.UtcNow;
                    }
                    await SendResponseAsync(stream, "application/json", "{\"success\":true}");
                    }
                    // [BATRUN-FORK-v9]: Sunshine /cancel endpoint — called from JS stopLaunch() to immediately
                    // disconnect the virtual controller on the host, instead of waiting 27s for timeout.
                    // FR: Endpoint /cancel Sunshine — appelé depuis JS stopLaunch() pour déconnecter immédiatement
                    // la manette virtuelle sur l'hôte, au lieu d'attendre 27s de timeout.
                    else if (path == "/api/public/sunshine-cancel" && method == "POST")
                    {
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
                    string? token = data?.token;
                    if (!_manager.PublicUserManager.ValidateToken(token ?? "") && !isLocal)
                    {
                    await SendResponseAsync(stream, "text/plain", "Unauthorized", HttpStatusCode.Unauthorized);
                    return;
                    }
                    try
                    {
                    await _manager.Moonlight.SendSunshineCancelAsync();
                    await SendResponseAsync(stream, "application/json", "{\"success\":true}");
                    }
                    catch (Exception ex)
                    {
                    _logger.LogWarning($"[API] sunshine-cancel error: {ex.Message}");
                    await SendResponseAsync(stream, "application/json", $"{{\"success\":false,\"error\":\"{ex.Message}\"}}");
                    }
                    }
                else
                {
                    await SendResponseAsync(stream, "text/plain", "Not Found", HttpStatusCode.NotFound);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in HandlePublicApi", ex);
                await SendResponseAsync(stream, "text/plain", "Bad Request", HttpStatusCode.BadRequest);
            }
        }

        private async Task HandleAdminApiAsync(Stream stream, string method, string path, string body)
        {
            try
            {
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
                string? password = data?.password;

                if (!string.IsNullOrEmpty(_manager.OperatorPassword) && password != _manager.OperatorPassword)
                {
                    await SendResponseAsync(stream, "text/plain", "Unauthorized", HttpStatusCode.Unauthorized);
                    return;
                }

                if (path == "/api/admin/users" && method == "POST") // Post to allow body auth
                {
                    var users = _manager.PublicUserManager.GetAllUsers().Select(u => new { 
                        id = u.Id, username = u.Username, status = u.Status, createdAt = u.CreatedAt 
                    });
                    await SendResponseAsync(stream, "application/json", Newtonsoft.Json.JsonConvert.SerializeObject(users, _jsonSettings));
                }
                else if (path == "/api/admin/users/action" && method == "POST")
                {
                    string? action = data?.action; // approve / reject
                    string? targetUserId = data?.userId;

                    bool success = false;
                    if (action == "approve") success = _manager.PublicUserManager.ApproveUser(targetUserId ?? "");
                    else if (action == "reject") success = _manager.PublicUserManager.RejectUser(targetUserId ?? "");

                    await SendResponseAsync(stream, "application/json", "{\"success\":" + success.ToString().ToLower() + "}");
                }
                else if (path == "/api/admin/config/reload" && method == "POST")
                {
                    _manager.ReloadPublicSettings();
                    await SendResponseAsync(stream, "application/json", "{\"success\":true}");
                }
                else if (path == "/api/admin/security/logs" && method == "POST")
                {
                    var logRequest = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(body, new { date = "" });
                    var logs = _manager.BlacklistManager.GetSecurityLogs(logRequest?.date);
                    var blocked = _manager.BlacklistManager.GetBlockedIps();
                    
                    var response = new {
                        logs = logs,
                        blocked = blocked
                    };
                    
                    await SendResponseAsync(stream, "application/json", Newtonsoft.Json.JsonConvert.SerializeObject(response, _jsonSettings));
                }
                else if (path == "/api/admin/security/dates" && method == "POST")
                {
                    var dates = _manager.BlacklistManager.GetAvailableLogDates();
                    await SendResponseAsync(stream, "application/json", Newtonsoft.Json.JsonConvert.SerializeObject(dates, _jsonSettings));
                }
                else
                {
                    await SendResponseAsync(stream, "text/plain", "Not Found", HttpStatusCode.NotFound);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in HandleAdminApi", ex);
                await SendResponseAsync(stream, "text/plain", "Bad Request", HttpStatusCode.BadRequest);
            }
        }

        // EN: GetConnectPageHtml() in Pages/ConnectPage.cs — login page only (ZERO game code)
        // FR: GetConnectPageHtml() dans Pages/ConnectPage.cs — page de login uniquement (AUCUN code jeu)
        // EN: GetCloudPageHtml() in Pages/CloudPage.cs — games/streaming page (server-side token validated)
        // FR: GetCloudPageHtml() dans Pages/CloudPage.cs — page jeux/streaming (token validé côté serveur)
        // EN: GetWebUIHtml() moved to Pages/AdminPage.cs
        // FR: GetWebUIHtml() deplace vers Pages/AdminPage.cs

        private async Task HandleWebSocketRelayAsync(TcpClient client, Stream clientStream, string method, string rawPath, Dictionary<string, string> headers)
        {
            var queryParams = ParseQueryString(rawPath);
            if (!queryParams.TryGetValue("target", out string? targetAlias) || string.IsNullOrEmpty(targetAlias)) return;
    
            try
            {
                // [BATRUN-HUB]: Resolve machine alias to IP for WebSocket bridge
                // EN: In HubMode, only known machine aliases are accepted (anti-SSRF).
                // FR: En HubMode, seuls les alias de machines connus sont acceptés (anti-SSRF).
                string? resolvedTarget = _manager.ResolveMachineAlias(targetAlias, out string? moonlightHost);
                if (resolvedTarget == null || string.IsNullOrEmpty(moonlightHost))
                {
                    _logger.LogWarning($"[Relay-WS] Cannot resolve target alias '{targetAlias}' — rejected or unknown machine");
                    return;
                }
    
                // EN: Extract just the IP part from resolved target (e.g., "192.168.1.20:4321" → "192.168.1.20")
                // FR: Extraire juste la partie IP du target résolu (ex: "192.168.1.20:4321" → "192.168.1.20")
                string targetIp = moonlightHost;
                int moonlightPort = _moonlight.Port; // EN: Default Moonlight Web port (8080)
    
                _logger.LogInfo($"[Relay-WS] Opening WebSocket bridge to {targetIp}:{moonlightPort} (alias={targetAlias}, Handshake relay)...");
                using (TcpClient targetClient = new TcpClient())
                {
                    targetClient.NoDelay = true;
                    client.NoDelay = true;
                    targetClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    await targetClient.ConnectAsync(targetIp, moonlightPort);
                    using (NetworkStream targetStream = targetClient.GetStream())
                    {
                        // EN: Re-send the handshake request to the target Moonlight-Web server
                        // FR: Renvoyer la requête de handshake au serveur Moonlight-Web cible
                        // EN: The target Moonlight-Web server expects the native /ws endpoint,
                        // not BatRun's relay route (/api/relay/ws?...). Rewriting the request
                        // line is mandatory or the remote WebSocket upgrade never reaches the
                        // Moonlight signaling server.
                        // FR: Le serveur Moonlight-Web distant attend le endpoint natif /ws,
                        // pas la route de relais BatRun (/api/relay/ws?...). Réécrire la ligne
                        // de requête est obligatoire sinon l'upgrade WebSocket n'atteint jamais
                        // le serveur de signalisation Moonlight.
                        string requestLine = $"{method} /ws HTTP/1.1\r\n";
                        byte[] lineBytes = Encoding.UTF8.GetBytes(requestLine);
                        await targetStream.WriteAsync(lineBytes, 0, lineBytes.Length);

                        foreach (var kv in headers)
                        {
                            // EN: Rebuild Host for the actual Moonlight-Web target and drop
                            // BatRun-specific relay query/path semantics.
                            // FR: Reconstruire Host pour la vraie cible Moonlight-Web et
                            // supprimer la sémantique de chemin/query du relais BatRun.
                            if (kv.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                            {
                                byte[] hostBytes = Encoding.UTF8.GetBytes($"Host: {targetIp}:{moonlightPort}\r\n");
                                await targetStream.WriteAsync(hostBytes, 0, hostBytes.Length);
                                continue;
                            }
                            if (kv.Key.Equals("Origin", StringComparison.OrdinalIgnoreCase))
                            {
                                string originScheme = (_serverCertificate != null) ? "https" : "http";
                                byte[] originBytes = Encoding.UTF8.GetBytes($"Origin: {originScheme}://{targetIp}:{moonlightPort}\r\n");
                                await targetStream.WriteAsync(originBytes, 0, originBytes.Length);
                                continue;
                            }
                            byte[] hBytes = Encoding.UTF8.GetBytes($"{kv.Key}: {kv.Value}\r\n");
                            await targetStream.WriteAsync(hBytes, 0, hBytes.Length);
                        }
                        await targetStream.WriteAsync(Encoding.UTF8.GetBytes("\r\n"), 0, 2);

                        // EN: Bridge raw binary traffic (WebSocket frames)
                        // FR: Ponter le trafic binaire brut (trames WebSocket)
                        var bridge1 = clientStream.CopyToAsync(targetStream);
                        var bridge2 = targetStream.CopyToAsync(clientStream);
                        
                        Task winner = await Task.WhenAny(bridge1, bridge2);
                        if (winner == bridge1)
                            _logger.LogInfo($"[Relay] Bridge closed: Target (Moonlight {targetAlias}) finished first.");
                        else
                            _logger.LogInfo("[Relay] Bridge closed: Client (Browser) finished first.");

                        if (bridge1.IsFaulted) _logger.LogWarning($"[Relay] TargetToClient error: {bridge1.Exception?.InnerException?.Message ?? bridge1.Exception?.Message ?? "Unknown"}");
                        if (bridge2.IsFaulted) _logger.LogWarning($"[Relay] ClientToTarget error: {bridge2.Exception?.InnerException?.Message ?? bridge2.Exception?.Message ?? "Unknown"}");

                        _logger.LogInfo("[Relay] WebSocket bridge closed. Triggering session cleanup...");
                        // EN: [BATRUN-FIX-SESSION] Try cancel on the target Sunshine, then conditionally restart local web-server
                        //     to clear stale session state if no active game or lobby is running.
                        // FR: [BATRUN-FIX-SESSION] Tenter l'annulation sur Sunshine cible, puis redémarrer le web-server
                        //     local sous condition si aucun jeu ou lobby n'est en cours.
                        _ = Task.Run(async () => {
                            try { await _manager.Moonlight.SendSunshineCancelAsync(targetIp); } catch { }
                            
                            bool isLobbyActive = false;
                            lock (_lobbyLock)
                            {
                                isLobbyActive = _lobby.Phase != "none";
                            }
                            
                            if (!_manager.IsGameInProgress && !isLobbyActive)
                            {
                                // EN: Restart both local Moonlight web-server and BatRun API services
                                // FR: Redémarrer à la fois le serveur web Moonlight local et les services d'API BatRun
                                await _manager.Moonlight.RestartWebServerAsync();
                                try { _ = _manager.RestartArcadeApiServicesAsync(3000); } catch { }
                            }
                            else
                            {
                                _logger.LogInfo("[Relay] WebSocket closed but game/lobby is still active. Postponing web-server restart to allow reconnection.");
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[Relay] WS Bridge error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// EN: A read-only stream wrapper that prepends a prefix buffer before the underlying stream.
    ///     Used for dual HTTP/HTTPS mode: after peeking the first byte(s) to detect TLS,
    ///     we need to "un-read" those bytes so the SslStream or StreamReader can read them again.
    /// FR: Un wrapper de flux en lecture seule qui préfixe un tampon avant le flux sous-jacent.
    ///     Utilisé pour le mode double HTTP/HTTPS : après avoir inspecté le(s) premier(s) octet(s)
    ///     pour détecter TLS, on doit "dé-lire" ces octets pour que SslStream ou StreamReader les relise.
    /// </summary>
    internal class PrefixedStream : Stream
    {
        private readonly byte[] _prefix;
        private int _prefixLength;
        private int _prefixOffset;
        private readonly Stream _inner;

        public PrefixedStream(byte[] prefix, int prefixLength, Stream inner)
        {
            _prefix = prefix;
            _prefixLength = prefixLength;
            _prefixOffset = 0;
            _inner = inner;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int remaining = _prefixLength - _prefixOffset;
            if (remaining > 0)
            {
                int toCopy = Math.Min(remaining, count);
                Buffer.BlockCopy(_prefix, _prefixOffset, buffer, offset, toCopy);
                _prefixOffset += toCopy;
                return toCopy;
            }
            return _inner.Read(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int remaining = _prefixLength - _prefixOffset;
            if (remaining > 0)
            {
                int toCopy = Math.Min(remaining, count);
                Buffer.BlockCopy(_prefix, _prefixOffset, buffer, offset, toCopy);
                _prefixOffset += toCopy;
                return toCopy;
            }
            return await _inner.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _inner.WriteAsync(buffer, offset, count, cancellationToken);
        public override void Flush() => _inner.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}


