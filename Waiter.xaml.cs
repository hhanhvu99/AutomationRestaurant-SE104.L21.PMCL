using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for Waiter.xaml
    /// </summary>
    /// 

    class TableInfo
    {
        public int tableid { get; set; }
        public string foodStatus { get; set; }
        public string tableStatus { get; set; }
        public int waiterid { get; set; }
        public string tableOrder { get; set; }
        public float totalPrice { get; set; }
    }

    class Table
    {
        public int id { get; set; }
        public int employeeid { get; set; }
        public string status { get; set; }
        public string foodStatus { get; set; }
    }

    class FoodInfo
    {
        public FoodInfo(string name, int quantity, int id = 0)
        {
            this.name = name;
            this.quantity = quantity;
            this.id = id;
        }
        public string name { get; set; }
        public int quantity { get; set; }
        public int id { get; set; }

    }

    class OrderInfo
    {
        public OrderInfo(string name, int quantity, string recipe, int id)
        {
            this.name = name;
            this.quantity = quantity;
            this.id = id;
            this.recipe = recipe;
        }
        public string name { get; set; }
        public int quantity { get; set; }
        public int id { get; set; }
        public string recipe { get; set; }

    }

    class AvailableDish
    {
        public int id { get; set; }
        public string name { get; set; }
        public int ingredientsID { get; set; }
        public int ingredientsQuantityNeed { get; set; }
        public int quantityInStore { get; set; }
        public float price { get; set; }
    }

    class Dishes
    {
        public int id { get; set; }
        public string name { get; set; }
        public int quantity { get; set; }
        public float pricePerDish { get; set; }
        public string recipe { get; set; }
    }

    public partial class Waiter : Window
    {
        int waiterID = 0;
        // Current table selected
        int currentSelected = 1;
        // Previous table selected
        int oldSelected = -1;
        // Current selected food menu
        int selectedComboBox = -1;
        // Previous selected food menu
        int oldSelectedComboBox = -1;
        // Current selected order food
        int selectedOrderComboBox = -1;
        bool changeTextQuantity = false;

        CancellationToken token = new CancellationToken();

        TableInfo[] tableOccupied;
        Table[] assignedTable;
        List<Dishes> availableDishes = new List<Dishes>();
        List<OrderInfo> foodSource = new List<OrderInfo>();
        Button[] tableButton = new Button[6];

        Thread thread = new Thread(() => { });
        Thread threadMain = new Thread(() => { });

        // Setup
        public Waiter()
        {
            InitializeComponent();
            this.userName.Text = "Null";
            this.waiterID = 55548888;

            var dueTime = TimeSpan.FromSeconds(0);
            var interval = TimeSpan.FromMilliseconds(500);

            this.updateButton.IsEnabled = false;
            this.deleteButton.IsEnabled = false;
            this.orderButton.IsEnabled = false;
            this.deliverButton.IsEnabled = false;
            this.billButton.IsEnabled = false;

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
            threadMain.IsBackground = true;
            threadMain.Start();
        }
        public Waiter(string fname, int id)
        {
            InitializeComponent();
            this.userName.Text = fname;
            this.waiterID = id;

            var dueTime = TimeSpan.FromSeconds(0);
            var interval = TimeSpan.FromMilliseconds(500);

            this.updateButton.IsEnabled = false;
            this.deleteButton.IsEnabled = false;
            this.orderButton.IsEnabled = false;
            this.deliverButton.IsEnabled = false;
            this.billButton.IsEnabled = false;

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
            threadMain.IsBackground = true;
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

        // Check if input string is a number
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // Query execution
        private void CheckConnection()
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
                        return;

                    Dispatcher.Invoke(() =>
                    {
                        this.confirmStatus.Text = "Connection Lost.";
                    });

                    return;
                }

                if (token.IsCancellationRequested)
                    return;

                Dispatcher.Invoke(() =>
                {
                    this.confirmStatus.Text = "";
                });

            }
        }

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

        // Get data from database
        private List<TableInfo> QueryTableInfo(string command, DynamicParameters parameter)
        {
            using (MySqlConnection cnn = new MySqlConnection(SqlDataAccess.LoadConnectionString()))
            {
                // Try connecting to server
                try
                {
                    cnn.Open();
                }
                catch
                {
                    if (token.IsCancellationRequested)
                        return new List<TableInfo>();
                    Dispatcher.Invoke(() =>
                    {
                        this.confirmStatus.Text = "Connection Lost.";
                    });

                    return new List<TableInfo>();
                }

                var temp = cnn.Query<TableInfo>(command, parameter);
                return temp.ToList();

            }

        }

        private List<Table> QueryAssignedTable(string command, DynamicParameters parameter)
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
                        return new List<Table>();

                    Dispatcher.Invoke(() =>
                    {
                        this.confirmStatus.Text = "Connection Lost.";
                    });

                    return new List<Table>();
                }

                var temp = cnn.Query<Table>(command, parameter);
                return temp.ToList();

            }

        }

        private List<AvailableDish> QueryAvailableDish(string command, DynamicParameters parameter)
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
                        return new List<AvailableDish>();
                    Dispatcher.Invoke(() =>
                    {
                        this.confirmStatus.Text = "Connection Lost.";
                    });

                    return new List<AvailableDish>();
                }

                var temp = cnn.Query<AvailableDish>(command, parameter);
                return temp.ToList();

            }

        }

        private int QueryOrderID(string command, DynamicParameters parameter)
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

                    return -1;
                }

                int temp = cnn.QuerySingle<int>(command, parameter);
                return temp;

            }

        }

        // Convert string to list
        private List<FoodInfo> GetFoodInfo(string recipe)
        {
            string[] ingredientList = recipe.Split(',');
            List<FoodInfo> listOfIngredients = new List<FoodInfo>();

            foreach (string ingredient in ingredientList)
            {
                string[] elementList = ingredient.Split(':');
                string[] ingredientNameList = elementList[0].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                string ingredientName = String.Join(" ", ingredientNameList);
                int quantity = Convert.ToInt32(elementList[1]);

                FoodInfo ingredientTuple = new FoodInfo(ingredientName, quantity);
                listOfIngredients.Add(ingredientTuple);
            }

            return listOfIngredients;
        }

        // Main loop
        private void Refresh()
        {
            // Check connection
            bool lostConnection = false;
            CheckConnection();

            // Check if app is closed
            if (token.IsCancellationRequested)
                return;
            Dispatcher.Invoke(() =>
            {
                lostConnection = this.confirmStatus.Text == "Connection Lost.";
            });

            // Check connection
            if (lostConnection)
                return;

            // Get table info
            // Get table after ordering
            string command = "SELECT SecondTable.tableid, FirstTable.foodStatus, SecondTable.tableStatus, SecondTable.waiterid, FirstTable.tableOrder, SecondTable.totalPrice FROM ( SELECT orderid, group_concat(CONCAT(food.name,':', orderdetail.quantity) SEPARATOR ', ') as tableOrder, orderdetail.status as foodStatus FROM orderdetail, food WHERE orderdetail.foodid = food.id GROUP BY orderid) as FirstTable, ( SELECT _order.id as orderid, tableid, waiterid, _table.status as tableStatus, _order.total as totalPrice FROM _order, _table WHERE _order.tableid = _table.id AND waiterid=@waiterid AND _table.status <> 'Empty' AND _table.status <> 'Dirty' AND _order.status <> 'Done') as SecondTable WHERE FirstTable.orderid = SecondTable.orderid ORDER BY SecondTable.tableid";
            DynamicParameters parameter = new DynamicParameters();

            parameter.Add("@waiterid", this.waiterID);
            List<TableInfo> tableQuery = QueryTableInfo(command, parameter);
            this.tableOccupied = tableQuery.ToArray();


            //Get table before ordering
            command = "SELECT * FROM _table";
            List<Table> tempTable = QueryAssignedTable(command, null);
            this.assignedTable = tempTable.ToArray();

            // Get available dishes
            int currentID = -1;

            command = "SELECT food.id, food.name, ingredientsID, ingredientsQuantityNeed, ingredients.quantity as quantityInStore, price FROM food, foodXingredients, ingredients WHERE food.id = foodXingredients.foodid AND foodXingredients.ingredientsID = ingredients.id AND food.status = 'Active' ORDER BY food.id";
            List<AvailableDish> tableAvailableDish = QueryAvailableDish(command, null);
            List<Dishes> dishes = new List<Dishes>();
            Dishes temp = new Dishes();

            // Check if app is closed
            if (token.IsCancellationRequested)
                return;
            Dispatcher.Invoke(() =>
            {
                lostConnection = this.confirmStatus.Text == "Connection Lost.";
            });

            // Check connection
            if (lostConnection)
                return;

            // Loop through available dishes and add them to list
            availableDishes.Clear();
            foreach (AvailableDish dish in tableAvailableDish)
            {
                if (currentID != dish.id)
                {
                    if (currentID != -1)
                        dishes.Add(temp);

                    temp = new Dishes();
                    temp.id = currentID = dish.id;
                    temp.name = dish.name;
                    temp.quantity = dish.quantityInStore / dish.ingredientsQuantityNeed;
                    temp.pricePerDish = dish.price;
                    temp.recipe = dish.ingredientsID.ToString() + ":" + dish.ingredientsQuantityNeed.ToString();
                }
                else
                {
                    temp.quantity = Math.Min(temp.quantity, dish.quantityInStore / dish.ingredientsQuantityNeed);
                    temp.recipe += ", " + dish.ingredientsID.ToString() + ":" + dish.ingredientsQuantityNeed.ToString();
                }
            }

            dishes.Add(temp);
            availableDishes = dishes;

            // Check if app is closed
            if (token.IsCancellationRequested)
                return;
            Dispatcher.Invoke(() =>
            {
                // Clear the box
                this.foodNameBox.Items.Clear();

                // Update the entire Window 
                if (currentSelected != -1)
                {
                    TableInfo currentTable = null;
                    string tableStatus = "";

                    // Check if selected table is changed
                    if (currentSelected != oldSelected)
                    {
                        this.orderTable.ItemsSource = null;
                        foodSource.Clear();
                        oldSelected = currentSelected;
                    }

                    // Update all the text
                    foreach (TableInfo table in tableOccupied)
                    {
                        if (table.tableid == currentSelected && table.waiterid == waiterID)
                        {
                            currentTable = table;
                            this.tableNumber.Text = table.tableid.ToString();
                            this.tableStatus.Text = table.tableStatus;
                            this.orderTable.ItemsSource = GetFoodInfo(table.tableOrder);
                            this.totalPrice.Text = table.totalPrice.ToString();
                            break;
                        }
                    }

                    // Enable/Disable button
                    if (currentTable == null)
                    {
                        this.tableNumber.Text = currentSelected.ToString();
                        this.tableStatus.Text = tableStatus = this.assignedTable[currentSelected - 1].status;

                        // Update total price
                        float total = 0;
                        int index = 0;
                        foreach (OrderInfo food in foodSource)
                        {
                            index = availableDishes.FindIndex(x => x.id == food.id);

                            if (index == -1)
                                continue;

                            total += food.quantity * availableDishes[index].pricePerDish;
                        }
                        this.totalPrice.Text = total.ToString();

                        if (tableStatus.Equals("Waiting") && this.assignedTable[currentSelected - 1].employeeid == this.waiterID)
                        {
                            // Add to combobox
                            string item = "";

                            foreach (Dishes dish in availableDishes)
                            {
                                item = dish.id.ToString() + '-' + dish.name + '-' + dish.pricePerDish.ToString();
                                this.foodNameBox.Items.Add(item);
                            }
                            
                            for (int i=0; i<foodSource.Count; ++i)
                            {
                                OrderInfo orderDish = foodSource[i];

                                index = availableDishes.FindIndex(x => x.id == orderDish.id);

                                if (index == -1)
                                {
                                    this.selectedOrderComboBox = i;
                                    DeleteOrderButton_Click(null, null);
                                }
                            }

                            this.orderTable.ItemsSource = foodSource;
                            this.orderTable.Items.Refresh();
                            this.foodNameBox.IsEnabled = true;
                            this.foodQuantityBox.IsEnabled = true;

                            this.updateButton.IsEnabled = true;
                            this.deleteButton.IsEnabled = true;

                            this.orderButton.IsEnabled = true;
                            this.deliverButton.IsEnabled = false;
                            this.billButton.IsEnabled = false;

                            this.foodNameBox.SelectedIndex = selectedComboBox;


                        }
                        else
                        {
                            this.orderTable.ItemsSource = null;

                            this.foodNameBox.IsEnabled = false;
                            this.foodQuantityBox.IsEnabled = false;

                            this.updateButton.IsEnabled = false;
                            this.deleteButton.IsEnabled = false;

                            this.orderButton.IsEnabled = false;
                            this.deliverButton.IsEnabled = false;
                            this.billButton.IsEnabled = false;
                        }

                    }
                    else
                    {
                        if (currentTable.foodStatus.Equals("Waiting") && currentTable.tableStatus.Equals("Waiting"))
                        {
                            this.foodNameBox.IsEnabled = false;
                            this.foodQuantityBox.IsEnabled = false;

                            this.updateButton.IsEnabled = false;
                            this.deleteButton.IsEnabled = false;

                            this.orderButton.IsEnabled = false;
                            this.deliverButton.IsEnabled = false;
                            this.billButton.IsEnabled = false;

                        }
                        else if (currentTable.foodStatus.Equals("Done") && currentTable.tableStatus.Equals("Waiting"))
                        {
                            this.foodNameBox.IsEnabled = false;
                            this.foodQuantityBox.IsEnabled = false;

                            this.updateButton.IsEnabled = false;
                            this.deleteButton.IsEnabled = false;

                            this.orderButton.IsEnabled = false;
                            this.deliverButton.IsEnabled = true;
                            this.billButton.IsEnabled = false;
                        }
                        else if (currentTable.foodStatus.Equals("Done") && currentTable.tableStatus.Equals("Eating"))
                        {
                            this.foodNameBox.IsEnabled = false;
                            this.foodQuantityBox.IsEnabled = false;

                            this.updateButton.IsEnabled = false;
                            this.deleteButton.IsEnabled = false;

                            this.orderButton.IsEnabled = false;
                            this.deliverButton.IsEnabled = false;
                            this.billButton.IsEnabled = true;
                        }
                    }
                }
                else
                {
                    this.tableNumber.Text = "";
                    this.tableStatus.Text = "";
                    this.totalPrice.Text = "";
                    this.orderTable.ItemsSource = null;

                    this.foodNameBox.IsEnabled = false;
                    this.foodQuantityBox.IsEnabled = false;

                    this.updateButton.IsEnabled = false;
                    this.deleteButton.IsEnabled = false;

                    this.orderButton.IsEnabled = false;
                    this.deliverButton.IsEnabled = false;
                    this.billButton.IsEnabled = false;
                }

                // Set background button
                for (int i = 0; i < 6; ++i)
                {
                    if (this.assignedTable[i].employeeid == this.waiterID)
                    {
                        TableInfo getCurrentTableInfo = this.tableOccupied.FirstOrDefault(x => x.tableid == i+1);
                        if (getCurrentTableInfo == null)
                        {
                            if (this.assignedTable[i].status.Equals("Waiting"))
                            {
                                tableButton[i].Background = Brushes.GreenYellow;
                            }
                            else if (this.assignedTable[i].status.Equals("Eating"))
                            {
                                tableButton[i].Background = Brushes.OrangeRed;
                            }
                        }
                        else
                        {
                            if (this.assignedTable[i].status.Equals("Waiting") && getCurrentTableInfo.foodStatus.Equals("Waiting"))
                            {
                                tableButton[i].Background = Brushes.GreenYellow;
                            }
                            else if (this.assignedTable[i].status.Equals("Waiting") && getCurrentTableInfo.foodStatus.Equals("Done"))
                            {
                                tableButton[i].Background = Brushes.Yellow;
                            }
                            else if (this.assignedTable[i].status.Equals("Eating"))
                            {
                                tableButton[i].Background = Brushes.OrangeRed;
                            }
                        }

                    }
                    else
                    {
                        tableButton[i].Background = Brushes.LightGray;
                    }
                }
            });

        }

        // This function get data and set the text accordingly. Food is a list
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Lost connection or thread is running
            if (this.confirmStatus.Text == "Connection Lost." || thread.IsAlive)
                return;

            // Check if there are any items in the food list
            if (this.updateButton.IsEnabled)
            {
                if (this.orderTable.Items.Count != 0)
                {
                    System.Windows.MessageBox.Show("Please send order or delete all the food on list first.");
                    return;
                }
            }

            // Get last character in table name
            char lastElement = ((Button)sender).Content.ToString().Last();
            int tableID = lastElement - '0';

            // Set current table selected
            currentSelected = tableID;
            this.foodQuantityBox.Text = "";

            thread = new Thread(Refresh);
            thread.Start();
        }

        // Fire if selected row changes in food menu
        private void FoodNameBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lost connection
            if (this.confirmStatus.Text == "Connection Lost.")
                return;

            // If this fire is not from ComboBox or not select any table
            if (!(e.Source is ComboBox) || ((ComboBox)sender).SelectedIndex == -1)
                return;

            // Get index of dishes in the database
            this.selectedComboBox = ((ComboBox)sender).SelectedIndex;
            int index = this.selectedComboBox;

            // Change quantity of a dish in the quantity field 
            // If quantity field is empty or allow to change
            if (this.foodQuantityBox.Text == "" || changeTextQuantity)
            {
                this.changeTextQuantity = false;
                this.oldSelectedComboBox = this.selectedComboBox;
                this.foodQuantityBox.Text = availableDishes[index].quantity.ToString();
            }
            // If the selected box is different from the old one or the quantity in the field is higher than the available quantity of a dish
            else if (this.selectedComboBox != this.oldSelectedComboBox || Convert.ToInt32(this.foodQuantityBox.Text) > availableDishes[index].quantity)
            {
                this.oldSelectedComboBox = this.selectedComboBox;
                this.foodQuantityBox.Text = availableDishes[index].quantity.ToString();
            }

            
        }

        // Fire when selected row in order table is changed. When choose a dish, a corresponding quantity will be show in quantity field
        // Can only order when all the ingredients after deduction is not lower than 0
        // The ingredients in this phase is not changed in the database
        private void OrderTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lost connection or thread is running
            if (this.confirmStatus.Text == "Connection Lost." || thread.IsAlive)
                return;

            FoodInfo currentSelected = (FoodInfo)this.orderTable.SelectedItem;

            if (currentSelected == null)
            {
                this.selectedOrderComboBox = -1;
                return;
            }
                
            if (this.foodNameBox.IsEnabled == false)
            {
                this.orderTable.SelectedIndex = -1;
                return;
            }

            this.selectedOrderComboBox = this.orderTable.SelectedIndex;
            int index = availableDishes.FindIndex(x => x.id == currentSelected.id);

            this.foodNameBox.SelectedIndex = index;
            this.foodQuantityBox.Text = availableDishes[index].quantity.ToString();
        }

        // This function write order in the Order table, and query to the database to deduct the necessary ingredients
        private void UpdateOrderButton_Click(object sender, RoutedEventArgs e)
        {
            // Lost connection or thread is running
            if (this.confirmStatus.Text == "Connection Lost." || thread.IsAlive)
                return;

            // Current available quantity of a dish
            int currentAmount = Convert.ToInt32(this.foodQuantityBox.Text);
            // Get dish's index
            int index = this.selectedComboBox;
            Dishes currentDish = availableDishes[index];

            // Check ingredients 
            if (currentAmount == 0 || currentAmount > currentDish.quantity)
            {
                System.Windows.MessageBox.Show("Not enough ingredients.");
                this.foodQuantityBox.Text = currentDish.quantity.ToString();
                return;
            }

            // Get recipe's list and update ingredients's quantity
            List<FoodInfo> recipeList = GetFoodInfo(currentDish.recipe);
            string command = "UPDATE ingredients SET quantity = quantity - @amount WHERE id = @id";
            DynamicParameters parameter = new DynamicParameters();

            foreach (FoodInfo ingredient in recipeList)
            {
                parameter.Add("@amount", currentAmount * ingredient.quantity);
                parameter.Add("@id", ingredient.name);

                QueryExecute(command, parameter);
            }

            // Check if item in the list
            index = foodSource.FindIndex(x => x.id == currentDish.id);
            
            if (index != -1)
            {
                foodSource[index] = new OrderInfo(currentDish.name, foodSource[index].quantity + currentAmount, currentDish.recipe, currentDish.id);
            }
            else
            {
                foodSource.Add(new OrderInfo(currentDish.name, currentAmount, currentDish.recipe, currentDish.id));
            }

            thread = new Thread(Refresh);
            thread.Start();
        }

        // This function delete order from Order tablem and query to database to plus all necessary ingredients
        private void DeleteOrderButton_Click(object sender, RoutedEventArgs e)
        {
            bool directCall = false;

            if (sender is null)
                directCall = true;

            // Lost connection or thread is running
            if (this.confirmStatus.Text == "Connection Lost." || thread.IsAlive)
                return;

            // Check if there is any dishes
            if (this.orderTable.Items.Count == 0 || this.selectedOrderComboBox == -1)
            {
                System.Windows.MessageBox.Show("No dishes to delete.");
                return;
            }

            // Get dish's index
            OrderInfo foodToDelete = foodSource[this.selectedOrderComboBox];

            // Find item in menu
            int index = availableDishes.FindIndex(x => x.id == foodToDelete.id);

            // If found dish
            if (index != -1)
            {
                int currentAmount = foodSource[this.selectedOrderComboBox].quantity;
                List<FoodInfo> recipeList = GetFoodInfo(availableDishes[index].recipe);

                string command = "UPDATE ingredients SET quantity = quantity + @amount WHERE id = @id";
                DynamicParameters parameter = new DynamicParameters();

                foreach (FoodInfo ingredient in recipeList)
                {
                    parameter.Add("@amount", currentAmount * ingredient.quantity);
                    parameter.Add("@id", ingredient.name);

                    QueryExecute(command, parameter);
                }

                foodSource.RemoveAt(this.selectedOrderComboBox);
                this.changeTextQuantity = true;
                this.selectedOrderComboBox = -1;
            }
            else
            {
                if (!directCall)
                {
                    System.Windows.MessageBox.Show("Cannot found the dish.");
                    return;
                }
                else
                {
                    int currentAmount = foodSource[this.selectedOrderComboBox].quantity;
                    List<FoodInfo> recipeList = GetFoodInfo(foodSource[this.selectedOrderComboBox].recipe);

                    string command = "UPDATE ingredients SET quantity = quantity + @amount WHERE id = @id";
                    DynamicParameters parameter = new DynamicParameters();

                    foreach (FoodInfo ingredient in recipeList)
                    {
                        parameter.Add("@amount", currentAmount * ingredient.quantity);
                        parameter.Add("@id", ingredient.name);

                        QueryExecute(command, parameter);
                    }

                    foodSource.RemoveAt(this.selectedOrderComboBox);
                    this.changeTextQuantity = true;
                    this.selectedOrderComboBox = -1;

                }
                
            }

            if (!directCall)
            {
                thread = new Thread(Refresh);
                thread.Start();
            }
            
        }

        // This function create query to 2 table: _order and orderdetail
        // In _order: update timeordered, id of waiter, status to "Waiting", and total is total cost of the order
        // Each orderdetail consist of a dish and its quantity from that order
        // orderid get from _order, foodid and quantity get from Food, set status to "Waiting"
        // After that clear and deactive combobox, disable Update and Remove button
        private void PlaceOrderButton_Click(object sender, RoutedEventArgs e)
        {
            // Lost connection or thread is running
            if (this.confirmStatus.Text == "Connection Lost." || thread.IsAlive)
                return;

            // Check if item is in list
            if (foodSource.Count == 0)
            {
                System.Windows.MessageBox.Show("Please add item to list.");
                return;
            }

            // Add order to database
            string command = "INSERT INTO _order (timeordered, waiterid, tableid, status, total) VALUES (CURRENT_TIMESTAMP, @waiterid, @tableid, 'Waiting', @total)";
            DynamicParameters parameter = new DynamicParameters();
            float total = 0;

            foreach (OrderInfo food in foodSource)
            {
                Dishes currentDish = availableDishes[food.id - 1];
                total += currentDish.pricePerDish * food.quantity;

            }

            parameter.Add("@waiterid", this.waiterID);
            parameter.Add("@tableid", this.currentSelected);
            parameter.Add("@total", total);

            // Check if query succeed
            bool result = QueryExecute(command, parameter);

            if (!result)
            {
                System.Windows.MessageBox.Show("Lost connection to server.");
                return;
            }

            // Add to orderdetail
            // Get id that just added
            command = "SELECT max(id) as id FROM _order WHERE tableid = @tableid";
            int orderid = QueryOrderID(command, parameter);

            // Add to orderdetail
            command = "INSERT INTO orderdetail (orderid, foodid, quantity, status) VALUES (@orderid, @foodid, @quantity, 'Waiting')";

            foreach (OrderInfo food in foodSource)
            {
                Dishes currentDish = availableDishes[food.id - 1];

                parameter.Add("@orderid", orderid);
                parameter.Add("@foodid", currentDish.id);
                parameter.Add("@quantity", food.quantity);

                result = QueryExecute(command, parameter);

                if (!result)
                {
                    System.Windows.MessageBox.Show("Lost connection to server.");
                    return;
                }
            }

            thread = new Thread(Refresh);
            thread.Start();
        }

        // This button is clickable when order is ready, meaning status in _order is "Waiting" and all 
        // orderid that belong to _order have status in orderdetail is "Done"
        // When click, this function will update status of _order to "Eating",
        // update status of _table to "Eating", update timedelivered in order and disable Deliver button
        private void DeliverButton_Click(object sender, RoutedEventArgs e)
        {
            // Lost connection or thread is running
            if (this.confirmStatus.Text == "Connection Lost." || thread.IsAlive)
                return;

            // Send query
            string command = "UPDATE _table SET status = 'Eating' WHERE id = @tableid; UPDATE _order SET status = 'Eating', timedelivered = CURRENT_TIMESTAMP WHERE id = (SELECT max(iD) FROM _order WHERE tableid = @tableid)";
            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@tableid", this.currentSelected);

            bool result = QueryExecute(command, parameter);

            // Check if succeed
            if (!result)
            {
                System.Windows.MessageBox.Show("Lost connection to server.");
                return;
            }

            thread = new Thread(Refresh);
            thread.Start();
        }

        // This function update status in _order to "Paid", and update status in _table to "Dirty"
        private void PaybillButton_Click(object sender, RoutedEventArgs e)
        {
            // Lost connection or thread is running
            if (this.confirmStatus.Text == "Connection Lost." || thread.IsAlive)
                return;

            // Send query
            string command = "UPDATE _table SET status = 'Dirty', employeeid = '' WHERE id = @tableid; UPDATE _order SET status = 'Done', timefinished = CURRENT_TIMESTAMP WHERE tableid = @tableid";
            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@tableid", this.currentSelected);

            bool result = QueryExecute(command, parameter);

            // Check if succeed
            if (!result)
            {
                System.Windows.MessageBox.Show("Lost connection to server.");
                return;
            }

            thread = new Thread(Refresh);
            thread.Start();
        }

        // Send user to login screen
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
