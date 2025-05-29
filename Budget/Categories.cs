using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;


namespace Budget
{
    public class Categories
    {
        private static String DefaultFileName = "budgetCategories.txt";
        private List<Category> _Cats = new List<Category>();
        private string _FileName;
        private string _DirName;


        public String FileName { get { return _FileName; } }
        public String DirName { get { return _DirName; } }


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


        public void ReadFromFile(String filepath = null)
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
        public void SaveToFile(String filepath = null)
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
            Add("Utilities", Category.CategoryType.Expense);
            Add("Rent", Category.CategoryType.Expense);
            Add("Food", Category.CategoryType.Expense);
            Add("Entertainment", Category.CategoryType.Expense);
            Add("Education", Category.CategoryType.Expense);
            Add("Miscellaneous", Category.CategoryType.Expense);
            Add("Medical Expenses", Category.CategoryType.Expense);
            Add("Vacation", Category.CategoryType.Expense);
            Add("Credit Card", Category.CategoryType.Credit);
            Add("Clothes", Category.CategoryType.Expense);
            Add("Gifts", Category.CategoryType.Expense);
            Add("Insurance", Category.CategoryType.Expense);
            Add("Transportation", Category.CategoryType.Expense);
            Add("Eating Out", Category.CategoryType.Expense);
            Add("Savings", Category.CategoryType.Savings);
            Add("Income", Category.CategoryType.Income);

        }


        private void Add(Category cat)
        {
            _Cats.Add(cat);
        }

        public void Add(String desc, Category.CategoryType type)
        {
            int new_num = 1;
            if (_Cats.Count > 0)
            {
                new_num = (from c in _Cats select c.Id).Max();
                new_num++;
            }
            _Cats.Add(new Category(new_num, desc, type));
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


        private void _ReadXMLFile(String filepath)
        {


            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filepath);

                foreach (XmlNode category in doc.DocumentElement.ChildNodes)
                {
                    String id = (((XmlElement)category).GetAttributeNode("ID")).InnerText;
                    String typestring = (((XmlElement)category).GetAttributeNode("type")).InnerText;
                    String desc = ((XmlElement)category).InnerText;

                    Category.CategoryType type;
                    switch (typestring.ToLower())
                    {
                        case "income":
                            type = Category.CategoryType.Income;
                            break;
                        case "expense":
                            type = Category.CategoryType.Expense;
                            break;
                        case "credit":
                            type = Category.CategoryType.Credit;
                            break;
                        default:
                            type = Category.CategoryType.Expense;
                            break;
                    }
                    this.Add(new Category(int.Parse(id), desc, type));
                }

            }
            catch (Exception e)
            {
                throw new Exception("ReadXMLFile: Reading XML " + e.Message);
            }

        }

        private void _WriteXMLFile(String filepath)
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

                    XmlText text = doc.CreateTextNode(cat.Description);
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

