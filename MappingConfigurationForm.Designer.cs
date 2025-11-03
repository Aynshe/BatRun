namespace BatRun
{
    partial class MappingConfigurationForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
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
            //
            // comboBoxJoysticks
            //
            this.comboBoxJoysticks.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxJoysticks.FormattingEnabled = true;
            this.comboBoxJoysticks.Location = new System.Drawing.Point(12, 32);
            this.comboBoxJoysticks.Name = "comboBoxJoysticks";
            this.comboBoxJoysticks.Size = new System.Drawing.Size(360, 23);
            this.comboBoxJoysticks.TabIndex = 0;
            this.comboBoxJoysticks.SelectedIndexChanged += new System.EventHandler(this.comboBoxJoysticks_SelectedIndexChanged);
            //
            // labelJoystick
            //
            this.labelJoystick.AutoSize = true;
            this.labelJoystick.Location = new System.Drawing.Point(12, 14);
            this.labelJoystick.Name = "labelJoystick";
            this.labelJoystick.Size = new System.Drawing.Size(92, 15);
            this.labelJoystick.TabIndex = 1;
            this.labelJoystick.Text = "Select Controller:";
            //
            // labelHotkey
            //
            this.labelHotkey.AutoSize = true;
            this.labelHotkey.Location = new System.Drawing.Point(12, 68);
            this.labelHotkey.Name = "labelHotkey";
            this.labelHotkey.Size = new System.Drawing.Size(85, 15);
            this.labelHotkey.TabIndex = 2;
            this.labelHotkey.Text = "Hotkey Button:";
            //
            // labelStart
            //
            this.labelStart.AutoSize = true;
            this.labelStart.Location = new System.Drawing.Point(12, 97);
            this.labelStart.Name = "labelStart";
            this.labelStart.Size = new System.Drawing.Size(71, 15);
            this.labelStart.TabIndex = 3;
            this.labelStart.Text = "Start Button:";
            //
            // textBoxHotkey
            //
            this.textBoxHotkey.Location = new System.Drawing.Point(103, 65);
            this.textBoxHotkey.Name = "textBoxHotkey";
            this.textBoxHotkey.ReadOnly = true;
            this.textBoxHotkey.Size = new System.Drawing.Size(188, 23);
            this.textBoxHotkey.TabIndex = 4;
            //
            // textBoxStart
            //
            this.textBoxStart.Location = new System.Drawing.Point(103, 94);
            this.textBoxStart.Name = "textBoxStart";
            this.textBoxStart.ReadOnly = true;
            this.textBoxStart.Size = new System.Drawing.Size(188, 23);
            this.textBoxStart.TabIndex = 5;
            //
            // buttonDetectHotkey
            //
            this.buttonDetectHotkey.Location = new System.Drawing.Point(297, 64);
            this.buttonDetectHotkey.Name = "buttonDetectHotkey";
            this.buttonDetectHotkey.Size = new System.Drawing.Size(75, 23);
            this.buttonDetectHotkey.TabIndex = 6;
            this.buttonDetectHotkey.Text = "Detect";
            this.buttonDetectHotkey.UseVisualStyleBackColor = true;
            this.buttonDetectHotkey.Click += new System.EventHandler(this.buttonDetectHotkey_Click);
            //
            // buttonDetectStart
            //
            this.buttonDetectStart.Location = new System.Drawing.Point(297, 93);
            this.buttonDetectStart.Name = "buttonDetectStart";
            this.buttonDetectStart.Size = new System.Drawing.Size(75, 23);
            this.buttonDetectStart.TabIndex = 7;
            this.buttonDetectStart.Text = "Detect";
            this.buttonDetectStart.UseVisualStyleBackColor = true;
            this.buttonDetectStart.Click += new System.EventHandler(this.buttonDetectStart_Click);
            //
            // buttonSave
            //
            this.buttonSave.Location = new System.Drawing.Point(216, 132);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(75, 23);
            this.buttonSave.TabIndex = 8;
            this.buttonSave.Text = "Save";
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            //
            // buttonCancel
            //
            this.buttonCancel.Location = new System.Drawing.Point(297, 132);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 9;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            //
            // buttonResetCurrent
            //
            this.buttonResetCurrent.Location = new System.Drawing.Point(12, 132);
            this.buttonResetCurrent.Name = "buttonResetCurrent";
            this.buttonResetCurrent.Size = new System.Drawing.Size(95, 23);
            this.buttonResetCurrent.TabIndex = 10;
            this.buttonResetCurrent.Text = "Reset Current";
            this.buttonResetCurrent.UseVisualStyleBackColor = true;
            this.buttonResetCurrent.Click += new System.EventHandler(this.buttonResetCurrent_Click);
            //
            // buttonResetAll
            //
            this.buttonResetAll.Location = new System.Drawing.Point(113, 132);
            this.buttonResetAll.Name = "buttonResetAll";
            this.buttonResetAll.Size = new System.Drawing.Size(95, 23);
            this.buttonResetAll.TabIndex = 11;
            this.buttonResetAll.Text = "Reset All";
            this.buttonResetAll.UseVisualStyleBackColor = true;
            this.buttonResetAll.Click += new System.EventHandler(this.buttonResetAll_Click);
            //
            // MappingConfigurationForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 167);
            this.Controls.Add(this.buttonResetAll);
            this.Controls.Add(this.buttonResetCurrent);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonSave);
            this.Controls.Add(this.buttonDetectStart);
            this.Controls.Add(this.buttonDetectHotkey);
            this.Controls.Add(this.textBoxStart);
            this.Controls.Add(this.textBoxHotkey);
            this.Controls.Add(this.labelStart);
            this.Controls.Add(this.labelHotkey);
            this.Controls.Add(this.labelJoystick);
            this.Controls.Add(this.comboBoxJoysticks);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MappingConfigurationForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Controller Mapping Configuration";
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