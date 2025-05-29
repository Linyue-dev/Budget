using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Budget
{
    public class Transaction
    {
        public int Id { get; }
        public DateTime Date { get; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public int Category { get; set; }

        public Transaction(int id, DateTime date, int category, decimal amount, string description)
        {
            if (id <= 0)
            {
                throw new ArgumentException("ID must be greater than zero.", nameof(id));
            }
            if (category <= 0)
            {
                throw new ArgumentException("Category ID must be greater than zero.", nameof(category));
            }

            Id = id;
            Date = date;
            Category = category;
            Amount = amount;
            Description = description;
        }

        public Transaction(Transaction obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj), "Transaction object cannot be null.");
            }

            Id = obj.Id;
            Date = obj.Date;
            Category = obj.Category;
            Amount = obj.Amount;
            Description = obj.Description;
        }
        /// <summary>
        /// Returns a string representation of the transaction.
        /// </summary>
        public override string ToString()
        {
            return $"{Date:yyyy-MM-dd} - {Description}: ${Amount}";
        }
    }    
}
