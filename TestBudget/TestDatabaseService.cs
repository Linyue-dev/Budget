using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using Xunit;
using Budget.Services;
using Budget.Models;

namespace Budget.Tests
{
    /// <summary>
    /// Comprehensive unit tests for the fixed DatabaseService class
    /// Tests all public methods, error handling, and resource management
    /// </summary>
    public class DatabaseServiceTests : IDisposable
    {
        private readonly List<string> _testDbPaths = new List<string>();

        #region Constructor Tests

        [Fact]
        public void Constructor_ValidPath_ShouldCreateConnection()
        {
            // Arrange
            var dbPath = GetTestDbPath("constructor_valid_test.db");

            // Act
            using var service = new DatabaseService(dbPath);

            // Assert
            Assert.True(File.Exists(dbPath));
            Assert.NotNull(service.Connection);
            Assert.Equal(ConnectionState.Open, service.Connection.State);
        }

        [Fact]
        public void Constructor_NullPath_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DatabaseService(null));
        }

        [Fact]
        public void Constructor_EmptyPath_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DatabaseService(""));
            Assert.Throws<ArgumentException>(() => new DatabaseService("   "));
        }

        [Fact]
        public void Constructor_InvalidPath_ShouldThrowException()
        {
            // Arrange
            var invalidPath = "Z:\\NonExistent\\Directory\\test.db";

            // Act & Assert
            Assert.Throws<Exception>(() => new DatabaseService(invalidPath));
        }

        [Fact]
        public void Constructor_ExistingValidDatabase_ShouldConnectSuccessfully()
        {
            // Arrange
            var dbPath = GetTestDbPath("existing_valid_test.db");
            using (var firstService = new DatabaseService(dbPath))
            {
                // Create a valid database first
                Assert.Equal(ConnectionState.Open, firstService.Connection.State);
            }

            // Act
            using var secondService = new DatabaseService(dbPath);

            // Assert
            Assert.Equal(ConnectionState.Open, secondService.Connection.State);
        }

        #endregion

        #region CreateNewDatabase Tests

        [Fact]
        public void CreateNewDatabase_FileAlreadyExists_ShouldReplaceWithNewDatabase()
        {
            // Arrange
            var dbPath = GetTestDbPath("replace_existing_test.db");

            // Create initial database with some data
            using (var firstService = DatabaseService.CreateNewDatabase(dbPath))
            {
                using var insertCmd = firstService.Connection.CreateCommand();  // 
                insertCmd.CommandText = "INSERT INTO categories (Name, TypeId) VALUES ('TestCategory', 2)";
                insertCmd.ExecuteNonQuery();
            }

            // Act - Create new database (should replace existing)
            using var newService = DatabaseService.CreateNewDatabase(dbPath);

            // Assert
            Assert.True(File.Exists(dbPath));
            Assert.Equal(ConnectionState.Open, newService.Connection.State);

            // Verify it's a fresh database (no custom categories)
            using var selectCmd = newService.Connection.CreateCommand();  //
            selectCmd.CommandText = "SELECT COUNT(*) FROM categories";
            var categoryCount = Convert.ToInt32(selectCmd.ExecuteScalar());
            Assert.Equal(0, categoryCount);
        }

        [Fact]
        public void CreateNewDatabase_NullPath_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => DatabaseService.CreateNewDatabase(null));
        }

        [Fact]
        public void CreateNewDatabase_EmptyPath_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => DatabaseService.CreateNewDatabase(""));
            Assert.Throws<ArgumentException>(() => DatabaseService.CreateNewDatabase("   "));
        }

        #endregion

        #region OpenExisting Tests

        [Fact]
        public void OpenExisting_ValidExistingDatabase_ShouldConnectSuccessfully()
        {
            // Arrange
            var dbPath = GetTestDbPath("open_existing_valid_test.db");
            using (var createService = DatabaseService.CreateNewDatabase(dbPath))
            {
                // Ensure database is properly created
                Assert.Equal(ConnectionState.Open, createService.Connection.State);
            }

            // Act
            using var openService = DatabaseService.OpenExisting(dbPath);

            // Assert
            Assert.Equal(ConnectionState.Open, openService.Connection.State);
            Assert.True(GetCategoryTypeCount(openService) > 0);
        }

        [Fact]
        public void OpenExisting_NonExistentFile_ShouldThrowFileNotFoundException()
        {
            // Act & Assert
            Assert.Throws<FileNotFoundException>(() =>
                DatabaseService.OpenExisting("nonexistent_file.db"));
        }

        [Fact]
        public void OpenExisting_NullPath_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => DatabaseService.OpenExisting(null));
        }

        [Fact]
        public void OpenExisting_TextFile_ShouldConnectButFailOnTableQuery()
        {
            // Arrange
            var dbPath = GetTestDbPath("text_file_test.db");
            File.WriteAllText(dbPath, "This is just a text file, not SQLite");

            // Act
            using var service = DatabaseService.OpenExisting(dbPath);

            // Assert - Connection succeeds (SQLite is tolerant)
            Assert.Equal(ConnectionState.Open, service.Connection.State);

            // But table queries should fail
            Assert.Throws<SQLiteException>(() => GetTableCount(service));
        }

        #endregion

        #region CreateTables Tests

        [Fact]
        public void CreateTables_ShouldCreateAllRequiredTables()
        {
            // Arrange
            var dbPath = GetTestDbPath("create_tables_test.db");

            // Act
            using var service = DatabaseService.CreateNewDatabase(dbPath);

            // Assert
            var tableNames = GetTableNames(service);
            Assert.Contains("categoryTypes", tableNames);
            Assert.Contains("categories", tableNames);
            Assert.Contains("transactions", tableNames);

            // Verify table structures
            VerifyTableStructure(service, "categoryTypes", new[] { "Id", "Description" });
            VerifyTableStructure(service, "categories", new[] { "Id", "Name", "TypeId" });
            VerifyTableStructure(service, "transactions", new[] { "Id", "CategoryId", "Amount", "TransactionDate", "Description" });
        }

        [Fact]
        public void CreateTables_CalledMultipleTimes_ShouldNotDuplicateData()
        {
            // Arrange
            var dbPath = GetTestDbPath("multiple_create_tables_test.db");
            using var service = DatabaseService.CreateNewDatabase(dbPath);
            var initialCategoryTypeCount = GetCategoryTypeCount(service);

            // Act
            service.CreateTables();

            // Assert
            Assert.Equal(initialCategoryTypeCount, GetCategoryTypeCount(service));
        }

        #endregion

        #region InsertDefaultCategoryTypes Tests (Indirect)

        [Fact]
        public void InsertDefaultCategoryTypes_ShouldPopulateAllEnumValues()
        {
            // Arrange
            var dbPath = GetTestDbPath("default_category_types_test.db");

            // Act
            using var service = DatabaseService.CreateNewDatabase(dbPath);

            // Assert
            var categoryTypes = GetCategoryTypesDetail(service);
            var enumValues = Enum.GetValues(typeof(Category.CategoryType)).Cast<Category.CategoryType>().ToList();

            Assert.Equal(enumValues.Count, categoryTypes.Count);

            foreach (Category.CategoryType enumValue in enumValues)
            {
                var expectedId = (int)enumValue;
                var expectedDescription = enumValue.ToString();
                Assert.Contains(categoryTypes, ct => ct.Id == expectedId && ct.Description == expectedDescription);
            }
        }

        [Fact]
        public void InsertDefaultCategoryTypes_ShouldUseCorrectEnumIds()
        {
            // Arrange
            var dbPath = GetTestDbPath("enum_ids_test.db");

            // Act
            using var service = DatabaseService.CreateNewDatabase(dbPath);

            // Assert
            var categoryTypes = GetCategoryTypesDetail(service);
            Assert.Contains(categoryTypes, ct => ct.Id == 1 && ct.Description == "Income");
            Assert.Contains(categoryTypes, ct => ct.Id == 2 && ct.Description == "Expense");
            Assert.Contains(categoryTypes, ct => ct.Id == 3 && ct.Description == "Debt");
            Assert.Contains(categoryTypes, ct => ct.Id == 4 && ct.Description == "Investment");
            Assert.Contains(categoryTypes, ct => ct.Id == 5 && ct.Description == "Savings");
        }

        #endregion

        #region Foreign Key Constraints Tests

        [Fact]
        public void ForeignKeyConstraints_ShouldBeEnabled()
        {
            // Arrange
            var dbPath = GetTestDbPath("foreign_keys_test.db");

            // Act
            using var service = DatabaseService.CreateNewDatabase(dbPath);

            // Assert
            using var comammd = service.Connection.CreateCommand();
            comammd.CommandText = "PRAGMA foreign_keys";
            var result = comammd.ExecuteScalar();
            Assert.Equal(1L, result);
        }

        [Fact]
        public void ForeignKeyConstraints_InvalidTypeId_ShouldFailInsertion()
        {
            // Arrange
            var dbPath = GetTestDbPath("foreign_key_violation_test.db");
            using var service = DatabaseService.CreateNewDatabase(dbPath);

            // Act & Assert
            using var comammd = service.Connection.CreateCommand();
            comammd.CommandText = @"INSERT INTO categories (Name, TypeId) VALUES ('Test Category', 999)";
            Assert.Throws<SQLiteException>(() => comammd.ExecuteNonQuery());
        }

        #endregion

        #region Connection Property Tests

        [Fact]
        public void Connection_AfterCreation_ShouldBeAccessible()
        {
            // Arrange
            var dbPath = GetTestDbPath("connection_property_test.db");

            // Act
            using var service = DatabaseService.CreateNewDatabase(dbPath);

            // Assert
            Assert.NotNull(service.Connection);
            Assert.Equal(ConnectionState.Open, service.Connection.State);
        }

        [Fact]
        public void Connection_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var dbPath = GetTestDbPath("connection_after_dispose_test.db");
            var service = DatabaseService.CreateNewDatabase(dbPath);

            // Act
            service.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => service.Connection);
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_ShouldCloseConnection()
        {
            // Arrange
            var dbPath = GetTestDbPath("dispose_close_test.db");
            var service = DatabaseService.CreateNewDatabase(dbPath);

            // Verify connection is initially open
            Assert.Equal(ConnectionState.Open, service.Connection.State);

            // Act
            service.Dispose();

            // Assert - After dispose, accessing Connection should throw
            Assert.Throws<ObjectDisposedException>(() => service.Connection);
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            var dbPath = GetTestDbPath("multiple_dispose_test.db");
            var service = DatabaseService.CreateNewDatabase(dbPath);

            // Act & Assert
            service.Dispose();

            // Multiple calls to Dispose should not throw
            var exception = Record.Exception(() => service.Dispose());
            Assert.Null(exception);

            // Third time
            exception = Record.Exception(() => service.Dispose());
            Assert.Null(exception);
        }

        [Fact]
        public void UsingStatement_ShouldDisposeAutomatically()
        {
            // Arrange
            var dbPath = GetTestDbPath("using_statement_test.db");
            DatabaseService service;

            // Act
            using (service = DatabaseService.CreateNewDatabase(dbPath))
            {
                Assert.Equal(ConnectionState.Open, service.Connection.State);
            }

            // Assert
            Assert.Throws<ObjectDisposedException>(() => service.Connection);
        }

        #endregion

        #region Data Persistence Tests

        [Fact]
        public void DataPersistence_CreateCloseReopen_ShouldPreserveData()
        {
            // Arrange
            var dbPath = GetTestDbPath("data_persistence_test.db");
            int initialCategoryTypeCount;
            List<string> initialTableNames;

            // Act - Create database, get initial state, then close
            using (var createService = DatabaseService.CreateNewDatabase(dbPath))
            {
                initialCategoryTypeCount = GetCategoryTypeCount(createService);
                initialTableNames = GetTableNames(createService);
            }

            // Reopen database
            using var reopenService = DatabaseService.OpenExisting(dbPath);

            // Assert
            Assert.Equal(initialCategoryTypeCount, GetCategoryTypeCount(reopenService));
            var persistedTableNames = GetTableNames(reopenService);
            Assert.Equal(initialTableNames.Count, persistedTableNames.Count);

            foreach (var tableName in initialTableNames)
            {
                Assert.Contains(tableName, persistedTableNames);
            }
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void CreateNewDatabase_Performance_ShouldCompleteWithinReasonableTime()
        {
            // Arrange
            var dbPath = GetTestDbPath("performance_test.db");
            var startTime = DateTime.Now;

            // Act
            using var service = DatabaseService.CreateNewDatabase(dbPath);

            // Assert
            var duration = DateTime.Now - startTime;
            Assert.True(duration.TotalSeconds < 5,
                $"Database creation took too long: {duration.TotalSeconds} seconds");
        }

        #endregion

        #region Helper Methods

        private string GetTestDbPath(string fileName)
        {
            var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}_{fileName}");
            _testDbPaths.Add(path);
            return path;
        }

        private int GetTableCount(DatabaseService db)
        {
            using var comammd = db.Connection.CreateCommand();
            comammd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table'";
            return Convert.ToInt32(comammd.ExecuteScalar());
        }

        private List<string> GetTableNames(DatabaseService db)
        {
            var tables = new List<string>();
            using var comammd = db.Connection.CreateCommand();
            comammd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
            using var reader = comammd.ExecuteReader();
            while (reader.Read())
            {
                tables.Add(reader.GetString(0));
            }
            return tables;
        }

        private int GetCategoryTypeCount(DatabaseService db)
        {
            using var comammd = db.Connection.CreateCommand();
            comammd.CommandText = "SELECT COUNT(*) FROM categoryTypes";
            return Convert.ToInt32(comammd.ExecuteScalar());
        }

        private List<(int Id, string Description)> GetCategoryTypesDetail(DatabaseService db)
        {
            var categoryTypes = new List<(int Id, string Description)>();
            using var comammd = db.Connection.CreateCommand();
            comammd.CommandText = "SELECT Id, Description FROM categoryTypes ORDER BY Id";
            using var reader = comammd.ExecuteReader();
            while (reader.Read())
            {
                categoryTypes.Add((reader.GetInt32(0), reader.GetString(1)));
            }
            return categoryTypes;
        }

        private void VerifyTableStructure(DatabaseService db, string tableName, string[] expectedColumns)
        {
            using var comammd = db.Connection.CreateCommand();
            comammd.CommandText = $"PRAGMA table_info({tableName})";
            using var reader = comammd.ExecuteReader();

            var actualColumns = new List<string>();
            while (reader.Read())
            {
                actualColumns.Add(reader.GetString("name"));
            }

            foreach (var expectedColumn in expectedColumns)
            {
                Assert.Contains(expectedColumn, actualColumns);
            }
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            // Clean up test database files
            foreach (var dbPath in _testDbPaths)
            {
                try
                {
                    if (File.Exists(dbPath))
                    {
                        File.Delete(dbPath);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
        #endregion
    }
}