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
using lg2de.SimpleAccounting.Properties;

namespace lg2de.SimpleAccounting
{
    public partial class MainForm : Form
    {
        private readonly List<BookingValue> debitEntries = new List<BookingValue>();
        private readonly List<BookingValue> creditEntries = new List<BookingValue>();
        private AccountingData accountingData;
        bool isDocumentChanged = false;
        string fileName = "";

        string bookingYearName = "";
        private AccountingDataJournal currentJournal;
        DateTime bookDate;
        ulong bookNumber;
        private string firmName;

        void SetNodeAttribute(XmlNode node, string strName, string strValue)
        {
            XmlAttribute attr = node.OwnerDocument.CreateAttribute(strName);
            attr.Value = strValue;
            node.Attributes.SetNamedItem(attr);
        }

        public MainForm()
        {
            this.InitializeComponent();

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

        internal IEnumerable<string> GetAccounts()
        {
            return this.accountingData.Accounts.Select(a => $"{a.Name} ({a.ID})");
        }

        internal string GetAccountName(uint accountId)
        {
            return this.accountingData.Accounts.Single(a => a.ID == accountId).Name;
        }

        internal string GetFormatedDate(string strInternalDate)
        {
            return this.GetFormatedDate(Convert.ToUInt32(strInternalDate));
        }
        internal string GetFormatedDate(uint nInternalDate)
        {
            return (nInternalDate % 100).ToString("D2") + "." + ((nInternalDate % 10000) / 100).ToString("D2") + "." + (nInternalDate / 10000).ToString("D2");
        }

        internal ulong GetMaxBookIdent()
        {
            if (!this.currentJournal.Booking.Any())
            {
                return 0;
            }

            return this.currentJournal.Booking.Max(b => b.ID);
        }

        internal void SetBookDate(DateTime date)
        {
            this.bookDate = date;
        }
        internal void SetBookIdent(ulong number)
        {
            this.bookNumber = number;
        }
        internal void AddDebitEntry(ulong nAccount, int nValue, string strText)
        {
            var entry = new BookingValue();
            entry.Account = nAccount;
            entry.Value = nValue;
            entry.Text = strText;
            this.debitEntries.Add(entry);
        }
        internal void AddCreditEntry(ulong nAccount, int nValue, string strText)
        {
            var entry = new BookingValue();
            entry.Account = nAccount;
            entry.Value = nValue;
            entry.Text = strText;
            this.creditEntries.Add(entry);
        }
        internal void RegisterBooking()
        {
            var newBooking = new AccountingDataJournalBooking
            {
                Date = Convert.ToUInt32(this.bookDate.ToString("yyyyMMdd")),
                ID = this.bookNumber
            };
            this.currentJournal.Booking.Add(newBooking);
            this.debitEntries.ForEach(newBooking.Debit.Add);
            this.creditEntries.ForEach(newBooking.Credit.Add);

            this.isDocumentChanged = true;

            this.debitEntries.Clear();
            this.creditEntries.Clear();

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
            foreach (var year in this.accountingData.Years)
            {
                dlg.AddYear(year.Name.ToString());
            }

            dlg.CurrentYear = this.bookingYearName;
            var result = dlg.ShowDialog();
            if (result != DialogResult.OK)
            {
                return;
            }

            this.SelectBookingYear(Convert.ToUInt16(dlg.CurrentYear));
        }

        void MenuItemActionCloseYear_Click(object sender, EventArgs e)
        {
            var accountingYear = this.accountingData.Years.Single(y => y.Name.ToString() == this.bookingYearName);
            if (accountingYear.Closed)
            {
                // nothing to do
                return;
            }

            var result = MessageBox.Show(
                "Wollen Sie das Jahr " + this.bookingYearName + " abschließen?",
                "Jahresabschluß",
                MessageBoxButtons.YesNo);
            if (result != DialogResult.Yes)
            {
                return;
            }

            accountingYear.Closed = true;

            var newYear = (ushort)(Convert.ToUInt16(this.bookingYearName) + 1);

            var newYearEntry = new AccountingDataYear
            {
                Name = newYear,
                DateStart = accountingYear.DateStart + 10000,
                DateEnd = accountingYear.DateEnd + 10000
            };
            this.accountingData.Years.Add(newYearEntry);
            var newYearJournal = new AccountingDataJournal { Year = newYear };
            this.accountingData.Journal.Add(newYearJournal);

            ulong bookingId = 1;

            // Asset Accounts (Bestandskonten), Credit and Debit Accounts
            var accounts = this.accountingData.Accounts.Where(a => a.Type == AccountingDataAccountType.Asset || a.Type == AccountingDataAccountType.Credit || a.Type == AccountingDataAccountType.Debit);
            foreach (var account in accounts)
            {
                var creditAmount = this.currentJournal.Booking
                    .SelectMany(b => b.Credit.Where(x => x.Account == account.ID))
                    .Sum(x => x.Value);
                var debitAmount = this.currentJournal.Booking
                    .SelectMany(b => b.Debit.Where(x => x.Account == account.ID))
                    .Sum(x => x.Value);

                if (creditAmount == 0 && debitAmount == 0 || creditAmount == debitAmount)
                {
                    // nothing to do
                    continue;
                }

                var dateStart = newYearEntry.DateStart + 10000;
                var newBooking = new AccountingDataJournalBooking
                {
                    Date = dateStart,
                    ID = bookingId,
                    Opening = true
                };
                newYearJournal.Booking.Add(newBooking);
                var newDebit = new BookingValue
                {
                    Value = Math.Abs(creditAmount - debitAmount),
                    Text = "EB-Wert " + bookingId.ToString()
                };
                var newCredit = new BookingValue
                {
                    Value = newDebit.Value,
                    Text = newDebit.Text
                };
                if (creditAmount > debitAmount)
                {
                    newCredit.Account = account.ID;
                    newDebit.Account = 990;
                }
                else
                {
                    newDebit.Account = account.ID;
                    newCredit.Account = 990;
                }

                bookingId++;
            }

            this.isDocumentChanged = true;
            this.SelectBookingYear(newYear);
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
            var yearNode = this.accountingData.Years.Single(y => y.Name.ToString() == this.bookingYearName);
            rangeNode.InnerText = this.GetFormatedDate(yearNode.DateStart) + " - " + this.GetFormatedDate(yearNode.DateEnd);

            var dateNode = doc.SelectSingleNode("//text[@ID=\"date\"]");
            dateNode.InnerText = "Dresden, " + DateTime.Now.ToLongDateString();

            XmlNode dataNode = doc.SelectSingleNode("//table/data");

            var journalEntries = this.currentJournal.Booking.OrderBy(b => b.Date);
            foreach (var entry in journalEntries)
            {
                XmlNode dataLineNode = doc.CreateElement("tr");
                XmlNode dataItemNode = doc.CreateElement("td");
                this.SetNodeAttribute(dataLineNode, "topline", "1");
                dataItemNode.InnerText = this.GetFormatedDate(entry.Date);
                dataLineNode.AppendChild(dataItemNode);
                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = entry.ID.ToString();
                dataLineNode.AppendChild(dataItemNode);

                if (entry.Debit.Count == 1
                    && entry.Credit.Count == 1
                    && entry.Debit.First().Text == entry.Credit.First().Text)
                {
                    var credit = entry.Credit.Single();
                    var debit = entry.Debit.Single();
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = debit.Text;
                    dataLineNode.AppendChild(dataItemNode);
                    string strAccountNumber = debit.Account.ToString();
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = strAccountNumber;
                    dataLineNode.AppendChild(dataItemNode);
                    double nValue = Convert.ToDouble(debit.Value) / 100;
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = nValue.ToString("0.00");
                    dataLineNode.AppendChild(dataItemNode);
                    strAccountNumber = credit.Account.ToString();
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = strAccountNumber;
                    dataLineNode.AppendChild(dataItemNode);
                    nValue = Convert.ToDouble(credit.Value) / 100;
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = nValue.ToString("0.00");
                    dataLineNode.AppendChild(dataItemNode);
                    dataNode.AppendChild(dataLineNode);
                    continue;
                }

                foreach (var debitEntry in entry.Debit)
                {
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = debitEntry.Text;
                    dataLineNode.AppendChild(dataItemNode);
                    string strAccountNumber = debitEntry.Account.ToString();
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = strAccountNumber;
                    dataLineNode.AppendChild(dataItemNode);
                    double nValue = Convert.ToDouble(debitEntry.Value) / 100;
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

                foreach (var creditEntry in entry.Credit)
                {
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = creditEntry.Text;
                    dataLineNode.AppendChild(dataItemNode);
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = "";
                    dataLineNode.AppendChild(dataItemNode);
                    dataLineNode.AppendChild(dataItemNode.Clone());
                    string strAccountNumber = creditEntry.Account.ToString();
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = strAccountNumber;
                    dataLineNode.AppendChild(dataItemNode);
                    double nValue = Convert.ToDouble(creditEntry.Value) / 100;
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
            var yearNode = this.accountingData.Years.Single(y => y.Name.ToString() == this.bookingYearName);
            rangeNode.InnerText = this.GetFormatedDate(yearNode.DateStart) + " - " + this.GetFormatedDate(yearNode.DateEnd);

            var dateNode = doc.SelectSingleNode("//text[@ID=\"date\"]");
            dateNode.InnerText = "Dresden, " + DateTime.Now.ToLongDateString();

            XmlNode dataNode = doc.SelectSingleNode("//table/data");

            double totalOpeningCredit = 0, totalOpeningDebit = 0;
            double totalSumSectionCredit = 0, totalSumSectionDebit = 0;
            double totalSumEndCredit = 0, totalSumEndDebit = 0;
            double totalSaldoCredit = 0, totalSaldoDebit = 0;
            foreach (var account in this.accountingData.Accounts)
            {
                if (this.currentJournal.Booking.All(b => b.Debit.All(x => x.Account != account.ID) && b.Credit.All(x => x.Account != account.ID)))
                {
                    continue;
                }

                var lastBookingDate = this.currentJournal.Booking.Select(x => x.Date).DefaultIfEmpty().Max();
                double saldoCredit = this.currentJournal.Booking
                    .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                double saldoDebit = this.currentJournal.Booking
                    .SelectMany(x => x.Debit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                double openingCredit = this.currentJournal.Booking
                    .Where(b => b.Opening)
                    .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                double openingDebit = this.currentJournal.Booking
                    .Where(b => b.Opening)
                    .SelectMany(x => x.Debit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                double sumSectionCredit = this.currentJournal.Booking
                    .Where(b => !b.Opening)
                    .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                double sumSectionDebit = this.currentJournal.Booking
                    .Where(b => !b.Opening)
                    .SelectMany(x => x.Debit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                // currently identical
                double sumEndCredit = sumSectionCredit, sumEndDebit = sumSectionDebit;

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

                dataItemNode.InnerText = account.ID.ToString();
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = account.Name;
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

            var dataNode = doc.SelectSingleNode("//table/data[@target='income']");
            double totalIncome = 0;
            var accounts = this.accountingData.Accounts.Where(a => a.Type == AccountingDataAccountType.Income);
            foreach (var account in accounts)
            {
                double saldoCredit = this.currentJournal.Booking
                    .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                double saldoDebit = this.currentJournal.Booking
                    .SelectMany(x => x.Debit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);

                double balance = saldoCredit - saldoDebit;
                if (balance == 0)
                {
                    continue;
                }

                totalIncome += balance;

                XmlNode dataLineNode = doc.CreateElement("tr");
                XmlNode dataItemNode = doc.CreateElement("td");

                dataLineNode.AppendChild(dataItemNode);

                string accountText = account.ID.ToString().PadLeft(5, '0') + " " + account.Name;

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
            double totalExpense = 0;
            accounts = this.accountingData.Accounts.Where(a => a.Type == AccountingDataAccountType.Expense);
            foreach (var account in accounts)
            {
                double saldoCredit = this.currentJournal.Booking
                    .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                double saldoDebit = this.currentJournal.Booking
                    .SelectMany(x => x.Debit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);

                double balance = saldoCredit - saldoDebit;
                if (balance == 0)
                {
                    continue;
                }

                totalExpense += balance;

                XmlNode dataLineNode = doc.CreateElement("tr");
                XmlNode dataItemNode = doc.CreateElement("td");

                dataLineNode.AppendChild(dataItemNode);

                string accountText = account.ID.ToString().PadLeft(5, '0') + " " + account.Name;

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
            saldoElement.InnerText = (totalExpense / 100).ToString("0.00");

            var saldoNode = doc.SelectSingleNode("//text[@ID=\"saldo\"]");
            saldoNode.InnerText = ((totalIncome + totalExpense) / 100).ToString("0.00");

            // receivables / Forderungen
            dataNode = doc.SelectSingleNode("//table/data[@target='receivable']");
            double totalReceivable = 0;
            accounts = this.accountingData.Accounts.Where(a => a.Type == AccountingDataAccountType.Debit || a.Type == AccountingDataAccountType.Credit);
            foreach (var account in accounts)
            {
                double saldoCredit = this.currentJournal.Booking
                    .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                double saldoDebit = this.currentJournal.Booking
                    .SelectMany(x => x.Debit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);

                double balance = saldoDebit - saldoCredit;
                if (balance <= 0)
                {
                    continue;
                }

                totalReceivable += balance;

                XmlNode dataLineNode = doc.CreateElement("tr");
                XmlNode dataItemNode = doc.CreateElement("td");

                dataLineNode.AppendChild(dataItemNode);

                string accountText = account.ID.ToString().PadLeft(5, '0') + " " + account.Name;

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
            double totalLiability = 0;
            accounts = this.accountingData.Accounts.Where(a => a.Type == AccountingDataAccountType.Debit || a.Type == AccountingDataAccountType.Credit);
            foreach (var account in accounts)
            {
                double saldoCredit = this.currentJournal.Booking
                    .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                double saldoDebit = this.currentJournal.Booking
                    .SelectMany(x => x.Debit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);

                double balance = saldoDebit - saldoCredit;
                if (balance >= 0)
                {
                    continue;
                }

                totalLiability += balance;

                XmlNode dataLineNode = doc.CreateElement("tr");
                XmlNode dataItemNode = doc.CreateElement("td");

                dataLineNode.AppendChild(dataItemNode);

                string accountText = account.ID.ToString().PadLeft(5, '0') + " " + account.Name;

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
            double totalAccount = 0;
            accounts = this.accountingData.Accounts.Where(a => a.Type == AccountingDataAccountType.Asset);
            foreach (var account in accounts)
            {
                double saldoCredit = this.currentJournal.Booking
                    .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                double saldoDebit = this.currentJournal.Booking
                    .SelectMany(x => x.Debit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);

                double balance = saldoDebit - saldoCredit;
                if (balance == 0)
                {
                    continue;
                }

                totalAccount += balance;

                XmlNode dataLineNode = doc.CreateElement("tr");
                XmlNode dataItemNode = doc.CreateElement("td");

                dataLineNode.AppendChild(dataItemNode);

                string accountText = account.ID.ToString().PadLeft(5, '0') + " " + account.Name;

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
            var acountNumber = Convert.ToUInt64(item.Text);
            this.RefreshAccount(acountNumber);
        }

        string BuildAccountDescription(string strAccountNumber)
        {
            var nAccountNumber = Convert.ToUInt32(strAccountNumber);
            string strAccountName = this.GetAccountName(nAccountNumber);
            return strAccountNumber + " (" + strAccountName + ")";
        }

        void SelectLastBookingYear()
        {
            var lastYear = this.accountingData.Years.LastOrDefault();
            if (lastYear != null)
            {
                this.SelectBookingYear(lastYear.Name);
            }
        }

        void SelectBookingYear(ushort newYear)
        {
            this.bookingYearName = newYear.ToString();
            this.currentJournal = this.accountingData.Journal.Single(y => y.Year == newYear);
            this.Text = "Buchhaltung - " + this.fileName + " - " + this.bookingYearName;
            this.RefreshJournal();
            this.listViewAccountJournal.Items.Clear();
        }

        void LoadDatabase(string fileName)
        {
            this.listViewAccounts.Items.Clear();

            this.fileName = fileName;
            this.accountingData = AccountingData.LoadFromFile(this.fileName);
            this.firmName = this.accountingData.Setup.Name;
            foreach (var account in this.accountingData.Accounts)
            {
                var item = new ListViewItem(account.ID.ToString());
                item.SubItems.Add(account.Name);
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

            this.accountingData.SaveToFile(this.fileName);
            this.isDocumentChanged = false;
        }

        void RefreshJournal()
        {
            this.listViewJournal.Items.Clear();
            bool bColorStatus = false;
            foreach (var booking in this.currentJournal.Booking.OrderBy(b => b.Date))
            {
                var item = new ListViewItem(booking.Date.ToString());
                if (bColorStatus)
                {
                    item.BackColor = Color.LightGreen;
                }

                bColorStatus = !bColorStatus;

                item.SubItems.Add(booking.ID.ToString());
                var debitAccounts = booking.Debit;
                var creditAccounts = booking.Credit;
                if (debitAccounts.Count == 1 && creditAccounts.Count == 1)
                {
                    var debit = debitAccounts[0];
                    item.SubItems.Add(debit.Text);
                    double nValue = Convert.ToDouble(debit.Value) / 100;
                    item.SubItems.Add(nValue.ToString("0.00"));
                    string accountNumber = debit.Account.ToString();
                    item.SubItems.Add(this.BuildAccountDescription(accountNumber));
                    accountNumber = creditAccounts[0].Account.ToString();
                    item.SubItems.Add(this.BuildAccountDescription(accountNumber));
                    this.listViewJournal.Items.Add(item);
                    continue;
                }

                foreach (var debitEntry in debitAccounts)
                {
                    var DebitItem = (ListViewItem)item.Clone();
                    DebitItem.SubItems.Add(debitEntry.Text);
                    double nValue = Convert.ToDouble(debitEntry.Value) / 100;
                    DebitItem.SubItems.Add(nValue.ToString("0.00"));
                    string strAccountNumber = debitEntry.Account.ToString();
                    DebitItem.SubItems.Add(this.BuildAccountDescription(strAccountNumber));
                    this.listViewJournal.Items.Add(DebitItem);
                }

                foreach (var creditEntry in creditAccounts)
                {
                    var CreditItem = (ListViewItem)item.Clone();
                    CreditItem.SubItems.Add(creditEntry.Text);
                    double nValue = Convert.ToDouble(creditEntry.Value) / 100;
                    CreditItem.SubItems.Add(nValue.ToString("0.00"));
                    CreditItem.SubItems.Add("");
                    string strAccountNumber = creditEntry.Account.ToString();
                    CreditItem.SubItems.Add(this.BuildAccountDescription(strAccountNumber));
                    this.listViewJournal.Items.Add(CreditItem);
                }
            }
        }

        void RefreshAccount(ulong accountNumber)
        {
            this.listViewAccountJournal.Items.Clear();
            double nCreditSum = 0;
            double nDebitSum = 0;
            bool bColorStatus = false;
            var entries =
                this.currentJournal.Booking.Where(b => b.Credit.Any(x => x.Account == accountNumber))
                .Concat(this.currentJournal.Booking.Where(b => b.Debit.Any(x => x.Account == accountNumber)));
            foreach (var entry in entries.OrderBy(x => x.Date))
            {
                var item = new ListViewItem(entry.Date.ToString());
                if (bColorStatus)
                {
                    item.BackColor = Color.LightGreen;
                }

                bColorStatus = !bColorStatus;

                item.SubItems.Add(entry.ID.ToString());
                var debitEntry = entry.Debit.FirstOrDefault(x => x.Account == accountNumber);
                if (debitEntry != null)
                {
                    item.SubItems.Add(debitEntry.Text);
                    double value = Convert.ToDouble(debitEntry.Value) / 100;
                    nDebitSum += value;
                    item.SubItems.Add(value.ToString("0.00"));
                    item.SubItems.Add("");
                    if (entry.Credit.Count == 1)
                    {
                        string creditAccount = entry.Credit[0].Account.ToString();
                        item.SubItems.Add(this.BuildAccountDescription(creditAccount));
                    }
                    else
                    {
                        item.SubItems.Add("Diverse");
                    }
                }
                else
                {
                    var creditEntry = entry.Credit.FirstOrDefault(x => x.Account == accountNumber);
                    item.SubItems.Add(creditEntry.Text);
                    item.SubItems.Add("");
                    double value = Convert.ToDouble(creditEntry.Value) / 100;
                    nCreditSum += value;
                    item.SubItems.Add(value.ToString("0.00"));
                    if (entry.Debit.Count == 1)
                    {
                        string debitAccount = entry.Debit[0].Account.ToString();
                        item.SubItems.Add(this.BuildAccountDescription(debitAccount));
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