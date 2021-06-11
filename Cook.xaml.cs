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
    /// Interaction logic for Cook.xaml
    /// </summary>
    /// 

    class MenuOrder
    {
        public int id { get; set; }
        public string menu { get; set; }
    }

    public partial class Cook : Window
    {
        List<MenuOrder> listOrder = new List<MenuOrder>();
        Thread thread = new Thread(() => { });
        bool lostConnetion = false;

        public Cook()
        {
            InitializeComponent();
            this.userName.Text = "Null";
            this.doneButton.IsEnabled = false;
        }
        public Cook(string fname)
        {
            InitializeComponent();
            this.userName.Text = fname;
            this.doneButton.IsEnabled = false;
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
                    Dispatcher.Invoke(() =>
                    {
                        switch (ex.ErrorCode)
                        {
                            case 1062:
                                System.Windows.MessageBox.Show("This ID has already in the table. Please choose another one.");
                                break;

                            default:
                                this.foodHolder.Text = "Lost connection to the server.";
                                break;
                        }
                    });
                    

                    return false;
                }

            }

            return true;
        }

        private List<MenuOrder> QueryGetOrder(string command, DynamicParameters parameter)
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
                        this.foodHolder.Text = "Lost connection to the server.";
                        lostConnetion = true;
                    });

                    return new List<MenuOrder>();
                }

                var temp = cnn.Query<MenuOrder>(command, parameter);
                lostConnetion = false;

                return temp.ToList();

            }

        }

        // Main loop
        private void Refresh()
        {
            // Get pending order
            string command = "SELECT orderdetail.orderid as id, GROUP_CONCAT(CONCAT(food.name, ':', orderdetail.quantity) SEPARATOR ', ') as menu FROM food, orderdetail WHERE food.id = orderdetail.foodid AND orderdetail.status <> 'Done' GROUP BY orderdetail.orderid";
            listOrder = QueryGetOrder(command, null);

            // Check connection
            if (lostConnetion)
                return;

            Dispatcher.Invoke(() =>
            {
                if (listOrder.Count != 0)
                {
                    this.foodHolder.Text = listOrder[0].menu;
                    this.orderButton.IsEnabled = false;
                    this.doneButton.IsEnabled = true;
                }
                else
                {
                    this.foodHolder.Text = "There is no order left.";
                    this.orderButton.IsEnabled = true;
                    this.doneButton.IsEnabled = false;
                }
            });
            
        }

        // This function get order from orderdetail in FIFO order. Only get orders which have "Waiting" status and have same orderid
        // Set text to dishes's name and its quantity get from the order
        // Enable Done button and disable Get Order button
        // If there aren't any order left that have status that is "Waiting", set text to "There is no order left."
        private void GetOrderButton_Click(object sender, RoutedEventArgs e)
        {
            thread = new Thread(Refresh);
            thread.Start();
        }

        // This function update order's status that have the same orderid in orderdetail to "Done", and update timeready in _order
        // Enable Get Order button and disable Done button
        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            string command = "UPDATE orderdetail SET status = 'Done' WHERE orderid = @orderid; UPDATE _order SET timeready = CURRENT_TIMESTAMP WHERE id = @orderid";
            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@orderid", listOrder[0].id);

            thread = new Thread(() =>
            {
                QueryExecute(command, parameter);
                Refresh();
            });
            thread.Start();
        }

        // Sen user to the login screen
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            Login loginWindow = new Login();
            App.Current.MainWindow = loginWindow;
            loginWindow.Show();
            this.Close();
        }
    }
}
