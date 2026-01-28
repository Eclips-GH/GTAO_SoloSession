using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTAO_SoloSession
{
    public sealed class MainForm : Form
    {
        private readonly Button _btn;
        private readonly NumericUpDown _seconds;
        private readonly Label _status;
        private readonly Label _hint;
        private readonly ProgressBar _progress;
        private bool _running;

        public MainForm()
        {
            // ----- Fenêtre -----
            Text = "GTAO Solo Session";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = true;
            ClientSize = new Size(520, 280);

            // Dark theme
            BackColor = Color.FromArgb(18, 18, 22);
            ForeColor = Color.FromArgb(235, 235, 235);

            // Icône (si présente dans le dossier de sortie)
            TrySetIconFromAssets();

            // ----- Titre -----
            var title = new Label
            {
                Text = "GTAO Solo Session",
                Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 18),
                ForeColor = ForeColor
            };

            var subtitle = new Label
            {
                Text = "Suspend GTA5.exe pendant X secondes puis reprend automatiquement.",
                Font = new Font(Font.FontFamily, 10, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(22, 55),
                ForeColor = Color.FromArgb(180, 180, 180)
            };

            // ----- Carte (panel) -----
            var card = new Panel
            {
                Location = new Point(20, 85),
                Size = new Size(480, 150),
                BackColor = Color.FromArgb(26, 26, 32)
            };
            card.Paint += (_, e) =>
            {
                using var p = new Pen(Color.FromArgb(45, 45, 55));
                e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
            };

            var lblTarget = new Label
            {
                Text = "Cible :",
                AutoSize = true,
                Location = new Point(18, 18),
                ForeColor = Color.FromArgb(180, 180, 180)
            };

            var lblTargetVal = new Label
            {
                Text = "GTA5.exe",
                AutoSize = true,
                Location = new Point(70, 18),
                Font = new Font(Font.FontFamily, 10, FontStyle.Bold),
                ForeColor = ForeColor
            };

            var lblSec = new Label
            {
                Text = "Durée (sec) :",
                AutoSize = true,
                Location = new Point(18, 52),
                ForeColor = Color.FromArgb(180, 180, 180)
            };

            _seconds = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 60,
                Value = 10,
                Location = new Point(110, 50),
                Width = 70,
                BackColor = Color.FromArgb(20, 20, 24),
                ForeColor = ForeColor,
                BorderStyle = BorderStyle.FixedSingle
            };

            _btn = new Button
            {
                Text = "Démarrer",
                Location = new Point(18, 88),
                Size = new Size(180, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(220, 60, 70),
                ForeColor = Color.White
            };
            _btn.FlatAppearance.BorderSize = 0;
            _btn.Click += async (_, __) => await RunSuspendResumeAsync();

            _hint = new Label
            {
                Text = "Astuce : lance GTA Online avant de cliquer.",
                AutoSize = true,
                Location = new Point(215, 100),
                ForeColor = Color.FromArgb(160, 160, 160)
            };

            _progress = new ProgressBar
            {
                Location = new Point(215, 128),
                Size = new Size(245, 16),
                Style = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };

            _status = new Label
            {
                Text = "Statut : prêt",
                AutoSize = true,
                Location = new Point(18, 122 + 40),
                ForeColor = Color.FromArgb(200, 200, 200)
            };

            card.Controls.Add(lblTarget);
            card.Controls.Add(lblTargetVal);
            card.Controls.Add(lblSec);
            card.Controls.Add(_seconds);
            card.Controls.Add(_btn);
            card.Controls.Add(_hint);
            card.Controls.Add(_progress);
            card.Controls.Add(_status);

            Controls.Add(title);
            Controls.Add(subtitle);
            Controls.Add(card);
        }

        private void TrySetIconFromAssets()
        {
            // On essaie de charger l’icône copiée dans le dossier de sortie:
            // ...\bin\Debug\net8.0-windows\assets\icon.ico
            try
            {
                var iconPath = Path.Combine(AppContext.BaseDirectory, "assets", "icon.ico");
                if (File.Exists(iconPath))
                {
                    Icon = new Icon(iconPath);
                }
            }
            catch { /* ignore */ }
        }

        private async Task RunSuspendResumeAsync()
        {
            if (_running) return;
            _running = true;

            try
            {
                SetUiBusy(true);

                var procs = Process.GetProcessesByName("GTA5");
                if (procs.Length == 0)
                {
                    SetStatus("Statut : GTA5.exe introuvable (lance le jeu d'abord)", isError: true);
                    return;
                }

                var proc = procs[0];
                int secs = (int)_seconds.Value;

                SetStatus($"Statut : suspension ({secs}s)…");
                _progress.Value = 0;

                ProcessHelper.SuspendProcess(proc);

                for (int i = secs; i >= 1; i--)
                {
                    int done = (int)Math.Round((1.0 - (i / (double)secs)) * 100.0);
                    _progress.Value = Math.Clamp(done, 0, 100);
                    SetStatus($"Statut : suspendu… reprise dans {i}s");
                    await Task.Delay(1000);
                }

                ProcessHelper.ResumeProcess(proc);
                _progress.Value = 100;
                SetStatus("Statut : repris ✅");
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur : {ex.Message}", isError: true);
            }
            finally
            {
                SetUiBusy(false);
                _running = false;
            }
        }

        private void SetUiBusy(bool busy)
        {
            _btn.Enabled = !busy;
            _seconds.Enabled = !busy;
            _btn.Text = busy ? "En cours…" : "Démarrer";
        }

        private void SetStatus(string text, bool isError = false)
        {
            _status.Text = text;
            _status.ForeColor = isError ? Color.FromArgb(255, 120, 120) : Color.FromArgb(200, 200, 200);
        }
    }
}
