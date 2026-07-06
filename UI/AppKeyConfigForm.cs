using System;
using System.Drawing;
using System.Windows.Forms;

using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.UI
{
    // EN: Configuration form for emulator and game key bindings.
    // FR: Formulaire de configuration pour les raccourcis clavier des émulateurs et jeux.
    public partial class AppKeyConfigForm : Form
    {
        public AppKeyConfigForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            grid.Rows.Clear();
            foreach (var cfg in AppKeyManager.Configs)
            {
                grid.Rows.Add(
                    cfg.ExeName, 
                    cfg.PauseKey, 
                    cfg.ResumeKey, 
                    cfg.TimeoutKey, 
                    cfg.TimeoutSeconds, 
                    cfg.AllowAltTab, 
                    cfg.AllowedForegroundWindows);
            }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            grid.Rows.Add("new_app", "ESC", "ESC", "ALT+F4", "30", false, "");
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (grid.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in grid.SelectedRows)
                {
                    if (!row.IsNewRow)
                    {
                        grid.Rows.Remove(row);
                    }
                }
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            AppKeyManager.Configs.Clear();
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                
                var cfg = new AppKeyConfig
                {
                    ExeName                  = row.Cells[0].Value?.ToString() ?? "",
                    PauseKey                 = row.Cells[1].Value?.ToString() ?? "",
                    ResumeKey                = row.Cells[2].Value?.ToString() ?? "",
                    TimeoutKey               = row.Cells[3].Value?.ToString() ?? "",
                    TimeoutSeconds           = int.TryParse(row.Cells[4].Value?.ToString(), out int ts) ? ts : 30,
                    AllowAltTab              = row.Cells[5].Value is bool b ? b : false,
                    AllowedForegroundWindows = row.Cells[6].Value?.ToString() ?? ""
                };

                if (!string.IsNullOrWhiteSpace(cfg.ExeName))
                {
                    AppKeyManager.Configs.Add(cfg);
                }
            }

            AppKeyManager.Save();
            this.Close();
        }
    }
}


