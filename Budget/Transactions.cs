using Budget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Budget
{
    public class Transactions
    {
        private static string DefaultFileName = "budget.txt";
        private List<Transaction> _Transactions = new List<Transaction>();
        private string _FileName;
        private string _DirName;

        #region property
        public string FileName { get { return _FileName; } }
        public string DirName { get { return _DirName; } }
        #endregion

        public void ReadFromFile(string filepath = null)
        {
            _Transactions.Clear();


            _DirName = null;
            _FileName = null;

            filepath = BudgetFiles.VerifyReadFromFileName(filepath, DefaultFileName);


            _ReadXMLFile(filepath);

            _DirName = Path.GetDirectoryName(filepath);
            _FileName = Path.GetFileName(filepath);


        }


        public void SaveToFile(string filepath = null)
        {

            if (filepath == null && DirName != null && FileName != null)
            {
                filepath = DirName + "\\" + FileName;
            }

            _DirName = null;
            _FileName = null;


            filepath = BudgetFiles.VerifyWriteToFileName(filepath, DefaultFileName);


            _WriteXMLFile(filepath);


            _DirName = Path.GetDirectoryName(filepath);
            _FileName = Path.GetFileName(filepath);
        }


        private void Add(Transaction trans)
        {
            _Transactions.Add(trans);
        }

        public void Add(DateTime date, int category, decimal amount, string description)
        {
            int new_id = 1;

            // if we already have expenses, set ID to max
            if (_Transactions.Count > 0)
            {
                new_id = (from e in _Transactions select e.Id).Max();
                new_id++;
            }

            _Transactions.Add(new Transaction(new_id, date, category, amount, description));

        }

        public void Delete(int Id)
        {
            int i = _Transactions.FindIndex(x => x.Id == Id);
            _Transactions.RemoveAt(i);

        }


        public List<Transaction> List()
        {
            List<Transaction> newList = new List<Transaction>();
            foreach (Transaction transaction in _Transactions)
            {
                newList.Add(new Transaction(transaction));
            }
            return newList;
        }


        private void _ReadXMLFile(string filepath)
        {


            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filepath);

                // Loop over each Expense
                foreach (XmlNode transaction in doc.DocumentElement.ChildNodes)
                {
                    // set default expense parameters
                    int id = int.Parse(((XmlElement)transaction).GetAttributeNode("ID").InnerText);
                    string description = "";
                    DateTime date = DateTime.Parse("2000-01-01");
                    int category = 0;
                    decimal amount = 0;

                    // get expense parameters
                    foreach (XmlNode info in transaction.ChildNodes)
                    {
                        switch (info.Name)
                        {
                            case "Date":
                                date = DateTime.Parse(info.InnerText);
                                break;
                            case "Amount":
                                amount = decimal.Parse(info.InnerText);
                                break;
                            case "Description":
                                description = info.InnerText;
                                break;
                            case "Category":
                                category = int.Parse(info.InnerText);
                                break;
                        }
                    }

                    // have all info for expense, so create new one
                    Add(new Transaction(id, date, category, amount, description));

                }

            }
            catch (Exception e)
            {
                throw new Exception("ReadFromFileException: Reading XML " + e.Message);
            }
        }


        private void _WriteXMLFile(string filepath)
        {

            try
            {
                // create top level element of expenses
                XmlDocument doc = new XmlDocument();
                doc.LoadXml("<Transactions></Transactions>");

                // foreach Category, create an new xml element
                foreach (Transaction trans in _Transactions)
                {
                    // main element 'Expense' with attribute ID
                    XmlElement ele = doc.CreateElement("Transaction");
                    XmlAttribute attr = doc.CreateAttribute("ID");
                    attr.Value = trans.Id.ToString();
                    ele.SetAttributeNode(attr);
                    doc.DocumentElement.AppendChild(ele);

                    // child attributes (date, description, amount, category)
                    XmlElement d = doc.CreateElement("Date");
                    XmlText dText = doc.CreateTextNode(trans.Date.ToString("M/dd/yyyy hh:mm:ss tt"));
                    ele.AppendChild(d);
                    d.AppendChild(dText);

                    XmlElement de = doc.CreateElement("Description");
                    XmlText deText = doc.CreateTextNode(trans.Description);
                    ele.AppendChild(de);
                    de.AppendChild(deText);

                    XmlElement a = doc.CreateElement("Amount");
                    XmlText aText = doc.CreateTextNode(trans.Amount.ToString());
                    ele.AppendChild(a);
                    a.AppendChild(aText);

                    XmlElement c = doc.CreateElement("Category");
                    XmlText cText = doc.CreateTextNode(trans.Category.ToString());
                    ele.AppendChild(c);
                    c.AppendChild(cText);

                }

                // write the xml to FilePath
                doc.Save(filepath);

            }
            catch (Exception e)
            {
                throw new Exception("SaveToFileException: Reading XML " + e.Message);
            }
        }
    }
}
