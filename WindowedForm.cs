using System;
using System.Windows.Forms;

namespace BatRun
{
    public partial class WindowedForm : Form
    {
        public WindowedForm()
        {
            InitializeComponent();
            // Configuration de la fenêtre
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
        }

        private void InitializeComponent()
        {
            // Initialisation des composants du formulaire
            this.SuspendLayout();
            // 
            // WindowedForm
            // 
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Name = "WindowedForm";
            this.ResumeLayout(false);
        }

        private void InitializeComponent()
        {
            // Initialisation des composants du formulaire
            this.SuspendLayout();
            // 
            // WindowedForm
            // 
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Name = "WindowedForm";
            this.ResumeLayout(false);
        }

        // Événements et logiques supplémentaires peuvent être ajoutés ici
    }
}
