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
            this.checkBoxMinimizeWindows = new System.Windows.Forms.CheckBox();
            this.checkBoxStartWithWindows = new System.Windows.Forms.CheckBox();
            
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
            this.groupBoxFocus.Size = new System.Drawing.Size(360, 100);
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
            this.numericFocusDuration.Location = new System.Drawing.Point(110, 23);
            this.numericFocusDuration.Maximum = new decimal(new int[] { 60000, 0, 0, 0 });
            this.numericFocusDuration.Minimum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numericFocusDuration.Name = "numericFocusDuration";
            this.numericFocusDuration.Size = new System.Drawing.Size(120, 23);
            this.numericFocusDuration.TabIndex = 2;
            this.numericFocusDuration.Value = new decimal(new int[] { 15000, 0, 0, 0 });

            // numericFocusInterval
            this.numericFocusInterval.Location = new System.Drawing.Point(110, 58);
            this.numericFocusInterval.Maximum = new decimal(new int[] { 60000, 0, 0, 0 });
            this.numericFocusInterval.Minimum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numericFocusInterval.Name = "numericFocusInterval";
            this.numericFocusInterval.Size = new System.Drawing.Size(120, 23);
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
            this.groupBoxWindows.Controls.Add(this.checkBoxStartWithWindows);
            this.groupBoxWindows.Controls.Add(this.checkBoxMinimizeWindows);
            this.groupBoxWindows.Location = new System.Drawing.Point(12, 118);
            this.groupBoxWindows.Name = "groupBoxWindows";
            this.groupBoxWindows.Size = new System.Drawing.Size(360, 85);
            this.groupBoxWindows.TabIndex = 1;
            this.groupBoxWindows.TabStop = false;
            this.groupBoxWindows.Text = "Windows Settings";

            // checkBoxMinimizeWindows
            this.checkBoxMinimizeWindows.AutoSize = true;
            this.checkBoxMinimizeWindows.Location = new System.Drawing.Point(15, 25);
            this.checkBoxMinimizeWindows.Name = "checkBoxMinimizeWindows";
            this.checkBoxMinimizeWindows.Size = new System.Drawing.Size(180, 19);
            this.checkBoxMinimizeWindows.TabIndex = 0;
            this.checkBoxMinimizeWindows.Text = "Minimize active windows on launch";

            // checkBoxStartWithWindows
            this.checkBoxStartWithWindows.AutoSize = true;
            this.checkBoxStartWithWindows.Location = new System.Drawing.Point(15, 50);
            this.checkBoxStartWithWindows.Name = "checkBoxStartWithWindows";
            this.checkBoxStartWithWindows.Size = new System.Drawing.Size(180, 19);
            this.checkBoxStartWithWindows.TabIndex = 1;
            this.checkBoxStartWithWindows.Text = "Start with Windows";

            // buttonSave
            this.buttonSave.Location = new System.Drawing.Point(216, 209);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(75, 23);
            this.buttonSave.TabIndex = 2;
            this.buttonSave.Text = "Save";
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);

            // buttonCancel
            this.buttonCancel.Location = new System.Drawing.Point(297, 209);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 3;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);

            // ConfigurationForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 244);
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
        private System.Windows.Forms.CheckBox checkBoxMinimizeWindows;
        private System.Windows.Forms.CheckBox checkBoxStartWithWindows;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.Button buttonCancel;
    }
} 