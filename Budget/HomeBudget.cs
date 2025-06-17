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
    /// <summary>
    /// Provides core functionality for managing personal budgeting, including categories,
    /// transactions, and various reporting features. Interfaces with an SQLite database.
    /// </summary>
    public class HomeBudget : IDisposable
    {
        #region Private Fields
        private Categories _categories;
        private Transactions _transactions;
        private DatabaseService _databaseService;
        private bool _disposed = false;
        #endregion

        #region Public Properties    
        /// <summary>
        /// Gets the Categories service which manages category-related data.
        /// </summary>
        public Categories categories { get { return _categories; } }


        /// <summary>
        /// Gets the Transactions service which manages transaction-related data.
        /// </summary>
        public Transactions transactions { get { return _transactions; } }

        /// <summary>
        /// Gets the DatabaseService used for database interactions.
        /// </summary>
        public DatabaseService DatabaseService { get { return _databaseService; } }
        #endregion

        #region Constructors


        /// <summary>
        /// Initializes a new instance of the <see cref="HomeBudget"/> class.
        /// </summary>
        /// <param name="DatabasePath">The path to the SQLite database file.</param>
        /// <param name="isNewDB">If true, creates a new database with default categories; otherwise loads an existing one.</param>
        /// <exception cref="ArgumentException">Thrown when the database path is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the existing database file is not found.</exception>
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

                // create default categories 
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

        /// <summary>
        /// Retrieves a list of budget items within the specified date range and optional category filter.
        /// </summary>
        /// <param name="Start">The start date of the range (inclusive).</param>
        /// <param name="End">The end date of the range (inclusive).</param>
        /// <param name="FilterFlag">Whether to filter by the specified CategoryID.</param>
        /// <param name="CategoryID">The category ID to filter by if FilterFlag is true.</param>
        /// <returns>A list of <see cref="BudgetItem"/> objects.</returns>
        public List<BudgetItem> GetBudgetItems(DateTime? Start, DateTime? End, bool FilterFlag, int CategoryID)
        {
            EnsureNotDisposed();
            DateTime realStart = Start ?? new DateTime(1900, 1, 1);
            DateTime realEnd = End ?? new DateTime(2500, 1, 1);
            List<BudgetItem> items = new List<BudgetItem>();
            decimal total = 0;

            using var command = DatabaseService.Connection.CreateCommand();
            command.CommandText = @$"
                        SELECT 
                            c.Id as CategoryId,
                            t.Id as TransactionId,
                            t.TransactionDate,
                            c.Name as Category,
                            t.Description,
                            t.Amount,
                            c.TypeId as CategoryType,
                            t.CreatedBy,
                            t.CreatedAt
                        FROM categories c
                        JOIN transactions t
                        ON c.Id = t.CategoryId
                        WHERE t.TransactionDate >= @Start AND t.TransactionDate <= @End {(FilterFlag ? "AND c.Id = @CategoryID" : "")}
                        ORDER BY t.TransactionDate, t.Id";

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
                DateTime transactionDate = reader.GetDateTime("TransactionDate");
                string category = reader.GetString("Category");
                string description = reader.GetString("Description");
                decimal amount = reader.GetDecimal("Amount");
                CategoryType categoryType = (CategoryType)reader.GetInt32("CategoryType");
                string createdBy = reader.GetString("CreatedBy");
                DateTime createdAt = reader.GetDateTime("CreatedAt");

                decimal displayAmount = categoryType == CategoryType.Income ? amount : -amount;

                // Calculate balance: Simply add up the displayed amounts.
                total += displayAmount;

                items.Add(new BudgetItem
                {
                    CategoryID = categoryId,
                    TransactionID = transactionId,
                    Date = transactionDate,
                    Category = category,
                    ShortDescription = description,
                    Amount = displayAmount,
                    Balance = total,
                    CreatedBy = createdBy,
                    CreatedAt = createdAt
                });
            }
            return items;
        }


        /// <summary>
        /// Groups budget items by month and provides summaries for each month.
        /// </summary>
        /// <param name="Start">The start date of the range (inclusive).</param>
        /// <param name="End">The end date of the range (inclusive).</param>
        /// <param name="FilterFlag">Whether to filter by the specified CategoryID.</param>
        /// <param name="CategoryID">The category ID to filter by if FilterFlag is true.</param>
        /// <returns>A list of <see cref="BudgetItemsByMonth"/> containing grouped and summarized budget data.</returns>
        public List<BudgetItemsByMonth> GetBudgetItemsByMonth(DateTime? Start, DateTime? End, bool FilterFlag, int CategoryID)
        {
            EnsureNotDisposed();
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
        /// <summary>
        /// Groups budget items by category and provides summaries for each category.
        /// </summary>
        /// <param name="Start">The start date of the range (inclusive).</param>
        /// <param name="End">The end date of the range (inclusive).</param>
        /// <param name="FilterFlag">Whether to filter by the specified CategoryID.</param>
        /// <param name="CategoryID">The category ID to filter by if FilterFlag is true.</param>
        /// <returns>A list of <see cref="BudgetItemsByCategory"/> containing grouped and summarized budget data.</returns>
        public List<BudgetItemsByCategory> GetBudgetItemsByCategory(DateTime? Start, DateTime? End, bool FilterFlag, int CategoryID)
        {
            EnsureNotDisposed();
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


        /// <summary>
        /// Provides a summary of budget items grouped by month and category.
        /// Each entry contains both category totals and detailed item lists.
        /// </summary>
        /// <param name="Start">The start date of the range (inclusive).</param>
        /// <param name="End">The end date of the range (inclusive).</param>
        /// <param name="FilterFlag">Whether to filter by the specified CategoryID.</param>
        /// <param name="CategoryID">The category ID to filter by if FilterFlag is true.</param>
        /// <returns>A list of dictionaries representing grouped budget data by category and month.</returns>
        public List<Dictionary<string, object>> GetBudgetDictionaryByCategoryAndMonth(DateTime? Start, DateTime? End, bool FilterFlag, int CategoryID)
        {
            EnsureNotDisposed();
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


        /// <summary>
        /// Retrieves budget items created by a specific user within the specified date range.
        /// </summary>
        /// <param name="createdBy">The username or identifier of the creator.</param>
        /// <param name="Start">The optional start date of the range.</param>
        /// <param name="End">The optional end date of the range.</param>
        /// <returns>A list of <see cref="BudgetItem"/> filtered by the creator.</returns>
        public List<BudgetItem> GetBudgetItemsByCreatedBy(string createdBy, DateTime? Start = null, DateTime? End = null)
        {
            EnsureNotDisposed();
            DateTime realStart = Start ?? new DateTime(1900, 1, 1);
            DateTime realEnd = End ?? new DateTime(2500, 1, 1);
            List<BudgetItem> items = new List<BudgetItem>();
            decimal total = 0;

            using var command = DatabaseService.Connection.CreateCommand();
            command.CommandText = @"
                        SELECT 
                            c.Id as CategoryId,
                            t.Id as TransactionId,
                            t.TransactionDate,
                            c.Name as Category,
                            t.Description,
                            t.Amount,
                            c.TypeId as CategoryType,
                            t.CreatedBy,
                            t.CreatedAt
                        FROM categories c
                        JOIN transactions t
                        ON c.Id = t.CategoryId
                        WHERE t.TransactionDate >= @Start 
                        AND t.TransactionDate <= @End 
                        AND t.CreatedBy = @CreatedBy
                        ORDER BY t.TransactionDate, t.Id";

            command.Parameters.AddWithValue("@Start", realStart);
            command.Parameters.AddWithValue("@End", realEnd);
            command.Parameters.AddWithValue("@CreatedBy", createdBy);

            using SQLiteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                int categoryId = reader.GetInt32("CategoryId");
                int transactionId = reader.GetInt32("TransactionId");
                DateTime transactionDate = reader.GetDateTime("TransactionDate");
                string category = reader.GetString("Category");
                string description = reader.GetString("Description");
                decimal amount = reader.GetDecimal("Amount");
                CategoryType categoryType = (CategoryType)reader.GetInt32("CategoryType");
                string recordCreatedBy = reader.GetString("CreatedBy");
                DateTime createdAt = reader.GetDateTime("CreatedAt");

                decimal displayAmount = categoryType == CategoryType.Income ? amount : -amount;
                total += displayAmount;

                items.Add(new BudgetItem
                {
                    CategoryID = categoryId,
                    TransactionID = transactionId,
                    Date = transactionDate,
                    Category = category,
                    ShortDescription = description,
                    Amount = displayAmount,
                    Balance = total,
                    CreatedBy = recordCreatedBy,  
                    CreatedAt = createdAt
                });
            }
            return items;
        }

        /// <summary>
        /// Generates statistical summaries grouped by the user who created the transactions.
        /// </summary>
        /// <param name="Start">Optional start date to limit results.</param>
        /// <param name="End">Optional end date to limit results.</param>
        /// <returns>A list of dictionaries containing statistics for each user, including counts and amounts.</returns>
        public List<Dictionary<string, object>> GetCreatedByStatistics(DateTime? Start = null, DateTime? End = null)
        {
            EnsureNotDisposed();
            DateTime realStart = Start ?? new DateTime(1900, 1, 1);
            DateTime realEnd = End ?? new DateTime(2500, 1, 1);
            var statistics = new List<Dictionary<string, object>>();

            using var command = DatabaseService.Connection.CreateCommand();
            command.CommandText = @"
                        SELECT 
                            t.CreatedBy,
                            COUNT(*) as TransactionCount,
                            SUM(CASE WHEN c.TypeId = 1 THEN t.Amount ELSE -t.Amount END) as NetAmount,
                            SUM(CASE WHEN c.TypeId = 1 THEN t.Amount ELSE 0 END) as IncomeAmount,
                            SUM(CASE WHEN c.TypeId != 1 THEN t.Amount ELSE 0 END) as ExpenseAmount
                        FROM transactions t
                        JOIN categories c ON t.CategoryId = c.Id
                        WHERE t.TransactionDate >= @Start AND t.TransactionDate <= @End
                        GROUP BY t.CreatedBy
                        ORDER BY t.CreatedBy";

            command.Parameters.AddWithValue("@Start", realStart);
            command.Parameters.AddWithValue("@End", realEnd);

            using SQLiteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                var stat = new Dictionary<string, object>
                {
                    ["CreatedBy"] = reader.GetString("CreatedBy"),
                    ["TransactionCount"] = reader.GetInt32("TransactionCount"),
                    ["NetAmount"] = reader.GetDecimal("NetAmount"),
                    ["IncomeAmount"] = reader.GetDecimal("IncomeAmount"),
                    ["ExpenseAmount"] = reader.GetDecimal("ExpenseAmount")
                };
                statistics.Add(stat);
            }

            return statistics;
        }
        #endregion GetList

        #region Helper Methods
        /// <summary>
        /// Releases all resources used by the HomeBudget class.
        /// </summary>
        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HomeBudget));
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Ensures that the object has not been disposed before performing operations.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the object is disposed.</exception>
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