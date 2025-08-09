using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Linq;

namespace BatRun
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
        public string? PlayUrl { get; set; }
        public string? Path { get; set; }
    }

    public class EmulationStationScraper
    {
        private readonly HttpClient httpClient;
        private string baseUrl;

        public EmulationStationScraper(string ipAddress = "127.0.0.1")
        {
            baseUrl = $"http://{ipAddress}:1234";
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<List<SystemInfo>> GetSystemsAsync()
        {
            try
            {
                var response = await httpClient.GetAsync($"{baseUrl}/systems");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var systems = JsonConvert.DeserializeObject<List<SystemInfo>>(content);
                return systems ?? new List<SystemInfo>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération des systèmes : {ex.Message}");
                return new List<SystemInfo>();
            }
        }

        public async Task<List<Game>> GetGamesForSystemAsync(string systemName)
        {
            var games = new List<Game>();
            try
            {
                var response = await httpClient.GetAsync($"{baseUrl}/systems/{systemName}/games");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var systemGames = JsonConvert.DeserializeObject<List<Game>>(content);
                        if (systemGames != null && systemGames.Any())
                        {
                            foreach (var game in systemGames)
                            {
                                game.System = systemName;
                                if (!string.IsNullOrEmpty(game.Id))
                                {
                                    game.PlayUrl = $"{baseUrl}/systems/{systemName}/games/{game.Id}/play";
                                }
                            }
                            games.AddRange(systemGames);
                        }
                    }
                    catch (JsonException)
                    {
                        Console.WriteLine($"L'endpoint ne retourne pas le format attendu pour {systemName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur pour {systemName}: {ex.Message}");
            }

            return games;
        }

        public async Task<List<Game>> GetAllGamesAsync()
        {
            var allGames = new List<Game>();
            var systems = await GetSystemsAsync();

            foreach (var system in systems.Where(s => s.totalGames > 0))
            {
                var games = await GetGamesForSystemAsync(system.name ?? "");
                allGames.AddRange(games);
            }

            return allGames;
        }

        public async Task<bool> LaunchGameAsync(string gamePath)
        {
            try
            {
                var launchResponse = await httpClient.PostAsync($"{baseUrl}/launch",
                    new StringContent(gamePath, Encoding.UTF8, "text/plain"));

                return launchResponse.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors du lancement du jeu : {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PingServerAsync()
        {
            try
            {
                var response = await httpClient.GetAsync($"{baseUrl}/ping");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
