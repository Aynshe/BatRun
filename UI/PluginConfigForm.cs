using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using BatRun.Utils;

namespace BatRun.UI
{
    // EN: Dynamically parses and displays fields for plugin .ini configuration files.
    // FR: Analyse et affiche dynamiquement les champs pour les fichiers de configuration .ini des plugins.
    public class PluginConfigForm : Form
    {
        private readonly string _pluginName;
        private readonly List<string> _iniFiles;
        private string _currentFilePath = "";
        private List<IniLine> _iniLines = new List<IniLine>();

        private ComboBox? _fileSelector;
        private Panel? _configPanel;
        private Button? _btnSave;
        private Button? _btnCancel;

        public PluginConfigForm(string pluginName, List<string> iniFiles)
        {
            _pluginName = pluginName;
            _iniFiles = iniFiles;

            InitializeForm();
            LoadIcon();

            if (_iniFiles.Count > 0)
            {
                _currentFilePath = _iniFiles[0];
                if (_fileSelector != null)
                {
                    _fileSelector.SelectedItem = Path.GetFileName(_currentFilePath);
                }
                LoadAndBuildUi(_currentFilePath);
            }
        }

        private void InitializeForm()
        {
            this.Text = $"Configure {_pluginName}";
            this.Size = new Size(550, 600);
            this.MinimumSize = new Size(400, 400);
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9F);
            this.StartPosition = FormStartPosition.CenterParent;

