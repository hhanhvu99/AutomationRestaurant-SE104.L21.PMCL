using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DoAnCuoiKi
{
    /// <summary>
    /// Interaction logic for BusBoy.xaml
    /// </summary>
    /// 

    class TableStatus
    {
        public int id { get; set; }
        public string status { get; set; }
    }

    public partial class BusBoy : Window
    {
        // Current selected table
        int currentSelected = -1;
        CancellationToken token = new CancellationToken();
        List<TableStatus> listTable = new List<TableStatus>();
        Button[] tableButton = new Button[6];

        Thread thread = new Thread(() => { });
        Thread threadMain = new Thread(() => { });

        public BusBoy()
        {
            InitializeComponent();
            this.userName.Text = "Null";

            var dueTime = TimeSpan.FromSeconds(0);
            var interval = TimeSpan.FromMilliseconds(500);
            this.doneButton.IsEnabled = false;

            // Setup table button
            tableButton[0] = this.Table1;
            tableButton[1] = this.Table2;
            tableButton[2] = this.Table3;
            tableButton[3] = this.Table4;
            tableButton[4] = this.Table5;
            tableButton[5] = this.Table6;

            threadMain = new Thread(() => {
                RunPeriodicAsync(Refresh, dueTime, interval, token);
            });
            threadMain.Start();
        }
        public BusBoy(string fname)
        {
            InitializeComponent();
            this.userName.Text = fname;

            var dueTime = TimeSpan.FromSeconds(0);
            var interval = TimeSpan.FromMilliseconds(500);
            this.doneButton.IsEnabled = false;

            // Setup table button
            tableButton[0] = this.Table1;
            tableButton[1] = this.Table2;
            tableButton[2] = this.Table3;
            tableButton[3] = this.Table4;
            tableButton[4] = this.Table5;
            tableButton[5] = this.Table6;

            threadMain = new Thread(() => {
                RunPeriodicAsync(Refresh, dueTime, interval, token);
            });
            threadMain.Start();
        }

        // Running thread
        private static async void RunPeriodicAsync(Action onTick, TimeSpan dueTime, TimeSpan interval, CancellationToken token)
        {
            // Initial wait time before we begin the periodic loop.
            if (dueTime > TimeSpan.Zero)
                await Task.Delay(dueTime, token);

            // Repeat this loop until cancelled.
            while (!token.IsCancellationRequested)
            {
                // Call our onTick function.
                onTick?.Invoke();

                // Wait to repeat again.
                if (interval > TimeSpan.Zero)
                    await Task.Delay(interval, token);
            }
        }

        // Query execution
        private bool QueryExecute(string command, DynamicParameters parameter)
        {
            using (MySqlConnection cnn = new MySqlConnection(SqlDataAccess.LoadConnectionString()))
            {
                try
                {
                    cnn.Open();
                    cnn.Execute(command, parameter);
                }
                catch (MySqlException ex)
                {
                    if (token.IsCancellationRequested)
                        return false;
                    Dispatcher.Invoke(() =>
                    {
                        switch (ex.ErrorCode)
                        {
                            case 1062:
                                System.Windows.MessageBox.Show("This ID has already in the table. Please choose another one.");
                                break;

                            default:
                                this.confirmStatus.Text = "Lost connection to the server.";
                                break;
                        }
                    });
                    
                    return false;
                }
 
            }

            return true;
        }

        private List<TableStatus> QueryExecuteReturn(string command, DynamicParameters parameter)
        {
            using (MySqlConnection cnn = new MySqlConnection(SqlDataAccess.LoadConnectionString()))
            {
                try
                {
                    cnn.Open();
                }
                catch
                {
                    if (token.IsCancellationRequested)
                        return new List<TableStatus>();
                    Dispatcher.Invoke(() =>
                    {
                        this.confirmStatus.Text = "Lost connection to the server.";
                    });

                    return new List<TableStatus>();
                }

                var temp = cnn.Query<TableStatus>(command, parameter);
                return temp.ToList();

            }

        }

        // Main loop
        private void Refresh()
        {
            // Get all table information in the database
            string command = "SELECT id, status FROM _table";
            listTable = QueryExecuteReturn(command, null);

            // If don't receive any response
            if (listTable.Count == 0)
            {
                if (token.IsCancellationRequested)
                    return;
                Dispatcher.Invoke(() =>
                {
                    this.doneButton.IsEnabled = false;
                });
                return;
            }

            if (token.IsCancellationRequested)
                return;

            Dispatcher.Invoke(() =>
            {
                if (currentSelected != -1)
                {
                    this.tableNumber.Text = this.listTable[currentSelected - 1].id.ToString();
                    this.tableStatus.Text = this.listTable[currentSelected - 1].status;

                    if (this.tableStatus.Text.Equals("Dirty"))
                        this.doneButton.IsEnabled = true;
                    else
                        this.doneButton.IsEnabled = false;

                    this.confirmStatus.Text = "";
                }

                // Set background button
                for (int i=0; i<6; ++i)
                {
                    if (this.listTable[i].status.Equals("Dirty"))
                    {
                        tableButton[i].Background = Brushes.Brown;
                    }
                    else
                    {
                        tableButton[i].Background = Brushes.LightGray;
                    }
                }

            });
            
        }

        // This function get data from database and set all the text accordingly, if table's status is "Dirty" then enable Done button
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (thread.IsAlive)
                return;

            currentSelected = ((Button)sender).Name.Last() - '0';

            thread = new Thread(Refresh);
            thread.Start();
        }

        // This function update table's status to "Empty" in the database and disable Done button
        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            if (thread.IsAlive)
                return;

            // Send query
            string command = "UPDATE _table SET status = 'Empty' WHERE id = @id";
            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@id", this.listTable[currentSelected - 1].id);

            thread = new Thread(() =>
            {
                QueryExecute(command, parameter);
                Refresh();
            });
            thread.Start();

        }

        // Send user back to the login screen
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            Login loginWindow = new Login();
            App.Current.MainWindow = loginWindow;
            loginWindow.Show();

            token.ThrowIfCancellationRequested();
            this.Close();
        }
    }
}
