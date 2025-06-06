using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Dynamic;
using Budget.Utils;
using Budget.Services;
using Budget.Models;
using static Budget.Models.Category;
using System.Data.SQLite;
using System.Data;
using System.Transactions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Budget
{

    public class HomeBudget : IDisposable
    {
        #region Private Fields
        private Categories _categories;
        private Transactions _transactions;
        private DatabaseService _databaseService;
        private bool _disposed = false;
        #endregion

        #region Public Properties    
        public Categories categories { get { return _categories; } }
        public Transactions transactions { get { return _transactions; } }
        public DatabaseService DatabaseService { get { return _databaseService; } }
        #endregion

        #region Constructors
        public HomeBudget(string DatabasePath, bool isNewDB = false)
        {
            if (string.IsNullOrWhiteSpace(DatabasePath))
                throw new ArgumentException("Database path cannot be null or empty.", nameof(DatabasePath));

            if (isNewDB)
            {
                // create new database
                _databaseService = DatabaseService.CreateNewDatabase(DatabasePath);
                _categories = new Categories(_databaseService);
                _transactions = new Transactions(_databaseService);

                // create default catecories 
                _categories.SetCategoriesToDefaults();
            }
            else
            {
                // use exist database 
                if (!File.Exists(DatabasePath))
                    throw new FileNotFoundException($"Database file not found: {DatabasePath}");

                _databaseService = DatabaseService.OpenExisting(DatabasePath);
                _categories = new Categories(_databaseService);
                _transactions = new Transactions(_databaseService);
            }

        }
        #endregion

        #region GetList
        public List<BudgetItem> GetBudgetItems(DateTime? Start, DateTime? End, bool FilterFlag, int CategoryID)
        {
            Start = Start ?? new DateTime(1900, 1, 1);
            End = End ?? new DateTime(2500, 1, 1);
            List<BudgetItem> items = new List<BudgetItem>();
            decimal total = 0;

            using var command = DatabaseService.Connection.CreateCommand();
            command.CommandText = @$"
                        SELECT 
                            c.Id as CategoryId,
                            e.Id as TransactionId,
                            e.Date,
                            c.Name as Category,
                            e.Description,
                            e.Amount,
                            c.TypeId as CategoryType -- Get the enumeration value from the TypeId field.
                        FROM categories c
                        JOIN transactions e
                        ON c.Id = e.CategoryId
                        WHERE e.Date >= @Start AND e.Date <= @End {(FilterFlag ? "AND c.Id = @CategoryID" : "")}";

            command.Parameters.AddWithValue("@Start", Start);
            command.Parameters.AddWithValue("@End", End);

            if (FilterFlag)
            {
                command.Parameters.AddWithValue("@CategoryID", CategoryID);
            }

            using SQLiteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                int categoryId = reader.GetInt32("CategoryId");
                int transactionId = reader.GetInt32("TransactionId");
                DateTime dateTime = reader.GetDateTime("Date");
                string category = reader.GetString("Category");
                string description = reader.GetString("Description");
                decimal amount = reader.GetDecimal("Amount");
                CategoryType categoryType = (CategoryType)reader.GetInt32("CategoryType");

                if (categoryType == CategoryType.Income)
                {
                    total += amount;
                }
                else
                {
                    total -= amount;
                }

                items.Add(new BudgetItem
                {
                    CategoryID = categoryId,
                    TransactionID = transactionId,
                    Date = dateTime,
                    Category = category,
                    ShortDescription = description,
                    Amount = amount,
                    Balance = total,
                });
            }
            return items;
        }

        public List<BudgetItemsByMonth> GetBudgetItemsByMonth(DateTime? Start, DateTime? End, bool FilterFlag, int CategoryID)
        {
            List<BudgetItem> items = GetBudgetItems(Start, End, FilterFlag, CategoryID);

            var GroupedByMonth = items.GroupBy(c => c.Date.Year.ToString("D4") + "/" + c.Date.Month.ToString("D2"));

            var summary = new List<BudgetItemsByMonth>();
            foreach (var MonthGroup in GroupedByMonth)
            {
                // calculate total for this month, and create list of details
                decimal total = 0;
                var details = new List<BudgetItem>();
                foreach (var item in MonthGroup)
                {
                    total = total + item.Amount;
                    details.Add(item);
                }

                // Add new BudgetItemsByMonth to our list
                summary.Add(new BudgetItemsByMonth
                {
                    Month = MonthGroup.Key,
                    Details = details,
                    Total = total
                });
            }

            return summary;
        }

        public List<BudgetItemsByCategory> GetBudgetItemsByCategory(DateTime? Start, DateTime? End, bool FilterFlag, int CategoryID)
        {

            List<BudgetItem> items = GetBudgetItems(Start, End, FilterFlag, CategoryID);

            var GroupedByCategory = items.GroupBy(c => c.Category);

            var summary = new List<BudgetItemsByCategory>();
            foreach (var CategoryGroup in GroupedByCategory.OrderBy(g => g.Key))
            {
                // calculate total for this category, and create list of details
                decimal total = 0;
                var details = new List<BudgetItem>();
                foreach (var item in CategoryGroup)
                {
                    total = total + item.Amount;
                    details.Add(item);
                }

                // Add new BudgetItemsByCategory to our list
                summary.Add(new BudgetItemsByCategory
                {
                    Category = CategoryGroup.Key,
                    Details = details,
                    Total = total
                });
            }

            return summary;
        }

        public List<Dictionary<string, object>> GetBudgetDictionaryByCategoryAndMonth(DateTime? Start, DateTime? End, bool FilterFlag, int CategoryID)
        {
            List<BudgetItemsByMonth> GroupedByMonth = GetBudgetItemsByMonth(Start, End, FilterFlag, CategoryID);

            var summary = new List<Dictionary<string, object>>();
            var totalsPerCategory = new Dictionary<String, decimal>();

            foreach (var MonthGroup in GroupedByMonth)
            {
                // create record object for this month
                Dictionary<string, object> record = new Dictionary<string, object>();
                record["Month"] = MonthGroup.Month;
                record["Total"] = MonthGroup.Total;

                // break up the month details into categories
                var GroupedByCategory = MonthGroup.Details.GroupBy(c => c.Category);

                foreach (var CategoryGroup in GroupedByCategory.OrderBy(g => g.Key))
                {
                    // calculate totals for the cat/month, and create list of details
                    decimal total = 0;
                    var details = new List<BudgetItem>();

                    foreach (var item in CategoryGroup)
                    {
                        total = total + item.Amount;
                        details.Add(item);
                    }

                    // add new properties and values to our record object
                    record["details:" + CategoryGroup.Key] = details;
                    record[CategoryGroup.Key] = total;

                    // keep track of totals for each category
                    if (totalsPerCategory.TryGetValue(CategoryGroup.Key, out decimal CurrentCatTotal))
                    {
                        totalsPerCategory[CategoryGroup.Key] = CurrentCatTotal + total;
                    }
                    else
                    {
                        totalsPerCategory[CategoryGroup.Key] = total;
                    }
                }
                // add record to collection
                summary.Add(record);
            }

            Dictionary<string, object> totalsRecord = new Dictionary<string, object>();
            totalsRecord["Month"] = "TOTALS";

            foreach (var cat in categories.GetAllCategories())
            {
                try
                {
                    totalsRecord.Add(cat.Name, totalsPerCategory[cat.Name]);
                }
                catch { }
            }
            summary.Add(totalsRecord);

            return summary;
        }
        #endregion GetList

        #region Helper Methods

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HomeBudget));
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            if (!_disposed)
            {
                _categories?.Dispose();
                _transactions?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }
}
