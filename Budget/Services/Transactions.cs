using Budget.Models;
using Budget.Utils;
using System;
using System.Collections.Generic;
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

        public int Add(DateTime date, int categoryId, decimal amount, string description)
        {
            EnsureNotDisposed();

            if (amount == 0)
                throw new ArgumentException("Amount cannot be zero.", nameof(amount));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description cannot be null or empty.", nameof(description));
            
            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                        INSERT INTO transactions (Date, Description, Amount, CategoryId)
                        VALUES (@date, @description, @amount, @category);
                        SELECT last_insert_rowid();";// Get just insert id

            command.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@description", categoryId);
            command.Parameters.AddWithValue("@amount", amount);
            command.Parameters.AddWithValue("@categoryId", categoryId);

            var result = command.ExecuteScalar();
            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException("Failed to insert transaction");

            return Convert.ToInt32(result);
        }

        public void Delete(int transactionId)
        {
            EnsureNotDisposed();
            using var command = _databaseService.Connection.CreateCommand();
            command.CommandText = @"
                            DELETE FROM Transactions
                            WHERE Id = @transactionId";

            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found.");
            }
        }
        public List<Transaction> List()
        {
            List<Transaction> newList = new List<Transaction>();
            foreach (Transaction transaction in _Transactions)
            {
                newList.Add(new Transaction(transaction));
            }
            return newList;
        }
        #region Helper Methods
        private void EnsureNotDisposed()
        {
            if (_disposed) // Check whether the Transactions object has been released
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
