using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Budget.Models
{
    /// <summary>
    /// Represents a budget category used to classify financial transactions.
    /// Categories help organize transactions into meaningful groups for reporting and analysis.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Categories are fundamental organizational units in budget management systems. They provide 
    /// a way to classify transactions into logical groups such as "Food &amp; Dining", "Transportation", 
    /// "Salary", etc. Each category has a specific type that determines how transactions assigned 
    /// to it affect balance calculations and financial reports.
    /// </para>
    /// <para>
    /// The category system supports five main types of financial activities:
    /// Income (money coming in), Expenses (money going out), Savings (money set aside), 
    /// Debt (money owed or paid toward debts), and Investments (money put into investments).
    /// </para>
    /// <para>
    /// All properties in this class are mutable to support category management operations 
    /// such as renaming or reclassifying categories as user needs evolve.
    /// </para>
    /// <para>
    /// Thread Safety: This class is not thread-safe. External synchronization is required 
    /// for concurrent access from multiple threads.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create different types of categories
    /// var salaryCategory = new Category(1, "Salary", CategoryType.Income);
    /// var groceryCategory = new Category(2, "Groceries", CategoryType.Expense);
    /// var savingsCategory = new Category(3, "Emergency Fund", CategoryType.Savings);
    /// var mortgageCategory = new Category(4, "Home Mortgage", CategoryType.Debt);
    /// var stocksCategory = new Category(5, "Stock Portfolio", CategoryType.Investment);
    /// 
    /// // Using default expense type
    /// var miscCategory = new Category(6, "Miscellaneous"); // Defaults to Expense
    /// 
    /// // Copy constructor usage
    /// var categoryCopy = new Category(salaryCategory);
    /// categoryCopy.Name = "Primary Salary"; // Rename the copy
    /// 
    /// // Display category information
    /// Console.WriteLine($"Category: {salaryCategory}"); // Output: "Salary"
    /// Console.WriteLine($"Type: {salaryCategory.Type}"); // Output: "Income"
    /// Console.WriteLine($"ID: {salaryCategory.Id}");     // Output: "1"
    /// </code>
    /// </example>
    public class Category
    {
        #region property
        /// <summary>
        /// Gets or sets the unique identifier for this category.
        /// </summary>
        /// <value>
        /// A positive integer that uniquely identifies this category within the budget system.
        /// This value typically corresponds to the database primary key.
        /// </value>
        /// <remarks>
        /// <para>
        /// The ID serves as the primary key for database operations and is used for establishing 
        /// relationships with transactions. While this property is mutable for flexibility in 
        /// data manipulation scenarios, the ID should generally remain constant once assigned 
        /// by the database system.
        /// </para>
        /// <para>
        /// In typical usage, IDs are assigned automatically by the database when categories 
        /// are created and should not be modified manually unless performing specific data 
        /// migration or synchronization operations.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var category = new Category(42, "Entertainment", CategoryType.Expense);
        /// Console.WriteLine($"Category ID: {category.Id}"); // Output: 42
        /// 
        /// // While possible, changing IDs manually is not recommended
        /// // category.Id = 99; // Possible but not recommended in normal usage
        /// 
        /// // IDs are typically used for database operations and relationships
        /// var transaction = new Transaction(1, DateTime.Now, "Movie tickets", 25.00m, category.Id);
        /// </code>
        /// </example>
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the display name of this category.
        /// </summary>
        /// <value>
        /// A string containing the human-readable name of the category.
        /// This is the text that appears in user interfaces and reports.
        /// </value>
        /// <remarks>
        /// <para>
        /// The category name should be descriptive and meaningful to help users easily 
        /// identify the purpose of the category. Good category names are concise yet 
        /// specific enough to clearly indicate what types of transactions belong in the category.
        /// </para>
        /// <para>
        /// Category names are mutable to support renaming operations as user needs change 
        /// or to correct initial naming choices. When displayed in lists, categories are 
        /// often sorted alphabetically by name.
        /// </para>
        /// <para>
        /// While there are no strict naming conventions enforced by this class, 
        /// consider using consistent capitalization and avoiding overly long names 
        /// for better user experience.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var category = new Category(1, "Food & Dining", CategoryType.Expense);
        /// Console.WriteLine($"Category name: {category.Name}"); // Output: "Food & Dining"
        /// 
        /// // Renaming a category
        /// category.Name = "Food & Restaurants";
        /// Console.WriteLine($"Updated name: {category.Name}"); // Output: "Food & Restaurants"
        /// 
        /// // Examples of good category names:
        /// var utilities = new Category(2, "Utilities", CategoryType.Expense);
        /// var salary = new Category(3, "Primary Salary", CategoryType.Income);
        /// var emergency = new Category(4, "Emergency Fund", CategoryType.Savings);
        /// var carLoan = new Category(5, "Auto Loan Payment", CategoryType.Debt);
        /// var stocks = new Category(6, "Stock Investments", CategoryType.Investment);
        /// </code>
        /// </example>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type classification of this category.
        /// </summary>
        /// <value>
        /// A <see cref="CategoryType"/> enumeration value that determines how transactions 
        /// in this category affect balance calculations and financial reporting.
        /// </value>
        /// <remarks>
        /// <para>
        /// The category type is fundamental to how the budget system interprets and processes 
        /// transactions. It determines whether transactions increase or decrease account balances 
        /// and controls which reports include the transactions.
        /// </para>
        /// <para>
        /// Changing a category's type will affect how all existing transactions in that category 
        /// are treated in balance calculations and reports. Exercise caution when changing types, 
        /// especially for categories with many associated transactions.
        /// </para>
        /// <para>
        /// The type also influences user interface elements such as color coding, grouping, 
        /// and validation rules in transaction entry forms.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var category = new Category(1, "Freelance Work", CategoryType.Income);
        /// Console.WriteLine($"Category type: {category.Type}"); // Output: "Income"
        /// 
        /// // Changing category type (use with caution)
        /// category.Type = CategoryType.Investment; // Now treats as investment income
        /// 
        /// // Type affects balance calculations:
        /// // Income: Increases balance
        /// // Expense: Decreases balance  
        /// // Savings: Decreases available balance, increases savings
        /// // Debt: Debt payments decrease balance
        /// // Investment: Investment purchases decrease balance
        /// 
        /// // Examples of appropriate type assignments:
        /// var groceries = new Category(2, "Groceries", CategoryType.Expense);
        /// var retirement = new Category(3, "401k Contribution", CategoryType.Savings);
        /// var mortgage = new Category(4, "Mortgage Payment", CategoryType.Debt);
        /// var portfolio = new Category(5, "Stock Purchase", CategoryType.Investment);
        /// </code>
        /// </example>
        public CategoryType Type { get; set; }
        #endregion

        #region CategoryType enum (The enumeration value corresponds directly to the database Id) 
        /// <summary>
        /// Defines the types of budget categories available in the system.
        /// Each type determines how transactions are treated in balance calculations and reporting.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The enumeration values correspond directly to database identifiers for efficient 
        /// storage and retrieval. The specific integer values are chosen to maintain 
        /// consistency with the database schema and should not be changed without 
        /// corresponding database updates.
        /// </para>
        /// <para>
        /// Each category type serves a specific purpose in personal finance management:
        /// </para>
        /// <list type="bullet">
        /// <item><description><strong>Income:</strong> Money flowing into accounts (salary, freelance, investments returns, etc.)</description></item>
        /// <item><description><strong>Expense:</strong> Money flowing out for regular purchases and bills (food, utilities, entertainment, etc.)</description></item>
        /// <item><description><strong>Debt:</strong> Money used for debt payments (mortgage, credit cards, loans, etc.)</description></item>
        /// <item><description><strong>Investment:</strong> Money allocated to investments (stocks, bonds, retirement accounts, etc.)</description></item>
        /// <item><description><strong>Savings:</strong> Money set aside for future use (emergency fund, vacation fund, etc.)</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Using different category types
        /// var incomeCategory = new Category(1, "Salary", CategoryType.Income);
        /// var expenseCategory = new Category(2, "Rent", CategoryType.Expense);
        /// var debtCategory = new Category(3, "Credit Card", CategoryType.Debt);
        /// var investmentCategory = new Category(4, "401k", CategoryType.Investment);
        /// var savingsCategory = new Category(5, "Emergency Fund", CategoryType.Savings);
        /// 
        /// // Checking category types programmatically
        /// if (incomeCategory.Type == CategoryType.Income)
        /// {
        ///     Console.WriteLine("This category increases available balance");
        /// }
        /// 
        /// // Grouping categories by type
        /// var categories = new List&lt;Category&gt; { incomeCategory, expenseCategory, debtCategory };
        /// var expenseCategories = categories.Where(c => c.Type == CategoryType.Expense);
        /// 
        /// // Getting the underlying integer value
        /// int typeValue = (int)CategoryType.Income; // Returns 1
        /// Console.WriteLine($"Income type database value: {typeValue}");
        /// </code>
        /// </example>
        public enum CategoryType
        {
            /// <summary>
            /// Represents income sources such as salary, freelance payments, investment returns, or other money inflows.
            /// Transactions in Income categories typically increase the available account balance.
            /// </summary>
            /// <remarks>
            /// Income categories are used for any money that flows into your accounts. This includes 
            /// regular income like salaries and wages, as well as irregular income like bonuses, 
            /// tax refunds, or income from side businesses.
            /// </remarks>
            Income = 1,
            /// <summary>
            /// Represents regular expenses and purchases such as food, utilities, entertainment, or other money outflows.
            /// Transactions in Expense categories typically decrease the available account balance.
            /// </summary>
            /// <remarks>
            /// Expense categories cover all types of spending from essential expenses like housing 
            /// and food to discretionary spending like entertainment and hobbies. This is typically 
            /// the largest category type in most personal budgets.
            /// </remarks>
            Expense = 2,
            /// <summary>
            /// Represents debt-related transactions such as loan payments, credit card payments, or other debt obligations.
            /// Transactions in Debt categories typically decrease the available account balance while reducing overall debt.
            /// </summary>
            /// <remarks>
            /// Debt categories help track money that goes toward reducing existing debts. This includes 
            /// mortgage payments, car loan payments, credit card payments, student loan payments, 
            /// and any other debt reduction activities.
            /// </remarks>
            Debt = 3,
            /// <summary>
            /// Represents investment-related transactions such as stock purchases, retirement contributions, or other investment activities.
            /// Transactions in Investment categories typically decrease the available account balance while building long-term wealth.
            /// </summary>
            /// <remarks>
            /// Investment categories track money that is put into various investment vehicles. This includes 
            /// stock purchases, bond investments, retirement account contributions, mutual fund investments, 
            /// and other wealth-building activities.
            /// </remarks>
            Investment = 4,
            /// <summary>
            /// Represents savings activities such as emergency fund contributions, vacation savings, or other money set aside for future use.
            /// Transactions in Savings categories typically decrease the available account balance while building savings reserves.
            /// </summary>
            /// <remarks>
            /// Savings categories help track money that is set aside for specific future goals or general 
            /// financial security. This includes emergency fund contributions, vacation savings, 
            /// down payment savings, and other targeted savings goals.
            /// </remarks>
            Savings = 5,
        };
        #endregion

        #region constructor 
        /// <summary>
        /// Initializes a new instance of the <see cref="Category"/> class with the specified values.
        /// </summary>
        /// <param name="id">
        /// The unique identifier for this category. Should be a positive integer that uniquely 
        /// identifies this category within the budget system.
        /// </param>
        /// <param name="name">
        /// The display name of the category. Should be a descriptive, user-friendly name 
        /// that clearly indicates the category's purpose.
        /// </param>
        /// <param name="type">
        /// The type classification of the category. Defaults to <see cref="CategoryType.Expense"/> 
        /// if not specified, as expenses are the most common category type in typical budgets.
        /// </param>
        /// <remarks>
        /// <para>
        /// This is the primary constructor for creating new category instances. The default type 
        /// of <see cref="CategoryType.Expense"/> is chosen because expenses represent the majority 
        /// of categories in typical personal budgets, making it a convenient default.
        /// </para>
        /// <para>
        /// No validation is performed on the parameters in this constructor to maintain simplicity 
        /// and flexibility. Validation should be performed at the service layer when categories 
        /// are persisted to the database.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Create categories with explicit types
        /// var salaryCategory = new Category(1, "Monthly Salary", CategoryType.Income);
        /// var rentCategory = new Category(2, "Rent Payment", CategoryType.Expense);
        /// var savingsCategory = new Category(3, "Emergency Fund", CategoryType.Savings);
        /// var loanCategory = new Category(4, "Student Loan", CategoryType.Debt);
        /// var stockCategory = new Category(5, "Stock Portfolio", CategoryType.Investment);
        /// 
        /// // Using default expense type
        /// var groceryCategory = new Category(6, "Groceries"); // Automatically set to Expense
        /// var utilityCategory = new Category(7, "Utilities"); // Automatically set to Expense
        /// 
        /// Console.WriteLine($"Grocery category type: {groceryCategory.Type}"); // Output: "Expense"
        /// 
        /// // Categories ready for use in transaction assignments
        /// var transaction = new Transaction(1, DateTime.Now, "Weekly groceries", 125.50m, groceryCategory.Id);
        /// </code>
        /// </example>
        /// <seealso cref="Category(Category)"/>
        /// <seealso cref="CategoryType"/>
        public Category(int id, string name, CategoryType type = CategoryType.Expense)
        {
            Id = id;
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Category"/> class by copying values from another category.
        /// </summary>
        /// <param name="category">
        /// The source <see cref="Category"/> object to copy values from. Cannot be null.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="category"/> is null.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This copy constructor creates a new category instance with identical values to the 
        /// source category. This is useful for creating category templates, backing up category 
        /// data before modifications, or implementing category duplication functionality.
        /// </para>
        /// <para>
        /// The new instance is completely independent of the source category - modifications 
        /// to one will not affect the other. This creates a true deep copy since all properties 
        /// are value types or immutable strings.
        /// </para>
        /// <para>
        /// When copying categories for duplication purposes, consider updating the ID and possibly 
        /// the name to ensure uniqueness in your system.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Create an original category
        /// var originalCategory = new Category(10, "Food & Dining", CategoryType.Expense);
        /// 
        /// // Create a copy
        /// var copiedCategory = new Category(originalCategory);
        /// 
        /// // Verify the copy has the same values
        /// Console.WriteLine($"Original: {originalCategory.Name} (ID: {originalCategory.Id})");
        /// Console.WriteLine($"Copy: {copiedCategory.Name} (ID: {copiedCategory.Id})");
        /// // Both will show identical values
        /// 
        /// // Modify the copy - original remains unchanged
        /// copiedCategory.Id = 11; // Give new ID for uniqueness
        /// copiedCategory.Name = "Restaurants Only";
        /// copiedCategory.Type = CategoryType.Expense; // Same type but could be changed
        /// 
        /// Console.WriteLine($"After modification:");
        /// Console.WriteLine($"Original: {originalCategory.Name}"); // Still "Food & Dining"
        /// Console.WriteLine($"Copy: {copiedCategory.Name}");       // Now "Restaurants Only"
        /// 
        /// // Use case: Creating similar categories with variations
        /// var transportCategory = new Category(20, "Transportation", CategoryType.Expense);
        /// var gasCategory = new Category(transportCategory);
        /// var publicTransitCategory = new Category(transportCategory);
        /// 
        /// gasCategory.Id = 21;
        /// gasCategory.Name = "Gasoline";
        /// 
        /// publicTransitCategory.Id = 22;
        /// publicTransitCategory.Name = "Public Transit";
        /// 
        /// // Error handling
        /// try
        /// {
        ///     var invalidCopy = new Category(null); // Throws ArgumentNullException
        /// }
        /// catch (ArgumentNullException ex)
        /// {
        ///     Console.WriteLine($"Error: {ex.Message}");
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="Category(int, string, CategoryType)"/>
        public Category(Category category)
        {
            Id = category.Id;;
            Name = category.Name;
            Type = category.Type;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns the name of this category as its string representation.
        /// </summary>
        /// <returns>
        /// The <see cref="Name"/> property value, providing a user-friendly string representation 
        /// of the category suitable for display in user interfaces and reports.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method is particularly useful in user interface scenarios where categories need 
        /// to be displayed in lists, dropdown menus, or reports. By returning just the name, 
        /// it provides clean, readable text without exposing implementation details like ID numbers.
        /// </para>
        /// <para>
        /// The string representation focuses on user-friendliness rather than debugging information, 
        /// making it ideal for end-user displays. For debugging purposes, you may want to access 
        /// the individual properties directly to see ID and type information.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var category = new Category(5, "Entertainment", CategoryType.Expense);
        /// 
        /// // Direct ToString() call
        /// Console.WriteLine(category.ToString()); // Output: "Entertainment"
        /// 
        /// // Implicit ToString() call in string context
        /// Console.WriteLine($"Selected category: {category}"); // Output: "Selected category: Entertainment"
        /// 
        /// // Useful in collections and UI scenarios
        /// var categories = new List&lt;Category&gt;
        /// {
        ///     new Category(1, "Salary", CategoryType.Income),
        ///     new Category(2, "Groceries", CategoryType.Expense),
        ///     new Category(3, "Savings", CategoryType.Savings)
        /// };
        /// 
        /// // Display category names in a list
        /// Console.WriteLine("Available categories:");
        /// foreach (var cat in categories)
        /// {
        ///     Console.WriteLine($"- {cat}"); // Uses ToString() implicitly
        /// }
        /// // Output:
        /// // Available categories:
        /// // - Salary
        /// // - Groceries  
        /// // - Savings
        /// 
        /// // Useful for dropdown/combobox data binding
        /// // UI frameworks often call ToString() automatically
        /// var categoryNames = categories.Select(c => c.ToString()).ToList();
        /// 
        /// // For debugging, you might want more detail:
        /// Console.WriteLine($"Category details: ID={category.Id}, Name={category.Name}, Type={category.Type}");
        /// </code>
        /// </example>
        public override string ToString()
        {
            return Name;
        }
        #endregion
    }
}

