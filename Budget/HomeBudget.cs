using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Dynamic;
using Budget.Utils;
using Budget.Services;
using Budget.Models;
using static Budget.Models.Category;

namespace Budget
{

    public class HomeBudget
    {
        private string _FileName;
        private string _DirName;
        private Categories _categories;
        private Transactions _transactions;

        public String FileName { get { return _FileName; } }
        public String DirName { get { return _DirName; } }
        public String PathName
        {
            get
            {
                if (_FileName != null && _DirName != null)
                {
                    return Path.GetFullPath(_DirName + "\\" + _FileName);
                }
                else
                {
                    return null;
                }
            }
        }
        public Categories categories { get { return _categories; } }
        public Transactions transactions { get { return _transactions; } }
        public HomeBudget()
        {
            _categories = new Categories();
            _transactions = new Transactions();
        }

        public HomeBudget(String budgetFileName)
        {
            _categories = new Categories();
            _transactions = new Transactions();
            ReadFromFile(budgetFileName);
        }

        #region OpenNewAndSave
        public void ReadFromFile(String budgetFileName)
        {

            // read the budget file and process
            try
            {
                // get filepath name (throws exception if it doesn't exist)
                budgetFileName = BudgetFiles.VerifyReadFromFileName(budgetFileName, "");

                // If file exists, read it
                string[] filenames = System.IO.File.ReadAllLines(budgetFileName);

                // Save information about budget file
                string folder = Path.GetDirectoryName(budgetFileName);
                _FileName = Path.GetFileName(budgetFileName);

                // read the expenses and categories from their respective files
                _categories.ReadFromFile(folder + "\\" + filenames[0]);
                _transactions.ReadFromFile(folder + "\\" + filenames[1]);

                // Save information about budget file
                _DirName = Path.GetDirectoryName(budgetFileName);
                _FileName = Path.GetFileName(budgetFileName);

            }
            // throw new exception if we cannot get the info that we need
            catch (Exception e)
            {
                throw new Exception("Could not read budget info: \n" + e.Message);
            }

        }
        public void SaveToFile(String filepath)
        {


            // just in case filepath doesn't exist, reset path info
            _DirName = null;
            _FileName = null;

            // get filepath name (throws exception if we can't write to the file)
             filepath = BudgetFiles.VerifyWriteToFileName(filepath, "");

            String path = Path.GetDirectoryName(Path.GetFullPath(filepath));
            String file = Path.GetFileNameWithoutExtension(filepath);
            String ext = Path.GetExtension(filepath);

            // construct file names for expenses and categories
            String expensepath = path + "\\" + file + "_transactions" + ".txns";
            String categorypath = path + "\\" + file + "_categories" + ".cats";

            // save the expenses and categories into their own files
            _transactions.SaveToFile(expensepath);
            _categories.SaveToFile(categorypath);

            // save filenames of expenses and categories to budget file
            string[] files = { Path.GetFileName(categorypath), Path.GetFileName(expensepath) };
            System.IO.File.WriteAllLines(filepath, files);

            // save filename info for later use
            _DirName = path;
            _FileName = Path.GetFileName(filepath);
        }
        #endregion OpenNewAndSave

        #region GetList


        public List<BudgetItem> GetBudgetItems(DateTime? Start, DateTime? End, bool FilterFlag, int CategoryID)
        {

            Start = Start ?? new DateTime(1900, 1, 1);
            End = End ?? new DateTime(2500, 1, 1);

            var query = from c in _categories.List()
                        join t in _transactions.List() on c.Id equals t.Category
                        where t.Date >= Start && t.Date <= End
                        select new { CatId = c.Id, TxnsId = t.Id, t.Date, CategoryType = c.Type, Category = c.Name, t.Description, t.Amount };

   
            List<BudgetItem> items = new List<BudgetItem>();
            decimal total = 0;

            foreach (var t in query.OrderBy(q => q.Date))
            {
                if (FilterFlag && CategoryID != t.CatId)
                {
                    continue;
                }

                // The impact on the balance depends on the classification type.
                if (t.CategoryType == CategoryType.Income)
                {
                    total = total + t.Amount;  // Increase in income balance
                }
                else
                {
                    total = total - t.Amount;  // Expenditure/Investment/Savings/Debt Reduction Balance
                }

                items.Add(new BudgetItem
                {
                    CategoryID = t.CatId,
                    TransactionID = t.TxnsId,
                    ShortDescription = t.Description,
                    Date = t.Date,
                    // Income is shown as a positive number, while other items are shown as a negative number.
                    Category = t.Category,
                    Amount = t.CategoryType == CategoryType.Income ? t.Amount : -t.Amount, 
                    Balance = total
                });
            }
            return items;
        }

