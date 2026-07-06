using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.UI
{
    partial class GameMetadataForm
    {
        /// <summary>
        /// EN: Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// EN: Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _scrollTimer?.Stop();
                _scrollTimer?.Dispose();
                _mediaPlayer?.Stop();
                _mediaPlayer?.Dispose();
                _libVLC?.Dispose();
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// EN: Required method for Designer support - do not modify the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._infoPanel = new System.Windows.Forms.TableLayoutPanel();
            this._mediaTabControl = new System.Windows.Forms.TabControl();
            this._imagePictureBox = new System.Windows.Forms.PictureBox();
            this._marqueePictureBox = new System.Windows.Forms.PictureBox();
            this._thumbnailPictureBox = new System.Windows.Forms.PictureBox();
            this._fanartPictureBox = new System.Windows.Forms.PictureBox();
            this._bezelPictureBox = new System.Windows.Forms.PictureBox();
            this._loadingPanel = new System.Windows.Forms.Panel();
            this.SuspendLayout();

            // --- Main Form Properties ---
            this.Text = "Game Metadata";
            this.ClientSize = new System.Drawing.Size(960, 720);
            this.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            this.ForeColor = System.Drawing.Color.White;
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;

            // --- Layout ---
            var mainLayout = new System.Windows.Forms.TableLayoutPanel();
            mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            mainLayout.ColumnCount = 1;
            mainLayout.RowCount = 2;
            mainLayout.Padding = new System.Windows.Forms.Padding(10);
            mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 380F));
            mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.Controls.Add(mainLayout);

            // --- Info Panel ---
            this._infoPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._infoPanel.ColumnCount = 2;
            this._infoPanel.AutoScroll = true;
            this._infoPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this._infoPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            mainLayout.Controls.Add(this._infoPanel, 0, 0);

            // --- Media Tabs ---
            this._mediaTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this._mediaTabControl.Alignment = System.Windows.Forms.TabAlignment.Top;
            this._mediaTabControl.SelectedIndexChanged += new System.EventHandler(this.MediaTabControl_SelectedIndexChanged);
            mainLayout.Controls.Add(this._mediaTabControl, 0, 1);

            // Tab Pages
            var imageTab = new System.Windows.Forms.TabPage("Image") { BackColor = System.Drawing.Color.Black };
            var videoTab = new System.Windows.Forms.TabPage("Video") { BackColor = System.Drawing.Color.Black };
            var marqueeTab = new System.Windows.Forms.TabPage("Marquee") { BackColor = System.Drawing.Color.Black };
            var thumbnailTab = new System.Windows.Forms.TabPage("Thumbnail") { BackColor = System.Drawing.Color.Black };
            var fanartTab = new System.Windows.Forms.TabPage("Fanart") { BackColor = System.Drawing.Color.Black };
            this._mediaTabControl.TabPages.AddRange(new System.Windows.Forms.TabPage[] { imageTab, videoTab, marqueeTab, thumbnailTab, fanartTab });

            // Media Controls setup
            this._imagePictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this._imagePictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            imageTab.Controls.Add(this._imagePictureBox);

            this._marqueePictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this._marqueePictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            marqueeTab.Controls.Add(this._marqueePictureBox);

            this._thumbnailPictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this._thumbnailPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            thumbnailTab.Controls.Add(this._thumbnailPictureBox);

            this._fanartPictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this._fanartPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            fanartTab.Controls.Add(this._fanartPictureBox);

            // Video Page
            var videoPanel = new System.Windows.Forms.Panel { Dock = System.Windows.Forms.DockStyle.Fill, BackColor = System.Drawing.Color.Black };
            videoPanel.Resize += new System.EventHandler(this.VideoPanel_Resize);
            
            this._bezelPictureBox.Dock = System.Windows.Forms.DockStyle.None;
            this._bezelPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            
            // VideoView and Player are handled at runtime
            videoPanel.Controls.Add(this._bezelPictureBox);
            videoTab.Controls.Add(videoPanel);

            // Loading Panel
            this._loadingPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._loadingPanel.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            this._loadingPanel.Visible = true;
            var loadingLabel = new System.Windows.Forms.Label
            {
                Text = "Loading...",
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 16F),
                Dock = System.Windows.Forms.DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            this._loadingPanel.Controls.Add(loadingLabel);
            this.Controls.Add(this._loadingPanel);
            this._loadingPanel.BringToFront();

            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel _infoPanel;
        private System.Windows.Forms.TabControl _mediaTabControl;
        private System.Windows.Forms.PictureBox _imagePictureBox;
        private System.Windows.Forms.PictureBox _marqueePictureBox;
        private System.Windows.Forms.PictureBox _thumbnailPictureBox;
        private System.Windows.Forms.PictureBox _fanartPictureBox;
        private System.Windows.Forms.PictureBox _bezelPictureBox;
        private System.Windows.Forms.Panel _loadingPanel;
    }
}


