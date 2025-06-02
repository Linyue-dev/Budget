using Budget.Models;

namespace Budget
{
    // Use struct to process parameters
    struct ReportParameter
    {
        public DateTime? Start;
        public DateTime? End;
        public bool FilterFlag;
        public int CategoryID;

        public ReportParameter(DateTime? start, DateTime? end, bool filter, int id)
        {
            Start = start;
            End = end;
            FilterFlag = filter;
            CategoryID = id;
        }
    }
    internal class Program
    {
        static HomeBudget homeBudget;
        public static void Main(string[] args)
        {
            InitializeHomeBudget();
            bool isOnging = true;

            while (isOnging)
            {
                try
                {
                    Console.Clear();
                    ReportParameter parameter = GetParameter();
                    DisplayReportType();
                    string userInput = Console.ReadLine();

                    switch (userInput)
                    {
                        case "1":
                            Console.Clear();
                            PrintBudgetItem(parameter);
                            isOnging = IsQuit();
                            break;
                        case "2":
                            Console.Clear();
                            PrintBudgetItemsByMonth(parameter);
                            isOnging = IsQuit();
                            break;
                        case "3":
                            Console.Clear();
                            PrintBudgetItemsByCategory(parameter);
                            isOnging = IsQuit();
                            break;
                        case "4":
                            Console.Clear();
                            PrintBudgetDictionaryByCategoryAndMonth(parameter);
                            isOnging = IsQuit();
                            break;
                        case "5":
                            isOnging = false;
                            Console.WriteLine("Exiting program...");
                            break;
                        default:
                            Console.WriteLine("Invalid option. Please try again.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        #region Prepare method
        public static void DisplayReportType()
        {
            Console.WriteLine("\nSelect Report Type:\n" +
                            "\t1. Report Budget Items\n" +
                            "\t2. Report Budget Items by Month\n" +
                            "\t3. Report Budget Items by Category\n" +
                            "\t4. Report Budget by Category and Month\n" +
                            "\t5. Quit\n");
            Console.Write("Enter your choice: ");
        }
        public static void InitializeHomeBudget()
        {
            try
            {
                string budgetFileName = @"./Data/test.budget";
                //string budgetFileName = Path.Combine("Data", "test.budget");
                homeBudget = new HomeBudget(budgetFileName);
                Console.WriteLine("Budget file loaded successfully!");
                //homeBudget.SaveToFile(budgetFileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing budget: {ex.Message}");
                throw;
            }
        }
        public static ReportParameter GetParameter()
        {
            Console.WriteLine("\nEnter report parameters:\n");

            DateTime? start = GetDateInput("Start time (MM/DD/YYYY): ");

            DateTime? end = GetDateInput("End time (MM/DD/YYYY): ");

            Console.Write("Filter by category? (y/n): ");

            string filterInput = GetFilterFlagInput();

            bool filterFlag = false;

            int categoryId = -1;

            if (filterInput == "y")

                filterFlag = true;

            if (filterFlag)
            {
                Console.WriteLine("\nAvailable Categories:");

                foreach (var category in homeBudget.categories.List())
                {
                    Console.WriteLine($"category id {category.Id},category description {category.Name}");
                }
                Console.Write("Enter category ID: ");

                while (!int.TryParse(Console.ReadLine(), out categoryId))
                {
                    Console.Write("Invalid ID. Please enter a number: ");
                }
            }
            ReportParameter parameter = new ReportParameter(start, end, filterFlag, categoryId);

            return parameter;
        }

        public static DateTime? GetDateInput(String message)
        {
            Console.Write(message);
            string input = Console.ReadLine();

            if (string.IsNullOrEmpty(input))
                return null;

            while (!DateTime.TryParse(input, out DateTime result))
            {
                Console.Write("Invalid date format. Please try again (MM/DD/YYYY): ");
                input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                    return null;
            }
            return DateTime.Parse(input);
        }

        public static string GetFilterFlagInput()
        {
            string filterInput = Console.ReadLine().ToLower();
            while (string.IsNullOrEmpty(filterInput) || (filterInput != "n" && filterInput != "y"))
            {
                Console.Write("Filter by category? (y/n): ");
                filterInput = Console.ReadLine();
            }
            return filterInput;
        }
        #endregion

        #region Print Report method
        public static void PrintBudgetItem(ReportParameter parameter)
        {
            List<BudgetItem> budgetItems = homeBudget.GetBudgetItems(parameter.Start, parameter.End, parameter.FilterFlag, parameter.CategoryID);

            // if budgetItems is empty list, console a feedback to user
            if (budgetItems == null || budgetItems.Count == 0)
            {
                //throw new Exception("There is no eligible information");
                Console.WriteLine("There is no eligible information");
            }
            else
            {
                Console.WriteLine("\n\t\t\tReport Budget Items");
                Console.WriteLine($"{"Date",-15}{"Description",-25}{"Amount",15}{"Balance",15}");
                Console.WriteLine(new string('-', 70));
                foreach (BudgetItem budgetItem in budgetItems)
                {
                    Console.WriteLine($"{budgetItem.Date.ToShortDateString(),-15} " +
                                      $"{budgetItem.ShortDescription,-25}" +
                                      $"{budgetItem.Amount,15:C2} " +
                                      $"{budgetItem.Balance,15:C2}");
                }
                Console.WriteLine(new string('-', 70));
            }
        }
        public static void PrintBudgetItemsByMonth(ReportParameter parameter)
        {
            List<BudgetItemsByMonth> budgetItemsByMonths = homeBudget.GetBudgetItemsByMonth(parameter.Start, parameter.End, parameter.FilterFlag, parameter.CategoryID);

            // if budgetItems is empty list, console a feedback to user
            if (budgetItemsByMonths == null || budgetItemsByMonths.Count == 0)
            {
                //throw new Exception("There is no eligible information");
                Console.WriteLine("There is no eligible information");
            }
            else
            {
                Console.WriteLine("\n\tReport Budget Items by Month");

                foreach (BudgetItemsByMonth budgetItemByMonth in budgetItemsByMonths)
                {
                    Console.WriteLine($"\nMonth: {budgetItemByMonth.Month,0}");
                    Console.WriteLine($"{"Date",-12} {"Description",-30} {"Amount",10}");
                    Console.WriteLine(new string('-', 54));
                    foreach (BudgetItem budgetItem in budgetItemByMonth.Details)
                    {
                        Console.WriteLine($"{budgetItem.Date.ToShortDateString(),-12} " +
                                          $"{budgetItem.ShortDescription,-30} " +
                                          $"{budgetItem.Amount,10:C2}");
                    }
                    Console.WriteLine(new string('-', 54));
                    Console.WriteLine($"{"Month Total:",-43} {budgetItemByMonth.Total,10:C2}");
                }
            }
        }
        public static void PrintBudgetItemsByCategory(ReportParameter parameter)
        {
            List<BudgetItemsByCategory> budgetItemsByCategories = homeBudget.GetBudgetItemsByCategory(parameter.Start, parameter.End, parameter.FilterFlag, parameter.CategoryID);
            // if budgetItems is empty list, console a feedback to user
            if (budgetItemsByCategories == null || budgetItemsByCategories.Count == 0)
            {
                //throw new Exception("There is no eligible information");
                Console.WriteLine("There is no eligible information");
            }
            else
            {
                Console.WriteLine("\n\tReport Budget Items by Category");
                foreach (BudgetItemsByCategory budgetItemByCategory in budgetItemsByCategories)
                {
                    Console.WriteLine($"\nCategory: {budgetItemByCategory.Category,-10}");
                    Console.WriteLine($"{"Date",-12} {"Description",-30} {"Amount",10}");
                    Console.WriteLine(new string('-', 54));
                    foreach (BudgetItem budgetItem in budgetItemByCategory.Details)
                    {
                        Console.WriteLine($"{budgetItem.Date.ToShortDateString(),-12} " +
                                          $"{budgetItem.ShortDescription,-30} " +
                                          $"{budgetItem.Amount,10:C2}");
                    }
                    Console.WriteLine(new string('-', 54));
                    Console.WriteLine($"{"Category Total:",-43} {budgetItemByCategory.Total,10:C2}");
                }
            }
        }
        public static void PrintBudgetDictionaryByCategoryAndMonth(ReportParameter parameter)
        {
            List<Dictionary<string, object>> summarry = homeBudget.GetBudgetDictionaryByCategoryAndMonth
                (
                    parameter.Start,
                    parameter.End,
                    parameter.FilterFlag,
                    parameter.CategoryID
                );

            Console.WriteLine("\n\tReport Budget by Category and Month\n");

            foreach (Dictionary<string, object> record in summarry)
            {
                Console.WriteLine($"Month: {record["Month"]}");

                if (record["Month"].ToString() != "TOTALS")
                {
                    Console.WriteLine($"Monthly Total: {record["Total"]:C2}\n");

                    foreach (string key in record.Keys)
                    {
                        if (key.StartsWith("details:"))
                        {
                            //remove prefix "details:"
                            string category = key.Substring(8);
                            Console.WriteLine($"Category: {category}");
                            Console.WriteLine($"{"Date",-16} {"Description",-25} {"Amount",-10}");
                            Console.WriteLine(new string('-', 54));

                            List<BudgetItem> details = (List<BudgetItem>)record[key];

                            foreach (BudgetItem budgetItem in details)
                            {
                                Console.WriteLine($"{budgetItem.Date.ToShortDateString(),-16}" +
                                                  $"{budgetItem.ShortDescription,-25}" +
                                                  $"{budgetItem.Amount,-10:C2}");
                            }
                            Console.WriteLine(new string('-', 54));
                            Console.WriteLine($"Category Total: {record[category]}\n");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Category Total:");
                    Console.WriteLine(new string('-', 54));
                    foreach (string key in record.Keys)
                    {
                        if (key != "Month")
                        {
                            Console.WriteLine($"{key,-38} {record[key],10:C2}");
                        }
                    }
                    Console.WriteLine(new string('-', 54));
                }
            }
        }
        public static bool IsQuit()
        {
            bool isQuit = true;
            Console.WriteLine("y -> quit\nany key -> continue");
            string userInput = Console.ReadLine().ToLower();
            if (userInput == "y")
            {
                isQuit = false;
            }
            return isQuit;
        }
        #endregion
    }
}
