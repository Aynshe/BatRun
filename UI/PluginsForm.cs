using System;
using System.Diagnostics;
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
    // EN: Form displaying various RetroBat tools and plugins from GitHub.
    // FR: Formulaire affichant divers outils et plugins RetroBat depuis GitHub.
    public partial class PluginsForm : Form
    {
        public PluginsForm()
        {
            InitializeComponent();
            LoadIcon();
            PopulateProjects();
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

        // EN: Populates the TableLayoutPanel with project rows.
        // FR: Remplit le TableLayoutPanel avec les lignes de projet.
        private void PopulateProjects()
        {
            mainLayout.Controls.Add(CreateHeaderLabel("MAIN PROJECTS"));
            
            AddProjectRow(mainLayout, "WiimoteGun (Fork for rawinput lightgun)", 
                "https://github.com/Aynshe/WiimoteGun/tree/3-removal-driver-interception-dependency",
                "https://github.com/Aynshe/WiimoteGun/releases/latest");

            AddProjectRow(mainLayout, "RetroBat Marquee Manager", 
                "https://github.com/Aynshe/RetroBatMarqueeManager",
                "https://github.com/Aynshe/RetroBatMarqueeManager/releases/latest");

            AddProjectRow(mainLayout, "XOrderHook (swap index xinput)", 
                "https://github.com/Aynshe/XOrderHook",
                "https://github.com/Aynshe/XOrderHook/releases/latest");

            AddProjectRow(mainLayout, "GSLM (Game Store Library Manager)", 
                "https://github.com/Aynshe/GSLM",
                "https://github.com/Aynshe/GSLM/releases/latest");

            // Spacer
            mainLayout.Controls.Add(new Label { Height = 20, Dock = DockStyle.Top });

            // Experimental Section
            mainLayout.Controls.Add(CreateHeaderLabel("EXPERIMENTAL (QUICK-RESUME DEMOS)"));

            AddProjectRow(mainLayout, "EmulatorLauncher (Demo QR)", 
                "https://github.com/Aynshe/emulatorlauncher/tree/emulatorlauncher_quickresume_demo",
                "https://github.com/Aynshe/emulatorlauncher/releases");

            AddProjectRow(mainLayout, "SuspendedNTime (Demo QR)", 
                "https://github.com/Aynshe/SuspendedNTime/tree/SuspendedNTime-quickresume_RetroBat-Demo",
                "https://github.com/Aynshe/SuspendedNTime/releases");
        }

        private Label CreateHeaderLabel(string text)
        {
            return new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 204),
                AutoSize  = true,
                Margin    = new Padding(0, 0, 0, 8)
            };
        }

        private void AddProjectRow(TableLayoutPanel panel, string name, string githubUrl, string releaseUrl)
        {
            var rowPanel = new Panel 
            { 
                Dock      = DockStyle.Top, 
                Height    = 65, 
                Margin    = new Padding(0, 0, 0, 8), 
                BackColor = Color.FromArgb(45, 45, 48) 
            };
            
            var lblName = new Label 
            { 
                Text      = name, 
                Location  = new Point(12, 8), 
                AutoSize  = true, 
                Font      = new Font("Segoe UI", 9.5F, FontStyle.Bold) 
            };
            
            var btnGithub = new Button 
            { 
                Text      = "🌐 GitHub PROJECT", 
                Location  = new Point(12, 32), 
                Size      = new Size(160, 26), 
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.FromArgb(70, 70, 70),
                Cursor    = Cursors.Hand,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 8.5F)
            };
            btnGithub.FlatAppearance.BorderSize = 0;
            btnGithub.Click += (s, e) => OpenUrl(githubUrl);

            var btnRelease = new Button 
            { 
                Text      = "📦 LATEST RELEASES", 
                Location  = new Point(185, 32), 
                Size      = new Size(160, 26), 
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.FromArgb(0, 122, 204),
                Cursor    = Cursors.Hand,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 8.5F)
            };
            btnRelease.FlatAppearance.BorderSize = 0;
            btnRelease.Click += (s, e) => OpenUrl(releaseUrl);

            rowPanel.Controls.Add(lblName);
            rowPanel.Controls.Add(btnGithub);
            rowPanel.Controls.Add(btnRelease);
            
            panel.Controls.Add(rowPanel);
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open link: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClose_Click(object? sender, EventArgs e)
        {
            this.Close();
        }
    }
}


