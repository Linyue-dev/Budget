using System;
using System.Data.SQLite;
using System.IO;
using Budget.Models;
using static Budget.Models.Category;

namespace Budget.Services
{
    /// <summary>
    /// Provides database connection and management functionality for the Budget application.
    /// Manages SQLite database operations including creation, connection, and table initialization.
    /// </summary>
    /// <remarks>
    /// This service handles both new database creation and existing database connections.
    /// It automatically creates necessary tables and populates default category types based on the CategoryType enumeration.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a new database
    /// using var db = DatabaseService.CreateNewDatabase("budget.db");
    /// 
    /// // Open existing database
    /// using var existingDb = DatabaseService.OpenExisting("existing_budget.db");
    /// 
    /// // Use the connection
    /// using var command = db.Connection.CreateCommand();
    /// command.CommandText = "SELECT * FROM categories";
    /// var reader = command.ExecuteReader();
    /// </code>
    /// </example>
    public class DatabaseService : IDisposable
    {
        #region Private Fields

        /// <summary>
        /// The active SQLite database connection.
        /// </summary>
        private SQLiteConnection _connection;

        /// <summary>
        /// The file path to the database file.
        /// </summary>
        private readonly string _databasePath;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current SQLite database connection.
        /// </summary>
        /// <value>
        /// The active <see cref="SQLiteConnection"/> instance used for database operations.
        /// </value>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the DatabaseService has been disposed.
        /// </exception>
        /// <example>
        /// <code>
        /// using var command = databaseService.Connection.CreateCommand();
        /// command.CommandText = "SELECT COUNT(*) FROM categories";
        /// var count = command.ExecuteScalar();
        /// </code>
        /// </example>
        public SQLiteConnection Connection
        {
            get { return _connection; }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseService"/> class.
        /// Creates the database file if it doesn't exist and establishes a connection.
        /// </summary>
        /// <param name="databasePath">The file path where the SQLite database is located or should be created.</param>
        /// <exception cref="Exception">
        /// Thrown when database connection fails due to file access issues, invalid path, or SQLite errors.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="databasePath"/> is null or empty.
        /// </exception>
        /// <remarks>
        /// This constructor will create a new database file if one doesn't exist at the specified path.
        /// Foreign key constraints are enabled by default in the connection string.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Create connection to new or existing database
        /// var dbService = new DatabaseService(@"C:\MyApp\budget.db");
        /// </code>
        /// </example>
        public DatabaseService(string databasePath)
        {
            try
            {
                _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));

                if (!File.Exists(databasePath))
                    SQLiteConnection.CreateFile(databasePath);

                _connection = new SQLiteConnection($"Data Source={databasePath}; Foreign Keys=1");
                _connection.Open();
            }
            catch (Exception ex)
            {
                throw new Exception($"Database connect fail: {ex.Message}", ex);
            }
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new database with all necessary tables and default data.
        /// </summary>
        /// <param name="databasePath">The file path where the new database should be created.</param>
        /// <returns>A new <see cref="DatabaseService"/> instance connected to the newly created database.</returns>
        /// <exception cref="Exception">
        /// Thrown when database creation fails due to file access issues, invalid path, or table creation errors.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="databasePath"/> is null or empty.
        /// </exception>
        /// <remarks>
        /// This method creates a complete database schema including:
        /// <list type="bullet">
        /// <item><description>categoryTypes table with default CategoryType enumeration values</description></item>
        /// <item><description>categories table for user-defined categories</description></item>
        /// <item><description>transactions table for financial transactions</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// using var newDb = DatabaseService.CreateNewDatabase("budget.db");
        /// // Database is ready to use with all tables created
        /// </code>
        /// </example>
        public static DatabaseService CreateNewDatabase(string databasePath)
        {
            var dbService = new DatabaseService(databasePath);
            dbService.CreateTables();
            return dbService;
        }

        /// <summary>
        /// Opens an existing database file for use.
        /// </summary>
        /// <param name="databasePath">The file path to the existing database file.</param>
        /// <returns>A new <see cref="DatabaseService"/> instance connected to the existing database.</returns>
        /// <exception cref="FileNotFoundException">
        /// Thrown when the specified database file does not exist.
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown when database connection fails due to file access issues or SQLite errors.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="databasePath"/> is null or empty.
        /// </exception>
        /// <remarks>
        /// This method assumes the database file already exists and has the correct schema.
        /// No table creation or data initialization is performed.
        /// </remarks>
        /// <example>
        /// <code>
        /// using var existingDb = DatabaseService.OpenExisting("existing_budget.db");
        /// // Connected to existing database
        /// </code>
        /// </example>
        public static DatabaseService OpenExisting(string databasePath)
        {
            if (!File.Exists(databasePath))
                throw new FileNotFoundException($"File doesn't exist: {databasePath}");

            return new DatabaseService(databasePath);
        }

