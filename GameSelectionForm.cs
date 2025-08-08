using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BatRun
{
    public partial class GameSelectionForm : Form
    {
        private readonly EmulationStationApi esApi;
        private readonly Logger logger;
        private List<SystemInfo> systems;
        private SystemInfo? selectedSystem;

        public Game? SelectedGame { get; private set; }
        public bool SelectRandomGame { get; private set; }

        public GameSelectionForm(Logger logger)
        {
            this.logger = logger;
            this.esApi = new EmulationStationApi(logger);
            InitializeComponent();
        }

        private async void GameSelectionForm_Load(object sender, EventArgs e)
        {
            if (!await esApi.IsApiAvailableAsync())
            {
                MessageBox.Show("EmulationStation API is not available. Please start RetroBat and ensure the web server is enabled.", "API Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Close();
                return;
            }
            await LoadSystems();
        }

        private async Task LoadSystems()
        {
            titleLabel.Text = "Select System";
            systemListBox.Visible = true;
            gameListBox.Visible = false;
            backButton.Visible = false;

            systems = await esApi.GetSystemsAsync();
            systemListBox.Items.Clear();
            foreach (var system in systems.Where(s => s.totalGames > 0))
            {
                systemListBox.Items.Add(system.fullname);
            }
        }

        private async void SystemListBox_DoubleClick(object sender, EventArgs e)
        {
            if (systemListBox.SelectedItem == null) return;

            string selectedSystemName = systemListBox.SelectedItem.ToString();
            selectedSystem = systems.FirstOrDefault(s => s.fullname == selectedSystemName);

            if (selectedSystem != null)
            {
                await LoadGamesForSystem(selectedSystem);
            }
        }

        private async Task LoadGamesForSystem(SystemInfo system)
        {
            titleLabel.Text = $"Select Game in {system.fullname}";
            systemListBox.Visible = false;
            gameListBox.Visible = true;
            backButton.Visible = true;

            var games = await esApi.GetGamesForSystemAsync(system.name);
            gameListBox.Items.Clear();
            gameListBox.DataSource = games;
            gameListBox.DisplayMember = "Name";
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            // Don't need to reload systems, just change visibility
            titleLabel.Text = "Select System";
            systemListBox.Visible = true;
            gameListBox.Visible = false;
            backButton.Visible = false;
            gameListBox.DataSource = null;
        }

        private void GameListBox_DoubleClick(object sender, EventArgs e)
        {
            SelectButton_Click(sender, e);
        }

        private void SelectButton_Click(object sender, EventArgs e)
        {
            if (randomCheckBox.Checked)
            {
                SelectRandomGame = true;
            }
            else
            {
                if (gameListBox.Visible && gameListBox.SelectedItem is Game selectedGame)
                {
                    SelectedGame = selectedGame;
                }
                else if (systemListBox.Visible && systemListBox.SelectedItem != null)
                {
                    // If user clicks select on a system, show games for that system
                    SystemListBox_DoubleClick(sender, e);
                    return; // Don't close the form yet
                }
                else
                {
                    MessageBox.Show("Please select a game or choose the random option.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void RandomCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            bool isChecked = randomCheckBox.Checked;
            systemListBox.Enabled = !isChecked;
            gameListBox.Enabled = !isChecked;
            selectButton.Enabled = true; // Always allow select
        }
    }
}
