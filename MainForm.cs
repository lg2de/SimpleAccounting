// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using lg2de.SimpleAccounting.Properties;

namespace lg2de.SimpleAccounting
{
    public partial class MainForm : Form
    {
        private const string AssetName = nameof(AccountingDataAccountType.Asset);
        private readonly XmlDocument document;
        bool isDocumentChanged = false;
        string fileName = "";

        string bookingYearName = "";
        DateTime bookDate;
        int bookNumber;
        private string firmName;

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
            this.InitializeComponent();

            this.document = new XmlDocument();

            Settings.Default.Upgrade();
            if (Settings.Default.RecentProjects == null)
            {
                Settings.Default.RecentProjects = new StringCollection();
            }

            if (File.Exists(Settings.Default.RecentProject))
            {
                this.LoadDatabase(Settings.Default.RecentProject);
            }

            this.MenuItemArchive.DropDownItems.Add(new ToolStripSeparator());
            foreach (var project in Settings.Default.RecentProjects)
            {
                if (!File.Exists(project))
                {
                    continue;
                }

                var item = this.MenuItemArchive.DropDownItems.Add(project);
                item.Tag = project;
                item.Click += (s, a) =>
                {
                    var menuEntry = (ToolStripItem)s;
                    string fileName = menuEntry.Tag.ToString();
                    if (!File.Exists(fileName))
                    {
                        return;
                    }

                    if (this.isDocumentChanged)
                    {
                        var result = MessageBox.Show(
                            "Die Datenbasis hat sich geändert.\nWollen Sie Speichern?",
                            "Programm beenden",
                            MessageBoxButtons.YesNoCancel);
                        if (result == DialogResult.Cancel)
                        {
                            return;
                        }
                        else if (result == DialogResult.Yes)
                        {
                            this.SaveDatabase();
                        }
                    }

                    this.LoadDatabase(fileName);
                };
            }
        }

        internal XmlNode GetAccountNode(int nAccountIdent)
        {
            XmlNode accoutNode = this.document.SelectSingleNode("//Accounts/Account[@ID=" + nAccountIdent + "]");
            return accoutNode;
        }

        internal IEnumerable<string> GetAccounts()
        {
            foreach (XmlElement account in this.document.SelectNodes("//Accounts/Account"))
            {
                yield return $"{account.GetAttribute("Name")} ({account.GetAttribute("ID")})";
            }
        }

        internal string GetAccountName(int nAccountIdent)
        {
            XmlNode accoutNode = this.GetAccountNode(nAccountIdent);
            return accoutNode?.Attributes.GetNamedItem("Name").Value ?? string.Empty;
        }

        internal string GetFormatedDate(string strInternalDate)
        {
            return this.GetFormatedDate(Convert.ToInt32(strInternalDate));
        }
        internal string GetFormatedDate(int nInternalDate)
        {
            return (nInternalDate % 100).ToString("D2") + "." + ((nInternalDate % 10000) / 100).ToString("D2") + "." + (nInternalDate / 10000).ToString("D2");
        }

        internal int GetMaxBookIdent()
        {
            XmlNode journal = this.document.SelectSingleNode($"//Journal[@Year='{this.bookingYearName}']");
            var ids = journal.SelectNodes("Booking/@ID");
            int maxIdent = 0;
            foreach (XmlNode id in ids)
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
            this.bookDate = date;
        }
        internal void SetBookIdent(int number)
        {
            this.bookNumber = number;
        }
        internal void AddDebitEntry(int nAccount, int nValue, string strText)
        {
            var entry = new BookEntry();
            entry.Account = nAccount;
            entry.Value = nValue;
            entry.Text = strText;
            this.DebitEntries.Add(entry);
        }
        internal void AddCreditEntry(int nAccount, int nValue, string strText)
        {
            var entry = new BookEntry();
            entry.Account = nAccount;
            entry.Value = nValue;
            entry.Text = strText;
            this.CreditEntries.Add(entry);
        }
        internal void RegisterBooking()
        {
            XmlElement newNode = this.document.CreateElement("Booking");
            XmlAttribute attrEntry = this.document.CreateAttribute("Date");
            attrEntry.Value = this.bookDate.ToString("yyyyMMdd");
            newNode.Attributes.Append(attrEntry);
            attrEntry = this.document.CreateAttribute("ID");
            attrEntry.Value = this.bookNumber.ToString();
            newNode.Attributes.Append(attrEntry);
            foreach (BookEntry entry in this.DebitEntries)
            {
                XmlElement newEntry = this.CreateXmlElement("Debit", entry);
                newNode.AppendChild(newEntry);
            }
            foreach (BookEntry entry in this.CreditEntries)
            {
                XmlElement newEntry = this.CreateXmlElement("Credit", entry);
                newNode.AppendChild(newEntry);
            }

            XmlNode JournalNode = this.document.SelectSingleNode($"//Journal[@Year='{this.bookingYearName}']");
            JournalNode.AppendChild(newNode);
            this.isDocumentChanged = true;

            this.DebitEntries.Clear();
            this.CreditEntries.Clear();

            this.RefreshJournal();
        }

