using System; // import that i use are here
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Andys_Project.db;



namespace Andys_Project
{
    public class ItemData // class that i use later defined here
    {
        public string Name { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; } // variable defined in class to use for later
    }

    public partial class Dashboard : Window
    {
        private dbServices db; // importing db methods from external file
        private string username; // defining username so can use it for queries

        public Dashboard(string username)
        {
            this.ResizeMode = ResizeMode.NoResize;
            
            InitializeComponent();
            db = new dbServices();
            this.username = username;

            grabItemData(db.Connection); // calling methods to grab data from database and display it on screen
            grabUserData(db.Connection);
            UserBookings(db.Connection);
        }

        private void grabUserData(SQLiteConnection connection) // method to grab user data from database and display it on screen
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

                        UserInfoTextBlock.Text = $"Name: {firstName} {lastName}";
                    }
                }
            }
        }

        private void grabItemData(SQLiteConnection connection) // method to grab item data from database and display it on screen
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

        private void ItemComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) // method that runs when item is selected from combobox to display description and amount in stock
        {
            if (ItemComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is ItemData data)
            {
                ItemDescriptionTextBox.Text = data.Description;
                InStorageTextBlock.Text = data.Amount.ToString();
            }
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e) // method that runs when submit button is clicked to add booking to database
        {
            if (!IsEnoughItems())
            {
                MessageBox.Show("Not enough items available in stock.");
                return;
            }

            if (!isDateValid())
            {
                return;
            }

            string query = "INSERT INTO Bookings (Name, Item, Amount, Date) VALUES (@Name, @Item, @Amount, @Date)";

            using (SQLiteCommand cmd = new SQLiteCommand(query, db.Connection))
            {
                cmd.Parameters.AddWithValue("@Name", username);

                ItemData data = null;
                if (ItemComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is ItemData itemData)
                {
                    cmd.Parameters.AddWithValue("@Item", itemData.Name);
                    data = itemData;
                }
                else
                {
                    MessageBox.Show("Please select an item.");
                    return;
                }

                int quantity;
                if (int.TryParse(QuantityTextBox.Text, out quantity))
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
                    ReduceItemStock(data.Name, quantity);
                    data.Amount -= quantity;
                    InStorageTextBlock.Text = data.Amount.ToString();

                    MessageBox.Show("Booking submitted successfully!");
                    UserBookings(db.Connection);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error submitting booking: " + ex.Message);
                }
            }
        }

        private void UserBookings(SQLiteConnection connection) // method to grab user bookings from database and display it on screen
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

                        bookings.Add($"Item: {item} Quantity: {amount} Date: {date}");
                    }

                    if (bookings.Count > 0)
                        Bookings.Text = string.Join("\n", bookings);
                    else
                        Bookings.Text = "No bookings yet.";
                }
            }
        }

        private void DeleteBookings_Click(object sender, RoutedEventArgs e) // method that runs when delete bookings button is clicked to delete all user bookings from database
        {
            SQLiteConnection connection = db.Connection;
            string fetchQuery = "SELECT Item, Amount FROM Bookings WHERE Name=@Username";
            List<(string Item, int Amount)> bookings = new List<(string, int)>();

            using (SQLiteCommand fetchCmd = new SQLiteCommand(fetchQuery, connection))
            {
                fetchCmd.Parameters.AddWithValue("@Username", username);
                using (SQLiteDataReader reader = fetchCmd.ExecuteReader())
                {
                    while (reader.Read()) // fetching bookings to increase stock back
                    {
                        string item = reader.GetString(0);
                        int amount = reader.GetInt32(1);
                        bookings.Add((item, amount));
                    }
                }
            }
            foreach (var booking in bookings) // increasing stock back for each booking deleted
            {
                IncreaseItemStock(booking.Item, booking.Amount);

                if (ItemComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is ItemData data)
                {
                    if (data.Name == booking.Item)
                    {
                        data.Amount += booking.Amount;
                        InStorageTextBlock.Text = data.Amount.ToString();
                    }
                }
            }
            string deleteQuery = "DELETE FROM Bookings WHERE Name=@Username";  // deleting bookings from database
            using (SQLiteCommand cmd = new SQLiteCommand(deleteQuery, connection))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                try
                {
                    int rowsAffected = cmd.ExecuteNonQuery();
                    MessageBox.Show($"{rowsAffected} bookings deleted successfully.");
                    UserBookings(connection);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting bookings: " + ex.Message);
                }
            }
        }


        private void LogoutButton_Click(object sender, RoutedEventArgs e) // method that runs when logout button is clicked to return to login screen
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private bool IsEnoughItems() // method to check if there are enough items in stock before allowing booking
        {
            if (ItemComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is ItemData data)
            {
                if (int.TryParse(QuantityTextBox.Text, out int requestedAmount))
                {
                    return data.Amount >= requestedAmount;
                }
            }
            return false;
        }

        private void ReduceItemStock(string itemName, int quantity) // method to reduce item stock in database after booking is made
        {
            string updateQuery = "UPDATE Items SET Amount = Amount - @BookedAmount WHERE Name = @Item";
            using (SQLiteCommand updateCmd = new SQLiteCommand(updateQuery, db.Connection))
            {
                updateCmd.Parameters.AddWithValue("@BookedAmount", quantity);
                updateCmd.Parameters.AddWithValue("@Item", itemName);
                updateCmd.ExecuteNonQuery();
            }
        }

        private void IncreaseItemStock(string itemName, int quantity) // method to increase item stock in database after bookings are deleted
        {
            string updateQuery = "UPDATE Items SET Amount = Amount + @BookedAmount WHERE Name = @Item";
            using (SQLiteCommand updateCmd = new SQLiteCommand(updateQuery, db.Connection))
            {
                updateCmd.Parameters.AddWithValue("@BookedAmount", quantity);
                updateCmd.Parameters.AddWithValue("@Item", itemName);
                updateCmd.ExecuteNonQuery();
            }
        }

        private bool isDateValid() // method to check if date selected is already booked and if its valid (not in the past)
        {
            string query="Select Date from Bookings Where Date = @Date ";
            using (SQLiteCommand updateCmd = new SQLiteCommand(query, db.Connection))
                {
                DateTime selectedDate = DatePickerControl.SelectedDate.Value;
                if (DatePickerControl.SelectedDate.HasValue)
                {
                    updateCmd.Parameters.AddWithValue("@Date", selectedDate.ToString("yyyy-MM-dd"));
                }
                else
                {
                    MessageBox.Show("Please select a date.");
                    return false;
                }
                if (selectedDate < DateTime.Today)
                {
                    MessageBox.Show("Please select a valid date (not in the past).");
                    DatePickerControl.SelectedDate = null;
                    return false;
                }
                using (SQLiteDataReader reader = updateCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        MessageBox.Show("Date already booked, please select another date.");
                        return false;
                    }
                }
            }
            return true;
        }

    }
}
