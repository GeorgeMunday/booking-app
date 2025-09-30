using System;
using System.Data.SQLite;
using System.Windows;
using Andys_Project.db;

namespace Andys_Project
{
    public partial class MainWindow : Window
    {
        private dbServices db;

        bool isLogin = true;
        bool isUserInfo = false;

        string username;
        string password;
        string forName;
        string lastName;
        string dob;

        public MainWindow()
        {
            this.ResizeMode = ResizeMode.NoResize;
            InitializeComponent();

            db = new dbServices(); 
            CreateTableIfNotExists(db.Connection);
        }

        private void CreateTableIfNotExists(SQLiteConnection connection)
        {
            string sql = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT UNIQUE,
                    Password TEXT,
                    FirstName TEXT,
                    LastName TEXT,
                    DOB TEXT
                )";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private void loginUser(string username, string password)
        {
            Dashboard Frmdashboard = new Dashboard(username);
            string query = "SELECT COUNT(1) FROM Users WHERE Username=@Username AND Password=@Password";

            using (SQLiteCommand cmd = new SQLiteCommand(query, db.Connection))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Password", password);

                int count = Convert.ToInt32(cmd.ExecuteScalar());

                if (count == 1)
                {
                    Frmdashboard.Show();
                    this.Hide();
                }
                else
                    MessageBox.Show("Invalid username or password.");
            }
        }

        private void registerUser(string username, string password, string forName, string lastName, string dob)
        {
            string query = "INSERT INTO Users (Username, Password, FirstName, LastName, DOB) VALUES (@Username, @Password, @FirstName, @LastName, @DOB)";

            using (SQLiteCommand cmd = new SQLiteCommand(query, db.Connection))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Password", password);
                cmd.Parameters.AddWithValue("@FirstName", forName);
                cmd.Parameters.AddWithValue("@LastName", lastName);
                cmd.Parameters.AddWithValue("@DOB", dob);

                try
                {
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("User registered successfully!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error registering user: " + ex.Message);
                }
            }
        }

        // Form methods
        private void switchStateBtn_Click(object sender, RoutedEventArgs e)
        {
            isLogin = !isLogin;

            if (isLogin)
            {
                switchStateBtn.Content = "Register";
                loginForm.Visibility = Visibility.Visible;
                registerForm.Visibility = Visibility.Hidden;
                userForm.Visibility = Visibility.Hidden;
            }
            else
            {
                switchStateBtn.Content = "Login";
                registerForm.Visibility = Visibility.Visible;
                loginForm.Visibility = Visibility.Hidden;
                userForm.Visibility = Visibility.Hidden;
            }
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (isLogin)
            {
                username = loginUsername.Text.Trim();
                password = loginPassword.Password.Trim();
                loginUser(username, password);
            }
            else
            {
                if (isUserInfo)
                {
                    forName = userForeName.Text.Trim();
                    lastName = userLastName.Text.Trim();
                    dob = userDob.Text.Trim();
                    registerUser(username, password, forName, lastName, dob);
                    isUserInfo = false;
                    registerForm.Visibility = Visibility.Visible;
                    userForm.Visibility = Visibility.Hidden;
                }
                else
                {
                    username = registerUsername.Text.Trim();
                    password = regesterPassowrd.Password.Trim();
                    isUserInfo = true;
                    registerForm.Visibility = Visibility.Hidden;
                    userForm.Visibility = Visibility.Visible;
                }
            }
        }
    }
}
