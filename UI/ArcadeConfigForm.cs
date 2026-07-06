using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.UI
{
    // EN: Configuration form for the Arcade mode (coin device, key binding, API, etc.)
    // FR: Formulaire de configuration du mode Arcade (périphérique de pièce, touche, API, etc.)
    public partial class ArcadeConfigForm : Form
    {
        private readonly IniFile _config;
        private readonly ArcadeManager? _manager;

        private ushort _currentBindingKey = 53; // Default '5'

        // EN: ToolTip reused for the "(?)" help label.
        // FR: ToolTip réutilisé pour l'icône d'aide "(?)".
        private readonly ToolTip _infoTip = new ToolTip
        {
            IsBalloon    = false,
            AutoPopDelay = 30000,
            InitialDelay = 100,
            ReshowDelay  = 100,
            ToolTipIcon  = ToolTipIcon.Warning,
            ToolTipTitle = "Enable Arcade Mode"
        };

        // EN: Warning message shown both as a tooltip on "(?)" and as a confirmation dialog
        // when the user toggles "Enable Arcade Mode" on. Same English copy in both spots.
        // FR: Message d'avertissement affiché en infobulle sur "(?)" ET en boîte de confirmation
        // quand l'utilisateur active « Enable Arcade Mode ». Même texte anglais aux deux endroits.
        private const string ArcadeInfoMessage =
            "Turning this on activates credit/coin simulation.\n\n" +
            "- Default startup mode is configured below in \"Default Startup Mode\".\n" +
            "- Once Credit mode is on, coins can be added with the D5 key of a keyboard\n" +
            "  or any dedicated coin hardware.\n" +
            "- Admin mode is unlocked with a long press of D9.\n" +
            "- Emergency mode: reboot BatRun via D0 key\n" +
            "  (default operator password is \"admin\").\n" +
            "- You can also manage all of this from a local Web UI by enabling\n" +
            "  \"Enable Operator API (Web UI)\".";


        // ─────────────────────────────────────────────────────────────────
        // Constructor / Constructeur
        // ─────────────────────────────────────────────────────────────────
        public ArcadeConfigForm(IniFile config, ArcadeManager? manager = null)
        {
            _config  = config;
            _manager = manager;

            // EN: Build designer controls, then populate runtime data.
            // FR: Construit les contrôles designer, puis charge les données runtime.
            InitializeComponent();
            InitializeRuntimeData();
        }

        // ─────────────────────────────────────────────────────────────────
        // Runtime data — called after InitializeComponent
        // Données runtime — appelé après InitializeComponent
        // ─────────────────────────────────────────────────────────────────
        private void InitializeRuntimeData()
        {
            // EN: Populate the keyboard device list (cannot be done in Designer).
            // FR: Remplir la liste des périphériques clavier (impossible dans le Designer).
            var devices = RawInputHandler.GetKeyboardDevices();
            devices.Insert(0, new RawInputDeviceItem
            {
                DisplayName = "ANY KEYBOARD (For testing)",
                Handle      = IntPtr.Zero,
                Name        = "ANY"
            });
            cmbDevices.DataSource = devices;

            LoadArcadeLogo();
            AttachArcadeInfoHandlers();
        }

        // EN: Pulls the icon.ico from Assets/, downscales it to 16x16, drops it in picArcadeLogo.
        // FR: Récupère icon.ico depuis Assets/, le redimensionne à 16x16, l'affiche dans picArcadeLogo.
        private void LoadArcadeLogo()
        {
            try
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
                if (File.Exists(iconPath))
                {
                    using var src = new Icon(iconPath);
                    using var bmp  = new Bitmap(16, 16);
                    using (var g   = Graphics.FromImage(bmp))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode     = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        g.Clear(Color.Transparent);
                        g.DrawIcon(src, new Rectangle(0, 0, 16, 16));
                    }
                    picArcadeLogo.Image = (Image)bmp.Clone();
                }
            }
            catch { /* logo is decorative; ignore load failures */ }
        }

        // EN: Wire up the "(?)" label: tooltip on hover, message box on click.
        // NB: the chkEnabled.CheckedChanged listener is NOT attached here — it is attached
        // at the END of LoadConfig(), after the checkbox has been initialized to its saved
        // value. Otherwise toggling Checked inside LoadConfig would fire the confirmation
        // prompt involuntarily (e.g. when "Enabled=true" is in the INI and the user simply
        // opens this form).
        // FR: Câble le label "(?)" : tooltip au survol, message box au clic.
        // L'écouteur chkEnabled.CheckedChanged n'est PAS attaché ici — il sera attaché à la
        // fin de LoadConfig(), une fois la checkbox initialisée à sa valeur sauvegardée. Sinon
        // écrire chkEnabled.Checked pendant LoadConfig déclencherait la confirmation à l'insu
        // de l'utilisateur (ex. quand « Enabled=true » est déjà dans l'INI et qu'il ouvre juste
        // ce formulaire).
        private void AttachArcadeInfoHandlers()
        {
            lblArcadeInfo.Click += LblArcadeInfo_Click;
            lblArcadeInfo.MouseHover += (s, e) =>
                _infoTip.SetToolTip(lblArcadeInfo, ArcadeInfoMessage);
            lblArcadeInfo.MouseEnter  += (s, e) =>
                _infoTip.SetToolTip(lblArcadeInfo, ArcadeInfoMessage);
        }

        private bool _previousEnabledState;

        // EN: Called from LoadConfig() AFTER chkEnabled.Checked has been set to the loaded
        // value. Captures that loaded value as the "previous" state, then wires the handler.
        // Real user toggles from this point on will correctly fire the confirmation prompt.
        // FR: Appelé depuis LoadConfig() APRÈS que chkEnabled.Checked a été posé à la valeur
        // chargée. Capture cette valeur chargée comme état « précédent », puis câble l'écouteur.
        // Les vrais toggles utilisateur à partir de ce point déclencheront correctement la
        // confirmation.
        private void WireCheckboxToggleListener()
        {
            _previousEnabledState = chkEnabled.Checked;
            chkEnabled.CheckedChanged += ChkEnabled_CheckedChanged;
        }

        private void ChkEnabled_CheckedChanged(object? sender, EventArgs e)
        {
            // EN: Only prompt when going from false to true. Toggling it back off is silent.
            // FR: Ne demande que pour le passage false -> true. Re-passer à off est silencieux.
            if (chkEnabled.Checked && !_previousEnabledState)
            {
                var prompt = new TextDialog(
                    "Enable Arcade Mode — caution",
                    ArcadeInfoMessage,
                    "Enable",
                    "Cancel",
                    true);
                if (prompt.ShowDialog(this) != DialogResult.Yes)
                {
                    // Revert to the previous state silently.
                    chkEnabled.CheckedChanged -= ChkEnabled_CheckedChanged;
                    chkEnabled.Checked = false;
                    chkEnabled.CheckedChanged += ChkEnabled_CheckedChanged;
                    return;
                }
            }
            _previousEnabledState = chkEnabled.Checked;
        }

        // EN: Short caption shown ABOVE the long info text. Keep it punchy so the user can
        // decide whether to read the note from inside the dialog.
        // FR: Titre court affiché AU-DESSUS du long texte d'info. Concis, pour que l'utilisateur
        // puisse décider s'il lit la note de la boîte de dialogue.
        private const string WarningShortCaption =
            "Enabling Arcade Mode starts an unattended credit simulation.\r\n\r\n" +
            "Please read the note below before you continue.";

        private void LblArcadeInfo_Click(object? sender, EventArgs e)
        {
            var dlg = new TextDialog(
                WarningShortCaption,
                ArcadeInfoMessage,
                "Close",
                null,
                false);
            dlg.ShowDialog(this);
        }

        // ─────────────────────────────────────────────────────────────────
        // Form load / Chargement du formulaire
        // ─────────────────────────────────────────────────────────────────
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadConfig();
        }

        // ─────────────────────────────────────────────────────────────────
        // Event handlers / Gestionnaires d'événements
        // ─────────────────────────────────────────────────────────────────

        private void TbOpacity_Scroll(object? sender, EventArgs e)
        {
            lblOpacityValue.Text = $"{tbOpacity.Value}%";
        }

        private void CmbDefaultMode_SelectedIndexChanged(object? sender, EventArgs e)
        {
            bool isCredit = cmbDefaultMode.Text == "Credit";
            lblInitialCredits.Visible  = isCredit;
            numInitialCredits.Visible  = isCredit;
        }

        private void BtnManageKeys_Click(object? sender, EventArgs e)
        {
            new AppKeyConfigForm().ShowDialog(this);
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            this.Close();
        }

        // EN: Opens a prompt form to capture the next key press as coin key binding.
        // FR: Ouvre un formulaire invite pour capturer la prochaine touche pressée.
        private void BtnDetect_Click(object? sender, EventArgs e)
        {
            var prompt = new Form
            {
                Width           = 250,
                Height          = 100,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text            = "Press any key...",
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.FromArgb(32, 32, 32),
                ForeColor       = Color.White
            };
            var textLabel = new Label { Left = 50, Top = 20, Text = "Press coin button now." };
            prompt.Controls.Add(textLabel);
            prompt.KeyPreview = true;
            prompt.KeyDown += (s, args) =>
            {
                _currentBindingKey  = (ushort)args.KeyValue;
                txtKeyBinding.Text  = args.KeyCode.ToString();
                prompt.Close();
            };
            prompt.ShowDialog();
        }

        // ─────────────────────────────────────────────────────────────────
        // Load config into controls / Charger la config dans les contrôles
        // ─────────────────────────────────────────────────────────────────
        private void LoadConfig()
        {
            chkEnabled.Checked = _config.ReadBool("Arcade", "Enabled", false);

            string savedDevice = _config.ReadValue("Arcade", "CoinDevice", "ANY").Trim();
            bool found = false;

            if (cmbDevices.Items.Count > 0)
            {
                _manager?.GetLogger()?.LogInfo($"ArcadeConfig: Attempting to match saved device: '{savedDevice}'");
                foreach (var obj in cmbDevices.Items)
                {
                    if (obj is RawInputDeviceItem item)
                    {
                        // EN: Clean both strings of null chars and whitespace.
                        // FR: Nettoyer les deux chaînes des caractères nuls et espaces.
                        string itemName        = item.Name.Replace("\0", "").Trim();
                        string cleanSavedDevice = savedDevice.Replace("\0", "").Trim();

                        bool match = string.Equals(itemName, savedDevice, StringComparison.OrdinalIgnoreCase);
                        if (!match && itemName.Length > 5 && cleanSavedDevice.Length > 5)
                        {
                            string cleanItem  = itemName.Contains("#") ? itemName.Substring(itemName.IndexOf("#")) : itemName;
                            string cleanSaved = cleanSavedDevice.Contains("#") ? cleanSavedDevice.Substring(cleanSavedDevice.IndexOf("#")) : cleanSavedDevice;
                            match = string.Equals(cleanItem, cleanSaved, StringComparison.OrdinalIgnoreCase);
                        }

                        if (match)
                        {
                            _manager?.GetLogger()?.LogInfo($"ArcadeConfig: Match found for '{item.DisplayName}' (Internal: {itemName})");
                            cmbDevices.SelectedItem = item;
                            found = true;
                            break;
                        }
                    }
                }
            }

            if (!found && (savedDevice.Equals("ANY", StringComparison.OrdinalIgnoreCase) || savedDevice == ""))
            {
                foreach (var obj in cmbDevices.Items)
                {
                    if (obj is RawInputDeviceItem item && item.Name.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                    {
                        cmbDevices.SelectedItem = item;
                        found = true;
                        break;
                    }
                }
                if (!found && cmbDevices.Items.Count > 0)
                    cmbDevices.SelectedIndex = 0;
            }

            _currentBindingKey = (ushort)_config.ReadInt("Arcade", "CoinKey", 53);
            txtKeyBinding.Text = ((Keys)_currentBindingKey).ToString();
            numMinutes.Value   = _config.ReadInt("Arcade", "MinutesPerCredit", 5);
            txtPassword.Text   = _config.ReadValue("Arcade", "OperatorPassword", "admin");

            if (int.TryParse(_config.ReadValue("Arcade", "OverlayOpacity", "85"), out int currentOpacity))
                tbOpacity.Value = Math.Max(10, Math.Min(100, currentOpacity));
            else
                tbOpacity.Value = 85;
            lblOpacityValue.Text = $"{tbOpacity.Value}%";

            chkApiEnabled.Checked    = _config.ReadBool("Arcade", "ApiEnabled", false);
            numApiPort.Value         = _config.ReadInt("Arcade", "ApiPort", 4321);
            chkHideOpButtons.Checked = _config.ReadBool("Arcade", "HideOperatorButtons", false);
            chkPublicAccessEnabled.Checked = _config.ReadBool("Arcade", "PublicAccessEnabled", false);
            chkPublicAccessRequiresLogin.Checked = _config.ReadBool("Arcade", "PublicAccessRequiresLogin", false);
            chkAllowRegistration.Checked = _config.ReadBool("Arcade", "PublicAccessAllowRegistration", true);
            txtAdminIps.Text = _config.ReadValue("Arcade", "AdminAllowedIps", "");
            chkMoonlightEnabled.Checked    = _config.ReadBool("Arcade", "MoonlightStreamEnabled", false);
            chkHttpsEnabled.Checked        = _config.ReadBool("Arcade", "HttpsEnabled", false);
            chkProxyMoonlight.Checked      = _config.ReadBool("Arcade", "ProxyMoonlight", false);
            txtMoonlightHostId.Text        = _config.ReadValue("Arcade", "MoonlightHostId", "");
            txtMoonlightAppId.Text         = _config.ReadValue("Arcade", "MoonlightAppId", "");

            string defMode = _config.ReadValue("Arcade", "DefaultMode", "None");
            cmbDefaultMode.SelectedItem = cmbDefaultMode.Items.Contains(defMode) ? defMode : "None";
            numInitialCredits.Value     = _config.ReadInt("Arcade", "InitialCredits", 0);
            txtPublicIp.Text            = _config.ReadValue("Arcade", "PublicIp", "");

            // EN: Explicitly refresh Initial Credits visibility after loading saved mode
            // FR: Forcer la mise à jour de la visibilité des crédits initiaux après chargement
            bool isCredit = cmbDefaultMode.Text == "Credit";
            lblInitialCredits.Visible  = isCredit;
            numInitialCredits.Visible  = isCredit;

            // EN: Now that the checkbox has been initialised to its loaded value, attach the
            // CheckedChanged listener so that only USER-initiated toggles will trigger the
            // confirmation popup. Must be the LAST thing LoadConfig() does.
            // FR: Maintenant que la checkbox a été initialisée à sa valeur chargée, on attache
            // l'écouteur CheckedChanged pour que seuls les toggles initiés par l'UTILISATEUR
            // déclenchent la confirmation. Doit être la DERNIÈRE chose que LoadConfig() fait.
            WireCheckboxToggleListener();
        }

        // ─────────────────────────────────────────────────────────────────
        // Save / Sauvegarde
        // ─────────────────────────────────────────────────────────────────
        private void BtnSave_Click(object? sender, EventArgs e)
        {
            _config.WriteValue("Arcade", "Enabled",              chkEnabled.Checked.ToString());
            var selectedDevice = (RawInputDeviceItem)cmbDevices.SelectedItem!;
            _config.WriteValue("Arcade", "CoinDevice",           selectedDevice?.Name ?? "ANY");
            _config.WriteValue("Arcade", "CoinKey",              _currentBindingKey.ToString());
            _config.WriteValue("Arcade", "MinutesPerCredit",     numMinutes.Value.ToString());
            _config.WriteValue("Arcade", "OperatorPassword",     txtPassword.Text);
            _config.WriteValue("Arcade", "OverlayOpacity",       tbOpacity.Value.ToString());
            _config.WriteValue("Arcade", "ApiEnabled",           chkApiEnabled.Checked.ToString());
            _config.WriteValue("Arcade", "ApiPort",              numApiPort.Value.ToString());
            _config.WriteValue("Arcade", "HideOperatorButtons",  chkHideOpButtons.Checked.ToString());
            _config.WriteValue("Arcade", "PublicAccessEnabled",  chkPublicAccessEnabled.Checked.ToString());
            _config.WriteValue("Arcade", "PublicAccessRequiresLogin", chkPublicAccessRequiresLogin.Checked.ToString());
            _config.WriteValue("Arcade", "PublicAccessAllowRegistration", chkAllowRegistration.Checked.ToString());
            _config.WriteValue("Arcade", "AdminAllowedIps",      txtAdminIps.Text);
            _config.WriteValue("Arcade", "MoonlightStreamEnabled", chkMoonlightEnabled.Checked.ToString());
            _config.WriteValue("Arcade", "HttpsEnabled",         chkHttpsEnabled.Checked.ToString());
            _config.WriteValue("Arcade", "ProxyMoonlight",       chkProxyMoonlight.Checked.ToString());
            _config.WriteValue("Arcade", "MoonlightHostId",      txtMoonlightHostId.Text);
            _config.WriteValue("Arcade", "MoonlightAppId",       txtMoonlightAppId.Text);
            _config.WriteValue("Arcade", "DefaultMode",          cmbDefaultMode.Text);
            _config.WriteValue("Arcade", "InitialCredits",       numInitialCredits.Value.ToString());
            _config.WriteValue("Arcade", "PublicIp",             txtPublicIp.Text);

            bool esLoadingActive = _config.ReadBool("Windows", "HideESLoading", false);
            ManageNotifyScript(chkEnabled.Checked, esLoadingActive);

            _manager?.SyncWithConfig();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void ManageNotifyScript(bool arcadeEnabled, bool esLoadingEnabled)
        {
            ArcadeManager.ManageNotifyScripts(arcadeEnabled, esLoadingEnabled);
        }
    }
}


