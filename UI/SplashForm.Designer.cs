using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.UI
{
    partial class SplashForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.logoBox     = new System.Windows.Forms.PictureBox();
            this.titleLabel  = new System.Windows.Forms.Label();
            this.versionLabel = new System.Windows.Forms.Label();
            this.statusLabel = new System.Windows.Forms.Label();

            ((System.ComponentModel.ISupportInitialize)(this.logoBox)).BeginInit();
            this.SuspendLayout();

            // ── logoBox ──────────────────────────────────────────────────
            this.logoBox.Location  = new System.Drawing.Point(168, 40);
            this.logoBox.Size      = new System.Drawing.Size(64, 64);
            this.logoBox.SizeMode  = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.logoBox.BackColor = System.Drawing.Color.FromArgb(32, 32, 32);
            this.logoBox.Name      = "logoBox";
            this.logoBox.TabIndex  = 0;

            // ── titleLabel ───────────────────────────────────────────────
            this.titleLabel.Location  = new System.Drawing.Point(0, 110);
            this.titleLabel.Size      = new System.Drawing.Size(400, 50);
            this.titleLabel.Text      = "BatRun";
            this.titleLabel.Font      = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Bold);
            this.titleLabel.ForeColor = System.Drawing.Color.White;
            this.titleLabel.BackColor = System.Drawing.Color.FromArgb(32, 32, 32);
            this.titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.titleLabel.AutoSize  = false;
            this.titleLabel.Name      = "titleLabel";
            this.titleLabel.TabIndex  = 1;

            // ── versionLabel ─────────────────────────────────────────────
            this.versionLabel.Location  = new System.Drawing.Point(0, 160);
            this.versionLabel.Size      = new System.Drawing.Size(400, 30);
            this.versionLabel.Text      = "v0.0.0";
            this.versionLabel.Font      = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular);
            this.versionLabel.ForeColor = System.Drawing.Color.LightGray;
            this.versionLabel.BackColor = System.Drawing.Color.FromArgb(32, 32, 32);
            this.versionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.versionLabel.AutoSize  = false;
            this.versionLabel.Name      = "versionLabel";
            this.versionLabel.TabIndex  = 2;

            // ── statusLabel ──────────────────────────────────────────────
            this.statusLabel.Location  = new System.Drawing.Point(0, 190);
            this.statusLabel.Size      = new System.Drawing.Size(400, 30);
            this.statusLabel.Text      = "Initializing...";
            this.statusLabel.Font      = new System.Drawing.Font("Segoe UI", 10F);
            this.statusLabel.ForeColor = System.Drawing.Color.White;
            this.statusLabel.BackColor = System.Drawing.Color.FromArgb(32, 32, 32);
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.statusLabel.AutoSize  = false;
            this.statusLabel.Name      = "statusLabel";
            this.statusLabel.TabIndex  = 3;

            // ── Form ─────────────────────────────────────────────────────
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor           = System.Drawing.Color.FromArgb(32, 32, 32);
            this.ClientSize          = new System.Drawing.Size(400, 250);
            this.FormBorderStyle     = System.Windows.Forms.FormBorderStyle.None;
            this.Name                = "SplashForm";
            this.ShowInTaskbar       = false;
            this.StartPosition       = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text                = "BatRun";
            this.TopMost             = true;

            this.Controls.Add(this.logoBox);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.versionLabel);
            this.Controls.Add(this.statusLabel);

            ((System.ComponentModel.ISupportInitialize)(this.logoBox)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.PictureBox logoBox;
        private System.Windows.Forms.Label      titleLabel;
        private System.Windows.Forms.Label      versionLabel;
        private System.Windows.Forms.Label      statusLabel;
    }
}


