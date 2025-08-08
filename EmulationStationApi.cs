using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BatRun
{
    // Data models adapted from user-provided code
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
        public string? Path { get; set; }
    }

    public class EmulationStationApi
    {
        private readonly HttpClient httpClient;
        private readonly Logger logger;
        private const string BaseUrl = "http://localhost:1234";

        public EmulationStationApi(Logger logger)
        {
            this.logger = logger;
            httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        }

        public async Task<bool> IsApiAvailableAsync()
        {
            try
            {
                var response = await httpClient.GetAsync($"{BaseUrl}/systems");
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                // Logged as Info because this is expected when ES is not running
                logger.LogInfo("EmulationStation API is not available.");
                return false;
            }
        }

        public async Task<List<SystemInfo>> GetSystemsAsync()
        {
            try
            {
                var response = await httpClient.GetAsync($"{BaseUrl}/systems");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var systems = JsonConvert.DeserializeObject<List<SystemInfo>>(content);
                return systems ?? new List<SystemInfo>();
            }
            catch (Exception ex)
            {
                logger.LogError($"Error getting systems from ES API: {ex.Message}", ex);
                return new List<SystemInfo>();
            }
        }

        public async Task<List<Game>> GetGamesForSystemAsync(string systemName)
        {
            try
            {
                var response = await httpClient.GetAsync($"{BaseUrl}/systems/{systemName}/games");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var games = JsonConvert.DeserializeObject<List<Game>>(content);
                if (games != null)
                {
                    foreach (var game in games)
                    {
                        game.System = systemName;
                    }
                }
                return games ?? new List<Game>();
            }
            catch (Exception ex)
            {
                logger.LogError($"Error getting games for system {systemName} from ES API: {ex.Message}", ex);
                return new List<Game>();
            }
        }

        public async Task<bool> LaunchGameAsync(Game game)
        {
            if (game?.Path == null)
            {
                logger.LogError("Cannot launch game, game path is null.");
                return false;
            }

            try
            {
                logger.LogInfo($"Attempting to launch game: {game.Name} on {game.System}");
                var content = new StringContent(game.Path, Encoding.UTF8, "text/plain");
                var response = await httpClient.PostAsync($"{BaseUrl}/launch", content);

                if (response.IsSuccessStatusCode)
                {
                    logger.LogInfo($"Successfully sent launch command for {game.Name}.");
                    return true;
                }
                else
                {
                    logger.LogError($"Failed to launch game {game.Name}. Status: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error launching game {game.Name}: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<List<Game>> GetAllGamesAsync()
        {
            var allGames = new List<Game>();
            var systems = await GetSystemsAsync();

            logger.LogInfo($"Found {systems.Count} systems. Fetching all games.");
            foreach (var system in systems)
            {
                if (system.totalGames > 0 && system.name != null)
                {
                    var games = await GetGamesForSystemAsync(system.name);
                    allGames.AddRange(games);
                }
            }
            logger.LogInfo($"Total games found: {allGames.Count}");
            return allGames;
        }
    }
}
