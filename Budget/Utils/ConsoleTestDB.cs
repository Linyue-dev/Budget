using Budget.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Budget.Utils
{
    public class ConsoleTestDB
    {

        public ConsoleTestDB() { }

        private static void InitializationDB()
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

                    InsertDataForCategories(newDb);

                    InsertDataTransactions(newDb);
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

        private static void InsertDataTransactions(DatabaseService newDb)
        {
            using var command = newDb.Connection.CreateCommand();
            command.CommandText = "INSERT INTO Transactions (Id, Date, Description, Amount, CategoryId) VALUES" +
                "(1, '2018-01-10', 'Tshirt', 22.00, 6)," +
                "(2, '2018-01-16', 'mortagage', 800.00, 14)," +
                "(3, '2018-02-10', 'monthly salary', 3500.00, 16)," +
                "(4, '2019-02-10', 'visit friend', 35.00, 9)," +
                "(5, '2020-01-11', 'McDonalds', 45.00, 2)," +
                "(6, '2020-01-10', 'monthly salary', 4500.00, 16)," +
                "(7, '2020-01-12', 'Electricity bill', 225.00, 1)," +
                "(8, '2020-01-15', 'Wendys', 25.00, 2)," +
                "(9, '2020-02-01', 'Costco', 133.33, 2)," +
                "(10, '2020-02-20', 'Mobile fee', 125.06, 1)," +
                "(11, '2020-03-25', 'french course', 450.00, 7)," +
                "(12, '2020-04-21', '2020 municipal', 2500.00, 10)," +
                "(13, '2021-02-10', 'car insurance', 1100.00, 5)," +
                "(14, '2021-07-11', 'school tax', 720.11, 10)," +
                "(15, '2024-01-11', 'us found', 1720.11, 18)," +
                "(16, '2024-03-01', 'rental income', 2720.11, 17)," +
                "(17, '2024-03-15', 'Costco', 126.66, 2)," +
                "(18, '2024-04-07', 'fix fence', 1026.66, 11)";
            command.ExecuteNonQuery();
            Console.WriteLine("Insert Transactions data successfuly!");
        }

        private static void InsertDataForCategories(DatabaseService newDb)
        {
            using var command = newDb.Connection.CreateCommand();
            //command.CommandText = "INSERT INTO Categories (Id, Name,TypeId )VALUES (1, 'Utilities', 'Expense')";
            command.CommandText = "INSERT INTO Categories (Id, Name, TypeId) VALUES" +
                "(1, 'Utilities', 2)," +
                "(2, 'Food & Dining', 2)," +
                "(3, 'Transportation', 2)," +
                "(4, 'Health & Personal Care',2)," +
                "(5, 'Insurance', 2)," +
                "(6, 'Clothes', 2)," +
                "(7, 'Education', 2)," +
                "(8, 'Vacation', 2)," +
                "(9, 'Social Expenses', 2)," +
                "(10, 'Municipal & School Tax', 2)," +
                "(11, 'Rental Expenses', 2)," +
                "(12, 'Miscellaneous', 2)," +
                "(13, 'Savings', 5)," +
                "(14, 'Housing mortgage', 3)," +
                "(15, 'Auto loan', 3)," +
                "(16, 'Salary', 1)," +
                "(17, 'Rental Income', 1)," +
                "(18, 'Stock & Fund', 4)";
            command.ExecuteNonQuery();
            Console.WriteLine("Insert Categories successfuly!");
        }
    }
}
