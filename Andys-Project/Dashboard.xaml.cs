using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using Andys_Project.db;

namespace Andys_Project
{
    public class ItemData
    {
        public string Name { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; }
    }

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
            LoadUserBookings(db.Connection);
        }

        private void grabUserData(SQLiteConnection connection)
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

                        UserInfoTextBlock.Text = $"Name: {firstName} {lastName}\nDOB: {dob}";
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
                        comboBoxItem.Tag = new ItemData { Name = name, Amount = amount, Description = description };

                        ItemComboBox.Items.Add(comboBoxItem);
                    }
                }
            }
        }

        private void ItemComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ItemComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is ItemData data)
            {
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

        private void LoadUserBookings(SQLiteConnection connection)
        {
            string query = "SELECT Item, Amount, Date FROM Bookings WHERE Name=@Username";
            using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    List<string> bookings = new List<string>();

                    while (reader.Read())
                    {
                        string item = reader.GetString(0);
                        int amount = reader.GetInt32(1);
                        string date = reader.GetString(2);

                        bookings.Add($"Item: {item}\nQuantity: {amount}\nDate: {date}\n");
                    }

                    if (bookings.Count > 0)
                        headin.Text = string.Join("\n\n", bookings);
                    else
                        headin.Text = "No bookings yet.";
                }
            }
        }
    }
}
