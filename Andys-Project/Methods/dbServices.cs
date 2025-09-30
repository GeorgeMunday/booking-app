using System;
using System.Data.SQLite;

namespace Andys_Project.db
{
    public class dbServices
    {
        public SQLiteConnection Connection { get; private set; }

        public dbServices()
        {
            Connection = DBConnection();
        }

        private SQLiteConnection DBConnection()
        {
            string dbPath = @"C:\Users\geoge\OneDrive\Desktop\dbs\records.db";
            string connectionString = $"Data Source={dbPath};Version=3;";
            SQLiteConnection conn = new SQLiteConnection(connectionString);

            try
            {
                conn.Open();
                Console.WriteLine("Connected to DB");
            }
            catch (Exception ex)
            {
                Console.WriteLine("DB error: " + ex.Message);
            }

            return conn;
        }
    }
}
