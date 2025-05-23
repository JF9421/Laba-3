using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;

namespace TimeTracker
{
    public partial class ShowAllTasksForm : Form
    {
        private readonly string _connectionString;
        public ShowAllTasksForm(string connectionString)
        {
            InitializeComponent();
            _connectionString = connectionString;
            Load += ShowAllTasksForm_Load;
        }

        private void ShowAllTasksForm_Load(object sender, EventArgs e)
        {
            FillDataGridWithTasks();
        }

        private void FillDataGridWithTasks()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                var selectSql = "SELECT task, date, duration, comment FROM task";

                using (var cmd = new SQLiteCommand(selectSql, conn))
                using (var da = new SQLiteDataAdapter(cmd))
                {
                    var dt = new System.Data.DataTable();
                    da.Fill(dt);
                    dataGridViewHistory.DataSource = dt;
                }
            }
        }
    }
}
