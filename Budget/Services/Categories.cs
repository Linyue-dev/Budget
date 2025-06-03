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
            checkCommand.CommandText = "SELECT COUNT(*) FROM categories";
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

        private void Add(Category cat)
        {
            _Cats.Add(cat);
        }

        public void Add(string name, Category.CategoryType type)
        {
            int new_num = 1;
            if (_Cats.Count > 0)
            {
                new_num = (from c in _Cats select c.Id).Max();
                new_num++;
            }
            _Cats.Add(new Category(new_num, name, type));
        }


        public void Delete(int Id)
        {
            int i = _Cats.FindIndex(x => x.Id == Id);
            _Cats.RemoveAt(i);
        }

        public List<Category> List()
        {
            List<Category> newList = new List<Category>();
            foreach (Category category in _Cats)
            {
                newList.Add(new Category(category));
            }
            return newList;
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
