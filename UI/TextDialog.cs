using System;
using System.Drawing;
using System.Windows.Forms;

namespace BatRun.UI
{
    // EN: A small modal dialog that shows a single block of text with up to two buttons.
    // Used by Arcade Config to surface the Arcade Mode warning (Caution + informational text).
    // / FR: Petite boîte de dialogue modale affichant un bloc de texte unique avec jusqu'à
    // deux boutons. Utilisée par la config Arcade pour présenter l'avertissement du mode Arcade
    // (mise en garde + texte informatif).
    public class TextDialog : Form
    {
        private readonly Button _okButton;
        private readonly Button? _cancelButton;

        // EN:
        //   caption       : short warning shown above the information text (bold red).
        //   information   : full body text below the caption.
        //   yesLabel      : label of the affirmative button (left-most). Returns DialogResult.Yes.
        //   noLabelOrNull : optional label for a second button (Cancel / Close). Returns DialogResult.No.
        //   withWarningIcon: if true, draws a yellow warning icon next to the caption.
        // FR:
        //   caption       : courte mise en garde affichée au-dessus du texte (rouge gras).
        //   information   : corps de texte complet sous la mise en garde.
        //   yesLabel      : libellé du bouton d'affirmation (le plus à gauche). Retourne DialogResult.Yes.
        //   noLabelOrNull : libellé optionnel d'un second bouton (Annuler / Fermer). Retourne DialogResult.No.
        //   withWarningIcon: si vrai, dessine une icône d'avertissement jaune à côté de la mise en garde.
        public TextDialog(
            string caption,
            string information,
            string yesLabel,
            string? noLabelOrNull,
            bool withWarningIcon)
        {
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MaximizeBox     = false;
            MinimizeBox     = false;
            ShowInTaskbar   = false;
            BackColor       = Color.FromArgb(32, 32, 32);
            ForeColor       = Color.White;
            Font            = new Font("Segoe UI", 9F);
            ClientSize      = new Size(560, 320);

            // ── Top warning header ────────────────────────────────────────
            var headerLabel = new Label
            {
                Text      = caption,
                Font      = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = withWarningIcon
                    ? Color.FromArgb(255, 199, 0) // warning yellow-orange
                    : Color.FromArgb(114, 137, 218), // informational blue
                TextAlign = ContentAlignment.MiddleLeft,
                Location  = new Point(20, 16),
                Size      = new Size(520, 36)
            };
            Controls.Add(headerLabel);

            // ── Body text (read-only) ─────────────────────────────────────
            var infoBox = new TextBox
            {
                Text         = information,
                Multiline    = true,
                ReadOnly     = true,
                ScrollBars   = ScrollBars.Vertical,
                WordWrap     = true,
                BackColor    = Color.FromArgb(45, 45, 48),
                ForeColor    = Color.White,
                BorderStyle  = BorderStyle.FixedSingle,
                Font         = new Font("Segoe UI", 9F),
                Location     = new Point(20, 58),
                Size         = new Size(520, 200)
            };
            Controls.Add(infoBox);

            // ── Buttons ───────────────────────────────────────────────────
            _okButton = new Button
            {
                Text      = yesLabel,
                DialogResult = DialogResult.Yes,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Size      = new Size(110, 30),
                Location  = new Point(330, 275)
            };
            _okButton.FlatAppearance.BorderSize = 0;
            AcceptButton = _okButton;
            Controls.Add(_okButton);

            if (!string.IsNullOrEmpty(noLabelOrNull))
            {
                _cancelButton = new Button
                {
                    Text         = noLabelOrNull,
                    DialogResult = DialogResult.No,
                    FlatStyle    = FlatStyle.Flat,
                    BackColor    = Color.FromArgb(87, 87, 87),
                    ForeColor    = Color.White,
                    Size         = new Size(110, 30),
                    Location     = new Point(450, 275)
                };
                _cancelButton.FlatAppearance.BorderSize = 0;
                CancelButton = _cancelButton;
                Controls.Add(_cancelButton);
            }
        }
    }
}
