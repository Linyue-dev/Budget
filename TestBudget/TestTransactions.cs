using Budget.Services;
using System;
using System.IO;
using System.Linq;
using Xunit;
using static Budget.Models.Category;

namespace TestBudget
{
    /// <summary>
    /// Unit tests for the Transactions service class.
    /// </summary>
    public class TestTransactions : IDisposable
    {
        private readonly string _testDatabasePath;
        private Transactions _transactions;
        private Categories _categories;

        public TestTransactions()
        {
            _testDatabasePath = Path.Combine(Path.GetTempPath(), $"test_transactions_{Guid.NewGuid()}.db");
        }

        public void Dispose()
        {
            _transactions?.Dispose();
            _categories?.Dispose();
            if (File.Exists(_testDatabasePath))
            {
                File.Delete(_testDatabasePath);
            }
        }

        private void SetupTestData()
        {
            _categories = new Categories(_testDatabasePath, isNew: true);
            _transactions = new Transactions(_testDatabasePath, isNew: false);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidPath_ShouldInitializeCorrectly()
        {
            // Act
            _transactions = new Transactions(_testDatabasePath, isNew: true);

            // Assert 
            Assert.True(_transactions.IsConnected);
            Assert.NotNull(_transactions.DatabasePath);
            Assert.NotEmpty(_transactions.DatabasePath);

            var transactions = _transactions.GetAllTransactions();
            Assert.NotNull(transactions);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithInvalidPath_ShouldThrowArgumentException(string invalidPath)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Transactions(invalidPath));
        }

        #endregion

        #region AddTransaction Tests

        [Fact]
        public void AddTransaction_ValidData_ShouldReturnNewId()
        {
            // Arrange
            SetupTestData();
            var categoryId = _categories.AddCategory("Food", CategoryType.Expense);
            var date = new DateTime(2024, 1, 1, 14, 30, 0);

            // Act
            int transactionId = _transactions.AddTransaction(date, categoryId, 25.50m, "Lunch at cafe", "Dad");

            // Assert
            Assert.True(transactionId > 0);
            var transaction = _transactions.GetTransactionFromId(transactionId);
            Assert.NotNull(transaction);
            Assert.Equal("Lunch at cafe", transaction.Description);
            Assert.Equal(25.50m, transaction.Amount);
            Assert.Equal(categoryId, transaction.CategoryId);
            Assert.Equal(date, transaction.TransactionDate);
            Assert.Equal("Dad", transaction.CreatedBy);
        }

        [Fact]
        public void AddTransaction_ZeroAmount_ShouldThrowArgumentException()
        {
            // Arrange
            SetupTestData();
            var categoryId = _categories.AddCategory("Food", CategoryType.Expense);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _transactions.AddTransaction(DateTime.Now, categoryId, 0m, "Test", "Dad"));
            Assert.Contains("Amount cannot be zero", exception.Message);
        }

