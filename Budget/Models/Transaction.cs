using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Budget.Models
{
    /// <summary>
    /// Represents a financial transaction with associated metadata such as category, description, and creator information.
    /// </summary>
    public class Transaction
    {
        #region Properties

        /// <summary>
        /// Gets the unique identifier for the transaction.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the date when the transaction occurred.
        /// </summary>
        public DateTime TransactionDate { get; }

        /// <summary>
        /// Gets or sets the monetary amount of the transaction.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Gets or sets the description of the transaction.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the identifier for the category this transaction belongs to.
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// Gets or sets the username of the person who created the transaction.
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the transaction was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Transaction"/> class with all properties explicitly set, including ID.
        /// </summary>
        /// <param name="id">The unique identifier of the transaction.</param>
        /// <param name="transactionDate">The date of the transaction.</param>
        /// <param name="categoryId">The ID of the associated category.</param>
        /// <param name="amount">The amount of the transaction.</param>
        /// <param name="description">A brief description of the transaction.</param>
        /// <param name="createdBy">The username of the creator.</param>
        /// <param name="createdAt">The date and time the transaction was created.</param>
        /// <exception cref="ArgumentException">Thrown when id or categoryId is less than or equal to zero.</exception>
        public Transaction(int id, DateTime transactionDate, int categoryId, decimal amount, string description, string createdBy, DateTime createdAt)
        {
            if (id <= 0)
                throw new ArgumentException("ID must be greater than zero.", nameof(id));
            if (categoryId <= 0)
                throw new ArgumentException("Category ID must be greater than zero.", nameof(categoryId));

            Id = id;
            TransactionDate = transactionDate;
            CategoryId = categoryId;
            Amount = amount;
            Description = description;
            CreatedBy = createdBy;
            CreatedAt = createdAt;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Transaction"/> class without specifying an ID or creation time.
        /// Creation time defaults to current time.
        /// </summary>
        /// <param name="transactionDate">The date of the transaction.</param>
        /// <param name="categoryId">The ID of the associated category.</param>
        /// <param name="amount">The amount of the transaction.</param>
        /// <param name="description">A brief description of the transaction.</param>
        /// <param name="createdBy">The username of the creator.</param>
        /// <exception cref="ArgumentException">Thrown when categoryId is less than or equal to zero.</exception>
        public Transaction(DateTime transactionDate, int categoryId, decimal amount, string description, string createdBy)
        {
            if (categoryId <= 0)
                throw new ArgumentException("Category ID must be greater than zero.", nameof(categoryId));

            TransactionDate = transactionDate;
            CategoryId = categoryId;
            Amount = amount;
            Description = description;
            CreatedBy = createdBy;
            CreatedAt = DateTime.Now;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Transaction"/> class by copying another transaction.
        /// </summary>
        /// <param name="obj">The transaction object to copy.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided object is null.</exception>
        public Transaction(Transaction obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "Transaction object cannot be null.");

            Id = obj.Id;
            TransactionDate = obj.TransactionDate;
            CategoryId = obj.CategoryId;
            Amount = obj.Amount;
            Description = obj.Description;
            CreatedBy = obj.CreatedBy;
            CreatedAt = obj.CreatedAt;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a string that represents the current transaction.
        /// </summary>
        /// <returns>A formatted string with date, description, amount, and creator.</returns>
        public override string ToString()
        {
            return $"{TransactionDate:yyyy-MM-dd} - {Description}: ${Amount} (by {CreatedBy})";
        }

        #endregion
    }
}
