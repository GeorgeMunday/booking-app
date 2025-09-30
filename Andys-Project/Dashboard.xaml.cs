using System;
using System.Collections.Generic;
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
using System.Xml.Linq;
using Andys_Project.db;



namespace Andys_Project
{
    public partial class Dashboard : Window
    {
        private dbServices db;
        private string username;
        public Dashboard(string username)
        {
            this.ResizeMode = ResizeMode.NoResize;
            InitializeComponent();
            db = new dbServices();
            this.username = username;
            grabItemData(db.Connection);
            grabUserData(db.Connection);

        }

        private void  grabUserData(SQLiteConnection connection)
        {
            string query = "SELECT FirstName, LastName, DOB FROM Users WHERE Username=@Username";
            using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string firstName = reader.GetString(0);
                        string lastName = reader.GetString(1);
                        string dob = reader.GetString(2);
                    }
                }
            }
        }

        private void grabItemData(SQLiteConnection connection)
        {
            string query = "SELECT Name, Amount, Description FROM Items";
            using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
            {
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string name = reader.GetString(0);
                        int amount = reader.GetInt32(1);
                        string description = reader.GetString(2);
                        ComboBoxItem comboBoxItem = new ComboBoxItem();
                        comboBoxItem.Content = name;
                        comboBoxItem.Tag = new { Amount = amount, Description = description };

                        ItemComboBox.Items.Add(comboBoxItem);
                       
                    }


                }
            }

        }

        private void ItemComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ItemComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                dynamic data = selectedItem.Tag;
                ItemDescriptionTextBox.Text = data.Description;
                InStorageTextBlock.Text = data.Amount.ToString();
            }
        }


        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            string query = "INSERT INTO Bookings (Name, Item,Amount, Date) VALUES (@Name, @Item, @Amount, @Date)";

            using (SQLiteCommand cmd = new SQLiteCommand(query, db.Connection))
            {
                cmd.Parameters.AddWithValue("@Name", username);

                if (ItemComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    cmd.Parameters.AddWithValue("@Item", selectedItem.Content.ToString());
                }
                else
                {
                    MessageBox.Show("Please select an item.");
                    return;
                }

                if (int.TryParse(QuantityTextBox.Text, out int quantity))
                {
                    cmd.Parameters.AddWithValue("@Amount", quantity);
                }
                else
                {
                    MessageBox.Show("Please enter a valid quantity.");
                    return;
                }

                if (DatePickerControl.SelectedDate.HasValue)
                {
                    DateTime selectedDate = DatePickerControl.SelectedDate.Value;
                    cmd.Parameters.AddWithValue("@Date", selectedDate.ToString("yyyy-MM-dd"));
                }
                else
                {
                    MessageBox.Show("Please select a date.");
                    return;
                }

                try
                {
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Booking submitted successfully!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error submitting booking: " + ex.Message);
                }
            }
        }

    }
}

