using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.UI
{
    partial class GameSelectionForm
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
            this.systemPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.systemLabel = new System.Windows.Forms.Label();
            this._systemComboBox = new System.Windows.Forms.ComboBox();
            this._gameListView = new System.Windows.Forms.ListView();
            this.gameColumn = new System.Windows.Forms.ColumnHeader();
            this._statusLabel = new System.Windows.Forms.Label();
            this.buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this.mainLayout.SuspendLayout();
            this.systemPanel.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainLayout
            // 
            this.mainLayout.ColumnCount = 1;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Controls.Add(this.systemPanel, 0, 0);
            this.mainLayout.Controls.Add(this._gameListView, 0, 1);
            this.mainLayout.Controls.Add(this._statusLabel, 0, 2);
            this.mainLayout.Controls.Add(this.buttonPanel, 0, 3);
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Location = new System.Drawing.Point(0, 0);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.Padding = new System.Windows.Forms.Padding(10);
            this.mainLayout.RowCount = 4;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.mainLayout.Size = new System.Drawing.Size(600, 450);
            this.mainLayout.TabIndex = 0;
            // 
            // systemPanel
            // 
            this.systemPanel.Controls.Add(this.systemLabel);
            this.systemPanel.Controls.Add(this._systemComboBox);
            this.systemPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.systemPanel.Location = new System.Drawing.Point(13, 13);
            this.systemPanel.Name = "systemPanel";
            this.systemPanel.Size = new System.Drawing.Size(574, 34);
            this.systemPanel.TabIndex = 0;
            // 
            // systemLabel
            // 
            this.systemLabel.AutoSize = true;
            this.systemLabel.Location = new System.Drawing.Point(3, 6);
            this.systemLabel.Margin = new System.Windows.Forms.Padding(3, 6, 3, 0);
            this.systemLabel.Name = "systemLabel";
            this.systemLabel.Size = new System.Drawing.Size(48, 15);
            this.systemLabel.TabIndex = 0;
            this.systemLabel.Text = "System:";
            // 
            // _systemComboBox
            // 
            this._systemComboBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this._systemComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._systemComboBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._systemComboBox.ForeColor = System.Drawing.Color.White;
            this._systemComboBox.FormattingEnabled = true;
            this._systemComboBox.Location = new System.Drawing.Point(57, 3);
            this._systemComboBox.Name = "_systemComboBox";
            this._systemComboBox.Size = new System.Drawing.Size(400, 23);
            this._systemComboBox.TabIndex = 1;
            this._systemComboBox.SelectedIndexChanged += new System.EventHandler(this.SystemComboBox_SelectedIndexChanged);
            // 
            // _gameListView
            // 
            this._gameListView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this._gameListView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._gameListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.gameColumn});
            this._gameListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._gameListView.ForeColor = System.Drawing.Color.White;
            this._gameListView.FullRowSelect = true;
            this._gameListView.HideSelection = false;
            this._gameListView.Location = new System.Drawing.Point(13, 53);
            this._gameListView.Name = "_gameListView";
            this._gameListView.Size = new System.Drawing.Size(574, 304);
            this._gameListView.TabIndex = 1;
            this._gameListView.UseCompatibleStateImageBehavior = false;
            this._gameListView.View = System.Windows.Forms.View.Details;
            this._gameListView.SelectedIndexChanged += new System.EventHandler(this.GameListView_SelectedIndexChanged);
            this._gameListView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.GameListView_MouseDoubleClick);
            // 
            // gameColumn
            // 
            this.gameColumn.Text = "Game";
            this.gameColumn.Width = 550;
            // 
            // _statusLabel
            // 
            this._statusLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._statusLabel.Location = new System.Drawing.Point(13, 360);
            this._statusLabel.Name = "_statusLabel";
            this._statusLabel.Size = new System.Drawing.Size(574, 30);
            this._statusLabel.TabIndex = 2;
            this._statusLabel.Text = "Checking EmulationStation API...";
            this._statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // buttonPanel
            // 
            this.buttonPanel.Controls.Add(this._cancelButton);
            this.buttonPanel.Controls.Add(this._okButton);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.buttonPanel.Location = new System.Drawing.Point(13, 393);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Size = new System.Drawing.Size(574, 34);
            this.buttonPanel.TabIndex = 3;
            // 
            // _okButton
            // 
            this._okButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.Enabled = false;
            this._okButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._okButton.ForeColor = System.Drawing.Color.White;
            this._okButton.Location = new System.Drawing.Point(365, 3);
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(100, 25);
            this._okButton.TabIndex = 0;
            this._okButton.Text = "OK";
            this._okButton.UseVisualStyleBackColor = false;
            this._okButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // _cancelButton
            // 
            this._cancelButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(87)))), ((int)(((byte)(87)))), ((int)(((byte)(87)))));
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._cancelButton.ForeColor = System.Drawing.Color.White;
            this._cancelButton.Location = new System.Drawing.Point(471, 3);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(100, 25);
            this._cancelButton.TabIndex = 1;
            this._cancelButton.Text = "Cancel";
            this._cancelButton.UseVisualStyleBackColor = false;
            // 
            // GameSelectionForm
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(600, 450);
            this.Controls.Add(this.mainLayout);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GameSelectionForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select a Game from EmulationStation";
            this.mainLayout.ResumeLayout(false);
            this.systemPanel.ResumeLayout(false);
            this.systemPanel.PerformLayout();
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.FlowLayoutPanel systemPanel;
        private System.Windows.Forms.Label systemLabel;
        private System.Windows.Forms.ComboBox _systemComboBox;
        private System.Windows.Forms.ListView _gameListView;
        private System.Windows.Forms.ColumnHeader gameColumn;
        private System.Windows.Forms.Label _statusLabel;
        private System.Windows.Forms.FlowLayoutPanel buttonPanel;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton;
    }
}


