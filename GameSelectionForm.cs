using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BatRun
{
    public partial class GameSelectionForm : Form
    {
        private readonly EmulationStationScraper _scraper;
        private readonly string _retrobatPath;
        private readonly Logger _logger;
        private readonly LibVLCSharp.Shared.LibVLC _libVLC;
        private ComboBox? _systemComboBox;
        private ListView? _gameListView;
        private Button? _okButton;
        private Button? _cancelButton;
        private Label? _statusLabel;

        public Game? SelectedGame { get; private set; }
        public SystemInfo? SelectedSystem { get; private set; }

        public GameSelectionForm(EmulationStationScraper scraper, string retrobatPath, Logger logger, LibVLCSharp.Shared.LibVLC libVLC)
        {
            _scraper = scraper;
            _retrobatPath = retrobatPath;
            _logger = logger;
            _libVLC = libVLC;
            InitializeComponent();
            this.Load += async (s, e) => await LoadSystemsAsync();
        }

        private void InitializeComponent()
        {
            this.Text = "Select a Game from EmulationStation";
            this.ClientSize = new Size(600, 450);
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9F);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                RowCount = 4,
                ColumnCount = 1
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            this.Controls.Add(mainLayout);

            // System selection
            var systemPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight
            };
            var systemLabel = new Label
            {
                Text = "System:",
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 0)
            };
            _systemComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 400,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };
            _systemComboBox.SelectedIndexChanged += SystemComboBox_SelectedIndexChanged;
            systemPanel.Controls.Add(systemLabel);
            systemPanel.Controls.Add(_systemComboBox);
            mainLayout.Controls.Add(systemPanel, 0, 0);

            // Game List
            _gameListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            _gameListView.Columns.Add("Game", -2); // Auto-size
            _gameListView.SelectedIndexChanged += GameListView_SelectedIndexChanged;
            _gameListView.MouseClick += GameListView_MouseClick;
            mainLayout.Controls.Add(_gameListView, 0, 1);

            // Status Label
            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Checking EmulationStation API...",
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainLayout.Controls.Add(_statusLabel, 0, 2);

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };
            _okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Width = 100,
                Height = 25,
                Enabled = false,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White
            };
            _okButton.Click += OkButton_Click;
            _cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Width = 100,
                Height = 25,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(87, 87, 87),
                ForeColor = Color.White
            };
            buttonPanel.Controls.Add(_okButton);
            buttonPanel.Controls.Add(_cancelButton);
            mainLayout.Controls.Add(buttonPanel, 0, 3);

            this.AcceptButton = _okButton;
            this.CancelButton = _cancelButton;
        }

        private async Task LoadSystemsAsync()
        {
            if (_systemComboBox == null || _statusLabel == null) return;

            _statusLabel.Text = "Pinging EmulationStation server...";
            if (!await _scraper.PingServerAsync())
            {
                _statusLabel.Text = "EmulationStation server not found at localhost:1234.";
                MessageBox.Show("Could not connect to the EmulationStation API. Make sure EmulationStation is running and the web server is enabled in the settings.", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }

            _statusLabel.Text = "Loading systems...";
            var systems = await _scraper.GetSystemsAsync();
            if (systems.Count > 0)
            {
                _systemComboBox.DataSource = systems;
                _systemComboBox.DisplayMember = "fullname";
                _systemComboBox.ValueMember = "name";
                _statusLabel.Text = "Select a system.";
            }
            else
            {
                _statusLabel.Text = "No systems found.";
            }
        }

        private async void SystemComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_systemComboBox?.SelectedItem is SystemInfo selectedSystem && _gameListView != null && _statusLabel != null)
            {
                _statusLabel.Text = $"Loading games for {selectedSystem.fullname}...";
                _gameListView.Items.Clear();
                _okButton!.Enabled = false;

                var games = await _scraper.GetGamesForSystemAsync(selectedSystem.name!);
                foreach (var game in games)
                {
                    var item = new ListViewItem(game.Name)
                    {
                        Tag = game
                    };
                    _gameListView.Items.Add(item);
                }
                _statusLabel.Text = $"{games.Count} games found for {selectedSystem.fullname}.";
            }
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            if (_gameListView?.SelectedItems.Count > 0 && _systemComboBox?.SelectedItem is SystemInfo selectedSystem)
            {
                SelectedGame = _gameListView.SelectedItems[0].Tag as Game;
                SelectedSystem = selectedSystem;
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                this.DialogResult = DialogResult.None; // Prevent closing if nothing is selected
            }
        }

        private void GameListView_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_gameListView?.SelectedItems.Count > 0)
            {
                _okButton!.Enabled = true;
            }
            else
            {
                _okButton!.Enabled = false;
            }
        }

        private void GameListView_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (_gameListView?.FocusedItem != null && _gameListView.FocusedItem.Bounds.Contains(e.Location))
                {
                    var game = _gameListView.FocusedItem.Tag as Game;
                    var system = _systemComboBox?.SelectedItem as SystemInfo;

                    if (game != null && system != null && game.Path != null && system.name != null)
                    {
                        var parser = new GamelistParser(_retrobatPath, _logger);
                        // The path in gamelist.xml is relative, e.g., "./rom.zip".
                        // The path from the scraper is absolute. We need to convert it.
                        string relativePath = $"./{System.IO.Path.GetFileName(game.Path)}";
                        var metadata = parser.GetGameMetadata(system.name, relativePath);

                        if (metadata.Count > 0)
                        {
                            using var viewer = new MetadataViewerForm(metadata, _retrobatPath, system.name, _libVLC);
                            viewer.ShowDialog();
                        }
                        else
                        {
                            MessageBox.Show("No local metadata found for this game in gamelist.xml.", "Metadata Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
        }
    }
}
