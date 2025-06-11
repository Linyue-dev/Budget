using Budget;
using Budget.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using static Budget.Models.Category;

namespace TestBudget
{
    public class TestConstants
    {
        public static string testDBInputFile = "testBudget.db";

        public static string GetSolutionDir()
        {
            string currentDir = Directory.GetCurrentDirectory();
            DirectoryInfo directory = new DirectoryInfo(currentDir);
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }
            return directory?.FullName ?? currentDir;
        }

        // New: Ensure test database exists
        public static string EnsureTestDatabaseExists()
        {
            string folder = GetSolutionDir();
            string testDbPath = Path.Combine(folder, testDBInputFile);

            if (!File.Exists(testDbPath))
            {
                CreateTestDatabase(testDbPath);
            }

            return testDbPath;
        }

        // Create test database and populate with data
        private static void CreateTestDatabase(string dbPath)
        {
            Console.WriteLine($"Creating test database at: {dbPath}");

            using (var homeBudget = new HomeBudget(dbPath, isNewDB: true))
            {
                PopulateTestData(homeBudget);
            }

            Console.WriteLine("Test database created successfully!");
        }

        // Populate test data
        private static void PopulateTestData(HomeBudget homeBudget)
        {
            var categories = homeBudget.categories.GetAllCategories();

            // Get different types of categories
            var salaryCategory = categories.FirstOrDefault(c => c.Type == CategoryType.Income);
            var utilitiesCategory = categories.FirstOrDefault(c => c.Name.Contains("Utilities"));
            var foodCategory = categories.FirstOrDefault(c => c.Name.Contains("Food"));
            var savingsCategory = categories.FirstOrDefault(c => c.Type == CategoryType.Savings);
            var mortgageCategory = categories.FirstOrDefault(c => c.Name.Contains("mortgage"));

            if (salaryCategory == null)
            {
                // If no salary category found, create one
                int newCategoryId = homeBudget.categories.AddCategory("Salary", CategoryType.Income);
                salaryCategory = new Category(newCategoryId, "Salary", CategoryType.Income);
            }

            // Add 2024 data
            AddYearData(homeBudget, 2024, salaryCategory, utilitiesCategory, foodCategory, savingsCategory, mortgageCategory);

            // Add 2025 data  
            AddYearData(homeBudget, 2025, salaryCategory, utilitiesCategory, foodCategory, savingsCategory, mortgageCategory);
        }

        private static void AddYearData(HomeBudget homeBudget, int year,
            Category salary, Category utilities, Category food, Category savings, Category mortgage)
        {
            var random = new Random(42); // Fixed seed for consistent data

            for (int month = 1; month <= 12; month++)
            {
                // Monthly salary
                if (salary != null)
                {
                    homeBudget.transactions.AddTransaction(
                        new DateTime(year, month, 1),
                        salary.Id,
                        5000m,
                        $"{year}-{month:D2} Monthly Salary"
                    );
                }

                // Utilities
                if (utilities != null)
                {
                    decimal amount = 150m + (decimal)(random.NextDouble() * 100);
                    homeBudget.transactions.AddTransaction(
                        new DateTime(year, month, 5),
                        utilities.Id,
                        amount,
                        $"{year}-{month:D2} Utilities Bill"
                    );
                }

                // Food expenses (2-4 times per month)
                if (food != null)
                {
                    int foodTransactions = random.Next(2, 5);
                    for (int i = 0; i < foodTransactions; i++)
                    {
                        decimal amount = 50m + (decimal)(random.NextDouble() * 200);
                        int day = random.Next(1, 28);
                        homeBudget.transactions.AddTransaction(
                            new DateTime(year, month, day),
                            food.Id,
                            amount,
                            $"{year}-{month:D2} Food Expense #{i + 1}"
                        );
                    }
                }

                // Savings (quarterly)
                if (savings != null && month % 3 == 0)
                {
                    homeBudget.transactions.AddTransaction(
                        new DateTime(year, month, 28),
                        savings.Id,
                        1000m,
                        $"{year}-Q{(month - 1) / 3 + 1} Quarterly Savings"
                    );
                }

                // Mortgage (monthly)
                if (mortgage != null)
                {
                    homeBudget.transactions.AddTransaction(
                        new DateTime(year, month, 15),
                        mortgage.Id,
                        1200m,
                        $"{year}-{month:D2} Mortgage Payment"
                    );
                }
            }
        }