        public List<BudgetItemsByMonth> GetBudgetItemsByMonth(DateTime? Start, DateTime? End, bool FilterFlag, int CategoryID)
        {       
            List<BudgetItem> items = GetBudgetItems(Start, End, FilterFlag, CategoryID);

            var GroupedByMonth = items.GroupBy(c => c.Date.Year.ToString("D4") + "/" + c.Date.Month.ToString("D2"));

            var summary = new List<BudgetItemsByMonth>();
            foreach (var MonthGroup in GroupedByMonth)
            {
                // calculate total for this month, and create list of details
                decimal total = 0;
                var details = new List<BudgetItem>();
                foreach (var item in MonthGroup)
                {
                    total = total + item.Amount;
                    details.Add(item);
                }

                // Add new BudgetItemsByMonth to our list
                summary.Add(new BudgetItemsByMonth
                {
                    Month = MonthGroup.Key,
                    Details = details,
                    Total = total
                });
            }

            return summary;
        }

        public List<BudgetItemsByCategory> GetBudgetItemsByCategory(DateTime? Start, DateTime? End, bool FilterFlag, int CategoryID)
        {

            List<BudgetItem> items = GetBudgetItems(Start, End, FilterFlag, CategoryID);

            var GroupedByCategory = items.GroupBy(c => c.Category);

            var summary = new List<BudgetItemsByCategory>();
            foreach (var CategoryGroup in GroupedByCategory.OrderBy(g => g.Key))
            {
                // calculate total for this category, and create list of details
                decimal total = 0;
                var details = new List<BudgetItem>();
                foreach (var item in CategoryGroup)
                {
                    total = total + item.Amount;
                    details.Add(item);
                }

                // Add new BudgetItemsByCategory to our list
                summary.Add(new BudgetItemsByCategory
                {
                    Category = CategoryGroup.Key,
                    Details = details,
                    Total = total
                });
            }

            return summary;
        }

        public List<Dictionary<string,object>> GetBudgetDictionaryByCategoryAndMonth(DateTime? Start, DateTime? End, bool FilterFlag, int CategoryID)
        {
            List<BudgetItemsByMonth> GroupedByMonth = GetBudgetItemsByMonth(Start, End, FilterFlag, CategoryID);

            var summary = new List<Dictionary<string, object>>();
            var totalsPerCategory = new Dictionary<String, decimal>();

            foreach (var MonthGroup in GroupedByMonth)
            {
                // create record object for this month
                Dictionary<string, object> record = new Dictionary<string, object>();
                record["Month"] = MonthGroup.Month;
                record["Total"] = MonthGroup.Total;

                // break up the month details into categories
                var GroupedByCategory = MonthGroup.Details.GroupBy(c => c.Category);

                foreach (var CategoryGroup in GroupedByCategory.OrderBy(g => g.Key))
                {
                    // calculate totals for the cat/month, and create list of details
                    decimal total = 0;
                    var details = new List<BudgetItem>();

                    foreach (var item in CategoryGroup)
                    {
                        total = total + item.Amount;
                        details.Add(item);
                    }

                    // add new properties and values to our record object
                    record["details:" + CategoryGroup.Key] =  details;
                    record[CategoryGroup.Key] = total;

                    // keep track of totals for each category
                    if (totalsPerCategory.TryGetValue(CategoryGroup.Key, out decimal CurrentCatTotal))
                    {
                        totalsPerCategory[CategoryGroup.Key] = CurrentCatTotal + total;
                    }
                    else
                    {
                        totalsPerCategory[CategoryGroup.Key] = total;
                    }
                }
                // add record to collection
                summary.Add(record);
            }

            Dictionary<string, object> totalsRecord = new Dictionary<string, object>();
            totalsRecord["Month"] = "TOTALS";

            foreach (var cat in categories.List())
            {
                try
                {
                    totalsRecord.Add(cat.Name, totalsPerCategory[cat.Name]);
                }
                catch { }
            }
            summary.Add(totalsRecord);

            return summary;
        }
        #endregion GetList
    }
}
