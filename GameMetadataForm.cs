using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace BatRun
{
    public partial class GameMetadataForm : Form
    {
        // P/Invoke for smooth scrolling
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        private const int WM_VSCROLL = 0x115;
        private const int SB_LINEDOWN = 1;
        private const int SB_LINEUP = 0;

        private readonly Game _selectedGame;
        private readonly string _gamelistPath;
        private readonly string _romsFolderPath;
        private readonly string _retrobatRootPath;
        private XElement? _gameMetadata;

        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;

        // UI Controls as fields
        private VideoView? _videoView;
        private TableLayoutPanel? _infoPanel;
        private PictureBox? _imagePictureBox;
        private PictureBox? _marqueePictureBox;
        private PictureBox? _thumbnailPictureBox;
        private PictureBox? _fanartPictureBox;
        private PictureBox? _bezelPictureBox;
        private TabControl? _mediaTabControl;
        private RichTextBox? _descriptionTextBox;
        private System.Windows.Forms.Timer? _scrollTimer;
        private BezelInfo? _bezelInfo;
        private double _bezelAspectRatio = 0;
        private bool _isResizing = false;


        public GameMetadataForm(Game selectedGame, string gamelistPath, string retrobatRoot)
        {
            _selectedGame = selectedGame;
            _gamelistPath = gamelistPath;
            _romsFolderPath = Path.GetDirectoryName(gamelistPath) ?? "";
            _retrobatRootPath = retrobatRoot;

            Core.Initialize();
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);

            InitializeComponent();
            LoadGameMetadata();

            this.Shown += (s, e) => InitializeTimer();
            this.Resize += GameMetadataForm_Resize;
        }

        private void InitializeComponent()
        {
            this.Text = "Game Metadata";
            this.ClientSize = new Size(960, 720);
            this.BackColor = Color.FromArgb(45, 45, 48); // Slightly lighter dark grey
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9F);
            this.StartPosition = FormStartPosition.CenterParent;

            // Main layout: TableLayoutPanel with 2 rows
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 380F)); // Final increase to Info Panel height
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Media Panel fills rest
            this.Controls.Add(mainLayout);

            // Top Panel: Game Info
            _infoPanel = new TableLayoutPanel {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoScroll = true,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            _infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            _infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.Controls.Add(_infoPanel, 0, 0);

            // Bottom Panel: Media Tabs
            _mediaTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Alignment = TabAlignment.Top
            };
            _mediaTabControl.SelectedIndexChanged += MediaTabControl_SelectedIndexChanged;
            mainLayout.Controls.Add(_mediaTabControl, 0, 1);

            // Create Tab Pages
            var imageTab = new TabPage("Image");
            var videoTab = new TabPage("Video");
            var marqueeTab = new TabPage("Marquee");
            var thumbnailTab = new TabPage("Thumbnail");
            var fanartTab = new TabPage("Fanart");
            _mediaTabControl.TabPages.AddRange(new TabPage[] { imageTab, videoTab, marqueeTab, thumbnailTab, fanartTab });
            _mediaTabControl.SelectedIndex = 1; // Default to Video tab

            // Media Controls
            // Image
            _imagePictureBox = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
            imageTab.Controls.Add(_imagePictureBox);

            // Marquee
            _marqueePictureBox = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
            marqueeTab.Controls.Add(_marqueePictureBox);

            // Thumbnail
            _thumbnailPictureBox = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
            thumbnailTab.Controls.Add(_thumbnailPictureBox);

            // Fanart
            _fanartPictureBox = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
            fanartTab.Controls.Add(_fanartPictureBox);

            // Video
            var videoPanel = new Panel { Dock = DockStyle.Fill };
            videoPanel.Resize += (s, e) => UpdateVideoPosition();
            _bezelPictureBox = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
            _videoView = new VideoView { Dock = DockStyle.None, MediaPlayer = _mediaPlayer };
            var videoControls = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 30, FlowDirection = FlowDirection.LeftToRight, BackColor = Color.FromArgb(45, 45, 48) };
            var playButton = new Button { Text = "Play", FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(87, 87, 87), ForeColor = Color.White, Width = 75 };
            var pauseButton = new Button { Text = "Pause", FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(87, 87, 87), ForeColor = Color.White, Width = 75 };
            var stopButton = new Button { Text = "Stop", FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(87, 87, 87), ForeColor = Color.White, Width = 75 };
            playButton.Click += (s, e) => _mediaPlayer.Play();
            pauseButton.Click += (s, e) => _mediaPlayer.SetPause(true);
            stopButton.Click += (s, e) => _mediaPlayer.Stop();
            videoControls.Controls.AddRange(new Control[] { playButton, pauseButton, stopButton });

            videoPanel.Controls.Add(_videoView);
            videoPanel.Controls.Add(_bezelPictureBox);
            videoPanel.Controls.Add(videoControls);
            _videoView.BringToFront();
            videoControls.BringToFront();

            videoTab.Controls.Add(videoPanel);
        }

        private void MediaTabControl_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Stop video when switching away from the video tab
            if (_mediaTabControl?.SelectedTab?.Text != "Video")
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
                string? gameFileName = Path.GetFileName(_selectedGame.Path);
                _gameMetadata = doc.Descendants("game").FirstOrDefault(g => Path.GetFileName(g.Element("path")?.Value) == gameFileName);

                if (_gameMetadata != null)
                {
                    this.Text = $"Metadata - {GetMetaValue("name", _selectedGame.Name ?? "N/A")}";
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
            if (_infoPanel == null) return;
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
            // Description is now last
            AddDescriptionRow(GetMetaValue("desc"));

            _infoPanel.ResumeLayout();
        }

        private void PopulateMediaTabs()
        {
            if (_imagePictureBox != null) LoadMediaIntoPictureBox(_imagePictureBox, GetMetaValue("image"));
            if (_marqueePictureBox != null) LoadMediaIntoPictureBox(_marqueePictureBox, GetMetaValue("marquee"));
            if (_thumbnailPictureBox != null) LoadMediaIntoPictureBox(_thumbnailPictureBox, GetMetaValue("thumbnail"));
            if (_fanartPictureBox != null) LoadMediaIntoPictureBox(_fanartPictureBox, GetMetaValue("fanart"));

            // Load Bezel and Layout Info
            if (_bezelPictureBox != null && _selectedGame.System != null)
            {
                string decorationsRoot = Path.Combine(_retrobatRootPath, "system", "decorations", "default_curve_night", "systems");
                string bezelPath = Path.Combine(decorationsRoot, $"{_selectedGame.System}.png");
                string bezelInfoPath = Path.Combine(decorationsRoot, $"{_selectedGame.System}.info");

                if (File.Exists(bezelPath))
                {
                    _bezelPictureBox.Image = Image.FromFile(bezelPath);
                }

                if (File.Exists(bezelInfoPath))
                {
                    try
                    {
                        string json = File.ReadAllText(bezelInfoPath);
                        _bezelInfo = JsonConvert.DeserializeObject<BezelInfo>(json);
                        if (_bezelInfo != null && _bezelInfo.Height > 0)
                        {
                            _bezelAspectRatio = (double)_bezelInfo.Width / _bezelInfo.Height;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log or handle error if JSON is malformed
                        Console.WriteLine($"Error parsing bezel info file: {ex.Message}");
                    }
                }
            }

            // Load Video
            string videoPath = GetMetaValue("video");
            if (!string.IsNullOrEmpty(videoPath))
            {
                string fullVideoPath = Path.Combine(_romsFolderPath, videoPath.TrimStart('.', '/', '\\'));
                if (File.Exists(fullVideoPath))
                {
                    var media = new Media(_libVLC, new Uri(fullVideoPath));
                    _mediaPlayer.Media = media;
                    media.Dispose(); // LibVLC clones the media object, so we can dispose our reference
                    _mediaPlayer.Play();
                }
            }

            // Update the video position now that we have layout info
            UpdateVideoPosition();
        }

        private void UpdateVideoPosition()
        {
            if (_videoView == null || _videoView.Parent == null) return;

            if (_bezelInfo == null)
            {
                // If no bezel info, just fill the panel (minus controls)
                _videoView.Dock = DockStyle.Fill;
                return;
            }

            // If we have bezel info, undock and position manually
            _videoView.Dock = DockStyle.None;

            var container = _videoView.Parent;

            // Calculate the video area based on the bezel info
            double videoX = _bezelInfo.Left;
            double videoY = _bezelInfo.Top;
            double videoW = _bezelInfo.Width - _bezelInfo.Left - _bezelInfo.Right;
            double videoH = _bezelInfo.Height - _bezelInfo.Top - _bezelInfo.Bottom;

            // Prevent division by zero if bezel info is invalid
            if (_bezelInfo.Width == 0 || _bezelInfo.Height == 0) return;

            // Calculate the ratios relative to the original bezel image size
            double ratioX = videoX / _bezelInfo.Width;
            double ratioY = videoY / _bezelInfo.Height;
            double ratioW = videoW / _bezelInfo.Width;
            double ratioH = videoH / _bezelInfo.Height;

            // Apply the ratios to the current size of the container panel
            int newX = (int)(container.Width * ratioX);
            int newY = (int)(container.Height * ratioY);
            int newW = (int)(container.Width * ratioW);
            int newH = (int)(container.Height * ratioH);

            _videoView.Bounds = new Rectangle(newX, newY, newW, newH);
        }

        private void GameMetadataForm_Resize(object? sender, EventArgs e)
        {
            if (_isResizing || _bezelAspectRatio == 0) return;

            _isResizing = true;

            // Maintain aspect ratio
            int newHeight = (int)(this.ClientSize.Width / _bezelAspectRatio);
            this.ClientSize = new Size(this.ClientSize.Width, newHeight);

            _isResizing = false;
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
            if (string.IsNullOrWhiteSpace(valueText) || _infoPanel == null) return;

            _infoPanel.RowCount++;
            _infoPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));

            var label = new Label {
                Text = labelText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 8, 5, 8),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            var value = new TextBox {
                Text = valueText,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(60, 63, 65), // Darker textbox background
                ForeColor = Color.Gainsboro,
                Padding = new Padding(5, 8, 5, 8), // Increased vertical padding
                Font = new Font("Segoe UI", 10F)
            };

            _infoPanel.Controls.Add(label, 0, _infoPanel.RowCount - 1);
            _infoPanel.Controls.Add(value, 1, _infoPanel.RowCount - 1);
        }

        private void AddDescriptionRow(string descText)
        {
             if (string.IsNullOrWhiteSpace(descText) || _infoPanel == null) return;

            _infoPanel.RowCount++;
            _infoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var label = new Label {
                Text = "Description",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft,
                Padding = new Padding(5),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            _descriptionTextBox = new RichTextBox {
                Text = descText,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(60, 63, 65),
                ForeColor = Color.Gainsboro,
                Padding = new Padding(5),
                Font = new Font("Segoe UI", 10F),
                ScrollBars = RichTextBoxScrollBars.None
            };

            _infoPanel.Controls.Add(label, 0, _infoPanel.RowCount - 1);
            _infoPanel.Controls.Add(_descriptionTextBox, 1, _infoPanel.RowCount - 1);
        }

        private void InitializeTimer()
        {
            if (_descriptionTextBox == null) return;

            // Only start the timer if the text is long enough to scroll
            int textHeight = TextRenderer.MeasureText(_descriptionTextBox.Text, _descriptionTextBox.Font).Height;
            if (textHeight < _descriptionTextBox.ClientSize.Height) return;

            _scrollTimer = new System.Windows.Forms.Timer();
            _scrollTimer.Interval = 100; // Slower, more readable scroll
            _scrollTimer.Tick += ScrollTimer_Tick;
            _scrollTimer.Start();
        }

        private void ScrollTimer_Tick(object? sender, EventArgs e)
        {
            if (_descriptionTextBox == null) return;

            // Get the position of the first visible character
            int firstVisibleCharIndex = _descriptionTextBox.GetCharIndexFromPosition(new Point(1, 1));
            // Get the line number of that character
            int currentLine = _descriptionTextBox.GetLineFromCharIndex(firstVisibleCharIndex);

            // Send a scroll down message
            SendMessage(_descriptionTextBox.Handle, WM_VSCROLL, (IntPtr)SB_LINEDOWN, IntPtr.Zero);

            // Check if we have reached the bottom
            int newFirstVisibleCharIndex = _descriptionTextBox.GetCharIndexFromPosition(new Point(1, 1));
            if (firstVisibleCharIndex == newFirstVisibleCharIndex)
            {
                // We're at the bottom, scroll back to the top
                _descriptionTextBox.Select(0, 0);
                _descriptionTextBox.ScrollToCaret();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _scrollTimer?.Stop();
            _scrollTimer?.Dispose();
            _mediaPlayer.Stop();
            _mediaPlayer.Dispose();
            _libVLC.Dispose();
            base.OnFormClosing(e);
        }
    }
}
