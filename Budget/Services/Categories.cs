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

        /// <summary>
        /// Gets the full path to the database file.
        /// </summary>
        /// <value>The full path as string, or empty string if no database is connected.</value>
        public string DatabasePath => Path.GetFullPath(_databaseService?.Connection?.DataSource ?? "");

        /// <summary>
        /// Gets a value indicating whether the database connection is open.
        /// </summary>
        /// <value>true if connected; otherwise, false.</value>
        public bool IsConnected => _databaseService?.Connection?.State == System.Data.ConnectionState.Open;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the Categories class using an existing DatabaseService.
        /// </summary>
        /// <param name="databaseService">An existing DatabaseService instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when databaseService is null.</exception>
        /// <remarks>This constructor does not take ownership of the database connection.</remarks>
        public Categories(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _ownsDatabase = false; // External ownership
        }

        /// <summary>
        /// Initializes a new instance of the Categories class with a database path.
        /// </summary>
        /// <param name="databasePath">Path to the SQLite database file.</param>
        /// <param name="isNew">If true, creates a new database and sets up default categories.</param>
        /// <exception cref="ArgumentNullException">Thrown when databasePath is null or whitespace.</exception>
        /// <remarks>This constructor takes ownership of the database connection.</remarks>
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
        /// Populates the database with default categories for a new budget system.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when categories already exist in the database.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the Categories instance has been disposed.</exception>
        /// <remarks>
        /// Adds the following default categories:
        /// - Expenses: Utilities, Food &amp; Dining, Transportation, Health &amp; Personal Care, Insurance, Clothes, Education, Vacation, Social Expenses, Municipal &amp; School Tax, Rental Expenses, Miscellaneous
        /// - Savings: Savings  
        /// - Debt: Housing mortgage, Auto loan
        /// - Income: Salary, Rental Income
        /// - Investment: Stock &amp; Fund
        /// </remarks>
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

        /// <summary>
        /// Adds a new category to the database.
        /// </summary>
        /// <param name="name">The name of the category.</param>
        /// <param name="type">The type of category (Income, Expense, Savings, Debt, Investment).</param>
        /// <returns>The ID of the newly created category.</returns>
        /// <exception cref="ArgumentException">Thrown when name is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the insert operation fails.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the Categories instance has been disposed.</exception>
        /// <example>
        /// <code>
        /// int categoryId = categories.Add("Entertainment", CategoryType.Expense);
        /// </code>
        /// </example>
        public int Add(string name, Category.CategoryType type)
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
        /// Deletes a category from the database if it has no associated transactions.
        /// </summary>
        /// <param name="id">The ID of the category to delete.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the category has associated transactions or doesn't exist.
        /// </exception>
        /// <exception cref="ObjectDisposedException">Thrown when the Categories instance has been disposed.</exception>
        /// <remarks>
        /// This method prevents deletion of categories that are referenced by transactions 
        /// to maintain referential integrity. All associated transactions must be deleted 
        /// or reassigned before the category can be removed.
        /// </remarks>
        /// <example>
        /// <code>
        /// try
        /// {
        ///     categories.Delete(5);
        /// }
        /// catch (InvalidOperationException ex)
        /// {
        ///     Console.WriteLine(ex.Message); // "Cannot delete category with 3 associated transactions..."
        /// }
        /// </code>
        /// </example>
        public void Delete(int id)
        {
            EnsureNotDisposed();

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

        /// <summary>
        /// Retrieves all categories from the database, ordered by type and name.
        /// </summary>
        /// <returns>A list of Category objects representing all categories in the database.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the Categories instance has been disposed.</exception>
        public List<Category> List()
        {
            EnsureNotDisposed();
            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                                SELECT c.Id, c.Name, c.TypeId
                                FROM categories c
                                ORDER BY c.TypeId, c.Name";

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

        /// <summary>
        /// Updates an existing category's name and type.
        /// </summary>
        /// <param name="id">The ID of the category to update.</param>
        /// <param name="name">The new name for the category.</param>
        /// <param name="type">The new type for the category.</param>
        /// <exception cref="ObjectDisposedException">Thrown when the Categories instance has been disposed.</exception>
        public void UpdateCategory(int id, string name, CategoryType type)
        {
            EnsureNotDisposed();
            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                                UPDATE categories 
                                SET Name = @name, TypeId = @typeId 
                                WHERE Id = @id";

            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@typeId", (int)type);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Retrieves all categories of a specific type, ordered by name.
        /// </summary>
        /// <param name="type">The category type to filter by.</param>
        /// <returns>A list of Category objects matching the specified type.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the Categories instance has been disposed.</exception>
        /// <example>
        /// <code>
        /// var expenses = categories.GetCategoriesByType(CategoryType.Expense);
        /// </code>
        /// </example>
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
        /// Retrieves a specific category by its ID.
        /// </summary>
        /// <param name="id">The ID of the category to retrieve.</param>
        /// <returns>A Category object if found; otherwise, null.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the Categories instance has been disposed.</exception>
        public Category GetCategoryFromId(int id)
        {
            EnsureNotDisposed();

            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                                    SELECT c.Id, c.Name, c.TypeId
                                    FROM categories c
                                    WHERE c.Id = @id";

            command.Parameters.AddWithValue("@id", id);

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
        /// Ensures that the Categories instance has not been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the Categories instance has been disposed.</exception>
        private void EnsureNotDisposed()
        {
            if (_disposed) // Check whether the Categories object has been released
                throw new ObjectDisposedException(nameof(Categories));
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Releases all resources used by the Categories instance.
        /// </summary>
        /// <remarks>
        /// If this instance owns the database connection, it will be disposed.
        /// If the database connection is externally owned, it will not be disposed.
        /// </remarks>
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
