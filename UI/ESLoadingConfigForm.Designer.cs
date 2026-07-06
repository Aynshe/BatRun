using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.UI
{
    partial class ESLoadingConfigForm
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
            this.labelVideos = new System.Windows.Forms.Label();
            this.comboBoxVideos = new System.Windows.Forms.ComboBox();
            this.checkBoxLoop = new System.Windows.Forms.CheckBox();
            this.checkBoxMuteAfterFirst = new System.Windows.Forms.CheckBox();
            this.checkBoxMuteAll = new System.Windows.Forms.CheckBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // labelVideos
            // 
            this.labelVideos.AutoSize = true;
            this.labelVideos.ForeColor = System.Drawing.Color.White;
            this.labelVideos.Location = new System.Drawing.Point(10, 10);
            this.labelVideos.Name = "labelVideos";
            this.labelVideos.Size = new System.Drawing.Size(95, 15);
            this.labelVideos.TabIndex = 0;
            this.labelVideos.Text = "Available videos:";
            // 
            // comboBoxVideos
            // 
            this.comboBoxVideos.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.comboBoxVideos.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxVideos.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBoxVideos.ForeColor = System.Drawing.Color.White;
            this.comboBoxVideos.FormattingEnabled = true;
            this.comboBoxVideos.Location = new System.Drawing.Point(10, 30);
            this.comboBoxVideos.Name = "comboBoxVideos";
            this.comboBoxVideos.Size = new System.Drawing.Size(415, 23);
            this.comboBoxVideos.TabIndex = 1;
            // 
            // checkBoxLoop
            // 
            this.checkBoxLoop.AutoSize = true;
            this.checkBoxLoop.ForeColor = System.Drawing.Color.White;
            this.checkBoxLoop.Location = new System.Drawing.Point(10, 70);
            this.checkBoxLoop.Name = "checkBoxLoop";
            this.checkBoxLoop.Size = new System.Drawing.Size(104, 19);
            this.checkBoxLoop.TabIndex = 2;
            this.checkBoxLoop.Text = "Loop playback";
            this.checkBoxLoop.UseVisualStyleBackColor = true;
            this.checkBoxLoop.CheckedChanged += new System.EventHandler(this.CheckBoxLoop_CheckedChanged);
            // 
            // checkBoxMuteAfterFirst
            // 
            this.checkBoxMuteAfterFirst.AutoSize = true;
            this.checkBoxMuteAfterFirst.Enabled = false;
            this.checkBoxMuteAfterFirst.ForeColor = System.Drawing.Color.White;
            this.checkBoxMuteAfterFirst.Location = new System.Drawing.Point(10, 95);
            this.checkBoxMuteAfterFirst.Name = "checkBoxMuteAfterFirst";
            this.checkBoxMuteAfterFirst.Size = new System.Drawing.Size(176, 19);
            this.checkBoxMuteAfterFirst.TabIndex = 3;
            this.checkBoxMuteAfterFirst.Text = "Mute after first playback";
            this.checkBoxMuteAfterFirst.UseVisualStyleBackColor = true;
            // 
            // checkBoxMuteAll
            // 
            this.checkBoxMuteAll.AutoSize = true;
            this.checkBoxMuteAll.ForeColor = System.Drawing.Color.White;
            this.checkBoxMuteAll.Location = new System.Drawing.Point(10, 120);
            this.checkBoxMuteAll.Name = "checkBoxMuteAll";
            this.checkBoxMuteAll.Size = new System.Drawing.Size(103, 19);
            this.checkBoxMuteAll.TabIndex = 4;
            this.checkBoxMuteAll.Text = "Mute all audio";
            this.checkBoxMuteAll.UseVisualStyleBackColor = true;
            // 
            // buttonOK
            // 
            this.buttonOK.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonOK.ForeColor = System.Drawing.Color.White;
            this.buttonOK.Location = new System.Drawing.Point(220, 230);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(100, 30);
            this.buttonOK.TabIndex = 5;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = false;
            // 
            // buttonCancel
            // 
            this.buttonCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonCancel.ForeColor = System.Drawing.Color.White;
            this.buttonCancel.Location = new System.Drawing.Point(330, 230);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(100, 30);
            this.buttonCancel.TabIndex = 6;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = false;
            // 
            // ESLoadingConfigForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.ClientSize = new System.Drawing.Size(450, 300);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.checkBoxMuteAll);
            this.Controls.Add(this.checkBoxMuteAfterFirst);
            this.Controls.Add(this.checkBoxLoop);
            this.Controls.Add(this.comboBoxVideos);
            this.Controls.Add(this.labelVideos);
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ESLoadingConfigForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "MediaPlayer Settings";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelVideos;
        private System.Windows.Forms.ComboBox comboBoxVideos;
        private System.Windows.Forms.CheckBox checkBoxLoop;
        private System.Windows.Forms.CheckBox checkBoxMuteAfterFirst;
        private System.Windows.Forms.CheckBox checkBoxMuteAll;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
    }
}


