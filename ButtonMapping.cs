using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SDL2;
using Newtonsoft.Json;

namespace BatRun
{
    public class ButtonMapping
    {
        public List<ControllerConfig> Controllers { get; set; } = [];

        public void LoadMappings(string jsonPath)
        {
            if (File.Exists(jsonPath))
            {
                var jsonData = File.ReadAllText(jsonPath);
                var mapping = JsonConvert.DeserializeObject<ButtonMapping>(jsonData);
                if (mapping != null)
                {
                    Controllers = mapping.Controllers;
                }
            }
        }

        public void SaveMappings(string joystickName, string deviceGuid, string hotkeyButton, string startButton)
        {
            var existingController = Controllers.FirstOrDefault(c => 
                c.JoystickName == joystickName && c.DeviceGuid == deviceGuid);

            if (existingController != null)
            {
                existingController.Mappings["Hotkey"] = hotkeyButton;
                existingController.Mappings["StartButton"] = startButton;
            }
            else
            {
                Controllers.Add(new ControllerConfig
                {
                    JoystickName = joystickName,
                    DeviceGuid = deviceGuid,
                    Mappings = new Dictionary<string, string>
                    {
                        { "Hotkey", hotkeyButton },
                        { "StartButton", startButton }
                    }
                });
            }

            string exePath = AppContext.BaseDirectory;
            string parentPath = Path.GetDirectoryName(exePath) ?? exePath;
            string jsonPath = Path.Combine(parentPath, "buttonMappings.json");
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(jsonPath, json);
        }
    }

    public class ControllerConfig
    {
        public string JoystickName { get; set; } = string.Empty;
        public string DeviceGuid { get; set; } = string.Empty;
        public Dictionary<string, string> Mappings { get; set; } = [];
    }
}