using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace BatRun
{
    public class ShellCommand
    {
        public bool IsEnabled { get; set; }
        public string Path { get; set; } = string.Empty;
        public int DelaySeconds { get; set; }
        public int Order { get; set; }
        public bool IsCommand { get; set; }
        public bool AutoHide { get; set; }
        public bool DoubleLaunch { get; set; }
        public int DoubleLaunchDelay { get; set; }
        public bool OnlyOneInstance { get; set; }
        public bool LaunchRetroBatAtEnd { get; set; }
        public int RetroBatDelay { get; set; }
    }

    public partial class ShellConfigurationForm : Form
    {
        private ListView? commandListView;
        private readonly List<ShellCommand> commands = [];
        private readonly IniFile config;
        private readonly string commandsDirectory;
        private CheckBox? launchRetroBatCheckBox;
        private NumericUpDown? retroBatDelayNumeric;
        private Label retroBatDelayNumericLabel = new();

        public ShellConfigurationForm()
        {
            commands = [];
            config = new IniFile(Path.Combine(AppContext.BaseDirectory, "config.ini"));
            
            // Enregistrer le fournisseur d'encodage pour Windows-1252
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            
            // Initialiser le répertoire des commandes dans le dossier de l'application
            commandsDirectory = Path.Combine(AppContext.BaseDirectory, "ShellCommands");
                Directory.CreateDirectory(commandsDirectory);

            InitializeComponent();
            UpdateLocalizedTexts();
            LoadCommands();

            // Charger la configuration globale de RetroBAT
            if (launchRetroBatCheckBox != null)
                launchRetroBatCheckBox.Checked = config.ReadBool("Shell", "LaunchRetroBatAtEnd", false);
            if (retroBatDelayNumeric != null)
                retroBatDelayNumeric.Value = config.ReadInt("Shell", "RetroBatDelay", 0);
        }

        private void UpdateLocalizedTexts()
        {
            // Mettre à jour les textes du formulaire
            this.Text = LocalizedStrings.GetString("Shell Configuration");

            // Mettre à jour les textes des boutons
            foreach (Control control in this.Controls)
            {
                if (control is TableLayoutPanel mainLayout)
                {
                    foreach (Control panelControl in mainLayout.Controls)
                    {
                        if (panelControl is Panel configPanel)
                        {
                            foreach (Control ctrl in configPanel.Controls)
                            {
                                if (ctrl is CheckBox checkBox && checkBox.Text.Contains("Custom User Interface"))
                                {
                                    checkBox.Text = LocalizedStrings.GetString("Enable Custom User Interface in Group Policy");
                                }
                            }
                        }
                        else if (panelControl is FlowLayoutPanel buttonPanel)
                        {
                            foreach (Control button in buttonPanel.Controls)
                            {
                                if (button is Button btn)
                                {
                                    if (btn.Text == "Save")
                                        btn.Text = LocalizedStrings.GetString("Save");
                                    else if (btn.Text == "Cancel")
                                        btn.Text = LocalizedStrings.GetString("Cancel");
                                }
                            }
                        }
                    }
                }
            }
        }

        private void InitializeComponent()
        {
            // Configuration de base de la fenêtre
            this.Text = "Shell Configuration";
            this.ClientSize = new Size(850, 750);
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9F);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            // Création du layout principal
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                RowCount = 2,
                ColumnCount = 1
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            // Zone de configuration principale
            var configPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(10)
            };

            // Panneau supérieur pour les options RetroBAT
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.Transparent,
                Padding = new Padding(10)
            };

            // Première ligne : Enable Custom User Interface in Group Policy
            var windowsPolicyCheckBox = new CheckBox
            {
                Text = LocalizedStrings.GetString("Enable Custom User Interface in Group Policy"),
                AutoSize = true,
                Location = new Point(10, 10),
                BackColor = Color.Transparent,
                ForeColor = Color.White
            };
            topPanel.Controls.Add(windowsPolicyCheckBox);

            // Deuxième ligne : Launch RetroBAT at the end of the list avec Enable
            var retroBatPanel = new Panel
            {
                Location = new Point(10, 40),
                Width = 600,
                Height = 30,
                BackColor = Color.Transparent
            };

            launchRetroBatCheckBox = new()
            {
                Location = new Point(0, 5),
                Text = LocalizedStrings.GetString("Enable"),
                AutoSize = true
            };
            retroBatPanel.Controls.Add(launchRetroBatCheckBox);

            Label retroBatLabel = new()
            {
                AutoSize = true,
                Location = new Point(launchRetroBatCheckBox.Right + 5, 5),
                Text = LocalizedStrings.GetString("Launch RetroBAT at the end of the list")
            };
            retroBatPanel.Controls.Add(retroBatLabel);

            retroBatDelayNumeric = new()
            {
                Location = new Point(retroBatLabel.Right + 10, 3),
                Width = 40,
                Minimum = 0,
                Maximum = 40,
                Value = 0
            };
            retroBatDelayNumericLabel.Location = new Point(retroBatDelayNumeric.Right + 10, 5);
            retroBatDelayNumericLabel.Text = LocalizedStrings.GetString("Delay (seconds)");
            retroBatDelayNumericLabel.AutoSize = true;

            retroBatPanel.Controls.Add(retroBatDelayNumeric);
            retroBatPanel.Controls.Add(retroBatDelayNumericLabel);

            topPanel.Controls.Add(retroBatPanel);

            // Panneau des boutons d'action
            var actionPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 5, 0, 0)
            };

            var addButton = new Button
            {
                Text = "+",
                Width = 40,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Margin = new Padding(10, 0, 5, 0)
            };
            addButton.Click += AddButton_Click;

            var removeButton = new Button
            {
                Text = "-",
                Width = 40,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(204, 0, 0),
                ForeColor = Color.White,
                Margin = new Padding(0, 0, 5, 0)
            };
            removeButton.Click += RemoveButton_Click;

            actionPanel.Controls.Add(addButton);
            actionPanel.Controls.Add(removeButton);

            // ListView pour les commandes
            commandListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                CheckBoxes = true,
                AllowDrop = true,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            commandListView.Columns.Add("#", 40);
            commandListView.Columns.Add(LocalizedStrings.GetString("Path"), 240);
            commandListView.Columns.Add(LocalizedStrings.GetString("Enable"), 60);
            commandListView.Columns.Add(LocalizedStrings.GetString("Delay (seconds)"), 100);
            commandListView.Columns.Add(LocalizedStrings.GetString("Type"), 80);
            commandListView.Columns.Add(LocalizedStrings.GetString("Auto-Hide"), 100);
            commandListView.Columns.Add(LocalizedStrings.GetString("Double Launch"), 110);

            // Gérer le clic dans la colonne Enabled
            commandListView.ItemChecked += CommandListView_ItemChecked;

            commandListView.ItemDrag += CommandListView_ItemDrag;
            commandListView.DragEnter += CommandListView_DragEnter;
            commandListView.DragDrop += CommandListView_DragDrop;
            commandListView.MouseDoubleClick += CommandListView_MouseDoubleClick;
            commandListView.MouseClick += CommandListView_MouseClick;

            // Zone des boutons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent
            };

            var saveButton = new Button
            {
                Text = LocalizedStrings.GetString("Save"),
                Width = 100,
                Height = 25,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Margin = new Padding(5)
            };
            saveButton.Click += SaveButton_Click;

            var cancelButton = new Button
            {
                Text = LocalizedStrings.GetString("Cancel"),
                Width = 100,
                Height = 25,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(87, 87, 87),
                ForeColor = Color.White,
                Margin = new Padding(5)
            };
            cancelButton.Click += (s, e) => this.Close();

            buttonPanel.Controls.Add(saveButton);
            buttonPanel.Controls.Add(cancelButton);

            // Vérifier l'état initial de la clé de registre
            string checkScriptPath = Path.Combine(Path.GetTempPath(), "CheckShellKey.ps1");
            File.WriteAllText(checkScriptPath, @"
try {
    $userSID = [System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value
    $registryPath = ""Registry::HKEY_USERS\$userSID\Software\Microsoft\Windows\CurrentVersion\Policies\System""
    $shellValue = Get-ItemProperty -Path $registryPath -Name ""Shell"" -ErrorAction SilentlyContinue
    if ($shellValue -ne $null) { exit 1 } else { exit 0 }
} catch { exit 0 }");

            try
            {
                using var checkProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{checkScriptPath}\"",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });

                if (checkProcess is not null)
                {
                    checkProcess.WaitForExit();
                    windowsPolicyCheckBox.Checked = checkProcess.ExitCode == 1;
                }
            }
            catch
            {
                windowsPolicyCheckBox.Checked = false;
            }
            finally
            {
                if (File.Exists(checkScriptPath))
                {
                    try { File.Delete(checkScriptPath); } catch { }
                }
            }

            windowsPolicyCheckBox.CheckedChanged += WindowsPolicyCheckBox_CheckedChanged;

            // Ajouter les contrôles dans l'ordre inverse pour le bon empilement
            configPanel.Controls.Add(commandListView);
            configPanel.Controls.Add(actionPanel);
            configPanel.Controls.Add(topPanel);

            mainLayout.Controls.Add(configPanel, 0, 0);
            mainLayout.Controls.Add(buttonPanel, 0, 1);

            this.Controls.Add(mainLayout);
        }

        private void LoadCommands()
        {
            var tempCommands = new List<(ShellCommand command, int order)>();

            // Charger les commandes
            int commandCount = config.ReadInt("Shell", "CommandCount", 0);
            for (int i = 0; i < commandCount; i++)
            {
                string batchPath = config.ReadValue("Shell", $"Command{i}Path", "");
                if (File.Exists(batchPath))
                {
                    try
                    {
                        bool enabled = config.ReadBool("Shell", $"Command{i}Enabled", false);
                        int delay = config.ReadInt("Shell", $"Command{i}Delay", 0);
                        int order = config.ReadInt("Shell", $"Command{i}Order", i);
                        bool autoHide = config.ReadBool("Shell", $"Command{i}AutoHide", false);
                        bool doubleLaunch = config.ReadBool("Shell", $"Command{i}DoubleLaunch", false);
                        int doubleLaunchDelay = config.ReadInt("Shell", $"Command{i}DoubleLaunchDelay", 5);
                        bool launchRetroBatAtEnd = config.ReadBool("Shell", $"Command{i}LaunchRetroBatAtEnd", false);
                        int retroBatDelay = config.ReadInt("Shell", $"Command{i}RetroBatDelay", 0);
                        string commandText = File.ReadAllText(batchPath, Encoding.GetEncoding(1252));

                        var command = new ShellCommand
                        {
                            IsEnabled = enabled,
                            Path = commandText,
                            DelaySeconds = delay,
                            Order = order,
                            IsCommand = true,
                            AutoHide = autoHide,
                            DoubleLaunch = doubleLaunch,
                            DoubleLaunchDelay = doubleLaunchDelay,
                            LaunchRetroBatAtEnd = launchRetroBatAtEnd,
                            RetroBatDelay = retroBatDelay
                        };
                        tempCommands.Add((command, order));
                    }
                    catch { }
                }
            }

            // Charger les applications
            int appCount = config.ReadInt("Shell", "AppCount", 0);
            for (int i = 0; i < appCount; i++)
            {
                string path = config.ReadValue("Shell", $"App{i}Path", "");
                if (!string.IsNullOrEmpty(path))
                {
                    bool enabled = config.ReadBool("Shell", $"App{i}Enabled", false);
                    int delay = config.ReadInt("Shell", $"App{i}Delay", 0);
                    int order = config.ReadInt("Shell", $"App{i}Order", i);
                    bool autoHide = config.ReadBool("Shell", $"App{i}AutoHide", false);
                    bool doubleLaunch = config.ReadBool("Shell", $"App{i}DoubleLaunch", false);
                    int doubleLaunchDelay = config.ReadInt("Shell", $"App{i}DoubleLaunchDelay", 5);
                    bool onlyOneInstance = config.ReadBool("Shell", $"App{i}OnlyOneInstance", false);
                    bool launchRetroBatAtEnd = config.ReadBool("Shell", $"App{i}LaunchRetroBatAtEnd", false);
                    int retroBatDelay = config.ReadInt("Shell", $"App{i}RetroBatDelay", 0);

                    var command = new ShellCommand
                    {
                        IsEnabled = enabled,
                        Path = path,
                        DelaySeconds = delay,
                        Order = order,
                        IsCommand = false,
                        AutoHide = autoHide,
                        DoubleLaunch = doubleLaunch,
                        DoubleLaunchDelay = doubleLaunchDelay,
                        OnlyOneInstance = onlyOneInstance,
                        LaunchRetroBatAtEnd = launchRetroBatAtEnd,
                        RetroBatDelay = retroBatDelay
                    };
                    tempCommands.Add((command, order));
                }
            }

            // Trier les commandes par ordre
            commands.Clear();
            foreach (var (command, _) in tempCommands.OrderBy(x => x.order))
            {
                commands.Add(command);
                AddCommandToListView(command);
            }
        }

        private void AddCommandToListView(ShellCommand command)
        {
            if (commandListView == null) return;

            var item = new ListViewItem((commandListView.Items.Count + 1).ToString())
            {
                Tag = command
            };

            item.SubItems.Add(command.Path);
            item.SubItems.Add(command.IsEnabled ? "✓" : "");
            item.SubItems.Add(command.DelaySeconds.ToString());
            item.SubItems.Add(command.IsCommand ? LocalizedStrings.GetString("Command") : LocalizedStrings.GetString("Application"));
            item.SubItems.Add(command.AutoHide ? "✓" : "");
            item.SubItems.Add(command.DoubleLaunch ? "✓" : "");

            item.Checked = command.IsEnabled;
            commandListView.Items.Add(item);

            // Si c'est une application et que AutoHide est activé, ajouter son exécutable à la liste persistante
            if (!command.IsCommand && command.AutoHide)
            {
                string executableName = Path.GetFileName(command.Path);
                if (!string.IsNullOrEmpty(executableName))
                {
                    int currentCount = config.ReadInt("PersistentHiddenWindows", "Count", 0);
                    bool alreadyExists = false;

                    // Vérifier si l'exécutable existe déjà dans la liste
                    for (int i = 0; i < currentCount; i++)
                    {
                        string title = config.ReadValue("PersistentHiddenWindows", $"Window{i}", "");
                        if (title.Equals(executableName, StringComparison.OrdinalIgnoreCase))
                        {
                            alreadyExists = true;
                            break;
                        }
                    }

                    // Ajouter seulement si l'exécutable n'existe pas déjà
                    if (!alreadyExists)
                    {
                        config.WriteValue("PersistentHiddenWindows", $"Window{currentCount}", executableName);
                        config.WriteValue("PersistentHiddenWindows", "Count", (currentCount + 1).ToString());
                    }
                }
            }
        }

        private void AddButton_Click(object? sender, EventArgs e)
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add(LocalizedStrings.GetString("Add Application"), null, (s, e) => AddApplication());
            menu.Items.Add(LocalizedStrings.GetString("Add Command"), null, (s, e) => AddCommand());

            if (sender is Button button)
            {
                menu.Show(button, new Point(0, button.Height));
            }
        }

        private void AddApplication()
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "All Files (*.*)|*.*|Batch Files (*.bat)|*.bat|PowerShell Scripts (*.ps1)|*.ps1",
                FilterIndex = 1
            };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    ShowDelayDialog((delay) =>
                {
                    ShowOnlyOneInstanceDialog((onlyOneInstance) =>
                    {
                        var command = new ShellCommand
                        {
                            IsEnabled = true,
                            Path = dialog.FileName,
                            DelaySeconds = delay,
                            Order = commands.Count,
                            IsCommand = false,
                            OnlyOneInstance = onlyOneInstance
                        };
                        commands.Add(command);
                        AddCommandToListView(command);
                    });
                });
            }
        }

        private static void ShowOnlyOneInstanceDialog(Action<bool> onOptionSet)
        {
            using var instanceForm = new Form
            {
                Text = LocalizedStrings.GetString("Instance Settings"),
                Size = new Size(400, 150),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.White
            };

            var checkBox = new CheckBox
            {
                Text = LocalizedStrings.GetString("Allow only one instance of this application"),
                Location = new Point(20, 20),
                AutoSize = true,
                ForeColor = Color.White,
                Checked = false
            };

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(160, 70),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            instanceForm.Controls.AddRange(new Control[] { checkBox, okButton });
            instanceForm.AcceptButton = okButton;

            if (instanceForm.ShowDialog() == DialogResult.OK)
            {
                onOptionSet(checkBox.Checked);
            }
            else
            {
                onOptionSet(false);
            }
        }

        private void AddCommand()
        {
            using Form commandForm = new()
            {
                Text = "Enter Command",
                Size = new Size(600, 400),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(32, 32, 32),
                MaximizeBox = false,
                MinimizeBox = false
            };

                var textBox = new TextBox
                {
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    Size = new Size(560, 280),
                    Location = new Point(10, 10),
                    BackColor = Color.FromArgb(45, 45, 48),
                    ForeColor = Color.White,
                    Font = new Font("Consolas", 10F),
                    AcceptsReturn = true,
                    AcceptsTab = true
                };

                var okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(400, 310),
                    Size = new Size(80, 30),
                    BackColor = Color.FromArgb(0, 122, 204),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };

                var cancelButton = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(490, 310),
                    Size = new Size(80, 30),
                    BackColor = Color.FromArgb(87, 87, 87),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };

                commandForm.Controls.AddRange(new Control[] { textBox, okButton, cancelButton });
                commandForm.AcceptButton = okButton;
                commandForm.CancelButton = cancelButton;

                if (commandForm.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    ShowDelayDialog((delay) =>
                    {
                        var command = new ShellCommand
                        {
                            IsEnabled = true,
                            Path = textBox.Text,
                            DelaySeconds = delay,
                            Order = commands.Count,
                            IsCommand = true
                        };
                        commands.Add(command);
                        AddCommandToListView(command);
                    });
            }
        }

        private static void ShowDelayDialog(Action<int> onDelaySet)
        {
            using var delayForm = new Form
            {
                Text = "Set Delay",
                Size = new Size(300, 150),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.White
            };

                var numericUpDown = new NumericUpDown
                {
                    Location = new Point(20, 20),
                    Size = new Size(260, 30),
                    Minimum = 0,
                    Maximum = 3600,
                    Value = 0,
                    BackColor = Color.FromArgb(45, 45, 48),
                    ForeColor = Color.White
                };

                var okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(110, 70),
                    Size = new Size(80, 30),
                    BackColor = Color.FromArgb(0, 122, 204),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };

                delayForm.Controls.Add(numericUpDown);
                delayForm.Controls.Add(okButton);
                delayForm.AcceptButton = okButton;

                if (delayForm.ShowDialog() == DialogResult.OK)
                {
                    onDelaySet((int)numericUpDown.Value);
            }
        }

        private void RemoveButton_Click(object? sender, EventArgs e)
        {
            if (commandListView?.SelectedItems.Count > 0)
            {
                var item = commandListView.SelectedItems[0];
                var command = item.Tag as ShellCommand;
                if (command != null)
                {
                    commands.Remove(command);
                    commandListView.Items.Remove(item);
                    ReorderCommands();
                }
            }
        }

        private void CommandListView_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            if (commandListView is not null && e.Item is not null)
            {
                commandListView.DoDragDrop(e.Item, DragDropEffects.Move);
            }
        }

        private void CommandListView_DragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void CommandListView_DragDrop(object? sender, DragEventArgs e)
        {
            if (commandListView is null || e.Data is null) return;

            Point cp = commandListView.PointToClient(new Point(e.X, e.Y));
            ListViewItem? dragToItem = commandListView.GetItemAt(cp.X, cp.Y);
            if (e.Data.GetData(typeof(ListViewItem)) is ListViewItem draggedItem && dragToItem is not null && draggedItem != dragToItem)
            {
                int draggedIndex = draggedItem.Index;
                int dragToIndex = dragToItem.Index;

                // Réorganiser la liste
                ListViewItem insertItem = (ListViewItem)draggedItem.Clone();
                if (dragToIndex > draggedIndex)
                {
                    commandListView.Items.Insert(dragToIndex + 1, insertItem);
                    commandListView.Items.Remove(draggedItem);
                }
                else
                {
                    commandListView.Items.Remove(draggedItem);
                    commandListView.Items.Insert(dragToIndex, insertItem);
                }

                ReorderCommands();
            }
        }

        private void ReorderCommands()
        {
            if (commandListView == null) return;

            commands.Clear();
            for (int i = 0; i < commandListView.Items.Count; i++)
            {
                var item = commandListView.Items[i];
                if (item.Tag is ShellCommand command)
                {
                    command.Order = i;
                    commands.Add(command);
                    item.SubItems[0].Text = (i + 1).ToString();
                }
            }
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            // Nettoyer toutes les anciennes entrées de configuration
            int oldCommandCount = config.ReadInt("Shell", "CommandCount", 0);
            int oldAppCount = config.ReadInt("Shell", "AppCount", 0);

            for (int i = 0; i < oldCommandCount; i++)
            {
                string oldBatchPath = config.ReadValue("Shell", $"Command{i}Path", "");
                if (!string.IsNullOrEmpty(oldBatchPath) && File.Exists(oldBatchPath))
                {
                    try { File.Delete(oldBatchPath); } catch { }
                }
                config.WriteValue("Shell", $"Command{i}Path", "");
                config.WriteValue("Shell", $"Command{i}Enabled", "");
                config.WriteValue("Shell", $"Command{i}Delay", "");
                config.WriteValue("Shell", $"Command{i}Order", "");
                config.WriteValue("Shell", $"Command{i}AutoHide", "");
            }

            for (int i = 0; i < oldAppCount; i++)
            {
                config.WriteValue("Shell", $"App{i}Path", "");
                config.WriteValue("Shell", $"App{i}Enabled", "");
                config.WriteValue("Shell", $"App{i}Delay", "");
                config.WriteValue("Shell", $"App{i}Order", "");
                config.WriteValue("Shell", $"App{i}AutoHide", "");
            }

            int commandCount = 0;
            int appCount = 0;
            int order = 0;

            // Sauvegarder la configuration globale de RetroBAT
            config.WriteValue("Shell", "LaunchRetroBatAtEnd", launchRetroBatCheckBox?.Checked.ToString() ?? "False");
            config.WriteValue("Shell", "RetroBatDelay", retroBatDelayNumeric?.Value.ToString() ?? "0");

            foreach (var command in commands)
                {
                    if (command.IsCommand)
                    {
                        // Sauvegarder la commande dans un fichier batch
                        string batchFile = Path.Combine(commandsDirectory, $"command_{commandCount}.bat");
                        
                        // Utiliser l'encodage Windows-1252 (ANSI) pour les fichiers batch
                        File.WriteAllText(batchFile, command.Path, Encoding.GetEncoding(1252));
                        
                        // Sauvegarder les métadonnées dans config.ini
                        config.WriteValue("Shell", $"Command{commandCount}Path", batchFile);
                        config.WriteValue("Shell", $"Command{commandCount}Enabled", command.IsEnabled.ToString());
                        config.WriteValue("Shell", $"Command{commandCount}Delay", command.DelaySeconds.ToString());
                        config.WriteValue("Shell", $"Command{commandCount}Order", order.ToString());
                        config.WriteValue("Shell", $"Command{commandCount}AutoHide", command.AutoHide.ToString());
                        config.WriteValue("Shell", $"Command{commandCount}DoubleLaunch", command.DoubleLaunch.ToString());
                        config.WriteValue("Shell", $"Command{commandCount}DoubleLaunchDelay", command.DoubleLaunchDelay.ToString());
                        commandCount++;
                    }
                    else
                    {
                        // Sauvegarder l'application dans config.ini
                        config.WriteValue("Shell", $"App{appCount}Path", command.Path);
                        config.WriteValue("Shell", $"App{appCount}Enabled", command.IsEnabled.ToString());
                        config.WriteValue("Shell", $"App{appCount}Delay", command.DelaySeconds.ToString());
                        config.WriteValue("Shell", $"App{appCount}Order", order.ToString());
                        config.WriteValue("Shell", $"App{appCount}AutoHide", command.AutoHide.ToString());
                        config.WriteValue("Shell", $"App{appCount}DoubleLaunch", command.DoubleLaunch.ToString());
                        config.WriteValue("Shell", $"App{appCount}DoubleLaunchDelay", command.DoubleLaunchDelay.ToString());
                        config.WriteValue("Shell", $"App{appCount}OnlyOneInstance", command.OnlyOneInstance.ToString());
                        appCount++;
                    }
                order++;
            }

            config.WriteValue("Shell", "CommandCount", commandCount.ToString());
            config.WriteValue("Shell", "AppCount", appCount.ToString());
            
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void CommandListView_ItemChecked(object? sender, ItemCheckedEventArgs e)
        {
            if (e.Item.Tag is ShellCommand command)
            {
                command.IsEnabled = e.Item.Checked;
                e.Item.SubItems[2].Text = command.IsEnabled ? "✓" : "";
            }
        }

        private void CommandListView_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (commandListView?.SelectedItems.Count > 0)
            {
                var item = commandListView.SelectedItems[0];
                if (item.Tag is ShellCommand command)
                {
                    var hitInfo = commandListView.HitTest(e.X, e.Y);
                    if (hitInfo.SubItem is not null && hitInfo.Item is not null)
                    {
                        int columnIndex = hitInfo.Item.SubItems.IndexOf(hitInfo.SubItem);
                        if (columnIndex == 6) // Double Launch column
                        {
                            EditDoubleLaunchDelay(command, item);
                        }
                        else
                {
                    EditCommand(command, item);
                        }
                    }
                }
            }
        }

        private static void EditDoubleLaunchDelay(ShellCommand command, ListViewItem item)
        {
            using var delayForm = new Form
            {
                Text = LocalizedStrings.GetString("Double Launch Delay"),
                Size = new Size(300, 150),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.White
            };

            var numericUpDown = new NumericUpDown
            {
                Location = new Point(20, 20),
                Size = new Size(260, 30),
                Minimum = 1,
                Maximum = 3600,
                Value = command.DoubleLaunchDelay > 0 ? command.DoubleLaunchDelay : 5,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            var label = new Label
            {
                Text = LocalizedStrings.GetString("Delay between launches (seconds):"),
                Location = new Point(20, 50),
                AutoSize = true,
                ForeColor = Color.White
            };

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(110, 70),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            delayForm.Controls.AddRange(new Control[] { numericUpDown, label, okButton });
            delayForm.AcceptButton = okButton;

            if (delayForm.ShowDialog() == DialogResult.OK)
            {
                command.DoubleLaunchDelay = (int)numericUpDown.Value;
                if (!command.DoubleLaunch)
                {
                    command.DoubleLaunch = true;
                    item.SubItems[6].Text = "✓";
                }
            }
        }

        private static void EditCommand(ShellCommand command, ListViewItem item)
        {
            using var commandForm = new Form
            {
                Text = command.IsCommand ? "Edit Command" : "Edit Application Path",
                Size = new Size(800, 400),  // Retour à la taille d'origine
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(32, 32, 32),
                MaximizeBox = false,
                MinimizeBox = false
            };

                var textBox = new TextBox
                {
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                Size = new Size(760, 280),
                    Location = new Point(10, 10),
                    BackColor = Color.FromArgb(45, 45, 48),
                    ForeColor = Color.White,
                    Font = new Font("Consolas", 10F),
                    AcceptsReturn = true,
                AcceptsTab = true
            };

            if (command.IsCommand)
            {
                textBox.Text = command.Path;
            }
            else
            {
                textBox.Text = command.Path;
            }

                var browseButton = new Button
                {
                    Text = "Browse...",
                    Size = new Size(80, 30),
                Location = new Point(690, 10),
                    BackColor = Color.FromArgb(60, 60, 60),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Visible = !command.IsCommand
                };

                browseButton.Click += (s, e) =>
                {
                    using var dialog = new OpenFileDialog
                    {
                        Filter = "All Files (*.*)|*.*|Batch Files (*.bat)|*.bat|PowerShell Scripts (*.ps1)|*.ps1",
                        FilterIndex = 1,
                        FileName = textBox.Text
                    };
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        textBox.Text = dialog.FileName;
                    }
                };

                var delayLabel = new Label
                {
                    Text = "Delay (seconds):",
                    Location = new Point(10, 300),
                    AutoSize = true,
                    ForeColor = Color.White
                };

                var delayNumeric = new NumericUpDown
                {
                    Value = command.DelaySeconds,
                    Location = new Point(10, 320),
                    Width = 100,
                    Minimum = 0,
                    Maximum = 3600,
                    BackColor = Color.FromArgb(45, 45, 48),
                    ForeColor = Color.White
                };

            var onlyOneInstanceCheckBox = new CheckBox
            {
                Text = LocalizedStrings.GetString("Allow only one instance of this application"),
                Location = new Point(130, 320),
                AutoSize = true,
                ForeColor = Color.White,
                Checked = command.OnlyOneInstance,
                Visible = !command.IsCommand
                };

                var okButton = new Button
                {
                Text = "OK",
                    DialogResult = DialogResult.OK,
                Location = new Point(600, 320),
                    Size = new Size(80, 30),
                    BackColor = Color.FromArgb(0, 122, 204),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };

                var cancelButton = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                Location = new Point(690, 320),
                    Size = new Size(80, 30),
                    BackColor = Color.FromArgb(87, 87, 87),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };

            commandForm.Controls.AddRange(new Control[] {
                textBox, browseButton, delayLabel, delayNumeric,
                onlyOneInstanceCheckBox, okButton, cancelButton
            });

                commandForm.AcceptButton = okButton;
                commandForm.CancelButton = cancelButton;

                if (commandForm.ShowDialog() == DialogResult.OK)
                {
                    command.Path = textBox.Text;
                    command.DelaySeconds = (int)delayNumeric.Value;
                if (!command.IsCommand)
                {
                    command.OnlyOneInstance = onlyOneInstanceCheckBox.Checked;
                }

                    item.SubItems[1].Text = command.Path;
                item.SubItems[3].Text = command.DelaySeconds.ToString();
            }
        }

        private void WindowsPolicyCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                try
                {
                    // Demander confirmation avant de procéder
                    var result = MessageBox.Show(
                        checkBox.Checked
                            ? $"{LocalizedStrings.GetString("This action will configure BatRun as a custom shell.")}{Environment.NewLine}{Environment.NewLine}{LocalizedStrings.GetString("To restore Windows Explorer:")}{Environment.NewLine}{LocalizedStrings.GetString("- Uncheck this box")}{Environment.NewLine}{Environment.NewLine}{LocalizedStrings.GetString("Do you want to continue?")}"
                            : $"{LocalizedStrings.GetString("This action will restore the default Windows Explorer shell.")}{Environment.NewLine}{Environment.NewLine}{LocalizedStrings.GetString("Do you want to continue?")}",
                        checkBox.Checked ? LocalizedStrings.GetString("Custom Shell Configuration") : LocalizedStrings.GetString("Restore Default Shell"),
                        MessageBoxButtons.YesNo,
                        checkBox.Checked ? MessageBoxIcon.Warning : MessageBoxIcon.Question
                    );

                    if (result != DialogResult.Yes)
                    {
                        // Restaurer l'état précédent sans déclencher l'événement CheckedChanged
                        checkBox.CheckedChanged -= WindowsPolicyCheckBox_CheckedChanged;
                        checkBox.Checked = !checkBox.Checked;
                        checkBox.CheckedChanged += WindowsPolicyCheckBox_CheckedChanged;
                        return;
                    }

                    // Obtenir le SID de l'utilisateur actuel avant l'élévation des privilèges
                    string userSID = string.Empty;
                    string getSIDScriptPath = Path.Combine(Path.GetTempPath(), "GetUserSID.ps1");
                    File.WriteAllText(getSIDScriptPath, @"
[System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value");

                    try
                    {
                        var getSIDProcess = Process.Start(new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{getSIDScriptPath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        });

                        if (getSIDProcess != null)
                        {
                            userSID = getSIDProcess.StandardOutput.ReadToEnd().Trim();
                            getSIDProcess.WaitForExit();
                        }
                    }
                    finally
                    {
                        if (File.Exists(getSIDScriptPath))
                        {
                            try { File.Delete(getSIDScriptPath); } catch { }
                        }
                    }

                    if (string.IsNullOrEmpty(userSID))
                    {
                        throw new Exception(LocalizedStrings.GetString("Unable to retrieve user SID"));
                    }

                    // Créer un script PowerShell temporaire pour configurer le shell personnalisé
                    string tempScriptPath = Path.Combine(Path.GetTempPath(), "ConfigureShell.ps1");
                    string applicationPath = Application.ExecutablePath;

                    // Écrire le script PowerShell avec la création du point de restauration
                    var scriptBuilder = new StringBuilder();
                    scriptBuilder.AppendLine("# Script to configure custom shell");
                    scriptBuilder.AppendLine("$ErrorActionPreference = 'Stop'");
                    scriptBuilder.AppendLine();
                    scriptBuilder.AppendLine("try {");
                    
                    // Ajouter la création du point de restauration si on active le shell
                    if (checkBox.Checked)
                    {
                        scriptBuilder.AppendLine("    Write-Host 'Creating a system restore point...'");
                        scriptBuilder.AppendLine("    Enable-ComputerRestore -Drive $env:SystemDrive");
                        scriptBuilder.AppendLine("    Checkpoint-Computer -Description 'Before activating the BatRun shell' -RestorePointType MODIFY_SETTINGS");
                        scriptBuilder.AppendLine("    Write-Host 'Restore point created successfully.'");
                    }

                    scriptBuilder.AppendLine($"    $userSID = '{userSID}'");
                    scriptBuilder.AppendLine("    Write-Host \"Configuration for user with SID : $userSID\"");
                    scriptBuilder.AppendLine();
                    scriptBuilder.AppendLine("    $registryPath = \"Registry::HKEY_USERS\\$userSID\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\"");
                    
                    if (checkBox.Checked)
                    {
                        scriptBuilder.AppendLine("    if (!(Test-Path $registryPath)) {");
                        scriptBuilder.AppendLine("        Write-Host 'Creating the key : ' $registryPath");
                        scriptBuilder.AppendLine("        New-Item -Path $registryPath -Force | Out-Null");
                        scriptBuilder.AppendLine("    }");
                        scriptBuilder.AppendLine();
                        scriptBuilder.AppendLine($"    Write-Host 'Custom Shell Configuration : {applicationPath}'");
                        scriptBuilder.AppendLine($"    Set-ItemProperty -Path $registryPath -Name 'Shell' -Value '{applicationPath}' -Type String");
                        scriptBuilder.AppendLine("    Write-Host 'Custom shell configured successfully.'");
                    }
                    else
                    {
                        scriptBuilder.AppendLine("    if (Test-Path $registryPath) {");
                        scriptBuilder.AppendLine("        Write-Host 'Deleting the Shell Key...'");
                        scriptBuilder.AppendLine("        Remove-ItemProperty -Path $registryPath -Name 'Shell' -ErrorAction SilentlyContinue");
                        scriptBuilder.AppendLine("        Write-Host 'Configuration restored to default.'");
                        scriptBuilder.AppendLine("    } else {");
                        scriptBuilder.AppendLine("        Write-Host 'No configuration to delete.'");
                        scriptBuilder.AppendLine("    }");
                    }

                    scriptBuilder.AppendLine("} catch {");
                    scriptBuilder.AppendLine("    Write-Host \"Erreur: $($_.Exception.Message)\"");
                    scriptBuilder.AppendLine("    exit 1");
                    scriptBuilder.AppendLine("} finally {");
                    scriptBuilder.AppendLine("    Write-Host \"`nPress any key to continue...\"");
                    scriptBuilder.AppendLine("    $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')");
                    scriptBuilder.AppendLine("}");

                    File.WriteAllText(tempScriptPath, scriptBuilder.ToString());

                    // Créer un processus PowerShell avec élévation de privilèges
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"& '{tempScriptPath}'\"",
                        Verb = "runas",
                        CreateNoWindow = false,
                        WindowStyle = ProcessWindowStyle.Normal,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(tempScriptPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.System)
                    };

                    try
                    {
                        Process? process = Process.Start(startInfo);
                        if (process == null)
                        {
                            throw new Exception("Unable to start PowerShell process");
                        }

                        process.WaitForExit();

                        if (process.ExitCode != 0)
                        {
                            throw new Exception("PowerShell script execution failed");
                        }
                    }
                    catch (System.ComponentModel.Win32Exception ex) when ((uint)ex.ErrorCode == 0x80004005)
                    {
                        // L'utilisateur a annulé l'élévation des privilèges
                        MessageBox.Show(
                            LocalizedStrings.GetString("Operation cancelled. Administrative privileges are required."),
                            LocalizedStrings.GetString("Operation cancelled"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                        // Restaurer l'état précédent
                        checkBox.CheckedChanged -= WindowsPolicyCheckBox_CheckedChanged;
                        checkBox.Checked = !checkBox.Checked;
                        checkBox.CheckedChanged += WindowsPolicyCheckBox_CheckedChanged;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            string.Format(LocalizedStrings.GetString("An error occurred: {0}\nMake sure you have administrative privileges."), ex.Message),
                            LocalizedStrings.GetString("Error"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                        // Restaurer l'état précédent
                        checkBox.CheckedChanged -= WindowsPolicyCheckBox_CheckedChanged;
                        checkBox.Checked = !checkBox.Checked;
                        checkBox.CheckedChanged += WindowsPolicyCheckBox_CheckedChanged;
                    }
                    finally
                    {
                        // Nettoyer le fichier temporaire
                        if (File.Exists(tempScriptPath))
                        {
                            try { File.Delete(tempScriptPath); } catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        string.Format(LocalizedStrings.GetString("An error occurred: {0}\nMake sure you have administrative privileges."), ex.Message),
                        LocalizedStrings.GetString("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    // Restaurer l'état précédent
                    checkBox.CheckedChanged -= WindowsPolicyCheckBox_CheckedChanged;
                    checkBox.Checked = !checkBox.Checked;
                    checkBox.CheckedChanged += WindowsPolicyCheckBox_CheckedChanged;
                }
            }
        }

        private void CommandListView_MouseClick(object? sender, MouseEventArgs e)
        {
            if (commandListView is null) return;

            var hitInfo = commandListView.HitTest(e.X, e.Y);
            if (hitInfo is { SubItem: not null, Item: not null })
            {
                int columnIndex = hitInfo.Item.SubItems.IndexOf(hitInfo.SubItem);
                if (columnIndex == 5 && hitInfo.Item.Tag is ShellCommand command) // Auto-Hide column
                {
                    command.AutoHide = !command.AutoHide;
                    hitInfo.Item.SubItems[5].Text = command.AutoHide ? "✓" : "";

                    // Gérer la liste persistante si c'est une application
                    if (!command.IsCommand)
                    {
                        string executableName = Path.GetFileName(command.Path);
                        if (!string.IsNullOrEmpty(executableName))
                        {
                            // Nettoyer d'abord toutes les entrées existantes pour cet exécutable
                            int currentCount = config.ReadInt("PersistentHiddenWindows", "Count", 0);
                            var uniqueTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                            // Collecter toutes les entrées uniques sauf celle qu'on veut retirer
                            for (int i = 0; i < currentCount; i++)
                            {
                                string title = config.ReadValue("PersistentHiddenWindows", $"Window{i}", "");
                                if (!string.IsNullOrEmpty(title) && !title.Equals(executableName, StringComparison.OrdinalIgnoreCase))
                                {
                                    uniqueTitles.Add(title);
                                }
                                // Nettoyer l'ancienne entrée
                                config.WriteValue("PersistentHiddenWindows", $"Window{i}", "");
                            }

                            // Ajouter le nouvel exécutable si AutoHide est activé
                            if (command.AutoHide)
                            {
                                uniqueTitles.Add(executableName);
                            }

                            // Réécrire la liste mise à jour
                            int newIndex = 0;
                            foreach (string title in uniqueTitles)
                            {
                                config.WriteValue("PersistentHiddenWindows", $"Window{newIndex}", title);
                                newIndex++;
                            }
                            config.WriteValue("PersistentHiddenWindows", "Count", newIndex.ToString());
                        }
                    }
                }
                else if (columnIndex == 6 && hitInfo.Item.Tag is ShellCommand cmdItem) // Double Launch column
                {
                    if (!cmdItem.DoubleLaunch)
                    {
                        // Si non coché, on propose d'ajouter le délai
                        using var delayForm = new Form
                        {
                            Text = LocalizedStrings.GetString("Double Launch Delay"),
                            Size = new Size(300, 150),
                            FormBorderStyle = FormBorderStyle.FixedDialog,
                            StartPosition = FormStartPosition.CenterParent,
                            BackColor = Color.FromArgb(32, 32, 32),
                            ForeColor = Color.White
                        };

                        var numericUpDown = new NumericUpDown
                        {
                            Location = new Point(20, 20),
                            Size = new Size(260, 30),
                            Minimum = 1,
                            Maximum = 3600,
                            Value = cmdItem.DoubleLaunchDelay > 0 ? cmdItem.DoubleLaunchDelay : 5,
                            BackColor = Color.FromArgb(45, 45, 48),
                            ForeColor = Color.White
                        };

                        var label = new Label
                        {
                            Text = LocalizedStrings.GetString("Delay between launches (seconds):"),
                            Location = new Point(20, 50),
                            AutoSize = true,
                            ForeColor = Color.White
                        };

                        var okButton = new Button
                        {
                            Text = "OK",
                            DialogResult = DialogResult.OK,
                            Location = new Point(110, 70),
                            Size = new Size(80, 30),
                            BackColor = Color.FromArgb(0, 122, 204),
                            ForeColor = Color.White,
                            FlatStyle = FlatStyle.Flat
                        };

                        delayForm.Controls.AddRange(new Control[] { numericUpDown, label, okButton });
                        delayForm.AcceptButton = okButton;

                        if (delayForm.ShowDialog() == DialogResult.OK)
                        {
                            cmdItem.DoubleLaunch = true;
                            cmdItem.DoubleLaunchDelay = (int)numericUpDown.Value;
                            hitInfo.Item.SubItems[6].Text = "✓";
                        }
                    }
                    else
                    {
                        // Si déjà coché, on désactive simplement l'option
                        cmdItem.DoubleLaunch = false;
                        hitInfo.Item.SubItems[6].Text = "";
                    }
                }
            }
        }
    }
} 