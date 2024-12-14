using System;
using System.Windows.Forms;
using SDL2;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;

namespace BatRun
{
    public partial class MappingConfigurationForm : Form
    {
        private ButtonMapping buttonMapping;
        private IntPtr? currentJoystick;
        private string? currentJoystickName;
        private string? currentDeviceGuid;
        private readonly Logger logger = new Logger("BatRun.log");

        public MappingConfigurationForm(ButtonMapping mapping)
        {
            buttonMapping = mapping;
            InitializeComponent();
            
            // Appliquer le style sombre
            FormStyles.ApplyDarkStyle(this);
            
            RefreshJoystickList();
        }

        private void RefreshJoystickList()
        {
            comboBoxJoysticks.Items.Clear();
            int numJoysticks = SDL.SDL_NumJoysticks();
            
            for (int i = 0; i < numJoysticks; i++)
            {
                string joystickName = SDL.SDL_JoystickNameForIndex(i);
                if (!string.IsNullOrEmpty(joystickName))
                {
                    comboBoxJoysticks.Items.Add(joystickName);
                }
            }

            if (comboBoxJoysticks.Items.Count > 0)
            {
                comboBoxJoysticks.SelectedIndex = 0;
            }
        }

        private void comboBoxJoysticks_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (currentJoystick.HasValue)
            {
                SDL.SDL_JoystickClose(currentJoystick.Value);
                currentJoystick = null;
            }

            if (comboBoxJoysticks.SelectedIndex >= 0)
            {
                currentJoystickName = comboBoxJoysticks.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(currentJoystickName))
                {
                    for (int i = 0; i < SDL.SDL_NumJoysticks(); i++)
                    {
                        if (SDL.SDL_JoystickNameForIndex(i) == currentJoystickName)
                        {
                            currentJoystick = SDL.SDL_JoystickOpen(i);
                            if (currentJoystick != IntPtr.Zero)
                            {
                                currentDeviceGuid = SDL.SDL_JoystickGetGUID(currentJoystick.Value).ToString();
                                
                                // Charger les mappings existants
                                var controllerConfig = buttonMapping.Controllers.FirstOrDefault(c => c.JoystickName == currentJoystickName);
                                if (controllerConfig != null && controllerConfig.Mappings != null)
                                {
                                    if (controllerConfig.Mappings.TryGetValue("Hotkey", out string? hotkeyValue))
                                    {
                                        textBoxHotkey.Text = hotkeyValue;
                                    }
                                    else
                                    {
                                        textBoxHotkey.Text = "";
                                    }
                                    
                                    if (controllerConfig.Mappings.TryGetValue("StartButton", out string? startValue))
                                    {
                                        textBoxStart.Text = startValue;
                                    }
                                    else
                                    {
                                        textBoxStart.Text = "";
                                    }
                                }
                                else
                                {
                                    textBoxHotkey.Text = "";
                                    textBoxStart.Text = "";
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentJoystickName))
            {
                MessageBox.Show("Please select a joystick first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(textBoxHotkey.Text) || string.IsNullOrEmpty(textBoxStart.Text))
            {
                MessageBox.Show("Please configure both Hotkey and Start button.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                buttonMapping.SaveMappings(currentJoystickName, currentDeviceGuid ?? "", textBoxHotkey.Text, textBoxStart.Text);
                MessageBox.Show("Mappings saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving mappings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void buttonResetCurrent_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentJoystickName))
            {
                MessageBox.Show("Please select a controller first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to reset the mapping for {currentJoystickName}?\nThis will restore the default mapping for this controller.",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    // Supprimer la configuration de la manette actuelle
                    var controllerToRemove = buttonMapping.Controllers.FirstOrDefault(c => c.JoystickName == currentJoystickName);
                    if (controllerToRemove != null)
                    {
                        buttonMapping.Controllers.Remove(controllerToRemove);
                        
                        // Sauvegarder le fichier JSON sans la configuration de cette manette
                        string exePath = AppContext.BaseDirectory;
                        string parentPath = Path.GetDirectoryName(exePath) ?? exePath;
                        string jsonPath = Path.Combine(parentPath, "buttonMappings.json");
                        var json = JsonConvert.SerializeObject(buttonMapping, Formatting.Indented);
                        File.WriteAllText(jsonPath, json);
                    }
                    
                    // Réinitialiser les champs
                    textBoxHotkey.Text = "";
                    textBoxStart.Text = "";
                    
                    MessageBox.Show(
                        $"Mapping for {currentJoystickName} has been reset.\nDefault mapping will be used.",
                        "Reset Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error resetting controller mapping: {ex.Message}", ex);
                    MessageBox.Show(
                        $"Error resetting controller mapping: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void buttonResetAll_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset ALL controller mappings?\nThis will restore default mappings for all controllers.",
                "Confirm Reset All",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    // Vider la liste des contrôleurs
                    buttonMapping.Controllers.Clear();

                    // Sauvegarder le fichier JSON vide (juste la structure)
                    string exePath = AppContext.BaseDirectory;
                    string parentPath = Path.GetDirectoryName(exePath) ?? exePath;
                    string jsonPath = Path.Combine(parentPath, "buttonMappings.json");
                    var json = JsonConvert.SerializeObject(buttonMapping, Formatting.Indented);
                    File.WriteAllText(jsonPath, json);
                    
                    // Réinitialiser les champs
                    textBoxHotkey.Text = "";
                    textBoxStart.Text = "";
                    
                    MessageBox.Show(
                        "All controller mappings have been reset.\nDefault mappings will be used for all controllers.",
                        "Reset Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error resetting all controller mappings: {ex.Message}", ex);
                    MessageBox.Show(
                        $"Error resetting all controller mappings: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (currentJoystick.HasValue)
            {
                SDL.SDL_JoystickClose(currentJoystick.Value);
            }
        }

        private void buttonDetectHotkey_Click(object sender, EventArgs e)
        {
            DetectButton(textBoxHotkey);
        }

        private void buttonDetectStart_Click(object sender, EventArgs e)
        {
            DetectButton(textBoxStart);
        }

        private void DetectButton(TextBox targetTextBox)
        {
            if (!currentJoystick.HasValue)
            {
                MessageBox.Show("Please select a joystick first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            targetTextBox.Text = "Press a button...";
            targetTextBox.Refresh();

            DateTime startTime = DateTime.Now;
            bool buttonDetected = false;

            while ((DateTime.Now - startTime).TotalSeconds < 5 && !buttonDetected)
            {
                SDL.SDL_JoystickUpdate();
                
                for (int i = 0; i < SDL.SDL_JoystickNumButtons(currentJoystick.Value); i++)
                {
                    if (SDL.SDL_JoystickGetButton(currentJoystick.Value, i) == 1)
                    {
                        targetTextBox.Text = $"Button {i}";
                        buttonDetected = true;
                        break;
                    }
                }

                Application.DoEvents();
                System.Threading.Thread.Sleep(50);
            }

            if (!buttonDetected)
            {
                targetTextBox.Text = "";
                MessageBox.Show("No button detected. Please try again.", "Timeout", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
