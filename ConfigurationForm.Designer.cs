namespace BatRun
{
    partial class ConfigurationForm
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
            this.components = new System.ComponentModel.Container();
            this.groupBoxFocus = new System.Windows.Forms.GroupBox();
            this.labelFocusDuration = new System.Windows.Forms.Label();
            this.labelFocusInterval = new System.Windows.Forms.Label();
            this.numericFocusDuration = new System.Windows.Forms.NumericUpDown();
            this.numericFocusInterval = new System.Windows.Forms.NumericUpDown();
            this.labelDurationMs = new System.Windows.Forms.Label();
            this.labelIntervalMs = new System.Windows.Forms.Label();
            
            this.groupBoxWindows = new System.Windows.Forms.GroupBox();
            this.comboBoxStartupMethod = new System.Windows.Forms.ComboBox();
            this.labelStartupMethod = new System.Windows.Forms.Label();
            this.checkBoxEnableVibration = new System.Windows.Forms.CheckBox();
            this.checkBoxMinimizeWindows = new System.Windows.Forms.CheckBox();
            this.checkBoxEnableLogging = new System.Windows.Forms.CheckBox();
            this.checkBoxHideESLoading = new System.Windows.Forms.CheckBox();
            this.checkBoxShowSplashScreen = new System.Windows.Forms.CheckBox();
            this.checkBoxShowHotkeySplash = new System.Windows.Forms.CheckBox();
            
            this.buttonSave = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();

            // groupBoxFocus
            this.groupBoxFocus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericFocusDuration)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericFocusInterval)).BeginInit();
            
            this.groupBoxFocus.Controls.Add(this.labelDurationMs);
            this.groupBoxFocus.Controls.Add(this.labelIntervalMs);
            this.groupBoxFocus.Controls.Add(this.numericFocusInterval);
            this.groupBoxFocus.Controls.Add(this.numericFocusDuration);
            this.groupBoxFocus.Controls.Add(this.labelFocusInterval);
            this.groupBoxFocus.Controls.Add(this.labelFocusDuration);
            this.groupBoxFocus.Location = new System.Drawing.Point(12, 12);
            this.groupBoxFocus.Name = "groupBoxFocus";
            this.groupBoxFocus.Size = new System.Drawing.Size(380, 100);
            this.groupBoxFocus.TabIndex = 0;
            this.groupBoxFocus.TabStop = false;
            this.groupBoxFocus.Text = "Focus Settings";

            // labelFocusDuration
            this.labelFocusDuration.AutoSize = true;
            this.labelFocusDuration.Location = new System.Drawing.Point(15, 25);
            this.labelFocusDuration.Name = "labelFocusDuration";
            this.labelFocusDuration.Size = new System.Drawing.Size(89, 15);
            this.labelFocusDuration.TabIndex = 0;
            this.labelFocusDuration.Text = "Focus Duration:";

            // labelFocusInterval
            this.labelFocusInterval.AutoSize = true;
            this.labelFocusInterval.Location = new System.Drawing.Point(15, 60);
            this.labelFocusInterval.Name = "labelFocusInterval";
            this.labelFocusInterval.Size = new System.Drawing.Size(84, 15);
            this.labelFocusInterval.TabIndex = 1;
            this.labelFocusInterval.Text = "Focus Interval:";

            // numericFocusDuration
            this.numericFocusDuration.Location = new System.Drawing.Point(135, 23);
            this.numericFocusDuration.Maximum = new decimal(new int[] { 60000, 0, 0, 0 });
            this.numericFocusDuration.Minimum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numericFocusDuration.Name = "numericFocusDuration";
            this.numericFocusDuration.Size = new System.Drawing.Size(100, 23);
            this.numericFocusDuration.TabIndex = 2;
            this.numericFocusDuration.Value = new decimal(new int[] { 15000, 0, 0, 0 });

            // numericFocusInterval
            this.numericFocusInterval.Location = new System.Drawing.Point(135, 58);
            this.numericFocusInterval.Maximum = new decimal(new int[] { 60000, 0, 0, 0 });
            this.numericFocusInterval.Minimum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numericFocusInterval.Name = "numericFocusInterval";
            this.numericFocusInterval.Size = new System.Drawing.Size(100, 23);
            this.numericFocusInterval.TabIndex = 3;
            this.numericFocusInterval.Value = new decimal(new int[] { 5000, 0, 0, 0 });

            // labelDurationMs
            this.labelDurationMs.AutoSize = true;
            this.labelDurationMs.Location = new System.Drawing.Point(236, 25);
            this.labelDurationMs.Name = "labelDurationMs";
            this.labelDurationMs.Size = new System.Drawing.Size(23, 15);
            this.labelDurationMs.TabIndex = 4;
            this.labelDurationMs.Text = "ms";

            // labelIntervalMs
            this.labelIntervalMs.AutoSize = true;
            this.labelIntervalMs.Location = new System.Drawing.Point(236, 60);
            this.labelIntervalMs.Name = "labelIntervalMs";
            this.labelIntervalMs.Size = new System.Drawing.Size(23, 15);
            this.labelIntervalMs.TabIndex = 5;
            this.labelIntervalMs.Text = "ms";

            // groupBoxWindows
            this.groupBoxWindows.Controls.Add(this.comboBoxStartupMethod);
            this.groupBoxWindows.Controls.Add(this.labelStartupMethod);
            this.groupBoxWindows.Controls.Add(this.checkBoxMinimizeWindows);
            this.groupBoxWindows.Controls.Add(this.checkBoxEnableVibration);
            this.groupBoxWindows.Controls.Add(this.checkBoxEnableLogging);
            this.groupBoxWindows.Controls.Add(this.checkBoxHideESLoading);
            this.groupBoxWindows.Controls.Add(this.checkBoxShowSplashScreen);
            this.groupBoxWindows.Controls.Add(this.checkBoxShowHotkeySplash);
            this.groupBoxWindows.Location = new System.Drawing.Point(12, 118);
            this.groupBoxWindows.Name = "groupBoxWindows";
            this.groupBoxWindows.Size = new System.Drawing.Size(380, 215);
            this.groupBoxWindows.TabIndex = 1;
            this.groupBoxWindows.TabStop = false;
            this.groupBoxWindows.Text = "Windows Settings";

            // labelStartupMethod
            this.labelStartupMethod.AutoSize = true;
            this.labelStartupMethod.Location = new System.Drawing.Point(15, 25);
            this.labelStartupMethod.Name = "labelStartupMethod";
            this.labelStartupMethod.Size = new System.Drawing.Size(120, 15);
            this.labelStartupMethod.TabIndex = 5;
            this.labelStartupMethod.Text = "Start with Windows:";

            // comboBoxStartupMethod
            this.comboBoxStartupMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxStartupMethod.FormattingEnabled = true;
            this.comboBoxStartupMethod.Location = new System.Drawing.Point(160, 22);
            this.comboBoxStartupMethod.Name = "comboBoxStartupMethod";
            this.comboBoxStartupMethod.Size = new System.Drawing.Size(190, 23);
            this.comboBoxStartupMethod.TabIndex = 6;
            this.comboBoxStartupMethod.Items.AddRange(new object[] {
                LocalizedStrings.GetString("Disabled"),
                LocalizedStrings.GetString("Shortcut"),
                LocalizedStrings.GetString("Registry")
            });
            this.comboBoxStartupMethod.SelectedIndex = 0;

            // checkBoxMinimizeWindows
            this.checkBoxMinimizeWindows.AutoSize = true;
            this.checkBoxMinimizeWindows.Location = new System.Drawing.Point(15, 50);
            this.checkBoxMinimizeWindows.Name = "checkBoxMinimizeWindows";
            this.checkBoxMinimizeWindows.Size = new System.Drawing.Size(180, 19);
            this.checkBoxMinimizeWindows.TabIndex = 0;
            this.checkBoxMinimizeWindows.Text = "Minimize active windows on launch";

            // checkBoxEnableVibration
            this.checkBoxEnableVibration.AutoSize = true;
            this.checkBoxEnableVibration.Location = new System.Drawing.Point(15, 75);
            this.checkBoxEnableVibration.Name = "checkBoxEnableVibration";
            this.checkBoxEnableVibration.Size = new System.Drawing.Size(180, 19);
            this.checkBoxEnableVibration.TabIndex = 7;
            this.checkBoxEnableVibration.Text = "Enable controller vibration";

            // checkBoxEnableLogging
            this.checkBoxEnableLogging.AutoSize = true;
            this.checkBoxEnableLogging.Location = new System.Drawing.Point(15, 100);
            this.checkBoxEnableLogging.Name = "checkBoxEnableLogging";
            this.checkBoxEnableLogging.Size = new System.Drawing.Size(180, 19);
            this.checkBoxEnableLogging.TabIndex = 2;
            this.checkBoxEnableLogging.Text = "Enable logging (requires restart)";

            // checkBoxHideESLoading
            this.checkBoxHideESLoading.AutoSize = true;
            this.checkBoxHideESLoading.Location = new System.Drawing.Point(15, 125);
            this.checkBoxHideESLoading.Name = "checkBoxHideESLoading";
            this.checkBoxHideESLoading.Size = new System.Drawing.Size(180, 19);
            this.checkBoxHideESLoading.TabIndex = 8;
            this.checkBoxHideESLoading.Text = "Hide ES during loading";

            // checkBoxShowSplashScreen
            this.checkBoxShowSplashScreen.AutoSize = true;
            this.checkBoxShowSplashScreen.Location = new System.Drawing.Point(15, 150);
            this.checkBoxShowSplashScreen.Name = "checkBoxShowSplashScreen";
            this.checkBoxShowSplashScreen.Size = new System.Drawing.Size(180, 19);
            this.checkBoxShowSplashScreen.TabIndex = 9;
            this.checkBoxShowSplashScreen.Text = "Show splash screen on startup";

            // checkBoxShowHotkeySplash
            this.checkBoxShowHotkeySplash.AutoSize = true;
            this.checkBoxShowHotkeySplash.Location = new System.Drawing.Point(15, 175);
            this.checkBoxShowHotkeySplash.Name = "checkBoxShowHotkeySplash";
            this.checkBoxShowHotkeySplash.Size = new System.Drawing.Size(180, 19);
            this.checkBoxShowHotkeySplash.TabIndex = 10;
            this.checkBoxShowHotkeySplash.Text = "Show RetroBat splash screen";

            // buttonSave
            this.buttonSave.Location = new System.Drawing.Point(236, 265);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(75, 23);
            this.buttonSave.TabIndex = 2;
            this.buttonSave.Text = "Save";
            this.buttonSave.Click += new System.EventHandler(this.ButtonSave_Click);

            // buttonCancel
            this.buttonCancel.Location = new System.Drawing.Point(317, 265);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 3;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.Click += new System.EventHandler(this.ButtonCancel_Click);

            // ConfigurationForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(405, 320);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonSave);
            this.Controls.Add(this.groupBoxWindows);
            this.Controls.Add(this.groupBoxFocus);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfigurationForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "BatRun Configuration";
            
            this.groupBoxFocus.ResumeLayout(false);
            this.groupBoxFocus.PerformLayout();
            this.groupBoxWindows.ResumeLayout(false);
            this.groupBoxWindows.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericFocusDuration)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericFocusInterval)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxFocus;
        private System.Windows.Forms.Label labelFocusDuration;
        private System.Windows.Forms.Label labelFocusInterval;
        private System.Windows.Forms.NumericUpDown numericFocusDuration;
        private System.Windows.Forms.NumericUpDown numericFocusInterval;
        private System.Windows.Forms.Label labelDurationMs;
        private System.Windows.Forms.Label labelIntervalMs;
        private System.Windows.Forms.GroupBox groupBoxWindows;
        private System.Windows.Forms.ComboBox comboBoxStartupMethod;
        private System.Windows.Forms.Label labelStartupMethod;
        private System.Windows.Forms.CheckBox checkBoxEnableVibration;
        private System.Windows.Forms.CheckBox checkBoxMinimizeWindows;
        private System.Windows.Forms.CheckBox checkBoxEnableLogging;
        private System.Windows.Forms.CheckBox checkBoxHideESLoading;
        private System.Windows.Forms.CheckBox checkBoxShowSplashScreen;
        private System.Windows.Forms.CheckBox checkBoxShowHotkeySplash;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.Button buttonCancel;
    }
}