        #endregion

        #region Table Creation

        /// <summary>
        /// Creates all necessary database tables and populates them with default data.
        /// </summary>
        /// <exception cref="Exception">
        /// Thrown when table creation fails due to SQL syntax errors, constraint violations, or database access issues.
        /// </exception>
        /// <remarks>
        /// This method creates the following tables:
        /// <list type="bullet">
        /// <item><description><c>categoryTypes</c> - Stores CategoryType enumeration values</description></item>
        /// <item><description><c>categories</c> - Stores user-defined financial categories</description></item>
        /// <item><description><c>transactions</c> - Stores individual financial transactions</description></item>
        /// </list>
        /// All tables use INTEGER PRIMARY KEY for auto-incrementing IDs and include appropriate foreign key constraints.
        /// </remarks>
        /// <example>
        /// <code>
        /// var dbService = new DatabaseService("new.db");
        /// dbService.CreateTables();
        /// // Database now has all required tables
        /// </code>
        /// </example>
        public void CreateTables()
        {
            try
            {
                using var command = _connection.CreateCommand();

                // Create categoryTypes table
                command.CommandText = @"CREATE TABLE IF NOT EXISTS categoryTypes(
                    Id INTEGER PRIMARY KEY, 
                    Description TEXT NOT NULL);";
                command.ExecuteNonQuery();

                // Create categories table
                command.CommandText = @"CREATE TABLE IF NOT EXISTS categories(
                    Id INTEGER PRIMARY KEY, 
                    Name TEXT NOT NULL, 
                    TypeId INTEGER NOT NULL, 
                    FOREIGN KEY(TypeId) REFERENCES categoryTypes(Id));";
                command.ExecuteNonQuery();

                // Create transactions table
                command.CommandText = @"CREATE TABLE IF NOT EXISTS transactions(
                    Id INTEGER PRIMARY KEY, 
                    CategoryId INTEGER NOT NULL, 
                    Amount DECIMAL(10,2) NOT NULL, 
                    Date TEXT NOT NULL, 
                    Description TEXT, 
                    FOREIGN KEY(CategoryId) REFERENCES categories(Id));";
                command.ExecuteNonQuery();

                // Populate default category types
                InsertDefaultCategoryTypes();
            }
            catch (Exception ex)
            {
                throw new Exception($"Create table fail: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Inserts default category types from the CategoryType enumeration into the categoryTypes table.
        /// </summary>
        /// <remarks>
        /// This method populates the categoryTypes table with all values from the <see cref="CategoryType"/> enumeration.
        /// The enumeration integer values are used as database IDs to maintain consistency between code and database.
        /// Uses INSERT OR IGNORE to prevent duplicate entries if the method is called multiple times.
        /// </remarks>
        /// <example>
        /// The following category types will be inserted:
        /// <list type="bullet">
        /// <item><description>Income (Id: 1)</description></item>
        /// <item><description>Expense (Id: 2)</description></item>
        /// <item><description>Debt (Id: 3)</description></item>
        /// <item><description>Investment (Id: 4)</description></item>
        /// <item><description>Savings (Id: 5)</description></item>
        /// </list>
        /// </example>
        public void InsertDefaultCategoryTypes()
        {
            using var command = _connection.CreateCommand();
            // Insert both Id and Description simultaneously to keep enumeration value consistent with database Id
            command.CommandText = @"INSERT OR IGNORE INTO categoryTypes(Id, Description) VALUES (@Id, @Description);";

            foreach (CategoryType categoryType in Enum.GetValues(typeof(CategoryType)))
            {
                command.Parameters.AddWithValue("@Id", (int)categoryType);        // Use integer values of enumeration
                command.Parameters.AddWithValue("@Description", categoryType.ToString());
                command.ExecuteNonQuery();
                command.Parameters.Clear();
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Releases all resources used by the <see cref="DatabaseService"/>.
        /// </summary>
        /// <remarks>
        /// This method closes the database connection and disposes of the SQLiteConnection object.
        /// It's safe to call this method multiple times.
        /// After disposal, the Connection property should not be accessed.
        /// </remarks>
        /// <example>
        /// <code>
        /// var dbService = DatabaseService.CreateNewDatabase("test.db");
        /// // Use the database...
        /// dbService.Dispose(); // Clean up resources
        /// 
        /// // Or use with using statement for automatic disposal
        /// using var dbService = DatabaseService.CreateNewDatabase("test.db");
        /// // Automatically disposed when leaving scope
        /// </code>
        /// </example>
        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }

        #endregion
    }
}