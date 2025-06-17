using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Budget.Models
{
    /// <summary>
    /// Represents a single budget transaction item, including details such as date, amount,
    /// category, balance, and metadata about who created it.
    /// </summary>
    public class BudgetItem
    {
        /// <summary>
        /// Gets or sets the ID of the category associated with the transaction.
        /// </summary>
        public int CategoryID { get; set; }

        /// <summary>
        /// Gets or sets the ID of the transaction.
        /// </summary>
        public int TransactionID { get; set; }

        /// <summary>
        /// Gets or sets the date of the transaction.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the name of the category.
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Gets or sets a short description of the transaction.
        /// </summary>
        public string? ShortDescription { get; set; }

        /// <summary>
        /// Gets or sets the amount of the transaction.
        /// Positive for income, negative for expense.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Gets or sets the balance after the transaction was applied.
        /// </summary>
        public decimal Balance { get; set; }

        /// <summary>
        /// Gets or sets the username of the person who created the transaction.
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the transaction was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

    }
    /// <summary>
    /// Represents a collection of budget items grouped by a specific month,
    /// including the total amount for that month.
    /// </summary>
    public class BudgetItemsByMonth
    {

        /// <summary>
        /// Gets or sets the name of the month (e.g., "January 2025").
        /// </summary>
        public string? Month { get; set; }

        /// <summary>
        /// Gets or sets the list of budget items for this month.
        /// </summary>
        public List<BudgetItem>? Details { get; set; }

        /// <summary>
        /// Gets or sets the total amount for all transactions in this month.
        /// </summary>
        public decimal Total { get; set; }
    }

    /// <summary>
    /// Represents a collection of budget items grouped by a specific category,
    /// including the total amount for that category.
    /// </summary>
    public class BudgetItemsByCategory
    {
        /// <summary>
        /// Gets or sets the name of the category.
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Gets or sets the list of budget items for this category.
        /// </summary>
        public List<BudgetItem>? Details { get; set; }

        /// <summary>
        /// Gets or sets the total amount for all transactions in this category.
        /// </summary>
        public decimal Total { get; set; }
    }
}
