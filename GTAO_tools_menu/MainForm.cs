
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTAO_tools_menu
{
    public sealed class MainForm : Form
    {
        private readonly Button _btnToggleGta;
        private readonly Button _btnSoloSession;
        private readonly NumericUpDown _seconds;
        private readonly Label _status;
        private readonly Label _progressLabel;
        private readonly Label _gtaState;
        private readonly ProgressBar _progress;
        private readonly System.Windows.Forms.Timer _gtaPollTimer;
        private readonly PictureBox _logo;

        private bool _running;

        public MainForm()
        {
            Text = "GTAO Tools Menu";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = true;
            ClientSize = new Size(520, 380);

            BackColor = Color.FromArgb(18, 18, 22);
            ForeColor = Color.FromArgb(235, 235, 235);

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
                Text = "Fais en sorte que tu sois seul dans ta session public.",
                Font = new Font(Font.FontFamily, 10),
                AutoSize = true,
                Location = new Point(22, 55),
                ForeColor = Color.FromArgb(180, 180, 180)
            };

            _logo = new PictureBox
            {
                Size = new Size(90, 90),
                Location = new Point(410, 10),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            try
            {
                string logoPath = Path.Combine(AppContext.BaseDirectory, "assets", "logo.png");

                if (File.Exists(logoPath))
                {
                    _logo.Image = Image.FromFile(logoPath);
                }
            }
            catch { }

            var card = new Panel
            {
                Location = new Point(20, 115),
                Size = new Size(480, 250),
                BackColor = Color.FromArgb(26, 26, 32)
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

            _gtaState = new Label
            {
                Text = "GTA : vérification…",
                AutoSize = true,
                Location = new Point(255, 18),
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
                Maximum = 20,
                Value = 10,
                Location = new Point(110, 50),
                Width = 70,
                BackColor = Color.FromArgb(20, 20, 24),
                ForeColor = ForeColor
            };

            _btnToggleGta = new Button
            {
                Text = "Lancer GTA",
                Location = new Point(18, 88),
                Size = new Size(180, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 150, 80),
                ForeColor = Color.White
            };
            _btnToggleGta.Click += ToggleGta;

            _btnSoloSession = new Button
            {
                Text = "Créer session solo",
                Location = new Point(18, 138),
                Size = new Size(180, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(220, 60, 70),
                ForeColor = Color.White,
                Enabled = false
            };
            _btnSoloSession.Click += async (_, __) => await RunSuspendResumeAsync();

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
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Continuous
            };

            _status = new Label
            {
                Text = "Statut : prêt",
                AutoSize = true,
                Location = new Point(18, 198),
                ForeColor = Color.FromArgb(200, 200, 200)
            };

            card.Controls.Add(lblTarget);
            card.Controls.Add(lblTargetVal);
            card.Controls.Add(_gtaState);
            card.Controls.Add(lblSec);
            card.Controls.Add(_seconds);
            card.Controls.Add(_btnToggleGta);
            card.Controls.Add(_btnSoloSession);
            card.Controls.Add(_progressLabel);
            card.Controls.Add(_progress);
            card.Controls.Add(_status);

            Controls.Add(title);
            Controls.Add(subtitle);
            Controls.Add(card);
            Controls.Add(_logo);

            _gtaPollTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _gtaPollTimer.Tick += (_, __) => UpdateGtaRunningState();
            _gtaPollTimer.Start();

            UpdateGtaRunningState();
        }

        private void UpdateGtaRunningState()
        {
            bool running = IsGtaRunning();

            _btnToggleGta.Text = running ? "Fermer GTA" : "Lancer GTA";

            if (!running)
            {
                _btnSoloSession.Enabled = false;
                _gtaState.Text = "GTA : non lancé ❌";
                _progress.Value = 0;
                _progressLabel.Text = "Pour générer la session, lance d'abord GTA";
                return;
            }

            _gtaState.Text = "GTA : lancé ✅";

            if (!_running)
            {
                _btnSoloSession.Enabled = true;
                if (_progress.Value == 0)
                    _progressLabel.Text = "Prêt à créer la session solo";
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
            if (_running)
                return;

            _running = true;

            try
            {
                _btnSoloSession.Enabled = false;
                _btnToggleGta.Enabled = false;
                _seconds.Enabled = false;

                var procs = Process.GetProcessesByName("GTA5");
                if (procs.Length == 0)
                {
                    _progress.Value = 0;
                    _progressLabel.Text = "GTA introuvable";
                    SetStatus("Statut : GTA5.exe introuvable", true);
                    return;
                }

                var proc = procs[0];
                int secs = (int)_seconds.Value;

                _progress.Value = 0;
                _progressLabel.Text = "Création de la session solo...";

                ProcessHelper.SuspendProcess(proc);

                for (int i = secs; i >= 1; i--)
                {
                    int percent = (int)Math.Round(((secs - i + 1) / (double)secs) * 100.0);
                    _progress.Value = Math.Max(0, Math.Min(100, percent));
                    _progressLabel.Text = $"Création de la session solo... {percent}%";
                    await Task.Delay(1000);
                }

                ProcessHelper.ResumeProcess(proc);

                _progress.Value = 100;
                _progressLabel.Text = "Session solo créée";
                SetStatus("Statut : session solo créée ✅");

                await Task.Delay(800);
            }
            catch (Exception ex)
            {
                _progress.Value = 0;
                _progressLabel.Text = "Erreur";
                SetStatus($"Erreur : {ex.Message}", true);
            }
            finally
            {
                _running = false;
                _btnToggleGta.Enabled = true;
                _seconds.Enabled = true;
                UpdateGtaRunningState();
            }
        }

        private void ToggleGta(object? sender, EventArgs e)
        {
            if (IsGtaRunning())
                CloseGta();
            else
                LaunchGta();

            UpdateGtaRunningState();
        }

        private void LaunchGta()
        {
            try
            {
                string? gtaLauncher = FindPlayGtavExecutable();

                if (string.IsNullOrWhiteSpace(gtaLauncher))
                {
                    _progress.Value = 0;
                    _progressLabel.Text = "PlayGTAV.exe introuvable";
                    SetStatus("PlayGTAV.exe introuvable sur l'ordinateur", true);
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = gtaLauncher,
                    WorkingDirectory = Path.GetDirectoryName(gtaLauncher),
                    UseShellExecute = true
                });

                _progress.Value = 0;
                _progressLabel.Text = "Lancement de GTA...";
                SetStatus("Statut : lancement de GTA…");
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, true);
            }
        }

        private void CloseGta()
        {
            try
            {
                foreach (var proc in Process.GetProcessesByName("GTA5"))
                {
                    proc.Kill();
                }

                _progress.Value = 0;
                _progressLabel.Text = "GTA fermé";
                SetStatus("Statut : GTA fermé ✅");
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, true);
            }
        }

        private void SetStatus(string text, bool isError = false)
        {
            _status.Text = text;
            _status.ForeColor = isError ? Color.Red : Color.White;
        }

        private string? FindPlayGtavExecutable()
        {
            try
            {
                string[] likelyPaths =
                {
                    @"C:\Program Files\Rockstar Games\Grand Theft Auto V\PlayGTAV.exe",
                    @"C:\Program Files\Rockstar Games\Grand Theft Auto V Legacy\PlayGTAV.exe",
                    @"C:\Program Files (x86)\Rockstar Games\Grand Theft Auto V\PlayGTAV.exe",
                    @"C:\Program Files (x86)\Rockstar Games\Grand Theft Auto V Legacy\PlayGTAV.exe",

                    @"C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V\PlayGTAV.exe",
                    @"C:\SteamLibrary\steamapps\common\Grand Theft Auto V\PlayGTAV.exe",
                    @"D:\SteamLibrary\steamapps\common\Grand Theft Auto V\PlayGTAV.exe",
                    @"E:\SteamLibrary\steamapps\common\Grand Theft Auto V\PlayGTAV.exe",
                    @"F:\SteamLibrary\steamapps\common\Grand Theft Auto V\PlayGTAV.exe",

                    @"C:\Program Files\Epic Games\GTAV\PlayGTAV.exe",
                    @"C:\Program Files\Epic Games\Grand Theft Auto V\PlayGTAV.exe"
                };

                foreach (var path in likelyPaths)
                {
                    if (File.Exists(path))
                        return path;
                }

                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (!drive.IsReady)
                        continue;

                    try
                    {
                        var files = Directory.GetFiles(
                            drive.RootDirectory.FullName,
                            "PlayGTAV.exe",
                            SearchOption.AllDirectories);

                        if (files.Length > 0)
                            return files[0];
                    }
                    catch { }
                }
            }
            catch { }

            return null;
        }
    }
}
