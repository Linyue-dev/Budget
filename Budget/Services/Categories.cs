using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using Budget.Models;
using Budget.Utils;
using static Budget.Models.Category;


namespace Budget.Services
{
    public class Categories
    {
        private static string DefaultFileName = "budgetCategories.txt";
        private List<Category> _Cats = new List<Category>();
        private string _FileName;
        private string _DirName;



        #region property        
        public string FileName { get { return _FileName; } }
        public string DirName { get { return _DirName; } }
        #endregion

        #region contructor
        public Categories()
        {
            SetCategoriesToDefaults();
        }


        public Category GetCategoryFromId(int i)
        {
            Category c = _Cats.Find(x => x.Id == i);
            if (c == null)
            {
                throw new Exception("Cannot find category with id " + i.ToString());
            }
            return c;
        }
        #endregion


        public void ReadFromFile(string filepath = null)
        {

            // reading from file resets all the current categories,
            _Cats.Clear();

            // reset default dir/filename to null 
            // ... filepath may not be valid, 
            _DirName = null;
            _FileName = null;


            filepath = BudgetFiles.VerifyReadFromFileName(filepath, DefaultFileName);

            // If file exists, read it
            _ReadXMLFile(filepath);
            _DirName = Path.GetDirectoryName(filepath);
            _FileName = Path.GetFileName(filepath);
        }
        public void SaveToFile(string filepath = null)
        {

            // if file path not specified, set to last read file
            if (filepath == null && DirName != null && FileName != null)
            {
                filepath = DirName + "\\" + FileName;
            }

            // just in case filepath doesn't exist, reset path info
            _DirName = null;
            _FileName = null;

            // get filepath name (throws exception if it doesn't exist)
            filepath = BudgetFiles.VerifyWriteToFileName(filepath, DefaultFileName);

            // save as XML
            _WriteXMLFile(filepath);

            // save filename info for later use
            _DirName = Path.GetDirectoryName(filepath);
            _FileName = Path.GetFileName(filepath);
        }

        public void SetCategoriesToDefaults()
        {
            // reset any current categories,
            _Cats.Clear();

            // Add Defaults
            Add("Utilities", CategoryType.Expense);
            Add("Food & Dining", CategoryType.Expense);
            Add("Transportation", CategoryType.Expense);
            Add("Health & Personal Care", CategoryType.Expense);
            Add("Insurance", CategoryType.Expense);
            Add("Clothes", CategoryType.Expense);
            Add("Education", CategoryType.Expense);
            Add("Vacation", CategoryType.Expense);
            Add("Social Expenses", CategoryType.Expense);
            Add("Municipal & SchoolTax", CategoryType.Expense);
            Add("Rental Expenses", CategoryType.Expense);
            Add("Miscellaneous", CategoryType.Expense);
            Add("Savings", CategoryType.Savings);
            Add("Housing mortgage", CategoryType.Debt);
            Add("Auto loan", CategoryType.Debt);
            Add("Salary", CategoryType.Income);
            Add("Rental Income", CategoryType.Income);
            Add("Stock & Fund", CategoryType.Investment);

        }

        private void Add(Category cat)
        {
            _Cats.Add(cat);
        }

        public void Add(string name, Category.CategoryType type)
        {
            int new_num = 1;
            if (_Cats.Count > 0)
            {
                new_num = (from c in _Cats select c.Id).Max();
                new_num++;
            }
            _Cats.Add(new Category(new_num, name, type));
        }


        public void Delete(int Id)
        {
            int i = _Cats.FindIndex(x => x.Id == Id);
            _Cats.RemoveAt(i);
        }

        public List<Category> List()
        {
            List<Category> newList = new List<Category>();
            foreach (Category category in _Cats)
            {
                newList.Add(new Category(category));
            }
            return newList;
        }


        private void _ReadXMLFile(string filepath)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filepath);

                foreach (XmlNode category in doc.DocumentElement.ChildNodes)
                {
                    string id = ((XmlElement)category).GetAttributeNode("ID").InnerText;
                    string typestring = ((XmlElement)category).GetAttributeNode("type").InnerText;
                    string desc = ((XmlElement)category).InnerText;

                    Category.CategoryType type;
                    switch (typestring.ToLower())
                    {
                        case "income":
                            type = Category.CategoryType.Income;
                            break;
                        case "expense":
                            type = Category.CategoryType.Expense;
                            break;
                        case "debt":
                            type = CategoryType.Debt;
                            break;
                        case "investment":
                            type = CategoryType.Investment;
                            break;
                        case "savings":
                            type = CategoryType.Savings;
                            break;
                        default:
                            type = CategoryType.Expense;
                            break;
                    }
                    Add(new Category(int.Parse(id), desc, type));
                }

            }
            catch (Exception e)
            {
                throw new Exception("ReadXMLFile: Reading XML " + e.Message);
            }

        }

        private void _WriteXMLFile(string filepath)
        {
            try
            {
                // create top level element of categories
                XmlDocument doc = new XmlDocument();
                doc.LoadXml("<Categories></Categories>");

                // foreach Category, create an new xml element
                foreach (Category cat in _Cats)
                {
                    XmlElement ele = doc.CreateElement("Category");
                    XmlAttribute attr = doc.CreateAttribute("ID");
                    attr.Value = cat.Id.ToString();
                    ele.SetAttributeNode(attr);
                    XmlAttribute type = doc.CreateAttribute("type");
                    type.Value = cat.Type.ToString();
                    ele.SetAttributeNode(type);

                    XmlText text = doc.CreateTextNode(cat.Name);
                    doc.DocumentElement.AppendChild(ele);
                    doc.DocumentElement.LastChild.AppendChild(text);

                }

                // write the xml to FilePath
                doc.Save(filepath);

            }
            catch (Exception e)
            {
                throw new Exception("_WriteXMLFile: Reading XML " + e.Message);
            }
        }
    }
}

