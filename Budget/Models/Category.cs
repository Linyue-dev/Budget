using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Budget.Models
{
    public class Category
    {
        #region property
        public int Id { get; set; }
        public string Name { get; set; }
        public CategoryType Type { get; set; }
        #endregion

        #region CategoryType enum (The enumeration value corresponds directly to the database Id) 
        public enum CategoryType
        {
            Income = 1,
            Expense = 2,
            Debt = 3,   
            Investment = 4,
            Savings = 5,
        };
        #endregion

        #region constructor 
        public Category(int id, string name, CategoryType type = CategoryType.Expense)
        {
            Id = id;
            Name = name;
            Type = type;
        }

        public Category(Category category)
        {
            Id = category.Id;;
            Name = category.Name;
            Type = category.Type;
        }
        #endregion
        public override string ToString()
        {
            return Name;
        }
    }
}

