using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.IO;

namespace BatRun
{
    public class ShortcutsForm : Form
    {
        private readonly IniFile config;
        private readonly Logger logger;
        private ListView? shortcutListView;
        private readonly List<(string Name, string Path)> shortcuts = [];

        public ShortcutsForm(IniFile config, Logger logger)
        {
            this.config = config;
            this.logger = logger;

            InitializeComponent();
            LoadShortcuts();
        }

        private void InitializeComponent()
        {
            // Configuration de base de la fenêtre
            this.Text = LocalizedStrings.GetString("Shortcuts Interface");
            this.Size = new Size(800, 500);
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Création du layout principal
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                RowCount = 2,
                ColumnCount = 1
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            // Panneau principal
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            // Panneau des boutons d'action
            var actionPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent
            };

            var addButton = new Button
            {
                Text = "+",
                Width = 40,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Margin = new Padding(0, 0, 5, 0)
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

            // ListView pour les raccourcis
            shortcutListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            shortcutListView.Columns.Add(LocalizedStrings.GetString("Name"), 200);
            shortcutListView.Columns.Add(LocalizedStrings.GetString("Path"), 550);

            shortcutListView.MouseDoubleClick += ShortcutListView_MouseDoubleClick;

            // Panneau des boutons
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

            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(saveButton);

            // Ajouter les contrôles dans l'ordre
            mainPanel.Controls.Add(shortcutListView);
            mainPanel.Controls.Add(actionPanel);

            mainLayout.Controls.Add(mainPanel, 0, 0);
            mainLayout.Controls.Add(buttonPanel, 0, 1);

            this.Controls.Add(mainLayout);
        }

        private void LoadShortcuts()
        {
            if (shortcutListView == null) return;

            shortcuts.Clear();
            shortcutListView.Items.Clear();

            int count = config.ReadInt("Shortcuts", "Count", 0);
            for (int i = 0; i < count; i++)
            {
                string name = config.ReadValue("Shortcuts", $"Name{i}", "");
                string path = config.ReadValue("Shortcuts", $"Path{i}", "");

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(path))
                {
                    shortcuts.Add((name, path));
                    var item = new ListViewItem(name);
                    item.SubItems.Add(path);
                    shortcutListView.Items.Add(item);
                }
            }
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // Effacer les anciens raccourcis
                int oldCount = config.ReadInt("Shortcuts", "Count", 0);
                for (int i = 0; i < oldCount; i++)
                {
                    config.WriteValue("Shortcuts", $"Name{i}", "");
                    config.WriteValue("Shortcuts", $"Path{i}", "");
                }

                // Sauvegarder les nouveaux raccourcis
                for (int i = 0; i < shortcuts.Count; i++)
                {
                    var shortcut = shortcuts[i];
                    config.WriteValue("Shortcuts", $"Name{i}", shortcut.Name);
                    config.WriteValue("Shortcuts", $"Path{i}", shortcut.Path);
                }
                config.WriteValue("Shortcuts", "Count", shortcuts.Count.ToString());

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                logger.LogError($"Error saving shortcuts: {ex.Message}", ex);
                MessageBox.Show(
                    $"Error saving shortcuts: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void AddButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "All Files (*.*)|*.*|Applications (*.exe)|*.exe|Shortcuts (*.lnk)|*.lnk",
                FilterIndex = 1
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string name = Path.GetFileNameWithoutExtension(dialog.FileName);
                string path = dialog.FileName;

                using var nameForm = new Form
                {
                    Text = LocalizedStrings.GetString("Enter Shortcut Name"),
                    Size = new Size(300, 150),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent,
                    BackColor = Color.FromArgb(32, 32, 32),
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                var textBox = new TextBox
                {
                    Text = name,
                    Location = new Point(10, 20),
                    Size = new Size(260, 20),
                    BackColor = Color.FromArgb(45, 45, 48),
                    ForeColor = Color.White
                };

                var okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(100, 60),
                    Size = new Size(80, 30),
                    BackColor = Color.FromArgb(0, 122, 204),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };

                nameForm.Controls.AddRange(new Control[] { textBox, okButton });
                nameForm.AcceptButton = okButton;

                if (nameForm.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    name = textBox.Text;
                    shortcuts.Add((name, path));

                    if (shortcutListView != null)
                    {
                        var item = new ListViewItem(name);
                        item.SubItems.Add(path);
                        shortcutListView.Items.Add(item);
                    }
                }
            }
        }

        private void RemoveButton_Click(object? sender, EventArgs e)
        {
            if (shortcutListView?.SelectedItems.Count > 0)
            {
                int index = shortcutListView.SelectedIndices[0];
                shortcuts.RemoveAt(index);
                shortcutListView.Items.RemoveAt(index);
            }
        }

        private void ShortcutListView_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (shortcutListView?.SelectedItems.Count > 0)
            {
                int index = shortcutListView.SelectedIndices[0];
                var shortcut = shortcuts[index];

                using var nameForm = new Form
                {
                    Text = LocalizedStrings.GetString("Edit Shortcut"),
                    Size = new Size(600, 200),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent,
                    BackColor = Color.FromArgb(32, 32, 32),
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                var nameLabel = new Label
                {
                    Text = LocalizedStrings.GetString("Name:"),
                    Location = new Point(10, 20),
                    AutoSize = true,
                    ForeColor = Color.White
                };

                var nameBox = new TextBox
                {
                    Text = shortcut.Name,
                    Location = new Point(10, 40),
                    Size = new Size(460, 20),
                    BackColor = Color.FromArgb(45, 45, 48),
                    ForeColor = Color.White
                };

                var pathLabel = new Label
                {
                    Text = LocalizedStrings.GetString("Path:"),
                    Location = new Point(10, 70),
                    AutoSize = true,
                    ForeColor = Color.White
                };

                var pathBox = new TextBox
                {
                    Text = shortcut.Path,
                    Location = new Point(10, 90),
                    Size = new Size(460, 20),
                    BackColor = Color.FromArgb(45, 45, 48),
                    ForeColor = Color.White
                };

                var browseButton = new Button
                {
                    Text = "...",
                    Location = new Point(480, 89),
                    Size = new Size(30, 23),
                    BackColor = Color.FromArgb(60, 60, 60),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };

                browseButton.Click += (s, ev) =>
                {
                    using var dialog = new OpenFileDialog
                    {
                        Filter = "All Files (*.*)|*.*|Applications (*.exe)|*.exe|Shortcuts (*.lnk)|*.lnk",
                        FilterIndex = 1,
                        FileName = pathBox.Text
                    };

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        pathBox.Text = dialog.FileName;
                    }
                };

                var okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(400, 120),
                    Size = new Size(80, 30),
                    BackColor = Color.FromArgb(0, 122, 204),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };

                var cancelButton = new Button
                {
                    Text = LocalizedStrings.GetString("Cancel"),
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(490, 120),
                    Size = new Size(80, 30),
                    BackColor = Color.FromArgb(87, 87, 87),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };

                nameForm.Controls.AddRange(new Control[] { nameLabel, nameBox, pathLabel, pathBox, browseButton, okButton, cancelButton });
                nameForm.AcceptButton = okButton;
                nameForm.CancelButton = cancelButton;

                if (nameForm.ShowDialog() == DialogResult.OK)
                {
                    shortcuts[index] = (nameBox.Text, pathBox.Text);
                    shortcutListView.Items[index].Text = nameBox.Text;
                    shortcutListView.Items[index].SubItems[1].Text = pathBox.Text;
                }
            }
        }
    }
} 