// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Xml;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Properties;

    [SuppressMessage(
        "Major Code Smell",
        "S4055:Literals should not be passed as localized parameters",
        Justification = "pending translation")]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    [SuppressMessage("ReSharper", "CommentTypo")]
    internal class AnnualBalanceReport : ReportBase, IAnnualBalanceReport
    {
        public const string ResourceName = "AnnualBalance.xml";
        private const string FourthColumnExpression = "../columns/column[position()=4]";

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

        public void CreateReport(string title)
        {
            this.PreparePrintDocument(title);

            // income / Einnahmen
            this.ProcessIncome(out var totalIncome);

            // expense / Ausgaben
            this.ProcessExpenses(out var totalExpense);

            var balanceNode = this.PrintDocument.SelectSingleNode("//text[@ID=\"balance\"]");
            balanceNode.InnerText = (totalIncome + totalExpense).FormatCurrency(this.culture);

            // receivables / Forderungen
            // liabilities / Verbindlichkeiten
            this.ProcessReceivablesAndLiabilities(out var totalReceivable, out var totalLiability);

            // asset / Vermögen
            this.ProcessAssets(totalReceivable, totalLiability);
        }

        private void ProcessIncome(out long totalIncome)
        {
            var dataNode = this.PrintDocument.SelectSingleNode("//table/data[@target='income']");
            totalIncome = 0;
            var accounts = this.allAccounts.Where(a => a.Type == AccountDefinitionType.Income);
            foreach (var account in accounts)
            {
                long balance = this.GetAccountBalance(account, creditFromDebit: true);
                if (balance == 0)
                {
                    continue;
                }

                totalIncome += balance;

                dataNode.AppendChild(this.CreateAccountBalanceNode(account, balance));
            }

            var balanceElement = dataNode.SelectSingleNode(FourthColumnExpression);
            balanceElement.InnerText = totalIncome.FormatCurrency(this.culture);
        }

        private void ProcessExpenses(out long totalExpense)
        {
            var dataNode = this.PrintDocument.SelectSingleNode("//table/data[@target='expense']");
            totalExpense = 0;
            var accounts = this.allAccounts.Where(a => a.Type == AccountDefinitionType.Expense);
            foreach (var account in accounts)
            {
                long balance = this.GetAccountBalance(account, creditFromDebit: true);
                if (balance == 0)
                {
                    continue;
                }

                totalExpense += balance;

                dataNode.AppendChild(this.CreateAccountBalanceNode(account, balance));
            }

            var balanceElement = dataNode.SelectSingleNode(FourthColumnExpression);
            balanceElement.InnerText = totalExpense.FormatCurrency(this.culture);
        }

        private void ProcessReceivablesAndLiabilities(out long totalReceivable, out long totalLiability)
        {
            var receivableNode = this.PrintDocument.SelectSingleNode("//table/data[@target='receivable']");
            var liabilityNode = this.PrintDocument.SelectSingleNode("//table/data[@target='liability']");
            totalReceivable = 0;
            totalLiability = 0;
            var accounts = this.allAccounts.Where(
                a =>
                    a.Type == AccountDefinitionType.Debit || a.Type == AccountDefinitionType.Credit);
            foreach (var account in accounts)
            {
                long balance = this.GetAccountBalance(account, creditFromDebit: false);
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

            var balanceElement = receivableNode.SelectSingleNode(FourthColumnExpression);
            balanceElement.InnerText = totalReceivable.FormatCurrency(this.culture);
            balanceElement = liabilityNode.SelectSingleNode(FourthColumnExpression);
            balanceElement.InnerText = totalLiability.FormatCurrency(this.culture);
        }

        private void ProcessAssets(long totalReceivable, long totalLiability)
        {
            var dataNode = this.PrintDocument.SelectSingleNode("//table/data[@target='asset']");
            long totalAccount = 0;
            var accounts = this.allAccounts.Where(a => a.Type == AccountDefinitionType.Asset);
            foreach (var account in accounts)
            {
                long balance = this.GetAccountBalance(account, creditFromDebit: false);
                if (balance == 0)
                {
                    continue;
                }

                totalAccount += balance;

                dataNode.AppendChild(this.CreateAccountBalanceNode(account, balance));
            }

            if (totalReceivable > 0)
            {
                dataNode.AppendChild(this.CreateBalanceNode(Resources.Word_Receivables, totalReceivable));
                totalAccount += totalReceivable;
            }

            if (totalLiability < 0)
            {
                dataNode.AppendChild(this.CreateBalanceNode(Resources.Word_Liabilities, totalLiability));
                totalAccount += totalLiability;
            }

            var balanceElement = dataNode.SelectSingleNode(FourthColumnExpression);
            balanceElement.InnerText = totalAccount.FormatCurrency(this.culture);
        }

        private long GetAccountBalance(AccountDefinition account, bool creditFromDebit)
        {
            long creditBalance = this.YearData.Booking
                .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
            long debitBalance = this.YearData.Booking
                .SelectMany(x => x.Debit.Where(y => y.Account == account.ID))
                .DefaultIfEmpty().Sum(x => x?.Value ?? 0);

            if (creditFromDebit)
            {
                return creditBalance - debitBalance;
            }

            return debitBalance - creditBalance;
        }

        private XmlNode CreateAccountBalanceNode(AccountDefinition account, long balance)
        {
            string accountText = account.ID.ToString(this.culture).PadLeft(5, '0') + " " + account.Name;

            return this.CreateBalanceNode(accountText, balance);
        }

        private XmlNode CreateBalanceNode(string balanceText, long balance)
        {
            XmlNode dataLineNode = this.PrintDocument.CreateElement("tr");
            XmlNode dataItemNode = this.PrintDocument.CreateElement("td");

            dataLineNode.AppendChild(dataItemNode);

            dataItemNode = dataItemNode.Clone();
            dataItemNode.InnerText = balanceText;
            dataLineNode.AppendChild(dataItemNode);

            dataItemNode = dataItemNode.Clone();
            dataItemNode.InnerText = balance.FormatCurrency(this.culture);
            dataLineNode.AppendChild(dataItemNode);

            return dataLineNode;
        }
    }
}