            // Main layout panel
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F)); // File selector / Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Scrollable fields
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F)); // Buttons

            // Top Panel (File Selection)
            var topPanel = new Panel { Dock = DockStyle.Fill };
            if (_iniFiles.Count > 1)
            {
                var lblSelect = new Label
                {
                    Text = "Config File:",
                    Location = new Point(5, 12),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
                };
                _fileSelector = new ComboBox
                {
                    Location = new Point(90, 8),
                    Width = 350,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = Color.FromArgb(45, 45, 48),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                foreach (var file in _iniFiles)
                {
                    _fileSelector.Items.Add(Path.GetFileName(file));
                }
                _fileSelector.SelectedIndexChanged += (s, e) =>
                {
                    string selectedName = _fileSelector.SelectedItem?.ToString() ?? "";
                    string? matchedPath = _iniFiles.FirstOrDefault(f => Path.GetFileName(f) == selectedName);
                    if (!string.IsNullOrEmpty(matchedPath))
                    {
                        _currentFilePath = matchedPath;
                        LoadAndBuildUi(_currentFilePath);
                    }
                };
                topPanel.Controls.Add(lblSelect);
                topPanel.Controls.Add(_fileSelector);
            }
            else if (_iniFiles.Count == 1)
            {
                var lblHeader = new Label
                {
                    Text = $"Editing: {Path.GetFileName(_iniFiles[0])}",
                    Location = new Point(5, 12),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 122, 204)
                };
                topPanel.Controls.Add(lblHeader);
            }
            mainLayout.Controls.Add(topPanel, 0, 0);

            // Middle Panel (Scrollable Config Fields)
            _configPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(10)
            };
            mainLayout.Controls.Add(_configPanel, 0, 1);

            // Bottom Panel (Buttons)
            var bottomPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 5, 0, 0)
            };

            _btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.Click += (s, e) => this.Close();

            _btnSave = new Button
            {
                Text = "Save",
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += (s, e) => SaveConfig();

            bottomPanel.Controls.Add(_btnCancel);
            bottomPanel.Controls.Add(_btnSave);
            mainLayout.Controls.Add(bottomPanel, 0, 2);

            this.Controls.Add(mainLayout);
        }

        private void LoadIcon()
        {
            try
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
            }
            catch { }
        }

        private void LoadAndBuildUi(string filePath)
        {
            if (_configPanel == null) return;
            _configPanel.Controls.Clear();
            _iniLines.Clear();

            if (!File.Exists(filePath)) return;

            // EN: Parse INI file line by line to build UI and model
            // FR: Analyse le fichier INI ligne par ligne pour construire l'UI et le modèle
            var lines = File.ReadAllLines(filePath);
            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();

                if (string.IsNullOrEmpty(line))
                {
                    _iniLines.Add(new EmptyLine());
                }
                else if (line.StartsWith(";") || line.StartsWith("#"))
                {
                    _iniLines.Add(new CommentLine { Content = rawLine });
                }
                else if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    string secName = line.Substring(1, line.Length - 2).Trim();
                    _iniLines.Add(new SectionLine { Name = secName });
                }
                else if (line.Contains("="))
                {
                    int eqIndex = rawLine.IndexOf('=');
                    string rawKey = rawLine.Substring(0, eqIndex);
                    string rawValAndComment = rawLine.Substring(eqIndex + 1);

                    string key = rawKey.Trim();
                    string value = rawValAndComment;
                    string comment = "";

                    // EN: Check for inline comment in value
                    // FR: Vérifie s'il y a un commentaire de fin de ligne dans la valeur
                    int semiIndex = rawValAndComment.IndexOf(';');
                    int hashIndex = rawValAndComment.IndexOf('#');
                    int commentStart = -1;

                    if (semiIndex >= 0 && hashIndex >= 0) commentStart = Math.Min(semiIndex, hashIndex);
                    else if (semiIndex >= 0) commentStart = semiIndex;
                    else if (hashIndex >= 0) commentStart = hashIndex;

                    if (commentStart >= 0)
                    {
                        value = rawValAndComment.Substring(0, commentStart);
                        comment = rawValAndComment.Substring(commentStart);
                    }

                    _iniLines.Add(new KeyValueLine
                    {
                        Key = key,
                        Value = value.Trim(),
                        Comment = comment
                    });
                }
                else
                {
                    // Treat unrecognized lines as comments to avoid loss of data
                    _iniLines.Add(new CommentLine { Content = rawLine });
                }
            }

            // Build controls dynamically
            int currentY = 10;
            foreach (var iniLine in _iniLines)
            {
                if (iniLine is SectionLine sec)
                {
                    var lblSection = new Label
                    {
                        Text = $"[{sec.Name}]",
                        Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(0, 122, 204),
                        Location = new Point(10, currentY),
                        AutoSize = true
                    };
                    _configPanel.Controls.Add(lblSection);
                    currentY += 28;
                }
                else if (iniLine is CommentLine comm)
                {
                    var lblComment = new Label
                    {
                        Text = comm.Content,
                        Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                        ForeColor = Color.Gray,
                        Location = new Point(15, currentY),
                        Size = new Size(500, 18),
                        AutoEllipsis = true
                    };
                    _configPanel.Controls.Add(lblComment);
                    currentY += 20;
                }
                else if (iniLine is KeyValueLine kv)
                {
                    var lblKey = new Label
                    {
                        Text = kv.Key,
                        Location = new Point(20, currentY + 3),
                        Size = new Size(180, 20),
                        ForeColor = Color.White,
                        TextAlign = ContentAlignment.MiddleLeft
                    };
                    _configPanel.Controls.Add(lblKey);

                    string lowerVal = kv.Value.ToLower();
                    bool isBool = lowerVal == "true" || lowerVal == "false" || lowerVal == "1" || lowerVal == "0";

                    if (isBool)
                    {
                        var combo = new ComboBox
                        {
                            Location = new Point(210, currentY),
                            Width = 100,
                            DropDownStyle = ComboBoxStyle.DropDownList,
                            BackColor = Color.FromArgb(45, 45, 48),
                            ForeColor = Color.White,
                            FlatStyle = FlatStyle.Flat
                        };
                        combo.Items.Add("true");
                        combo.Items.Add("false");

                        if (lowerVal == "true" || lowerVal == "1") combo.SelectedItem = "true";
                        else combo.SelectedItem = "false";

                        kv.EditorControl = combo;
                        _configPanel.Controls.Add(combo);
                    }
                    else
                    {
                        var txt = new TextBox
                        {
                            Text = kv.Value,
                            Location = new Point(210, currentY),
                            Width = 200,
                            BackColor = Color.FromArgb(45, 45, 48),
                            ForeColor = Color.White,
                            BorderStyle = BorderStyle.FixedSingle
                        };
                        kv.EditorControl = txt;
                        _configPanel.Controls.Add(txt);
                    }

                    if (!string.IsNullOrEmpty(kv.Comment))
                    {
                        var lblInlineComment = new Label
                        {
                            Text = kv.Comment,
                            Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                            ForeColor = Color.Gray,
                            Location = new Point(420, currentY + 3),
                            Size = new Size(100, 20),
                            TextAlign = ContentAlignment.MiddleLeft,
                            AutoEllipsis = true
                        };
                        _configPanel.Controls.Add(lblInlineComment);
                    }

                    currentY += 30;
                }
                else if (iniLine is EmptyLine)
                {
                    currentY += 10;
                }
            }

            // Adjust panel inner layout height
            var dummy = new Label { Location = new Point(0, currentY + 10), Size = new Size(1, 1) };
            _configPanel.Controls.Add(dummy);
        }

        private void SaveConfig()
        {
            if (string.IsNullOrEmpty(_currentFilePath)) return;

            try
            {
                var outputLines = new List<string>();
                foreach (var line in _iniLines)
                {
                    if (line is SectionLine sec)
                    {
                        outputLines.Add($"[{sec.Name}]");
                    }
                    else if (line is CommentLine comm)
                    {
                        outputLines.Add(comm.Content);
                    }
                    else if (line is KeyValueLine kv)
                    {
                        string newValue = kv.Value;
                        if (kv.EditorControl is ComboBox cb)
                        {
                            newValue = cb.SelectedItem?.ToString() ?? kv.Value;
                        }
                        else if (kv.EditorControl is TextBox tb)
                        {
                            newValue = tb.Text;
                        }

                        // Preserves exact formatting (key = value [comment])
                        string formattedLine = $"{kv.Key}={newValue}";
                        if (!string.IsNullOrEmpty(kv.Comment))
                        {
                            formattedLine += $" {kv.Comment}";
                        }
                        outputLines.Add(formattedLine);
                    }
                    else if (line is EmptyLine)
                    {
                        outputLines.Add("");
                    }
                }

                File.WriteAllLines(_currentFilePath, outputLines);
                MessageBox.Show("Configuration saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // INI Parsing Line models
        private abstract class IniLine { }

        private class CommentLine : IniLine
        {
            public string Content { get; set; } = "";
        }

        private class SectionLine : IniLine
        {
            public string Name { get; set; } = "";
        }

        private class KeyValueLine : IniLine
        {
            public string Key { get; set; } = "";
            public string Value { get; set; } = "";
            public string Comment { get; set; } = "";
            public Control? EditorControl { get; set; }
        }

        private class EmptyLine : IniLine { }
    }
}
