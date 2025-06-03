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
            int transactionId = _transactions.AddTransaction(date, categoryId, -25.50m, "Lunch at cafe");

            // Assert
            Assert.True(transactionId > 0);
            var transaction = _transactions.GetTransactionFromId(transactionId);
            Assert.NotNull(transaction);
            Assert.Equal("Lunch at cafe", transaction.Description);
            Assert.Equal(-25.50m, transaction.Amount);
            Assert.Equal(categoryId, transaction.CategoryId);
            Assert.Equal(date, transaction.Date);
        }

        [Fact]
        public void AddTransaction_ZeroAmount_ShouldThrowArgumentException()
        {
            // Arrange
            SetupTestData();
            var categoryId = _categories.AddCategory("Food", CategoryType.Expense);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _transactions.AddTransaction(DateTime.Now, categoryId, 0m, "Test"));
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
                _transactions.AddTransaction(DateTime.Now, categoryId, -50m, ""));
            Assert.Contains("Description cannot be null or empty", exception.Message);
        }

        [Fact]
        public void AddTransaction_VariousAmounts_ShouldWork()
        {
            // Arrange
            SetupTestData();
            var expenseCategory = _categories.AddCategory("Expense", CategoryType.Expense);
            var incomeCategory = _categories.AddCategory("Income", CategoryType.Income);

            // Act & Assert - Test different amount types
            var expenseId = _transactions.AddTransaction(DateTime.Now, expenseCategory, -123.45m, "Expense");
            var incomeId = _transactions.AddTransaction(DateTime.Now, incomeCategory, 1500.00m, "Salary");
            var smallId = _transactions.AddTransaction(DateTime.Now, expenseCategory, -0.01m, "Small expense");

            var expense = _transactions.GetTransactionFromId(expenseId);
            var income = _transactions.GetTransactionFromId(incomeId);
            var small = _transactions.GetTransactionFromId(smallId);

            Assert.Equal(-123.45m, expense.Amount);
            Assert.Equal(1500.00m, income.Amount);
            Assert.Equal(-0.01m, small.Amount);
        }

        #endregion

        #region DeleteTransaction Tests

        [Fact]
        public void DeleteTransaction_ExistingTransaction_ShouldRemoveFromDatabase()
        {
            // Arrange
            SetupTestData();
            var categoryId = _categories.AddCategory("Food", CategoryType.Expense);
            int transactionId = _transactions.AddTransaction(DateTime.Now, categoryId, -50m, "Test transaction");

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

            _transactions.AddTransaction(date2, categoryId, -50m, "Middle");
            _transactions.AddTransaction(date1, categoryId, -30m, "Oldest");
            _transactions.AddTransaction(date3, categoryId, -70m, "Newest");

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

            int transactionId = _transactions.AddTransaction(originalDate, categoryId1, -25m, "Original");

            // Act
            _transactions.UpdateTransaction(transactionId, newDate, "Updated Description", -35.75m, categoryId2);

            // Assert
            var transaction = _transactions.GetTransactionFromId(transactionId);
            Assert.NotNull(transaction);
            Assert.Equal(transactionId, transaction.Id); // ID should remain the same
            Assert.Equal("Updated Description", transaction.Description);
            Assert.Equal(-35.75m, transaction.Amount);
            Assert.Equal(categoryId2, transaction.CategoryId);
            Assert.Equal(newDate, transaction.Date);
        }

        [Fact]
        public void UpdateTransaction_NonExistentTransaction_ShouldThrowInvalidOperationException()
        {
            // Arrange
            SetupTestData();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _transactions.UpdateTransaction(99999, DateTime.Now, "Test", 50m, 1));
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
            int transactionId = _transactions.AddTransaction(date, categoryId, -42.75m, "Coffee and cake");

            // Act
            var transaction = _transactions.GetTransactionFromId(transactionId);

            // Assert
            Assert.NotNull(transaction);
            Assert.Equal(transactionId, transaction.Id);
            Assert.Equal("Coffee and cake", transaction.Description);
            Assert.Equal(-42.75m, transaction.Amount);
            Assert.Equal(categoryId, transaction.CategoryId);
            Assert.Equal(date, transaction.Date);
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
            int transactionId = _transactions.AddTransaction(DateTime.Now, categoryId, -15.50m, "Movie ticket");
            Assert.True(transactionId > 0);

            // Act & Assert - Get
            var transaction = _transactions.GetTransactionFromId(transactionId);
            Assert.NotNull(transaction);
            Assert.Equal("Movie ticket", transaction.Description);

            // Act & Assert - Update
            _transactions.UpdateTransaction(transactionId, DateTime.Now, "Movie + Popcorn", -22.75m, categoryId);
            var updatedTransaction = _transactions.GetTransactionFromId(transactionId);
            Assert.Equal("Movie + Popcorn", updatedTransaction.Description);
            Assert.Equal(-22.75m, updatedTransaction.Amount);

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
            _transactions.AddTransaction(new DateTime(2024, 1, 1), foodCategory, -35.50m, "Grocery shopping");
            _transactions.AddTransaction(new DateTime(2024, 1, 2), salaryCategory, 2500.00m, "Monthly salary");
            _transactions.AddTransaction(new DateTime(2024, 1, 3), transportCategory, -12.75m, "Bus ticket");
            _transactions.AddTransaction(new DateTime(2024, 1, 4), foodCategory, -8.50m, "Coffee");

            // Assert
            var allTransactions = _transactions.GetAllTransactions();
            Assert.Equal(4, allTransactions.Count);

            // Check order (newest first)
            Assert.Equal("Coffee", allTransactions[0].Description);
            Assert.Equal("Bus ticket", allTransactions[1].Description);
            Assert.Equal("Monthly salary", allTransactions[2].Description);
            Assert.Equal("Grocery shopping", allTransactions[3].Description);

            // Check amounts by type
            var expenses = allTransactions.Where(t => t.Amount < 0).ToList();
            var income = allTransactions.Where(t => t.Amount > 0).ToList();

            Assert.Equal(3, expenses.Count);
            Assert.Single(income);
            Assert.Equal(2500.00m, income[0].Amount);

            // Calculate total
            var totalExpenses = expenses.Sum(t => t.Amount);
            var totalIncome = income.Sum(t => t.Amount);
            Assert.Equal(-56.75m, totalExpenses); // -35.50 + -12.75 + -8.50
            Assert.Equal(2500.00m, totalIncome);
        }

        #endregion
    }
}