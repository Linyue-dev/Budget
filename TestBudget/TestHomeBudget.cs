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
    /// <summary>
    /// Tests for the updated HomeBudget class with CreatedBy and TransactionDate support
    /// </summary>
    public class TestHomeBudgetNew : IDisposable
    {
        private HomeBudget homeBudget;
        private string testDatabasePath;
        private bool disposed = false;

        public TestHomeBudgetNew()
        {
            // Create a unique temporary database for each test
            testDatabasePath = Path.Combine(Path.GetTempPath(), $"test_homebudget_{Guid.NewGuid()}.db");

            // Create new database with test data
            homeBudget = new HomeBudget(testDatabasePath, isNewDB: true);

            // Add some test data
            CreateTestData();
        }

        private void CreateTestData()
        {
            // Get default categories
            var categories = homeBudget.categories.GetAllCategories();
            var incomeCategory = categories.FirstOrDefault(c => c.Type == CategoryType.Income);
            var expenseCategory = categories.FirstOrDefault(c => c.Type == CategoryType.Expense);

            // If no income category, create one
            if (incomeCategory == null)
            {
                int categoryId = homeBudget.categories.AddCategory("Salary", CategoryType.Income);
                incomeCategory = new Category(categoryId, "Salary", CategoryType.Income);
            }

            // If no expense category, create one
            if (expenseCategory == null)
            {
                int categoryId = homeBudget.categories.AddCategory("Food", CategoryType.Expense);
                expenseCategory = new Category(categoryId, "Food", CategoryType.Expense);
            }

            // Add test transactions with different creators
            homeBudget.transactions.AddTransaction(
                new DateTime(2024, 1, 15),
                incomeCategory.Id,
                5000m,
                "January Salary",
                "selina"
            );

            homeBudget.transactions.AddTransaction(
                new DateTime(2024, 1, 20),
                expenseCategory.Id,
                150m,
                "Grocery Shopping",
                "Sophie"
            );

            homeBudget.transactions.AddTransaction(
                new DateTime(2024, 2, 1),
                incomeCategory.Id,
                5000m,
                "February Salary",
                "Woody"
            );

            homeBudget.transactions.AddTransaction(
                new DateTime(2024, 2, 10),
                expenseCategory.Id,
                80m,
                "Restaurant",
                "Charles"
            );

            homeBudget.transactions.AddTransaction(
                new DateTime(2025, 1, 5),
                expenseCategory.Id,
                200m,
                "New Year Shopping",
                "Sophie"
            );
        }

        public void Dispose()
        {
            if (!disposed)
            {
                homeBudget?.Dispose();

                if (File.Exists(testDatabasePath))
                {
                    try
                    {
                        File.Delete(testDatabasePath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not delete test database {testDatabasePath}: {ex.Message}");
                    }
                }

                disposed = true;
            }
        }

        #region Basic Functionality Tests

        [Fact]
        public void HomeBudget_Constructor_ShouldCreateValidInstance()
        {
            // Assert
            Assert.NotNull(homeBudget);
            Assert.NotNull(homeBudget.categories);
            Assert.NotNull(homeBudget.transactions);
            Assert.NotNull(homeBudget.DatabaseService);
        }

        [Fact]
        public void HomeBudget_GetBudgetItems_ShouldReturnAllTransactions()
        {
            // Act
            List<BudgetItem> budgetItems = homeBudget.GetBudgetItems(null, null, false, 9);

            // Assert
            Assert.NotNull(budgetItems);
            Assert.True(budgetItems.Count > 0, "Should have budget items");

            // Verify each item has all required fields
            foreach (var item in budgetItems)
            {
                Assert.True(item.TransactionID > 0, "TransactionID should be positive");
                Assert.True(item.CategoryID > 0, "CategoryID should be positive");
                Assert.NotNull(item.Category);
                Assert.NotNull(item.ShortDescription);
                Assert.NotNull(item.CreatedBy);
                Assert.NotEqual(default(DateTime), item.Date);
                Assert.NotEqual(default(DateTime), item.CreatedAt);
            }

            Console.WriteLine($"Found {budgetItems.Count} budget items");
        }

        #endregion

        #region CreatedBy Functionality Tests

        [Fact]
        public void HomeBudget_GetBudgetItemsByCreatedBy_ShouldFilterCorrectly()
        {
            // Act
            var CharlesnItems = homeBudget.GetBudgetItemsByCreatedBy("Charles");
            var SohpieItems = homeBudget.GetBudgetItemsByCreatedBy("Sophie");

            // Assert
            Assert.True(CharlesnItems.Count > 0, "Should have transactions created by Charles");
            Assert.True(SohpieItems.Count > 0, "Should have transactions created by Sohpie");

            // Verify filtering
            foreach (var item in CharlesnItems)
            {
                Assert.Equal("Charles", item.CreatedBy);
            }

            foreach (var item in SohpieItems)
            {
                Assert.Equal("Sophie", item.CreatedBy);
            }

            Console.WriteLine($"Charles created {CharlesnItems.Count} transactions");
            Console.WriteLine($"Sophie created {SohpieItems.Count} transactions");
        }


        [Fact]
        public void HomeBudget_GetCreatedByStatistics_ShouldReturnValidStats()
        {
            // Act
            var statistics = homeBudget.GetCreatedByStatistics();

            // Assert
            Assert.NotNull(statistics);
            Assert.True(statistics.Count > 0, "Should have statistics");

            foreach (var stat in statistics)
            {
                Assert.True(stat.ContainsKey("CreatedBy"));
                Assert.True(stat.ContainsKey("TransactionCount"));
                Assert.True(stat.ContainsKey("NetAmount"));
                Assert.True(stat.ContainsKey("IncomeAmount"));
                Assert.True(stat.ContainsKey("ExpenseAmount"));

                Assert.NotNull(stat["CreatedBy"]);
                Assert.True((int)stat["TransactionCount"] > 0);
            }

            // Print statistics for verification
            Console.WriteLine("Created By Statistics:");
            foreach (var stat in statistics)
            {
                Console.WriteLine($"  {stat["CreatedBy"]}: {stat["TransactionCount"]} transactions, " +
                                $"Net: {stat["NetAmount"]:C}");
            }
        }

        #endregion

        #region Date Filtering Tests

        [Fact]
        public void HomeBudget_GetBudgetItems_WithDateRange_ShouldFilterCorrectly()
        {
            // Arrange
            DateTime startDate = new DateTime(2024, 1, 1);
            DateTime endDate = new DateTime(2024, 12, 31);

            // Act
            List<BudgetItem> budgetItems = homeBudget.GetBudgetItems(startDate, endDate, false, 9);

            // Assert
            Assert.True(budgetItems.Count > 0, "Should have transactions in 2024");

            foreach (BudgetItem item in budgetItems)
            {
                Assert.True(item.Date >= startDate, $"Date {item.Date} should be >= {startDate}");
                Assert.True(item.Date <= endDate, $"Date {item.Date} should be <= {endDate}");
            }

            Console.WriteLine($"Found {budgetItems.Count} transactions in 2024");
        }

        [Fact]
        public void HomeBudget_GetBudgetItemsByCreatedBy_WithDateRange_ShouldWork()
        {
            // Arrange
            DateTime startDate = new DateTime(2024, 1, 1);
            DateTime endDate = new DateTime(2024, 12, 31);

            // Act
            var zhangSanItems = homeBudget.GetBudgetItemsByCreatedBy("Charles", startDate, endDate);

            // Assert
            foreach (var item in zhangSanItems)
            {
                Assert.Equal("Charles", item.CreatedBy);
                Assert.True(item.Date >= startDate);
                Assert.True(item.Date <= endDate);
            }

            Console.WriteLine($"Charles had {zhangSanItems.Count} transactions in 2024");
        }

        #endregion

        #region Category Filtering Tests

        [Fact]
        public void HomeBudget_GetBudgetItems_WithCategoryFilter_ShouldWork()
        {
            // Arrange
            List<Category> categories = homeBudget.categories.GetAllCategories();
            var incomeCategory = categories.FirstOrDefault(c => c.Type == CategoryType.Income);
            Assert.NotNull(incomeCategory);

            // Act
            List<BudgetItem> budgetItems = homeBudget.GetBudgetItems(null, null, true, incomeCategory.Id);

            // Assert
            Assert.True(budgetItems.Count > 0, "Should have income transactions");

            foreach (BudgetItem item in budgetItems)
            {
                Assert.Equal(incomeCategory.Id, item.CategoryID);
                Assert.True(item.Amount > 0, "Income should be displayed as positive");
            }

            Console.WriteLine($"Found {budgetItems.Count} income transactions");
        }

        #endregion

        #region Amount Display Logic Tests

        [Fact]
        public void HomeBudget_GetBudgetItems_AmountDisplayLogic_ShouldBeCorrect()
        {
            // Arrange
            List<Transaction> allTransactions = homeBudget.transactions.GetAllTransactions();
            List<Category> allCategories = homeBudget.categories.GetAllCategories();
            List<BudgetItem> budgetItems = homeBudget.GetBudgetItems(null, null, false, 9);

            // Act & Assert
            foreach (Transaction transaction in allTransactions)
            {
                BudgetItem budgetItem = budgetItems.Find(b => b.TransactionID == transaction.Id);
                Assert.NotNull(budgetItem);

                Category category = allCategories.Find(c => c.Id == transaction.CategoryId);
                Assert.NotNull(category);

                // Verify amount display logic
                if (category.Type == CategoryType.Income)
                {
                    Assert.True(budgetItem.Amount > 0, $"Income should be positive, got {budgetItem.Amount}");
                    Assert.Equal(transaction.Amount, budgetItem.Amount);
                }
                else
                {
                    Assert.True(budgetItem.Amount < 0, $"Expense should be negative, got {budgetItem.Amount}");
                    Assert.Equal(-transaction.Amount, budgetItem.Amount);
                }
            }
        }

        #endregion

        #region Balance Calculation Tests

        [Fact]

        public void HomeBudget_GetBudgetItems_BalanceCalculation_ShouldBeCorrect()
        {
            // Act
            List<BudgetItem> budgetItems = homeBudget.GetBudgetItems(null, null, false, 9);

            // Assert
            Assert.True(budgetItems.Count > 0, "Should have budget items");

            // The items are already sorted by the GetBudgetItems method
            decimal expectedBalance = 0;
            foreach (BudgetItem item in budgetItems)
            {
                expectedBalance += item.Amount;

                Assert.True(expectedBalance == item.Balance,
                    $"Balance mismatch for transaction {item.TransactionID}. Expected: {expectedBalance}, Actual: {item.Balance}");
            }

            Console.WriteLine($"Final balance: {expectedBalance:C}");
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void HomeBudget_AllMethods_ShouldReturnConsistentData()
        {
            // Act
            List<BudgetItem> allItems = homeBudget.GetBudgetItems(null, null, false, 9);
            List<BudgetItemsByMonth> monthlyItems = homeBudget.GetBudgetItemsByMonth(null, null, false, 9);
            List<BudgetItemsByCategory> categoryItems = homeBudget.GetBudgetItemsByCategory(null, null, false, 9);

            // Assert - Data consistency
            int monthlyDetailCount = monthlyItems.Sum(m => m.Details.Count);
            int categoryDetailCount = categoryItems.Sum(c => c.Details.Count);

            Assert.Equal(allItems.Count, monthlyDetailCount);
            Assert.Equal(allItems.Count, categoryDetailCount);

            // Amount consistency
            decimal totalFromItems = allItems.Sum(i => i.Amount);
            decimal totalFromMonthly = monthlyItems.Sum(m => m.Total);
            decimal totalFromCategory = categoryItems.Sum(c => c.Total);

            Assert.Equal(totalFromItems, totalFromMonthly);
            Assert.Equal(totalFromItems, totalFromCategory);

            Console.WriteLine($"Total transactions: {allItems.Count}");
            Console.WriteLine($"Total amount: {totalFromItems:C}");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void HomeBudget_EmptyDateRange_ShouldReturnEmptyList()
        {
            // Arrange
            DateTime futureStart = new DateTime(2030, 1, 1);
            DateTime futureEnd = new DateTime(2030, 12, 31);

            // Act
            var items = homeBudget.GetBudgetItems(futureStart, futureEnd, false, 9);

            // Assert
            Assert.Empty(items);
        }

        [Fact]
        public void HomeBudget_NonExistentCreatedBy_ShouldReturnEmptyList()
        {
            // Act
            var items = homeBudget.GetBudgetItemsByCreatedBy("not exist!");

            // Assert
            Assert.Empty(items);
        }

        #endregion
    }
}