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

        public void Add(DateTime date, int category, decimal amount, string description)
        {
            int new_id = 1;

            // if we already have expenses, set ID to max
            if (_Transactions.Count > 0)
            {
                new_id = (from e in _Transactions select e.Id).Max();
                new_id++;
            }

            _Transactions.Add(new Transaction(new_id, date, category, amount, description));

        }

        public void Delete(int Id)
        {
            int i = _Transactions.FindIndex(x => x.Id == Id);
            _Transactions.RemoveAt(i);

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
