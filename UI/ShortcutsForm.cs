using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.UI
{
    // EN: Form for managing application shortcuts.
    // FR: Formulaire pour gérer les raccourcis d'applications.
    public partial class ShortcutsForm : Form
    {
        private readonly IniFile config;
        private readonly Logger logger;
        private readonly List<(string Name, string Path)> shortcuts = [];

        public ShortcutsForm(IniFile config, Logger logger)
        {
            this.config = config;
            this.logger = logger;

            InitializeComponent();
            ApplyLocalization();
            LoadShortcuts();
        }

        // EN: Apply translated strings to the UI controls.
        // FR: Appliquer les chaînes traduites aux contrôles de l'interface.
        private void ApplyLocalization()
        {
            this.Text                  = LocalizedStrings.GetString("Shortcuts Interface");
            this.columnHeaderName.Text = LocalizedStrings.GetString("Name");
            this.columnHeaderPath.Text = LocalizedStrings.GetString("Path");
            this.saveButton.Text       = LocalizedStrings.GetString("Save");
            this.cancelButton.Text     = LocalizedStrings.GetString("Cancel");
        }

        private void LoadShortcuts()
        {
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
                // EN: Clear old shortcuts.
                // FR: Effacer les anciens raccourcis.
                int oldCount = config.ReadInt("Shortcuts", "Count", 0);
                for (int i = 0; i < oldCount; i++)
                {
                    config.WriteValue("Shortcuts", $"Name{i}", "");
                    config.WriteValue("Shortcuts", $"Path{i}", "");
                }

                // EN: Save new shortcuts.
                // FR: Sauvegarder les nouveaux raccourcis.
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
                    Text            = LocalizedStrings.GetString("Enter Shortcut Name"),
                    Size            = new Size(300, 150),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition   = FormStartPosition.CenterParent,
                    BackColor       = Color.FromArgb(32, 32, 32),
                    MaximizeBox     = false,
                    MinimizeBox     = false
                };

                var textBox = new TextBox
                {
                    Text      = name,
                    Location  = new Point(10, 20),
                    Size      = new Size(260, 20),
                    BackColor = Color.FromArgb(45, 45, 48),
                    ForeColor = Color.White
                };

                var okButton = new Button
                {
                    Text      = "OK",
                    DialogResult = DialogResult.OK,
                    Location  = new Point(100, 60),
                    Size      = new Size(80, 30),
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

                    var item = new ListViewItem(name);
                    item.SubItems.Add(path);
                    shortcutListView.Items.Add(item);
                }
            }
        }

        private void RemoveButton_Click(object? sender, EventArgs e)
        {
            if (shortcutListView.SelectedItems.Count > 0)
            {
                int index = shortcutListView.SelectedIndices[0];
                shortcuts.RemoveAt(index);
                shortcutListView.Items.RemoveAt(index);
            }
        }

        private void CancelButton_Click(object? sender, EventArgs e)
        {
            this.Close();
        }

        private void ShortcutListView_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (shortcutListView.SelectedItems.Count > 0)
            {
                int index = shortcutListView.SelectedIndices[0];
                var shortcut = shortcuts[index];

                using var nameForm = new Form
                {
                    Text            = LocalizedStrings.GetString("Edit Shortcut"),
                    Size            = new Size(600, 200),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition   = FormStartPosition.CenterParent,
                    BackColor       = Color.FromArgb(32, 32, 32),
                    MaximizeBox     = false,
                    MinimizeBox     = false
                };

                var nameLabel = new Label
                {
                    Text      = LocalizedStrings.GetString("Name:"),
                    Location  = new Point(10, 20),
                    AutoSize  = true,
                    ForeColor = Color.White
                };

                var nameBox = new TextBox
                {
                    Text      = shortcut.Name,
                    Location  = new Point(10, 40),
                    Size      = new Size(460, 20),
                    BackColor = Color.FromArgb(45, 45, 48),
                    ForeColor = Color.White
                };

                var pathLabel = new Label
                {
                    Text      = LocalizedStrings.GetString("Path:"),
                    Location  = new Point(10, 70),
                    AutoSize  = true,
                    ForeColor = Color.White
                };

                var pathBox = new TextBox
                {
                    Text      = shortcut.Path,
                    Location  = new Point(10, 90),
                    Size      = new Size(460, 20),
                    BackColor = Color.FromArgb(45, 45, 48),
                    ForeColor = Color.White
                };

                var browseButton = new Button
                {
                    Text      = "...",
                    Location  = new Point(480, 89),
                    Size      = new Size(30, 23),
                    BackColor = Color.FromArgb(60, 60, 60),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };

                browseButton.Click += (s, ev) =>
                {
                    using var dialog = new OpenFileDialog
                    {
                        Filter      = "All Files (*.*)|*.*|Applications (*.exe)|*.exe|Shortcuts (*.lnk)|*.lnk",
                        FilterIndex = 1,
                        FileName    = pathBox.Text
                    };

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        pathBox.Text = dialog.FileName;
                    }
                };

                var okButton = new Button
                {
                    Text      = "OK",
                    DialogResult = DialogResult.OK,
                    Location  = new Point(400, 120),
                    Size      = new Size(80, 30),
                    BackColor = Color.FromArgb(0, 122, 204),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };

                var cancelButton = new Button
                {
                    Text      = LocalizedStrings.GetString("Cancel"),
                    DialogResult = DialogResult.Cancel,
                    Location  = new Point(490, 120),
                    Size      = new Size(80, 30),
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

