using Budget.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Budget.Models.Category;

namespace TestBudget
{
    /// <summary>
    /// Unit tests for the Categories service class.
    /// </summary>
    public class TestCategories : IDisposable
    {
        private readonly string _testDatabasePath;
        private Categories _categories;
        public TestCategories()
        {
            // Create a unique test database for each test run
            _testDatabasePath = Path.Combine(Path.GetTempPath(), $"test_budget_{Guid.NewGuid()}.db");
        }

        public void Dispose()
        {
            _categories?.Dispose();
            if (File.Exists(_testDatabasePath))
            {
                File.Delete(_testDatabasePath);
            }
        }

        #region Constructor Tests
        [Fact]
        public void Constructor_WithNewDatabase_ShouldCreateDefaultCategories()
        {
            // Act
            _categories = new Categories(_testDatabasePath, isNew :true);

            // Assert
            Assert.True(_categories.IsConnected);
            var categories = _categories.List();
            Assert.True(categories.Count > 0);
            Assert.Contains(categories, c => c.Name == "Utilities");
            Assert.Contains(categories, c => c.Name == "Education");
        }
        [Fact]
        public void Constructor_WithNullPath_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Categories((string)null));
        }

        #endregion
        #region Core CRUD Tests

        [Fact]
        public void Add_ValidCategory_ShouldReturnNewId()
        {
            // Arrange
            _categories = new Categories(_testDatabasePath, isNew: true);

            // Act
            int categoryId = _categories.Add("Test Category", CategoryType.Expense);

            // Assert
            Assert.True(categoryId > 0);
            var category = _categories.GetCategoryFromId(categoryId);
            Assert.Equal("Test Category", category.Name);
            Assert.Equal(CategoryType.Expense, category.Type);
        }

        [Fact]
        public void Add_EmptyName_ShouldThrowArgumentException()
        {
            // Arrange
            _categories = new Categories(_testDatabasePath, isNew: true);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _categories.Add("", CategoryType.Expense));
        }

        [Fact]
        public void Delete_ExistingCategory_ShouldRemoveFromDatabase()
        {
            // Arrange
            _categories = new Categories(_testDatabasePath, isNew: true);
            int categoryId = _categories.Add("Test Category", CategoryType.Expense);

            // Act
            _categories.Delete(categoryId);

            // Assert
            var category = _categories.GetCategoryFromId(categoryId);
            Assert.Null(category);
        }

        [Fact]
        public void Delete_NonExistentCategory_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _categories = new Categories(_testDatabasePath, isNew: true);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _categories.Delete(99999));
        }

        [Fact]
        public void UpdateCategory_ValidData_ShouldUpdateSuccessfully()
        {
            // Arrange
            _categories = new Categories(_testDatabasePath, isNew: true);
            int categoryId = _categories.Add("Original", CategoryType.Expense);

            // Act
            _categories.UpdateCategory(categoryId, "Updated", CategoryType.Income);

            // Assert
            var category = _categories.GetCategoryFromId(categoryId);
            Assert.Equal("Updated", category.Name);
            Assert.Equal(CategoryType.Income, category.Type);
        }

        #endregion

        #region Query Tests

        [Fact]
        public void List_ShouldReturnAllCategories()
        {
            // Arrange
            _categories = new Categories(_testDatabasePath, isNew: true);
            int initialCount = _categories.List().Count;
            _categories.Add("Test1", CategoryType.Expense);
            _categories.Add("Test2", CategoryType.Income);

            // Act
            var categories = _categories.List();

            // Assert
            Assert.Equal(initialCount + 2, categories.Count);
        }

        [Fact]
        public void GetCategoriesByType_ShouldReturnOnlyMatchingType()
        {
            // Arrange
            _categories = new Categories(_testDatabasePath, isNew: true);
            _categories.Add("Expense1", CategoryType.Expense);
            _categories.Add("Income1", CategoryType.Income);

            // Act
            var expenses = _categories.GetCategoriesByType(CategoryType.Expense);
            var incomes = _categories.GetCategoriesByType(CategoryType.Income);

            // Assert
            Assert.All(expenses, c => Assert.Equal(CategoryType.Expense, c.Type));
            Assert.All(incomes, c => Assert.Equal(CategoryType.Income, c.Type));
            Assert.Contains(expenses, c => c.Name == "Expense1");
            Assert.Contains(incomes, c => c.Name == "Income1");
        }

        [Fact]
        public void GetCategoryFromId_ExistingId_ShouldReturnCategory()
        {
            // Arrange
            _categories = new Categories(_testDatabasePath, isNew: true);
            int categoryId = _categories.Add("Test", CategoryType.Expense);

            // Act
            var category = _categories.GetCategoryFromId(categoryId);

            // Assert
            Assert.NotNull(category);
            Assert.Equal("Test", category.Name);
        }

        [Fact]
        public void GetCategoryFromId_NonExistentId_ShouldReturnNull()
        {
            // Arrange
            _categories = new Categories(_testDatabasePath, isNew: true);

            // Act
            var category = _categories.GetCategoryFromId(99999);

            // Assert
            Assert.Null(category);
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_ShouldAllowMultipleCalls()
        {
            // Arrange
            _categories = new Categories(_testDatabasePath, isNew: true);
            Assert.True(_categories.IsConnected);

            // Act - Multiple dispose calls should not throw
            _categories.Dispose();
            _categories.Dispose(); // Should not throw

            // Assert - Accessing properties after dispose should throw
            Assert.Throws<ObjectDisposedException>(() => _categories.IsConnected);
        }

        [Fact]
        public void DisposedInstance_ShouldThrowObjectDisposedException()
        {
            // Arrange
            _categories = new Categories(_testDatabasePath, isNew: true);
            _categories.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => _categories.List());
        }

        #endregion

        #region Integration Test

        [Fact]
        public void CompleteWorkflow_ShouldWorkCorrectly()
        {
            // Arrange
            _categories = new Categories(_testDatabasePath, isNew: true);

            // Add -> Update -> Delete workflow
            int id = _categories.Add("Groceries", CategoryType.Expense);
            _categories.UpdateCategory(id, "Food Shopping", CategoryType.Expense);

            var updated = _categories.GetCategoryFromId(id);
            Assert.Equal("Food Shopping", updated.Name);

            _categories.Delete(id);
            var deleted = _categories.GetCategoryFromId(id);
            Assert.Null(deleted);
        }
        #endregion
    }
}
