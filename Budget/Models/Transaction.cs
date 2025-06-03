using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Budget.Models
{
    public class Transaction
    {
        #region property
        public int Id { get; }
        public DateTime Date { get; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }

        #endregion

        #region constructor
        public Transaction(int id, DateTime date, string description, decimal amount, int categoryId)
        {
            if (id <= 0)
            {
                throw new ArgumentException("ID must be greater than zero.", nameof(id));
            }
            if (categoryId <= 0)
            {
                throw new ArgumentException("Category ID must be greater than zero.", nameof(categoryId));
            }

            Id = id;
            Date = date;
            Description = description;
            Amount = amount;
            CategoryId = categoryId;
        }

        public Transaction(Transaction obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj), "Transaction object cannot be null.");
            }

            Id = obj.Id;
            Date = obj.Date;
            CategoryId = obj.CategoryId;
            Amount = obj.Amount;
            Description = obj.Description;
        }
        #endregion
        public override string ToString()
        {
            return $"{Date:yyyy-MM-dd} - {Description}: ${Amount}";
        }
    }    
}
