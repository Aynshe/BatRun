namespace BatRun
{
    partial class MappingConfigurationForm
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
            this.comboBoxJoysticks = new System.Windows.Forms.ComboBox();
            this.labelJoystick = new System.Windows.Forms.Label();
            this.labelHotkey = new System.Windows.Forms.Label();
            this.labelStart = new System.Windows.Forms.Label();
            this.textBoxHotkey = new System.Windows.Forms.TextBox();
            this.textBoxStart = new System.Windows.Forms.TextBox();
            this.buttonDetectHotkey = new System.Windows.Forms.Button();
            this.buttonDetectStart = new System.Windows.Forms.Button();
            this.buttonSave = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonResetCurrent = new System.Windows.Forms.Button();
            this.buttonResetAll = new System.Windows.Forms.Button();
            this.SuspendLayout();

            var mainLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            mainLayoutPanel.SuspendLayout();
            mainLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            mainLayoutPanel.Padding = new System.Windows.Forms.Padding(10);
            mainLayoutPanel.ColumnCount = 3;
            mainLayoutPanel.RowCount = 5;
            mainLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            mainLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            mainLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));

            // RowStyles
            for(int i=0; i<5; i++) mainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));

            // Controls
            this.labelJoystick.Text = "Select Controller:";
            mainLayoutPanel.Controls.Add(this.labelJoystick, 0, 0);
            mainLayoutPanel.SetColumnSpan(this.labelJoystick, 3);

            this.comboBoxJoysticks.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxJoysticks.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxJoysticks.SelectedIndexChanged += new System.EventHandler(this.comboBoxJoysticks_SelectedIndexChanged);
            mainLayoutPanel.Controls.Add(this.comboBoxJoysticks, 0, 1);
            mainLayoutPanel.SetColumnSpan(this.comboBoxJoysticks, 3);

            this.labelHotkey.Text = "Hotkey Button:";
            this.labelHotkey.Anchor = System.Windows.Forms.AnchorStyles.Left;
            mainLayoutPanel.Controls.Add(this.labelHotkey, 0, 2);

            this.textBoxHotkey.ReadOnly = true;
            this.textBoxHotkey.Dock = System.Windows.Forms.DockStyle.Fill;
            mainLayoutPanel.Controls.Add(this.textBoxHotkey, 1, 2);

            this.buttonDetectHotkey.Text = "Detect";
            this.buttonDetectHotkey.Click += new System.EventHandler(this.buttonDetectHotkey_Click);
            mainLayoutPanel.Controls.Add(this.buttonDetectHotkey, 2, 2);

            this.labelStart.Text = "Start Button:";
            this.labelStart.Anchor = System.Windows.Forms.AnchorStyles.Left;
            mainLayoutPanel.Controls.Add(this.labelStart, 0, 3);

            this.textBoxStart.ReadOnly = true;
            this.textBoxStart.Dock = System.Windows.Forms.DockStyle.Fill;
            mainLayoutPanel.Controls.Add(this.textBoxStart, 1, 3);

            this.buttonDetectStart.Text = "Detect";
            this.buttonDetectStart.Click += new System.EventHandler(this.buttonDetectStart_Click);
            mainLayoutPanel.Controls.Add(this.buttonDetectStart, 2, 3);

            var buttonsFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel
            {
                FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight,
                Dock = System.Windows.Forms.DockStyle.Fill,
                AutoSize = true
            };
            this.buttonResetCurrent.Text = "Reset Current";
            this.buttonResetCurrent.Click += new System.EventHandler(this.buttonResetCurrent_Click);
            this.buttonResetAll.Text = "Reset All";
            this.buttonResetAll.Click += new System.EventHandler(this.buttonResetAll_Click);
            this.buttonSave.Text = "Save";
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            buttonsFlowLayoutPanel.Controls.Add(this.buttonResetCurrent);
            buttonsFlowLayoutPanel.Controls.Add(this.buttonResetAll);
            buttonsFlowLayoutPanel.Controls.Add(this.buttonSave);
            buttonsFlowLayoutPanel.Controls.Add(this.buttonCancel);

            mainLayoutPanel.Controls.Add(buttonsFlowLayoutPanel, 0, 4);
            mainLayoutPanel.SetColumnSpan(buttonsFlowLayoutPanel, 3);

            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(500, 250);
            this.Controls.Add(mainLayoutPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MappingConfigurationForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Controller Mapping Configuration";
            mainLayoutPanel.ResumeLayout(false);
            mainLayoutPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxJoysticks;
        private System.Windows.Forms.Label labelJoystick;
        private System.Windows.Forms.Label labelHotkey;
        private System.Windows.Forms.Label labelStart;
        private System.Windows.Forms.TextBox textBoxHotkey;
        private System.Windows.Forms.TextBox textBoxStart;
        private System.Windows.Forms.Button buttonDetectHotkey;
        private System.Windows.Forms.Button buttonDetectStart;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonResetCurrent;
        private System.Windows.Forms.Button buttonResetAll;
    }
}