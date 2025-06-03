using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using Budget.Models;
using Budget.Utils;
using static Budget.Models.Category;
using System.Data.SQLite;
using System.Data;


namespace Budget.Services
{
    public class Categories : IDisposable 
    {

        #region Private Fields
        private DatabaseService _databaseService;
        private readonly bool _ownsDatabase;
        private bool _disposed = false;
        #endregion

        #region Public Properties    
        public string DatabasePath => Path.GetFullPath(_databaseService?.Connection?.DataSource ?? "");
        public bool IsConnected => _databaseService?.Connection?.State == System.Data.ConnectionState.Open;

        #endregion

        #region Constructors
        public Categories(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _ownsDatabase = false; // External ownership
        }

        public Categories(string databasePath, bool isNew = false)
        {
            if (string.IsNullOrWhiteSpace(databasePath)) throw new ArgumentNullException(nameof(databasePath));
            if (isNew)
            {
                _databaseService = DatabaseService.CreateNewDatabase(databasePath);
                SetCategoriesToDefaults(); // Automatically set up default categories for new database
            }
            else
            {
                _databaseService = DatabaseService.OpenExisting(databasePath);
            }
            _ownsDatabase = true; // This instance owns the database
        }
        #endregion

        public void SetCategoriesToDefaults()
        {
            EnsureNotDisposed();

            using var checkCommand = _databaseService.Connection.CreateCommand();
            checkCommand.CommandText = @"SELECT COUNT(*) FROM categories";
            var categoryCount = Convert.ToInt32(checkCommand.ExecuteScalar());

            if (categoryCount > 0)
            {
                throw new InvalidOperationException("Categories already exist");
            }

            // Add defaults
            Add("Utilities", CategoryType.Expense);
            Add("Food & Dining", CategoryType.Expense);
            Add("Transportation", CategoryType.Expense);
            Add("Health & Personal Care", CategoryType.Expense);
            Add("Insurance", CategoryType.Expense);
            Add("Clothes", CategoryType.Expense);
            Add("Education", CategoryType.Expense);
            Add("Vacation", CategoryType.Expense);
            Add("Social Expenses", CategoryType.Expense);
            Add("Municipal & School Tax", CategoryType.Expense);
            Add("Rental Expenses", CategoryType.Expense);
            Add("Miscellaneous", CategoryType.Expense);
            Add("Savings", CategoryType.Savings);
            Add("Housing mortgage", CategoryType.Debt);
            Add("Auto loan", CategoryType.Debt);
            Add("Salary", CategoryType.Income);
            Add("Rental Income", CategoryType.Income);
            Add("Stock & Fund", CategoryType.Investment);
        }
        public int Add(string name, Category.CategoryType type)
        {
            EnsureNotDisposed();

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Category name cannot be null or empty.", nameof(name));

            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                    INSERT INTO categories (Name, TypeId) 
                    VALUES (@name, @typeId);
                    SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@typeId", (int)type);

            var result = command.ExecuteScalar();
            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException("Failed to insert category");

            return Convert.ToInt32(result);
        }


        public void Delete(int id)
        {
            using var checkCommand = _databaseService.Connection.CreateCommand();
            checkCommand.CommandText = @"SELECT COUNT(*) FROM transactions WHERE CategoryId = @id";
            checkCommand.Parameters.AddWithValue("@id", id);

            var transactionCount = Convert.ToInt32(checkCommand.ExecuteScalar());
            if (transactionCount > 0)
            {
                throw new InvalidOperationException($"Cannot delete category with {transactionCount} associated transactions. Delete the transactions first.");
            }

            // Safe Deletion Categories
            using var deleteCommand = _databaseService.Connection.CreateCommand();
            deleteCommand.CommandText = "DELETE FROM categories WHERE Id = @id";
            deleteCommand.Parameters.AddWithValue("@id", id);

            var rowsAffected = deleteCommand.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Category with ID {id} not found.");
            }
        }

        public List<Category> List()
        {
            EnsureNotDisposed();
            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = "SELECT Id, Name,TypeId FROM categories";
            using SQLiteDataReader reader = command.ExecuteReader();

            var categories = new List<Category>();

            while (reader.Read())
            {
                categories.Add(new Category(
                    reader.GetInt32("Id"),
                    reader.GetString("Name"),
                    (CategoryType)reader.GetInt32("TypeId")
                    ));
            }
            return categories;
        }

        #region Helper Methods
        private void EnsureNotDisposed()
        {
            if (_disposed) // Check whether the Categories object has been released
                throw new ObjectDisposedException(nameof(Categories));
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_ownsDatabase)
                {
                    _databaseService?.Dispose();
                }
                _disposed = true; // Mark as released
            }
        }
        #endregion
    }
}
