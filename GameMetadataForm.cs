using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

namespace BatRun
{
    public partial class GameMetadataForm : Form
    {
        private readonly Game _selectedGame;
        private readonly string _gamelistPath;
        private readonly string _romsFolderPath;
        private XElement? _gameMetadata;

        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;

        // UI Controls as fields
        private VideoView _videoView;
        private TableLayoutPanel _infoPanel;
        private PictureBox _imagePictureBox;
        private PictureBox _marqueePictureBox;
        private TabControl _mediaTabControl;


        public GameMetadataForm(Game selectedGame, string gamelistPath)
        {
            _selectedGame = selectedGame;
            _gamelistPath = gamelistPath;
            _romsFolderPath = Path.GetDirectoryName(gamelistPath) ?? "";

            Core.Initialize();
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);

            InitializeComponent();
            LoadGameMetadata();
        }

        private void InitializeComponent()
        {
            this.Text = "Game Metadata";
            this.ClientSize = new Size(800, 600);
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9F);
            this.StartPosition = FormStartPosition.CenterParent;

            var mainSplitContainer = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1, SplitterDistance = 150 };
            this.Controls.Add(mainSplitContainer);

            _mediaTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Alignment = TabAlignment.Left,
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(35, 100),
                DrawMode = TabDrawMode.OwnerDrawFixed,
            };
            _mediaTabControl.DrawItem += MediaTabControl_DrawItem;
            _mediaTabControl.SelectedIndexChanged += MediaTabControl_SelectedIndexChanged;
            mainSplitContainer.Panel1.Controls.Add(_mediaTabControl);

            var contentPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            mainSplitContainer.Panel2.Controls.Add(contentPanel);

            var infoTab = new TabPage("Info");
            var imageTab = new TabPage("Image");
            var videoTab = new TabPage("Video");
            var marqueeTab = new TabPage("Marquee");
            _mediaTabControl.TabPages.AddRange(new TabPage[] { infoTab, imageTab, videoTab, marqueeTab });

            // Info Tab Content
            _infoPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoScroll = true, CellBorderStyle = TableLayoutPanelCellBorderStyle.Single };
            _infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            _infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            infoTab.Controls.Add(_infoPanel);

            // Video Tab Content
            var videoPanel = new Panel { Dock = DockStyle.Fill };
            _videoView = new VideoView { Dock = DockStyle.Fill, MediaPlayer = _mediaPlayer };
            var videoControls = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 30, FlowDirection = FlowDirection.LeftToRight };
            var playButton = new Button { Text = "Play", FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(87, 87, 87), ForeColor = Color.White, Width = 75 };
            var pauseButton = new Button { Text = "Pause", FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(87, 87, 87), ForeColor = Color.White, Width = 75 };
            var stopButton = new Button { Text = "Stop", FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(87, 87, 87), ForeColor = Color.White, Width = 75 };
            playButton.Click += (s, e) => _mediaPlayer.Play();
            pauseButton.Click += (s, e) => _mediaPlayer.SetPause(true);
            stopButton.Click += (s, e) => _mediaPlayer.Stop();
            videoControls.Controls.AddRange(new Control[] { playButton, pauseButton, stopButton });
            videoPanel.Controls.Add(_videoView);
            videoPanel.Controls.Add(videoControls);
            videoTab.Controls.Add(videoPanel);

            // Image Tab Content
            _imagePictureBox = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
            imageTab.Controls.Add(_imagePictureBox);

            // Marquee Tab Content
            _marqueePictureBox = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.ScaleDown };
            marqueeTab.Controls.Add(_marqueePictureBox);
        }

        private void MediaTabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            var g = e.Graphics;
            var tab = _mediaTabControl.TabPages[e.Index];
            var rect = e.Bounds;
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            if (e.State == DrawItemState.Selected)
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(0, 122, 204)), e.Bounds);
                g.DrawString(tab.Text, e.Font, Brushes.White, rect, sf);
            }
            else
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(45, 45, 48)), e.Bounds);
                g.DrawString(tab.Text, e.Font, Brushes.White, rect, sf);
            }
        }

        private void MediaTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Stop video when switching away from the video tab
            if (_mediaTabControl.SelectedTab.Text != "Video")
            {
                _mediaPlayer.Stop();
            }
        }

        private void LoadGameMetadata()
        {
            if (!File.Exists(_gamelistPath))
            {
                MessageBox.Show($"gamelist.xml not found at:\n{_gamelistPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            try
            {
                var doc = XDocument.Load(_gamelistPath);
                string gameFileName = Path.GetFileName(_selectedGame.Path);
                _gameMetadata = doc.Descendants("game").FirstOrDefault(g => Path.GetFileName(g.Element("path")?.Value) == gameFileName);

                if (_gameMetadata != null)
                {
                    this.Text = $"Metadata - {GetMetaValue("name", _selectedGame.Name)}";
                    PopulateInfoTab();
                    PopulateMediaTabs();
                }
                else
                {
                     MessageBox.Show($"Game '{_selectedGame.Name}' not found in gamelist.xml.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                     Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing gamelist.xml:\n{ex.Message}", "XML Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private void PopulateInfoTab()
        {
            _infoPanel.SuspendLayout();
            _infoPanel.Controls.Clear();
            _infoPanel.RowCount = 0;

            AddInfoRow("Name", GetMetaValue("name"));
            AddInfoRow("Developer", GetMetaValue("developer"));
            AddInfoRow("Publisher", GetMetaValue("publisher"));
            AddInfoRow("Release Date", GetMetaValue("releasedate"));
            AddInfoRow("Genre", GetMetaValue("genre"));
            AddInfoRow("Players", GetMetaValue("players"));
            AddInfoRow("Rating", GetMetaValue("rating"));
            AddInfoRow("Play Count", GetMetaValue("playcount"));
            AddInfoRow("Last Played", GetMetaValue("lastplayed"));
            AddDescriptionRow(GetMetaValue("desc"));

            _infoPanel.ResumeLayout();
        }

        private void PopulateMediaTabs()
        {
            LoadMediaIntoPictureBox(_imagePictureBox, GetMetaValue("image"));
            LoadMediaIntoPictureBox(_marqueePictureBox, GetMetaValue("marquee"));

            string videoPath = GetMetaValue("video");
            if (!string.IsNullOrEmpty(videoPath))
            {
                string fullVideoPath = Path.Combine(_romsFolderPath, videoPath.TrimStart('.', '/', '\\'));
                if (File.Exists(fullVideoPath))
                {
                    _mediaPlayer.Media = new Media(_libVLC, new Uri(fullVideoPath));
                }
            }
        }

        private void LoadMediaIntoPictureBox(PictureBox pb, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;

            string fullPath = Path.Combine(_romsFolderPath, relativePath.TrimStart('.', '/', '\\'));
            if (File.Exists(fullPath))
            {
                pb.Image = Image.FromFile(fullPath);
            }
        }

        private string GetMetaValue(string key, string defaultValue = "N/A")
        {
            return _gameMetadata?.Element(key)?.Value ?? defaultValue;
        }

        private void AddInfoRow(string labelText, string valueText)
        {
            if (string.IsNullOrWhiteSpace(valueText)) return;

            _infoPanel.RowCount++;
            _infoPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var label = new Label { Text = labelText, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(5) };
            var value = new TextBox { Text = valueText, Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(55,55,58), ForeColor = Color.White, Padding = new Padding(5) };

            _infoPanel.Controls.Add(label, 0, _infoPanel.RowCount - 1);
            _infoPanel.Controls.Add(value, 1, _infoPanel.RowCount - 1);
        }

        private void AddDescriptionRow(string descText)
        {
             if (string.IsNullOrWhiteSpace(descText)) return;

            _infoPanel.RowCount++;
            _infoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var label = new Label { Text = "Description", Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft, Padding = new Padding(5) };
            var value = new RichTextBox { Text = descText, Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(55,55,58), ForeColor = Color.White, Padding = new Padding(5) };

            _infoPanel.Controls.Add(label, 0, _infoPanel.RowCount - 1);
            _infoPanel.Controls.Add(value, 1, _infoPanel.RowCount - 1);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _mediaPlayer.Stop();
            _mediaPlayer.Dispose();
            _libVLC.Dispose();
            base.OnFormClosing(e);
        }
    }
}
