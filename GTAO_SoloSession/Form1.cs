using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTAO_SoloSession
{
    public partial class Form1 : Form
    {
        private readonly Button btn;
        private readonly NumericUpDown seconds;
        private readonly Label status;
        private bool running = false;

        public Form1()
        {
            Text = "GTAO Solo Session";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            ClientSize = new Size(420, 220);

            var title = new Label
            {
                Text = "GTAO Solo Session",
                Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(16, 14)
            };

            var target = new Label
            {
                Text = "Cible : GTA5.exe",
                AutoSize = true,
                Location = new Point(16, 55)
            };

            var secLabel = new Label
            {
                Text = "Durée (secondes) :",
                AutoSize = true,
                Location = new Point(16, 85)
            };

            seconds = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 60,
                Value = 10,
                Location = new Point(160, 83),
                Width = 60
            };

            btn = new Button
            {
                Text = "Suspendre 10s",
                Location = new Point(16, 125),
                Size = new Size(180, 45)
            };
            btn.Click += async (_, __) => await SuspendResumeAsync();

            status = new Label
            {
                Text = "Statut : prêt",
                AutoSize = true,
                Location = new Point(16, 185)
            };

            seconds.ValueChanged += (_, __) => btn.Text = $"Suspendre {seconds.Value:0}s";

            Controls.Add(title);
            Controls.Add(target);
            Controls.Add(secLabel);
            Controls.Add(seconds);
            Controls.Add(btn);
            Controls.Add(status);
        }

        private async Task SuspendResumeAsync()
        {
            if (running) return;
            running = true;

            try
            {
                btn.Enabled = false;
                seconds.Enabled = false;

                var list = Process.GetProcessesByName("GTA5");
                if (list.Length == 0)
                {
                    status.Text = "Statut : GTA5.exe introuvable (lance le jeu d'abord)";
                    return;
                }

                var proc = list[0];
                int secs = (int)seconds.Value;

                status.Text = $"Statut : suspension ({secs}s)…";
                ProcessHelper.SuspendProcess(proc);

                for (int i = secs; i >= 1; i--)
                {
                    status.Text = $"Statut : suspendu… reprise dans {i}s";
                    await Task.Delay(1000);
                }

                ProcessHelper.ResumeProcess(proc);
                status.Text = "Statut : repris ✅";
            }
            catch (Exception ex)
            {
                status.Text = $"Erreur : {ex.Message}";
            }
            finally
            {
                btn.Enabled = true;
                seconds.Enabled = true;
                running = false;
            }
        }
    }
}
