using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Budget
{

    public class BudgetItem
    {
        public int CategoryID { get; set; }
        public int TransactionID { get; set; }
        public DateTime Date { get; set; }
        public string? Category { get; set; }
        public string? ShortDescription { get; set; }
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }

    }

    public class BudgetItemsByMonth
    {
        public string? Month { get; set; }
        public List<BudgetItem>? Details { get; set; }
        public decimal Total { get; set; }
    }


    public class BudgetItemsByCategory
    {
        public string? Category { get; set; }
        public List<BudgetItem>? Details { get; set; }
        public decimal Total { get; set; }

    }


}
