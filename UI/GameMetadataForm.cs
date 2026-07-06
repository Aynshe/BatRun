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
using System.Threading.Tasks;

using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.UI
{
    public partial class GameMetadataForm : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        private const int WM_VSCROLL = 0x115;
        private const int SB_LINEDOWN = 1;

        private readonly Game _selectedGame;
        private readonly string _gamelistPath;
        private readonly string _romsFolderPath;
        private readonly string _retrobatRootPath;
        private XElement? _gameMetadata;

        private LibVLC? _libVLC;
        private MediaPlayer? _mediaPlayer;
        private VideoView? _videoView;
        private RichTextBox? _descriptionTextBox;
        private System.Windows.Forms.Timer? _scrollTimer;
        private BezelInfo? _bezelInfo;

        public GameMetadataForm(Game selectedGame, string gamelistPath, string retrobatRoot)
        {
            _selectedGame = selectedGame;
            _gamelistPath = gamelistPath;
            _romsFolderPath = Path.GetDirectoryName(gamelistPath) ?? "";
            _retrobatRootPath = retrobatRoot;

            InitializeComponent();
            this.Load += async (s, e) => await LoadGameMetadataAsync();
            
            // Runtime dynamic buttons logic
            AddVideoControls();
        }

        private void AddVideoControls()
        {
            var videoTab = _mediaTabControl.TabPages[1]; // Video tab
            var videoPanel = videoTab.Controls[0] as Panel;
            if (videoPanel == null) return;

            _videoView = new VideoView { Dock = DockStyle.None, MediaPlayer = _mediaPlayer };
            var videoControls = new FlowLayoutPanel 
            { 
                Dock = DockStyle.Bottom, 
                Height = 40, 
                FlowDirection = FlowDirection.LeftToRight, 
                WrapContents = false, 
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(5, 5, 5, 5)
            };
            
            var playButton = new Button { Text = "Play", FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(87, 87, 87), ForeColor = Color.White, Width = 80, Height = 30, Margin = new Padding(5, 0, 5, 0) };
            var pauseButton = new Button { Text = "Pause", FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(87, 87, 87), ForeColor = Color.White, Width = 80, Height = 30, Margin = new Padding(5, 0, 5, 0) };
            var stopButton = new Button { Text = "Stop", FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(87, 87, 87), ForeColor = Color.White, Width = 80, Height = 30, Margin = new Padding(5, 0, 5, 0) };
            
            playButton.Click += (s, e) => _mediaPlayer?.Play();
            pauseButton.Click += (s, e) => _mediaPlayer?.SetPause(true);
            stopButton.Click += (s, e) => _mediaPlayer?.Stop();
            
            videoControls.Controls.AddRange(new Control[] { playButton, pauseButton, stopButton });
            videoPanel.Controls.Add(_videoView);
            _videoView.BringToFront();
            videoControls.BringToFront();
            videoPanel.Controls.Add(videoControls);
            
            this.MouseClick += OnRightClickClose;
            _infoPanel.MouseClick += OnRightClickClose;
        }

        private async Task LoadGameMetadataAsync()
        {
            try
            {
                var gameData = await Task.Run(() =>
                {
                    if (!File.Exists(_gamelistPath)) throw new FileNotFoundException($"gamelist.xml not found at:\n{_gamelistPath}");
                    // [BATRUN-FIX]: Force UTF-8 encoding
                    XDocument doc;
                    using (var sr = new StreamReader(_gamelistPath, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                    {
                        doc = XDocument.Load(sr);
                    }
                    string? gameFileName = Path.GetFileName(_selectedGame.Path);
                    var gameMetadata = doc.Descendants("game").FirstOrDefault(g => Path.GetFileName(g.Element("path")?.Value) == gameFileName);
                    if (gameMetadata == null) throw new Exception($"Game '{_selectedGame.Name}' not found in gamelist.xml.");
                    return new GameData(gameMetadata);
                });

                _gameMetadata = gameData.Metadata;
                if (_gameMetadata == null) throw new Exception("Game metadata could not be loaded.");

                this.Text = $"Metadata - {GetMetaValue("name", _selectedGame.Name ?? "N/A")}";

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
                if (_loadingPanel != null) _loadingPanel.Visible = false;
            }
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
                byte[]? LoadFullPath(string? fullPath) => !string.IsNullOrEmpty(fullPath) && File.Exists(fullPath) ? File.ReadAllBytes(fullPath) : null;
                string? LoadTextFile(string? fullPath) => !string.IsNullOrEmpty(fullPath) && File.Exists(fullPath) ? File.ReadAllText(fullPath, System.Text.Encoding.UTF8) : null;

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

            if (_imagePictureBox != null && mediaData.Image != null) _imagePictureBox.Image = Image.FromStream(new MemoryStream(mediaData.Image));
            if (_marqueePictureBox != null && mediaData.Marquee != null) _marqueePictureBox.Image = Image.FromStream(new MemoryStream(mediaData.Marquee));
            if (_thumbnailPictureBox != null && mediaData.Thumbnail != null) _thumbnailPictureBox.Image = Image.FromStream(new MemoryStream(mediaData.Thumbnail));
            if (_fanartPictureBox != null && mediaData.Fanart != null) _fanartPictureBox.Image = Image.FromStream(new MemoryStream(mediaData.Fanart));
            if (_bezelPictureBox != null && mediaData.Bezel != null) _bezelPictureBox.Image = Image.FromStream(new MemoryStream(mediaData.Bezel));

            if (mediaData.BezelInfoJson != null)
            {
                try { _bezelInfo = JsonConvert.DeserializeObject<BezelInfo>(mediaData.BezelInfoJson); } catch { }
            }

            if (!string.IsNullOrEmpty(mediaPaths.Video))
            {
                string fullVideoPath = Path.Combine(_romsFolderPath, mediaPaths.Video.TrimStart('.', '/', '\\'));
                if (File.Exists(fullVideoPath))
                {
                    _libVLC = new LibVLC();
                    _mediaPlayer = new MediaPlayer(_libVLC);
                    if (_videoView != null) _videoView.MediaPlayer = _mediaPlayer;
                    var media = new Media(_libVLC, new Uri(fullVideoPath));
                    _mediaPlayer.Media = media;
                    media.Dispose();
                    _mediaPlayer.Play();
                }
            }
            UpdateBezelAndVideoPosition();
        }

        private void VideoPanel_Resize(object? sender, EventArgs e) => UpdateBezelAndVideoPosition();

        private void MediaTabControl_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_mediaTabControl?.SelectedTab?.Text != "Video") _mediaPlayer?.Stop();
        }

        private void UpdateBezelAndVideoPosition()
        {
            if (_videoView == null || _bezelPictureBox == null || _bezelPictureBox.Parent == null) return;
            var container = _bezelPictureBox.Parent;
            if (_bezelInfo == null || _bezelPictureBox.Image == null)
            {
                _videoView.Dock = DockStyle.Fill;
                _bezelPictureBox.Visible = false;
                return;
            }

            _videoView.Dock = DockStyle.None;
            _bezelPictureBox.Visible = true;
            float bezelAspectRatio = (float)_bezelPictureBox.Image.Width / _bezelPictureBox.Image.Height;
            int bottomMargin = container.Controls.Cast<Control>().FirstOrDefault(c => c.Dock == DockStyle.Bottom)?.Height ?? 0;
            int availableHeight = container.Height - bottomMargin;
            int newWidth, newHeight;
            
            if ((float)container.Width / availableHeight > bezelAspectRatio)
            {
                newHeight = availableHeight;
                newWidth = (int)(newHeight * bezelAspectRatio);
            }
            else
            {
                newWidth = container.Width;
                newHeight = (int)(newWidth / bezelAspectRatio);
            }

            _bezelPictureBox.Size = new Size(newWidth, newHeight);
            _bezelPictureBox.Location = new Point((container.Width - newWidth) / 2, (availableHeight - newHeight) / 2);

            double videoX = _bezelInfo.Left, videoY = _bezelInfo.Top;
            double videoW = _bezelInfo.Width - _bezelInfo.Left - _bezelInfo.Right;
            double videoH = _bezelInfo.Height - _bezelInfo.Top - _bezelInfo.Bottom;

            if (_bezelInfo.Width == 0 || _bezelInfo.Height == 0) return;

            double ratioX = videoX / _bezelInfo.Width, ratioY = videoY / _bezelInfo.Height;
            double ratioW = videoW / _bezelInfo.Width, ratioH = videoH / _bezelInfo.Height;

            int newVidX = _bezelPictureBox.Left + (int)(_bezelPictureBox.Width * ratioX);
            int newVidY = _bezelPictureBox.Top + (int)(_bezelPictureBox.Height * ratioY);
            int newVidW = (int)(_bezelPictureBox.Width * ratioW);
            int newVidH = (int)(_bezelPictureBox.Height * ratioH);

            _videoView.Bounds = new Rectangle(newVidX, newVidY, newVidW, newVidH);
        }

        private string GetMetaValue(string key, string defaultValue = "N/A") => _gameMetadata?.Element(key)?.Value ?? defaultValue;

        private void AddInfoRow(string labelText, string valueText)
        {
            if (string.IsNullOrWhiteSpace(valueText) || _infoPanel == null) return;
            _infoPanel.RowCount++;
            _infoPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            _infoPanel.Controls.Add(new Label { Text = labelText, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(5, 8, 5, 8), Font = new Font("Segoe UI", 9F, FontStyle.Bold) }, 0, _infoPanel.RowCount - 1);
            _infoPanel.Controls.Add(new TextBox { Text = valueText, Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(60, 63, 65), ForeColor = Color.Gainsboro, Padding = new Padding(5, 8, 5, 8), Font = new Font("Segoe UI", 10F) }, 1, _infoPanel.RowCount - 1);
        }

        private void AddDescriptionRow(string descText)
        {
            if (string.IsNullOrWhiteSpace(descText) || _infoPanel == null) return;
            _infoPanel.RowCount++;
            _infoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _infoPanel.Controls.Add(new Label { Text = "Description", Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft, Padding = new Padding(5), Font = new Font("Segoe UI", 9F, FontStyle.Bold) }, 0, _infoPanel.RowCount - 1);
            _descriptionTextBox = new RichTextBox { Text = descText, Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(60, 63, 65), ForeColor = Color.Gainsboro, Padding = new Padding(5), Font = new Font("Segoe UI", 10F), ScrollBars = RichTextBoxScrollBars.None };
            _descriptionTextBox.MouseEnter += (s, e) => _scrollTimer?.Stop();
            _descriptionTextBox.MouseLeave += (s, e) => _scrollTimer?.Start();
            _infoPanel.Controls.Add(_descriptionTextBox, 1, _infoPanel.RowCount - 1);
        }

        private void InitializeTimer()
        {
            if (_descriptionTextBox == null) return;
            int textHeight = TextRenderer.MeasureText(_descriptionTextBox.Text, _descriptionTextBox.Font).Height;
            if (textHeight < _descriptionTextBox.ClientSize.Height) return;
            _scrollTimer = new System.Windows.Forms.Timer { Interval = 200 };
            _scrollTimer.Tick += (s, e) =>
            {
                if (_descriptionTextBox == null) return;
                int firstVisibleCharIndex = _descriptionTextBox.GetCharIndexFromPosition(new Point(1, 1));
                SendMessage(_descriptionTextBox.Handle, WM_VSCROLL, (IntPtr)1, IntPtr.Zero);
                if (firstVisibleCharIndex == _descriptionTextBox.GetCharIndexFromPosition(new Point(1, 1))) { _descriptionTextBox.Select(0, 0); _descriptionTextBox.ScrollToCaret(); }
            };
            _scrollTimer.Start();
        }

        private void OnRightClickClose(object? sender, MouseEventArgs e) { if (e.Button == MouseButtons.Right) this.Close(); }



        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            try
            {
                _mediaPlayer?.Stop();
                _mediaPlayer?.Dispose();
                _libVLC?.Dispose();
            }
            catch { }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape) { this.Close(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    // EN: Simple wrapper for game metadata
    // FR: Simple wrapper pour les métadonnées du jeu
    public class GameData
    {
        public XElement? Metadata { get; }
        public GameData(XElement? metadata) => Metadata = metadata;
    }
}


