using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DoAnCuoiKi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"The description is: {this.DescriptionText.Text}");
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            this.CheckBox1.IsChecked = this.CheckBox2.IsChecked = this.CheckBox3.IsChecked = this.CheckBox4.IsChecked = this.CheckBox5.IsChecked
                = this.CheckBox6.IsChecked = this.CheckBox7.IsChecked = this.CheckBox8.IsChecked = this.CheckBox9.IsChecked = this.CheckBox10.IsChecked = false;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            this.LengthText.Text += ((CheckBox)sender).Content;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.NoteText == null)
                return;

            var comboBox = (ComboBox)sender;
            var value = (ComboBoxItem)comboBox.SelectedValue;

            this.NoteText.Text = (string)value.Content;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox_SelectionChanged(this.DropdownBox, null);
        }

        private void SupplierText_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.MassText.Text = this.SupplierText.Text;
        }
    }
}
