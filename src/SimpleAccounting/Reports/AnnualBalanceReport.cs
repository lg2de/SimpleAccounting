// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using lg2de.SimpleAccounting.Model;

    internal class AnnualBalanceReport
    {
        public const string ResourceName = "AnnualBalance.xml";

        private readonly AccountingDataJournal journal;
        private readonly List<AccountDefinition> allAccounts;
        private readonly AccountingDataSetup setup;
        private readonly string bookingYearName;

        public AnnualBalanceReport(
            AccountingDataJournal journal,
            IEnumerable<AccountDefinition> accounts,
            AccountingDataSetup setup,
            string bookingYearName)
        {
            this.journal = journal;
            this.allAccounts = accounts.ToList();
            this.setup = setup;
            this.bookingYearName = bookingYearName;
        }

        public void CreateReport()
        {
            var print = new XmlPrinter();
            print.LoadDocument(ResourceName);

            XmlDocument doc = print.Document;

            var firmNode = doc.SelectSingleNode("//text[@ID=\"firm\"]");
            firmNode.InnerText = this.setup.Name;

            var rangeNode = doc.SelectSingleNode("//text[@ID=\"range\"]");
            rangeNode.InnerText = this.bookingYearName;

            var dateNode = doc.SelectSingleNode("//text[@ID=\"date\"]");
            dateNode.InnerText = this.setup.Location + ", " + DateTime.Now.ToLongDateString();

            // income / Einnahmen
            this.ProcessIncome(doc, out var totalIncome);

            // expense / Ausgaben
            this.ProcessExpenses(doc, out var totalExpense);

            var saldoNode = doc.SelectSingleNode("//text[@ID=\"saldo\"]");
            saldoNode.InnerText = ((totalIncome + totalExpense) / 100).ToString("0.00");

            // receivables / Forderungen
            this.ProcessReceivables(doc, out double totalReceivable);

            // liabilities / Verbindlichkeiten
            this.ProcessLiabilities(doc, out double totalLiability);

            // asset / Vermögen
            this.ProcessAssets(doc, totalReceivable, totalLiability);

            print.PrintDocument(DateTime.Now.ToString("yyyy-MM-dd") + " Jahresbilanz " + this.bookingYearName);
        }

        private void ProcessIncome(XmlDocument doc, out double totalIncome)
        {
            var dataNode = doc.SelectSingleNode("//table/data[@target='income']");
            totalIncome = 0;
            var accounts = this.allAccounts.Where(a => a.Type == AccountDefinitionType.Income);
            foreach (var account in accounts)
            {
                double saldoCredit = this.journal.Booking
                    .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                double saldoDebit = this.journal.Booking
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
        }

        private void ProcessExpenses(XmlDocument doc, out double totalExpense)
        {
            var dataNode = doc.SelectSingleNode("//table/data[@target='expense']");
            totalExpense = 0;
            var accounts = this.allAccounts.Where(a => a.Type == AccountDefinitionType.Expense);
            foreach (var account in accounts)
            {
                double saldoCredit = this.journal.Booking
                    .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                double saldoDebit = this.journal.Booking
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

            var saldoElement = dataNode.SelectSingleNode("../columns/column[position()=4]");
            saldoElement.InnerText = (totalExpense / 100).ToString("0.00");
        }

        private void ProcessReceivables(XmlDocument doc, out double totalReceivable)
        {
            var dataNode = doc.SelectSingleNode("//table/data[@target='receivable']");
            totalReceivable = 0;
            var accounts = this.allAccounts.Where(a => a.Type == AccountDefinitionType.Debit || a.Type == AccountDefinitionType.Credit);
            foreach (var account in accounts)
            {
                double saldoCredit = this.journal.Booking
                    .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                double saldoDebit = this.journal.Booking
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

            var saldoElement = dataNode.SelectSingleNode("../columns/column[position()=4]");
            saldoElement.InnerText = (totalReceivable / 100).ToString("0.00");
        }

        private void ProcessLiabilities(XmlDocument doc, out double totalLiability)
        {
            var dataNode = doc.SelectSingleNode("//table/data[@target='liability']");
            totalLiability = 0;
            var accounts = this.allAccounts.Where(a => a.Type == AccountDefinitionType.Debit || a.Type == AccountDefinitionType.Credit);
            foreach (var account in accounts)
            {
                double saldoCredit = this.journal.Booking
                    .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                double saldoDebit = this.journal.Booking
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

            var saldoElement = dataNode.SelectSingleNode("../columns/column[position()=4]");
            saldoElement.InnerText = (totalLiability / 100).ToString("0.00");
        }

        private void ProcessAssets(XmlDocument doc, double totalReceivable, double totalLiability)
        {
            var dataNode = doc.SelectSingleNode("//table/data[@target='asset']");
            double totalAccount = 0;
            var accounts = this.allAccounts.Where(a => a.Type == AccountDefinitionType.Asset);
            foreach (var account in accounts)
            {
                double saldoCredit = this.journal.Booking
                    .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                double saldoDebit = this.journal.Booking
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

            var saldoElement = dataNode.SelectSingleNode("../columns/column[position()=4]");
            saldoElement.InnerText = (totalAccount / 100).ToString("0.00");
        }
    }
}
