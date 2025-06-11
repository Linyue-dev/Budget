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
            DateTime realStart = Start ?? new DateTime(1900, 1, 1);
            DateTime realEnd = End ?? new DateTime(2500, 1, 1);
            List<BudgetItem> items = new List<BudgetItem>();
            decimal total = 0;

            using var command = DatabaseService.Connection.CreateCommand();
            command.CommandText = @$"
                        SELECT 
                            c.Id as CategoryId,
                            t.Id as TransactionId,
                            t.Date,
                            c.Name as Category,
                            t.Description,
                            t.Amount,
                            c.TypeId as CategoryType -- Get the enumeration value from the TypeId field.
                        FROM categories c
                        JOIN transactions t
                        ON c.Id = t.CategoryId
                        WHERE t.Date >= @Start AND t.Date <= @End {(FilterFlag ? "AND c.Id = @CategoryID" : "")}
                        ORDER BY t.Date, t.Id";

            command.Parameters.AddWithValue("@Start", realStart);
            command.Parameters.AddWithValue("@End", realEnd);

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

                decimal displayAmount = categoryType == CategoryType.Income ? amount : -amount;

                // Calculate balance: Simply add up the displayed amounts.
                total += displayAmount;

                items.Add(new BudgetItem
                {
                    CategoryID = categoryId,
                    TransactionID = transactionId,
                    Date = dateTime,
                    Category = category,
                    ShortDescription = description,
                    Amount = displayAmount,  // Display the adjusted amount 
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
                decimal total = 0;
                var details = new List<BudgetItem>();
                foreach (var item in MonthGroup)
                {
                    total += item.Amount;
                    details.Add(item);
                }

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
            var totalsPerCategory = new Dictionary<string, decimal>();

            foreach (var MonthGroup in GroupedByMonth)
            {
                // create record object for this month
                var record = new Dictionary<string, object>
                {
                    ["Month"] = MonthGroup.Month,
                    ["Total"] = MonthGroup.Total
                };

                // break up the month details into categories
                var GroupedByCategory = MonthGroup.Details
                    .GroupBy(c => c.Category)
                    .OrderBy(g => g.Key);

                foreach (var CategoryGroup in GroupedByCategory)
                {
                    var details = CategoryGroup.ToList();
                    var total = CategoryGroup.Sum(item => item.Amount);

                    // add new properties and values to our record object
                    record["details:" + CategoryGroup.Key] = details;
                    record[CategoryGroup.Key] = total;

                    // keep track of totals for each category
                    totalsPerCategory[CategoryGroup.Key] = totalsPerCategory.GetValueOrDefault(CategoryGroup.Key, 0) + total;
                }

                summary.Add(record);
            }

            // Create totals record
            var totalsRecord = new Dictionary<string, object> { ["Month"] = "TOTALS" };

            foreach (var categoryTotal in totalsPerCategory.OrderBy(kvp => kvp.Key))
            {
                totalsRecord[categoryTotal.Key] = categoryTotal.Value;
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
