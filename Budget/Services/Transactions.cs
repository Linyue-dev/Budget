using Budget.Models;
using Budget.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Budget.Services
{
    /// <summary>
    /// Provides comprehensive transaction management functionality for budget applications.
    /// Manages financial transaction data including creation, retrieval, updating, and deletion operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class handles all database operations related to financial transactions and maintains 
    /// the database connection lifecycle. It supports both shared database connections and 
    /// independent database management.
    /// </para>
    /// <para>
    /// The class implements the <see cref="IDisposable"/> pattern to ensure proper resource cleanup.
    /// Always dispose of instances when finished to prevent resource leaks.
    /// </para>
    /// <para>
    /// Thread Safety: This class is not thread-safe. External synchronization is required 
    /// for concurrent access from multiple threads.
    /// </para>
    /// </remarks>
    public class Transactions : IDisposable
    {
        #region Private Fields
        private DatabaseService _databaseService;
        private readonly bool _ownsDatabase;
        private bool _disposed = false;
        #endregion

        #region Properties
        public string DatabasePath => Path.GetFullPath(_databaseService?.Connection?.DataSource ?? "");
        public bool IsConnected => !_disposed && _databaseService?.Connection?.State == System.Data.ConnectionState.Open;
        #endregion

        #region Constructors
        public Transactions(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _ownsDatabase = false;
        }

        public Transactions(string databasePath, bool isNew = false)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
                throw new ArgumentException("Database path cannot be null or empty.", nameof(databasePath));

            if (isNew)
            {
                _databaseService = DatabaseService.CreateNewDatabase(databasePath);
            }
            else
            {
                _databaseService = DatabaseService.OpenExisting(databasePath);
            }
            _ownsDatabase = true;
        }
        #endregion

        #region Transaction Methods

        /// <summary>
        /// Adds a new financial transaction to the database.
        /// </summary>
        /// <param name="transactionDate">The date when the transaction actually occurred</param>
        /// <param name="categoryId">The category ID for this transaction</param>
        /// <param name="amount">The transaction amount</param>
        /// <param name="description">Description of the transaction</param>
        /// <param name="createdBy">The person who created this transaction record</param>
        /// <returns>The ID of the newly created transaction</returns>
        public int AddTransaction(DateTime transactionDate, int categoryId, decimal amount, string description, string createdBy)
        {
            EnsureNotDisposed();

            if (amount == 0)
                throw new ArgumentException("Amount cannot be zero.", nameof(amount));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description cannot be null or empty.", nameof(description));
            if (string.IsNullOrWhiteSpace(createdBy))
                throw new ArgumentException("CreatedBy cannot be null or empty.", nameof(createdBy));

            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO transactions (TransactionDate, Description, Amount, CategoryId, CreatedBy, CreatedAt)
                VALUES (@transactionDate, @description, @amount, @categoryId, @createdBy, @createdAt);
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@transactionDate", transactionDate.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@description", description);
            command.Parameters.AddWithValue("@amount", amount);
            command.Parameters.AddWithValue("@categoryId", categoryId);
            command.Parameters.AddWithValue("@createdBy", createdBy);
            command.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            var result = command.ExecuteScalar();
            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException("Failed to insert transaction");

            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Adds a new transaction with automatic CreatedAt timestamp and specified CreatedBy
        /// </summary>
        public int AddTransaction(DateTime transactionDate, int categoryId, decimal amount, string description, string createdBy, DateTime createdAt)
        {
            EnsureNotDisposed();

            if (amount == 0)
                throw new ArgumentException("Amount cannot be zero.", nameof(amount));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description cannot be null or empty.", nameof(description));
            if (string.IsNullOrWhiteSpace(createdBy))
                throw new ArgumentException("CreatedBy cannot be null or empty.", nameof(createdBy));

            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO transactions (TransactionDate, Description, Amount, CategoryId, CreatedBy, CreatedAt)
                VALUES (@transactionDate, @description, @amount, @categoryId, @createdBy, @createdAt);
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@transactionDate", transactionDate.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@description", description);
            command.Parameters.AddWithValue("@amount", amount);
            command.Parameters.AddWithValue("@categoryId", categoryId);
            command.Parameters.AddWithValue("@createdBy", createdBy);
            command.Parameters.AddWithValue("@createdAt", createdAt.ToString("yyyy-MM-dd HH:mm:ss"));

            var result = command.ExecuteScalar();
            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException("Failed to insert transaction");

            return Convert.ToInt32(result);
        }

        public int AddTransaction(DateTime transactionDate, int categoryId, decimal amount, string description)
        {
            return AddTransaction(transactionDate, categoryId, amount, description, "Unknown");
        }
        /// <summary>
        /// Deletes a transaction from the database.
        /// </summary>
        public void DeleteTransaction(int transactionId)
        {
            EnsureNotDisposed();

            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM transactions
                WHERE Id = @transactionId";

            command.Parameters.AddWithValue("@transactionId", transactionId);

            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found.");
            }
        }

        /// <summary>
        /// Retrieves all transactions from the database, ordered by transaction date descending.
        /// </summary>
        public List<Transaction> GetAllTransactions()
        {
            EnsureNotDisposed();
            var transactions = new List<Transaction>();

            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, TransactionDate, Description, Amount, CategoryId, CreatedBy, CreatedAt
                FROM transactions
                ORDER BY TransactionDate DESC, Id DESC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                transactions.Add(new Transaction(
                    reader.GetInt32("Id"),
                    reader.GetDateTime("TransactionDate"),
                    reader.GetInt32("CategoryId"),
                    Convert.ToDecimal(reader["Amount"]),
                    reader.GetString("Description"),
                    reader.GetString("CreatedBy"),
                    reader.GetDateTime("CreatedAt")
                ));
            }
            return transactions;
        }

        /// <summary>
        /// Retrieves a specific transaction by its unique identifier.
        /// </summary>
        public Transaction? GetTransactionFromId(int transactionId)
        {
            EnsureNotDisposed();

            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, TransactionDate, Description, Amount, CategoryId, CreatedBy, CreatedAt
                FROM transactions
                WHERE Id = @transactionId";
            command.Parameters.AddWithValue("@transactionId", transactionId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Transaction(
                    reader.GetInt32("Id"),
                    reader.GetDateTime("TransactionDate"),
                    reader.GetInt32("CategoryId"),
                    Convert.ToDecimal(reader["Amount"]),
                    reader.GetString("Description"),
                    reader.GetString("CreatedBy"),
                    reader.GetDateTime("CreatedAt")
                );
            }
            return null;
        }

        /// <summary>
        /// Updates an existing transaction with new values.
        /// </summary>
        public void UpdateTransaction(int transactionId, DateTime transactionDate, string description, decimal amount, int categoryId, string createdBy)
        {
            EnsureNotDisposed();

            if (amount == 0)
                throw new ArgumentException("Amount cannot be zero.", nameof(amount));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description cannot be null or empty.", nameof(description));
            if (string.IsNullOrWhiteSpace(createdBy))
                throw new ArgumentException("CreatedBy cannot be null or empty.", nameof(createdBy));

            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                UPDATE transactions 
                SET TransactionDate = @transactionDate, 
                    Description = @description, 
                    Amount = @amount, 
                    CategoryId = @categoryId,
                    CreatedBy = @createdBy
                WHERE Id = @transactionId";

            command.Parameters.AddWithValue("@transactionId", transactionId);
            command.Parameters.AddWithValue("@transactionDate", transactionDate.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@description", description);
            command.Parameters.AddWithValue("@amount", amount);
            command.Parameters.AddWithValue("@categoryId", categoryId);
            command.Parameters.AddWithValue("@createdBy", createdBy);

            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found.");
            }
        }

        /// <summary>
        /// Gets transactions created by a specific person
        /// </summary>
        public List<Transaction> GetTransactionsByCreatedBy(string createdBy)
        {
            EnsureNotDisposed();
            var transactions = new List<Transaction>();

            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, TransactionDate, Description, Amount, CategoryId, CreatedBy, CreatedAt
                FROM transactions
                WHERE CreatedBy = @createdBy
                ORDER BY TransactionDate DESC, Id DESC";

            command.Parameters.AddWithValue("@createdBy", createdBy);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                transactions.Add(new Transaction(
                    reader.GetInt32("Id"),
                    reader.GetDateTime("TransactionDate"),
                    reader.GetInt32("CategoryId"),
                    Convert.ToDecimal(reader["Amount"]),
                    reader.GetString("Description"),
                    reader.GetString("CreatedBy"),
                    reader.GetDateTime("CreatedAt")
                ));
            }
            return transactions;
        }

        /// <summary>
        /// Gets transactions within a date range
        /// </summary>
        public List<Transaction> GetTransactionsByDateRange(DateTime startDate, DateTime endDate)
        {
            EnsureNotDisposed();
            var transactions = new List<Transaction>();

            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, TransactionDate, Description, Amount, CategoryId, CreatedBy, CreatedAt
                FROM transactions
                WHERE TransactionDate BETWEEN @startDate AND @endDate
                ORDER BY TransactionDate DESC, Id DESC";

            command.Parameters.AddWithValue("@startDate", startDate.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy-MM-dd HH:mm:ss"));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                transactions.Add(new Transaction(
                    reader.GetInt32("Id"),
                    reader.GetDateTime("TransactionDate"),
                    reader.GetInt32("CategoryId"),
                    Convert.ToDecimal(reader["Amount"]),
                    reader.GetString("Description"),
                    reader.GetString("CreatedBy"),
                    reader.GetDateTime("CreatedAt")
                ));
            }
            return transactions;
        }

        #endregion

        #region Helper Methods
        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Transactions));
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
                _disposed = true;
            }
        }
        #endregion
    }
}