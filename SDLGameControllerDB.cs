using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SDL2;

namespace BatRun
{
    public class SDLGameControllerDB
    {
        private readonly Dictionary<string, SDLControllerMapping> mappings = new Dictionary<string, SDLControllerMapping>();
        private readonly Logger logger;

        public SDLGameControllerDB(Logger logger)
        {
            this.logger = logger;
        }

        public void LoadDatabase(string retrobatDir)
        {
            // The parameter is now the directory, not the full exe path.
            string dbPath = Path.Combine(retrobatDir, "system", "tools", "gamecontrollerdb.txt");
            logger.LogInfo($"Looking for SDL controller database at: {dbPath}");

            if (!File.Exists(dbPath))
            {
                logger.LogError($"SDL controller database not found at: {dbPath}");
                return;
            }

            try
            {
                logger.LogInfo($"Loading SDL controller database from: {dbPath}");
                var lines = File.ReadAllLines(dbPath);
                logger.LogInfo($"Found {lines.Length} lines in database");

                int validMappings = 0;
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    {
                        continue;
                    }

                    try
                    {
                        var mapping = ParseMappingLine(line);
                        if (mapping != null)
                        {
                            mappings[mapping.Guid] = mapping;
                            validMappings++;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error parsing mapping line: {line}", ex);
                    }
                }
                logger.LogInfo($"Successfully loaded {validMappings} valid mappings from database");
            }
            catch (Exception ex)
            {
                logger.LogError("Error loading SDL controller database", ex);
            }
        }

        private SDLControllerMapping? ParseMappingLine(string line)
        {
            try
            {
                // Format: GUID,Name,Mapping1:Value1,Mapping2:Value2,...
                var firstComma = line.IndexOf(',');
                if (firstComma == -1) return null;

                var secondComma = line.IndexOf(',', firstComma + 1);
                if (secondComma == -1) return null;

                var guid = line.Substring(0, firstComma);
                var name = line.Substring(firstComma + 1, secondComma - firstComma - 1);
                var mappingPart = line.Substring(secondComma + 1);

                var buttonMappings = new Dictionary<string, int>();
                var mappings = mappingPart.Split(',');

                foreach (var mapping in mappings)
                {
                    var parts = mapping.Split(':');
                    if (parts.Length == 2)
                    {
                        var buttonName = parts[0].Trim();
                        var buttonValue = parts[1].Trim();

                        // On ne s'intéresse qu'aux boutons "back" et "start"
                        if ((buttonName == "back" || buttonName == "start") && buttonValue.StartsWith("b"))
                        {
                            if (int.TryParse(buttonValue.Substring(1), out int buttonNumber))
                            {
                                buttonMappings[buttonName] = buttonNumber;
                            }
                        }
                    }
                }

                if (buttonMappings.Count > 0)
                {
                    return new SDLControllerMapping
                    {
                        Guid = guid,
                        Name = name,
                        ButtonMappings = buttonMappings
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error parsing line: {line}", ex);
                return null;
            }
        }

        public SDLControllerMapping? FindMappingByGuid(string guid)
        {
            // Convertir le GUID au format SDL si nécessaire
            string sdlGuid = ConvertToSDLGuid(guid);
            
            // Chercher d'abord par GUID exact
            if (mappings.TryGetValue(sdlGuid, out var mapping))
            {
                logger.LogInfo($"Found mapping for controller: {mapping.Name}");
                return mapping;
            }

            // Si pas trouvé, chercher en ignorant la casse
            var matchingMapping = mappings.Values.FirstOrDefault(m => 
                m.Guid.Equals(sdlGuid, StringComparison.OrdinalIgnoreCase));

            if (matchingMapping != null)
            {
                logger.LogInfo($"Found mapping for controller: {matchingMapping.Name}");
                return matchingMapping;
            }

            logger.LogInfo($"No mapping found for GUID: {sdlGuid}");
            return null;
        }

        public SDLControllerMapping? FindMappingByName(string name)
        {
            // Chercher d'abord par nom exact
            var exactMatch = mappings.Values.FirstOrDefault(m => 
                m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
            {
                logger.LogInfo($"Found mapping by exact name match: {exactMatch.Name}");
                return exactMatch;
            }

            // Si pas trouvé, chercher par contenance
            var containsMatch = mappings.Values.FirstOrDefault(m => 
                m.Name.Contains(name, StringComparison.OrdinalIgnoreCase) || 
                name.Contains(m.Name, StringComparison.OrdinalIgnoreCase));

            if (containsMatch != null)
            {
                logger.LogInfo($"Found mapping by partial name match: {containsMatch.Name}");
                return containsMatch;
            }

            logger.LogInfo($"No mapping found for name: {name}");
            return null;
        }

        private string ConvertToSDLGuid(string windowsGuid)
        {
            try
            {
                // Nettoyer le GUID Windows (enlever les tirets)
                string cleanGuid = windowsGuid.Replace("-", "").ToLower();

                // Si c'est déjà un GUID SDL (32 caractères hex), le retourner tel quel
                if (cleanGuid.Length == 32 && cleanGuid.All(c => "0123456789abcdef".Contains(c)))
                {
                    return cleanGuid;
                }

                // Format Windows GUID: ff880003-045e-0000-2600-000000000000
                // Format SDL désiré:   030000005e0400002600000000000000
                // On remplace juste les 8 premiers caractères par 03000000
                string sdlGuid = "03000000" + cleanGuid.Substring(8);
                return sdlGuid;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error converting GUID {windowsGuid}: {ex.Message}");
                return windowsGuid;
            }
        }

        private string ReverseHexPairs(string hex)
        {
            // Inverse les paires d'octets (ex: "045e" devient "5e04")
            if (hex.Length % 2 != 0) return hex;
            return string.Concat(
                Enumerable.Range(0, hex.Length / 2)
                    .Select(i => hex.Substring(hex.Length - 2 - (i * 2), 2))
            );
        }

        public void AddMapping(string guid, string name, Dictionary<string, int> buttonMappings)
        {
            var mapping = new SDLControllerMapping
            {
                Guid = guid,
                Name = name,
                ButtonMappings = buttonMappings
            };
            mappings[guid] = mapping;
            logger.LogInfo($"Added mapping for controller: {name} (GUID: {guid})");
        }

        public void LogAllMappings()
        {
            // Cette méthode ne fait plus rien pour éviter les logs inutiles
        }
    }

    public class SDLControllerMapping
    {
        public string Guid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, int> ButtonMappings { get; set; } = new Dictionary<string, int>();

        public bool HasBackAndStartButtons => 
            ButtonMappings.ContainsKey("back") && ButtonMappings.ContainsKey("start");
    }
} 