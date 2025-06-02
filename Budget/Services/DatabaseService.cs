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

        /// <summary>
        /// Flag to track if the object has been disposed.
        /// </summary>
        private bool _disposed = false;

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
        public SQLiteConnection Connection
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(DatabaseService));
                return _connection;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseService"/> class.
        /// Creates the database file if it doesn't exist and establishes a connection.
        /// </summary>
        /// <param name="databasePath">The file path where the SQLite database is located or should be created.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="databasePath"/> is null or empty.
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown when database connection fails due to file access issues, invalid path, or SQLite errors.
        /// </exception>
        public DatabaseService(string databasePath)
        {
            // Parameter validation outside try block to preserve ArgumentNullException
            if (databasePath == null)
                throw new ArgumentNullException(nameof(databasePath));

            if (string.IsNullOrWhiteSpace(databasePath))
                throw new ArgumentException("Database path cannot be empty or whitespace.", nameof(databasePath));

            try
            {
                _databasePath = databasePath;

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
        /// If a file already exists at the specified path, it will be deleted and replaced.
        /// </summary>
        /// <param name="databasePath">The file path where the new database should be created.</param>
        /// <returns>A new <see cref="DatabaseService"/> instance connected to the newly created database.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="databasePath"/> is null or empty.
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown when database creation fails due to file access issues, invalid path, or table creation errors.
        /// </exception>
        public static DatabaseService CreateNewDatabase(string databasePath)
        {
            // Force creation of new database by deleting existing file
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }

            var dbService = new DatabaseService(databasePath);
            dbService.CreateTables();
            return dbService;
        }

        /// <summary>
        /// Opens an existing database file for use.
        /// </summary>
        /// <param name="databasePath">The file path to the existing database file.</param>
        /// <returns>A new <see cref="DatabaseService"/> instance connected to the existing database.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="databasePath"/> is null or empty.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Thrown when the specified database file does not exist.
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown when database connection fails due to file access issues or SQLite errors.
        /// </exception>
        public static DatabaseService OpenExisting(string databasePath)
        {
            if (databasePath == null)
                throw new ArgumentNullException(nameof(databasePath));

            if (string.IsNullOrWhiteSpace(databasePath))
                throw new ArgumentException("Database path cannot be empty or whitespace.", nameof(databasePath));

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
        private void InsertDefaultCategoryTypes()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"INSERT OR IGNORE INTO categoryTypes(Id, Description) VALUES (@Id, @Description);";

            foreach (CategoryType categoryType in Enum.GetValues(typeof(CategoryType)))
            {
                command.Parameters.AddWithValue("@Id", (int)categoryType);
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
        public void Dispose()
        {
            if (!_disposed)
            {
                _connection?.Close();
                _connection?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }
}