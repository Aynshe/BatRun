using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.UI
{
    partial class ArcadeOverlayForm
    {
        /// <summary>
        /// EN: Required designer variable.
        /// FR: Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// EN: Clean up any resources being used.
        /// FR: Libérer les ressources utilisées.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// EN: Required method for Designer support - do not modify the contents of this method with the code editor.
        /// FR: Méthode requise pour le concepteur - ne pas modifier le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.messageLabel = new System.Windows.Forms.Label();
            this.countdownLabel = new System.Windows.Forms.Label();
            this.operatorButton = new System.Windows.Forms.Button();
            this.freePlayButton = new System.Windows.Forms.Button();
            this.addCreditsButton = new System.Windows.Forms.Button();
            this.lockMiniButton = new System.Windows.Forms.Button();
            this.interfaceMiniButton = new System.Windows.Forms.Button();
            this.taskSwitcherPanel = new System.Windows.Forms.Panel();
            this.taskSwitcherTitleLabel = new System.Windows.Forms.Label();
            this.taskSwitcherPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // messageLabel
            // 
            this.messageLabel.BackColor = System.Drawing.Color.Transparent;
            this.messageLabel.Font = new System.Drawing.Font("Arial", 48F, System.Drawing.FontStyle.Bold);
            this.messageLabel.ForeColor = System.Drawing.Color.White;
            this.messageLabel.Location = new System.Drawing.Point(0, 0);
            this.messageLabel.Name = "messageLabel";
            this.messageLabel.Size = new System.Drawing.Size(800, 400);
            this.messageLabel.TabIndex = 3;
            this.messageLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // countdownLabel
            // 
            this.countdownLabel.BackColor = System.Drawing.Color.Transparent;
            this.countdownLabel.Font = new System.Drawing.Font("Arial", 72F, System.Drawing.FontStyle.Bold);
            this.countdownLabel.ForeColor = System.Drawing.Color.Red;
            this.countdownLabel.Location = new System.Drawing.Point(0, 0);
            this.countdownLabel.Name = "countdownLabel";
            this.countdownLabel.Size = new System.Drawing.Size(800, 120);
            this.countdownLabel.TabIndex = 4;
            this.countdownLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.countdownLabel.Visible = false;
            // 
            // operatorButton
            // 
            this.operatorButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.operatorButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.operatorButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.operatorButton.FlatAppearance.BorderSize = 0;
            this.operatorButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.operatorButton.ForeColor = System.Drawing.Color.White;
            this.operatorButton.Location = new System.Drawing.Point(635, 710);
            this.operatorButton.Name = "operatorButton";
            this.operatorButton.Size = new System.Drawing.Size(40, 30);
            this.operatorButton.TabIndex = 0;
            this.operatorButton.TabStop = false;
            this.operatorButton.Text = "OP";
            this.operatorButton.UseVisualStyleBackColor = false;
            this.operatorButton.Click += new System.EventHandler(this.OperatorButton_Click);
            this.operatorButton.GotFocus += new System.EventHandler(this.OperatorButton_GotFocus);
            // 
            // freePlayButton
            // 
            this.freePlayButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.freePlayButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.freePlayButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.freePlayButton.FlatAppearance.BorderSize = 0;
            this.freePlayButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.freePlayButton.ForeColor = System.Drawing.Color.White;
            this.freePlayButton.Location = new System.Drawing.Point(680, 710);
            this.freePlayButton.Name = "freePlayButton";
            this.freePlayButton.Size = new System.Drawing.Size(40, 30);
            this.freePlayButton.TabIndex = 1;
            this.freePlayButton.TabStop = false;
            this.freePlayButton.Text = "FP";
            this.freePlayButton.UseVisualStyleBackColor = false;
            this.freePlayButton.Click += new System.EventHandler(this.FreePlayButton_Click);
            this.freePlayButton.GotFocus += new System.EventHandler(this.FreePlayButton_GotFocus);
            // 
            // addCreditsButton
            // 
            this.addCreditsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.addCreditsButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.addCreditsButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.addCreditsButton.FlatAppearance.BorderSize = 0;
            this.addCreditsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.addCreditsButton.ForeColor = System.Drawing.Color.White;
            this.addCreditsButton.Location = new System.Drawing.Point(725, 710);
            this.addCreditsButton.Name = "addCreditsButton";
            this.addCreditsButton.Size = new System.Drawing.Size(40, 30);
            this.addCreditsButton.TabIndex = 2;
            this.addCreditsButton.TabStop = false;
            this.addCreditsButton.Text = "+$";
            this.addCreditsButton.UseVisualStyleBackColor = false;
            this.addCreditsButton.Click += new System.EventHandler(this.AddCreditsButton_Click);
            this.addCreditsButton.GotFocus += new System.EventHandler(this.AddCreditsButton_GotFocus);
            // 
            // lockMiniButton
            // 
            this.lockMiniButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(204)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.lockMiniButton.FlatAppearance.BorderSize = 0;
            this.lockMiniButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.lockMiniButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lockMiniButton.ForeColor = System.Drawing.Color.White;
            this.lockMiniButton.Location = new System.Drawing.Point(4, 4);
            this.lockMiniButton.Name = "lockMiniButton";
            this.lockMiniButton.Size = new System.Drawing.Size(110, 32);
            this.lockMiniButton.TabIndex = 0;
            this.lockMiniButton.Text = "🔒 LOCK";
            this.lockMiniButton.UseVisualStyleBackColor = false;
            this.lockMiniButton.Visible = false;
            this.lockMiniButton.Click += new System.EventHandler(this.LockRequested_Click);
            // 
            // interfaceMiniButton
            // 
            this.interfaceMiniButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.interfaceMiniButton.FlatAppearance.BorderSize = 0;
            this.interfaceMiniButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.interfaceMiniButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.interfaceMiniButton.ForeColor = System.Drawing.Color.White;
            this.interfaceMiniButton.Location = new System.Drawing.Point(115, 4);
            this.interfaceMiniButton.Name = "interfaceMiniButton";
            this.interfaceMiniButton.Size = new System.Drawing.Size(60, 32);
            this.interfaceMiniButton.TabIndex = 1;
            this.interfaceMiniButton.Text = "BR";
            this.interfaceMiniButton.UseVisualStyleBackColor = false;
            this.interfaceMiniButton.Visible = false;
            this.interfaceMiniButton.Click += new System.EventHandler(this.InterfaceRequested_Click);
            // 
            // taskSwitcherPanel
            // 
            this.taskSwitcherPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(24)))));
            this.taskSwitcherPanel.Controls.Add(this.taskSwitcherTitleLabel);
            this.taskSwitcherPanel.Location = new System.Drawing.Point(0, 0);
            this.taskSwitcherPanel.Name = "taskSwitcherPanel";
            this.taskSwitcherPanel.Padding = new System.Windows.Forms.Padding(20);
            this.taskSwitcherPanel.Size = new System.Drawing.Size(200, 100);
            this.taskSwitcherPanel.TabIndex = 5;
            this.taskSwitcherPanel.Visible = false;
            // 
            // taskSwitcherTitleLabel
            // 
            this.taskSwitcherTitleLabel.AutoSize = true;
            this.taskSwitcherTitleLabel.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Bold);
            this.taskSwitcherTitleLabel.ForeColor = System.Drawing.Color.Orange;
            this.taskSwitcherTitleLabel.Location = new System.Drawing.Point(20, 20);
            this.taskSwitcherTitleLabel.Name = "taskSwitcherTitleLabel";
            this.taskSwitcherTitleLabel.Size = new System.Drawing.Size(248, 45);
            this.taskSwitcherTitleLabel.TabIndex = 0;
            this.taskSwitcherTitleLabel.Text = "ARCADE TASKS";
            // 
            // ArcadeOverlayForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(800, 750);
            this.Controls.Add(this.interfaceMiniButton);
            this.Controls.Add(this.lockMiniButton);
            this.Controls.Add(this.addCreditsButton);
            this.Controls.Add(this.freePlayButton);
            this.Controls.Add(this.operatorButton);
            this.Controls.Add(this.countdownLabel);
            this.Controls.Add(this.messageLabel);
            this.Controls.Add(this.taskSwitcherPanel);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "ArcadeOverlayForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.taskSwitcherPanel.ResumeLayout(false);
            this.taskSwitcherPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label messageLabel;
        private System.Windows.Forms.Label countdownLabel;
        private System.Windows.Forms.Button operatorButton;
        private System.Windows.Forms.Button freePlayButton;
        private System.Windows.Forms.Button addCreditsButton;
        private System.Windows.Forms.Button lockMiniButton;
        private System.Windows.Forms.Button interfaceMiniButton;
        private System.Windows.Forms.Panel taskSwitcherPanel;
        private System.Windows.Forms.Label taskSwitcherTitleLabel;
    }
}


