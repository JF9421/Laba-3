using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TimeTracker
{
    public partial class FormMain : Form
    {
        private TimerService _timerService = new TimerService();
        private TaskManager _taskManager = new TaskManager();
        private HistoryService _historyService = new HistoryService();
        private DateTime? _trackStart;
        private string _currentTask;
        private string _currentTheme = "light";

        private Timer breakTimer;
        private int breakIntervalMin = 60;

        private Timer pomodoroTimer = new Timer();
        private int pomodoroSecondsLeft = 25 * 60;
        private bool pomodoroRunning = false;

        public FormMain()
        {
            InitializeComponent();
            _timerService.OnTick += UpdateTime;

            _taskManager.AddTask("Работа");
            _taskManager.AddTask("Учёба");
            _taskManager.AddTask("Отдых");

            comboBoxTasks.Items.AddRange(_taskManager.Tasks.ToArray());
            if (comboBoxTasks.Items.Count > 0)
                comboBoxTasks.SelectedIndex = 0;

            breakTimer = new Timer();
            breakTimer.Interval = breakIntervalMin * 60 * 1000;
            breakTimer.Tick += (s, e) =>
            {
                breakTimer.Stop();
                MessageBox.Show("Время сделать перерыв!", "Перерыв", MessageBoxButtons.OK, MessageBoxIcon.Information);
                breakTimer.Start();
            };
            breakTimer.Start();

            this.Resize += new System.EventHandler(this.FormMain_Resize);

            pomodoroTimer.Interval = 1000;
            pomodoroTimer.Tick += PomodoroTimer_Tick;
            lblPomodoro.Text = "25:00";

            ApplyTheme("light");
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            _currentTask = comboBoxTasks.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(_currentTask)) return;
            _trackStart = DateTime.Now;
            _timerService.Start();
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            _timerService.Pause();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _timerService.Pause();
            string comment = "";
            if (_trackStart.HasValue && !string.IsNullOrEmpty(_currentTask))
            {
                comment = Prompt.ShowDialog("Комментарий к сессии (необязательно):", "Комментарий");
                _historyService.AddRecord(_currentTask, _trackStart.Value, _timerService.GetElapsed(), comment);
            }
            _timerService.Stop();
            _trackStart = null;
            UpdateHistory();
        }

        private void btnAddTask_Click(object sender, EventArgs e)
        {
            string input = Prompt.ShowDialog("Введите название новой задачи:", "Добавить задачу");
            if (!string.IsNullOrWhiteSpace(input))
            {
                _taskManager.AddTask(input);
                comboBoxTasks.Items.Add(input);
            }
        }

        private void btnRemoveTask_Click(object sender, EventArgs e)
        {
            if (comboBoxTasks.SelectedItem != null)
            {
                string task = comboBoxTasks.SelectedItem.ToString();
                _taskManager.RemoveTask(task);
                comboBoxTasks.Items.Remove(task);
                if (comboBoxTasks.Items.Count > 0)
                    comboBoxTasks.SelectedIndex = 0;
            }
        }

        private void btnEditTask_Click(object sender, EventArgs e)
        {
            if (comboBoxTasks.SelectedItem != null)
            {
                string oldTask = comboBoxTasks.SelectedItem.ToString();
                string newTask = Prompt.ShowDialog("Новое название для задачи:", "Переименовать задачу", oldTask);
                if (!string.IsNullOrWhiteSpace(newTask) && newTask != oldTask)
                {
                    _taskManager.EditTask(oldTask, newTask);
                    int idx = comboBoxTasks.SelectedIndex;
                    comboBoxTasks.Items[idx] = newTask;
                    comboBoxTasks.SelectedIndex = idx;
                }
            }
        }

        private void btnExportCsv_Click(object sender, EventArgs e)
        {
            if (_historyService.Records.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта.", "Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV файл (*.csv)|*.csv";
                sfd.FileName = "История.csv";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        StringBuilder csv = new StringBuilder();
                        csv.AppendLine("Задача;Время старта;Длительность;Комментарий");
                        foreach (var rec in _historyService.Records)
                        {
                            csv.AppendLine($"{rec.Task};{rec.StartTime};{rec.Duration};{rec.Comment}");
                        }
                        File.WriteAllText(sfd.FileName, csv.ToString(), Encoding.UTF8);
                        MessageBox.Show("Экспорт завершён!", "Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка экспорта: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnImportCsv_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "CSV файл (*.csv)|*.csv";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var lines = File.ReadAllLines(ofd.FileName, Encoding.UTF8);
                        int imported = 0;
                        foreach (var line in lines.Skip(1))
                        {
                            var parts = line.Split(';');
                            if (parts.Length >= 4)
                            {
                                string task = parts[0];
                                DateTime startTime;
                                TimeSpan duration;
                                string comment = parts[3];

                                if (DateTime.TryParse(parts[1], out startTime) &&
                                    TimeSpan.TryParse(parts[2], out duration))
                                {
                                    _historyService.AddRecord(task, startTime, duration, comment);
                                    imported++;
                                }
                            }
                        }
                        UpdateHistory();
                        MessageBox.Show($"Импорт завершён! Импортировано записей: {imported}", "Импорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка импорта: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnPomodoro_Click(object sender, EventArgs e)
        {
            if (pomodoroRunning)
            {
                pomodoroTimer.Stop();
                pomodoroRunning = false;
                btnPomodoro.Text = "Помидорка";
                lblPomodoro.Text = "25:00";
                pomodoroSecondsLeft = 25 * 60;
            }
            else
            {
                pomodoroSecondsLeft = 25 * 60;
                lblPomodoro.Text = "25:00";
                pomodoroTimer.Start();
                pomodoroRunning = true;
                btnPomodoro.Text = "Стоп 🍅";
            }
        }

        private void PomodoroTimer_Tick(object sender, EventArgs e)
        {
            if (pomodoroSecondsLeft > 0)
            {
                pomodoroSecondsLeft--;
                int min = pomodoroSecondsLeft / 60;
                int sec = pomodoroSecondsLeft % 60;
                lblPomodoro.Text = $"{min:D2}:{sec:D2}";
            }
            else
            {
                pomodoroTimer.Stop();
                pomodoroRunning = false;
                btnPomodoro.Text = "Помидорка";
                lblPomodoro.Text = "25:00";
                pomodoroSecondsLeft = 25 * 60;
                System.Media.SystemSounds.Exclamation.Play();
                MessageBox.Show("Таймер Pomodoro завершён! Сделайте перерыв.", "Помидорка!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnStats_Click(object sender, EventArgs e)
        {
            var statsForm = new StatsForm(_historyService);
            statsForm.ShowDialog();
        }

        private void btnTheme_Click(object sender, EventArgs e)
        {
            if (_currentTheme == "light")
                ApplyTheme("dark");
            else
                ApplyTheme("light");
        }

        private void ApplyTheme(string theme)
        {
            _currentTheme = theme;
            if (_currentTheme == "dark")
            {
                this.BackColor = Color.FromArgb(45, 45, 48);
                foreach (Control c in this.Controls)
                {
                    c.ForeColor = Color.White;
                    c.BackColor = (c is Button) ? Color.FromArgb(63, 63, 70) : Color.FromArgb(45, 45, 48);
                }
                btnTheme.Text = "Тема: Тёмная";
                dataGridViewHistory.BackgroundColor = Color.FromArgb(45, 45, 48);
                dataGridViewHistory.DefaultCellStyle.BackColor = Color.FromArgb(45, 45, 48);
                dataGridViewHistory.DefaultCellStyle.ForeColor = Color.White;
                dataGridViewHistory.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(63, 63, 70);
                dataGridViewHistory.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dataGridViewHistory.EnableHeadersVisualStyles = false;
            }
            else
            {
                this.BackColor = SystemColors.Control;
                foreach (Control c in this.Controls)
                {
                    c.ForeColor = Color.Black;
                    if (c is Button b)
                    {
                        if (b == btnStart) b.BackColor = Color.LightGreen;
                        else if (b == btnPause) b.BackColor = Color.Khaki;
                        else if (b == btnStop) b.BackColor = Color.LightCoral;
                        else if (b == btnAddTask) b.BackColor = Color.Silver;
                        else if (b == btnRemoveTask) b.BackColor = Color.Silver;
                        else if (b == btnEditTask) b.BackColor = Color.Silver;
                        else if (b == btnExportCsv) b.BackColor = Color.LightSteelBlue;
                        else if (b == btnImportCsv) b.BackColor = Color.LightSteelBlue;
                        else if (b == btnStats) b.BackColor = Color.Silver;
                        else if (b == btnTheme) b.BackColor = Color.Silver;
                        else if (b == btnPomodoro) b.BackColor = Color.OrangeRed;
                        else if (b == btnDeleteRecord) b.BackColor = Color.Silver;
                        else if (b == btnEditRecord) b.BackColor = Color.Silver;
                    }
                    else
                        c.BackColor = SystemColors.Control;
                }
                btnTheme.Text = "Тема: Светлая";
                dataGridViewHistory.BackgroundColor = SystemColors.Window;
                dataGridViewHistory.DefaultCellStyle.BackColor = SystemColors.Window;
                dataGridViewHistory.DefaultCellStyle.ForeColor = Color.Black;
                dataGridViewHistory.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control;
                dataGridViewHistory.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
                dataGridViewHistory.EnableHeadersVisualStyles = false;
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            UpdateHistory(txtSearch.Text);
        }

        private void UpdateTime(TimeSpan ts)
        {
            lblTime.Text = ts.ToString(@"hh\:mm\:ss");
        }

        private void UpdateHistory(string filter = "")
        {
            dataGridViewHistory.Rows.Clear();
            var filtered = _historyService.Records;
            if (!string.IsNullOrWhiteSpace(filter))
            {
                filtered = filtered.Where(r =>
                    (r.Task?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                    || (r.Comment?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                    || r.StartTime.ToString().Contains(filter)
                ).ToList();
            }
            foreach (var rec in filtered)
            {
                dataGridViewHistory.Rows.Add(rec.Task, rec.StartTime, rec.Duration, rec.Comment);
            }
        }

        private void lblTime_Click(object sender, EventArgs e)
        {
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Shift | Keys.S))
            {
                btnStart.PerformClick();
                return true;
            }
            else if (keyData == (Keys.Control | Keys.Shift | Keys.P))
            {
                btnPause.PerformClick();
                return true;
            }
            else if (keyData == (Keys.Control | Keys.Shift | Keys.T))
            {
                btnStop.PerformClick();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void FormMain_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(1000, "Тайм-Трекер", "Приложение свернуто в трей", ToolTipIcon.Info);
            }
            else
            {
                notifyIcon1.Visible = false;
            }
        }

        private void toolStripMenuItemOpen_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        private void toolStripMenuItemExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // Новое: удаление записи истории
        private void btnDeleteRecord_Click(object sender, EventArgs e)
        {
            if (dataGridViewHistory.SelectedRows.Count > 0)
            {
                int idx = dataGridViewHistory.SelectedRows[0].Index;
                if (idx >= 0 && idx < _historyService.Records.Count)
                {
                    if (MessageBox.Show("Удалить выбранную запись?", "Удаление", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        _historyService.Records.RemoveAt(idx);
                        UpdateHistory();
                    }
                }
            }
            else
            {
                MessageBox.Show("Выделите запись для удаления.", "Удаление", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Новое: редактирование записи истории
        private void btnEditRecord_Click(object sender, EventArgs e)
        {
            if (dataGridViewHistory.SelectedRows.Count > 0)
            {
                int idx = dataGridViewHistory.SelectedRows[0].Index;
                if (idx >= 0 && idx < _historyService.Records.Count)
                {
                    var record = _historyService.Records[idx];

                    string task = Prompt.ShowDialog("Задача:", "Редактировать", record.Task);
                    string startTimeStr = Prompt.ShowDialog("Время старта (yyyy-MM-dd HH:mm:ss):", "Редактировать", record.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    string durationStr = Prompt.ShowDialog("Длительность (hh:mm:ss):", "Редактировать", record.Duration.ToString(@"hh\:mm\:ss"));
                    string comment = Prompt.ShowDialog("Комментарий:", "Редактировать", record.Comment);

                    if (!string.IsNullOrWhiteSpace(task) &&
                        DateTime.TryParse(startTimeStr, out var st) &&
                        TimeSpan.TryParse(durationStr, out var dur))
                    {
                        record.Task = task;
                        record.StartTime = st;
                        record.Duration = dur;
                        record.Comment = comment;
                        UpdateHistory();
                    }
                    else
                    {
                        MessageBox.Show("Некорректные данные!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выделите запись для редактирования.", "Редактирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    public static class Prompt
    {
        public static string ShowDialog(string text, string caption, string defaultValue = "")
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterParent
            };
            Label textLabel = new Label() { Left = 20, Top = 20, Text = text, Width = 340 };
            TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 340, Text = defaultValue };
            Button confirmation = new Button() { Text = "OK", Left = 210, Width = 70, Top = 80, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Отмена", Left = 290, Width = 70, Top = 80, DialogResult = DialogResult.Cancel };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(cancel);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;
            prompt.CancelButton = cancel;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }
    }
}