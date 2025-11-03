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

            InitializeComponent();

            // Setup main layout
            var mainLayoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(10)
            };
            mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.Controls.Add(mainLayoutPanel);

            // Controls
            var labelVideos = new Label { Text = LocalizedStrings.GetString("Available videos:"), AutoSize = true };
            comboBoxVideos = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };

            var optionsGroupBox = new GroupBox { Text = "Playback Options", Dock = DockStyle.Fill, AutoSize = true };
            var optionsLayoutPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true };
            optionsGroupBox.Controls.Add(optionsLayoutPanel);

            checkBoxLoop = new CheckBox { Text = LocalizedStrings.GetString("Loop playback"), AutoSize = true };
            checkBoxMuteAfterFirst = new CheckBox { Text = LocalizedStrings.GetString("Mute after first playback"), AutoSize = true, Enabled = false };
            checkBoxMuteAll = new CheckBox { Text = LocalizedStrings.GetString("Mute all audio"), AutoSize = true };

            var buttonsLayoutPanel = new TableLayoutPanel { Dock = DockStyle.Bottom, ColumnCount = 3, Height = 40 };
            buttonsLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            buttonsLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            buttonsLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            buttonOK = new Button { Text = LocalizedStrings.GetString("OK"), DialogResult = DialogResult.OK };
            buttonCancel = new Button { Text = LocalizedStrings.GetString("Cancel"), DialogResult = DialogResult.Cancel };
            buttonsLayoutPanel.Controls.Add(buttonOK, 1, 0);
            buttonsLayoutPanel.Controls.Add(buttonCancel, 2, 0);

            // Add controls to layouts
            mainLayoutPanel.Controls.Add(labelVideos, 0, 0);
            mainLayoutPanel.Controls.Add(comboBoxVideos, 0, 1);
            mainLayoutPanel.Controls.Add(optionsGroupBox, 0, 2);
            mainLayoutPanel.Controls.Add(buttonsLayoutPanel, 0, 4);

            optionsLayoutPanel.Controls.Add(checkBoxLoop);
            optionsLayoutPanel.Controls.Add(checkBoxMuteAfterFirst);
            optionsLayoutPanel.Controls.Add(checkBoxMuteAll);

            // Event handlers
            checkBoxLoop.CheckedChanged += CheckBoxLoop_CheckedChanged!;

            // Apply styles and load data
            ApplyDarkTheme();
            LoadSettings();
            LoadVideos();
        }

        private void ApplyDarkTheme()
        {
            this.BackColor = Color.FromArgb(28, 28, 28);
            this.ForeColor = Color.White;

            foreach (Control control in this.Controls)
            {
                if (control is TableLayoutPanel tlp)
                {
                    foreach (Control c in tlp.Controls)
                    {
                        ApplyControlTheme(c);
                    }
                }
                else
                {
                    ApplyControlTheme(control);
                }
            }
        }

        private void ApplyControlTheme(Control control)
        {
            control.ForeColor = Color.White;
            if (control is Button || control is ComboBox)
            {
                control.BackColor = Color.FromArgb(45, 45, 48);
                if(control is ComboBox combo) combo.FlatStyle = FlatStyle.Flat;
                if(control is Button btn) btn.FlatStyle = FlatStyle.Flat;
            }
            if (control is GroupBox gb)
            {
                foreach (Control c in gb.Controls)
                {
                   ApplyControlTheme(c);
                }
            }
             if (control is TableLayoutPanel tlp)
            {
                foreach (Control c in tlp.Controls)
                {
                   ApplyControlTheme(c);
                }
            }
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