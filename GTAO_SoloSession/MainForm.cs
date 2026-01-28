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
        private readonly Label _progressLabel;
        private readonly Label _gtaState;
        private readonly ProgressBar _progress;
        private readonly PictureBox _logo;
        private readonly System.Windows.Forms.Timer _gtaPollTimer;


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

            // Thème (inchangé)
            BackColor = Color.FromArgb(18, 18, 22);
            ForeColor = Color.FromArgb(235, 235, 235);

            // Icône fenêtre (si présente dans le dossier de sortie)
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

            // Logo en haut à droite (même ligne que le titre)
            _logo = new PictureBox
            {
                Size = new Size(100, 100), // ← logo plus grand

                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            TryLoadLogoPng();
            Controls.Add(_logo);

            // Sous-titre (modifié)
            var subtitle = new Label
            {
                Text = "Fais en sorte que tu sois seul dans ta session public.",
                Font = new Font(Font.FontFamily, 10, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(22, 55),
                ForeColor = Color.FromArgb(180, 180, 180)
            };

            // ----- Carte (panel) -----
            var card = new Panel
            {
                Location = new Point(20, 115),   // ← on descend
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

            // État GTA (ajouté)
            _gtaState = new Label
            {
                Text = "GTA : vérification…",
                AutoSize = true,
                Location = new Point(330, 18),
                ForeColor = Color.FromArgb(180, 180, 180)
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

            // Texte au-dessus de la progress bar (modifié)
            _progressLabel = new Label
            {
                Text = "Pour générer la session, lance d'abord GTA",
                AutoSize = true,
                Location = new Point(215, 92),
                ForeColor = Color.FromArgb(160, 160, 160)
            };


            _progress = new ProgressBar
            {
                Location = new Point(215, 116),
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
                Location = new Point(18, 162),
                ForeColor = Color.FromArgb(200, 200, 200)
            };

            card.Controls.Add(lblTarget);
            card.Controls.Add(lblTargetVal);
            card.Controls.Add(_gtaState);
            card.Controls.Add(lblSec);
            card.Controls.Add(_seconds);
            card.Controls.Add(_btn);
            card.Controls.Add(_progressLabel);
            card.Controls.Add(_progress);
            card.Controls.Add(_status);

            Controls.Add(title);
            Controls.Add(subtitle);
            Controls.Add(card);

            // Place le logo après ajout des contrôles (pour calculer la position)
            PositionLogoTopRight();

            // Timer : affiche si GTA est lancé ou non (ajouté)
            _gtaPollTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _gtaPollTimer.Tick += (_, __) => UpdateGtaRunningState();
            _gtaPollTimer.Start();

            // Premier check immédiat
            UpdateGtaRunningState();

            // Si la fenêtre est redimensionnée (peu probable), on repositionne le logo
            Resize += (_, __) => PositionLogoTopRight();
        }

        private void TrySetIconFromAssets()
        {
            try
            {
                var iconPath = Path.Combine(AppContext.BaseDirectory, "assets", "icon.ico");
                if (File.Exists(iconPath))
                {
                    Icon = new Icon(iconPath);
                }
            }
            catch { }
        }

        private void TryLoadLogoPng()
        {
            try
            {
                var logoPath = Path.Combine(AppContext.BaseDirectory, "assets", "logo.png");
                if (File.Exists(logoPath))
                {
                    // Important: charger via Image.FromFile garde un lock fichier.
                    // On lit en mémoire pour éviter ça.
                    using var fs = new FileStream(logoPath, FileMode.Open, FileAccess.Read);
                    using var imgTemp = Image.FromStream(fs);
                    _logo.Image = new Bitmap(imgTemp);
                }
            }
            catch
            {
                // si absent, pas grave
                _logo.Image = null;
            }
        }

        private void PositionLogoTopRight()
        {
            // Aligné "sur la même ligne" que le titre (zone haute)
            int marginRight = 20;
            int top = 10; // même top que le titre
            _logo.Location = new Point(ClientSize.Width - _logo.Width - marginRight, top);
        }

        private void UpdateGtaRunningState()
        {
            bool running = IsGtaRunning();

            _gtaState.Text = running ? "GTA : lancé ✅" : "GTA : non lancé ❌";
            _gtaState.ForeColor = running
                ? Color.FromArgb(140, 220, 140)
                : Color.FromArgb(255, 140, 140);

            // Si une génération est en cours, on ne change pas les textes/boutons via le timer
            if (_running)
                return;

            // Bouton activé seulement si GTA est lancé
            _btn.Enabled = running;

            if (!running)
            {
                _progressLabel.Text = "Pour générer la session, lance d'abord GTA";
                SetStatus("Statut : lance GTA Online puis clique sur Démarrer", isError: false);
            }
            else
            {
                // GTA lancé, prêt
                // (On ne force pas le statut si tu es déjà en train d'afficher autre chose.)
                if (_status.Text.StartsWith("Statut : lance GTA", StringComparison.OrdinalIgnoreCase))
                    SetStatus("Statut : prêt");

                // Texte au-dessus de la barre quand tout est prêt
                if (_progressLabel.Text.StartsWith("Pour générer la session", StringComparison.OrdinalIgnoreCase))
                    _progressLabel.Text = "Prêt à générer la session solo";
            }
        }

        private static bool IsGtaRunning()
        {
            try
            {
                return Process.GetProcessesByName("GTA5").Length > 0;
            }
            catch
            {
                return false;
            }
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
                    _progressLabel.Text = "Pour générer la session, lance d'abord GTA";
                    SetStatus("Statut : GTA5.exe introuvable (lance le jeu d'abord)", isError: true);
                    return;
                }

                var proc = procs[0];
                int secs = (int)_seconds.Value;

                // Texte demandé pendant le clic / génération
                _progressLabel.Text = "Génération de la session solo en cours";

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

                // Refresh état GTA juste après
                UpdateGtaRunningState();
            }
        }

        private void SetUiBusy(bool busy)
        {
            // Pendant l'action, on force le bouton actif (même si timer voit GTA off)
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
