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

            var query = from c in _categories.GetAllCategories()
                        join t in _transactions.GetAllTransactions() on c.Id equals t.CategoryId
                        where t.Date >= Start && t.Date <= End
                        select new { CatId = c.Id, TxnsId = t.Id, t.Date, CategoryType = c.Type, Category = c.Name, t.Description, t.Amount };


            List<BudgetItem> items = new List<BudgetItem>();
            decimal total = 0;

            foreach (var t in query.OrderBy(q => q.Date))
            {
                if (FilterFlag && CategoryID != t.CatId)
                {
                    continue;
                }

                // The impact on the balance depends on the classification type.
                if (t.CategoryType == CategoryType.Income)
                {
                    total = total + t.Amount;  // Increase in income balance
                }
                else
                {
                    total = total - t.Amount;  // Expenditure/Investment/Savings/Debt Reduction Balance
                }

                items.Add(new BudgetItem
                {
                    CategoryID = t.CatId,
                    TransactionID = t.TxnsId,
                    ShortDescription = t.Description,
                    Date = t.Date,
                    // Income is shown as a positive number, while other items are shown as a negative number.
                    Category = t.Category,
                    Amount = t.CategoryType == CategoryType.Income ? t.Amount : -t.Amount,
                    Balance = total
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
