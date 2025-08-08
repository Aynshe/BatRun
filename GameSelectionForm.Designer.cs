namespace BatRun
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
            this.systemListBox = new System.Windows.Forms.ListBox();
            this.gameListBox = new System.Windows.Forms.ListBox();
            this.selectButton = new System.Windows.Forms.Button();
            this.backButton = new System.Windows.Forms.Button();
            this.randomCheckBox = new System.Windows.Forms.CheckBox();
            this.titleLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // systemListBox
            //
            this.systemListBox.FormattingEnabled = true;
            this.systemListBox.ItemHeight = 15;
            this.systemListBox.Location = new System.Drawing.Point(12, 40);
            this.systemListBox.Name = "systemListBox";
            this.systemListBox.Size = new System.Drawing.Size(360, 304);
            this.systemListBox.TabIndex = 0;
            this.systemListBox.DoubleClick += new System.EventHandler(this.SystemListBox_DoubleClick);
            //
            // gameListBox
            //
            this.gameListBox.FormattingEnabled = true;
            this.gameListBox.ItemHeight = 15;
            this.gameListBox.Location = new System.Drawing.Point(12, 40);
            this.gameListBox.Name = "gameListBox";
            this.gameListBox.Size = new System.Drawing.Size(360, 304);
            this.gameListBox.TabIndex = 1;
            this.gameListBox.Visible = false;
            this.gameListBox.DoubleClick += new System.EventHandler(this.GameListBox_DoubleClick);
            //
            // selectButton
            //
            this.selectButton.Location = new System.Drawing.Point(297, 350);
            this.selectButton.Name = "selectButton";
            this.selectButton.Size = new System.Drawing.Size(75, 23);
            this.selectButton.TabIndex = 2;
            this.selectButton.Text = "Select";
            this.selectButton.UseVisualStyleBackColor = true;
            this.selectButton.Click += new System.EventHandler(this.SelectButton_Click);
            //
            // backButton
            //
            this.backButton.Location = new System.Drawing.Point(12, 350);
            this.backButton.Name = "backButton";
            this.backButton.Size = new System.Drawing.Size(75, 23);
            this.backButton.TabIndex = 3;
            this.backButton.Text = "Back";
            this.backButton.UseVisualStyleBackColor = true;
            this.backButton.Visible = false;
            this.backButton.Click += new System.EventHandler(this.BackButton_Click);
            //
            // randomCheckBox
            //
            this.randomCheckBox.AutoSize = true;
            this.randomCheckBox.Location = new System.Drawing.Point(12, 380);
            this.randomCheckBox.Name = "randomCheckBox";
            this.randomCheckBox.Size = new System.Drawing.Size(155, 19);
            this.randomCheckBox.TabIndex = 4;
            this.randomCheckBox.Text = "Select a random game";
            this.randomCheckBox.UseVisualStyleBackColor = true;
            this.randomCheckBox.CheckedChanged += new System.EventHandler(this.RandomCheckBox_CheckedChanged);
            //
            // titleLabel
            //
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.titleLabel.Location = new System.Drawing.Point(12, 9);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(126, 21);
            this.titleLabel.TabIndex = 5;
            this.titleLabel.Text = "Select System";
            //
            // GameSelectionForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 411);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.randomCheckBox);
            this.Controls.Add(this.backButton);
            this.Controls.Add(this.selectButton);
            this.Controls.Add(this.gameListBox);
            this.Controls.Add(this.systemListBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GameSelectionForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select Game for Auto-Launch";
            this.Load += new System.EventHandler(this.GameSelectionForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ListBox systemListBox;
        private System.Windows.Forms.ListBox gameListBox;
        private System.Windows.Forms.Button selectButton;
        private System.Windows.Forms.Button backButton;
        private System.Windows.Forms.CheckBox randomCheckBox;
        private System.Windows.Forms.Label titleLabel;
    }
}
