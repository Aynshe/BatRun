using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.UI
{
    partial class ShellConfigurationForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.configPanel = new System.Windows.Forms.Panel();
            this.commandListView = new System.Windows.Forms.ListView();
            this.colNum = new System.Windows.Forms.ColumnHeader();
            this.colPath = new System.Windows.Forms.ColumnHeader();
            this.colEnable = new System.Windows.Forms.ColumnHeader();
            this.colDelay = new System.Windows.Forms.ColumnHeader();
            this.colType = new System.Windows.Forms.ColumnHeader();
            this.colAutoHide = new System.Windows.Forms.ColumnHeader();
            this.colDoubleLaunch = new System.Windows.Forms.ColumnHeader();
            this.actionPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.addButton = new System.Windows.Forms.Button();
            this.removeButton = new System.Windows.Forms.Button();
            this.scrapButton = new System.Windows.Forms.Button();
            this.arcadeButton = new System.Windows.Forms.Button();
            this.topPanel = new System.Windows.Forms.Panel();
            this.postLaunchGameLabel = new System.Windows.Forms.Label();
            this.clearPostLaunchGameButton = new System.Windows.Forms.Button();
            this.randomSystemComboBox = new System.Windows.Forms.ComboBox();
            this.randomGameCheckBox = new System.Windows.Forms.CheckBox();
            this.retroBatPanel = new System.Windows.Forms.Panel();
            this.retroBatDelayNumericLabel = new System.Windows.Forms.Label();
            this.retroBatDelayNumeric = new System.Windows.Forms.NumericUpDown();
            this.retroBatLabel = new System.Windows.Forms.Label();
            this.launchRetroBatCheckBox = new System.Windows.Forms.CheckBox();
            this.windowsPolicyCheckBox = new System.Windows.Forms.CheckBox();
            this.buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.saveButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.mainLayout.SuspendLayout();
            this.configPanel.SuspendLayout();
            this.actionPanel.SuspendLayout();
            this.topPanel.SuspendLayout();
            this.retroBatPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.retroBatDelayNumeric)).BeginInit();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainLayout
            // 
            this.mainLayout.ColumnCount = 1;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Controls.Add(this.configPanel, 0, 0);
            this.mainLayout.Controls.Add(this.buttonPanel, 0, 1);
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Location = new System.Drawing.Point(0, 0);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.Padding = new System.Windows.Forms.Padding(20);
            this.mainLayout.RowCount = 2;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.mainLayout.Size = new System.Drawing.Size(850, 750);
            this.mainLayout.TabIndex = 0;
            // 
            // configPanel
            // 
            this.configPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.configPanel.Controls.Add(this.commandListView);
            this.configPanel.Controls.Add(this.actionPanel);
            this.configPanel.Controls.Add(this.topPanel);
            this.configPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.configPanel.Location = new System.Drawing.Point(23, 23);
            this.configPanel.Name = "configPanel";
            this.configPanel.Padding = new System.Windows.Forms.Padding(10);
            this.configPanel.Size = new System.Drawing.Size(804, 664);
            this.configPanel.TabIndex = 0;
            // 
            // commandListView
            // 
            this.commandListView.AllowDrop = true;
            this.commandListView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.commandListView.CheckBoxes = true;
            this.commandListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colNum,
            this.colPath,
            this.colEnable,
            this.colDelay,
            this.colType,
            this.colAutoHide,
            this.colDoubleLaunch});
            this.commandListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.commandListView.ForeColor = System.Drawing.Color.White;
            this.commandListView.FullRowSelect = true;
            this.commandListView.GridLines = true;
            this.commandListView.HideSelection = false;
            this.commandListView.Location = new System.Drawing.Point(10, 190);
            this.commandListView.Name = "commandListView";
            this.commandListView.Size = new System.Drawing.Size(784, 464);
            this.commandListView.TabIndex = 2;
            this.commandListView.UseCompatibleStateImageBehavior = false;
            this.commandListView.View = System.Windows.Forms.View.Details;
            this.commandListView.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.CommandListView_ItemChecked);
            this.commandListView.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.CommandListView_ItemDrag);
            this.commandListView.DragDrop += new System.Windows.Forms.DragEventHandler(this.CommandListView_DragDrop);
            this.commandListView.DragEnter += new System.Windows.Forms.DragEventHandler(this.CommandListView_DragEnter);
            this.commandListView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.CommandListView_MouseClick);
            this.commandListView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.CommandListView_MouseDoubleClick);
            // 
            // colNum
            // 
            this.colNum.Text = "#";
            this.colNum.Width = 40;
            // 
            // colPath
            // 
            this.colPath.Text = "Path";
            this.colPath.Width = 240;
            // 
            // colEnable
            // 
            this.colEnable.Text = "Enable";
            // 
            // colDelay
            // 
            this.colDelay.Text = "Delay (seconds)";
            this.colDelay.Width = 100;
            // 
            // colType
            // 
            this.colType.Text = "Type";
            this.colType.Width = 80;
            // 
            // colAutoHide
            // 
            this.colAutoHide.Text = "Auto-Hide";
            this.colAutoHide.Width = 100;
            // 
            // colDoubleLaunch
            // 
            this.colDoubleLaunch.Text = "Double Launch";
            this.colDoubleLaunch.Width = 110;
            // 
            // actionPanel
            // 
            this.actionPanel.BackColor = System.Drawing.Color.Transparent;
            this.actionPanel.Controls.Add(this.addButton);
            this.actionPanel.Controls.Add(this.removeButton);
            this.actionPanel.Controls.Add(this.scrapButton);
            this.actionPanel.Controls.Add(this.arcadeButton);
            this.actionPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.actionPanel.Location = new System.Drawing.Point(10, 150);
            this.actionPanel.Name = "actionPanel";
            this.actionPanel.Size = new System.Drawing.Size(784, 40);
            this.actionPanel.TabIndex = 1;
            // 
            // addButton
            // 
            this.addButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.addButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.addButton.ForeColor = System.Drawing.Color.White;
            this.addButton.Location = new System.Drawing.Point(10, 0);
            this.addButton.Margin = new System.Windows.Forms.Padding(10, 0, 5, 0);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(40, 30);
            this.addButton.TabIndex = 0;
            this.addButton.Text = "+";
            this.addButton.UseVisualStyleBackColor = false;
            this.addButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // removeButton
            // 
            this.removeButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(204)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.removeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.removeButton.ForeColor = System.Drawing.Color.White;
            this.removeButton.Location = new System.Drawing.Point(55, 0);
            this.removeButton.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.removeButton.Name = "removeButton";
            this.removeButton.Size = new System.Drawing.Size(40, 30);
            this.removeButton.TabIndex = 1;
            this.removeButton.Text = "-";
            this.removeButton.UseVisualStyleBackColor = false;
            this.removeButton.Click += new System.EventHandler(this.RemoveButton_Click);
            // 
            // scrapButton
            // 
            this.scrapButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(62)))));
            this.scrapButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.scrapButton.ForeColor = System.Drawing.Color.White;
            this.scrapButton.Location = new System.Drawing.Point(110, 0);
            this.scrapButton.Margin = new System.Windows.Forms.Padding(10, 0, 5, 0);
            this.scrapButton.Name = "scrapButton";
            this.scrapButton.Size = new System.Drawing.Size(100, 30);
            this.scrapButton.TabIndex = 2;
            this.scrapButton.Text = "Scrap Game";
            this.scrapButton.UseVisualStyleBackColor = false;
            this.scrapButton.Click += new System.EventHandler(this.ScrapButton_Click);
            // 
            // arcadeButton
            // 
            this.arcadeButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(0)))), ((int)(((byte)(128)))));
            this.arcadeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.arcadeButton.ForeColor = System.Drawing.Color.White;
            this.arcadeButton.Location = new System.Drawing.Point(225, 0);
            this.arcadeButton.Margin = new System.Windows.Forms.Padding(10, 0, 5, 0);
            this.arcadeButton.Name = "arcadeButton";
            this.arcadeButton.Size = new System.Drawing.Size(120, 30);
            this.arcadeButton.TabIndex = 3;
            this.arcadeButton.Text = "Mode / Cloud 🕹️";
            this.arcadeButton.UseVisualStyleBackColor = false;
            this.arcadeButton.Click += new System.EventHandler(this.ArcadeButton_Click);
            // 
            // topPanel
            // 
            this.topPanel.BackColor = System.Drawing.Color.Transparent;
            this.topPanel.Controls.Add(this.postLaunchGameLabel);
            this.topPanel.Controls.Add(this.clearPostLaunchGameButton);
            this.topPanel.Controls.Add(this.randomSystemComboBox);
            this.topPanel.Controls.Add(this.randomGameCheckBox);
            this.topPanel.Controls.Add(this.retroBatPanel);
            this.topPanel.Controls.Add(this.windowsPolicyCheckBox);
            this.topPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.topPanel.Location = new System.Drawing.Point(10, 10);
            this.topPanel.Name = "topPanel";
            this.topPanel.Padding = new System.Windows.Forms.Padding(10);
            this.topPanel.Size = new System.Drawing.Size(784, 140);
            this.topPanel.TabIndex = 0;
            // 
            // postLaunchGameLabel
            // 
            this.postLaunchGameLabel.AutoEllipsis = true;
            this.postLaunchGameLabel.ForeColor = System.Drawing.Color.White;
            this.postLaunchGameLabel.Location = new System.Drawing.Point(75, 100);
            this.postLaunchGameLabel.Name = "postLaunchGameLabel";
            this.postLaunchGameLabel.Size = new System.Drawing.Size(400, 22);
            this.postLaunchGameLabel.TabIndex = 5;
            this.postLaunchGameLabel.Text = "Game to launch after RetroBat: None";
            // 
            // clearPostLaunchGameButton
            // 
            this.clearPostLaunchGameButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(87)))), ((int)(((byte)(87)))), ((int)(((byte)(87)))));
            this.clearPostLaunchGameButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.clearPostLaunchGameButton.ForeColor = System.Drawing.Color.White;
            this.clearPostLaunchGameButton.Location = new System.Drawing.Point(10, 98);
            this.clearPostLaunchGameButton.Name = "clearPostLaunchGameButton";
            this.clearPostLaunchGameButton.Size = new System.Drawing.Size(60, 22);
            this.clearPostLaunchGameButton.TabIndex = 4;
            this.clearPostLaunchGameButton.Text = "Clear";
            this.clearPostLaunchGameButton.UseVisualStyleBackColor = false;
            this.clearPostLaunchGameButton.Click += new System.EventHandler(this.ClearPostLaunchGameButton_Click);
            // 
            // randomSystemComboBox
            // 
            this.randomSystemComboBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.randomSystemComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.randomSystemComboBox.Enabled = false;
            this.randomSystemComboBox.ForeColor = System.Drawing.Color.White;
            this.randomSystemComboBox.FormattingEnabled = true;
            this.randomSystemComboBox.Location = new System.Drawing.Point(400, 68);
            this.randomSystemComboBox.Name = "randomSystemComboBox";
            this.randomSystemComboBox.Size = new System.Drawing.Size(300, 23);
            this.randomSystemComboBox.TabIndex = 3;
            // 
            // randomGameCheckBox
            // 
            this.randomGameCheckBox.AutoSize = true;
            this.randomGameCheckBox.BackColor = System.Drawing.Color.Transparent;
            this.randomGameCheckBox.ForeColor = System.Drawing.Color.White;
            this.randomGameCheckBox.Location = new System.Drawing.Point(10, 70);
            this.randomGameCheckBox.Name = "randomGameCheckBox";
            this.randomGameCheckBox.Size = new System.Drawing.Size(342, 19);
            this.randomGameCheckBox.TabIndex = 2;
            this.randomGameCheckBox.Text = "Launch a random game at startup (Shell Launcher only)";
            this.randomGameCheckBox.UseVisualStyleBackColor = false;
            this.randomGameCheckBox.CheckedChanged += new System.EventHandler(this.RandomGameCheckBox_CheckedChanged);
            // 
            // retroBatPanel
            // 
            this.retroBatPanel.BackColor = System.Drawing.Color.Transparent;
            this.retroBatPanel.Controls.Add(this.retroBatDelayNumericLabel);
            this.retroBatPanel.Controls.Add(this.retroBatDelayNumeric);
            this.retroBatPanel.Controls.Add(this.retroBatLabel);
            this.retroBatPanel.Controls.Add(this.launchRetroBatCheckBox);
            this.retroBatPanel.Location = new System.Drawing.Point(10, 40);
            this.retroBatPanel.Name = "retroBatPanel";
            this.retroBatPanel.Size = new System.Drawing.Size(600, 30);
            this.retroBatPanel.TabIndex = 1;
            // 
            // retroBatDelayNumericLabel
            // 
            this.retroBatDelayNumericLabel.AutoSize = true;
            this.retroBatDelayNumericLabel.Location = new System.Drawing.Point(400, 5);
            this.retroBatDelayNumericLabel.Name = "retroBatDelayNumericLabel";
            this.retroBatDelayNumericLabel.Size = new System.Drawing.Size(89, 15);
            this.retroBatDelayNumericLabel.TabIndex = 3;
            this.retroBatDelayNumericLabel.Text = "Delay (seconds)";
            // 
            // retroBatDelayNumeric
            // 
            this.retroBatDelayNumeric.Location = new System.Drawing.Point(350, 3);
            this.retroBatDelayNumeric.Maximum = new decimal(new int[] {
            40,
            0,
            0,
            0});
            this.retroBatDelayNumeric.Name = "retroBatDelayNumeric";
            this.retroBatDelayNumeric.Size = new System.Drawing.Size(40, 23);
            this.retroBatDelayNumeric.TabIndex = 2;
            // 
            // retroBatLabel
            // 
            this.retroBatLabel.AutoSize = true;
            this.retroBatLabel.Location = new System.Drawing.Point(70, 5);
            this.retroBatLabel.Name = "retroBatLabel";
            this.retroBatLabel.Size = new System.Drawing.Size(207, 15);
            this.retroBatLabel.TabIndex = 1;
            this.retroBatLabel.Text = "Launch RetroBAT at the end of the list";
            // 
            // launchRetroBatCheckBox
            // 
            this.launchRetroBatCheckBox.AutoSize = true;
            this.launchRetroBatCheckBox.Location = new System.Drawing.Point(0, 5);
            this.launchRetroBatCheckBox.Name = "launchRetroBatCheckBox";
            this.launchRetroBatCheckBox.Size = new System.Drawing.Size(61, 19);
            this.launchRetroBatCheckBox.TabIndex = 0;
            this.launchRetroBatCheckBox.Text = "Enable";
            this.launchRetroBatCheckBox.UseVisualStyleBackColor = true;
            // 
            // windowsPolicyCheckBox
            // 
            this.windowsPolicyCheckBox.AutoSize = true;
            this.windowsPolicyCheckBox.BackColor = System.Drawing.Color.Transparent;
            this.windowsPolicyCheckBox.ForeColor = System.Drawing.Color.White;
            this.windowsPolicyCheckBox.Location = new System.Drawing.Point(10, 10);
            this.windowsPolicyCheckBox.Name = "windowsPolicyCheckBox";
            this.windowsPolicyCheckBox.Size = new System.Drawing.Size(294, 19);
            this.windowsPolicyCheckBox.TabIndex = 0;
            this.windowsPolicyCheckBox.Text = "Enable Custom User Interface in Group Policy";
            this.windowsPolicyCheckBox.UseVisualStyleBackColor = false;
            this.windowsPolicyCheckBox.CheckedChanged += new System.EventHandler(this.WindowsPolicyCheckBox_CheckedChanged);
            // 
            // buttonPanel
            // 
            this.buttonPanel.BackColor = System.Drawing.Color.Transparent;
            this.buttonPanel.Controls.Add(this.cancelButton);
            this.buttonPanel.Controls.Add(this.saveButton);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.buttonPanel.Location = new System.Drawing.Point(23, 693);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Size = new System.Drawing.Size(804, 34);
            this.buttonPanel.TabIndex = 1;
            // 
            // saveButton
            // 
            this.saveButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.saveButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.saveButton.ForeColor = System.Drawing.Color.White;
            this.saveButton.Location = new System.Drawing.Point(594, 5);
            this.saveButton.Margin = new System.Windows.Forms.Padding(5);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(100, 25);
            this.saveButton.TabIndex = 1;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = false;
            this.saveButton.Click += new System.EventHandler(this.SaveButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(87)))), ((int)(((byte)(87)))), ((int)(((byte)(87)))));
            this.cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cancelButton.ForeColor = System.Drawing.Color.White;
            this.cancelButton.Location = new System.Drawing.Point(704, 5);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(5);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(100, 25);
            this.cancelButton.TabIndex = 0;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = false;
            this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // ShellConfigurationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.ClientSize = new System.Drawing.Size(850, 750);
            this.Controls.Add(this.mainLayout);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ShellConfigurationForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Shell Configuration";
            this.mainLayout.ResumeLayout(false);
            this.configPanel.ResumeLayout(false);
            this.actionPanel.ResumeLayout(false);
            this.topPanel.ResumeLayout(false);
            this.topPanel.PerformLayout();
            this.retroBatPanel.ResumeLayout(false);
            this.retroBatPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.retroBatDelayNumeric)).EndInit();
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.Panel configPanel;
        private System.Windows.Forms.Panel topPanel;
        private System.Windows.Forms.CheckBox windowsPolicyCheckBox;
        private System.Windows.Forms.Panel retroBatPanel;
        private System.Windows.Forms.CheckBox launchRetroBatCheckBox;
        private System.Windows.Forms.Label retroBatLabel;
        private System.Windows.Forms.NumericUpDown retroBatDelayNumeric;
        private System.Windows.Forms.Label retroBatDelayNumericLabel;
        private System.Windows.Forms.CheckBox randomGameCheckBox;
        private System.Windows.Forms.ComboBox randomSystemComboBox;
        private System.Windows.Forms.Button clearPostLaunchGameButton;
        private System.Windows.Forms.Label postLaunchGameLabel;
        private System.Windows.Forms.FlowLayoutPanel actionPanel;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.Button removeButton;
        private System.Windows.Forms.Button scrapButton;
        private System.Windows.Forms.Button arcadeButton;
        private System.Windows.Forms.ListView commandListView;
        private System.Windows.Forms.ColumnHeader colNum;
        private System.Windows.Forms.ColumnHeader colPath;
        private System.Windows.Forms.ColumnHeader colEnable;
        private System.Windows.Forms.ColumnHeader colDelay;
        private System.Windows.Forms.ColumnHeader colType;
        private System.Windows.Forms.ColumnHeader colAutoHide;
        private System.Windows.Forms.ColumnHeader colDoubleLaunch;
        private System.Windows.Forms.FlowLayoutPanel buttonPanel;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Button cancelButton;
    }
}


