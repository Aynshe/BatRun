using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;

using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.UI
{
    // EN: Form for selecting a game from EmulationStation.
    // FR: Formulaire pour sélectionner un jeu depuis EmulationStation.
    public partial class GameSelectionForm : Form
    {
        private readonly EmulationStationScraper _scraper;

        public Game? SelectedGame { get; private set; }
        public SystemInfo? SelectedSystem { get; private set; }

        public GameSelectionForm(EmulationStationScraper scraper)
        {
            _scraper = scraper;
            InitializeComponent();
            this.Load += async (s, e) => await LoadSystemsAsync();
        }

        private async Task LoadSystemsAsync()
        {
            if (_statusLabel == null || _systemComboBox == null) return;

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
            if (_systemComboBox.SelectedItem is SystemInfo selectedSystem && _gameListView != null && _statusLabel != null)
            {
                _statusLabel.Text = $"Loading games for {selectedSystem.fullname}...";
                _gameListView.Items.Clear();
                _okButton.Enabled = false;

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
            if (_gameListView.SelectedItems.Count > 0 && _systemComboBox.SelectedItem is SystemInfo selectedSystem)
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
            if (_gameListView.SelectedItems.Count > 0)
            {
                _okButton.Enabled = true;
            }
            else
            {
                _okButton.Enabled = false;
            }
        }

        private void GameListView_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (_gameListView.SelectedItems.Count > 0 && _systemComboBox.SelectedItem is SystemInfo selectedSystem)
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
                        MessageBox.Show("Could determine RetroBat installation directory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                    var metadataForm = new GameMetadataForm(selectedGame, gamelistPath, retrobatRoot);
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


