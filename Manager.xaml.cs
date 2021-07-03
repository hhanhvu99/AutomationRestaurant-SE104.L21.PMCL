using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Controls.DataVisualization.Charting;
using System.Threading;
using MySql.Data.MySqlClient;

namespace DoAnCuoiKi
{
    /// <summary>
    /// Interaction logic for Manager.xaml
    /// </summary>
    /// 

    // Các class lưu info
    class EmployeeInfo
    {
        public int id { get; set; }
        public string fname { get; set; }
        public string lname { get; set; }
        public int wage { get; set; }
        public string password { get; set; }
        public string type { get; set; }
        public int hoursWork { get; set; }

        public string fullname
        {
            get
            {
                return $"{fname} {lname}";
            }
        }
    }

    class MenuItem
    {
        public int id { get; set; }
        public string name { get; set; }
        public string ingredient { get; set; }
        public float price { get; set; }
        public string status { get; set; }

    }

    class Inventory
    {
        public int id { get; set; }
        public string name { get; set; }
        public int quantity { get; set; }
    }

    class SalesChart
    {
        public DateTime timeOrdered { get; set; }
        public int total { get; set; }
    }

    class PopularityChart
    {
        public DateTime timeOrdered { get; set; }
        public int id { get; set; }
        public int quantity { get; set; }
    }

    class DishesName
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    class CustomerTimeChart
    {
        public DateTime timeOrdered { get; set; }
        public DateTime timeFinished { get; set; }
    }

    class PrepareTimeChart
    {
        public DateTime timeOrdered { get; set; }
        public DateTime timeReady { get; set; }
    }

    public partial class Manager : Window
    {
        string oldAccountID = "";
        string oldIngredient = "";
        int lastFoodID = 0;
        int oldFoodID = 0;
        int oldIndex = -1;

        Thread thread = new Thread(() => { });

        public Manager()
        {
            InitializeComponent();

        }
        public Manager(string fname)
        {
            InitializeComponent();
            this.userName.Text = fname;
        }

        // When main tab changes, it automatically gets data from the database
        private void MainTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Prevent double query
            if (e != null)
                if (!(e.Source is TabControl))
                    return;

            int currentTabIndex = -1;
            TabItem currentTab = null;
            oldIndex = -1;

            // Get current tab
            Dispatcher.Invoke(() =>
            {
                currentTabIndex = this.mainTab.SelectedIndex;
                currentTab = (TabItem)(this.mainTab).SelectedItem;
            });

            using (MySqlConnection cnn = new MySqlConnection(SqlDataAccess.LoadConnectionString()))
            {
                // Try connect to the database
                try
                {
                    cnn.Open();
                }
                catch
                {
                    Dispatcher.Invoke(() =>
                    {
                        this.confirmStatus.Text = "Connection Lost.";
                    });

                    return;
                }

                // Get data query according to the current tab
                Dispatcher.Invoke(() =>
                {
                    this.confirmStatus.Text = "";

                    switch (currentTabIndex)
                    {
                        // Account
                        case 0:
                            var accountResult = cnn.Query<EmployeeInfo>("SELECT * FROM employee ORDER BY employee.type");
                            var accountTable = (ListView)currentTab.FindName("accountTable");

                            accountTable.ItemsSource = null;
                            accountTable.ItemsSource = accountResult.ToList();

                            break;

                        // Menu
                        case 1:
                            var menuResult = cnn.Query<MenuItem>("SELECT food.id as id, food.name as name, group_concat(CONCAT(ingredients.name, ': ', foodXingredients.ingredientsQuantityNeed) SEPARATOR ', ') as ingredient, food.price as price, food.status as status FROM food, foodXingredients, ingredients WHERE foodXingredients.foodid = food.id AND foodXingredients.ingredientsID = ingredients.id GROUP BY id");
                            var menuTable = (ListView)currentTab.FindName("menuTable");

                            menuTable.ItemsSource = null;
                            menuTable.ItemsSource = menuResult.ToList();

                            this.lastFoodID = menuResult.Last().id;

                            break;

                        // Paycheck
                        case 3:
                            var paycheckResult = cnn.Query<EmployeeInfo>("SELECT * FROM employee, payroll WHERE employee.id = payroll.employeeID ORDER BY employee.type");
                            var paycheckTable = (ListView)currentTab.FindName("paycheckTable");

                            paycheckTable.ItemsSource = null;
                            paycheckTable.ItemsSource = paycheckResult.ToList();

                            break;

                        // The rest
                        case 4:
                            RefreshChart();

                            break;
                    }
                });

                // Always update
                // Inventory
                Dispatcher.Invoke(() =>
                {
                    var inventoryResult = cnn.Query<Inventory>("SELECT * FROM ingredients ORDER BY id");
                    var inventoryTable = this.inventoryTable;

                    inventoryTable.ItemsSource = null;
                    inventoryTable.ItemsSource = inventoryResult.ToList();
                });
                    
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
                    Dispatcher.Invoke(() =>
                    {
                        switch (ex.ErrorCode)
                        {
                            case 19:
                                System.Windows.MessageBox.Show("This ID has already in the table. Please choose another one.");
                                break;

                            case 275:
                                System.Windows.MessageBox.Show("The amount is lower than 0. Please choose another one.");
                                break;

                            default:
                                this.confirmStatus.Text = "Connection Lost.";
                                break;
                        }
                    });
                        

                    return false;
                }
                
            }

