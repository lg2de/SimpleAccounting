// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Xml;
    using lg2de.SimpleAccounting.Model;

#pragma warning disable S4055 // string literals => pending translation

    internal class AnnualBalanceReport : ReportBase
    {
        public const string ResourceName = "AnnualBalance.xml";

        private readonly List<AccountDefinition> allAccounts;
        private readonly CultureInfo culture;

        public AnnualBalanceReport(
            AccountingDataJournal yearData,
            IEnumerable<AccountDefinition> accounts,
            AccountingDataSetup setup,
            CultureInfo culture)
            : base(ResourceName, setup, yearData, culture)
        {
            this.allAccounts = accounts.ToList();
            this.culture = culture;
        }

        public void CreateReport()
        {
            this.PreparePrintDocument();

            // income / Einnahmen
            this.ProcessIncome(out var totalIncome);

            // expense / Ausgaben
            this.ProcessExpenses(out var totalExpense);

            var saldoNode = this.PrintDocument.SelectSingleNode("//text[@ID=\"saldo\"]");
            saldoNode.InnerText = ((totalIncome + totalExpense) / 100).ToString("0.00", this.culture);

            // receivables / Forderungen
            // liabilities / Verbindlichkeiten
            this.ProcessReceivablesAndLiabilities(out double totalReceivable, out double totalLiability);

            // asset / Vermögen
            this.ProcessAssets(totalReceivable, totalLiability);
        }

        private void ProcessIncome(out double totalIncome)
        {
            var dataNode = this.PrintDocument.SelectSingleNode("//table/data[@target='income']");
            totalIncome = 0;
            var accounts = this.allAccounts.Where(a => a.Type == AccountDefinitionType.Income);
            foreach (var account in accounts)
            {
                double balance = this.GetAccountBalance(account, creditFromDebit: true);
                if (balance == 0)
                {
                    continue;
                }

                totalIncome += balance;

                dataNode.AppendChild(this.CreateAccountBalanceNode(account, balance));
            }

            var saldoElement = dataNode.SelectSingleNode("../columns/column[position()=4]");
            saldoElement.InnerText = (totalIncome / 100).ToString("0.00", this.culture);
        }

        private void ProcessExpenses(out double totalExpense)
        {
            var dataNode = this.PrintDocument.SelectSingleNode("//table/data[@target='expense']");
            totalExpense = 0;
            var accounts = this.allAccounts.Where(a => a.Type == AccountDefinitionType.Expense);
            foreach (var account in accounts)
            {
                double balance = this.GetAccountBalance(account, creditFromDebit: true);
                if (balance == 0)
                {
                    continue;
                }

                totalExpense += balance;

                dataNode.AppendChild(this.CreateAccountBalanceNode(account, balance));
            }

            var saldoElement = dataNode.SelectSingleNode("../columns/column[position()=4]");
            saldoElement.InnerText = (totalExpense / 100).ToString("0.00", this.culture);
        }

        private void ProcessReceivablesAndLiabilities(out double totalReceivable, out double totalLiability)
        {
            var receivableNode = this.PrintDocument.SelectSingleNode("//table/data[@target='receivable']");
            var liabilityNode = this.PrintDocument.SelectSingleNode("//table/data[@target='liability']");
            totalReceivable = 0;
            totalLiability = 0;
            var accounts = this.allAccounts.Where(a => a.Type == AccountDefinitionType.Debit || a.Type == AccountDefinitionType.Credit);
            foreach (var account in accounts)
            {
                double balance = this.GetAccountBalance(account, creditFromDebit: false);
                if (balance > 0)
                {
                    totalReceivable += balance;
                    receivableNode.AppendChild(this.CreateAccountBalanceNode(account, balance));
                }
                else if (balance < 0)
                {
                    totalLiability += balance;
                    liabilityNode.AppendChild(this.CreateAccountBalanceNode(account, balance));
                }
            }

            var saldoElement = receivableNode.SelectSingleNode("../columns/column[position()=4]");
            saldoElement.InnerText = (totalReceivable / 100).ToString("0.00", this.culture);
            saldoElement = liabilityNode.SelectSingleNode("../columns/column[position()=4]");
            saldoElement.InnerText = (totalLiability / 100).ToString("0.00", this.culture);
        }

        private void ProcessAssets(double totalReceivable, double totalLiability)
        {
            var dataNode = this.PrintDocument.SelectSingleNode("//table/data[@target='asset']");
            double totalAccount = 0;
            var accounts = this.allAccounts.Where(a => a.Type == AccountDefinitionType.Asset);
            foreach (var account in accounts)
            {
                double balance = this.GetAccountBalance(account, creditFromDebit: false);
                if (balance == 0)
                {
                    continue;
                }

                totalAccount += balance;

                dataNode.AppendChild(this.CreateAccountBalanceNode(account, balance));
            }

            if (totalReceivable > 0)
            {
                dataNode.AppendChild(this.CreateBalanceNode("Forderungen", totalReceivable));
                totalAccount += totalReceivable;
            }

            if (totalLiability < 0)
            {
                dataNode.AppendChild(this.CreateBalanceNode("Verbindlichkeiten", totalLiability));
                totalAccount += totalLiability;
            }

            var saldoElement = dataNode.SelectSingleNode("../columns/column[position()=4]");
            saldoElement.InnerText = (totalAccount / 100).ToString("0.00", this.culture);
        }

        private double GetAccountBalance(AccountDefinition account, bool creditFromDebit)
        {
            double saldoCredit = this.YearData.Booking
                .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
            double saldoDebit = this.YearData.Booking
                .SelectMany(x => x.Debit.Where(y => y.Account == account.ID))
                .DefaultIfEmpty().Sum(x => x?.Value ?? 0);

            if (creditFromDebit)
            {
                return saldoCredit - saldoDebit;
            }

            return saldoDebit - saldoCredit;
        }

        private XmlNode CreateAccountBalanceNode(AccountDefinition account, double balance)
        {
            string accountText = account.ID.ToString(this.culture).PadLeft(5, '0') + " " + account.Name;

            return this.CreateBalanceNode(accountText, balance);
        }

        private XmlNode CreateBalanceNode(string balanceText, double balance)
        {
            XmlNode dataLineNode = this.PrintDocument.CreateElement("tr");
            XmlNode dataItemNode = this.PrintDocument.CreateElement("td");

            dataLineNode.AppendChild(dataItemNode);

            dataItemNode = dataItemNode.Clone();
            dataItemNode.InnerText = balanceText;
            dataLineNode.AppendChild(dataItemNode);

            dataItemNode = dataItemNode.Clone();
            dataItemNode.InnerText = (balance / 100).ToString("0.00", this.culture);
            dataLineNode.AppendChild(dataItemNode);

            return dataLineNode;
        }
    }
}
