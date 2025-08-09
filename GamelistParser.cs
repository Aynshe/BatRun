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

        public GamelistParser(string retrobatExePath)
        {
            _retrobatBasePath = Path.GetDirectoryName(retrobatExePath) ?? string.Empty;
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

                if (!File.Exists(gamelistPath))
                {
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
                }
            }
            catch (Exception ex)
            {
                // In a real app, we'd log this error. For now, we just return empty metadata.
                Console.WriteLine($"Error parsing gamelist.xml: {ex.Message}");
            }

            return metadata;
        }
    }
}