            return true;
        }

        private List<dynamic> QueryExecuteReturn(string command, DynamicParameters parameter)
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
                        this.confirmStatus.Text = "Connection Lost.";
                    });

                    return new List<dynamic>();
                }

                var temp = cnn.Query(command, parameter);
                return temp.ToList();

            }
            
        }

        private List<SalesChart> QueryGetSales(string command, DynamicParameters parameter)
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
                        this.confirmStatus.Text = "Connection Lost.";
                    });

                    return new List<SalesChart>();
                }

                var temp = cnn.Query<SalesChart>(command, parameter);
                return temp.ToList();

            }
        }

        private List<PopularityChart> QueryGetPopularity(string command, DynamicParameters parameter)
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
                        this.confirmStatus.Text = "Connection Lost.";
                    });

                    return new List<PopularityChart>();
                }

                var temp = cnn.Query<PopularityChart>(command, parameter);
                return temp.ToList();

            }
        }

        private List<DishesName> QueryFoodName(string command, DynamicParameters parameter)
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
                        this.confirmStatus.Text = "Connection Lost.";
                    });

                    return new List<DishesName>();
                }

                var temp = cnn.Query<DishesName>(command, parameter);
                return temp.ToList();

            }
        }

        private List<CustomerTimeChart> QueryAvegareTime(string command, DynamicParameters parameter)
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
                        this.confirmStatus.Text = "Connection Lost.";
                    });

                    return new List<CustomerTimeChart>();
                }

                var temp = cnn.Query<CustomerTimeChart>(command, parameter);
                return temp.ToList();

            }
        }

        private List<PrepareTimeChart> QueryPrepareTime(string command, DynamicParameters parameter)
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
                        this.confirmStatus.Text = "Connection Lost.";
                    });

                    return new List<PrepareTimeChart>();
                }

                var temp = cnn.Query<PrepareTimeChart>(command, parameter);
                return temp.ToList();

            }
        }

        // Check if input string is number or not
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TextValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^a-zA-Z]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        ///////////////////////////////////////////////////////////////////////////////
        // ----------------------------Statistic Tab-------------------------------- //
        ///////////////////////////////////////////////////////////////////////////////

        // Refresh the chart 
        private void RefreshChart()
        {
            string command = "";

            // Hourly check
            if (this.hourlyCheck.IsChecked == true)
            {
                switch (this.statisticTab.SelectedIndex)
                {
                    // Sales report
                    case 0:
                        command = "SELECT tableOrder.timeordered, total FROM (SELECT timeordered FROM _order ORDER BY id DESC LIMIT 1) AS lastRecord, (SELECT * FROM _order WHERE status = 'Done') AS tableOrder WHERE tableOrder.timeordered > lastRecord.timeordered - INTERVAL 1 DAY";
                        List<SalesChart> chartSales = QueryGetSales(command, null);
                        int[] salesArray = new int[24];
                        KeyValuePair<int, int>[] chartSalesSource = new KeyValuePair<int, int>[24];

                        if (this.confirmStatus.Text == "Connection Lost.")
                            return;

                        foreach (var record in chartSales)
                        {
                            double diffInHour = (chartSales.Last().timeOrdered - record.timeOrdered).TotalHours;
                            int index = 23 - (int)Math.Floor(diffInHour);

                            salesArray[index] += record.total;
                        }

                        // Insert to chart < Hour, Sales >
                        for (int i = 0; i < salesArray.Length; ++i)
                        {
                            chartSalesSource[i] = new KeyValuePair<int, int>(i, salesArray[i]);
                        }

                        ((LineSeries)this.saleChart.Series[0]).ItemsSource = chartSalesSource;

                        break;

                    // Popularity
                    case 1:
                        // Get record
                        command = "SELECT tableOrder.timeordered, tableOrder.id, tableOrder.quantity FROM (SELECT timeordered FROM _order ORDER BY id DESC LIMIT 1) AS lastRecord, (SELECT timeordered, food.id, quantity FROM _order, orderdetail, food WHERE _order.status = 'Done' AND _order.id = orderdetail.orderid AND orderdetail.foodid = food.id) AS tableOrder WHERE tableOrder.timeordered > lastRecord.timeordered - INTERVAL 1 DAY ORDER BY tableOrder.timeordered";
                        List<PopularityChart> popularity = QueryGetPopularity(command, null);
                        Dictionary<int, int>[] popularityArray = new Dictionary<int, int>[24];

                        // Get food name
                        command = "SELECT id, CONCAT(name, '-', status) as name FROM food ";
                        List<DishesName> foodName = QueryFoodName(command, null);

                        if (this.confirmStatus.Text == "Connection Lost.")
                            return;

                        // Create dictionary
                        for (int i = 0; i < 24; ++i)
                        {
                            popularityArray[i] = new Dictionary<int, int>();

                            foreach (var food in foodName)
                            {
                                popularityArray[i].Add(food.id, 0);
                            }
                        }

                        // Count dishes
                        foreach (var record in popularity)
                        {
                            double diffInHour = (popularity.Last().timeOrdered - record.timeOrdered).TotalHours;
                            int index = 23 - (int)Math.Floor(diffInHour);

                            popularityArray[index][record.id] += record.quantity;
                            
                        }

                        // Insert to chart 
                        KeyValuePair<int, int>[] chartPopularitySource = null;
                        this.popularityChart.Series.Clear();

                        foreach (var food in foodName)
                        {
                            chartPopularitySource = new KeyValuePair<int, int>[24];

                            for (int i = 0; i < popularityArray.Length; ++i)
                            {
                                chartPopularitySource[i] = new KeyValuePair<int, int>(i, popularityArray[i][food.id]);
                            }

                            LineSeries lineSeries = new LineSeries();
                            lineSeries.Title = food.name;
                            lineSeries.DependentValuePath = "Value";
                            lineSeries.IndependentValuePath = "Key";
                            lineSeries.ItemsSource = chartPopularitySource;
                            popularityChart.Series.Add(lineSeries);

                        }

                        break;

                    // Customer Time
                    case 2:
                        command = "SELECT tableOrder.timeordered, tableOrder.timefinished FROM (SELECT timeordered FROM _order ORDER BY id DESC LIMIT 1) AS lastRecord, (SELECT timeordered, timefinished FROM _order WHERE _order.status = 'Done') AS tableOrder WHERE tableOrder.timeordered > lastRecord.timeordered - INTERVAL 1 DAY";
                        List<CustomerTimeChart> customerTime = QueryAvegareTime(command, null);

                        if (this.confirmStatus.Text == "Connection Lost.")
                            return;

                        double[] customerArray = new double[24];
                        KeyValuePair<int, double>[] customerTimeSource = new KeyValuePair<int, double>[24];
                        int count = 1;
                        int oldIndex = -1;
                        CustomerTimeChart last = customerTime.Last();

                        foreach (var record in customerTime)
                        {
                            double diffInHour = (customerTime.Last().timeOrdered - record.timeOrdered).TotalHours;
                            int index = 23 - (int)Math.Floor(diffInHour);

                            if (oldIndex == -1)
                            {
                                customerArray[index] += (record.timeFinished - record.timeOrdered).TotalMinutes;

                                oldIndex = index;
                                count = 1;
                            }
                            else if (index != oldIndex)
                            {
                                customerArray[oldIndex] = Math.Round(customerArray[oldIndex] / count, 2);
                                customerArray[index] += (record.timeFinished - record.timeOrdered).TotalMinutes;

                                oldIndex = index;
                                count = 1;
                            }
                            else
                            {
                                customerArray[index] += (record.timeFinished - record.timeOrdered).TotalMinutes;
                                count += 1;
                            }
                                
                            if (record.Equals(last))
                            {
                                customerArray[index] = Math.Round(customerArray[index] / count, 2);
                            }
                        }

                        // Insert to chart 
                        for (int i = 0; i < customerArray.Length; ++i)
                        {
                            customerTimeSource[i] = new KeyValuePair<int, double>(i, customerArray[i]);
                        }

                        ((ColumnSeries)this.turnAroundTimeChart.Series[0]).ItemsSource = customerTimeSource;

                        break;

                    // Prepare Time
                    case 3:
                        command = "SELECT tableOrder.timeordered, tableOrder.timeready FROM (SELECT timeordered FROM _order ORDER BY id DESC LIMIT 1) AS lastRecord, (SELECT timeordered, timeready FROM _order WHERE _order.status = 'Done') AS tableOrder WHERE tableOrder.timeordered > lastRecord.timeordered - INTERVAL 1 DAY";
                        List<PrepareTimeChart> prepareTime = QueryPrepareTime(command, null);

                        if (this.confirmStatus.Text == "Connection Lost.")
                            return;

                        double[] prepareTimeArray = new double[24];
                        KeyValuePair<int, double>[] prepareTimeSource = new KeyValuePair<int, double>[24];
                        int count3 = 1;
                        int oldIndex3 = -1;
                        PrepareTimeChart last3 = prepareTime.Last();

                        foreach (var record in prepareTime)
                        {
                            double diffInHour = (prepareTime.Last().timeOrdered - record.timeOrdered).TotalHours;
                            int index = 23 - (int)Math.Floor(diffInHour);

                            if (oldIndex3 == -1)
                            {
                                prepareTimeArray[index] += (record.timeReady - record.timeOrdered).TotalMinutes;

                                oldIndex3 = index;
                                count3 = 1;
                            }
                            else if (index != oldIndex3)
                            {
                                prepareTimeArray[oldIndex3] = Math.Round(prepareTimeArray[oldIndex3] / count3, 2);
                                prepareTimeArray[index] += (record.timeReady - record.timeOrdered).TotalMinutes;

                                oldIndex3 = index;
                                count3 = 1;
                            }
                            else
                            {
                                prepareTimeArray[index] += (record.timeReady - record.timeOrdered).TotalMinutes;
                                count3 += 1;
                            }

                            if (record.Equals(last3))
                            {
                                prepareTimeArray[index] = Math.Round(prepareTimeArray[index] / count3, 2);
                            }
                        }

                        // Insert to chart 
                        for (int i = 0; i < prepareTimeArray.Length; ++i)
                        {
                            prepareTimeSource[i] = new KeyValuePair<int, double>(i, prepareTimeArray[i]);
                        }

                        ((ColumnSeries)this.prepareTimeChart.Series[0]).ItemsSource = prepareTimeSource;

                        break;
                }

                
                
            }
            // Monthly check
            else
            {
                switch (this.statisticTab.SelectedIndex)
                {
                    // Sales report
                    case 0:
                        command = "SELECT tableOrder.timeordered, total FROM (SELECT timeordered FROM _order ORDER BY id DESC LIMIT 1) AS lastRecord, (SELECT * FROM _order WHERE status = 'Done') AS tableOrder WHERE tableOrder.timeordered > lastRecord.timeordered - INTERVAL 21 DAY";
                        List<SalesChart> chartSales = QueryGetSales(command, null);
                        int[] salesArray = new int[21];
                        KeyValuePair<int, int>[] chartSalesSource = new KeyValuePair<int, int>[21];

                        if (this.confirmStatus.Text == "Connection Lost.")
                            return;

                        foreach (var record in chartSales)
                        {
                            double diffInDay = (chartSales.Last().timeOrdered - record.timeOrdered).TotalDays;
                            int index = 20 - (int)Math.Floor(diffInDay);

                            salesArray[index] += record.total;
                        }

                        // Insert to chart < Hour, Sales >
                        for (int i = 0; i < salesArray.Length; ++i)
                        {
                            chartSalesSource[i] = new KeyValuePair<int, int>(i, salesArray[i]);
                        }

                        ((LineSeries)this.saleChart.Series[0]).ItemsSource = chartSalesSource;

                        break;

                    // Popularity
                    case 1:
                        // Get record
                        command = "SELECT tableOrder.timeordered, tableOrder.id, tableOrder.quantity FROM (SELECT timeordered FROM _order ORDER BY id DESC LIMIT 1) AS lastRecord, (SELECT timeordered, food.id, quantity FROM _order, orderdetail, food WHERE _order.status = 'Done' AND _order.id = orderdetail.orderid AND orderdetail.foodid = food.id) AS tableOrder WHERE tableOrder.timeordered > lastRecord.timeordered - INTERVAL 21 DAY ORDER BY tableOrder.timeordered";
                        List<PopularityChart> popularity = QueryGetPopularity(command, null);
                        Dictionary<int, int>[] popularityArray = new Dictionary<int, int>[21];

                        // Get food name
                        command = "SELECT id, CONCAT(name, '-', status) as name FROM food ";
                        List<DishesName> foodName = QueryFoodName(command, null);

                        if (this.confirmStatus.Text == "Connection Lost.")
                            return;

                        // Create dictionary
                        for (int i = 0; i < 21; ++i)
                        {
                            popularityArray[i] = new Dictionary<int, int>();

                            foreach (var food in foodName)
                            {
                                popularityArray[i].Add(food.id, 0);
                            }
                        }


                        // Count dishes
                        foreach (var record in popularity)
                        {
                            double diffInDay = (popularity.Last().timeOrdered - record.timeOrdered).TotalDays;
                            int index = 20 - (int)Math.Floor(diffInDay);

                            popularityArray[index][record.id] += record.quantity;

                        }

                        // Insert to chart 
                        KeyValuePair<int, int>[] chartPopularitySource = null;
                        this.popularityChart.Series.Clear();

                        foreach (var food in foodName)
                        {
                            chartPopularitySource = new KeyValuePair<int, int>[21];

                            for (int i = 0; i < popularityArray.Length; ++i)
                            {
                                chartPopularitySource[i] = new KeyValuePair<int, int>(i, popularityArray[i][food.id]);
                            }

                            LineSeries lineSeries = new LineSeries();
                            lineSeries.Title = food.name;
                            lineSeries.DependentValuePath = "Value";
                            lineSeries.IndependentValuePath = "Key";
                            lineSeries.ItemsSource = chartPopularitySource;
                            popularityChart.Series.Add(lineSeries);
                        }

                        break;

                    // Customer Time
                    case 2:
                        command = "SELECT tableOrder.timeordered, tableOrder.timefinished FROM (SELECT timeordered FROM _order ORDER BY id DESC LIMIT 1) AS lastRecord, (SELECT timeordered, timefinished FROM _order WHERE _order.status = 'Done') AS tableOrder WHERE tableOrder.timeordered > lastRecord.timeordered - INTERVAL 21 DAY";
                        List<CustomerTimeChart> customerTime = QueryAvegareTime(command, null);

                        if (this.confirmStatus.Text == "Connection Lost.")
                            return;

                        double[] customerArray = new double[21];
                        KeyValuePair<int, double>[] customerTimeSource = new KeyValuePair<int, double>[21];
                        int count = 1;
                        int oldIndex = -1;
                        CustomerTimeChart last = customerTime.Last();

                        foreach (var record in customerTime)
                        {
                            double diffInDay = (customerTime.Last().timeOrdered - record.timeOrdered).TotalDays;
                            int index = 20 - (int)Math.Floor(diffInDay);

                            if (oldIndex == -1)
                            {
                                customerArray[index] += (record.timeFinished - record.timeOrdered).TotalMinutes;

                                oldIndex = index;
                                count = 1;
                            }
                            else if (index != oldIndex)
                            {
                                customerArray[oldIndex] = Math.Round(customerArray[oldIndex] / count, 2);
                                customerArray[index] += (record.timeFinished - record.timeOrdered).TotalMinutes;

                                oldIndex = index;
                                count = 1;
                            }
                            else
                            {
                                customerArray[index] += (record.timeFinished - record.timeOrdered).TotalMinutes;
                                count += 1;
                            }

                            if (record.Equals(last))
                            {
                                customerArray[index] = Math.Round(customerArray[index] / count, 2);
                            }
                        }

                        // Insert to chart 
                        for (int i = 0; i < customerArray.Length; ++i)
                        {
                            customerTimeSource[i] = new KeyValuePair<int, double>(i, customerArray[i]);
                        }

                        ((ColumnSeries)this.turnAroundTimeChart.Series[0]).ItemsSource = customerTimeSource;

                        break;

                    // Prepare Time
                    case 3:
                        command = "SELECT tableOrder.timeordered, tableOrder.timeready FROM (SELECT timeordered FROM _order ORDER BY id DESC LIMIT 1) AS lastRecord, (SELECT timeordered, timeready FROM _order WHERE _order.status = 'Done') AS tableOrder WHERE tableOrder.timeordered > lastRecord.timeordered - INTERVAL 21 DAY";
                        List<PrepareTimeChart> prepareTime = QueryPrepareTime(command, null);

                        if (this.confirmStatus.Text == "Connection Lost.")
                            return;

                        double[] prepareTimeArray = new double[21];
                        KeyValuePair<int, double>[] prepareTimeSource = new KeyValuePair<int, double>[21];
                        int count3 = 1;
                        int oldIndex3 = -1;
                        PrepareTimeChart last3 = prepareTime.Last();

                        foreach (var record in prepareTime)
                        {
                            double diffInDay = (prepareTime.Last().timeOrdered - record.timeOrdered).TotalDays;
                            int index = 20 - (int)Math.Floor(diffInDay);

                            if (oldIndex3 == -1)
                            {
                                prepareTimeArray[index] += (record.timeReady - record.timeOrdered).TotalMinutes;

                                oldIndex3 = index;
                                count3 = 1;
                            }
                            else if (index != oldIndex3)
                            {
                                prepareTimeArray[oldIndex3] = Math.Round(prepareTimeArray[oldIndex3] / count3, 2);
                                prepareTimeArray[index] += (record.timeReady - record.timeOrdered).TotalMinutes;

                                oldIndex3 = index;
                                count3 = 1;
                            }
                            else
                            {
                                prepareTimeArray[index] += (record.timeReady - record.timeOrdered).TotalMinutes;
                                count3 += 1;
                            }

                            if (record.Equals(last3))
                            {
                                prepareTimeArray[index] = Math.Round(prepareTimeArray[index] / count3, 2);
                            }
                        }

                        // Insert to chart 
                        for (int i = 0; i < prepareTimeArray.Length; ++i)
                        {
                            prepareTimeSource[i] = new KeyValuePair<int, double>(i, prepareTimeArray[i]);
                        }

                        ((ColumnSeries)this.prepareTimeChart.Series[0]).ItemsSource = prepareTimeSource;

                        break;

                }

                
            }
            
        }

        /////////////////////////////////////////////////////////////////////////////
        // ----------------------------Account Tab-------------------------------- //
        /////////////////////////////////////////////////////////////////////////////

        // Fire when selected row in list's account changes in Account tab
        private void AccountTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lost connection or thread is running
            if (this.confirmStatus.Text == "Connection Lost." || thread.IsAlive)
                return;

            EmployeeInfo currentItem = (EmployeeInfo)((ListView)sender).SelectedItem;

            // If there is not a selected row
            if (currentItem == null)
            {
                if (oldIndex != -1)
                    ((ListView)sender).SelectedIndex = oldIndex;
                else
                {
                    this.accountID.Text = "";
                    this.accountPassword.Text = "";
                    this.accountFirstName.Text = "";
                    this.accountLastName.Text = "";
                    this.accountType.SelectedIndex = -1;
                }
            }

            // If there is a selected row
            if (currentItem != null)
            {
                // Set text box
                oldIndex = ((ListView)sender).SelectedIndex;
                this.accountID.Text = this.oldAccountID = currentItem.id.ToString();
                this.accountPassword.Text = currentItem.password;
                this.accountFirstName.Text = currentItem.fname;
                this.accountLastName.Text = currentItem.lname;

                foreach (ComboBoxItem comboItem in this.accountType.Items)
                {
                    if (comboItem.Content.ToString() == currentItem.type)
                    {
                        this.accountType.SelectedItem = comboItem;
                        break;
                    }
                }

            }

        }

        // Function Create Button in Account tab
        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            // Lost connection or thread is running
            if (this.confirmStatus.Text == "Connection Lost." || thread.IsAlive)
                return;

            // Check if all the field all fill up
            if (this.accountID.Text != "" && this.accountPassword.Text != "" && this.accountType.SelectedItem != null && this.accountFirstName.Text != "" && this.accountLastName.Text != "")
            {
                DynamicParameters parameter = new DynamicParameters();
                parameter.Add("@id", Convert.ToInt32(this.accountID.Text));
                parameter.Add("@fname", this.accountFirstName.Text);
                parameter.Add("@lname", this.accountLastName.Text);
                parameter.Add("@password", this.accountPassword.Text);
                parameter.Add("@type", ((ComboBoxItem)this.accountType.SelectedItem).Content);

                // Send query
                string command = "INSERT INTO employee VALUES(@id, @fname, @lname, 0, @password, @type); INSERT INTO payroll VALUES(@id, 0)";
                bool result = QueryExecute(command, parameter);

                if (result)
                {
                    this.accountID.Text = "";
                    this.accountPassword.Text = "";
                    this.accountFirstName.Text = "";
                    this.accountLastName.Text = "";
                    this.accountType.SelectedIndex = -1;
                }

                // Refresh table
                thread = new Thread(() =>
                {
                    MainTab_SelectionChanged(this.mainTab, null);
                });
                thread.Start();
                
            }
            else
            {
                System.Windows.MessageBox.Show("Please fill in all the data.");
            }

        }

        // Function Edit Button in Account tab
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            // Lost connection or thread is running
            if (this.confirmStatus.Text == "Connection Lost." || thread.IsAlive)
                return;

            // If there is a selected row
            if (this.accountTable.SelectedValue != null)
            {
                // Check if edit ID
                if (this.oldAccountID != this.accountID.Text)
                {
                    System.Windows.MessageBox.Show("Please do not edit the ID field.");
                    this.accountID.Text = this.oldAccountID;
                    return;
                }

                DynamicParameters parameter = new DynamicParameters();
                parameter.Add("@id", Convert.ToInt32(this.accountID.Text));
                parameter.Add("@fname", this.accountFirstName.Text);
                parameter.Add("@lname", this.accountLastName.Text);
                parameter.Add("@password", this.accountPassword.Text);
                parameter.Add("@type", ((ComboBoxItem)this.accountType.SelectedItem).Content);

                // Send query
                string command = "UPDATE employee SET fname = @fname, lname= @lname, password = @password, type = @type WHERE id = @id";
                bool result = QueryExecute(command, parameter);

                if (result)
                {
                    this.accountID.Text = "";
                    this.accountPassword.Text = "";
                    this.accountFirstName.Text = "";
                    this.accountLastName.Text = "";
                    this.accountType.SelectedIndex = -1;
                }

                // Refresh table
                thread = new Thread(() =>
                {
                    MainTab_SelectionChanged(this.mainTab, null);
                });
                thread.Start();
            }
            else
            {
                System.Windows.MessageBox.Show("Please choose something in the table.");
            }

            
        }

        // Function Delete Button in Account tab
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // Lost connection or thread is running
            if (this.confirmStatus.Text == "Connection Lost." || thread.IsAlive)
                return;

            // If there is a selected row
            if (this.accountTable.SelectedValue != null)
            {
                // Check if edit ID 
                if (this.oldAccountID != this.accountID.Text)
                {
                    System.Windows.MessageBox.Show("Please do not edit the ID field.");
                    this.accountID.Text = this.oldAccountID;
                    return;
                }

                DynamicParameters parameter = new DynamicParameters();
                parameter.Add("@id", Convert.ToInt32(this.accountID.Text));
                parameter.Add("@fname", this.accountFirstName.Text);
                parameter.Add("@lname", this.accountLastName.Text);
                parameter.Add("@password", this.accountPassword.Text);
                parameter.Add("@type", ((ComboBoxItem)this.accountType.SelectedItem).Content);

                // Check if the employee is in service
                string commandCheck = "SELECT COUNT(1) as checking FROM employee, _table WHERE employee.id = @id AND employee.id = _table.employeeid";
                bool inservice = QueryExecuteReturn(commandCheck, parameter).FirstOrDefault().checking > 0;
                if (inservice)
                {
                    System.Windows.MessageBox.Show("This employee is currently in service.");
                    return;
                }
  
                // Send query
                string command = "DELETE FROM payroll WHERE employeeID = @id; DELETE FROM employee WHERE id = @id";
                bool result = QueryExecute(command, parameter);

                if (result)
                {
                    this.accountID.Text = "";
                    this.accountPassword.Text = "";
                    this.accountFirstName.Text = "";
                    this.accountLastName.Text = "";
                    this.accountType.SelectedIndex = -1;
                }

                // Refresh table
                thread = new Thread(() =>
                {
                    MainTab_SelectionChanged(this.mainTab, null);
                });
                thread.Start();
            }
            else
            {
                System.Windows.MessageBox.Show("Please choose something in the table.");
            }

        }

        //////////////////////////////////////////////////////////////////////////
        // ----------------------------Menu Tab-------------------------------- //
        //////////////////////////////////////////////////////////////////////////

        // Split string menu to food name and food quantity
        private List<Tuple<string, int>> SplitRecipe(string recipe)
        {
            string[] ingredientList = recipe.Split(',');
            List<Tuple<string, int>> listOfIngredients = new List<Tuple<string, int>>();

            foreach (string ingredient in ingredientList)
            {
                string[] elementList = ingredient.Split(':');
                string[] ingredientNameList = elementList[0].Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);

                string ingredientName = String.Join(" ", ingredientNameList).ToLower();
                int quantity = Convert.ToInt32(elementList[1]);

                Tuple<string, int> ingredientTuple = new Tuple<string, int>(ingredientName, quantity);
                listOfIngredients.Add(ingredientTuple);
            }

            return listOfIngredients;
        }

        private int GetIngredientID(string ingredient)
        {
            foreach (Inventory item in this.inventoryTable.Items)
            {
                if (item.name.Equals(ingredient))
                {
                    return item.id;
                }
            }

            return -1;
        }

        // Fire when selected row in list's menu changes in Menu tab
        private void MenuTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lost connection
            if (this.confirmStatus.Text == "Connection Lost.")
                return;

            MenuItem currentItem = (MenuItem)((ListView)sender).SelectedItem;

            // If not select an row
            if (currentItem == null)
            {
                if (oldIndex != -1)
                    ((ListView)sender).SelectedIndex = oldIndex;
                else
                {
                    this.foodName.Text = "";
                    this.ingredientText.Text = "";
                    this.price.Text = "";
                }
            }

            // If select an row
            if (currentItem != null)
            {
                // Set text box
                oldIndex = ((ListView)sender).SelectedIndex;
                this.oldFoodID = currentItem.id;
                this.foodName.Text = currentItem.name;
                this.ingredientText.Text = currentItem.ingredient;
                this.price.Text = currentItem.price.ToString();
            }
            
        }

        // Function Add Button in Menu tab
        private void AddMenuButton_Click(object sender, RoutedEventArgs e)
        {
            // Lost connection or thread is running
            if (this.confirmStatus.Text == "Connection Lost." || thread.IsAlive)
                return;

            // If select an row
            if ((MenuItem)this.menuTable.SelectedValue != null)
            {
                // If dish is inactive then set to active
                if (((MenuItem)this.menuTable.SelectedValue).status == "Inactive")
                {
                    int foodID = ((MenuItem)this.menuTable.SelectedValue).id;
                    DynamicParameters parameter = new DynamicParameters();
                    parameter.Add("@foodid", foodID);

                    string command = "UPDATE food SET status = 'Active' WHERE id = @foodid";
                    QueryExecute(command, parameter);

                    // Refresh table
                    thread = new Thread(() =>
                    {
                        MainTab_SelectionChanged(this.mainTab, null);
                    });
                    thread.Start();
                    return;
                }
            }        

            // If all the field are filled
            if (this.foodName.Text != "" && this.ingredientText.Text != "" && this.price.Text != "")
            {
                int foodID = this.lastFoodID + 1;

                DynamicParameters parameter = new DynamicParameters();
                parameter.Add("@foodid", foodID);
                parameter.Add("@dish", this.foodName.Text.ToLower());
                parameter.Add("@ingredientsID");
                parameter.Add("@ingredientsQuantity");
                parameter.Add("@price", Convert.ToInt32(this.price.Text));

                // Check ingredient 
                var listOfIngredientsID = new List<Tuple<int, int>>();
                var listOfIngredients = SplitRecipe(this.ingredientText.Text);

                foreach (var ingredient in listOfIngredients)
                {
                    int ingredientID = GetIngredientID(ingredient.Item1);

                    if (ingredientID == -1)
                    {
                        System.Windows.MessageBox.Show("Ingredients not found.");
                        return;
                    }

                    listOfIngredientsID.Add(new Tuple<int, int>(ingredientID, ingredient.Item2));
                }

                // Insert food to menu
                string command = "INSERT INTO food(id, name, price) VALUES(@foodid, @dish, @price)";
                bool result = QueryExecute(command, parameter);

                if (!result)
                    return;

                // Get needed ingredients and added to the dish's recipe in the database
                foreach (var ingredient in listOfIngredientsID)
                {
                    parameter.Add("@ingredientsID", ingredient.Item1);
                    parameter.Add("@ingredientsQuantity", ingredient.Item2);

                    command = "INSERT INTO foodXingredients VALUES (@foodid, @ingredientsID, @ingredientsQuantity)";
                    result = QueryExecute(command, parameter);
                        
                }
                
                if (result)
                {
                    this.foodName.Text = "";
                    this.ingredientText.Text = "";
                    this.price.Text = "";
                }

                // Refresh table
                thread = new Thread(() =>
                {
                    MainTab_SelectionChanged(this.mainTab, null);
                });
                thread.Start();
            }
            else
            {
                System.Windows.MessageBox.Show("Please fill in all the data.");
            }

        }

        // Function Update Button in Menu tab
        private void UpdateMenuButton_Click(object sender, RoutedEventArgs e)
        {
            // Lost connection or thread is running
            if (this.confirmStatus.Text == "Connection Lost." || thread.IsAlive)
                return;

            if (this.oldFoodID == 0)
            {
                System.Windows.MessageBox.Show("Please choose a dish to edit.");
                return;
            }

            if (((MenuItem)this.menuTable.SelectedValue).status == "Inactive")
            {
                System.Windows.MessageBox.Show("Please choose a dish that is not inactive.");
                return;
            }

            if (this.foodName.Text != "" && this.ingredientText.Text != "" && this.price.Text != "")
            {
                DynamicParameters parameter = new DynamicParameters();
                parameter.Add("@foodid", this.oldFoodID);
                parameter.Add("@dish", this.foodName.Text.ToLower());
                parameter.Add("@ingredientsID");
                parameter.Add("@ingredientsQuantity");
                parameter.Add("@price", Convert.ToInt32(this.price.Text));

                // Check ingredient
                var listOfIngredientsID = new List<Tuple<int, int>>();
                var listOfIngredients = SplitRecipe(this.ingredientText.Text);

                foreach (var ingredient in listOfIngredients)
                {
                    int ingredientID = GetIngredientID(ingredient.Item1);

                    if (ingredientID == -1)
                    {
                        System.Windows.MessageBox.Show("Ingredients not found.");
                        return;
                    }

                    listOfIngredientsID.Add(new Tuple<int, int>(ingredientID, ingredient.Item2));
                }

                // Delete ingredient from dish's recipe
                string command = "DELETE FROM foodXingredients WHERE foodid = @foodid";
                bool result = QueryExecute(command, parameter);

                if (!result)
                    return;

                // Edit food in menu
                command = "UPDATE food SET name = @dish, price = @price WHERE id = @foodid";
                result = QueryExecute(command, parameter);

                // Get new dish's ingredients and add to the database
                foreach (var ingredient in listOfIngredientsID)
                {
                    parameter.Add("@ingredientsID", ingredient.Item1);
                    parameter.Add("@ingredientsQuantity", ingredient.Item2);

                    command = "INSERT INTO foodXingredients VALUES (@foodid, @ingredientsID, @ingredientsQuantity)";
                    result = QueryExecute(command, parameter);

                }

                if (result)
                {
                    this.foodName.Text = "";
                    this.ingredientText.Text = "";
                    this.price.Text = "";
                }

                // Refresh table
                thread = new Thread(() =>
                {
                    MainTab_SelectionChanged(this.mainTab, null);
                });
                thread.Start();
            }
            else
            {
                System.Windows.MessageBox.Show("Please fill in all the data.");
            }

        }

        // Function Delete Button in Menu tab
        private void DeleteMenuButton_Click(object sender, RoutedEventArgs e)
        {
            // Lost connection or thread is running
            if (this.confirmStatus.Text == "Connection Lost." || thread.IsAlive)
                return;

            if (this.oldFoodID == 0)
            {
                System.Windows.MessageBox.Show("Please choose a dish to delete.");
                return;
            }

            if (this.foodName.Text != "" && this.ingredientText.Text != "" && this.price.Text != "")
            {
                DynamicParameters parameter = new DynamicParameters();
                parameter.Add("@foodid", this.oldFoodID);

                // Set seleted dish to inactive
                string command = "UPDATE food SET status = 'Inactive' WHERE id = @foodid";
                bool result = QueryExecute(command, parameter);

                if (result)
                {
                    this.foodName.Text = "";
                    this.ingredientText.Text = "";
                    this.price.Text = "";
                }

                // Refresh table
                thread = new Thread(() =>
                {
                    MainTab_SelectionChanged(this.mainTab, null);
                });
                thread.Start();
            }
            else
            {
                System.Windows.MessageBox.Show("Please fill in all the data.");
            }
  
        }

        ///////////////////////////////////////////////////////////////////////////////
        // ----------------------------Inventory Tab-------------------------------- //
        ///////////////////////////////////////////////////////////////////////////////

        // Fire when selected row in list's inventory changes in Inventory tab
        private void InventoryTable_SelectionChanged(object sender, RoutedEventArgs e)
        {
            // Lost connection or thread is running
            if (this.confirmStatus.Text == "Connection Lost.")
                return;

            Inventory currentItem = (Inventory)((ListView)sender).SelectedItem;

            // If not select a row
            if (currentItem == null)
            {
                if (oldIndex != -1)
                    ((ListView)sender).SelectedIndex = oldIndex;
                else
                {
                    this.ingredientName.Text = "";
                    this.ingredientQuantity.Text = "";
                }
            }

            // If select a row
            if (currentItem != null)
            {
                // Set text box
                oldIndex = ((ListView)sender).SelectedIndex;
                this.ingredientName.Text = this.oldIngredient = currentItem.name;
                this.ingredientQuantity.Text = currentItem.quantity.ToString();
            }
            
        }

        // Function Add Button in Inventory tab
        private void AddIngredientButton_Click(object sender, RoutedEventArgs e)
        {
            // Lost connection or thread is running
            if (this.confirmStatus.Text == "Connection Lost." || thread.IsAlive)
                return;

            // Check if all the field are filled
            if (this.ingredientName.Text != "" && this.ingredientQuantity.Text != "")
            {
                DynamicParameters parameter = new DynamicParameters();
                parameter.Add("@name", this.ingredientName.Text.ToLower());
                parameter.Add("@quantity", Convert.ToInt32(this.ingredientQuantity.Text));

                // Send query
                string command = "INSERT INTO ingredients (name, quantity) VALUES (@name, @quantity) ON DUPLICATE KEY UPDATE quantity = quantity + @quantity";
                bool result = QueryExecute(command, parameter);

                if (result)
                {
                    this.ingredientName.Text = "";
                    this.ingredientQuantity.Text = "";
                }

                // Refresh table
                thread = new Thread(() =>
                {
                    MainTab_SelectionChanged(this.mainTab, null);
                });
                thread.Start();
            }
            else
            {
                System.Windows.MessageBox.Show("Please fill in all the data.");
            }

            
        }

        // Function Update Button in Inventory tab
        private void UpdateIngredientButton_Click(object sender, RoutedEventArgs e)
        {
            // Lost connection or thread is running
            if (this.confirmStatus.Text == "Connection Lost." || thread.IsAlive)
                return;

            // Check if all the field are filled
            if (this.ingredientName.Text != "" && this.ingredientQuantity.Text != "")
            {
                DynamicParameters parameter = new DynamicParameters();
                parameter.Add("@name", this.ingredientName.Text.ToLower());
                parameter.Add("@quantity", Convert.ToInt32(this.ingredientQuantity.Text));

                // Check if edit the Name field
                if (this.ingredientName.Text.ToLower() != this.oldIngredient)
                {
                    System.Windows.MessageBox.Show("Please do not edit the Name field.");
                    this.ingredientName.Text = this.oldIngredient;
                    return;
                }

                // Send query
                string command = "UPDATE ingredients SET quantity = @quantity WHERE name = @name";
                bool result = QueryExecute(command, parameter);

                if (result)
                {
                    this.ingredientName.Text = "";
                    this.ingredientQuantity.Text = "";
                }

                // Refresh table
                thread = new Thread(() =>
                {
                    MainTab_SelectionChanged(this.mainTab, null);
                });
                thread.Start();
            }
            else
            {
                System.Windows.MessageBox.Show("Please fill in all the data.");
            }

            
        }

        //////////////////////////////////////////////////////////////////////////////
        // ----------------------------Paycheck Tab-------------------------------- //
        //////////////////////////////////////////////////////////////////////////////

        // Fire when selected row in list's employeee changes in Paycheck tab
        private void PaycheckTable_SelectionChanged(object sender, RoutedEventArgs e)
        {
            // Lost connection or thread is running
            if (this.confirmStatus.Text == "Connection Lost.")
                return;

            EmployeeInfo currentItem = (EmployeeInfo)((ListView)sender).SelectedItem;

            // If not select a row
            if (currentItem == null)
            {
                if (oldIndex != -1)
                    ((ListView)sender).SelectedIndex = oldIndex;
                else
                {
                    this.employeeID.Text = "";
                    this.hourWork.Text = "";
                    this.salary.Text = "";
                }
            }

            // If select a row
            if (currentItem != null)
            {
                // Set text box
                oldIndex = ((ListView)sender).SelectedIndex;
                this.employeeID.Text = currentItem.id.ToString();
                this.hourWork.Text = currentItem.hoursWork.ToString();

                int salary = currentItem.hoursWork * 20000;
                this.salary.Text = salary.ToString();
            }
            
        }

        // Function Set Hour button in Paycheck tab
        private void SetHourButton_Click(object sender, RoutedEventArgs e)
        {
            // Lost connection or thread is running
            if (this.confirmStatus.Text == "Connection Lost." || thread.IsAlive)
                return;

            // Check if all field are filled
            if (this.employeeID.Text != "" && this.hourWork.Text != "")
            {
                DynamicParameters parameter = new DynamicParameters();
                parameter.Add("@id", Convert.ToInt32(this.employeeID.Text));
                parameter.Add("@hour", Convert.ToInt32(this.hourWork.Text));

                // Send query
                string command = "UPDATE payroll SET hoursWork = @hour WHERE employeeid = @id";
                bool result = QueryExecute(command, parameter);

                if (result)
                {
                    this.employeeID.Text = "";
                    this.hourWork.Text = "";
                    this.salary.Text = "";
                }

                // Refresh table
                thread = new Thread(() =>
                {
                    MainTab_SelectionChanged(this.mainTab, null);
                });
                thread.Start();
            }
            else
            {
                System.Windows.MessageBox.Show("Please choose an employee and fill in the hour.");
            }

        }

        // Function Pay Check button in Paycheck tab
        private void SetPaycheckButton_Click(object sender, RoutedEventArgs e)
        {
            // Lost connection or thread is running
            if (this.confirmStatus.Text == "Connection Lost." || thread.IsAlive)
                return;

            // Check if all field are filled
            if (this.employeeID.Text != "" && this.salary.Text != "")
            {
                DynamicParameters parameter = new DynamicParameters();
                parameter.Add("@id", Convert.ToInt32(this.employeeID.Text));
                parameter.Add("@salary", Convert.ToInt32(this.salary.Text));

                // Send query
                string command = "UPDATE employee SET wage = @salary WHERE id = @id";
                bool result = QueryExecute(command, parameter);

                if (result)
                {
                    this.employeeID.Text = "";
                    this.hourWork.Text = "";
                    this.salary.Text = "";
                }

                // Refresh table
                thread = new Thread(() =>
                {
                    MainTab_SelectionChanged(this.mainTab, null);
                });
                thread.Start();
            }
            else
            {
                System.Windows.MessageBox.Show("Please choose an employee and fill in the salary.");
            }

        }

        // Send user back to the login screen
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            Login loginWindow = new Login();
            App.Current.MainWindow = loginWindow;
            loginWindow.Show();
            this.Close();
        }

        
    }
}
