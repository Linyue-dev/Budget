using Budget.Models;
using Budget.Services;

namespace Budget
{
    internal class Program
    {
        
        public static void Main(string[] args)
        {
            try
            {
                using var db = DatabaseService.CreateNewDatabase("test.db");
                Console.WriteLine("Database created successfully");

                Console.WriteLine($"Connection state: {db.Connection.State}");

                using var command = db.Connection.CreateCommand();
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
                using var reader = command.ExecuteReader();
                Console.WriteLine("Tables created:");

                while (reader.Read())
                {
                    Console.WriteLine($"  - {reader.GetString(0)}");
                }
                reader.Close();

                command.CommandText = "SELECT Id, Description FROM categoryTypes";

                using var reader1 = command.ExecuteReader();
                Console.WriteLine("CategoryTypes data:");

                while (reader1.Read())
                {
                    Console.WriteLine($"  {reader1.GetInt32(0)}: {reader1.GetString(1)}");
                }
                Console.WriteLine("All tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex.Message}");
            }
        }        
    }
}
