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

        public int AddTransaction(DateTime date, int categoryId, decimal amount, string description)
        {
            EnsureNotDisposed();

            if (amount == 0)
                throw new ArgumentException("Amount cannot be zero.", nameof(amount));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description cannot be null or empty.", nameof(description));

            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                            INSERT INTO transactions (Date, Description, Amount, CategoryId)
                            VALUES (@date, @description, @amount, @categoryId);
                            SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@description", description);
            command.Parameters.AddWithValue("@amount", amount);
            command.Parameters.AddWithValue("@categoryId", categoryId); 

            var result = command.ExecuteScalar();
            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException("Failed to insert transaction");

            return Convert.ToInt32(result);
        }

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

        public List<Transaction> GetAllTransactions()
        {
            EnsureNotDisposed();
            var transactions = new List<Transaction>();

            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                            SELECT t.Id, t.Date, t.Description, t.Amount, t.CategoryId
                            FROM transactions t
                            ORDER BY t.Date DESC, t.Id DESC";  

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                transactions.Add(new Transaction(
                    reader.GetInt32("Id"),
                    reader.GetDateTime("Date"),
                    reader.GetString("Description"),
                    Convert.ToDecimal(reader["Amount"]),
                    reader.GetInt32("CategoryId")
                ));
            }
            return transactions;
        }

        public Transaction? GetTransactionFromId(int transactionId)
        {
            EnsureNotDisposed();

            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                            SELECT t.Id, t.Date, t.Description, t.Amount, t.CategoryId
                            FROM transactions t
                            WHERE Id = @transactionId";
            command.Parameters.AddWithValue("@transactionId", transactionId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Transaction(
                    reader.GetInt32("Id"),
                    reader.GetDateTime("Date"),
                    reader.GetString("Description"),
                    Convert.ToDecimal(reader["Amount"]),
                    reader.GetInt32("CategoryId")
                );
            }
            return null;
        }

        public void UpdateTransaction(int transactionId, DateTime date, string description, decimal amount, int categoryId)
        {
            EnsureNotDisposed();

            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                                UPDATE transactions 
                                SET Date = @date, 
                                    Description = @description, 
                                    Amount = @amount, 
                                    CategoryId = @categoryId
                                WHERE Id = @transactionId";  

            command.Parameters.AddWithValue("@transactionId", transactionId);
            command.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@description", description);
            command.Parameters.AddWithValue("@amount", amount);
            command.Parameters.AddWithValue("@categoryId", categoryId);

            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found.");
            }
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
