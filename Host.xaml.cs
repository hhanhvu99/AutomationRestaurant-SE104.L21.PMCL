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
    /// Interaction logic for Host.xaml
    /// </summary>
    /// 

    // Class to hold returned result
    class TableHost
    {
        public int id { get; set; }
        public int employeeid { get; set; }
        public string status { get; set; }
    }

    class EmployeeID
    {
        public int id { get; set; }
        public string fullname { get; set; }
    }

    public partial class Host : Window
    {
        // Current selected table
        int currentSelected = -1;
        CancellationToken token = new CancellationToken();
        // List of table
        List<TableHost> listTable = new List<TableHost>();
        // List of waiter
        List<EmployeeID> listEmployee = new List<EmployeeID>();
        // List of table's button
        Button[] tableButton = new Button[6];

        Thread thread = new Thread(() => { });
        Thread threadMain = new Thread(() => { });

        public Host()
        {
            InitializeComponent();
            this.userName.Text = "Null";

            this.confirmButton.IsEnabled = false;
            this.waiterBox.IsEnabled = false;

            var dueTime = TimeSpan.FromSeconds(0);
            var interval = TimeSpan.FromMilliseconds(500);

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
        public Host(string fname)
        {
            InitializeComponent();
            this.userName.Text = fname;

            this.confirmButton.IsEnabled = false;
            this.waiterBox.IsEnabled = false;

            var dueTime = TimeSpan.FromSeconds(0);
            var interval = TimeSpan.FromMilliseconds(500);

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

        // Run thread
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

        // Query execute
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

        private List<TableHost> QueryGetTable(string command, DynamicParameters parameter)
        {
            using (MySqlConnection cnn = new MySqlConnection(SqlDataAccess.LoadConnectionString()))
            {
                try
                {
                    cnn.Open();
                }
                catch
                {
                    Dispatcher.Invoke(() =>
                    {
                        
                        this.confirmStatus.Text = "Lost connection to the server.";
                    });

                    return new List<TableHost>();
                }
                
                var temp = cnn.Query<TableHost>(command, parameter);
                return temp.ToList();

            }

        }
        
        private List<EmployeeID> QueryGetEmployee(string command, DynamicParameters parameter)
        {
            using (MySqlConnection cnn = new MySqlConnection(SqlDataAccess.LoadConnectionString()))
            {
                try
                {
                    cnn.Open();
                }
                catch
                {
                    Dispatcher.Invoke(() =>
                    {

                        this.confirmStatus.Text = "Lost connection to the server.";
                    });

                    return new List<EmployeeID>();
                }

                var temp = cnn.Query<EmployeeID>(command, parameter);
                return temp.ToList();

            }

        }

        // Main loop
        private void Refresh()
        {
            // Get table information
            string command = "SELECT * FROM _table";
            listTable = QueryGetTable(command, null);

            // Get employee's information
            command = "SELECT id, CONCAT(fname, ' ', lname) as fullname FROM employee WHERE type = 'Waiter'";
            listEmployee = QueryGetEmployee(command, null);

            // Both need to have result
            if (listTable.Count * listEmployee.Count == 0)
                return;

            Dispatcher.Invoke(() =>
            {
                // If select a table
                if (currentSelected != -1)
                {
                    // Set all information to the box in the form
                    this.tableNumber.Text = this.listTable[currentSelected - 1].id.ToString();
                    this.tableStatus.Text = this.listTable[currentSelected - 1].status;
                    this.waiterBox.Items.Clear();

                    // If table is empty
                    if (this.tableStatus.Text.Equals("Empty"))
                    {
                        this.confirmButton.IsEnabled = true;
                        this.waiterBox.IsEnabled = true;

                        foreach (var employee in listEmployee)
                        {
                            this.waiterBox.Items.Add(employee.fullname + '-' + employee.id.ToString());
                        }

                        this.waiterBox.SelectedIndex = 0;
                    }
                    // If table is dirty
                    else if (this.tableStatus.Text.Equals("Dirty"))
                    {
                        this.confirmButton.IsEnabled = false;
                        this.waiterBox.IsEnabled = false;
                    }
                    // If table is eating, then show the waiter assigned to that table
                    else
                    {
                        this.confirmButton.IsEnabled = false;
                        this.waiterBox.IsEnabled = false;

                        EmployeeID result = listEmployee.Find(x => x.id == this.listTable[currentSelected - 1].employeeid);
                        this.waiterBox.Items.Add(result.fullname);
                        this.waiterBox.SelectedIndex = 0;
                    }

                    this.confirmStatus.Text = "";
                }

                // Set background button
                for (int i = 0; i < 6; ++i)
                {
                    if (this.listTable[i].status.Equals("Empty"))
                    {
                        tableButton[i].Background = Brushes.LightGray;
                    }
                    else
                    {
                        tableButton[i].Background = Brushes.Brown;
                    }
                }
            });
            
        }

        // This function get data from database and set all the corresponding text's box. If table' status is not empty then set conform button to unclickable
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (thread.IsAlive)
                return;

            currentSelected = ((Button)sender).Name.Last() - '0';

            thread = new Thread(Refresh);
            thread.Start();
        }

        // This function set Waiter to a table, send query to server to update table's waiter and table's status to Waiting
        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (thread.IsAlive)
                return;

            int id = currentSelected;
            string employeeID = this.waiterBox.SelectedValue.ToString().Split('-')[1];
            // Send query
            string command = "UPDATE _table SET employeeid = @employeeid, status = 'Waiting' WHERE id = @id";
            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@id", id);
            parameter.Add("@employeeid", employeeID);

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
