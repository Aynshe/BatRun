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
    public class GameData
    {
        public XElement? Metadata { get; }

        public GameData(XElement? metadata)
        {
            Metadata = metadata;
        }
    }

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

        private LibVLC? _libVLC;
        private MediaPlayer? _mediaPlayer;

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
        private Panel? _loadingPanel;


        public GameMetadataForm(Game selectedGame, string gamelistPath, string retrobatRoot)
        {
            _selectedGame = selectedGame;
            _gamelistPath = gamelistPath;
            _romsFolderPath = Path.GetDirectoryName(gamelistPath) ?? "";
            _retrobatRootPath = retrobatRoot;

            InitializeComponent();
            this.Load += async (s, e) => await LoadGameMetadataAsync();
        }

        private async Task LoadGameMetadataAsync()
        {
            try
            {
                var gameData = await Task.Run(() =>
                {
                    if (!File.Exists(_gamelistPath))
                    {
                        throw new FileNotFoundException($"gamelist.xml not found at:\n{_gamelistPath}");
                    }

                    var doc = XDocument.Load(_gamelistPath);
                    string? gameFileName = Path.GetFileName(_selectedGame.Path);
                    var gameMetadata = doc.Descendants("game").FirstOrDefault(g => Path.GetFileName(g.Element("path")?.Value) == gameFileName);

                    if (gameMetadata == null)
                    {
                        throw new Exception($"Game '{_selectedGame.Name}' not found in gamelist.xml.");
                    }

                    return new GameData(gameMetadata);
                });

                _gameMetadata = gameData.Metadata;
                if (_gameMetadata == null)
                {
                    throw new Exception("Game metadata could not be loaded.");
                }


                // --- Populate UI on UI thread ---
                this.Text = $"Metadata - {GetMetaValue("name", _selectedGame.Name ?? "N/A")}";

                // Populate Info Tab
                if (_infoPanel != null)
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

                // Populate Media Tabs
                await PopulateMediaTabsAsync();


                InitializeTimer();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
            finally
            {
                if (_loadingPanel != null)
                {
                    _loadingPanel.Visible = false;
                }
            }
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
            var imageTab = new TabPage("Image") { BackColor = Color.Black };
            var videoTab = new TabPage("Video") { BackColor = Color.Black };
            var marqueeTab = new TabPage("Marquee") { BackColor = Color.Black };
            var thumbnailTab = new TabPage("Thumbnail") { BackColor = Color.Black };
            var fanartTab = new TabPage("Fanart") { BackColor = Color.Black };
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
            var videoPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black };
            videoPanel.Resize += (s, e) => UpdateBezelAndVideoPosition();
            _bezelPictureBox = new PictureBox { Dock = DockStyle.None, SizeMode = PictureBoxSizeMode.Zoom };
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

            // Add right-click to close event
            this.MouseClick += OnRightClickClose;
            mainLayout.MouseClick += OnRightClickClose;
            _infoPanel.MouseClick += OnRightClickClose;

            // Add loading panel
            _loadingPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Visible = true
            };
            var loadingLabel = new Label
            {
                Text = "Loading...",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16F),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _loadingPanel.Controls.Add(loadingLabel);
            this.Controls.Add(_loadingPanel);
            _loadingPanel.BringToFront();
        }

        private void OnRightClickClose(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                this.Close();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private async Task PopulateMediaTabsAsync()
        {
            var mediaPaths = new
            {
                Image = GetMetaValue("image"),
                Marquee = GetMetaValue("marquee"),
                Thumbnail = GetMetaValue("thumbnail"),
                Fanart = GetMetaValue("fanart"),
                Video = GetMetaValue("video"),
                Bezel = _selectedGame.System != null ? Path.Combine(_retrobatRootPath, "system", "decorations", "default_curve_night", "systems", $"{_selectedGame.System}.png") : null,
                BezelInfo = _selectedGame.System != null ? Path.Combine(_retrobatRootPath, "system", "decorations", "default_curve_night", "systems", $"{_selectedGame.System}.info") : null
            };

            var mediaData = await Task.Run(() =>
            {
                byte[]? LoadFile(string? relativePath)
                {
                    if (string.IsNullOrEmpty(relativePath)) return null;
                    string fullPath = Path.Combine(_romsFolderPath, relativePath.TrimStart('.', '/', '\\'));
                    return File.Exists(fullPath) ? File.ReadAllBytes(fullPath) : null;
                }

                byte[]? LoadFullPath(string? fullPath)
                {
                    if (string.IsNullOrEmpty(fullPath)) return null;
                    return File.Exists(fullPath) ? File.ReadAllBytes(fullPath) : null;
                }

                string? LoadTextFile(string? fullPath)
                {
                    if (string.IsNullOrEmpty(fullPath)) return null;
                    return File.Exists(fullPath) ? File.ReadAllText(fullPath) : null;
                }

                return new
                {
                    Image = LoadFile(mediaPaths.Image),
                    Marquee = LoadFile(mediaPaths.Marquee),
                    Thumbnail = LoadFile(mediaPaths.Thumbnail),
                    Fanart = LoadFile(mediaPaths.Fanart),
                    Bezel = LoadFullPath(mediaPaths.Bezel),
                    BezelInfoJson = LoadTextFile(mediaPaths.BezelInfo)
                };
            });

            // --- Populate UI on UI thread ---
            if (_imagePictureBox != null && mediaData.Image != null) _imagePictureBox.Image = Image.FromStream(new MemoryStream(mediaData.Image));
            if (_marqueePictureBox != null && mediaData.Marquee != null) _marqueePictureBox.Image = Image.FromStream(new MemoryStream(mediaData.Marquee));
            if (_thumbnailPictureBox != null && mediaData.Thumbnail != null) _thumbnailPictureBox.Image = Image.FromStream(new MemoryStream(mediaData.Thumbnail));
            if (_fanartPictureBox != null && mediaData.Fanart != null) _fanartPictureBox.Image = Image.FromStream(new MemoryStream(mediaData.Fanart));

            if (_bezelPictureBox != null && mediaData.Bezel != null)
            {
                _bezelPictureBox.Image = Image.FromStream(new MemoryStream(mediaData.Bezel));
            }

            if (mediaData.BezelInfoJson != null)
            {
                try
                {
                    _bezelInfo = JsonConvert.DeserializeObject<BezelInfo>(mediaData.BezelInfoJson);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing bezel info file: {ex.Message}");
                }
            }

            if (!string.IsNullOrEmpty(mediaPaths.Video))
            {
                string fullVideoPath = Path.Combine(_romsFolderPath, mediaPaths.Video.TrimStart('.', '/', '\\'));
                if (File.Exists(fullVideoPath))
                {
                    _libVLC = new LibVLC();
                    _mediaPlayer = new MediaPlayer(_libVLC);
                    _videoView.MediaPlayer = _mediaPlayer;

                    var media = new Media(_libVLC, new Uri(fullVideoPath));
                    _mediaPlayer.Media = media;
                    media.Dispose();
                    _mediaPlayer.Play();
                }
            }

            UpdateBezelAndVideoPosition();
        }

        private void MediaTabControl_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Stop video when switching away from the video tab
            if (_mediaTabControl?.SelectedTab?.Text != "Video")
            {
                _mediaPlayer.Stop();
            }
        }

        private void UpdateBezelAndVideoPosition()
        {
            if (_videoView == null || _bezelPictureBox == null || _bezelPictureBox.Parent == null) return;

            var container = _bezelPictureBox.Parent;

            if (_bezelInfo == null || _bezelPictureBox.Image == null)
            {
                // If no bezel, video fills the container (minus controls)
                _videoView.Dock = DockStyle.Fill;
                _bezelPictureBox.Visible = false;
                return;
            }

            _videoView.Dock = DockStyle.None;
            _bezelPictureBox.Visible = true;

            // Calculate bezel's new size and position to maintain aspect ratio
            float bezelAspectRatio = (float)_bezelPictureBox.Image.Width / _bezelPictureBox.Image.Height;
            int newWidth, newHeight;

            if ((float)container.Width / container.Height > bezelAspectRatio)
            {
                // Container is wider than the bezel's aspect ratio, so height is the limiting factor
                newHeight = container.Height;
                newWidth = (int)(newHeight * bezelAspectRatio);
            }
            else
            {
                // Container is taller or same aspect ratio, so width is the limiting factor
                newWidth = container.Width;
                newHeight = (int)(newWidth / bezelAspectRatio);
            }

            _bezelPictureBox.Size = new Size(newWidth, newHeight);
            _bezelPictureBox.Location = new Point((container.Width - newWidth) / 2, (container.Height - newHeight) / 2);

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

            // Apply the ratios to the new, correctly-scaled size of the bezel PictureBox
            int newVidX = _bezelPictureBox.Left + (int)(_bezelPictureBox.Width * ratioX);
            int newVidY = _bezelPictureBox.Top + (int)(_bezelPictureBox.Height * ratioY);
            int newVidW = (int)(_bezelPictureBox.Width * ratioW);
            int newVidH = (int)(_bezelPictureBox.Height * ratioH);

            _videoView.Bounds = new Rectangle(newVidX, newVidY, newVidW, newVidH);
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
            _descriptionTextBox.MouseEnter += DescriptionTextBox_MouseEnter;
            _descriptionTextBox.MouseLeave += DescriptionTextBox_MouseLeave;

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
            _scrollTimer.Interval = 200; // Slower, more readable scroll
            _scrollTimer.Tick += ScrollTimer_Tick;
            _scrollTimer.Start();
        }

        private void DescriptionTextBox_MouseEnter(object? sender, EventArgs e)
        {
            _scrollTimer?.Stop();
        }

        private void DescriptionTextBox_MouseLeave(object? sender, EventArgs e)
        {
            _scrollTimer?.Start();
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
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
