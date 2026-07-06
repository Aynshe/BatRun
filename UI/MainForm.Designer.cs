using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.UI
{
    partial class MainForm
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
            pnlAction = new Panel();
            lblLauncherTitle = new Label();
            btnLaunchRetroBat = new Button();
            btnLaunchBatGui = new Button();
            pnlConfig = new Panel();
            lblConfigTitle = new Label();
            btnGeneralSettings = new Button();
            btnControllerMappings = new Button();
            pnlSupport = new Panel();
            lblHelpTitle = new Label();
            btnViewLogs = new Button();
            btnViewErrorLogs = new Button();
            btnAbout = new Button();
            pnlTools = new Panel();
            btnToolsPlugins = new Button();
            pnlShell = new Panel();
            btnShellLauncher = new Button();
            pnlVersion = new Panel();
            lblVersion = new Label();
            pnlUpdateIndicator = new Panel();
            lblUpdateStatus = new Label();
            pnlAction.SuspendLayout();
            pnlConfig.SuspendLayout();
            pnlSupport.SuspendLayout();
            pnlTools.SuspendLayout();
            pnlShell.SuspendLayout();
            pnlVersion.SuspendLayout();
            SuspendLayout();
            // 
            // pnlAction
            // 
            pnlAction.BackColor = Color.FromArgb(45, 45, 48);
            pnlAction.Controls.Add(lblLauncherTitle);
            pnlAction.Controls.Add(btnLaunchRetroBat);
            pnlAction.Controls.Add(btnLaunchBatGui);
            pnlAction.Location = new Point(5, 9);
            pnlAction.Name = "pnlAction";
            pnlAction.Size = new Size(385, 215);
            pnlAction.TabIndex = 0;
            // 
            // lblLauncherTitle
            // 
            lblLauncherTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblLauncherTitle.BackColor = Color.Transparent;
            lblLauncherTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblLauncherTitle.ForeColor = Color.White;
            lblLauncherTitle.Location = new Point(5, 0);
            lblLauncherTitle.Name = "lblLauncherTitle";
            lblLauncherTitle.Size = new Size(375, 42);
            lblLauncherTitle.TabIndex = 0;
            lblLauncherTitle.Text = "RetroBat Launcher";
            lblLauncherTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnLaunchRetroBat
            // 
            btnLaunchRetroBat.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnLaunchRetroBat.AutoEllipsis = true;
            btnLaunchRetroBat.BackColor = Color.FromArgb(0, 122, 204);
            btnLaunchRetroBat.FlatAppearance.BorderSize = 0;
            btnLaunchRetroBat.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 84, 153);
            btnLaunchRetroBat.FlatAppearance.MouseOverBackColor = Color.FromArgb(51, 153, 255);
            btnLaunchRetroBat.FlatStyle = FlatStyle.Flat;
            btnLaunchRetroBat.Font = new Font("Segoe UI", 11F);
            btnLaunchRetroBat.ForeColor = Color.White;
            btnLaunchRetroBat.Location = new Point(5, 54);
            btnLaunchRetroBat.Name = "btnLaunchRetroBat";
            btnLaunchRetroBat.Size = new Size(375, 60);
            btnLaunchRetroBat.TabIndex = 1;
            btnLaunchRetroBat.Text = "Launch RetroBat";
            btnLaunchRetroBat.UseVisualStyleBackColor = false;
            btnLaunchRetroBat.Click += BtnLaunchRetroBat_Click;
            // 
            // btnLaunchBatGui
            // 
            btnLaunchBatGui.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnLaunchBatGui.AutoEllipsis = true;
            btnLaunchBatGui.BackColor = Color.FromArgb(0, 122, 204);
            btnLaunchBatGui.FlatAppearance.BorderSize = 0;
            btnLaunchBatGui.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 84, 153);
            btnLaunchBatGui.FlatAppearance.MouseOverBackColor = Color.FromArgb(51, 153, 255);
            btnLaunchBatGui.FlatStyle = FlatStyle.Flat;
            btnLaunchBatGui.Font = new Font("Segoe UI", 11F);
            btnLaunchBatGui.ForeColor = Color.White;
            btnLaunchBatGui.Location = new Point(5, 124);
            btnLaunchBatGui.Name = "btnLaunchBatGui";
            btnLaunchBatGui.Size = new Size(375, 60);
            btnLaunchBatGui.TabIndex = 2;
            btnLaunchBatGui.Text = "Launch BatGui";
            btnLaunchBatGui.UseVisualStyleBackColor = false;
            btnLaunchBatGui.Click += BtnLaunchBatGui_Click;
            // 
            // pnlConfig
            // 
            pnlConfig.BackColor = Color.FromArgb(45, 45, 48);
            pnlConfig.Controls.Add(lblConfigTitle);
            pnlConfig.Controls.Add(btnGeneralSettings);
            pnlConfig.Controls.Add(btnControllerMappings);
            pnlConfig.Location = new Point(5, 234);
            pnlConfig.Name = "pnlConfig";
            pnlConfig.Size = new Size(385, 211);
            pnlConfig.TabIndex = 1;
            // 
            // lblConfigTitle
            // 
            lblConfigTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblConfigTitle.BackColor = Color.Transparent;
            lblConfigTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblConfigTitle.ForeColor = Color.White;
            lblConfigTitle.Location = new Point(5, 2);
            lblConfigTitle.Name = "lblConfigTitle";
            lblConfigTitle.Size = new Size(375, 42);
            lblConfigTitle.TabIndex = 0;
            lblConfigTitle.Text = "Configuration";
            lblConfigTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnGeneralSettings
            // 
            btnGeneralSettings.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnGeneralSettings.AutoEllipsis = true;
            btnGeneralSettings.BackColor = Color.FromArgb(104, 33, 122);
            btnGeneralSettings.FlatAppearance.BorderSize = 0;
            btnGeneralSettings.FlatAppearance.MouseDownBackColor = Color.FromArgb(75, 18, 88);
            btnGeneralSettings.FlatAppearance.MouseOverBackColor = Color.FromArgb(153, 51, 178);
            btnGeneralSettings.FlatStyle = FlatStyle.Flat;
            btnGeneralSettings.Font = new Font("Segoe UI", 11F);
            btnGeneralSettings.ForeColor = Color.White;
            btnGeneralSettings.Location = new Point(5, 54);
            btnGeneralSettings.Name = "btnGeneralSettings";
            btnGeneralSettings.Size = new Size(375, 60);
            btnGeneralSettings.TabIndex = 1;
            btnGeneralSettings.Text = "General Settings";
            btnGeneralSettings.UseVisualStyleBackColor = false;
            btnGeneralSettings.Click += BtnGeneralSettings_Click;
            // 
            // btnControllerMappings
            // 
            btnControllerMappings.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnControllerMappings.AutoEllipsis = true;
            btnControllerMappings.BackColor = Color.FromArgb(104, 33, 122);
            btnControllerMappings.FlatAppearance.BorderSize = 0;
            btnControllerMappings.FlatAppearance.MouseDownBackColor = Color.FromArgb(75, 18, 88);
            btnControllerMappings.FlatAppearance.MouseOverBackColor = Color.FromArgb(153, 51, 178);
            btnControllerMappings.FlatStyle = FlatStyle.Flat;
            btnControllerMappings.Font = new Font("Segoe UI", 11F);
            btnControllerMappings.ForeColor = Color.White;
            btnControllerMappings.Location = new Point(5, 124);
            btnControllerMappings.Name = "btnControllerMappings";
            btnControllerMappings.Size = new Size(375, 60);
            btnControllerMappings.TabIndex = 2;
            btnControllerMappings.Text = "Controller Mappings";
            btnControllerMappings.UseVisualStyleBackColor = false;
            btnControllerMappings.Click += BtnControllerMappings_Click;
            // 
            // pnlSupport
            // 
            pnlSupport.BackColor = Color.FromArgb(45, 45, 48);
            pnlSupport.Controls.Add(lblHelpTitle);
            pnlSupport.Controls.Add(btnViewLogs);
            pnlSupport.Controls.Add(btnViewErrorLogs);
            pnlSupport.Controls.Add(btnAbout);
            pnlSupport.Location = new Point(400, 9);
            pnlSupport.Name = "pnlSupport";
            pnlSupport.Size = new Size(380, 252);
            pnlSupport.TabIndex = 2;
            // 
            // lblHelpTitle
            // 
            lblHelpTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblHelpTitle.BackColor = Color.Transparent;
            lblHelpTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblHelpTitle.ForeColor = Color.White;
            lblHelpTitle.Location = new Point(5, 0);
            lblHelpTitle.Name = "lblHelpTitle";
            lblHelpTitle.Size = new Size(370, 42);
            lblHelpTitle.TabIndex = 0;
            lblHelpTitle.Text = "Aide Support";
            lblHelpTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnViewLogs
            // 
            btnViewLogs.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnViewLogs.AutoEllipsis = true;
            btnViewLogs.BackColor = Color.FromArgb(87, 87, 38);
            btnViewLogs.FlatAppearance.BorderSize = 0;
            btnViewLogs.FlatAppearance.MouseDownBackColor = Color.FromArgb(60, 60, 20);
            btnViewLogs.FlatAppearance.MouseOverBackColor = Color.FromArgb(128, 128, 51);
            btnViewLogs.FlatStyle = FlatStyle.Flat;
            btnViewLogs.Font = new Font("Segoe UI", 11F);
            btnViewLogs.ForeColor = Color.White;
            btnViewLogs.Location = new Point(5, 54);
            btnViewLogs.Name = "btnViewLogs";
            btnViewLogs.Size = new Size(370, 60);
            btnViewLogs.TabIndex = 1;
            btnViewLogs.Text = "View Logs";
            btnViewLogs.UseVisualStyleBackColor = false;
            btnViewLogs.Click += BtnViewLogs_Click;
            // 
            // btnViewErrorLogs
            // 
            btnViewErrorLogs.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnViewErrorLogs.AutoEllipsis = true;
            btnViewErrorLogs.BackColor = Color.FromArgb(87, 87, 38);
            btnViewErrorLogs.FlatAppearance.BorderSize = 0;
            btnViewErrorLogs.FlatAppearance.MouseDownBackColor = Color.FromArgb(60, 60, 20);
            btnViewErrorLogs.FlatAppearance.MouseOverBackColor = Color.FromArgb(128, 128, 51);
            btnViewErrorLogs.FlatStyle = FlatStyle.Flat;
            btnViewErrorLogs.Font = new Font("Segoe UI", 11F);
            btnViewErrorLogs.ForeColor = Color.White;
            btnViewErrorLogs.Location = new Point(5, 121);
            btnViewErrorLogs.Name = "btnViewErrorLogs";
            btnViewErrorLogs.Size = new Size(370, 60);
            btnViewErrorLogs.TabIndex = 2;
            btnViewErrorLogs.Text = "View Error Logs";
            btnViewErrorLogs.UseVisualStyleBackColor = false;
            btnViewErrorLogs.Click += BtnViewErrorLogs_Click;
            // 
            // btnAbout
            // 
            btnAbout.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnAbout.AutoEllipsis = true;
            btnAbout.BackColor = Color.FromArgb(87, 87, 38);
            btnAbout.FlatAppearance.BorderSize = 0;
            btnAbout.FlatAppearance.MouseDownBackColor = Color.FromArgb(60, 60, 20);
            btnAbout.FlatAppearance.MouseOverBackColor = Color.FromArgb(128, 128, 51);
            btnAbout.FlatStyle = FlatStyle.Flat;
            btnAbout.Font = new Font("Segoe UI", 11F);
            btnAbout.ForeColor = Color.White;
            btnAbout.Location = new Point(5, 188);
            btnAbout.Name = "btnAbout";
            btnAbout.Size = new Size(370, 60);
            btnAbout.TabIndex = 3;
            btnAbout.Text = "About";
            btnAbout.UseVisualStyleBackColor = false;
            btnAbout.Click += BtnAbout_Click;
            // 
            // pnlTools
            // 
            pnlTools.BackColor = Color.FromArgb(45, 45, 48);
            pnlTools.Controls.Add(btnToolsPlugins);
            pnlTools.Location = new Point(400, 271);
            pnlTools.Name = "pnlTools";
            pnlTools.Size = new Size(380, 82);
            pnlTools.TabIndex = 3;
            // 
            // btnToolsPlugins
            // 
            btnToolsPlugins.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnToolsPlugins.AutoEllipsis = true;
            btnToolsPlugins.BackColor = Color.FromArgb(0, 122, 204);
            btnToolsPlugins.FlatAppearance.BorderSize = 0;
            btnToolsPlugins.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 84, 153);
            btnToolsPlugins.FlatAppearance.MouseOverBackColor = Color.FromArgb(51, 153, 255);
            btnToolsPlugins.FlatStyle = FlatStyle.Flat;
            btnToolsPlugins.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnToolsPlugins.ForeColor = Color.White;
            btnToolsPlugins.Location = new Point(5, 11);
            btnToolsPlugins.Name = "btnToolsPlugins";
            btnToolsPlugins.Size = new Size(370, 60);
            btnToolsPlugins.TabIndex = 0;
            btnToolsPlugins.Text = "Tools / Plugins GitHub";
            btnToolsPlugins.UseVisualStyleBackColor = false;
            btnToolsPlugins.Click += BtnToolsPlugins_Click;
            // 
            // pnlShell
            // 
            pnlShell.BackColor = Color.FromArgb(45, 45, 48);
            pnlShell.Controls.Add(btnShellLauncher);
            pnlShell.Location = new Point(400, 363);
            pnlShell.Name = "pnlShell";
            pnlShell.Size = new Size(380, 82);
            pnlShell.TabIndex = 4;
            // 
            // btnShellLauncher
            // 
            btnShellLauncher.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnShellLauncher.AutoEllipsis = true;
            btnShellLauncher.BackColor = Color.FromArgb(104, 33, 122);
            btnShellLauncher.FlatAppearance.BorderSize = 0;
            btnShellLauncher.FlatAppearance.MouseDownBackColor = Color.FromArgb(75, 18, 88);
            btnShellLauncher.FlatAppearance.MouseOverBackColor = Color.FromArgb(153, 51, 178);
            btnShellLauncher.FlatStyle = FlatStyle.Flat;
            btnShellLauncher.Font = new Font("Segoe UI", 11F);
            btnShellLauncher.ForeColor = Color.White;
            btnShellLauncher.Location = new Point(5, 11);
            btnShellLauncher.Name = "btnShellLauncher";
            btnShellLauncher.Size = new Size(370, 60);
            btnShellLauncher.TabIndex = 0;
            btnShellLauncher.Text = "Shell Launcher";
            btnShellLauncher.UseVisualStyleBackColor = false;
            btnShellLauncher.Click += BtnShellLauncher_Click;
            // 
            // pnlVersion
            // 
            pnlVersion.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlVersion.BackColor = Color.FromArgb(45, 45, 48);
            pnlVersion.Controls.Add(lblVersion);
            pnlVersion.Controls.Add(pnlUpdateIndicator);
            pnlVersion.Controls.Add(lblUpdateStatus);
            pnlVersion.Location = new Point(5, 456);
            pnlVersion.Name = "pnlVersion";
            pnlVersion.Size = new Size(775, 55);
            pnlVersion.TabIndex = 5;
            // 
            // lblVersion
            // 
            lblVersion.BackColor = Color.Transparent;
            lblVersion.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblVersion.ForeColor = Color.White;
            lblVersion.Location = new Point(15, 12);
            lblVersion.Name = "lblVersion";
            lblVersion.Size = new Size(200, 30);
            lblVersion.TabIndex = 0;
            lblVersion.Text = "v0.0.0";
            lblVersion.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // pnlUpdateIndicator
            // 
            pnlUpdateIndicator.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pnlUpdateIndicator.BackColor = Color.Gray;
            pnlUpdateIndicator.Location = new Point(481, 17);
            pnlUpdateIndicator.Name = "pnlUpdateIndicator";
            pnlUpdateIndicator.Size = new Size(20, 20);
            pnlUpdateIndicator.TabIndex = 1;
            // 
            // lblUpdateStatus
            // 
            lblUpdateStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblUpdateStatus.BackColor = Color.Transparent;
            lblUpdateStatus.Font = new Font("Segoe UI", 11F);
            lblUpdateStatus.ForeColor = Color.White;
            lblUpdateStatus.Location = new Point(509, 10);
            lblUpdateStatus.Name = "lblUpdateStatus";
            lblUpdateStatus.Size = new Size(248, 34);
            lblUpdateStatus.TabIndex = 2;
            lblUpdateStatus.Text = "Checking...";
            lblUpdateStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(32, 32, 32);
            ClientSize = new Size(784, 521);
            Controls.Add(pnlAction);
            Controls.Add(pnlConfig);
            Controls.Add(pnlSupport);
            Controls.Add(pnlTools);
            Controls.Add(pnlShell);
            Controls.Add(pnlVersion);
            Font = new Font("Segoe UI", 9F);
            ForeColor = Color.White;
            MaximumSize = new Size(800, 560);
            MinimumSize = new Size(800, 560);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "BatRun";
            pnlAction.ResumeLayout(false);
            pnlConfig.ResumeLayout(false);
            pnlSupport.ResumeLayout(false);
            pnlTools.ResumeLayout(false);
            pnlShell.ResumeLayout(false);
            pnlVersion.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        // Control field declarations
        private System.Windows.Forms.Panel  pnlAction;
        private System.Windows.Forms.Label  lblLauncherTitle;
        private System.Windows.Forms.Button btnLaunchRetroBat;
        private System.Windows.Forms.Button btnLaunchBatGui;

        private System.Windows.Forms.Panel  pnlConfig;
        private System.Windows.Forms.Label  lblConfigTitle;
        private System.Windows.Forms.Button btnGeneralSettings;
        private System.Windows.Forms.Button btnControllerMappings;

        private System.Windows.Forms.Panel  pnlSupport;
        private System.Windows.Forms.Label  lblHelpTitle;
        private System.Windows.Forms.Button btnViewLogs;
        private System.Windows.Forms.Button btnViewErrorLogs;
        private System.Windows.Forms.Button btnAbout;

        private System.Windows.Forms.Panel  pnlTools;
        private System.Windows.Forms.Button btnToolsPlugins;

        private System.Windows.Forms.Panel  pnlShell;
        private System.Windows.Forms.Button btnShellLauncher;

        private System.Windows.Forms.Panel  pnlVersion;
        private System.Windows.Forms.Label  lblVersion;
        private System.Windows.Forms.Panel  pnlUpdateIndicator;
        private System.Windows.Forms.Label  lblUpdateStatus;
    }
}

