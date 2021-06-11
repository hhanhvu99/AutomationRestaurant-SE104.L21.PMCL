using System;
using System.Collections.Generic;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Dapper;
using System.Threading;
using System.Text.RegularExpressions;

namespace DoAnCuoiKi
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    /// 

    // Hold information about returned data, the returned query must be similar to this
    class LoginResult
    {
        public string type { get; set; }
        public string fname { get; set; }
    }

    public partial class Login : Window
    {
        Thread thread = new Thread(() =>{});
        public Login()
        {
            InitializeComponent();
        }

        // Check if input string is a number
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // Main loop for executing this module
        private void MainExecute()
        {
            int id = 0;
            string pass = "";
            var type = "";
            var fname = "";

            // Get id and password from the form
            Dispatcher.Invoke(() =>
            {
                id = Convert.ToInt32(usernameID.Text);
                pass = password.Password.ToString();  
            });
            
            // Connect to Database
            using (MySqlConnection cnn = new MySqlConnection(SqlDataAccess.LoadConnectionString()))
            {
                // Try to connect to the database
                try
                {
                    cnn.Open();
                }
                catch
                {
                    Dispatcher.Invoke(() =>
                    {
                        this.loginStatus.Text = "Lost connection to the server.";
                    });
                    cnn.Close();
                    return;
                }

                // Query parameter
                DynamicParameters parameter = new DynamicParameters();
                parameter.Add("@id", id);
                parameter.Add("@password", pass);

                // Class LoginResult uses to hold return result
                var result = cnn.Query<LoginResult>("SELECT type, fname FROM employee WHERE id=@id AND password=@password", parameter);
                result.ToList();

                // If there is result
                if (result.Count() != 0)
                {
                    // Get data
                    type = result.FirstOrDefault().type;
                    fname = result.FirstOrDefault().fname;
                }

            }

            Dispatcher.Invoke(() =>
            {
                // Change screen according to the account's type
                switch (type)
                {
                    case "Manager":
                        Manager managerWindow = new Manager(fname);
                        App.Current.MainWindow = managerWindow;
                        managerWindow.Show();
                        this.Close();

                        break;

                    case "Host":
                        Host hostWindow = new Host(fname);
                        App.Current.MainWindow = hostWindow;
                        hostWindow.Show();
                        this.Close();

                        break;

                    case "Waiter":
                        Waiter waiterWindow = new Waiter(fname, id);
                        App.Current.MainWindow = waiterWindow;
                        waiterWindow.Show();
                        this.Close();

                        break;

                    case "Cook":
                        Cook cookWindow = new Cook(fname);
                        App.Current.MainWindow = cookWindow;
                        cookWindow.Show();
                        this.Close();

                        break;

                    case "Busboy":
                        BusBoy busWindow = new BusBoy(fname);
                        App.Current.MainWindow = busWindow;
                        busWindow.Show();
                        this.Close();

                        break;

                    default:
                        loginStatus.Text = "Wrong information. Please try again.";

                        break;
                }
            });

        }

        // Button click function
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.thread.IsAlive)
            {
                thread = new Thread(MainExecute);
                thread.Start();
            }
  
        }
    }
}