        [Fact]
        public void AddTransaction_EmptyDescription_ShouldThrowArgumentException()
        {
            // Arrange
            SetupTestData();
            var categoryId = _categories.AddCategory("Food", CategoryType.Expense);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _transactions.AddTransaction(DateTime.Now, categoryId, 50m, "", "Dad"));
            Assert.Contains("Description cannot be null or empty", exception.Message);
        }

        [Fact]
        public void AddTransaction_EmptyCreatedBy_ShouldThrowArgumentException()
        {
            // Arrange
            SetupTestData();
            var categoryId = _categories.AddCategory("Food", CategoryType.Expense);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _transactions.AddTransaction(DateTime.Now, categoryId, 50m, "Test", ""));
            Assert.Contains("CreatedBy cannot be null or empty", exception.Message);
        }

        [Fact]
        public void AddTransaction_VariousAmounts_ShouldWork()
        {
            // Arrange
            SetupTestData();
            var expenseCategory = _categories.AddCategory("Expense", CategoryType.Expense);
            var incomeCategory = _categories.AddCategory("Income", CategoryType.Income);

            // Act & Assert - Test different amount types
            var expenseId = _transactions.AddTransaction(DateTime.Now, expenseCategory, 123.45m, "Expense", "Mom");
            var incomeId = _transactions.AddTransaction(DateTime.Now, incomeCategory, 1500.00m, "Salary", "Dad");
            var smallId = _transactions.AddTransaction(DateTime.Now, expenseCategory, 0.01m, "Small expense", "Son");

            var expense = _transactions.GetTransactionFromId(expenseId);
            var income = _transactions.GetTransactionFromId(incomeId);
            var small = _transactions.GetTransactionFromId(smallId);

            Assert.Equal(123.45m, expense.Amount);
            Assert.Equal(1500.00m, income.Amount);
            Assert.Equal(0.01m, small.Amount);
            Assert.Equal("Mom", expense.CreatedBy);
            Assert.Equal("Dad", income.CreatedBy);
            Assert.Equal("Son", small.CreatedBy);
        }

        [Fact]
        public void AddTransaction_WithSpecificCreatedAt_ShouldWork()
        {
            // Arrange
            SetupTestData();
            var categoryId = _categories.AddCategory("Food", CategoryType.Expense);
            var transactionDate = new DateTime(2024, 1, 1, 14, 30, 0);
            var createdAt = new DateTime(2024, 1, 2, 10, 15, 0);

            // Act
            int transactionId = _transactions.AddTransaction(transactionDate, categoryId, 25.50m, "Late entry", "Mom", createdAt);

            // Assert
            var transaction = _transactions.GetTransactionFromId(transactionId);
            Assert.NotNull(transaction);
            Assert.Equal(transactionDate, transaction.TransactionDate);
            Assert.Equal(createdAt, transaction.CreatedAt);
            Assert.Equal("Mom", transaction.CreatedBy);
        }

        #endregion

        #region DeleteTransaction Tests

        [Fact]
        public void DeleteTransaction_ExistingTransaction_ShouldRemoveFromDatabase()
        {
            // Arrange
            SetupTestData();
            var categoryId = _categories.AddCategory("Food", CategoryType.Expense);
            int transactionId = _transactions.AddTransaction(DateTime.Now, categoryId, 50m, "Test transaction", "Dad");

            // Act
            _transactions.DeleteTransaction(transactionId);

            // Assert
            var transaction = _transactions.GetTransactionFromId(transactionId);
            Assert.Null(transaction);
        }

        [Fact]
        public void DeleteTransaction_NonExistentTransaction_ShouldThrowInvalidOperationException()
        {
            // Arrange
            SetupTestData();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _transactions.DeleteTransaction(99999));
            Assert.Contains("not found", exception.Message);
        }

        #endregion

        #region GetAllTransactions Tests

        [Fact]
        public void GetAllTransactions_EmptyDatabase_ShouldReturnEmptyList()
        {
            // Arrange
            SetupTestData();

            // Act
            var transactions = _transactions.GetAllTransactions();

            // Assert
            Assert.Empty(transactions);
        }

        [Fact]
        public void GetAllTransactions_MultipleTransactions_ShouldReturnOrderedByDateDesc()
        {
            // Arrange
            SetupTestData();
            var categoryId = _categories.AddCategory("Food", CategoryType.Expense);

            var date1 = new DateTime(2024, 1, 1);
            var date2 = new DateTime(2024, 1, 2);
            var date3 = new DateTime(2024, 1, 3);

            _transactions.AddTransaction(date2, categoryId, 50m, "Middle", "Dad");
            _transactions.AddTransaction(date1, categoryId, 30m, "Oldest", "Mom");
            _transactions.AddTransaction(date3, categoryId, 70m, "Newest", "Son");

            // Act
            var transactions = _transactions.GetAllTransactions();

            // Assert
            Assert.Equal(3, transactions.Count);
            Assert.Equal("Newest", transactions[0].Description);
            Assert.Equal("Middle", transactions[1].Description);
            Assert.Equal("Oldest", transactions[2].Description);
        }

        #endregion

        #region UpdateTransaction Tests

        [Fact]
        public void UpdateTransaction_ValidData_ShouldUpdateAllFields()
        {
            // Arrange
            SetupTestData();
            var categoryId1 = _categories.AddCategory("Food", CategoryType.Expense);
            var categoryId2 = _categories.AddCategory("Transport", CategoryType.Expense);

            var originalDate = new DateTime(2024, 1, 1);
            var newDate = new DateTime(2024, 1, 2);

            int transactionId = _transactions.AddTransaction(originalDate, categoryId1, 25m, "Original", "Dad");

            // Act
            _transactions.UpdateTransaction(transactionId, newDate, "Updated Description", 35.75m, categoryId2, "Mom");

            // Assert
            var transaction = _transactions.GetTransactionFromId(transactionId);
            Assert.NotNull(transaction);
            Assert.Equal(transactionId, transaction.Id); // ID should remain the same
            Assert.Equal("Updated Description", transaction.Description);
            Assert.Equal(35.75m, transaction.Amount);
            Assert.Equal(categoryId2, transaction.CategoryId);
            Assert.Equal(newDate, transaction.TransactionDate);
            Assert.Equal("Mom", transaction.CreatedBy); // CreatedBy updated
        }

        [Fact]
        public void UpdateTransaction_NonExistentTransaction_ShouldThrowInvalidOperationException()
        {
            // Arrange
            SetupTestData();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _transactions.UpdateTransaction(99999, DateTime.Now, "Test", 50m, 1, "Dad"));
            Assert.Contains("not found", exception.Message);
        }

        #endregion

        #region GetTransactionFromId Tests

        [Fact]
        public void GetTransactionFromId_ExistingId_ShouldReturnTransaction()
        {
            // Arrange
            SetupTestData();
            var categoryId = _categories.AddCategory("Food", CategoryType.Expense);
            var date = new DateTime(2024, 1, 1, 15, 30, 45);
            int transactionId = _transactions.AddTransaction(date, categoryId, 42.75m, "Coffee and cake", "Mom");

            // Act
            var transaction = _transactions.GetTransactionFromId(transactionId);

            // Assert
            Assert.NotNull(transaction);
            Assert.Equal(transactionId, transaction.Id);
            Assert.Equal("Coffee and cake", transaction.Description);
            Assert.Equal(42.75m, transaction.Amount);
            Assert.Equal(categoryId, transaction.CategoryId);
            Assert.Equal(date, transaction.TransactionDate);
            Assert.Equal("Mom", transaction.CreatedBy);
        }

        [Fact]
        public void GetTransactionFromId_NonExistentId_ShouldReturnNull()
        {
            // Arrange
            SetupTestData();

            // Act
            var transaction = _transactions.GetTransactionFromId(99999);

            // Assert
            Assert.Null(transaction);
        }

        #endregion

        #region GetTransactionsByCreatedBy Tests

        [Fact]
        public void GetTransactionsByCreatedBy_ShouldReturnCorrectTransactions()
        {
            // Arrange
            SetupTestData();
            var categoryId = _categories.AddCategory("Food", CategoryType.Expense);

            _transactions.AddTransaction(DateTime.Now, categoryId, 25m, "Dad's transaction 1", "Dad");
            _transactions.AddTransaction(DateTime.Now, categoryId, 35m, "Mom's transaction", "Mom");
            _transactions.AddTransaction(DateTime.Now, categoryId, 45m, "Dad's transaction 2", "Dad");

            // Act
            var dadTransactions = _transactions.GetTransactionsByCreatedBy("Dad");
            var momTransactions = _transactions.GetTransactionsByCreatedBy("Mom");

            // Assert
            Assert.Equal(2, dadTransactions.Count);
            Assert.Single(momTransactions);
            Assert.All(dadTransactions, t => Assert.Equal("Dad", t.CreatedBy));
            Assert.All(momTransactions, t => Assert.Equal("Mom", t.CreatedBy));
        }

        [Fact]
        public void GetTransactionsByCreatedBy_NonExistentUser_ShouldReturnEmpty()
        {
            // Arrange
            SetupTestData();

            // Act
            var transactions = _transactions.GetTransactionsByCreatedBy("NonExistentUser");

            // Assert
            Assert.Empty(transactions);
        }

        #endregion

        #region GetTransactionsByDateRange Tests

        [Fact]
        public void GetTransactionsByDateRange_ShouldReturnCorrectTransactions()
        {
            // Arrange
            SetupTestData();
            var categoryId = _categories.AddCategory("Food", CategoryType.Expense);

            var date1 = new DateTime(2024, 1, 1);
            var date2 = new DateTime(2024, 1, 15);
            var date3 = new DateTime(2024, 2, 1);

            _transactions.AddTransaction(date1, categoryId, 25m, "January early", "Dad");
            _transactions.AddTransaction(date2, categoryId, 35m, "January mid", "Mom");
            _transactions.AddTransaction(date3, categoryId, 45m, "February", "Dad");

            // Act
            var januaryTransactions = _transactions.GetTransactionsByDateRange(
                new DateTime(2024, 1, 1),
                new DateTime(2024, 1, 31));

            // Assert
            Assert.Equal(2, januaryTransactions.Count);
            Assert.All(januaryTransactions, t => Assert.Equal(1, t.TransactionDate.Month));
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_ShouldPreventFurtherOperations()
        {
            // Arrange
            SetupTestData();

            // Act
            _transactions.Dispose();

            // Assert
            Assert.False(_transactions.IsConnected);
            Assert.Throws<ObjectDisposedException>(() => _transactions.GetAllTransactions());
        }

        #endregion

        #region Complete Workflow Test

        [Fact]
        public void CompleteWorkflow_AddUpdateDelete_ShouldWorkCorrectly()
        {
            // Arrange
            SetupTestData();
            var categoryId = _categories.AddCategory("Entertainment", CategoryType.Expense);

            // Act & Assert - Add
            int transactionId = _transactions.AddTransaction(DateTime.Now, categoryId, 15.50m, "Movie ticket", "Dad");
            Assert.True(transactionId > 0);

            // Act & Assert - Get
            var transaction = _transactions.GetTransactionFromId(transactionId);
            Assert.NotNull(transaction);
            Assert.Equal("Movie ticket", transaction.Description);
            Assert.Equal("Dad", transaction.CreatedBy);

            // Act & Assert - Update
            _transactions.UpdateTransaction(transactionId, DateTime.Now, "Movie + Popcorn", 22.75m, categoryId, "Mom");
            var updatedTransaction = _transactions.GetTransactionFromId(transactionId);
            Assert.Equal("Movie + Popcorn", updatedTransaction.Description);
            Assert.Equal(22.75m, updatedTransaction.Amount);
            Assert.Equal("Mom", updatedTransaction.CreatedBy);

            // Act & Assert - List (should have 1 transaction)
            var allTransactions = _transactions.GetAllTransactions();
            Assert.Single(allTransactions);
            Assert.Equal("Movie + Popcorn", allTransactions[0].Description);

            // Act & Assert - Delete
            _transactions.DeleteTransaction(transactionId);
            var deletedTransaction = _transactions.GetTransactionFromId(transactionId);
            Assert.Null(deletedTransaction);

            // Final check - list should be empty
            var emptyList = _transactions.GetAllTransactions();
            Assert.Empty(emptyList);
        }

        #endregion

        #region Real-world Scenario Tests

        [Fact]
        public void MultipleTransactions_WithDifferentTypes_ShouldWorkCorrectly()
        {
            // Arrange
            SetupTestData();
            var foodCategory = _categories.AddCategory("Food", CategoryType.Expense);
            var salaryCategory = _categories.AddCategory("Salary", CategoryType.Income);
            var transportCategory = _categories.AddCategory("Transport", CategoryType.Expense);

            // Act - Add various types of transactions
            _transactions.AddTransaction(new DateTime(2024, 1, 1), foodCategory, 35.50m, "Grocery shopping", "Mom");
            _transactions.AddTransaction(new DateTime(2024, 1, 2), salaryCategory, 2500.00m, "Monthly salary", "Dad");
            _transactions.AddTransaction(new DateTime(2024, 1, 3), transportCategory, 12.75m, "Bus ticket", "Son");
            _transactions.AddTransaction(new DateTime(2024, 1, 4), foodCategory, 8.50m, "Coffee", "Daughter");

            // Assert
            var allTransactions = _transactions.GetAllTransactions();
            Assert.Equal(4, allTransactions.Count);

            // Check order (newest first)
            Assert.Equal("Coffee", allTransactions[0].Description);
            Assert.Equal("Bus ticket", allTransactions[1].Description);
            Assert.Equal("Monthly salary", allTransactions[2].Description);
            Assert.Equal("Grocery shopping", allTransactions[3].Description);

            // Check creators
            Assert.Equal("Daughter", allTransactions[0].CreatedBy);
            Assert.Equal("Son", allTransactions[1].CreatedBy);
            Assert.Equal("Dad", allTransactions[2].CreatedBy);
            Assert.Equal("Mom", allTransactions[3].CreatedBy);

            // Check amounts (all positive in database)
            var expenses = allTransactions.Where(t => t.CategoryId == foodCategory || t.CategoryId == transportCategory).ToList();
            var income = allTransactions.Where(t => t.CategoryId == salaryCategory).ToList();

            Assert.Equal(3, expenses.Count);
            Assert.Single(income);
            Assert.Equal(2500.00m, income[0].Amount);

            // Calculate totals
            var totalExpenses = expenses.Sum(t => t.Amount);
            var totalIncome = income.Sum(t => t.Amount);
            Assert.Equal(56.75m, totalExpenses); // 35.50 + 12.75 + 8.50
            Assert.Equal(2500.00m, totalIncome);
        }

        [Fact]
        public void FamilyUsageScenario_ShouldTrackCorrectly()
        {
            // Arrange
            SetupTestData();
            var foodCategory = _categories.AddCategory("Food", CategoryType.Expense);
            var entertainmentCategory = _categories.AddCategory("Entertainment", CategoryType.Expense);

            // Act - Family members add different transactions
            _transactions.AddTransaction(DateTime.Now.AddDays(-3), foodCategory, 150m, "Weekly groceries", "Mom");
            _transactions.AddTransaction(DateTime.Now.AddDays(-2), entertainmentCategory, 25m, "Movie tickets", "Dad");
            _transactions.AddTransaction(DateTime.Now.AddDays(-1), foodCategory, 12m, "School lunch", "Son");
            _transactions.AddTransaction(DateTime.Now, entertainmentCategory, 8m, "Ice cream", "Daughter");

            // Assert - Check family spending patterns
            var momTransactions = _transactions.GetTransactionsByCreatedBy("Mom");
            var dadTransactions = _transactions.GetTransactionsByCreatedBy("Dad");
            var sonTransactions = _transactions.GetTransactionsByCreatedBy("Son");
            var daughterTransactions = _transactions.GetTransactionsByCreatedBy("Daughter");

            Assert.Single(momTransactions);
            Assert.Single(dadTransactions);
            Assert.Single(sonTransactions);
            Assert.Single(daughterTransactions);

            Assert.Equal(150m, momTransactions[0].Amount);
            Assert.Equal(25m, dadTransactions[0].Amount);
            Assert.Equal(12m, sonTransactions[0].Amount);
            Assert.Equal(8m, daughterTransactions[0].Amount);

            // Total family spending
            var allTransactions = _transactions.GetAllTransactions();
            var totalSpending = allTransactions.Sum(t => t.Amount);
            Assert.Equal(195m, totalSpending);
        }

        #endregion
    }
}