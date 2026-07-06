using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using BatRun.Utils;

namespace BatRun.Core
{
    public class PublicUser
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public string Token { get; set; } = ""; // Active session token
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    }

    public class UserManager
    {
        private readonly string _storagePath;
        private List<PublicUser> _users = new();
        private readonly Logger _logger;
        private readonly Dictionary<string, string> _invalidatedTokens = new(); // token -> username map for logging purposes

        public UserManager(Logger logger, string storageDirectory)
        {
            _logger = logger;
            _storagePath = Path.Combine(storageDirectory, "PublicUsers.json");
            LoadUsers();
        }

        private void LoadUsers()
        {
            try
            {
                if (File.Exists(_storagePath))
                {
                    string json = File.ReadAllText(_storagePath);
                    _users = JsonSerializer.Deserialize<List<PublicUser>>(json) ?? new List<PublicUser>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to load users", ex);
                _users = new List<PublicUser>();
            }
        }

        private void SaveUsers()
        {
            try
            {
                string json = JsonSerializer.Serialize(_users, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_storagePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to save users", ex);
            }
        }

        public PublicUser? RegisterUser(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return null;

            if (_users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                return null; // Username already exists
            }

            var user = new PublicUser
            {
                Username = username,
                PasswordHash = HashPassword(password),
                Status = "Pending"
            };

            _users.Add(user);
            SaveUsers();
            return user;
        }

        public PublicUser? AuthenticateLogin(string username, string password)
        {
            var user = _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (user != null && VerifyPassword(password, user.PasswordHash))
            {
                if (user.Status == "Approved")
                {
                    // EN: Invalidate previous session for this account (Single connection limit)
                    // FR: Invalider la session précédente pour ce compte (limite d'une seule connexion active)
                    if (!string.IsNullOrEmpty(user.Token))
                    {
                        _invalidatedTokens[user.Token] = user.Username + " (session replaced by new login)";
                        if (_invalidatedTokens.Count > 100) _invalidatedTokens.Remove(_invalidatedTokens.Keys.First());
                    }

                    // [BATRUN-SEC]: Auto-upgrade legacy SHA256 hashes to PBKDF2 on successful login
                    // FR: Migration automatique des anciens hash SHA256 vers PBKDF2 lors d'un login réussi
                    if (!user.PasswordHash.Contains(':'))
                    {
                        user.PasswordHash = HashPassword(password);
                        _logger.LogInfo($"[Security] Auto-upgraded password hash for user '{user.Username}' from SHA256 to PBKDF2.");
                    }

                    user.Token = Guid.NewGuid().ToString("N"); // Generate new session token
                    SaveUsers();
                    return user;
                }
            }
            return null; // Login failed or not approved
        }

        public bool ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return false;
            
            var user = _users.FirstOrDefault(u => u.Token == token && u.Status == "Approved");
            if (user != null)
            {
                user.LastActivity = DateTime.UtcNow;
                return true;
            }

            // EN: If not found, check if it was recently invalidated for better logging
            // FR: Si non trouvé, vérifier s'il a été récemment invalidé pour de meilleurs logs
            if (_invalidatedTokens.TryGetValue(token, out string? username))
            {
                _logger.LogWarning($"[Security] Access attempt with invalidated token belonging to user '{username}'.");
            }
            
            return false;
        }

        public PublicUser? GetUserByToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            return _users.FirstOrDefault(u => u.Token == token && u.Status == "Approved");
        }

        public List<PublicUser> GetAllUsers() => _users.ToList();

        public bool ApproveUser(string id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                user.Status = "Approved";
                SaveUsers();
                return true;
            }
            return false;
        }

        public bool RejectUser(string id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                // EN: Keep track of token -> username mapping before removal for logging
                // FR: Garder trace du mapping token -> username avant suppression pour les logs
                if (!string.IsNullOrEmpty(user.Token))
                {
                    _invalidatedTokens[user.Token] = user.Username;
                    // EN: Limit cache size to 100 entries / FR: Limiter la taille du cache à 100 entrées
                    if (_invalidatedTokens.Count > 100) _invalidatedTokens.Remove(_invalidatedTokens.Keys.First());
                }

                user.Status = "Rejected";
                _users.Remove(user);
                SaveUsers();
                return true;
            }
            return false;
        }

        public PublicUser? LoginGuest(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) identifier = "generic";
            
            // EN: Prune stale guests before creating/finding new ones to keep the JSON clean
            // FR: Nettoyer les invités expirés avant d'en créer/trouver de nouveaux pour garder le JSON propre
            PruneGuestAccounts();

            string guestUsername = $"Guest_{identifier}";
            var user = _users.FirstOrDefault(u => u.Username.Equals(guestUsername, StringComparison.OrdinalIgnoreCase));
            
            if (user == null)
            {
                user = new PublicUser
                {
                    Username = guestUsername,
                    PasswordHash = HashPassword("Guest"), // dummy
                    Status = "Approved",
                    CreatedAt = DateTime.UtcNow
                };
                _users.Add(user);
            }
            
            user.Status = "Approved"; // Ensure approved
            user.Token = Guid.NewGuid().ToString("N");
            user.LastActivity = DateTime.UtcNow;
            
            SaveUsers();
            return user;
        }

        public void PruneGuestAccounts()
        {
            try
            {
                // EN: Remove Guest accounts with no activity for more than 1 hour
                // FR: Supprimer les comptes Guest sans activité depuis plus d'une heure
                var now = DateTime.UtcNow;
                int removed = _users.RemoveAll(u => u.Username.StartsWith("Guest_", StringComparison.OrdinalIgnoreCase) 
                                                 && (now - u.LastActivity).TotalHours > 1);
                if (removed > 0)
                {
                    _logger.LogInfo($"[Security] Pruned {removed} stale guest accounts.");
                    SaveUsers();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[Security] Error pruning guest accounts: {ex.Message}");
            }
        }

        // EN: Force-remove ALL guest accounts immediately (called when requireLogin switches to true)
        // FR: Supprimer immédiatement TOUS les comptes guest (appelé quand requireLogin passe à true)
        public int PurgeAllGuests()
        {
            var guests = _users.Where(u => u.Username.StartsWith("Guest_", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var g in guests)
            {
                if (!string.IsNullOrEmpty(g.Token))
                {
                    _invalidatedTokens[g.Token] = g.Username + " (purged: requireLogin enabled)";
                    if (_invalidatedTokens.Count > 100) _invalidatedTokens.Remove(_invalidatedTokens.Keys.First());
                }
                _users.Remove(g);
            }
            if (guests.Count > 0)
            {
                _logger.LogInfo($"[Security] Purged {guests.Count} guest account(s) because requireLogin was enabled.");
                SaveUsers();
            }
            return guests.Count;
        }

        // EN: Logout user by token — invalidates the session and auto-deletes guest accounts
        // FR: Deconnecter l'utilisateur par token — invalide la session et supprime automatiquement les comptes guest
        public bool LogoutUser(string token)
        {
            if (string.IsNullOrEmpty(token)) return false;

            var user = _users.FirstOrDefault(u => u.Token == token);
            if (user == null) return false;

            string username = user.Username;
            bool isGuest = username.StartsWith("Guest_", StringComparison.OrdinalIgnoreCase);

            // EN: Invalidate the token for logging purposes
            // FR: Invalider le token pour les logs
            _invalidatedTokens[token] = username + " (logout)";
            if (_invalidatedTokens.Count > 100) _invalidatedTokens.Remove(_invalidatedTokens.Keys.First());

            if (isGuest)
            {
                // EN: Guest accounts are ephemeral — delete entirely on logout
                // FR: Les comptes guest sont ephemeres — supprimer completement a la deconnexion
                _users.Remove(user);
                _logger.LogInfo($"[Security] Guest account '{username}' deleted on logout.");
            }
            else
            {
                // EN: Regular accounts — just clear the session token
                // FR: Comptes reguliers — juste effacer le token de session
                user.Token = "";
            }

            SaveUsers();
            return true;
        }

        public int GetPendingUsersCount()
        {
            return _users.Count(u => u.Status == "Pending");
        }

        // [BATRUN-SEC]: PBKDF2-SHA256 password hashing with random salt (100k iterations)
        // EN: Generates a secure password hash in format "base64salt:base64hash"
        // FR: Génère un hash de mot de passe sécurisé au format "base64salt:base64hash"
        private string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) { rng.GetBytes(salt); }
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
        }

        // [BATRUN-SEC]: Verify password against stored hash (supports legacy SHA256 and new PBKDF2)
        // EN: If storedHash contains ':', it's PBKDF2 format. Otherwise, fall back to legacy SHA256.
        // FR: Si storedHash contient ':', c'est le format PBKDF2. Sinon, repli sur l'ancien SHA256.
        private bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return false;

            if (storedHash.Contains(':'))
            {
                // EN: New PBKDF2 format — "base64salt:base64hash"
                // FR: Nouveau format PBKDF2 — "base64salt:base64hash"
                var parts = storedHash.Split(':', 2);
                if (parts.Length != 2) return false;
                try
                {
                    byte[] salt = Convert.FromBase64String(parts[0]);
                    byte[] expectedHash = Convert.FromBase64String(parts[1]);
                    byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
                    return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
                }
                catch { return false; }
            }
            else
            {
                // EN: Legacy SHA256 format (no salt) — for backward compatibility only
                // FR: Ancien format SHA256 (sans salt) — rétrocompatibilité uniquement
                using (var sha256 = SHA256.Create())
                {
                    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                    string legacyHash = Convert.ToHexString(bytes).ToLower();
                    return string.Equals(legacyHash, storedHash, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }
}