        void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!this.isDocumentChanged)
            {
                return;
            }

            DialogResult ret = MessageBox.Show(
                "Die Datenbasis hat sich geändert.\nWollen Sie Speichern?",
                "Programm beenden",
                MessageBoxButtons.YesNoCancel);
            if (ret == DialogResult.Cancel)
            {
                e.Cancel = true;
            }
            else if (ret == DialogResult.Yes)
            {
                this.SaveDatabase();
            }
        }

        private void MenuItemArchiveOpen_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Projektdateien (*.bxml)|*.bxml";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                this.LoadDatabase(openFileDialog.FileName);
            }
        }

        void MenuItemArchiveSave_Click(object sender, EventArgs e)
        {
            this.SaveDatabase();
        }

        void MenuItemArchiveExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        void MenuItemActionsBooking_Click(object sender, EventArgs e)
        {
            var dlg = new BookingDialog(this);
            DialogResult ret = dlg.ShowDialog();
        }

        void MenuItemActionsSelectYear_Click(object sender, EventArgs e)
        {
            var dlg = new SelectBookingYear();
            XmlNodeList years = this.document.SelectNodes("//Years/Year");
            foreach (XmlNode year in years)
            {
                dlg.AddYear(year.Attributes.GetNamedItem("Name").Value);
            }

            dlg.CurrentYear = this.bookingYearName;
            var result = dlg.ShowDialog();
            if (result != DialogResult.OK)
            {
                return;
            }

            this.SelectBookingYear(dlg.CurrentYear);
        }

        void MenuItemActionCloseYear_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Wollen Sie das Jahr " + this.bookingYearName + " abschließen?",
                "Jahresabschluß",
                MessageBoxButtons.YesNo);
            if (result != DialogResult.Yes)
            {
                return;
            }

            XmlNode yearNode = this.document.SelectSingleNode("//Year[@Name=" + this.bookingYearName + "]");
            var attr = (XmlAttribute)yearNode.Attributes.GetNamedItem("Closed");
            if (attr != null && attr.Value == "True")
            {
                return;
            }

            attr = this.document.CreateAttribute("Closed");
            attr.Value = "True";
            yearNode.Attributes.Append(attr);

            string strNewYear = (Convert.ToInt32(this.bookingYearName) + 1).ToString();

            XmlNode newYearNode = this.document.CreateElement("Year");
            attr = this.document.CreateAttribute("Name");
            attr.Value = strNewYear;
            newYearNode.Attributes.Append(attr);
            attr = this.document.CreateAttribute("DateStart");
            string strDateStart = (Convert.ToInt64(yearNode.Attributes.GetNamedItem("DateStart").Value) + 10000).ToString();
            attr.Value = strDateStart;
            newYearNode.Attributes.Append(attr);
            attr = this.document.CreateAttribute("DateEnd");
            attr.Value = (Convert.ToInt64(yearNode.Attributes.GetNamedItem("DateEnd").Value) + 10000).ToString();
            newYearNode.Attributes.Append(attr);
            yearNode.ParentNode.AppendChild(newYearNode);

            XmlNode newYearJournal = this.document.CreateElement("Journal");
            attr = this.document.CreateAttribute("Year");
            attr.Value = strNewYear;
            newYearJournal.Attributes.Append(attr);
            this.document.DocumentElement.InsertAfter(newYearJournal, this.document.SelectSingleNode("//Journal[@Year=" + this.bookingYearName + "]"));

            int nID = 1;

            // Asset Accounts (Bestandskonten), Credit and Debit Accounts
            XmlNodeList accountNodes = this.document.SelectNodes($"//Accounts/Account[@Type='{AssetName}' or @Type='Credit' or @Type='Debit']");
            foreach (XmlNode accountNode in accountNodes)
            {
                string strAccount = accountNode.Attributes.GetNamedItem("ID").Value;
                XPathNavigator nav = this.document.CreateNavigator();
                double nCredit = (double)nav.Evaluate(
                    "sum(//Journal[@Year=" + this.bookingYearName + "]/Booking/Credit[@Account=" + strAccount + "]/@Value)");
                double nDebit = (double)nav.Evaluate(
                    "sum(//Journal[@Year=" + this.bookingYearName + "]/Booking/Debit[@Account=" + strAccount + "]/@Value)");

                if (nCredit == 0 && nDebit == 0 || nCredit == nDebit)
                {
                    continue;
                }

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

                XmlNode entry = this.document.CreateElement("Booking");
                attr = this.document.CreateAttribute("Date");
                attr.Value = strDateStart;
                entry.Attributes.Append(attr);
                attr = this.document.CreateAttribute("ID");
                attr.Value = nID.ToString();
                entry.Attributes.Append(attr);
                attr = this.document.CreateAttribute("Opening");
                attr.Value = "1";
                entry.Attributes.Append(attr);
                newYearJournal.AppendChild(entry);

                XmlNode value = this.document.CreateElement(strName1);
                attr = this.document.CreateAttribute("Value");
                attr.Value = nValue.ToString();
                value.Attributes.Append(attr);
                attr = this.document.CreateAttribute("Account");
                attr.Value = strAccount;
                value.Attributes.Append(attr);
                attr = this.document.CreateAttribute("Text");
                attr.Value = "EB-Wert " + nID.ToString();
                value.Attributes.Append(attr);
                entry.AppendChild(value);

                value = this.document.CreateElement(strName2);
                attr = this.document.CreateAttribute("Value");
                attr.Value = nValue.ToString();
                value.Attributes.Append(attr);
                attr = this.document.CreateAttribute("Account");
                attr.Value = "990";
                value.Attributes.Append(attr);
                attr = this.document.CreateAttribute("Text");
                attr.Value = "EB-Wert " + nID.ToString();
                value.Attributes.Append(attr);
                entry.AppendChild(value);

                nID++;
            }

            this.isDocumentChanged = true;
            this.SelectBookingYear(strNewYear);
        }

        void MenuItemReportsJournal_Click(object sender, EventArgs e)
        {
            var print = new PrintClass();
            string fileName = Application.ExecutablePath;
            fileName = fileName.Substring(0, fileName.LastIndexOf("\\"));
            fileName = fileName.Substring(0, fileName.LastIndexOf("\\"));
            fileName = fileName.Substring(0, fileName.LastIndexOf("\\") + 1);
            fileName += "Journal.xml";
            print.LoadDocument(fileName);

            XmlDocument doc = print.Document;

            XmlNode firmNode = doc.SelectSingleNode("//text[@ID=\"firm\"]");
            firmNode.InnerText = this.firmName;

            XmlNode rangeNode = doc.SelectSingleNode("//text[@ID=\"range\"]");
            XmlNode yearNode = this.document.SelectSingleNode("//Year[@Name=" + this.bookingYearName + "]");
            XmlNode startNode = yearNode.Attributes.GetNamedItem("DateStart");
            XmlNode endNode = yearNode.Attributes.GetNamedItem("DateEnd");
            rangeNode.InnerText = this.GetFormatedDate(startNode.Value) + " - " + this.GetFormatedDate(endNode.Value);

            var dateNode = doc.SelectSingleNode("//text[@ID=\"date\"]");
            dateNode.InnerText = "Dresden, " + DateTime.Now.ToLongDateString();

            XmlNode dataNode = doc.SelectSingleNode("//table/data");

            var xdoc = XDocument.Parse(this.document.OuterXml);
            var journalEntries = xdoc.XPathSelectElements("//Journal[@Year=" + this.bookingYearName + "]/Booking");
            foreach (var entry in journalEntries.OrderBy(x => x.Attribute("Date").Value))
            {
                XmlNode dataLineNode = doc.CreateElement("tr");
                XmlNode dataItemNode = doc.CreateElement("td");
                this.SetNodeAttribute(dataLineNode, "topline", "1");
                dataItemNode.InnerText = this.GetFormatedDate(entry.Attribute("Date").Value);
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

            print.PrintDocument(DateTime.Now.ToString("yyyy-MM-dd") + " Journal " + this.bookingYearName);
        }
        void MenuItemReportsSummary_Click(object sender, EventArgs e)
        {
            var print = new PrintClass();
            string fileName = Application.ExecutablePath;
            fileName = fileName.Substring(0, fileName.LastIndexOf("\\"));
            fileName = fileName.Substring(0, fileName.LastIndexOf("\\"));
            fileName = fileName.Substring(0, fileName.LastIndexOf("\\") + 1);
            fileName += "Saldo.xml";
            print.LoadDocument(fileName);

            XmlDocument doc = print.Document;

            XmlNode firmNode = doc.SelectSingleNode("//text[@ID=\"firm\"]");
            firmNode.InnerText = this.firmName;

            XmlNode rangeNode = doc.SelectSingleNode("//text[@ID=\"range\"]");
            XmlNode yearNode = this.document.SelectSingleNode("//Year[@Name=" + this.bookingYearName + "]");
            XmlNode startNode = yearNode.Attributes.GetNamedItem("DateStart");
            XmlNode endNode = yearNode.Attributes.GetNamedItem("DateEnd");
            rangeNode.InnerText = this.GetFormatedDate(startNode.Value) + " - " + this.GetFormatedDate(endNode.Value);

            var dateNode = doc.SelectSingleNode("//text[@ID=\"date\"]");
            dateNode.InnerText = "Dresden, " + DateTime.Now.ToLongDateString();

            XmlNode dataNode = doc.SelectSingleNode("//table/data");
            XmlNodeList accountEntries = this.document.SelectNodes("//Accounts/Account");
            XmlNode journalNode = this.document.SelectSingleNode("//Journal[@Year=" + this.bookingYearName + "]");

            double totalOpeningCredit = 0, totalOpeningDebit = 0;
            double totalSumSectionCredit = 0, totalSumSectionDebit = 0;
            double totalSumEndCredit = 0, totalSumEndDebit = 0;
            double totalSaldoCredit = 0, totalSaldoDebit = 0;
            foreach (XmlNode accountNode in accountEntries)
            {
                XmlNodeList journalEntries = journalNode.SelectNodes("Booking/*[@Account=" + accountNode.Attributes.GetNamedItem("ID").Value + "]");
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
                this.SetNodeAttribute(dataLineNode, "topline", "1");

                dataItemNode.InnerText = accountNode.Attributes.GetNamedItem("ID").Value;
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = accountNode.Attributes.GetNamedItem("Name").Value;
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = this.GetFormatedDate(lastBookingDate);
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
            this.SetNodeAttribute(totalLineNode, "topline", "1");

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

            print.PrintDocument(DateTime.Now.ToString("yyyy-MM-dd") + " Summen und Salden " + this.bookingYearName);
        }

        private void MenuItemReportsBilanz_Click(object sender, EventArgs e)
        {
            var print = new PrintClass();
            string fileName = Application.ExecutablePath;
            fileName = fileName.Substring(0, fileName.LastIndexOf("\\"));
            fileName = fileName.Substring(0, fileName.LastIndexOf("\\"));
            fileName = fileName.Substring(0, fileName.LastIndexOf("\\") + 1);
            fileName += "Bilanz.xml";
            print.LoadDocument(fileName);

            XmlDocument doc = print.Document;

            var firmNode = doc.SelectSingleNode("//text[@ID=\"firm\"]");
            firmNode.InnerText = this.firmName;

            var rangeNode = doc.SelectSingleNode("//text[@ID=\"range\"]");
            rangeNode.InnerText = this.bookingYearName;

            var dateNode = doc.SelectSingleNode("//text[@ID=\"date\"]");
            dateNode.InnerText = "Dresden, " + DateTime.Now.ToLongDateString();

            var xdoc = XDocument.Parse(this.document.OuterXml);
            var journal = xdoc.XPathSelectElement("//Journal[@Year=" + this.bookingYearName + "]");

            var dataNode = doc.SelectSingleNode("//table/data[@target='income']");
            var accounts = xdoc.XPathSelectElements("//Accounts/Account[@Type='Income']");
            double totalIncome = 0;
            foreach (var account in accounts)
            {
                var journalEntries = journal.XPathSelectElements("Booking/*[@Account=" + account.Attribute("ID").Value + "]");

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
                {
                    continue;
                }

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
                var journalEntries = journal.XPathSelectElements("Booking/*[@Account=" + account.Attribute("ID").Value + "]");

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
                {
                    continue;
                }

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
                var journalEntries = journal.XPathSelectElements("Booking/*[@Account=" + account.Attribute("ID").Value + "]");

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
                var journalEntries = journal.XPathSelectElements("Booking/*[@Account=" + account.Attribute("ID").Value + "]");

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
            accounts = xdoc.XPathSelectElements($"//Accounts/Account[@Type='{AssetName}']");
            double totalAccount = 0;
            foreach (var account in accounts)
            {
                var journalEntries = journal.XPathSelectElements("Booking/*[@Account=" + account.Attribute("ID").Value + "]");

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
                {
                    continue;
                }

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

            print.PrintDocument(DateTime.Now.ToString("yyyy-MM-dd") + " Jahresbilanz " + this.bookingYearName);
        }

        void listViewAccounts_DoubleClick(object sender, EventArgs e)
        {
            ListViewItem item = ((ListView)sender).SelectedItems[0];
            Int32 nAccountNumber = Convert.ToInt32(item.Text);
            this.RefreshAccount(nAccountNumber);
        }

        XmlElement CreateXmlElement(string Type, BookEntry entry)
        {
            XmlElement newEntry = this.document.CreateElement(Type);
            XmlAttribute attrDebit = this.document.CreateAttribute("Value");
            attrDebit.Value = entry.Value.ToString();
            newEntry.Attributes.Append(attrDebit);
            attrDebit = this.document.CreateAttribute("Account");
            attrDebit.Value = entry.Account.ToString();
            newEntry.Attributes.Append(attrDebit);
            attrDebit = this.document.CreateAttribute("Text");
            attrDebit.Value = entry.Text;
            newEntry.Attributes.Append(attrDebit);
            return newEntry;
        }
        string BuildAccountDescription(string strAccountNumber)
        {
            int nAccountNumber = Convert.ToInt32(strAccountNumber);
            string strAccountName = this.GetAccountName(nAccountNumber);
            return strAccountNumber + " (" + strAccountName + ")";
        }

        void SelectLastBookingYear()
        {
            XmlNodeList years = this.document.SelectNodes("//Years/Year");
            if (years.Count > 0)
            {
                this.SelectBookingYear(years[years.Count - 1].Attributes.GetNamedItem("Name").Value);
            }
        }

        void SelectBookingYear(string strYearName)
        {
            this.bookingYearName = strYearName;
            this.Text = "Buchhaltung - " + this.fileName + " - " + this.bookingYearName;
            this.RefreshJournal();
            this.listViewAccountJournal.Items.Clear();
        }

        void LoadDatabase(string fileName)
        {
            this.listViewAccounts.Items.Clear();

            this.fileName = fileName;
            this.document.Load(this.fileName);
            var booking = AccountingData.LoadFromFile(this.fileName);
            this.firmName = booking.Setup.Name;
            XmlNodeList nodes = this.document.SelectNodes("//Accounts/Account");
            foreach (XmlNode entry in nodes)
            {
                var item = new ListViewItem(entry.Attributes.GetNamedItem("ID").Value);
                item.SubItems.Add(entry.Attributes.GetNamedItem("Name").Value);
                this.listViewAccounts.Items.Add(item);
            }

            this.SelectLastBookingYear();

            Settings.Default.RecentProject = fileName;
            Settings.Default.RecentProjects.Remove(fileName);
            Settings.Default.RecentProjects.Insert(0, fileName);
            while (Settings.Default.RecentProjects.Count > 10)
            {
                Settings.Default.RecentProjects.RemoveAt(10);
            }

            Settings.Default.Save();
        }

        void SaveDatabase()
        {
            DateTime fileDate = File.GetLastWriteTime(this.fileName);
            string strNewFileName = this.fileName + "." + fileDate.ToString("yyyyMMddHHmmss");
            try
            {
                File.Move(this.fileName, strNewFileName);
            }
            catch (FileNotFoundException)
            {
            }
            var file = new FileStream(this.fileName, FileMode.Create, FileAccess.Write);
            this.document.Save(file);
            file.Close();
            this.isDocumentChanged = false;
        }

        void RefreshJournal()
        {
            this.listViewJournal.Items.Clear();
            XmlNode journal = this.document.SelectSingleNode("//Journal[@Year=" + this.bookingYearName + "]");
            bool bColorStatus = false;
            foreach (XmlNode entry in journal.ChildNodes)
            {
                if (entry.Name != "Booking")
                {
                    continue;
                }

                var item = new ListViewItem(entry.Attributes.GetNamedItem("Date").Value);
                if (bColorStatus)
                {
                    item.BackColor = Color.LightGreen;
                }

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
                    item.SubItems.Add(this.BuildAccountDescription(strAccountNumber));
                    strAccountNumber = creditAccounts[0].Attributes.GetNamedItem("Account").Value;
                    item.SubItems.Add(this.BuildAccountDescription(strAccountNumber));
                    this.listViewJournal.Items.Add(item);
                    continue;
                }
                foreach (XmlNode debitEntry in debitAccounts)
                {
                    var DebitItem = (ListViewItem)item.Clone();
                    DebitItem.SubItems.Add(debitEntry.Attributes.GetNamedItem("Text").Value);
                    double nValue = Convert.ToDouble(debitEntry.Attributes.GetNamedItem("Value").Value) / 100;
                    DebitItem.SubItems.Add(nValue.ToString("0.00"));
                    string strAccountNumber = debitEntry.Attributes.GetNamedItem("Account").Value;
                    DebitItem.SubItems.Add(this.BuildAccountDescription(strAccountNumber));
                    this.listViewJournal.Items.Add(DebitItem);
                }
                foreach (XmlNode creditEntry in creditAccounts)
                {
                    var CreditItem = (ListViewItem)item.Clone();
                    CreditItem.SubItems.Add(creditEntry.Attributes.GetNamedItem("Text").Value);
                    double nValue = Convert.ToDouble(creditEntry.Attributes.GetNamedItem("Value").Value) / 100;
                    CreditItem.SubItems.Add(nValue.ToString("0.00"));
                    CreditItem.SubItems.Add("");
                    string strAccountNumber = creditEntry.Attributes.GetNamedItem("Account").Value;
                    CreditItem.SubItems.Add(this.BuildAccountDescription(strAccountNumber));
                    this.listViewJournal.Items.Add(CreditItem);
                }
            }
        }

        void RefreshAccount(Int32 nAccountNumber)
        {
            this.RefreshAccount(nAccountNumber, true);
        }
        void RefreshAccount(Int32 nAccountNumber, bool bOnlyCurrentBookyear)
        {
            XmlNodeList nodes;
            if (bOnlyCurrentBookyear)
            {
                nodes = this.document.SelectNodes("//Journal[@Year=" + this.bookingYearName + "]/Booking/*[@Account=" + nAccountNumber.ToString() + "]");
            }
            else
            {
                nodes = this.document.SelectNodes("//Journal/Booking/*[@Account=" + nAccountNumber.ToString() + "]");
            }

            this.listViewAccountJournal.Items.Clear();
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

                var item = new ListViewItem(entry.Attributes.GetNamedItem("Date").Value);
                if (bColorStatus)
                {
                    item.BackColor = Color.LightGreen;
                }

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
                        item.SubItems.Add(this.BuildAccountDescription(strCreditAccountNumber));
                    }
                    else
                    {
                        item.SubItems.Add("Diverse");
                    }
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
                        item.SubItems.Add(this.BuildAccountDescription(strDebitAccountNumber));
                    }
                    else
                    {
                        item.SubItems.Add("Diverse");
                    }
                }
                this.listViewAccountJournal.Items.Add(item);
            }

            var sumItem = new ListViewItem();
            sumItem.BackColor = Color.LightGray;
            sumItem.SubItems.Add("");
            sumItem.SubItems.Add("Summe");
            sumItem.SubItems.Add(nDebitSum.ToString("0.00"));
            sumItem.SubItems.Add(nCreditSum.ToString("0.00"));
            this.listViewAccountJournal.Items.Add(sumItem);

            var saldoItem = new ListViewItem();
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
            this.listViewAccountJournal.Items.Add(saldoItem);
        }
    }
}