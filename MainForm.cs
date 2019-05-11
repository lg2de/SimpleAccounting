using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Buchhaltung
{
    public partial class MainForm : Form
    {
        XmlDocument m_Document;
        bool m_bDocumentChanged = false;
        string m_strFileName = "";

        string m_strBookingYearName = "";
        DateTime m_dateBookDate;
        int m_nBookNumber;
        struct BookEntry { public int Account; public int Value; public string Text; };
        List<BookEntry> DebitEntries = new List<BookEntry>();
        List<BookEntry> CreditEntries = new List<BookEntry>();

        void SetNodeAttribute(XmlNode node, string strName, string strValue)
        {
            XmlAttribute attr = node.OwnerDocument.CreateAttribute(strName);
            attr.Value = strValue;
            node.Attributes.SetNamedItem(attr);
        }

        public MainForm()
        {
            InitializeComponent();

            m_Document = new XmlDocument();
            string strFileName = Application.ExecutablePath;
            strFileName = strFileName.Substring(0, strFileName.LastIndexOf("\\"));
            strFileName = strFileName.Substring(0, strFileName.LastIndexOf("\\"));
            strFileName = strFileName.Substring(0, strFileName.LastIndexOf("\\") + 1);
            strFileName += "AGJM.bxml";
            LoadDatabase(strFileName);
        }

        internal XmlNode GetAccountNode(int nAccountIdent)
        {
            XmlNode accoutNode = m_Document.SelectSingleNode("//Accounts/Account[@ID=" + nAccountIdent + "]");
            return accoutNode;
        }

        internal IEnumerable<string> GetAccounts()
        {
            foreach(XmlElement account in m_Document.SelectNodes("//Accounts/Account"))
            {
                yield return $"{account.GetAttribute("Name")} ({account.GetAttribute("ID")})";
            }
        }

        internal string GetAccountName(int nAccountIdent)
        {
            XmlNode accoutNode = GetAccountNode(nAccountIdent);
            return accoutNode?.Attributes.GetNamedItem("Name").Value ?? string.Empty;
        }

        internal string GetFormatedDate(string strInternalDate)
        {
            return GetFormatedDate(Convert.ToInt32(strInternalDate));
        }
        internal string GetFormatedDate(int nInternalDate)
        {
            return (nInternalDate % 100).ToString("D2") + "." + ((nInternalDate % 10000) / 100).ToString("D2") + "." + (nInternalDate / 10000).ToString("D2");
        }

        internal int GetMaxBookIdent()
        {
            XmlNode journal = m_Document.SelectSingleNode($"//Journal[@Year='{m_strBookingYearName}']");
            var ids = journal.SelectNodes("Entry/@ID");
            int maxIdent = 0;
            foreach(XmlNode id in ids)
            {
                var ident = Convert.ToInt32(id.Value);
                if (maxIdent < ident)
                {
                    maxIdent = ident;
                }
            }

            return maxIdent;
        }

        internal void SetBookDate(DateTime date)
        {
            m_dateBookDate = date;
        }
        internal void SetBookIdent(int number)
        {
            m_nBookNumber = number;
        }
        internal void AddDebitEntry(int nAccount, int nValue, string strText)
        {
            BookEntry entry = new BookEntry();
            entry.Account = nAccount;
            entry.Value = nValue;
            entry.Text = strText;
            DebitEntries.Add(entry);
        }
        internal void AddCreditEntry(int nAccount, int nValue, string strText)
        {
            BookEntry entry = new BookEntry();
            entry.Account = nAccount;
            entry.Value = nValue;
            entry.Text = strText;
            CreditEntries.Add(entry);
        }
        internal void RegisterBooking()
        {
            XmlElement newNode = m_Document.CreateElement("Entry");
            XmlAttribute attrEntry = m_Document.CreateAttribute("Date");
            attrEntry.Value = m_dateBookDate.ToString("yyyyMMdd");
            newNode.Attributes.Append(attrEntry);
            attrEntry = m_Document.CreateAttribute("ID");
            attrEntry.Value = m_nBookNumber.ToString();
            newNode.Attributes.Append(attrEntry);
            foreach (BookEntry entry in DebitEntries)
            {
                XmlElement newEntry = CreateXmlElement("Debit", entry);
                newNode.AppendChild(newEntry);
            }
            foreach (BookEntry entry in CreditEntries)
            {
                XmlElement newEntry = CreateXmlElement("Credit", entry);
                newNode.AppendChild(newEntry);
            }

            XmlNode JournalNode = m_Document.SelectSingleNode($"//Journal[@Year='{m_strBookingYearName}']");
            JournalNode.AppendChild(newNode);
            m_bDocumentChanged = true;

            DebitEntries.Clear();
            CreditEntries.Clear();

            this.RefreshJournal();
        }

        void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!m_bDocumentChanged)
                return;

            DialogResult ret = MessageBox.Show(
                "Die Datenbasis hat sich geändert.\nWollen Sie Speichern?",
                "Programm beenden",
                MessageBoxButtons.YesNoCancel);
            if (ret == DialogResult.Cancel)
                e.Cancel = true;
            else if (ret == DialogResult.Yes)
                SaveDatabase();
        }

        void MenuItemArchiveSave_Click(object sender, EventArgs e)
        {
            SaveDatabase();
        }

        void MenuItemArchiveExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        void MenuItemActionsBooking_Click(object sender, EventArgs e)
        {
            BookingDialog dlg = new BookingDialog(this);
            DialogResult ret = dlg.ShowDialog();
        }

        void MenuItemActionsSelectYear_Click(object sender, EventArgs e)
        {
            SelectBookingYear dlg = new SelectBookingYear();
            XmlNodeList years = m_Document.SelectNodes("//Years/Year");
            foreach (XmlNode year in years)
            {
                dlg.AddYear(year.Attributes.GetNamedItem("Name").Value);
            }

            dlg.CurrentYear = m_strBookingYearName;
            var result = dlg.ShowDialog();
            if (result != DialogResult.OK)
            {
                return;
            }

            SelectBookingYear(dlg.CurrentYear);
        }

        void MenuItemActionCloseYear_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Wollen Sie das Jahr " + m_strBookingYearName + " abschließen?",
                "Jahresabschluß",
                MessageBoxButtons.YesNo);
            if (result != DialogResult.Yes)
            {
                return;
            }

            XmlNode yearNode = m_Document.SelectSingleNode("//Year[@Name=" + m_strBookingYearName + "]");
            XmlAttribute attr = (XmlAttribute)yearNode.Attributes.GetNamedItem("Closed");
            if (attr != null && attr.Value == "True")
                return;
            attr = m_Document.CreateAttribute("Closed");
            attr.Value = "True";
            yearNode.Attributes.Append(attr);

            string strNewYear = (Convert.ToInt32(m_strBookingYearName) + 1).ToString();

            XmlNode newYearNode = m_Document.CreateElement("Year");
            attr = (XmlAttribute)m_Document.CreateAttribute("Name");
            attr.Value = strNewYear;
            newYearNode.Attributes.Append(attr);
            attr = (XmlAttribute)m_Document.CreateAttribute("DateStart");
            string strDateStart = (Convert.ToInt64(yearNode.Attributes.GetNamedItem("DateStart").Value) + 10000).ToString();
            attr.Value = strDateStart;
            newYearNode.Attributes.Append(attr);
            attr = (XmlAttribute)m_Document.CreateAttribute("DateEnd");
            attr.Value = (Convert.ToInt64(yearNode.Attributes.GetNamedItem("DateEnd").Value) + 10000).ToString();
            newYearNode.Attributes.Append(attr);
            yearNode.ParentNode.AppendChild(newYearNode);

            XmlNode newYearJournal = m_Document.CreateElement("Journal");
            attr = (XmlAttribute)m_Document.CreateAttribute("Year");
            attr.Value = strNewYear;
            newYearJournal.Attributes.Append(attr);
            m_Document.DocumentElement.InsertAfter(newYearJournal, m_Document.SelectSingleNode("//Journal[@Year=" + m_strBookingYearName + "]"));

            int nID = 1;

            // Assest Accounts (Bestandskonten), Credit and Debit Accounts
            XmlNodeList accountNodes = m_Document.SelectNodes("//Accounts/Account[@Type='Assest' or @Type='Credit' or @Type='Debit']");
            foreach (XmlNode accountNode in accountNodes)
            {
                string strAccount = accountNode.Attributes.GetNamedItem("ID").Value;
                XPathNavigator nav = m_Document.CreateNavigator();
                double nCredit = (double)nav.Evaluate(
                    "sum(//Journal[@Year=" + m_strBookingYearName + "]/Entry/Credit[@Account=" + strAccount + "]/@Value)");
                double nDebit = (double)nav.Evaluate(
                    "sum(//Journal[@Year=" + m_strBookingYearName + "]/Entry/Debit[@Account=" + strAccount + "]/@Value)");

                if (nCredit == 0 && nDebit == 0 || nCredit == nDebit)
                    continue;

                int nValue = 0;
                string strName1 = "";
                string strName2 = "";
                if (nCredit > nDebit)
                {
                    nValue = (int)(nCredit - nDebit);
                    strName1 = "Credit";
                    strName2 = "Debit";
                }
                else
                {
                    nValue = (int)(nDebit - nCredit);
                    strName1 = "Debit";
                    strName2 = "Credit";
                }

                XmlNode entry = m_Document.CreateElement("Entry");
                attr = m_Document.CreateAttribute("Date");
                attr.Value = strDateStart;
                entry.Attributes.Append(attr);
                attr = m_Document.CreateAttribute("ID");
                attr.Value = nID.ToString();
                entry.Attributes.Append(attr);
                attr = m_Document.CreateAttribute("Opening");
                attr.Value = "1";
                entry.Attributes.Append(attr);
                newYearJournal.AppendChild(entry);

                XmlNode value = m_Document.CreateElement(strName1);
                attr = (XmlAttribute)m_Document.CreateAttribute("Value");
                attr.Value = nValue.ToString();
                value.Attributes.Append(attr);
                attr = (XmlAttribute)m_Document.CreateAttribute("Account");
                attr.Value = strAccount;
                value.Attributes.Append(attr);
                attr = (XmlAttribute)m_Document.CreateAttribute("Text");
                attr.Value = "EB-Wert " + nID.ToString();
                value.Attributes.Append(attr);
                entry.AppendChild(value);

                value = m_Document.CreateElement(strName2);
                attr = (XmlAttribute)m_Document.CreateAttribute("Value");
                attr.Value = nValue.ToString();
                value.Attributes.Append(attr);
                attr = (XmlAttribute)m_Document.CreateAttribute("Account");
                attr.Value = "990";
                value.Attributes.Append(attr);
                attr = (XmlAttribute)m_Document.CreateAttribute("Text");
                attr.Value = "EB-Wert " + nID.ToString();
                value.Attributes.Append(attr);
                entry.AppendChild(value);

                nID++;
            }

            m_bDocumentChanged = true;
            SelectBookingYear(strNewYear);
        }

        void MenuItemReportsJournal_Click(object sender, EventArgs e)
        {
            PrintClass print = new PrintClass();
            string fileName = Application.ExecutablePath;
            fileName = fileName.Substring(0, fileName.LastIndexOf("\\"));
            fileName = fileName.Substring(0, fileName.LastIndexOf("\\"));
            fileName = fileName.Substring(0, fileName.LastIndexOf("\\") + 1);
            fileName += "Journal.xml";
            print.LoadDocument(fileName);

            XmlDocument doc = print.Document;

            XmlNode firmNode = doc.SelectSingleNode("//text[@ID=\"firm\"]");
            firmNode.InnerText = "AGJM im Bistum Dresden Meißen";

            XmlNode rangeNode = doc.SelectSingleNode("//text[@ID=\"range\"]");
            XmlNode yearNode = m_Document.SelectSingleNode("//Year[@Name=" + m_strBookingYearName + "]");
            XmlNode startNode = yearNode.Attributes.GetNamedItem("DateStart");
            XmlNode endNode = yearNode.Attributes.GetNamedItem("DateEnd");
            rangeNode.InnerText = GetFormatedDate(startNode.Value) + " - " + GetFormatedDate(endNode.Value);

            var dateNode = doc.SelectSingleNode("//text[@ID=\"date\"]");
            dateNode.InnerText = "Dresden, " + DateTime.Now.ToLongDateString();

            XmlNode dataNode = doc.SelectSingleNode("//table/data");

            var xdoc = XDocument.Parse(m_Document.OuterXml);
            var journalEntries = xdoc.XPathSelectElements("//Journal[@Year=" + m_strBookingYearName + "]/Entry");
            foreach (var entry in journalEntries.OrderBy(x => x.Attribute("Date").Value))
            {
                XmlNode dataLineNode = doc.CreateElement("tr");
                XmlNode dataItemNode = doc.CreateElement("td");
                SetNodeAttribute(dataLineNode, "topline", "1");
                dataItemNode.InnerText = GetFormatedDate(entry.Attribute("Date").Value);
                dataLineNode.AppendChild(dataItemNode);
                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = entry.Attribute("ID").Value;
                dataLineNode.AppendChild(dataItemNode);

                var debitAccounts = entry.XPathSelectElements("Debit").ToList();
                var creditAccounts = entry.XPathSelectElements("Credit").ToList();
                if (debitAccounts.Count == 1
                    && creditAccounts.Count == 1
                    && debitAccounts.First().Attribute("Text").Value == creditAccounts.First().Attribute("Text").Value)
                {
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = debitAccounts[0].Attribute("Text").Value;
                    dataLineNode.AppendChild(dataItemNode);
                    string strAccountNumber = debitAccounts[0].Attribute("Account").Value;
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = strAccountNumber;
                    dataLineNode.AppendChild(dataItemNode);
                    double nValue = Convert.ToDouble(debitAccounts[0].Attribute("Value").Value) / 100;
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = nValue.ToString("0.00");
                    dataLineNode.AppendChild(dataItemNode);
                    strAccountNumber = creditAccounts[0].Attribute("Account").Value;
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = strAccountNumber;
                    dataLineNode.AppendChild(dataItemNode);
                    nValue = Convert.ToDouble(creditAccounts[0].Attribute("Value").Value) / 100;
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = nValue.ToString("0.00");
                    dataLineNode.AppendChild(dataItemNode);
                    dataNode.AppendChild(dataLineNode);
                    continue;
                }

                foreach (var debitEntry in debitAccounts)
                {
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = debitEntry.Attribute("Text").Value;
                    dataLineNode.AppendChild(dataItemNode);
                    string strAccountNumber = debitEntry.Attribute("Account").Value;
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = strAccountNumber;
                    dataLineNode.AppendChild(dataItemNode);
                    double nValue = Convert.ToDouble(debitEntry.Attribute("Value").Value) / 100;
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = nValue.ToString("0.00");
                    dataLineNode.AppendChild(dataItemNode);
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = "";
                    dataLineNode.AppendChild(dataItemNode);
                    dataLineNode.AppendChild(dataItemNode.Clone());
                    dataNode.AppendChild(dataLineNode);

                    dataLineNode = doc.CreateElement("tr");
                    dataItemNode = doc.CreateElement("td");
                    dataLineNode.AppendChild(dataItemNode);
                    dataLineNode.AppendChild(dataItemNode.Clone());
                }

                foreach (var creditEntry in creditAccounts)
                {
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = creditEntry.Attribute("Text").Value;
                    dataLineNode.AppendChild(dataItemNode);
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = "";
                    dataLineNode.AppendChild(dataItemNode);
                    dataLineNode.AppendChild(dataItemNode.Clone());
                    string strAccountNumber = creditEntry.Attribute("Account").Value;
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = strAccountNumber;
                    dataLineNode.AppendChild(dataItemNode);
                    double nValue = Convert.ToDouble(creditEntry.Attribute("Value").Value) / 100;
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = nValue.ToString("0.00");
                    dataLineNode.AppendChild(dataItemNode);
                    dataNode.AppendChild(dataLineNode);

                    dataLineNode = doc.CreateElement("tr");
                    dataItemNode = doc.CreateElement("td");
                    dataLineNode.AppendChild(dataItemNode);
                    dataLineNode.AppendChild(dataItemNode.Clone());
                }
            }

            print.PrintDocument(DateTime.Now.ToString("yyyy-MM-dd") + " Journal " + m_strBookingYearName);
        }
        void MenuItemReportsSummary_Click(object sender, EventArgs e)
        {
            PrintClass print = new PrintClass();
            string fileName = Application.ExecutablePath;
            fileName = fileName.Substring(0, fileName.LastIndexOf("\\"));
            fileName = fileName.Substring(0, fileName.LastIndexOf("\\"));
            fileName = fileName.Substring(0, fileName.LastIndexOf("\\") + 1);
            fileName += "Saldo.xml";
            print.LoadDocument(fileName);

            XmlDocument doc = print.Document;

            XmlNode firmNode = doc.SelectSingleNode("//text[@ID=\"firm\"]");
            firmNode.InnerText = "AGJM im Bistum Dresden Meißen";

            XmlNode rangeNode = doc.SelectSingleNode("//text[@ID=\"range\"]");
            XmlNode yearNode = m_Document.SelectSingleNode("//Year[@Name=" + m_strBookingYearName + "]");
            XmlNode startNode = yearNode.Attributes.GetNamedItem("DateStart");
            XmlNode endNode = yearNode.Attributes.GetNamedItem("DateEnd");
            rangeNode.InnerText = GetFormatedDate(startNode.Value) + " - " + GetFormatedDate(endNode.Value);

            var dateNode = doc.SelectSingleNode("//text[@ID=\"date\"]");
            dateNode.InnerText = "Dresden, " + DateTime.Now.ToLongDateString();

            XmlNode dataNode = doc.SelectSingleNode("//table/data");
            XmlNodeList accountEntries = m_Document.SelectNodes("//Accounts/Account");
            XmlNode journalNode = m_Document.SelectSingleNode("//Journal[@Year=" + m_strBookingYearName + "]");

            double totalOpeningCredit = 0, totalOpeningDebit = 0;
            double totalSumSectionCredit = 0, totalSumSectionDebit = 0;
            double totalSumEndCredit = 0, totalSumEndDebit = 0;
            double totalSaldoCredit = 0, totalSaldoDebit = 0;
            foreach (XmlNode accountNode in accountEntries)
            {
                XmlNodeList journalEntries = journalNode.SelectNodes("Entry/*[@Account=" + accountNode.Attributes.GetNamedItem("ID").Value + "]");
                if (journalEntries.Count == 0)
                {
                    continue;
                }

                int lastBookingDate = 0;
                double openingCredit = 0, openingDebit = 0;
                double sumSectionCredit = 0, sumSectionDebit = 0;
                double sumEndCredit = 0, sumEndDebit = 0;
                double saldoCredit = 0, saldoDebit = 0;
                foreach (XmlNode entry in journalEntries)
                {
                    int date = Convert.ToInt32(entry.ParentNode.Attributes.GetNamedItem("Date").Value);
                    if (date > lastBookingDate)
                    {
                        lastBookingDate = date;
                    }

                    int value = Convert.ToInt32(entry.Attributes.GetNamedItem("Value").Value);

                    XmlNode node = entry.ParentNode.Attributes.GetNamedItem("Opening");
                    if (node != null && node.Value == "1")
                    {
                        if (entry.Name == "Debit")
                        {
                            openingDebit += value;
                        }
                        else
                        {
                            openingCredit += value;
                        }
                    }
                    else
                    {
                        if (true)
                        {
                            if (entry.Name == "Debit")
                            {
                                sumSectionDebit += value;
                            }
                            else
                            {
                                sumSectionCredit += value;
                            }
                        }

                        if (true)
                        {
                            if (entry.Name == "Debit")
                            {
                                sumEndDebit += value;
                            }
                            else
                            {
                                sumEndCredit += value;
                            }
                        }
                    }

                    if (entry.Name == "Debit")
                    {
                        saldoDebit += value;
                    }
                    else
                    {
                        saldoCredit += value;
                    }
                }

                if (openingCredit > openingDebit)
                {
                    openingCredit -= openingDebit;
                    openingDebit = 0;
                }
                else
                {
                    openingDebit -= openingCredit;
                    openingCredit = 0;
                }

                if (saldoCredit > saldoDebit)
                {
                    saldoCredit -= saldoDebit;
                    saldoDebit = 0;
                }
                else
                {
                    saldoDebit -= saldoCredit;
                    saldoCredit = 0;
                }

                XmlNode dataLineNode = doc.CreateElement("tr");
                XmlNode dataItemNode = doc.CreateElement("td");
                SetNodeAttribute(dataLineNode, "topline", "1");

                dataItemNode.InnerText = accountNode.Attributes.GetNamedItem("ID").Value;
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = accountNode.Attributes.GetNamedItem("Name").Value;
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = GetFormatedDate(lastBookingDate);
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = openingDebit > 0 ? (openingDebit / 100).ToString("0.00") : "";
                dataLineNode.AppendChild(dataItemNode);
                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = openingCredit > 0 ? (openingCredit / 100).ToString("0.00") : "";
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = sumSectionDebit > 0 ? (sumSectionDebit / 100).ToString("0.00") : "";
                dataLineNode.AppendChild(dataItemNode);
                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = sumSectionCredit > 0 ? (sumSectionCredit / 100).ToString("0.00") : "";
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = sumEndDebit > 0 ? (sumEndDebit / 100).ToString("0.00") : "";
                dataLineNode.AppendChild(dataItemNode);
                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = sumEndCredit > 0 ? (sumEndCredit / 100).ToString("0.00") : "";
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = saldoDebit > 0 ? (saldoDebit / 100).ToString("0.00") : "";
                dataLineNode.AppendChild(dataItemNode);
                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = saldoCredit > 0 ? (saldoCredit / 100).ToString("0.00") : "";
                dataLineNode.AppendChild(dataItemNode);

                dataNode.AppendChild(dataLineNode);

                totalOpeningCredit += openingCredit;
                totalOpeningDebit += openingDebit;
                totalSumSectionCredit += sumSectionCredit;
                totalSumSectionDebit += sumSectionDebit;
                totalSumEndCredit += sumEndCredit;
                totalSumEndDebit += sumEndDebit;
                totalSaldoCredit += saldoCredit;
                totalSaldoDebit += saldoDebit;
            }

            XmlNode totalLineNode = doc.CreateElement("tr");
            XmlNode totalItemNode = doc.CreateElement("td");
            SetNodeAttribute(totalLineNode, "topline", "1");

            totalItemNode.InnerText = "";
            totalLineNode.AppendChild(totalItemNode);

            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = "Total";
            totalLineNode.AppendChild(totalItemNode);

            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = "";
            totalLineNode.AppendChild(totalItemNode);

            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = totalOpeningDebit > 0 ? (totalOpeningDebit / 100).ToString("0.00") : "";
            totalLineNode.AppendChild(totalItemNode);
            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = totalOpeningCredit > 0 ? (totalOpeningCredit / 100).ToString("0.00") : "";
            totalLineNode.AppendChild(totalItemNode);

            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = totalSumSectionDebit > 0 ? (totalSumSectionDebit / 100).ToString("0.00") : "";
            totalLineNode.AppendChild(totalItemNode);
            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = totalSumSectionCredit > 0 ? (totalSumSectionCredit / 100).ToString("0.00") : "";
            totalLineNode.AppendChild(totalItemNode);

            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = totalSumEndDebit > 0 ? (totalSumEndDebit / 100).ToString("0.00") : "";
            totalLineNode.AppendChild(totalItemNode);
            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = totalSumEndCredit > 0 ? (totalSumEndCredit / 100).ToString("0.00") : "";
            totalLineNode.AppendChild(totalItemNode);

            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = totalSaldoDebit > 0 ? (totalSaldoDebit / 100).ToString("0.00") : "";
            totalLineNode.AppendChild(totalItemNode);
            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = totalSaldoCredit > 0 ? (totalSaldoCredit / 100).ToString("0.00") : "";
            totalLineNode.AppendChild(totalItemNode);

            dataNode.AppendChild(totalLineNode);

            print.PrintDocument(DateTime.Now.ToString("yyyy-MM-dd") + " Summen und Salden " + m_strBookingYearName);
        }

        private void MenuItemReportsBilanz_Click(object sender, EventArgs e)
        {
            PrintClass print = new PrintClass();
            string fileName = Application.ExecutablePath;
            fileName = fileName.Substring(0, fileName.LastIndexOf("\\"));
            fileName = fileName.Substring(0, fileName.LastIndexOf("\\"));
            fileName = fileName.Substring(0, fileName.LastIndexOf("\\") + 1);
            fileName += "Bilanz.xml";
            print.LoadDocument(fileName);

            XmlDocument doc = print.Document;

            var firmNode = doc.SelectSingleNode("//text[@ID=\"firm\"]");
            firmNode.InnerText = "AGJM im Bistum Dresden Meißen";

            var rangeNode = doc.SelectSingleNode("//text[@ID=\"range\"]");
            rangeNode.InnerText = m_strBookingYearName;

            var dateNode = doc.SelectSingleNode("//text[@ID=\"date\"]");
            dateNode.InnerText = "Dresden, " + DateTime.Now.ToLongDateString();

            var xdoc = XDocument.Parse(m_Document.OuterXml);
            var journal = xdoc.XPathSelectElement("//Journal[@Year=" + m_strBookingYearName + "]");

            var dataNode = doc.SelectSingleNode("//table/data[@target='income']");
            var accounts = xdoc.XPathSelectElements("//Accounts/Account[@Type='Income']");
            double totalIncome = 0;
            foreach (var account in accounts)
            {
                var journalEntries = journal.XPathSelectElements("Entry/*[@Account=" + account.Attribute("ID").Value + "]");

                int credits =
                    (from entry
                    in journalEntries
                     where entry.Name == "Credit"
                     select entry.Attribute("Value")).Sum(p => Convert.ToInt32(p.Value));
                int debits =
                    (from entry
                    in journalEntries
                     where entry.Name == "Debit"
                     select entry.Attribute("Value")).Sum(p => Convert.ToInt32(p.Value));

                double balance = credits - debits;
                if (balance == 0)
                    continue;

                totalIncome += balance;

                XmlNode dataLineNode = doc.CreateElement("tr");
                XmlNode dataItemNode = doc.CreateElement("td");

                dataLineNode.AppendChild(dataItemNode);

                string accountText = account.Attribute("ID").Value.PadLeft(5, '0') + " " + account.Attribute("Name").Value;

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = accountText;
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = (balance / 100).ToString("0.00");
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = doc.CreateElement("td");
                dataLineNode.AppendChild(dataItemNode);

                dataNode.AppendChild(dataLineNode);
            }

            var saldoElement = dataNode.SelectSingleNode("../columns/column[position()=4]");
            saldoElement.InnerText = (totalIncome / 100).ToString("0.00");

            dataNode = doc.SelectSingleNode("//table/data[@target='expense']");
            accounts = xdoc.XPathSelectElements("//Accounts/Account[@Type='Expense']");
            double totalExpense = 0;
            foreach (var account in accounts)
            {
                var journalEntries = journal.XPathSelectElements("Entry/*[@Account=" + account.Attribute("ID").Value + "]");

                int credits =
                    (from entry
                    in journalEntries
                     where entry.Name == "Credit"
                     select entry.Attribute("Value")).Sum(p => Convert.ToInt32(p.Value));
                int debits =
                    (from entry
                    in journalEntries
                     where entry.Name == "Debit"
                     select entry.Attribute("Value")).Sum(p => Convert.ToInt32(p.Value));

                double nBalance = credits - debits;
                if (nBalance == 0)
                    continue;

                totalExpense += nBalance;

                XmlNode dataLineNode = doc.CreateElement("tr");
                XmlNode dataItemNode = doc.CreateElement("td");

                dataLineNode.AppendChild(dataItemNode);

                string accountText = account.Attribute("ID").Value.PadLeft(5, '0') + " " + account.Attribute("Name").Value;

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = accountText;
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = (nBalance / 100).ToString("0.00");
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = doc.CreateElement("td");
                dataLineNode.AppendChild(dataItemNode);

                dataNode.AppendChild(dataLineNode);
            }

            saldoElement = dataNode.SelectSingleNode("../columns/column[position()=4]");
            saldoElement.InnerText = (totalExpense / 100).ToString("0.00");

            var saldoNode = doc.SelectSingleNode("//text[@ID=\"saldo\"]");
            saldoNode.InnerText = ((totalIncome + totalExpense) / 100).ToString("0.00");

            // receivables / Forderungen
            dataNode = doc.SelectSingleNode("//table/data[@target='receivable']");
            accounts = xdoc.XPathSelectElements("//Accounts/Account[@Type='Credit' or @Type='Debit']");
            double totalReceivable = 0;
            foreach (var account in accounts)
            {
                string accountType = account.Attribute("Type").Value;
                var journalEntries = journal.XPathSelectElements("Entry/*[@Account=" + account.Attribute("ID").Value + "]");

                int credits =
                    (from entry
                    in journalEntries
                     where entry.Name == "Credit"
                     select entry.Attribute("Value")).Sum(p => Convert.ToInt32(p.Value));
                int debits =
                    (from entry
                    in journalEntries
                     where entry.Name == "Debit"
                     select entry.Attribute("Value")).Sum(p => Convert.ToInt32(p.Value));

                double balance = debits - credits;
                if (balance <= 0)
                {
                    continue;
                }

                totalReceivable += balance;

                XmlNode dataLineNode = doc.CreateElement("tr");
                XmlNode dataItemNode = doc.CreateElement("td");

                dataLineNode.AppendChild(dataItemNode);

                string accountText = account.Attribute("ID").Value.PadLeft(5, '0') + " " + account.Attribute("Name").Value;

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = accountText;
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = (balance / 100).ToString("0.00");
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = doc.CreateElement("td");
                dataLineNode.AppendChild(dataItemNode);

                dataNode.AppendChild(dataLineNode);
            }

            saldoElement = dataNode.SelectSingleNode("../columns/column[position()=4]");
            saldoElement.InnerText = (totalReceivable / 100).ToString("0.00");

            // liabilities / Verbindlichkeiten
            dataNode = doc.SelectSingleNode("//table/data[@target='liability']");
            accounts = xdoc.XPathSelectElements("//Accounts/Account[@Type='Credit' or @Type='Debit']");
            double totalLiability = 0;
            foreach (var account in accounts)
            {
                string accountType = account.Attribute("Type").Value;
                var journalEntries = journal.XPathSelectElements("Entry/*[@Account=" + account.Attribute("ID").Value + "]");

                int credits =
                    (from entry
                    in journalEntries
                     where entry.Name == "Credit"
                     select entry.Attribute("Value")).Sum(p => Convert.ToInt32(p.Value));
                int debits =
                    (from entry
                    in journalEntries
                     where entry.Name == "Debit"
                     select entry.Attribute("Value")).Sum(p => Convert.ToInt32(p.Value));

                double balance = debits - credits;
                if (balance >= 0)
                {
                    continue;
                }

                totalLiability += balance;

                XmlNode dataLineNode = doc.CreateElement("tr");
                XmlNode dataItemNode = doc.CreateElement("td");

                dataLineNode.AppendChild(dataItemNode);

                string accountText = account.Attribute("ID").Value.PadLeft(5, '0') + " " + account.Attribute("Name").Value;

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = accountText;
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = (balance / 100).ToString("0.00");
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = doc.CreateElement("td");
                dataLineNode.AppendChild(dataItemNode);

                dataNode.AppendChild(dataLineNode);
            }

            saldoElement = dataNode.SelectSingleNode("../columns/column[position()=4]");
            saldoElement.InnerText = (totalLiability / 100).ToString("0.00");

            dataNode = doc.SelectSingleNode("//table/data[@target='account']");
            accounts = xdoc.XPathSelectElements("//Accounts/Account[@Type='Assest']");
            double totalAccount = 0;
            foreach (var account in accounts)
            {
                var journalEntries = journal.XPathSelectElements("Entry/*[@Account=" + account.Attribute("ID").Value + "]");

                int credits =
                    (from entry
                    in journalEntries
                     where entry.Name == "Credit"
                     select entry.Attribute("Value")).Sum(p => Convert.ToInt32(p.Value));
                int debits =
                    (from entry
                    in journalEntries
                     where entry.Name == "Debit"
                     select entry.Attribute("Value")).Sum(p => Convert.ToInt32(p.Value));

                double balance = debits - credits;
                if (balance == 0)
                    continue;

                totalAccount += balance;

                XmlNode dataLineNode = doc.CreateElement("tr");
                XmlNode dataItemNode = doc.CreateElement("td");

                dataLineNode.AppendChild(dataItemNode);

                string accountText = account.Attribute("ID").Value.PadLeft(5, '0') + " " + account.Attribute("Name").Value;

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = accountText;
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = (balance / 100).ToString("0.00");
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = doc.CreateElement("td");
                dataLineNode.AppendChild(dataItemNode);

                dataNode.AppendChild(dataLineNode);
            }

            if (totalReceivable > 0)
            {
                XmlNode dataLineNode = doc.CreateElement("tr");
                XmlNode dataItemNode = doc.CreateElement("td");

                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = "Forderungen";
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = (totalReceivable / 100).ToString("0.00");
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = doc.CreateElement("td");
                dataLineNode.AppendChild(dataItemNode);

                dataNode.AppendChild(dataLineNode);

                totalAccount += totalReceivable;
            }

            if (totalLiability < 0)
            {
                XmlNode dataLineNode = doc.CreateElement("tr");
                XmlNode dataItemNode = doc.CreateElement("td");

                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = "Verbindlichkeiten";
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = (totalLiability / 100).ToString("0.00");
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = doc.CreateElement("td");
                dataLineNode.AppendChild(dataItemNode);

                dataNode.AppendChild(dataLineNode);

                totalAccount += totalLiability;
            }

            saldoElement = dataNode.SelectSingleNode("../columns/column[position()=4]");
            saldoElement.InnerText = (totalAccount / 100).ToString("0.00");

            print.PrintDocument(DateTime.Now.ToString("yyyy-MM-dd") + " Jahresbilanz " + m_strBookingYearName);
        }

        void listViewAccounts_DoubleClick(object sender, EventArgs e)
        {
            ListViewItem item = ((ListView)sender).SelectedItems[0];
            Int32 nAccountNumber = Convert.ToInt32(item.Text);
            RefreshAccount(nAccountNumber);
        }

        XmlElement CreateXmlElement(string Type, BookEntry entry)
        {
            XmlElement newEntry = m_Document.CreateElement(Type);
            XmlAttribute attrDebit = m_Document.CreateAttribute("Value");
            attrDebit.Value = entry.Value.ToString();
            newEntry.Attributes.Append(attrDebit);
            attrDebit = m_Document.CreateAttribute("Account");
            attrDebit.Value = entry.Account.ToString();
            newEntry.Attributes.Append(attrDebit);
            attrDebit = m_Document.CreateAttribute("Text");
            attrDebit.Value = entry.Text;
            newEntry.Attributes.Append(attrDebit);
            return newEntry;
        }
        string BuildAccountDescription(string strAccountNumber)
        {
            int nAccountNumber = Convert.ToInt32(strAccountNumber);
            string strAccountName = GetAccountName(nAccountNumber);
            return strAccountNumber + " (" + strAccountName + ")";
        }

        void SelectLastBookingYear()
        {
            XmlNodeList years = m_Document.SelectNodes("//Years/Year");
            if (years.Count > 0)
                SelectBookingYear(years[years.Count - 1].Attributes.GetNamedItem("Name").Value);
        }

        void SelectBookingYear(string strYearName)
        {
            m_strBookingYearName = strYearName;
            this.Text = "Buchhaltung - " + m_strFileName + " - " + m_strBookingYearName;
            RefreshJournal();
            listViewAccountJournal.Items.Clear();
        }

        void LoadDatabase(string strFileName)
        {
            m_strFileName = strFileName;
            m_Document.Load(m_strFileName);
            XmlNodeList nodes = m_Document.SelectNodes("//Accounts/Account");
            foreach (XmlNode entry in nodes)
            {
                ListViewItem item = new ListViewItem(entry.Attributes.GetNamedItem("ID").Value);
                item.SubItems.Add(entry.Attributes.GetNamedItem("Name").Value);
                listViewAccounts.Items.Add(item);
            }
            SelectLastBookingYear();
        }
        void SaveDatabase()
        {
            DateTime fileDate = File.GetLastWriteTime(m_strFileName);
            string strNewFileName = m_strFileName + "." + fileDate.ToString("yyyyMMddHHmmss");
            try
            {
                File.Move(m_strFileName, strNewFileName);
            }
            catch (FileNotFoundException)
            {
            }
            FileStream file = new FileStream(m_strFileName, FileMode.Create, FileAccess.Write);
            m_Document.Save(file);
            file.Close();
            m_bDocumentChanged = false;
        }

        void RefreshJournal()
        {
            listViewJournal.Items.Clear();
            XmlNode journal = m_Document.SelectSingleNode("//Journal[@Year=" + m_strBookingYearName + "]");
            bool bColorStatus = false;
            foreach (XmlNode entry in journal.ChildNodes)
            {
                if (entry.Name != "Entry")
                    continue;

                ListViewItem item = new ListViewItem(entry.Attributes.GetNamedItem("Date").Value);
                if (bColorStatus)
                    item.BackColor = Color.LightGreen;
                bColorStatus = !bColorStatus;

                item.SubItems.Add(entry.Attributes.GetNamedItem("ID").Value);
                XmlNodeList debitAccounts = entry.SelectNodes("Debit");
                XmlNodeList creditAccounts = entry.SelectNodes("Credit");
                if (debitAccounts.Count == 1 && creditAccounts.Count == 1)
                {
                    item.SubItems.Add(debitAccounts[0].Attributes.GetNamedItem("Text").Value);
                    double nValue = Convert.ToDouble(debitAccounts[0].Attributes.GetNamedItem("Value").Value) / 100;
                    item.SubItems.Add(nValue.ToString("0.00"));
                    string strAccountNumber = debitAccounts[0].Attributes.GetNamedItem("Account").Value;
                    item.SubItems.Add(BuildAccountDescription(strAccountNumber));
                    strAccountNumber = creditAccounts[0].Attributes.GetNamedItem("Account").Value;
                    item.SubItems.Add(BuildAccountDescription(strAccountNumber));
                    listViewJournal.Items.Add(item);
                    continue;
                }
                foreach (XmlNode debitEntry in debitAccounts)
                {
                    ListViewItem DebitItem = (ListViewItem)item.Clone();
                    DebitItem.SubItems.Add(debitEntry.Attributes.GetNamedItem("Text").Value);
                    double nValue = Convert.ToDouble(debitEntry.Attributes.GetNamedItem("Value").Value) / 100;
                    DebitItem.SubItems.Add(nValue.ToString("0.00"));
                    string strAccountNumber = debitEntry.Attributes.GetNamedItem("Account").Value;
                    DebitItem.SubItems.Add(BuildAccountDescription(strAccountNumber));
                    listViewJournal.Items.Add(DebitItem);
                }
                foreach (XmlNode creditEntry in creditAccounts)
                {
                    ListViewItem CreditItem = (ListViewItem)item.Clone();
                    CreditItem.SubItems.Add(creditEntry.Attributes.GetNamedItem("Text").Value);
                    double nValue = Convert.ToDouble(creditEntry.Attributes.GetNamedItem("Value").Value) / 100;
                    CreditItem.SubItems.Add(nValue.ToString("0.00"));
                    CreditItem.SubItems.Add("");
                    string strAccountNumber = creditEntry.Attributes.GetNamedItem("Account").Value;
                    CreditItem.SubItems.Add(BuildAccountDescription(strAccountNumber));
                    listViewJournal.Items.Add(CreditItem);
                }
            }
        }

        void RefreshAccount(Int32 nAccountNumber)
        {
            RefreshAccount(nAccountNumber, true);
        }
        void RefreshAccount(Int32 nAccountNumber, bool bOnlyCurrentBookyear)
        {
            XmlNodeList nodes;
            if (bOnlyCurrentBookyear)
                nodes = m_Document.SelectNodes("//Journal[@Year=" + m_strBookingYearName + "]/Entry/*[@Account=" + nAccountNumber.ToString() + "]");
            else
                nodes = m_Document.SelectNodes("//Journal/Entry/*[@Account=" + nAccountNumber.ToString() + "]");
            listViewAccountJournal.Items.Clear();
            Double nCreditSum = 0;
            Double nDebitSum = 0;
            bool bColorStatus = false;
            //bool bOpeningSeen = false;
            foreach (XmlNode subentry in nodes)
            {
                XmlNode entry = subentry.ParentNode;

                //XmlNode opening = entry.Attributes.GetNamedItem("Opening");
                //if ( opening != null && opening.Value == "1" )
                //{
                //    if ( bOpeningSeen )
                //        continue;
                //}
                //// any first entry for the account defines an opening value
                //bOpeningSeen = true;

                ListViewItem item = new ListViewItem(entry.Attributes.GetNamedItem("Date").Value);
                if (bColorStatus)
                    item.BackColor = Color.LightGreen;
                bColorStatus = !bColorStatus;

                item.SubItems.Add(entry.Attributes.GetNamedItem("ID").Value);
                item.SubItems.Add(subentry.Attributes.GetNamedItem("Text").Value);
                if (subentry.Name == "Debit")
                {
                    string strValue = subentry.Attributes.GetNamedItem("Value").Value;
                    Double nValue = Convert.ToDouble(strValue) / 100;
                    nDebitSum += nValue;
                    item.SubItems.Add(nValue.ToString("0.00"));
                    item.SubItems.Add("");
                    XmlNodeList CreditAccounts = entry.SelectNodes("Credit");
                    if (CreditAccounts.Count == 1)
                    {
                        string strCreditAccountNumber = CreditAccounts[0].Attributes.GetNamedItem("Account").Value;
                        item.SubItems.Add(BuildAccountDescription(strCreditAccountNumber));
                    }
                    else
                        item.SubItems.Add("Diverse");
                }
                else
                {
                    item.SubItems.Add("");
                    string strValue = subentry.Attributes.GetNamedItem("Value").Value;
                    Double nValue = Convert.ToDouble(strValue) / 100;
                    nCreditSum += nValue;
                    item.SubItems.Add(nValue.ToString("0.00"));
                    XmlNodeList DebitAccounts = entry.SelectNodes("Debit");
                    if (DebitAccounts.Count == 1)
                    {
                        string strDebitAccountNumber = DebitAccounts[0].Attributes.GetNamedItem("Account").Value;
                        item.SubItems.Add(BuildAccountDescription(strDebitAccountNumber));
                    }
                    else
                        item.SubItems.Add("Diverse");
                }
                listViewAccountJournal.Items.Add(item);
            }

            ListViewItem sumItem = new ListViewItem();
            sumItem.BackColor = Color.LightGray;
            sumItem.SubItems.Add("");
            sumItem.SubItems.Add("Summe");
            sumItem.SubItems.Add(nDebitSum.ToString("0.00"));
            sumItem.SubItems.Add(nCreditSum.ToString("0.00"));
            listViewAccountJournal.Items.Add(sumItem);

            ListViewItem saldoItem = new ListViewItem();
            saldoItem.BackColor = Color.LightGray;
            saldoItem.SubItems.Add("");
            saldoItem.SubItems.Add("Saldo");
            if (nDebitSum > nCreditSum)
            {
                saldoItem.SubItems.Add((nDebitSum - nCreditSum).ToString("0.00"));
                saldoItem.SubItems.Add("");
            }
            else
            {
                saldoItem.SubItems.Add("");
                saldoItem.SubItems.Add((nCreditSum - nDebitSum).ToString("0.00"));
            }
            listViewAccountJournal.Items.Add(saldoItem);
        }
    }
}