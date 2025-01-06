using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;

namespace BatRun
{
    public partial class ESLoadingConfigForm : Form
    {
        private static readonly string[] SupportedExtensions = { ".mp4" };
        private readonly IniFile config;
        private readonly Logger logger;
        private readonly ComboBox comboBoxVideos;
        private readonly CheckBox checkBoxLoop;
        private readonly CheckBox checkBoxMuteAfterFirst;
        private readonly CheckBox checkBoxMuteAll;
        private readonly Button buttonOK;
        private readonly Button buttonCancel;

        public ESLoadingConfigForm(IniFile config, Logger logger)
        {
            this.config = config;
            this.logger = logger;

            Text = LocalizedStrings.GetString("MediaPlayer Settings");
            Size = new Size(450, 300);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(28, 28, 28);
            ForeColor = Color.White;

            // Video list
            var labelVideos = new Label
            {
                Text = LocalizedStrings.GetString("Available videos:"),
                Location = new Point(10, 10),
                AutoSize = true,
                ForeColor = Color.White
            };
            Controls.Add(labelVideos);

            comboBoxVideos = new ComboBox
            {
                Location = new Point(10, 30),
                Size = new Size(415, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            Controls.Add(comboBoxVideos);

            // Playback options
            checkBoxLoop = new CheckBox
            {
                Text = LocalizedStrings.GetString("Loop playback"),
                Location = new Point(10, 70),
                AutoSize = true,
                ForeColor = Color.White
            };
            checkBoxLoop.CheckedChanged += CheckBoxLoop_CheckedChanged!;
            Controls.Add(checkBoxLoop);

            checkBoxMuteAfterFirst = new CheckBox
            {
                Text = LocalizedStrings.GetString("Mute after first playback"),
                Location = new Point(10, 95),
                AutoSize = true,
                Enabled = false,
                ForeColor = Color.White
            };
            Controls.Add(checkBoxMuteAfterFirst);

            checkBoxMuteAll = new CheckBox
            {
                Text = LocalizedStrings.GetString("Mute all audio"),
                Location = new Point(10, 120),
                AutoSize = true,
                ForeColor = Color.White
            };
            Controls.Add(checkBoxMuteAll);

            // Buttons
            buttonOK = new Button
            {
                Text = LocalizedStrings.GetString("OK"),
                DialogResult = DialogResult.OK,
                Location = new Point(220, 230),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            Controls.Add(buttonOK);

            buttonCancel = new Button
            {
                Text = LocalizedStrings.GetString("Cancel"),
                DialogResult = DialogResult.Cancel,
                Location = new Point(330, 230),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            Controls.Add(buttonCancel);

            LoadSettings();
            LoadVideos();
        }

        private void CheckBoxLoop_CheckedChanged(object? sender, EventArgs e)
        {
            checkBoxMuteAfterFirst.Enabled = checkBoxLoop.Checked;
            if (!checkBoxLoop.Checked)
                checkBoxMuteAfterFirst.Checked = false;
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
            checkBoxLoop.Checked = config.ReadValue("Windows", "ESLoadingVideoLoop", "false") == "true";
            checkBoxMuteAfterFirst.Checked = config.ReadValue("Windows", "ESLoadingVideoMuteAfterFirst", "false") == "true";
            checkBoxMuteAll.Checked = config.ReadValue("Windows", "ESLoadingVideoMuteAll", "false") == "true";
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