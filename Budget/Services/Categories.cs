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
    /// <summary>
    /// Provides services for managing categories in the budget application.
    /// Handles creation, retrieval, update, and deletion of category records.
    /// </summary>
    public class Categories : IDisposable
    {
        #region Private Fields
        private DatabaseService _databaseService;
        private readonly bool _ownsDatabase;
        private bool _disposed = false;
        #endregion

        #region Public Properties    
        /// <summary>
        /// Gets the full path of the connected database.
        /// </summary>
        public string DatabasePath => Path.GetFullPath(_databaseService?.Connection?.DataSource ?? "");

        /// <summary>
        /// Gets a value indicating whether the database connection is open.
        /// </summary>
        public bool IsConnected => _databaseService?.Connection?.State == System.Data.ConnectionState.Open;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Categories"/> class with an existing <see cref="DatabaseService"/>.
        /// </summary>
        /// <param name="databaseService">The existing database service instance.</param>
        /// <exception cref="ArgumentNullException">Thrown if the databaseService is null.</exception>
        public Categories(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _ownsDatabase = false; // External ownership
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Categories"/> class and creates or opens a database at the specified path.
        /// </summary>
        /// <param name="databasePath">The file path of the SQLite database.</param>
        /// <param name="isNew">Indicates whether to create a new database.</param>
        /// <exception cref="ArgumentNullException">Thrown if the databasePath is null or empty.</exception>
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

        /// <summary>
        /// Adds a predefined list of default categories to the database.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if categories already exist in the database.</exception>
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
            AddCategory("Utilities", CategoryType.Expense);
            AddCategory("Food & Dining", CategoryType.Expense);
            AddCategory("Transportation", CategoryType.Expense);
            AddCategory("Health & Personal Care", CategoryType.Expense);
            AddCategory("Insurance", CategoryType.Expense);
            AddCategory("Clothes", CategoryType.Expense);
            AddCategory("Education", CategoryType.Expense);
            AddCategory("Vacation", CategoryType.Expense);
            AddCategory("Social Expenses", CategoryType.Expense);
            AddCategory("Municipal & School Tax", CategoryType.Expense);
            AddCategory("Miscellaneous", CategoryType.Expense);
            AddCategory("Savings", CategoryType.Savings);
            AddCategory("Housing mortgage", CategoryType.Debt);
            AddCategory("Auto loan", CategoryType.Debt);
            AddCategory("Salary", CategoryType.Income);
            AddCategory("Rental Income", CategoryType.Income);
            AddCategory("Stock & Fund", CategoryType.Investment);
        }

        /// <summary>
        /// Adds a new category to the database.
        /// </summary>
        /// <param name="name">The name of the category.</param>
        /// <param name="type">The type of the category.</param>
        /// <returns>The ID of the newly inserted category.</returns>
        /// <exception cref="ArgumentException">Thrown if the category name is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown if insertion fails.</exception>
        public int AddCategory(string name, Category.CategoryType type)
        {
            EnsureNotDisposed();

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Category name cannot be null or empty.", nameof(name));

            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                            INSERT INTO categories (Name, TypeId) 
                            VALUES (@name, @typeId);
                            SELECT last_insert_rowid();"; // Get just insert id

            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@typeId", (int)type);

            var result = command.ExecuteScalar(); // result = new record id
            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException("Failed to insert category");

            return Convert.ToInt32(result); // return to user
        }

        /// <summary>
        /// Deletes a category by its ID if no transactions are associated with it.
        /// </summary>
        /// <param name="categoryId">The ID of the category to delete.</param>
        /// <exception cref="InvalidOperationException">Thrown if transactions are associated or category not found.</exception>
        public void DeleteCategory(int categoryId)
        {
            EnsureNotDisposed();

            using var checkCommand = _databaseService.Connection.CreateCommand();
            checkCommand.CommandText = @"SELECT COUNT(*) FROM transactions WHERE CategoryId = @categoryId";
            checkCommand.Parameters.AddWithValue("@categoryId", categoryId);

            var transactionCount = Convert.ToInt32(checkCommand.ExecuteScalar());
            if (transactionCount > 0)
            {
                throw new InvalidOperationException($"Cannot delete category with {transactionCount} associated transactions. Delete the transactions first.");
            }

            // Safe Deletion Categories
            using var deleteCommand = _databaseService.Connection.CreateCommand();
            deleteCommand.CommandText = "DELETE FROM categories WHERE Id = @categoryId";
            deleteCommand.Parameters.AddWithValue("@categoryId", categoryId);

            var rowsAffected = deleteCommand.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Category with ID {categoryId} not found.");
            }
        }
        /// <summary>
        /// Retrieves all categories from the database.
        /// </summary>
        /// <returns>A list of all categories.</returns>
        public List<Category> GetAllCategories()
        {
            EnsureNotDisposed();
            var categories = new List<Category>();

            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                                SELECT c.Id, c.Name, c.TypeId
                                FROM categories c
                                ORDER BY c.TypeId, c.Name";

            using SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                categories.Add(new Category(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    (CategoryType)reader.GetInt32(2)
                ));
            }
            return categories;
        }
        /// <summary>
        /// Updates the name and type of a category.
        /// </summary>
        /// <param name="categoryId">The ID of the category to update.</param>
        /// <param name="name">The new name of the category.</param>
        /// <param name="type">The new type of the category.</param>
        public void UpdateCategory(int categoryId, string name, CategoryType type)
        {
            EnsureNotDisposed();
            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                                UPDATE categories 
                                SET Name = @name, TypeId = @typeId 
                                WHERE Id = @id";

            command.Parameters.AddWithValue("@id", categoryId);
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@typeId", (int)type);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Retrieves all categories matching a specified type.
        /// </summary>
        /// <param name="type">The type of category to filter by.</param>
        /// <returns>A list of categories of the specified type.</returns>
        public List<Category> GetCategoriesByType(CategoryType type)
        {
            EnsureNotDisposed();
            var categories = new List<Category>();

            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                            SELECT c.Id, c.Name, c.TypeId
                            FROM categories c
                            WHERE c.TypeId = @typeId
                            ORDER BY c.Name";
            command.Parameters.AddWithValue("@typeId", (int)type);

            using var reader = command.ExecuteReader();

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

        /// <summary>
        /// Retrieves a single category based on its ID.
        /// </summary>
        /// <param name="categoryId">The ID of the category.</param>
        /// <returns>The corresponding category, or null if not found.</returns>
        public Category? GetCategoryFromId(int categoryId)
        {
            EnsureNotDisposed();

            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                                    SELECT c.Id, c.Name, c.TypeId
                                    FROM categories c
                                    WHERE c.Id = @id";

            command.Parameters.AddWithValue("@id", categoryId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Category(
                    reader.GetInt32("Id"),
                    reader.GetString("Name"),
                    (CategoryType)reader.GetInt32("TypeId")
                );
            }
            return null;
        }

        #region Helper Methods

        /// <summary>
        /// Throws an exception if this instance has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if this instance has already been disposed.</exception>
        private void EnsureNotDisposed()
        {
            if (_disposed) // Check whether the Categories object has been released
                throw new ObjectDisposedException(nameof(Categories));
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Releases all resources used by this instance of <see cref="Categories"/>.
        /// </summary>
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
