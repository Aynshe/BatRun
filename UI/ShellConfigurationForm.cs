using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading.Tasks;

using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.UI
{
    public partial class ShellConfigurationForm : Form
    {
        private readonly List<ShellCommand> commands = [];
        private readonly IniFile config;
        private readonly string commandsDirectory;
        private string _postLaunchGamePath = string.Empty;
        private string _postLaunchGameDisplayName = string.Empty;
        private readonly Logger logger;
        private readonly ConfigurationForm? configForm;
        private readonly IBatRunProgram? program;
        private bool _isInitializing = false;

        public ShellConfigurationForm(IniFile config, Logger logger, ConfigurationForm? configForm = null, IBatRunProgram? program = null)
        {
            this.config = config;
            this.logger = logger;
            this.configForm = configForm;
            this.program = program;
            
            LocalizedStrings.LoadTranslations();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            
            commandsDirectory = Path.Combine(AppContext.BaseDirectory, "ShellCommands");
            Directory.CreateDirectory(commandsDirectory);

            _isInitializing = true;
            InitializeComponent();
            InitializeState();
            UpdateLocalizedTexts();
            _isInitializing = false;
        }

        private void InitializeState()
        {
            LoadCommands();

            launchRetroBatCheckBox.Checked = config.ReadBool("Shell", "LaunchRetroBatAtEnd", false);
            retroBatDelayNumeric.Value = config.ReadInt("Shell", "RetroBatDelay", 0);

            randomGameCheckBox.Checked = config.ReadBool("Shell", "LaunchRandomGame", false);
            if (randomGameCheckBox.Checked)
            {
                RandomGameCheckBox_CheckedChanged(randomGameCheckBox, EventArgs.Empty);
            }

            _postLaunchGameDisplayName = config.ReadValue("PostLaunch", "DisplayName", "");
            _postLaunchGamePath = config.ReadValue("PostLaunch", "GamePath", "");
            if (!string.IsNullOrEmpty(_postLaunchGameDisplayName))
            {
                postLaunchGameLabel.Text = $"Game to launch after RetroBat: {_postLaunchGameDisplayName}";
            }

            try
            {
                // EN: Check Shell Policy in Registry. Priority to CurrentUser (direct) followed by Users\SID
                // FR: Détection du Shell dans le registre. Priorité à CurrentUser (direct) puis Users\SID
                bool isShellActive = false;
                
                string policySubKey = @"Software\Microsoft\Windows\CurrentVersion\Policies\System";
                
                // Try HKCU first (Direct)
                using (var key = Registry.CurrentUser.OpenSubKey(policySubKey))
                {
                    if (key != null && key.GetValue("Shell") != null) isShellActive = true;
                }

                // If not found, try Users hive with SID (Cabinet might use specific user profile paths)
                if (!isShellActive)
                {
                    string? userSid = WindowsIdentity.GetCurrent().User?.Value;
                    if (!string.IsNullOrEmpty(userSid))
                    {
                        using var key = Registry.Users.OpenSubKey(userSid + "\\" + policySubKey);
                        if (key != null && key.GetValue("Shell") != null) isShellActive = true;
                    }
                }

                windowsPolicyCheckBox.Checked = isShellActive;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error checking Windows Shell policy: {ex.Message}");
                windowsPolicyCheckBox.Checked = false;
            }
        }

        private void UpdateLocalizedTexts()
        {
            this.Text = LocalizedStrings.GetString("Shell Configuration");
            windowsPolicyCheckBox.Text = LocalizedStrings.GetString("Enable Custom User Interface in Group Policy");
            launchRetroBatCheckBox.Text = LocalizedStrings.GetString("Enable");
            retroBatLabel.Text = LocalizedStrings.GetString("Launch RetroBAT at the end of the list");
            retroBatDelayNumericLabel.Text = LocalizedStrings.GetString("Delay (seconds)");
            randomGameCheckBox.Text = LocalizedStrings.GetString("Launch a random game at startup (Shell Launcher only)");
            saveButton.Text = LocalizedStrings.GetString("Save");
            cancelButton.Text = LocalizedStrings.GetString("Cancel");
        }

        private void ClearPostLaunchGameButton_Click(object? sender, EventArgs e)
        {
            _postLaunchGamePath = string.Empty;
            _postLaunchGameDisplayName = string.Empty;
            postLaunchGameLabel.Text = "Game to launch after RetroBat: None";
        }

        private async void RandomGameCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is CheckBox cb && cb.Checked)
            {
                randomSystemComboBox.Enabled = true;
                await PopulateRandomSystemComboBox();
            }
            else
            {
                randomSystemComboBox.Enabled = false;
                randomSystemComboBox.DataSource = null;
            }
        }

        private async Task PopulateRandomSystemComboBox()
        {
            randomSystemComboBox.Items.Clear();
            var scraper = new EmulationStationScraper(logger: logger);
            var systems = await scraper.GetSystemsAsync();

            var allSystemsItem = new SystemInfo { name = "all", fullname = "All Systems" };
            var dataSource = new List<SystemInfo> { allSystemsItem };
            dataSource.AddRange(systems);

            randomSystemComboBox.DataSource = dataSource;
            randomSystemComboBox.DisplayMember = "fullname";
            randomSystemComboBox.ValueMember = "name";

            string savedSystem = config.ReadValue("Shell", "RandomLaunchSystem", "all");
            var selectedSystem = dataSource.FirstOrDefault(s => s.name == savedSystem);
            if (selectedSystem != null) randomSystemComboBox.SelectedItem = selectedSystem;
        }

        private void ScrapButton_Click(object? sender, EventArgs e)
        {
            var scraper = new EmulationStationScraper(logger: logger);
            using var gameSelectionForm = new GameSelectionForm(scraper);
            if (gameSelectionForm.ShowDialog() == DialogResult.OK)
            {
                var selectedGame = gameSelectionForm.SelectedGame;
                var selectedSystem = gameSelectionForm.SelectedSystem;
                if (selectedGame != null && selectedSystem != null)
                {
                    _postLaunchGameDisplayName = $"{selectedGame.Name} ({selectedSystem.fullname})";
                    _postLaunchGamePath = selectedGame.Path ?? string.Empty;
                    postLaunchGameLabel.Text = $"Game to launch after RetroBat: {_postLaunchGameDisplayName}";
                }
            }
        }

        private void ArcadeButton_Click(object? sender, EventArgs e)
        {
            var arcadeManager = program?.ArcadeManager ?? configForm?.program?.ArcadeManager;
            using var form = new ArcadeConfigForm(this.config, arcadeManager);
            form.ShowDialog();
        }

        private void LoadCommands()
        {
            commandListView.BeginUpdate();
            try
            {
                var tempCommands = new List<(ShellCommand command, int order)>();
                int commandCount = config.ReadInt("Shell", "CommandCount", 0);
                for (int i = 0; i < commandCount; i++)
                {
                    string batchPath = config.ReadValue("Shell", $"Command{i}Path", "");
                    if (File.Exists(batchPath))
                    {
                        try
                        {
                            var command = new ShellCommand
                            {
                                IsEnabled = config.ReadBool("Shell", $"Command{i}Enabled", false),
                                Path = File.ReadAllText(batchPath, Encoding.GetEncoding(1252)),
                                DelaySeconds = config.ReadInt("Shell", $"Command{i}Delay", 0),
                                Order = config.ReadInt("Shell", $"Command{i}Order", i),
                                Type = CommandType.Command,
                                AutoHide = config.ReadBool("Shell", $"Command{i}AutoHide", false),
                                DoubleLaunch = config.ReadBool("Shell", $"Command{i}DoubleLaunch", false),
                                DoubleLaunchDelay = config.ReadInt("Shell", $"Command{i}DoubleLaunchDelay", 5),
                                LaunchRetroBatAtEnd = config.ReadBool("Shell", $"Command{i}LaunchRetroBatAtEnd", false),
                                RetroBatDelay = config.ReadInt("Shell", $"Command{i}RetroBatDelay", 0)
                            };
                            tempCommands.Add((command, command.Order));
                        }
                        catch { }
                    }
                }

                int appCount = config.ReadInt("Shell", "AppCount", 0);
                for (int i = 0; i < appCount; i++)
                {
                    string path = config.ReadValue("Shell", $"App{i}Path", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        var command = new ShellCommand
                        {
                            IsEnabled = config.ReadBool("Shell", $"App{i}Enabled", false),
                            Path = path,
                            DelaySeconds = config.ReadInt("Shell", $"App{i}Delay", 0),
                            Order = config.ReadInt("Shell", $"App{i}Order", i),
                            Type = CommandType.Application,
                            AutoHide = config.ReadBool("Shell", $"App{i}AutoHide", false),
                            DoubleLaunch = config.ReadBool("Shell", $"App{i}DoubleLaunch", false),
                            DoubleLaunchDelay = config.ReadInt("Shell", $"App{i}DoubleLaunchDelay", 5),
                            OnlyOneInstance = config.ReadBool("Shell", $"App{i}OnlyOneInstance", false),
                            LaunchRetroBatAtEnd = config.ReadBool("Shell", $"App{i}LaunchRetroBatAtEnd", false),
                            RetroBatDelay = config.ReadInt("Shell", $"App{i}RetroBatDelay", 0)
                        };
                        tempCommands.Add((command, command.Order));
                    }
                }

                commands.Clear();
                commandListView.Items.Clear();
                foreach (var (command, _) in tempCommands.OrderBy(x => x.order))
                {
                    commands.Add(command);
                    AddCommandToListView(command);
                }
            }
            finally
            {
                commandListView.EndUpdate();
            }
        }

        private void AddCommandToListView(ShellCommand command)
        {
            var item = new ListViewItem((commandListView.Items.Count + 1).ToString()) { Tag = command };
            item.SubItems.Add(command.Path);
            item.SubItems.Add(command.IsEnabled ? "\u2713" : "");
            item.SubItems.Add(command.DelaySeconds.ToString());
            item.SubItems.Add(GetCommandTypeString(command.Type));
            item.SubItems.Add(command.AutoHide ? "\u2713" : "");
            item.SubItems.Add(command.DoubleLaunch ? "\u2713" : "");
            item.Checked = command.IsEnabled;
            commandListView.Items.Add(item);
        }

        private string GetCommandTypeString(CommandType type) => type switch
        {
            CommandType.Application => LocalizedStrings.GetString("Application"),
            CommandType.Command => LocalizedStrings.GetString("Command"),
            _ => string.Empty,
        };

        private void AddButton_Click(object? sender, EventArgs e)
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add(LocalizedStrings.GetString("Add Application"), null, (s, ev) => AddApplication());
            menu.Items.Add(LocalizedStrings.GetString("Add Command"), null, (s, ev) => AddCommand());
            if (sender is Button btn) menu.Show(btn, new Point(0, btn.Height));
        }

        private void AddApplication()
        {
            using var dialog = new OpenFileDialog { Filter = "All Files (*.*)|*.*|Batch Files (*.bat)|*.bat|PowerShell Scripts (*.ps1)|*.ps1" };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ShowDelayDialog((delay) =>
                {
                    ShowOnlyOneInstanceDialog((onlyOneInstance) =>
                    {
                        var command = new ShellCommand { IsEnabled = true, Path = dialog.FileName, DelaySeconds = delay, Order = commands.Count, Type = CommandType.Application, OnlyOneInstance = onlyOneInstance };
                        commands.Add(command);
                        AddCommandToListView(command);
                    });
                });
            }
        }

        private static void ShowOnlyOneInstanceDialog(Action<bool> onOptionSet)
        {
            using var form = new Form { Text = LocalizedStrings.GetString("Instance Settings"), Size = new Size(400, 150), FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, BackColor = Color.FromArgb(32, 32, 32), ForeColor = Color.White };
            var cb = new CheckBox { Text = LocalizedStrings.GetString("Allow only one instance of this application"), Location = new Point(20, 20), AutoSize = true, Checked = false };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(160, 70), Size = new Size(80, 30), BackColor = Color.FromArgb(0, 122, 204), FlatStyle = FlatStyle.Flat };
            form.Controls.AddRange(new Control[] { cb, ok });
            if (form.ShowDialog() == DialogResult.OK) onOptionSet(cb.Checked); else onOptionSet(false);
        }

        private void AddCommand()
        {
            using var form = new Form { Text = "Enter Command", Size = new Size(600, 400), FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, BackColor = Color.FromArgb(32, 32, 32) };
            var tb = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Size = new Size(560, 280), Location = new Point(10, 10), BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, Font = new Font("Consolas", 10F) };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(400, 310), Size = new Size(80, 30), BackColor = Color.FromArgb(0, 122, 204), FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(490, 310), Size = new Size(80, 30), BackColor = Color.FromArgb(87, 87, 87), FlatStyle = FlatStyle.Flat };
            form.Controls.AddRange(new Control[] { tb, ok, cancel });
            if (form.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(tb.Text))
            {
                ShowDelayDialog((delay) =>
                {
                    var cmd = new ShellCommand { IsEnabled = true, Path = tb.Text, DelaySeconds = delay, Order = commands.Count, Type = CommandType.Command };
                    commands.Add(cmd);
                    AddCommandToListView(cmd);
                });
            }
        }

        private static void ShowDelayDialog(Action<int> onDelaySet)
        {
            using var form = new Form { Text = "Set Delay", Size = new Size(300, 150), FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, BackColor = Color.FromArgb(32, 32, 32), ForeColor = Color.White };
            var num = new NumericUpDown { Location = new Point(20, 20), Size = new Size(260, 30), Minimum = 0, Maximum = 3600, Value = 0, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(110, 70), Size = new Size(80, 30), BackColor = Color.FromArgb(0, 122, 204), FlatStyle = FlatStyle.Flat };
            form.Controls.AddRange(new Control[] { num, ok });
            if (form.ShowDialog() == DialogResult.OK) onDelaySet((int)num.Value);
        }

        private void RemoveButton_Click(object? sender, EventArgs e)
        {
            if (commandListView?.SelectedItems.Count > 0)
            {
                var item = commandListView.SelectedItems[0];
                if (item.Tag is ShellCommand cmd)
                {
                    commands.Remove(cmd);
                    commandListView.Items.Remove(item);
                    ReorderCommands();
                }
            }
        }

        private void ReorderCommands()
        {
            commands.Clear();
            for (int i = 0; i < commandListView.Items.Count; i++)
            {
                if (commandListView.Items[i].Tag is ShellCommand cmd)
                {
                    cmd.Order = i;
                    commands.Add(cmd);
                    commandListView.Items[i].SubItems[0].Text = (i + 1).ToString();
                }
            }
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            int oldCmdCount = config.ReadInt("Shell", "CommandCount", 0);
            int oldAppCount = config.ReadInt("Shell", "AppCount", 0);
            for (int i = 0; i < oldCmdCount; i++)
            {
                string path = config.ReadValue("Shell", $"Command{i}Path", "");
                if (!string.IsNullOrEmpty(path) && File.Exists(path)) try { File.Delete(path); } catch { }
                config.WriteValue("Shell", $"Command{i}Path", ""); config.WriteValue("Shell", $"Command{i}Enabled", "");
                config.WriteValue("Shell", $"Command{i}Delay", ""); config.WriteValue("Shell", $"Command{i}Order", "");
                config.WriteValue("Shell", $"Command{i}AutoHide", "");
            }
            for (int i = 0; i < oldAppCount; i++)
            {
                config.WriteValue("Shell", $"App{i}Path", ""); config.WriteValue("Shell", $"App{i}Enabled", "");
                config.WriteValue("Shell", $"App{i}Delay", ""); config.WriteValue("Shell", $"App{i}Order", "");
                config.WriteValue("Shell", $"App{i}AutoHide", "");
            }

            config.WriteValue("Shell", "LaunchRetroBatAtEnd", launchRetroBatCheckBox.Checked.ToString());
            config.WriteValue("Shell", "RetroBatDelay", retroBatDelayNumeric.Value.ToString());
            config.WriteValue("Shell", "LaunchRandomGame", randomGameCheckBox.Checked.ToString());
            config.WriteValue("PostLaunch", "DisplayName", _postLaunchGameDisplayName);
            config.WriteValue("PostLaunch", "GamePath", _postLaunchGamePath);

            if (randomGameCheckBox.Checked && randomSystemComboBox.SelectedItem is SystemInfo info)
                config.WriteValue("Shell", "RandomLaunchSystem", info.name ?? "all");
            else
                config.WriteValue("Shell", "RandomLaunchSystem", "all");

            int cmdIdx = 0, appIdx = 0, order = 0;
            foreach (var cmd in commands)
            {
                if (cmd.Type == CommandType.Command)
                {
                    string path = Path.Combine(commandsDirectory, $"command_{cmdIdx}.bat");
                    File.WriteAllText(path, cmd.Path, Encoding.GetEncoding(1252));
                    config.WriteValue("Shell", $"Command{cmdIdx}Path", path);
                    config.WriteValue("Shell", $"Command{cmdIdx}Enabled", cmd.IsEnabled.ToString());
                    config.WriteValue("Shell", $"Command{cmdIdx}Delay", cmd.DelaySeconds.ToString());
                    config.WriteValue("Shell", $"Command{cmdIdx}Order", order.ToString());
                    config.WriteValue("Shell", $"Command{cmdIdx}AutoHide", cmd.AutoHide.ToString());
                    config.WriteValue("Shell", $"Command{cmdIdx}DoubleLaunch", cmd.DoubleLaunch.ToString());
                    config.WriteValue("Shell", $"Command{cmdIdx}DoubleLaunchDelay", cmd.DoubleLaunchDelay.ToString());
                    cmdIdx++;
                }
                else
                {
                    config.WriteValue("Shell", $"App{appIdx}Path", cmd.Path);
                    config.WriteValue("Shell", $"App{appIdx}Enabled", cmd.IsEnabled.ToString());
                    config.WriteValue("Shell", $"App{appIdx}Delay", cmd.DelaySeconds.ToString());
                    config.WriteValue("Shell", $"App{appIdx}Order", order.ToString());
                    config.WriteValue("Shell", $"App{appIdx}AutoHide", cmd.AutoHide.ToString());
                    config.WriteValue("Shell", $"App{appIdx}DoubleLaunch", cmd.DoubleLaunch.ToString());
                    config.WriteValue("Shell", $"App{appIdx}DoubleLaunchDelay", cmd.DoubleLaunchDelay.ToString());
                    config.WriteValue("Shell", $"App{appIdx}OnlyOneInstance", cmd.OnlyOneInstance.ToString());
                    appIdx++;
                }
                order++;
            }
            config.WriteValue("Shell", "CommandCount", cmdIdx.ToString());
            config.WriteValue("Shell", "AppCount", appIdx.ToString());
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void CommandListView_ItemChecked(object? sender, ItemCheckedEventArgs e)
        {
            if (e.Item.Tag is ShellCommand cmd) { cmd.IsEnabled = e.Item.Checked; e.Item.SubItems[2].Text = cmd.IsEnabled ? "\u2713" : ""; }
        }

        private void CommandListView_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (commandListView.SelectedItems.Count > 0)
            {
                var item = commandListView.SelectedItems[0];
                if (item.Tag is ShellCommand cmd)
                {
                    var hit = commandListView.HitTest(e.X, e.Y);
                    if (hit.SubItem != null && hit.Item != null && hit.Item.SubItems.IndexOf(hit.SubItem) == 6)
                        EditDoubleLaunchDelay(cmd, item);
                    else
                        EditCommand(cmd, item);
                }
            }
        }

        private static void EditDoubleLaunchDelay(ShellCommand command, ListViewItem item)
        {
            using var form = new Form { Text = LocalizedStrings.GetString("Double Launch Delay"), Size = new Size(300, 150), FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, BackColor = Color.FromArgb(32, 32, 32), ForeColor = Color.White };
            var num = new NumericUpDown { Location = new Point(20, 20), Size = new Size(260, 30), Minimum = 1, Maximum = 3600, Value = command.DoubleLaunchDelay > 0 ? command.DoubleLaunchDelay : 5, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(110, 70), Size = new Size(80, 30), BackColor = Color.FromArgb(0, 122, 204), FlatStyle = FlatStyle.Flat };
            form.Controls.AddRange(new Control[] { num, ok });
            if (form.ShowDialog() == DialogResult.OK) { command.DoubleLaunchDelay = (int)num.Value; if (!command.DoubleLaunch) { command.DoubleLaunch = true; item.SubItems[6].Text = "\u2713"; } }
        }

        private static void EditCommand(ShellCommand command, ListViewItem item)
        {
            using var form = new Form { Text = "Edit", Size = new Size(800, 400), FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, BackColor = Color.FromArgb(32, 32, 32) };
            var tb = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Size = new Size(760, 280), Location = new Point(10, 10), BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, Font = new Font("Consolas", 10F), Text = command.Path };
            var num = new NumericUpDown { Value = command.DelaySeconds, Location = new Point(10, 320), Width = 100, Minimum = 0, Maximum = 3600 };
            var cb = new CheckBox { Text = LocalizedStrings.GetString("Allow only one instance"), Location = new Point(130, 320), AutoSize = true, Checked = command.OnlyOneInstance, Visible = command.Type == CommandType.Application };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(600, 320), Size = new Size(80, 30), BackColor = Color.FromArgb(0, 122, 204), FlatStyle = FlatStyle.Flat };
            form.Controls.AddRange(new Control[] { tb, num, cb, ok });
            if (form.ShowDialog() == DialogResult.OK) { command.Path = tb.Text; command.DelaySeconds = (int)num.Value; if (command.Type == CommandType.Application) command.OnlyOneInstance = cb.Checked; item.SubItems[1].Text = command.Path; item.SubItems[3].Text = command.DelaySeconds.ToString(); }
        }

        private void CommandListView_MouseClick(object? sender, MouseEventArgs e)
        {
            var hit = commandListView.HitTest(e.X, e.Y);
            if (hit.SubItem != null && hit.Item != null && hit.Item.Tag is ShellCommand cmd)
            {
                int col = hit.Item.SubItems.IndexOf(hit.SubItem);
                if (col == 5) { cmd.AutoHide = !cmd.AutoHide; hit.Item.SubItems[5].Text = cmd.AutoHide ? "\u2713" : ""; UpdatePersistentHiddenWindows(cmd); }
                else if (col == 6) { if (!cmd.DoubleLaunch) EditDoubleLaunchDelay(cmd, hit.Item); else { cmd.DoubleLaunch = false; hit.Item.SubItems[6].Text = ""; } }
            }
        }

        private void UpdatePersistentHiddenWindows(ShellCommand command)
        {
            if (command.Type != CommandType.Application) return;
            string exe = Path.GetFileName(command.Path);
            if (string.IsNullOrEmpty(exe)) return;
            int count = config.ReadInt("PersistentHiddenWindows", "Count", 0);
            var titles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < count; i++)
            {
                string t = config.ReadValue("PersistentHiddenWindows", $"Window{i}", "");
                if (!string.IsNullOrEmpty(t) && !t.Equals(exe, StringComparison.OrdinalIgnoreCase)) titles.Add(t);
                config.WriteValue("PersistentHiddenWindows", $"Window{i}", "");
            }
            if (command.AutoHide) titles.Add(exe);
            int idx = 0;
            foreach (string t in titles) { config.WriteValue("PersistentHiddenWindows", $"Window{idx}", t); idx++; }
            config.WriteValue("PersistentHiddenWindows", "Count", idx.ToString());
        }

        private void WindowsPolicyCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is not CheckBox cb || _isInitializing) return;
            try
            {
                config.WriteValue("Shell", "EnableCustomUI", cb.Checked.ToString());
                if (cb.Checked)
                {
                    string startupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "BatRun.lnk");
                    if (File.Exists(startupPath)) try { File.Delete(startupPath); } catch { }
                    try { using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true); key?.DeleteValue("BatRun", false); } catch { }
                }
                configForm?.UpdateStartupState(cb.Checked);
                var result = MessageBox.Show(cb.Checked ? LocalizedStrings.GetString("This action will configure BatRun as a custom shell.") : LocalizedStrings.GetString("This action will restore the default Windows Explorer shell."), LocalizedStrings.GetString("Confirmation"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result != DialogResult.Yes) { cb.CheckedChanged -= WindowsPolicyCheckBox_CheckedChanged; cb.Checked = !cb.Checked; cb.CheckedChanged += WindowsPolicyCheckBox_CheckedChanged; return; }
                
                string psScript = Path.Combine(Path.GetTempPath(), "ConfigureShell.ps1");
                var sb = new StringBuilder();
                sb.AppendLine("$userSID = [System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value");
                sb.AppendLine("$regPath = \"Registry::HKEY_USERS\\$userSID\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\"");
                if (cb.Checked) { sb.AppendLine("if (!(Test-Path $regPath)) { New-Item -Path $regPath -Force | Out-Null }"); sb.AppendLine($"Set-ItemProperty -Path $regPath -Name 'Shell' -Value '{Application.ExecutablePath}' -Type String"); }
                else { sb.AppendLine("if (Test-Path $regPath) { Remove-ItemProperty -Path $regPath -Name 'Shell' -ErrorAction SilentlyContinue }"); }
                File.WriteAllText(psScript, sb.ToString());
                Process.Start(new ProcessStartInfo { FileName = "powershell.exe", Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{psScript}\"", Verb = "runas", UseShellExecute = true })?.WaitForExit();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); cb.CheckedChanged -= WindowsPolicyCheckBox_CheckedChanged; cb.Checked = !cb.Checked; cb.CheckedChanged += WindowsPolicyCheckBox_CheckedChanged; }
        }

        private void CancelButton_Click(object? sender, EventArgs e) => this.Close();
        private void CommandListView_ItemDrag(object? sender, ItemDragEventArgs e) { if (e.Item != null) DoDragDrop(e.Item, DragDropEffects.Move); }
        private void CommandListView_DragEnter(object? sender, DragEventArgs e) => e.Effect = DragDropEffects.Move;
        private void CommandListView_DragDrop(object? sender, DragEventArgs e)
        {
            var cp = commandListView.PointToClient(new Point(e.X, e.Y));
            var target = commandListView.GetItemAt(cp.X, cp.Y);
            if (e.Data?.GetData(typeof(ListViewItem)) is ListViewItem dragged && target != null && dragged != target)
            {
                int tidx = target.Index;
                commandListView.Items.Remove(dragged);
                commandListView.Items.Insert(tidx, dragged);
                ReorderCommands();
            }
        }
    }
}

