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

            //
            // mainLayoutPanel
            //
            var mainLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            mainLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            mainLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            mainLayoutPanel.ColumnCount = 1;
            mainLayoutPanel.RowCount = 4;
            mainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            mainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            mainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            mainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            mainLayoutPanel.Padding = new System.Windows.Forms.Padding(10);


            // groupBoxFocus
            this.groupBoxFocus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericFocusDuration)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericFocusInterval)).BeginInit();

            var focusLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            focusLayoutPanel.SuspendLayout();
            focusLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            focusLayoutPanel.ColumnCount = 3;
            focusLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            focusLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            focusLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            focusLayoutPanel.RowCount = 2;
            focusLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            focusLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            
            this.groupBoxFocus.Controls.Add(focusLayoutPanel);
            this.groupBoxFocus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxFocus.AutoSize = true;
            this.groupBoxFocus.Name = "groupBoxFocus";
            this.groupBoxFocus.TabIndex = 0;
            this.groupBoxFocus.TabStop = false;
            this.groupBoxFocus.Text = "Focus Settings";

            // labelFocusDuration
            this.labelFocusDuration.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelFocusDuration.AutoSize = true;
            this.labelFocusDuration.Name = "labelFocusDuration";
            this.labelFocusDuration.TabIndex = 0;
            this.labelFocusDuration.Text = "Focus Duration:";
            focusLayoutPanel.Controls.Add(this.labelFocusDuration, 0, 0);

            // numericFocusDuration
            this.numericFocusDuration.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericFocusDuration.Maximum = new decimal(new int[] { 60000, 0, 0, 0 });
            this.numericFocusDuration.Minimum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numericFocusDuration.Name = "numericFocusDuration";
            this.numericFocusDuration.TabIndex = 2;
            this.numericFocusDuration.Value = new decimal(new int[] { 15000, 0, 0, 0 });
            focusLayoutPanel.Controls.Add(this.numericFocusDuration, 1, 0);

            // labelDurationMs
            this.labelDurationMs.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelDurationMs.AutoSize = true;
            this.labelDurationMs.Name = "labelDurationMs";
            this.labelDurationMs.TabIndex = 4;
            this.labelDurationMs.Text = "ms";
            focusLayoutPanel.Controls.Add(this.labelDurationMs, 2, 0);

            // labelFocusInterval
            this.labelFocusInterval.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelFocusInterval.AutoSize = true;
            this.labelFocusInterval.Name = "labelFocusInterval";
            this.labelFocusInterval.TabIndex = 1;
            this.labelFocusInterval.Text = "Focus Interval:";
            focusLayoutPanel.Controls.Add(this.labelFocusInterval, 0, 1);

            // numericFocusInterval
            this.numericFocusInterval.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericFocusInterval.Maximum = new decimal(new int[] { 60000, 0, 0, 0 });
            this.numericFocusInterval.Minimum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numericFocusInterval.Name = "numericFocusInterval";
            this.numericFocusInterval.TabIndex = 3;
            this.numericFocusInterval.Value = new decimal(new int[] { 5000, 0, 0, 0 });
            focusLayoutPanel.Controls.Add(this.numericFocusInterval, 1, 1);

            // labelIntervalMs
            this.labelIntervalMs.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelIntervalMs.AutoSize = true;
            this.labelIntervalMs.Name = "labelIntervalMs";
            this.labelIntervalMs.TabIndex = 5;
            this.labelIntervalMs.Text = "ms";
            focusLayoutPanel.Controls.Add(this.labelIntervalMs, 2, 1);

            // groupBoxWindows
            var windowsLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            windowsLayoutPanel.SuspendLayout();
            windowsLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            windowsLayoutPanel.ColumnCount = 2;
            windowsLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            windowsLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            windowsLayoutPanel.RowCount = 7;
            windowsLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            windowsLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            windowsLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            windowsLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            windowsLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            windowsLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            windowsLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.groupBoxWindows.Controls.Add(windowsLayoutPanel);
            this.groupBoxWindows.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxWindows.AutoSize = true;
            this.groupBoxWindows.Name = "groupBoxWindows";
            this.groupBoxWindows.TabIndex = 1;
            this.groupBoxWindows.TabStop = false;
            this.groupBoxWindows.Text = "Windows Settings";

            // labelStartupMethod
            this.labelStartupMethod.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelStartupMethod.AutoSize = true;
            this.labelStartupMethod.Name = "labelStartupMethod";
            this.labelStartupMethod.TabIndex = 5;
            this.labelStartupMethod.Text = "Start with Windows:";
            windowsLayoutPanel.Controls.Add(this.labelStartupMethod, 0, 0);

            // comboBoxStartupMethod
            this.comboBoxStartupMethod.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxStartupMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxStartupMethod.FormattingEnabled = true;
            this.comboBoxStartupMethod.Name = "comboBoxStartupMethod";
            this.comboBoxStartupMethod.TabIndex = 6;
            this.comboBoxStartupMethod.Items.AddRange(new object[] {
                LocalizedStrings.GetString("Disabled"),
                LocalizedStrings.GetString("Shortcut"),
                LocalizedStrings.GetString("Registry")
            });
            this.comboBoxStartupMethod.SelectedIndex = 0;
            windowsLayoutPanel.Controls.Add(this.comboBoxStartupMethod, 1, 0);

            // checkBoxMinimizeWindows
            this.checkBoxMinimizeWindows.AutoSize = true;
            windowsLayoutPanel.SetColumnSpan(this.checkBoxMinimizeWindows, 2);
            this.checkBoxMinimizeWindows.Name = "checkBoxMinimizeWindows";
            this.checkBoxMinimizeWindows.TabIndex = 0;
            this.checkBoxMinimizeWindows.Text = "Minimize active windows on launch";
            windowsLayoutPanel.Controls.Add(this.checkBoxMinimizeWindows, 0, 1);

            // checkBoxEnableVibration
            this.checkBoxEnableVibration.AutoSize = true;
            windowsLayoutPanel.SetColumnSpan(this.checkBoxEnableVibration, 2);
            this.checkBoxEnableVibration.Name = "checkBoxEnableVibration";
            this.checkBoxEnableVibration.TabIndex = 7;
            this.checkBoxEnableVibration.Text = "Enable controller vibration";
            windowsLayoutPanel.Controls.Add(this.checkBoxEnableVibration, 0, 2);

            // checkBoxEnableLogging
            this.checkBoxEnableLogging.AutoSize = true;
            windowsLayoutPanel.SetColumnSpan(this.checkBoxEnableLogging, 2);
            this.checkBoxEnableLogging.Name = "checkBoxEnableLogging";
            this.checkBoxEnableLogging.TabIndex = 2;
            this.checkBoxEnableLogging.Text = "Enable logging (requires restart)";
            windowsLayoutPanel.Controls.Add(this.checkBoxEnableLogging, 0, 3);

            // checkBoxHideESLoading
            this.checkBoxHideESLoading.AutoSize = true;
            windowsLayoutPanel.SetColumnSpan(this.checkBoxHideESLoading, 2);
            this.checkBoxHideESLoading.Name = "checkBoxHideESLoading";
            this.checkBoxHideESLoading.TabIndex = 8;
            this.checkBoxHideESLoading.Text = "Hide ES during loading";
            windowsLayoutPanel.Controls.Add(this.checkBoxHideESLoading, 0, 4);

            // checkBoxShowSplashScreen
            this.checkBoxShowSplashScreen.AutoSize = true;
            windowsLayoutPanel.SetColumnSpan(this.checkBoxShowSplashScreen, 2);
            this.checkBoxShowSplashScreen.Name = "checkBoxShowSplashScreen";
            this.checkBoxShowSplashScreen.TabIndex = 9;
            this.checkBoxShowSplashScreen.Text = "Show splash screen on startup";
            windowsLayoutPanel.Controls.Add(this.checkBoxShowSplashScreen, 0, 5);

            // checkBoxShowHotkeySplash
            this.checkBoxShowHotkeySplash.AutoSize = true;
            windowsLayoutPanel.SetColumnSpan(this.checkBoxShowHotkeySplash, 2);
            this.checkBoxShowHotkeySplash.Name = "checkBoxShowHotkeySplash";
            this.checkBoxShowHotkeySplash.TabIndex = 10;
            this.checkBoxShowHotkeySplash.Text = "Show RetroBat splash screen";
            windowsLayoutPanel.Controls.Add(this.checkBoxShowHotkeySplash, 0, 6);

            // buttonSave and buttonCancel
            var buttonsLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            buttonsLayoutPanel.SuspendLayout();
            buttonsLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            buttonsLayoutPanel.ColumnCount = 3;
            buttonsLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            buttonsLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            buttonsLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            buttonsLayoutPanel.RowCount = 1;
            buttonsLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));

            this.buttonSave.Name = "buttonSave";
            this.buttonSave.TabIndex = 2;
            this.buttonSave.Text = "Save";
            this.buttonSave.Click += new System.EventHandler(this.ButtonSave_Click);
            buttonsLayoutPanel.Controls.Add(this.buttonSave, 1, 0);

            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.TabIndex = 3;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.Click += new System.EventHandler(this.ButtonCancel_Click);
            buttonsLayoutPanel.Controls.Add(this.buttonCancel, 2, 0);

            // Add all to main layout panel
            mainLayoutPanel.Controls.Add(this.groupBoxFocus, 0, 0);
            mainLayoutPanel.Controls.Add(this.groupBoxWindows, 0, 1);
            mainLayoutPanel.Controls.Add(buttonsLayoutPanel, 0, 3);

            // ConfigurationForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(550, 750);
            this.Controls.Add(mainLayoutPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfigurationForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "BatRun Configuration";
            
            this.groupBoxFocus.ResumeLayout(false);
            this.groupBoxFocus.PerformLayout();
            focusLayoutPanel.ResumeLayout(false);
            focusLayoutPanel.PerformLayout();
            this.groupBoxWindows.ResumeLayout(false);
            this.groupBoxWindows.PerformLayout();
            windowsLayoutPanel.ResumeLayout(false);
            windowsLayoutPanel.PerformLayout();
            buttonsLayoutPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numericFocusDuration)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericFocusInterval)).EndInit();
            mainLayoutPanel.ResumeLayout(false);
            mainLayoutPanel.PerformLayout();
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