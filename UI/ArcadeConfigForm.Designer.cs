using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.UI
{
    partial class ArcadeConfigForm
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
            this.components = new System.ComponentModel.Container();

            // EN: Instantiate all controls.
            // FR: Instanciation de tous les contrôles.
            this.chkEnabled         = new System.Windows.Forms.CheckBox();
            this.picArcadeLogo      = new System.Windows.Forms.PictureBox();
            this.lblArcadeInfo      = new System.Windows.Forms.Label();
            this.lblCoinDevice      = new System.Windows.Forms.Label();
            this.cmbDevices         = new System.Windows.Forms.ComboBox();
            this.lblCoinKey         = new System.Windows.Forms.Label();
            this.txtKeyBinding      = new System.Windows.Forms.TextBox();
            this.btnDetect          = new System.Windows.Forms.Button();
            this.lblMinutesPerCoin  = new System.Windows.Forms.Label();
            this.numMinutes         = new System.Windows.Forms.NumericUpDown();
            this.lblOperatorPwd     = new System.Windows.Forms.Label();
            this.txtPassword        = new System.Windows.Forms.TextBox();
            this.lblOverlayOpacity  = new System.Windows.Forms.Label();
            this.tbOpacity          = new System.Windows.Forms.TrackBar();
            this.lblOpacityValue    = new System.Windows.Forms.Label();
            this.lblApiEnabled      = new System.Windows.Forms.Label();
            this.chkApiEnabled      = new System.Windows.Forms.CheckBox();
            this.lblApiPort         = new System.Windows.Forms.Label();
            this.numApiPort         = new System.Windows.Forms.NumericUpDown();
            this.lblHideOpButtons   = new System.Windows.Forms.Label();
            this.chkHideOpButtons   = new System.Windows.Forms.CheckBox();
            this.lblDefaultMode     = new System.Windows.Forms.Label();
            this.cmbDefaultMode     = new System.Windows.Forms.ComboBox();
            this.lblInitialCredits  = new System.Windows.Forms.Label();
            this.numInitialCredits  = new System.Windows.Forms.NumericUpDown();
            this.lblPublicIp        = new System.Windows.Forms.Label();
            this.txtPublicIp        = new System.Windows.Forms.TextBox();
            this.btnManageKeys      = new System.Windows.Forms.Button();
            this.chkAllowRegistration     = new System.Windows.Forms.CheckBox();
            this.lblAdminIps              = new System.Windows.Forms.Label();
            this.txtAdminIps              = new System.Windows.Forms.TextBox();
            this.chkPublicAccessEnabled   = new System.Windows.Forms.CheckBox();
            this.chkPublicAccessRequiresLogin = new System.Windows.Forms.CheckBox();
            this.chkMoonlightEnabled    = new System.Windows.Forms.CheckBox();
            this.lblMoonlightHostId     = new System.Windows.Forms.Label();
            this.txtMoonlightHostId     = new System.Windows.Forms.TextBox();
            this.lblMoonlightAppId      = new System.Windows.Forms.Label();
            this.txtMoonlightAppId      = new System.Windows.Forms.TextBox();
            this.chkHttpsEnabled        = new System.Windows.Forms.CheckBox();
            this.chkProxyMoonlight      = new System.Windows.Forms.CheckBox();
            this.btnManageKeys      = new System.Windows.Forms.Button();
            this.btnSave            = new System.Windows.Forms.Button();
            this.btnCancel          = new System.Windows.Forms.Button();

            ((System.ComponentModel.ISupportInitialize)(this.numMinutes)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numApiPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInitialCredits)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbOpacity)).BeginInit();
            this.SuspendLayout();

            // ── picArcadeLogo ─────────────────────────────────────────────
            // EN: Small icon placed before the "Enable Arcade Mode" checkbox.
            // FR: Petite icône placée avant la case à cocher « Enable Arcade Mode ».
            this.picArcadeLogo.Location      = new System.Drawing.Point(20, 22);
            this.picArcadeLogo.Size          = new System.Drawing.Size(16, 16);
            this.picArcadeLogo.SizeMode      = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picArcadeLogo.TabIndex      = 0;
            this.picArcadeLogo.TabStop       = false;
            this.picArcadeLogo.Name          = "picArcadeLogo";

            // ── chkEnabled ───────────────────────────────────────────────
            // EN: Shifted right to leave room for picArcadeLogo on the left.
            // FR: Décalée vers la droite pour laisser la place au picArcadeLogo.
            this.chkEnabled.Location = new System.Drawing.Point(42, 20);
            this.chkEnabled.AutoSize = true;
            this.chkEnabled.Text     = "Enable Arcade Mode";
            this.chkEnabled.ForeColor = System.Drawing.Color.White;
            this.chkEnabled.Name     = "chkEnabled";
            this.chkEnabled.TabIndex = 1;

            // ── lblArcadeInfo ────────────────────────────────────────────
            // EN: "?" help icon — clicking it shows the warning bubble.
            // FR: Icône « ? » d'aide — un clic affiche la bulle d'avertissement.
            this.lblArcadeInfo.Location  = new System.Drawing.Point(195, 20);
            this.lblArcadeInfo.AutoSize  = true;
            this.lblArcadeInfo.Font      = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblArcadeInfo.ForeColor = System.Drawing.Color.FromArgb(114, 137, 218);
            this.lblArcadeInfo.Text      = "(?)";
            this.lblArcadeInfo.Name      = "lblArcadeInfo";
            this.lblArcadeInfo.TabIndex  = 2;
            this.lblArcadeInfo.Cursor    = System.Windows.Forms.Cursors.Help;
            // Click event is wired in InitializeRuntimeData to keep designer code minimal.

            // ── lblCoinDevice ────────────────────────────────────────────
            this.lblCoinDevice.Location  = new System.Drawing.Point(20, 60);
            this.lblCoinDevice.AutoSize  = true;
            this.lblCoinDevice.Text      = "Coin Input Device:";
            this.lblCoinDevice.ForeColor = System.Drawing.Color.White;
            this.lblCoinDevice.Name      = "lblCoinDevice";
            this.lblCoinDevice.TabIndex  = 1;

            // ── cmbDevices ───────────────────────────────────────────────
            this.cmbDevices.Location      = new System.Drawing.Point(20, 80);
            this.cmbDevices.Size          = new System.Drawing.Size(340, 23);
            this.cmbDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDevices.Name         = "cmbDevices";
            this.cmbDevices.TabIndex     = 2;

            // ── lblCoinKey ───────────────────────────────────────────────
            this.lblCoinKey.Location  = new System.Drawing.Point(20, 120);
            this.lblCoinKey.AutoSize  = true;
            this.lblCoinKey.Text      = "Coin Key Binding:";
            this.lblCoinKey.ForeColor = System.Drawing.Color.White;
            this.lblCoinKey.Name      = "lblCoinKey";
            this.lblCoinKey.TabIndex  = 3;

            // ── txtKeyBinding ────────────────────────────────────────────
            this.txtKeyBinding.Location  = new System.Drawing.Point(140, 117);
            this.txtKeyBinding.Size      = new System.Drawing.Size(100, 23);
            this.txtKeyBinding.ReadOnly  = true;
            this.txtKeyBinding.Text      = "D5";
            this.txtKeyBinding.Name      = "txtKeyBinding";
            this.txtKeyBinding.TabIndex  = 4;

            // ── btnDetect ────────────────────────────────────────────────
            this.btnDetect.Location                          = new System.Drawing.Point(250, 115);
            this.btnDetect.Size                              = new System.Drawing.Size(75, 27);
            this.btnDetect.Text                              = "Detect";
            this.btnDetect.FlatStyle                         = System.Windows.Forms.FlatStyle.Flat;
            this.btnDetect.BackColor                         = System.Drawing.Color.FromArgb(87, 87, 87);
            this.btnDetect.ForeColor                         = System.Drawing.Color.White;
            this.btnDetect.UseVisualStyleBackColor           = false;
            this.btnDetect.Name                              = "btnDetect";
            this.btnDetect.TabIndex                          = 5;
            this.btnDetect.Click                            += new System.EventHandler(this.BtnDetect_Click);

            // ── lblMinutesPerCoin ────────────────────────────────────────
            this.lblMinutesPerCoin.Location  = new System.Drawing.Point(20, 160);
            this.lblMinutesPerCoin.AutoSize  = true;
            this.lblMinutesPerCoin.Text      = "Minutes per coin:";
            this.lblMinutesPerCoin.ForeColor = System.Drawing.Color.White;
            this.lblMinutesPerCoin.Name      = "lblMinutesPerCoin";
            this.lblMinutesPerCoin.TabIndex  = 6;

            // ── numMinutes ───────────────────────────────────────────────
            this.numMinutes.Location = new System.Drawing.Point(140, 157);
            this.numMinutes.Size     = new System.Drawing.Size(100, 23);
            this.numMinutes.Minimum  = new decimal(new int[] { 1, 0, 0, 0 });
            this.numMinutes.Maximum  = new decimal(new int[] { 60, 0, 0, 0 });
            this.numMinutes.Value    = new decimal(new int[] { 5, 0, 0, 0 });
            this.numMinutes.Name     = "numMinutes";
            this.numMinutes.TabIndex = 7;

            // ── lblOperatorPwd ───────────────────────────────────────────
            this.lblOperatorPwd.Location  = new System.Drawing.Point(20, 200);
            this.lblOperatorPwd.AutoSize  = true;
            this.lblOperatorPwd.Text      = "Operator Password:";
            this.lblOperatorPwd.ForeColor = System.Drawing.Color.White;
            this.lblOperatorPwd.Name      = "lblOperatorPwd";
            this.lblOperatorPwd.TabIndex  = 8;

            // ── txtPassword ──────────────────────────────────────────────
            this.txtPassword.Location     = new System.Drawing.Point(140, 197);
            this.txtPassword.Size         = new System.Drawing.Size(100, 23);
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Name         = "txtPassword";
            this.txtPassword.TabIndex     = 9;

            // ── lblOverlayOpacity ────────────────────────────────────────
            this.lblOverlayOpacity.Location  = new System.Drawing.Point(20, 240);
            this.lblOverlayOpacity.AutoSize  = true;
            this.lblOverlayOpacity.Text      = "Overlay Opacity:";
            this.lblOverlayOpacity.ForeColor = System.Drawing.Color.White;
            this.lblOverlayOpacity.Name      = "lblOverlayOpacity";
            this.lblOverlayOpacity.TabIndex  = 10;

            // ── tbOpacity ────────────────────────────────────────────────
            this.tbOpacity.Location      = new System.Drawing.Point(135, 237);
            this.tbOpacity.Size          = new System.Drawing.Size(180, 45);
            this.tbOpacity.Minimum       = 10;
            this.tbOpacity.Maximum       = 100;
            this.tbOpacity.TickFrequency = 10;
            this.tbOpacity.Value         = 85;
            this.tbOpacity.Name          = "tbOpacity";
            this.tbOpacity.TabIndex      = 11;
            this.tbOpacity.Scroll       += new System.EventHandler(this.TbOpacity_Scroll);

            // ── lblOpacityValue ──────────────────────────────────────────
            this.lblOpacityValue.Location  = new System.Drawing.Point(320, 240);
            this.lblOpacityValue.AutoSize  = true;
            this.lblOpacityValue.Text      = "85%";
            this.lblOpacityValue.ForeColor = System.Drawing.Color.White;
            this.lblOpacityValue.Name      = "lblOpacityValue";
            this.lblOpacityValue.TabIndex  = 12;

            // ── lblApiEnabled ────────────────────────────────────────────
            this.lblApiEnabled.Location  = new System.Drawing.Point(20, 295);
            this.lblApiEnabled.AutoSize  = true;
            this.lblApiEnabled.Text      = "Enable Operator API (Web UI):";
            this.lblApiEnabled.ForeColor = System.Drawing.Color.White;
            this.lblApiEnabled.Name      = "lblApiEnabled";
            this.lblApiEnabled.TabIndex  = 13;

            // ── chkApiEnabled ────────────────────────────────────────────
            this.chkApiEnabled.Location = new System.Drawing.Point(250, 293);
            this.chkApiEnabled.AutoSize = true;
            this.chkApiEnabled.Name     = "chkApiEnabled";
            this.chkApiEnabled.TabIndex = 14;

            // ── lblApiPort ───────────────────────────────────────────────
            this.lblApiPort.Location  = new System.Drawing.Point(20, 330);
            this.lblApiPort.AutoSize  = true;
            this.lblApiPort.Text      = "API Port (Default 4321):";
            this.lblApiPort.ForeColor = System.Drawing.Color.White;
            this.lblApiPort.Name      = "lblApiPort";
            this.lblApiPort.TabIndex  = 15;

            // ── numApiPort ───────────────────────────────────────────────
            this.numApiPort.Location = new System.Drawing.Point(250, 327);
            this.numApiPort.Size     = new System.Drawing.Size(80, 23);
            this.numApiPort.Minimum  = new decimal(new int[] { 80, 0, 0, 0 });
            this.numApiPort.Maximum  = new decimal(new int[] { 65535, 0, 0, 0 });
            this.numApiPort.Value    = new decimal(new int[] { 4321, 0, 0, 0 });
            this.numApiPort.Name     = "numApiPort";
            this.numApiPort.TabIndex = 16;

            // ── lblAdminIps ───────────────────────────────────────────
            this.lblAdminIps.Location  = new System.Drawing.Point(20, 360);
            this.lblAdminIps.AutoSize  = true;
            this.lblAdminIps.Text      = "Admin IPs (External):";
            this.lblAdminIps.ForeColor = System.Drawing.Color.White;
            this.lblAdminIps.Name      = "lblAdminIps";
            this.lblAdminIps.TabIndex  = 34;

            // ── txtAdminIps ───────────────────────────────────────────
            this.txtAdminIps.Location = new System.Drawing.Point(160, 357);
            this.txtAdminIps.Size     = new System.Drawing.Size(200, 23);
            this.txtAdminIps.Name     = "txtAdminIps";
            this.txtAdminIps.TabIndex = 35;

            // ── lblHideOpButtons ─────────────────────────────────────────
            this.lblHideOpButtons.Location  = new System.Drawing.Point(20, 400);
            this.lblHideOpButtons.AutoSize  = true;
            this.lblHideOpButtons.Text      = "Hide Floating Buttons (LOCK/BR):";
            this.lblHideOpButtons.ForeColor = System.Drawing.Color.White;
            this.lblHideOpButtons.Name      = "lblHideOpButtons";
            this.lblHideOpButtons.TabIndex  = 17;

            // ── chkHideOpButtons ─────────────────────────────────────────
            this.chkHideOpButtons.Location = new System.Drawing.Point(250, 398);
            this.chkHideOpButtons.AutoSize = true;
            this.chkHideOpButtons.Name     = "chkHideOpButtons";
            this.chkHideOpButtons.TabIndex = 18;

            // ── chkPublicAccessEnabled ───────────────────────────────────
            this.chkPublicAccessEnabled.Location = new System.Drawing.Point(20, 435);
            this.chkPublicAccessEnabled.AutoSize = true;
            this.chkPublicAccessEnabled.Text     = "Enable Public Access Dashboard";
            this.chkPublicAccessEnabled.ForeColor = System.Drawing.Color.White;
            this.chkPublicAccessEnabled.Name     = "chkPublicAccessEnabled";
            this.chkPublicAccessEnabled.TabIndex = 19;

            // ── chkPublicAccessRequiresLogin ─────────────────────────────
            this.chkPublicAccessRequiresLogin.Location = new System.Drawing.Point(235, 435);
            this.chkPublicAccessRequiresLogin.AutoSize = true;
            this.chkPublicAccessRequiresLogin.Text     = "Requires Login";
            this.chkPublicAccessRequiresLogin.ForeColor = System.Drawing.Color.White;
            this.chkPublicAccessRequiresLogin.Name     = "chkPublicAccessRequiresLogin";
            this.chkPublicAccessRequiresLogin.TabIndex = 20;

            // ── chkAllowRegistration ───────────────────────────────────────
            this.chkAllowRegistration.Location  = new System.Drawing.Point(353, 438);
            this.chkAllowRegistration.AutoSize  = true;
            this.chkAllowRegistration.Text      = "Allow Registration";
            this.chkAllowRegistration.ForeColor = System.Drawing.Color.White;
            this.chkAllowRegistration.Checked   = true;
            this.chkAllowRegistration.Name      = "chkAllowRegistration";
            this.chkAllowRegistration.TabIndex  = 36;

            // ── chkMoonlightEnabled ──────────────────────────────────────
            this.chkMoonlightEnabled.Location = new System.Drawing.Point(20, 465);
            this.chkMoonlightEnabled.AutoSize = true;
            this.chkMoonlightEnabled.Text     = "Enable Moonlight Web Stream";
            this.chkMoonlightEnabled.ForeColor = System.Drawing.Color.White;
            this.chkMoonlightEnabled.Name     = "chkMoonlightEnabled";
            this.chkMoonlightEnabled.TabIndex = 21;

            // ── lblMoonlightHostId ──────────────────────────────────────
            this.lblMoonlightHostId.Location  = new System.Drawing.Point(40, 495);
            this.lblMoonlightHostId.AutoSize  = true;
            this.lblMoonlightHostId.Text      = "Manual Host ID (Pair):";
            this.lblMoonlightHostId.ForeColor = System.Drawing.Color.Gray;
            this.lblMoonlightHostId.Name      = "lblMoonlightHostId";
            this.lblMoonlightHostId.TabIndex  = 21;

            // ── txtMoonlightHostId ──────────────────────────────────────
            this.txtMoonlightHostId.Location  = new System.Drawing.Point(180, 492);
            this.txtMoonlightHostId.Size      = new System.Drawing.Size(150, 23);
            this.txtMoonlightHostId.Name      = "txtMoonlightHostId";
            this.txtMoonlightHostId.TabIndex  = 22;

            // ── lblMoonlightAppId ───────────────────────────────────────
            this.lblMoonlightAppId.Location   = new System.Drawing.Point(40, 525);
            this.lblMoonlightAppId.AutoSize   = true;
            this.lblMoonlightAppId.Text       = "Manual App ID:";
            this.lblMoonlightAppId.ForeColor  = System.Drawing.Color.Gray;
            this.lblMoonlightAppId.Name       = "lblMoonlightAppId";
            this.lblMoonlightAppId.TabIndex   = 23;

            // ── txtMoonlightAppId ───────────────────────────────────────
            this.txtMoonlightAppId.Location   = new System.Drawing.Point(180, 522);
            this.txtMoonlightAppId.Size       = new System.Drawing.Size(150, 23);
            this.txtMoonlightAppId.Name       = "txtMoonlightAppId";
            this.txtMoonlightAppId.TabIndex   = 24;

            // ─── chkHttpsEnabled ──────────────────────────────────────────
            this.chkHttpsEnabled.Location = new System.Drawing.Point(20, 555);
            this.chkHttpsEnabled.AutoSize = true;
            this.chkHttpsEnabled.Text     = "Enable HTTPS (Self-Signed)";
            this.chkHttpsEnabled.ForeColor = System.Drawing.Color.White;
            this.chkHttpsEnabled.Name     = "chkHttpsEnabled";
            this.chkHttpsEnabled.TabIndex = 25;

            // ─── chkProxyMoonlight ────────────────────────────────────────
            this.chkProxyMoonlight.Location = new System.Drawing.Point(230, 555);
            this.chkProxyMoonlight.AutoSize = true;
            this.chkProxyMoonlight.Text     = "Proxy Moonlight (Hide 8080)";
            this.chkProxyMoonlight.ForeColor = System.Drawing.Color.White;
            this.chkProxyMoonlight.Name     = "chkProxyMoonlight";
            this.chkProxyMoonlight.TabIndex = 26;

            // ── lblDefaultMode ───────────────────────────────────────────
            this.lblDefaultMode.Location  = new System.Drawing.Point(20, 590);
            this.lblDefaultMode.AutoSize  = true;
            this.lblDefaultMode.Text      = "Default Startup Mode:";
            this.lblDefaultMode.ForeColor = System.Drawing.Color.White;
            this.lblDefaultMode.Name      = "lblDefaultMode";
            this.lblDefaultMode.TabIndex  = 27;

            // ── cmbDefaultMode ───────────────────────────────────────────
            this.cmbDefaultMode.Location      = new System.Drawing.Point(160, 587);
            this.cmbDefaultMode.Size          = new System.Drawing.Size(170, 23);
            this.cmbDefaultMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDefaultMode.Items.AddRange(new object[] { "None", "Credit", "Freeplay", "Operator" });
            this.cmbDefaultMode.SelectedIndex = 0;
            this.cmbDefaultMode.Name         = "cmbDefaultMode";
            this.cmbDefaultMode.TabIndex     = 28;
            this.cmbDefaultMode.SelectedIndexChanged += new System.EventHandler(this.CmbDefaultMode_SelectedIndexChanged);

            // ── lblInitialCredits ────────────────────────────────────────
            this.lblInitialCredits.Location = new System.Drawing.Point(40, 630);
            this.lblInitialCredits.AutoSize = true;
            this.lblInitialCredits.Text     = "Initial Credits:";
            this.lblInitialCredits.ForeColor = System.Drawing.Color.White;
            this.lblInitialCredits.Visible  = false;
            this.lblInitialCredits.Name     = "lblInitialCredits";
            this.lblInitialCredits.TabIndex = 29;

            // ── numInitialCredits ────────────────────────────────────────
            this.numInitialCredits.Location = new System.Drawing.Point(160, 627);
            this.numInitialCredits.Size     = new System.Drawing.Size(80, 23);
            this.numInitialCredits.Minimum  = new decimal(new int[] { 0, 0, 0, 0 });
            this.numInitialCredits.Maximum  = new decimal(new int[] { 999, 0, 0, 0 });
            this.numInitialCredits.Value    = new decimal(new int[] { 0, 0, 0, 0 });
            this.numInitialCredits.Visible  = false;
            this.numInitialCredits.Name     = "numInitialCredits";
            this.numInitialCredits.TabIndex = 30;

            // ── lblPublicIp ──────────────────────────────────────────────
            this.lblPublicIp.Location  = new System.Drawing.Point(20, 670);
            this.lblPublicIp.AutoSize  = true;
            this.lblPublicIp.Text      = "Public IP (STUN Fallback):";
            this.lblPublicIp.ForeColor = System.Drawing.Color.White;
            this.lblPublicIp.Name      = "lblPublicIp";
            this.lblPublicIp.TabIndex  = 37;

            // ── txtPublicIp ──────────────────────────────────────────────
            this.txtPublicIp.Location = new System.Drawing.Point(180, 667);
            this.txtPublicIp.Size     = new System.Drawing.Size(200, 23);
            this.txtPublicIp.Name     = "txtPublicIp";
            this.txtPublicIp.TabIndex = 38;

            // ── btnManageKeys ────────────────────────────────────────────
            this.btnManageKeys.Location                = new System.Drawing.Point(20, 715);
            this.btnManageKeys.Size                    = new System.Drawing.Size(160, 30);
            this.btnManageKeys.Text                    = "Manage Emulator Keys";
            this.btnManageKeys.FlatStyle               = System.Windows.Forms.FlatStyle.Flat;
            this.btnManageKeys.BackColor               = System.Drawing.Color.FromArgb(87, 87, 87);
            this.btnManageKeys.ForeColor               = System.Drawing.Color.White;
            this.btnManageKeys.UseVisualStyleBackColor = false;
            this.btnManageKeys.Name                    = "btnManageKeys";
            this.btnManageKeys.TabIndex                = 31;
            this.btnManageKeys.Click                  += new System.EventHandler(this.BtnManageKeys_Click);

            // ── btnSave ──────────────────────────────────────────────────
            this.btnSave.Location                = new System.Drawing.Point(200, 765);
            this.btnSave.Size                    = new System.Drawing.Size(75, 27);
            this.btnSave.Text                    = "Save";
            this.btnSave.FlatStyle               = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.BackColor               = System.Drawing.Color.FromArgb(0, 122, 204);
            this.btnSave.ForeColor               = System.Drawing.Color.White;
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Name                    = "btnSave";
            this.btnSave.TabIndex                = 32;
            this.btnSave.Click                  += new System.EventHandler(this.BtnSave_Click);

            // ── btnCancel ────────────────────────────────────────────────
            this.btnCancel.Location                = new System.Drawing.Point(285, 765);
            this.btnCancel.Size                    = new System.Drawing.Size(75, 27);
            this.btnCancel.Text                    = "Cancel";
            this.btnCancel.FlatStyle               = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.BackColor               = System.Drawing.Color.FromArgb(87, 87, 87);
            this.btnCancel.ForeColor               = System.Drawing.Color.White;
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Name                    = "btnCancel";
            this.btnCancel.TabIndex                = 33;
            this.btnCancel.Click                  += new System.EventHandler(this.BtnCancel_Click);

            // ── Form ─────────────────────────────────────────────────────
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor           = System.Drawing.Color.FromArgb(32, 32, 32);
            this.ForeColor           = System.Drawing.Color.White;
            this.ClientSize          = new System.Drawing.Size(480, 810);
            this.FormBorderStyle     = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox         = false;
            this.MinimizeBox         = false;
            this.Name                = "ArcadeConfigForm";
            this.StartPosition       = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text                = "Arcade Mode Configuration";

            this.Controls.Add(this.picArcadeLogo);
            this.Controls.Add(this.chkEnabled);
            this.Controls.Add(this.lblArcadeInfo);
            this.Controls.Add(this.lblCoinDevice);
            this.Controls.Add(this.cmbDevices);
            this.Controls.Add(this.lblCoinKey);
            this.Controls.Add(this.txtKeyBinding);
            this.Controls.Add(this.btnDetect);
            this.Controls.Add(this.lblMinutesPerCoin);
            this.Controls.Add(this.numMinutes);
            this.Controls.Add(this.lblOperatorPwd);
            this.Controls.Add(this.txtPassword);
            this.Controls.Add(this.lblOverlayOpacity);
            this.Controls.Add(this.tbOpacity);
            this.Controls.Add(this.lblOpacityValue);
            this.Controls.Add(this.lblApiEnabled);
            this.Controls.Add(this.chkApiEnabled);
            this.Controls.Add(this.lblApiPort);
            this.Controls.Add(this.numApiPort);
            this.Controls.Add(this.lblHideOpButtons);
            this.Controls.Add(this.chkHideOpButtons);
            this.Controls.Add(this.lblDefaultMode);
            this.Controls.Add(this.cmbDefaultMode);
            this.Controls.Add(this.lblInitialCredits);
            this.Controls.Add(this.numInitialCredits);
            this.Controls.Add(this.lblPublicIp);
            this.Controls.Add(this.txtPublicIp);
            this.Controls.Add(this.chkPublicAccessEnabled);
            this.Controls.Add(this.chkPublicAccessRequiresLogin);
            this.Controls.Add(this.chkAllowRegistration);
            this.Controls.Add(this.lblAdminIps);
            this.Controls.Add(this.txtAdminIps);
            this.Controls.Add(this.chkMoonlightEnabled);
            this.Controls.Add(this.lblMoonlightHostId);
            this.Controls.Add(this.txtMoonlightHostId);
            this.Controls.Add(this.lblMoonlightAppId);
            this.Controls.Add(this.txtMoonlightAppId);
            this.Controls.Add(this.chkHttpsEnabled);
            this.Controls.Add(this.chkProxyMoonlight);
            this.Controls.Add(this.btnManageKeys);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnCancel);

            ((System.ComponentModel.ISupportInitialize)(this.numMinutes)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numApiPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInitialCredits)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbOpacity)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        // Control field declarations
        private System.Windows.Forms.CheckBox       chkEnabled;
        private System.Windows.Forms.PictureBox     picArcadeLogo;
        private System.Windows.Forms.Label          lblArcadeInfo;
        private System.Windows.Forms.Label          lblCoinDevice;
        private System.Windows.Forms.ComboBox       cmbDevices;
        private System.Windows.Forms.Label          lblCoinKey;
        private System.Windows.Forms.TextBox        txtKeyBinding;
        private System.Windows.Forms.Button         btnDetect;
        private System.Windows.Forms.Label          lblMinutesPerCoin;
        private System.Windows.Forms.NumericUpDown  numMinutes;
        private System.Windows.Forms.Label          lblOperatorPwd;
        private System.Windows.Forms.TextBox        txtPassword;
        private System.Windows.Forms.Label          lblOverlayOpacity;
        private System.Windows.Forms.TrackBar       tbOpacity;
        private System.Windows.Forms.Label          lblOpacityValue;
        private System.Windows.Forms.Label          lblApiEnabled;
        private System.Windows.Forms.CheckBox       chkApiEnabled;
        private System.Windows.Forms.Label          lblApiPort;
        private System.Windows.Forms.NumericUpDown  numApiPort;
        private System.Windows.Forms.Label          lblHideOpButtons;
        private System.Windows.Forms.CheckBox       chkHideOpButtons;
        private System.Windows.Forms.Label          lblDefaultMode;
        private System.Windows.Forms.ComboBox       cmbDefaultMode;
        private System.Windows.Forms.Label          lblInitialCredits;
        private System.Windows.Forms.NumericUpDown  numInitialCredits;
        private System.Windows.Forms.Label          lblPublicIp;
        private System.Windows.Forms.TextBox        txtPublicIp;
        private System.Windows.Forms.CheckBox       chkPublicAccessEnabled;
        private System.Windows.Forms.CheckBox       chkPublicAccessRequiresLogin;
        private System.Windows.Forms.CheckBox       chkAllowRegistration;
        private System.Windows.Forms.Label          lblAdminIps;
        private System.Windows.Forms.TextBox        txtAdminIps;
        private System.Windows.Forms.CheckBox       chkMoonlightEnabled;
        private System.Windows.Forms.Label          lblMoonlightHostId;
        private System.Windows.Forms.TextBox        txtMoonlightHostId;
        private System.Windows.Forms.Label          lblMoonlightAppId;
        private System.Windows.Forms.TextBox        txtMoonlightAppId;
        private System.Windows.Forms.CheckBox       chkHttpsEnabled;
        private System.Windows.Forms.CheckBox       chkProxyMoonlight;
        private System.Windows.Forms.Button         btnManageKeys;
        private System.Windows.Forms.Button         btnSave;
        private System.Windows.Forms.Button         btnCancel;
    }
}