        // Get temporary test database path
        public static string GetTempDbPath(string suffix = "")
        {
            string tempDir = Path.GetTempPath();
            string fileName = $"temp_test_budget_{suffix}_{Guid.NewGuid()}.db";
            return Path.Combine(tempDir, fileName);
        }
    }

    // Test base class - provides common setup and cleanup
    public abstract class TestBase : IDisposable
    {
        protected HomeBudget homeBudget;
        protected string messyDB;
        private bool disposed = false;

        protected void SetupTest(string testSuffix = "")
        {
            // Ensure test database exists
            string goodDB = TestConstants.EnsureTestDatabaseExists();

            // Create temporary database
            messyDB = TestConstants.GetTempDbPath(testSuffix);
            File.Copy(goodDB, messyDB, true);

            // Create HomeBudget instance
            homeBudget = new HomeBudget(messyDB, false);
        }

        public virtual void Dispose()
        {
            if (!disposed)
            {
                homeBudget?.Dispose();

                if (!string.IsNullOrEmpty(messyDB) && File.Exists(messyDB))
                {
                    try
                    {
                        File.Delete(messyDB);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Failed to delete temp file {messyDB}: {ex.Message}");
                    }
                }

                disposed = true;
            }
        }
    }

    // Updated test classes using base class
    public class TestHomeBudget : TestBase
    {
        [Fact]
        public void HomeBudgetMethod_GetBudgetItems_NoStartEnd_NoFilter()
        {
            // Arrange
            SetupTest("no_filter");

            List<Transaction> listTransactions = homeBudget.transactions.GetAllTransactions();
            List<Category> listCategories = homeBudget.categories.GetAllCategories();

            // Act
            List<BudgetItem> budgetItems = homeBudget.GetBudgetItems(null, null, false, 9);

            // Assert
            Assert.Equal(listTransactions.Count, budgetItems.Count);

            foreach (Transaction transaction in listTransactions)
            {
                BudgetItem budgetItem = budgetItems.Find(b => b.TransactionID == transaction.Id);
                Assert.NotNull(budgetItem); // Ensure corresponding BudgetItem was found

                Category category = listCategories.Find(c => c.Id == transaction.CategoryId);
                Assert.NotNull(category); // Ensure corresponding Category was found

                Assert.Equal(budgetItem.Category, category.Name);
                Assert.Equal(budgetItem.CategoryID, transaction.CategoryId);
                Assert.Equal(budgetItem.ShortDescription, transaction.Description);
                Assert.Equal(budgetItem.Date, transaction.Date);

                // Verify amount display logic: Income is positive, expenses are negative
                if (category.Type == CategoryType.Income)
                {
                    Assert.True(Math.Abs(budgetItem.Amount - transaction.Amount) < 0.000001m,
                               $"Income amount mismatch: expected {transaction.Amount}, actual {budgetItem.Amount}");
                }
                else
                {
                    Assert.True(Math.Abs(budgetItem.Amount - (-transaction.Amount)) < 0.000001m,
                               $"Expense amount mismatch: expected {-transaction.Amount}, actual {budgetItem.Amount}");
                }
            }
        }

        [Fact]
        public void HomeBudgetMethod_GetBudgetItems_WithDateFilter()
        {
            // Arrange
            SetupTest("date_filter");

            DateTime startDate = new DateTime(2025, 1, 1);
            DateTime endDate = new DateTime(2025, 3, 31);

            // Act
            List<BudgetItem> budgetItems = homeBudget.GetBudgetItems(startDate, endDate, false, 9);

            // Assert
            Assert.True(budgetItems.Count > 0, "Should have transactions in the specified date range");

            foreach (BudgetItem item in budgetItems)
            {
                Assert.True(item.Date >= startDate, $"Date {item.Date} should be >= {startDate}");
                Assert.True(item.Date <= endDate, $"Date {item.Date} should be <= {endDate}");
            }
        }

        [Fact]
        public void HomeBudgetMethod_GetBudgetItems_WithCategoryFilter()
        {
            // Arrange
            SetupTest("category_filter");

            List<Category> categories = homeBudget.categories.GetAllCategories();
            var incomeCategory = categories.FirstOrDefault(c => c.Type == CategoryType.Income);
            Assert.NotNull(incomeCategory); // Ensure we have an income category

            int targetCategoryId = incomeCategory.Id;

            // Act
            List<BudgetItem> budgetItems = homeBudget.GetBudgetItems(null, null, true, targetCategoryId);

            // Assert
            Assert.True(budgetItems.Count > 0);
            foreach (BudgetItem item in budgetItems)
            {
                Assert.Equal(targetCategoryId, item.CategoryID);
                Assert.True(item.Amount > 0); // Income should be displayed as positive
            }
        }

        [Fact]
        public void HomeBudgetMethod_GetBudgetItems_BalanceCalculation()
        {
            // Arrange
            SetupTest("balance_calc");

            // Act
            List<BudgetItem> budgetItems = homeBudget.GetBudgetItems(null, null, false, 9);

            // Assert - Verify balance calculation
            var sortedItems = budgetItems.OrderBy(b => b.Date).ThenBy(b => b.TransactionID).ToList();
            decimal expectedBalance = 0;

            foreach (BudgetItem item in sortedItems)
            {
                expectedBalance += item.Amount; // Amount has already been adjusted for positive/negative based on type
                Assert.Equal(expectedBalance, item.Balance);
            }
        }
    }

    public class TestHomeBudget_GetBudgetItemsByMonth : TestBase
    {
        [Fact]
        public void HomeBudgetMethod_GetBudgetItemsByMonth_NoFilter()
        {
            // Arrange
            SetupTest("month_no_filter");

            List<BudgetItem> allItems = homeBudget.GetBudgetItems(null, null, false, 9);

            // Act
            List<BudgetItemsByMonth> monthlyItems = homeBudget.GetBudgetItemsByMonth(null, null, false, 9);

            // Assert
            Assert.True(monthlyItems.Count > 0);

            // Verify totals are consistent
            int totalDetailCount = monthlyItems.Sum(m => m.Details.Count);
            Assert.Equal(allItems.Count, totalDetailCount);

            // Verify monthly summary calculations
            foreach (var monthGroup in monthlyItems)
            {
                decimal expectedTotal = monthGroup.Details.Sum(d => d.Amount);
                Assert.Equal(expectedTotal, monthGroup.Total);

                // Verify month format
                Assert.Matches(@"^\d{4}/\d{2}$", monthGroup.Month);
            }
        }

        [Fact]
        public void HomeBudgetMethod_GetBudgetItemsByMonth_WithDateRange()
        {
            // Arrange
            SetupTest("month_date_range");

            DateTime startDate = new DateTime(2025, 2, 1);
            DateTime endDate = new DateTime(2025, 4, 30);

            // Act
            List<BudgetItemsByMonth> monthlyItems = homeBudget.GetBudgetItemsByMonth(startDate, endDate, false, 9);

            // Assert
            foreach (var monthGroup in monthlyItems)
            {
                // Parse month string
                var parts = monthGroup.Month.Split('/');
                int year = int.Parse(parts[0]);
                int month = int.Parse(parts[1]);

                // Verify month is within specified range
                Assert.True(year == 2025);
                Assert.True(month >= 2 && month <= 4);

                // Verify all detail records are in correct month
                foreach (var detail in monthGroup.Details)
                {
                    Assert.Equal(year, detail.Date.Year);
                    Assert.Equal(month, detail.Date.Month);
                }
            }
        }
    }

    public class TestHomeBudget_GetBudgetItemsByCategory : TestBase
    {
        [Fact]
        public void HomeBudgetMethod_GetBudgetItemsByCategory_NoFilter()
        {
            // Arrange
            SetupTest("category_no_filter");

            List<BudgetItem> allItems = homeBudget.GetBudgetItems(null, null, false, 9);
            List<Category> allCategories = homeBudget.categories.GetAllCategories();

            // Act
            List<BudgetItemsByCategory> categoryItems = homeBudget.GetBudgetItemsByCategory(null, null, false, 9);

            // Assert
            // Verify totals are consistent
            int totalDetailCount = categoryItems.Sum(c => c.Details.Count);
            Assert.Equal(allItems.Count, totalDetailCount);

            // Verify summary calculations for each category
            foreach (var categoryGroup in categoryItems)
            {
                decimal expectedTotal = categoryGroup.Details.Sum(d => d.Amount);
                Assert.Equal(expectedTotal, categoryGroup.Total);

                // Verify all detail records belong to correct category
                foreach (var detail in categoryGroup.Details)
                {
                    Assert.Equal(categoryGroup.Category, detail.Category);
                }

                // Verify category exists in system
                Assert.Contains(allCategories, c => c.Name == categoryGroup.Category);
            }
        }

        [Fact]
        public void HomeBudgetMethod_GetBudgetItemsByCategory_IncomeVsExpense()
        {
            // Arrange
            SetupTest("income_vs_expense");

            List<Category> allCategories = homeBudget.categories.GetAllCategories();

            // Act
            List<BudgetItemsByCategory> categoryItems = homeBudget.GetBudgetItemsByCategory(null, null, false, 9);

            // Assert
            foreach (var categoryGroup in categoryItems)
            {
                var category = allCategories.Find(c => c.Name == categoryGroup.Category);
                Assert.NotNull(category);

                if (category.Type == CategoryType.Income)
                {
                    // Income category totals should be positive (or zero)
                    Assert.True(categoryGroup.Total >= 0, $"Income category {categoryGroup.Category} should have positive total");

                    // All items in income category should be displayed as positive
                    foreach (var detail in categoryGroup.Details)
                    {
                        Assert.True(detail.Amount >= 0, $"Income item should be positive: {detail.Amount}");
                    }
                }
                else
                {
                    // Expense category totals should be negative (or zero)
                    Assert.True(categoryGroup.Total <= 0, $"Expense category {categoryGroup.Category} should have negative total");

                    // All items in expense category should be displayed as negative
                    foreach (var detail in categoryGroup.Details)
                    {
                        Assert.True(detail.Amount <= 0, $"Expense item should be negative: {detail.Amount}");
                    }
                }
            }
        }
    }

    public class TestHomeBudget_Integration : TestBase
    {
        [Fact]
        public void HomeBudgetMethod_ConsistencyAcrossAllMethods()
        {
            // Arrange
            SetupTest("consistency");

            // Act
            List<BudgetItem> allItems = homeBudget.GetBudgetItems(null, null, false, 9);
            List<BudgetItemsByMonth> monthlyItems = homeBudget.GetBudgetItemsByMonth(null, null, false, 9);
            List<BudgetItemsByCategory> categoryItems = homeBudget.GetBudgetItemsByCategory(null, null, false, 9);

            // Assert - Verify data consistency

            // 1. Total record count should be consistent
            int monthlyDetailCount = monthlyItems.Sum(m => m.Details.Count);
            int categoryDetailCount = categoryItems.Sum(c => c.Details.Count);

            Assert.Equal(allItems.Count, monthlyDetailCount);
            Assert.Equal(allItems.Count, categoryDetailCount);

            // 2. Total amounts should be consistent
            decimal totalFromItems = allItems.Sum(i => i.Amount);
            decimal totalFromMonthly = monthlyItems.Sum(m => m.Total);
            decimal totalFromCategory = categoryItems.Sum(c => c.Total);

            Assert.Equal(totalFromItems, totalFromMonthly);
            Assert.Equal(totalFromItems, totalFromCategory);

            // 3. Each transaction record should exist in all methods
            foreach (var item in allItems)
            {
                // Find in monthly report
                bool foundInMonthly = monthlyItems
                    .SelectMany(m => m.Details)
                    .Any(d => d.TransactionID == item.TransactionID);
                Assert.True(foundInMonthly, $"Transaction {item.TransactionID} not found in monthly report");

                // Find in category report
                bool foundInCategory = categoryItems
                    .SelectMany(c => c.Details)
                    .Any(d => d.TransactionID == item.TransactionID);
                Assert.True(foundInCategory, $"Transaction {item.TransactionID} not found in category report");
            }
        }
    }

    // Additional utility test for database setup verification
    public class TestHomeBudget_Setup : TestBase
    {
        [Fact]
        public void TestDatabase_CreationAndBasicData()
        {
            // Arrange & Act
            SetupTest("setup_verification");

            // Assert
            Assert.NotNull(homeBudget);

            var categories = homeBudget.categories.GetAllCategories();
            var transactions = homeBudget.transactions.GetAllTransactions();

            Assert.True(categories.Count > 0, "Should have categories");
            Assert.True(transactions.Count > 0, "Should have transactions");

            // Verify we have both income and expense categories
            var incomeCategories = categories.Where(c => c.Type == CategoryType.Income).ToList();
            var expenseCategories = categories.Where(c => c.Type == CategoryType.Expense).ToList();

            Assert.True(incomeCategories.Count > 0, "Should have income categories");
            Assert.True(expenseCategories.Count > 0, "Should have expense categories");

            Console.WriteLine($"Test database contains:");
            Console.WriteLine($"- {categories.Count} categories");
            Console.WriteLine($"- {transactions.Count} transactions");
            Console.WriteLine($"- {incomeCategories.Count} income categories");
            Console.WriteLine($"- {expenseCategories.Count} expense categories");
        }
    }
}