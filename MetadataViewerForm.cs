using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BatRun
{
    public class MetadataViewerForm : Form
    {
        private readonly Dictionary<string, string> _metadata;
        private readonly string _romsBasePath;

        public MetadataViewerForm(Dictionary<string, string> metadata, string retrobatBasePath, string systemName)
        {
            _metadata = metadata;
            _romsBasePath = Path.Combine(Path.GetDirectoryName(retrobatBasePath) ?? "", "roms", systemName);

            InitializeComponent();
            PopulateControls();
        }

        private void InitializeComponent()
        {
            this.Text = "Game Metadata";
            this.ClientSize = new Size(800, 600);
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9F);
            this.StartPosition = FormStartPosition.CenterParent;
        }

        private void PopulateControls()
        {
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                ColumnCount = 2,
                RowCount = 1
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.Controls.Add(mainLayout);

            // Left Panel (Image)
            var pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };
            mainLayout.Controls.Add(pictureBox, 0, 0);

            // Right Panel (Details)
            var detailsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 0, 0, 0),
                RowCount = 3,
                ColumnCount = 1
            };
            detailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // Title
            detailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F)); // Description
            detailsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Other details
            mainLayout.Controls.Add(detailsLayout, 1, 0);

            // Title Label
            var titleLabel = new Label
            {
                Name = "titleLabel",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            detailsLayout.Controls.Add(titleLabel, 0, 0);

            // Description TextBox
            var descTextBox = new TextBox
            {
                Name = "descTextBox",
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            detailsLayout.Controls.Add(descTextBox, 0, 1);

            // Other Details Panel
            var otherDetailsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true,
                WrapContents = false,
                Padding = new Padding(0, 10, 0, 0)
            };
            detailsLayout.Controls.Add(otherDetailsPanel, 0, 2);

            // --- Populate with data ---

            // Image
            if (_metadata.TryGetValue("image", out string? imagePath))
            {
                // The path in gamelist.xml is relative, e.g., "./images/mygame.png"
                string fullImagePath = Path.Combine(_romsBasePath, imagePath.TrimStart('.', '/'));
                if (File.Exists(fullImagePath))
                {
                    try
                    {
                        pictureBox.Image = Image.FromFile(fullImagePath);
                    }
                    catch (Exception ex) { Console.WriteLine($"Error loading image: {ex.Message}"); }
                }
            }

            // Title
            if (_metadata.TryGetValue("name", out string? name))
            {
                titleLabel.Text = name;
                this.Text = $"Metadata: {name}";
            }

            // Description
            if (_metadata.TryGetValue("desc", out string? desc))
            {
                descTextBox.Text = desc;
            }

            // Other fields
            AddDetailRow(otherDetailsPanel, "Developer", _metadata.GetValueOrDefault("developer"));
            AddDetailRow(otherDetailsPanel, "Publisher", _metadata.GetValueOrDefault("publisher"));
            AddDetailRow(otherDetailsPanel, "Release Date", FormatDate(_metadata.GetValueOrDefault("releasedate")));
            AddDetailRow(otherDetailsPanel, "Genre", _metadata.GetValueOrDefault("genre"));
            AddDetailRow(otherDetailsPanel, "Players", _metadata.GetValueOrDefault("players"));
            AddDetailRow(otherDetailsPanel, "Rating", _metadata.GetValueOrDefault("rating"));
            AddDetailRow(otherDetailsPanel, "Play Count", _metadata.GetValueOrDefault("playcount"));
            AddDetailRow(otherDetailsPanel, "Last Played", FormatDate(_metadata.GetValueOrDefault("lastplayed")));
        }

        private void AddDetailRow(FlowLayoutPanel panel, string key, string? value)
        {
            if (string.IsNullOrEmpty(value)) return;

            var rowPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 5)
            };

            var keyLabel = new Label
            {
                Text = $"{key}:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.LightGray,
                AutoSize = true
            };

            var valueLabel = new Label
            {
                Text = value,
                AutoSize = true,
                Margin = new Padding(5, 0, 0, 0)
            };

            rowPanel.Controls.Add(keyLabel);
            rowPanel.Controls.Add(valueLabel);
            panel.Controls.Add(rowPanel);
        }

        private string FormatDate(string? esDate)
        {
            if (string.IsNullOrEmpty(esDate) || esDate.Length < 8) return esDate ?? "";

            // ES format is YYYYMMDD...
            if (DateTime.TryParseExact(esDate.AsSpan(0, 8), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
            {
                return parsedDate.ToLongDateString();
            }
            return esDate;
        }
    }
}
