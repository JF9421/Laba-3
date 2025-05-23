using System;
using System.Linq;
using System.Windows.Forms;

namespace TimeTracker
{
    public partial class StatsForm : Form
    {
        public StatsForm(HistoryService history)
        {
            InitializeComponent();
            var stats = history.Records
                .GroupBy(r => r.Task)
                .OrderByDescending(g => g.Sum(r => r.Duration.TotalSeconds))
                .Select(g => $"{g.Key}: {TimeSpan.FromSeconds(g.Sum(r => r.Duration.TotalSeconds)):hh\\:mm\\:ss}")
                .ToArray();

            statsBox.Text = stats.Length == 0 ? "Нет данных." : string.Join(Environment.NewLine, stats);
        }

        private void InitializeComponent()
        {
            this.statsBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // statsBox
            // 
            this.statsBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statsBox.Multiline = true;
            this.statsBox.ReadOnly = true;
            this.statsBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.statsBox.Font = new System.Drawing.Font("Consolas", 12F);
            this.statsBox.BackColor = System.Drawing.Color.White;
            this.statsBox.ForeColor = System.Drawing.Color.Black;
            this.statsBox.Name = "statsBox";
            this.statsBox.Size = new System.Drawing.Size(384, 261);
            this.statsBox.TabIndex = 0;
            // 
            // StatsForm
            // 
            this.ClientSize = new System.Drawing.Size(384, 261);
            this.Controls.Add(this.statsBox);
            this.Name = "StatsForm";
            this.Text = "Статистика по задачам";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.TextBox statsBox;
    }
}