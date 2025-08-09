using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace BatRun
{
    public class GamelistParser
    {
        private readonly string _retrobatBasePath;
        private readonly Logger _logger;

        public GamelistParser(string retrobatExePath, Logger logger)
        {
            _retrobatBasePath = Path.GetDirectoryName(retrobatExePath) ?? string.Empty;
            _logger = logger;
        }

        public Dictionary<string, string> GetGameMetadata(string systemName, string gameRomPath)
        {
            var metadata = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(_retrobatBasePath) || string.IsNullOrEmpty(systemName) || string.IsNullOrEmpty(gameRomPath))
            {
                return metadata;
            }

            try
            {
                string gamelistPath = Path.Combine(_retrobatBasePath, "roms", systemName, "gamelist.xml");
                _logger.LogInfo($"Attempting to read metadata from: {gamelistPath}");

                if (!File.Exists(gamelistPath))
                {
                    _logger.LogWarning($"Gamelist file not found at: {gamelistPath}");
                    return metadata;
                }

                XDocument doc = XDocument.Load(gamelistPath);

                var gameElement = doc.Descendants("game")
                                     .FirstOrDefault(g => g.Element("path")?.Value == gameRomPath);

                if (gameElement != null)
                {
                    foreach (var element in gameElement.Elements())
                    {
                        metadata[element.Name.LocalName] = element.Value;
                    }
                    _logger.LogInfo($"Successfully found metadata for game: {gameRomPath}");
                }
                else
                {
                    _logger.LogWarning($"Could not find game entry for '{gameRomPath}' in {gamelistPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing gamelist.xml: {ex.Message}", ex);
            }

            return metadata;
        }
    }
}
