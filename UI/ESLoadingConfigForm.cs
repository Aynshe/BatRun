using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;

using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.UI
{
    // EN: Configuration form for the ESLoading video player settings.
    // FR: Formulaire de configuration pour les paramètres du lecteur vidéo ESLoading.
    public partial class ESLoadingConfigForm : Form
    {
        private static readonly string[] SupportedExtensions = { ".mp4" };
        private readonly IniFile config;
        private readonly Logger logger;

        public ESLoadingConfigForm(IniFile config, Logger logger)
        {
            this.config = config;
            this.logger = logger;

            InitializeComponent();
            ApplyLocalization();

            LoadSettings();
            LoadVideos();
        }

        // EN: Apply translated strings to the UI controls.
        // FR: Appliquer les chaînes traduites aux contrôles de l'interface.
        private void ApplyLocalization()
        {
            this.Text                  = LocalizedStrings.GetString("MediaPlayer Settings");
            this.labelVideos.Text      = LocalizedStrings.GetString("Available videos:");
            this.checkBoxLoop.Text     = LocalizedStrings.GetString("Loop playback");
            this.checkBoxMuteAfterFirst.Text = LocalizedStrings.GetString("Mute after first playback");
            this.checkBoxMuteAll.Text  = LocalizedStrings.GetString("Mute all audio");
            this.buttonOK.Text         = LocalizedStrings.GetString("OK");
            this.buttonCancel.Text     = LocalizedStrings.GetString("Cancel");
        }

        private void CheckBoxLoop_CheckedChanged(object? sender, EventArgs e)
        {
            checkBoxMuteAfterFirst.Enabled = checkBoxLoop.Checked;
            if (!checkBoxLoop.Checked)
            {
                checkBoxMuteAfterFirst.Checked = false;
            }
        }

        private void LoadVideos()
        {
            try
            {
                string videoPath = Path.Combine(AppContext.BaseDirectory, "ESloading");
                if (Directory.Exists(videoPath))
                {
                    var videos = Directory.GetFiles(videoPath, "*.*")
                        .Where(file => SupportedExtensions.Contains(Path.GetExtension(file).ToLower()))
                        .Select(Path.GetFileName)
                        .Where(name => name != null)
                        .Cast<object>()
                        .ToArray();

                    comboBoxVideos.Items.Clear();
                    comboBoxVideos.Items.Add("None");
                    comboBoxVideos.Items.AddRange(videos);

                    string selectedVideo = config.ReadValue("Windows", "ESLoadingVideo", "None");
                    comboBoxVideos.SelectedItem = comboBoxVideos.Items.Contains(selectedVideo) ? selectedVideo : "None";
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error loading videos: {ex.Message}");
            }
        }

        private void LoadSettings()
        {
            checkBoxLoop.Checked           = config.ReadValue("Windows", "ESLoadingVideoLoop", "false") == "true";
            checkBoxMuteAfterFirst.Checked = config.ReadValue("Windows", "ESLoadingVideoMuteAfterFirst", "false") == "true";
            checkBoxMuteAll.Checked        = config.ReadValue("Windows", "ESLoadingVideoMuteAll", "false") == "true";
        }

        public void SaveSettings()
        {
            if (DialogResult == DialogResult.OK)
            {
                config.WriteValue("Windows", "ESLoadingVideo", comboBoxVideos.SelectedItem?.ToString() ?? "None");
                config.WriteValue("Windows", "ESLoadingVideoLoop", checkBoxLoop.Checked.ToString().ToLower());
                config.WriteValue("Windows", "ESLoadingVideoMuteAfterFirst", checkBoxMuteAfterFirst.Checked.ToString().ToLower());
                config.WriteValue("Windows", "ESLoadingVideoMuteAll", checkBoxMuteAll.Checked.ToString().ToLower());
            }
        }
    }
}

