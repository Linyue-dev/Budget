using Budget.Models;
using Budget.Services;

namespace Budget
{
    internal class Program
    {
        
        public static void Main(string[] args)
        {
            string dbPath = "test.db";

            try
            {
                Console.WriteLine("=============== Test Database ===============");

                // 1. Cleaned up  file
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }

                // 2. Test for creating new db
                Console.WriteLine("\nTest create new database...");
                using (var newDb = DatabaseService.CreateNewDatabase(dbPath))
                {
                    Console.WriteLine("Database created successfully");
                    Console.WriteLine($"Connection state: {newDb.Connection.State}");
                    Console.WriteLine("Tables created:");
                    VerifyTables(newDb);
                    Console.WriteLine("CategoryTypes data:");
                    VerifyCategoryTypes(newDb);
                }

                // 3. Test for existing db
                Console.WriteLine("\nTesting OpenExisting...");
                using (var existingDb = DatabaseService.OpenExisting(dbPath))
                {
                    Console.WriteLine("Existing database opened successfully");
                    Console.WriteLine($"Connection state: {existingDb.Connection.State}");

                    Console.WriteLine("CategoryTypes data:");
                    VerifyCategoryTypes(existingDb);
                }

                // 4. Test not existing db
                Console.WriteLine("\nTesting error handling...");
                try
                {
                    DatabaseService.OpenExisting("nonexistent.db");
                    Console.WriteLine("Should have thrown exception!");
                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine("Correctly handled missing file");
                }

                Console.WriteLine("\nAll tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex.Message}");
            }
            finally
            {
                if (File.Exists(dbPath))
                    File.Delete(dbPath);
            }         
        }    
        
        private static void VerifyTables(DatabaseService db)
        {
            using var command = db.Connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine($"  - {reader.GetString(0)}");
            }
        }

        private static void VerifyCategoryTypes(DatabaseService db)
        {
            using var commmand = db.Connection.CreateCommand();
            commmand.CommandText = "SELECT Id, Description FROM categoryTypes";
            using var reader = commmand.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine($"  {reader.GetInt32(0)}: {reader.GetString(1)}");
            }
        }
    }
}
