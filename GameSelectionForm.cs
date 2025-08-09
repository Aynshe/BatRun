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
        private ComboBox? _systemComboBox;
        private ListView? _gameListView;
        private Button? _okButton;
        private Button? _cancelButton;
        private Label? _statusLabel;

        public Game? SelectedGame { get; private set; }
        public SystemInfo? SelectedSystem { get; private set; }

        public GameSelectionForm(EmulationStationScraper scraper)
        {
            _scraper = scraper;
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
            _gameListView.MouseDoubleClick += GameListView_MouseDoubleClick;
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

        private void GameListView_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (_gameListView?.SelectedItems.Count > 0 && _systemComboBox?.SelectedItem is SystemInfo selectedSystem)
            {
                var selectedGame = _gameListView.SelectedItems[0].Tag as Game;
                if (selectedGame == null) return;

                try
                {
                    var logger = new Logger("BatRun_metadata.log", true);
                    var config = new IniFile("config.ini");
                    var retrobatService = new RetroBatService(logger, config);

                    string retrobatExePath = retrobatService.GetRetrobatPath();
                    string retrobatRoot = System.IO.Path.GetDirectoryName(retrobatExePath) ?? "";

                    if (string.IsNullOrEmpty(retrobatRoot) || !System.IO.Directory.Exists(retrobatRoot))
                    {
                        MessageBox.Show("Could not determine RetroBat installation directory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    if (string.IsNullOrEmpty(selectedSystem.name))
                    {
                        MessageBox.Show("Selected system has no name, cannot find gamelist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    string gamelistPath = System.IO.Path.Combine(retrobatRoot, "roms", selectedSystem.name, "gamelist.xml");
                    logger.Log($"Attempting to access gamelist for '{selectedSystem.fullname}' at: {gamelistPath}");

                    if (!System.IO.File.Exists(gamelistPath))
                    {
                        logger.LogWarning($"gamelist.xml not found for system '{selectedSystem.fullname}'.");
                        MessageBox.Show($"gamelist.xml not found for system '{selectedSystem.fullname}'.\nExpected at: {gamelistPath}", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    logger.Log($"Found gamelist.xml. Opening metadata view for '{selectedGame.Name}'.");
                    if (!string.IsNullOrEmpty(selectedGame.PlayUrl))
                    {
                        logger.Log($"Game launch URL: {selectedGame.PlayUrl}");
                    }

                    var metadataForm = new GameMetadataForm(selectedGame, gamelistPath);
                    metadataForm.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while trying to show game metadata:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
