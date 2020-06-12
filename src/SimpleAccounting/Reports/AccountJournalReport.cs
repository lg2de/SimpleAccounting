// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Xml;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Model;

    [SuppressMessage(
        "Major Code Smell",
        "S4055:Literals should not be passed as localized parameters",
        Justification = "pending translation")]
    internal class AccountJournalReport : ReportBase, IAccountJournalReport
    {
        public const string ResourceName = "AccountJournal.xml";

        private readonly IEnumerable<AccountDefinition> accounts;
        private readonly CultureInfo culture;
        private double creditSum;
        private double debitSum;
        private bool firstAccount;

        public AccountJournalReport(
            AccountingDataJournal yearData,
            IEnumerable<AccountDefinition> accounts,
            AccountingDataSetup setup,
            CultureInfo culture)
            : base(ResourceName, setup, yearData, culture)
        {
            this.accounts = accounts.OrderBy(a => a.ID);
            this.culture = culture;
        }

        /// <summary>
        ///     Gets or sets a value indicating whether account reports should be separated by page break.
        /// </summary>
        public bool PageBreakBetweenAccounts { get; set; }

        public void CreateReport(string title)
        {
            this.PreparePrintDocument(title);

            XmlNode tableNode = this.PrintDocument.SelectSingleNode("//table");

            this.firstAccount = true;

            foreach (var account in this.accounts)
            {
                var accountEntries = this.YearData.Booking
                    .Where(x => x.Debit.Any(a => a.Account == account.ID) || x.Credit.Any(a => a.Account == account.ID))
                    .OrderBy(x => x.Date)
                    .ToList();

                if (accountEntries.Count == 0)
                {
                    // ignore
                    continue;
                }

                this.AddAccountSeparator(tableNode);

                var titleFont = this.PrintDocument.CreateElement("font");
                titleFont.SetAttribute("size", TitleSize);
                titleFont.SetAttribute("bold", 1);
                tableNode.ParentNode.InsertBefore(titleFont, tableNode);

                var titleNode = this.PrintDocument.CreateElement("text");
                titleNode.InnerText = account.FormatName();
                titleFont.AppendChild(titleNode);

                var moveNode = this.PrintDocument.CreateElement("move");
                moveNode.SetAttribute("relY", "5");
                tableNode.ParentNode.InsertBefore(moveNode, tableNode);

                var newTable = tableNode.CloneNode(deep: true);
                tableNode.ParentNode.InsertBefore(newTable, tableNode);

                var dataNode = newTable.SelectSingleNode("data");

                this.creditSum = 0;
                this.debitSum = 0;

                foreach (var entry in accountEntries)
                {
                    this.AddBookingEntries(entry, account.ID, dataNode);
                }

                // sum
                if (this.debitSum > 0 && this.creditSum > 0)
                {
                    XmlNode sumLineNode = this.PrintDocument.CreateElement("tr");
                    sumLineNode.SetAttribute("topLine", true);
                    dataNode.AppendChild(sumLineNode);
                    XmlNode sumItemNode = this.PrintDocument.CreateElement("td");
                    sumItemNode.InnerText = "Summen";
                    sumItemNode.SetAttribute("align", "right");
                    sumLineNode.AppendChild(sumItemNode);
                    sumLineNode.AppendChild(this.PrintDocument.CreateElement("td")); // id
                    sumLineNode.AppendChild(this.PrintDocument.CreateElement("td")); // text
                    sumItemNode = this.PrintDocument.CreateElement("td");
                    sumItemNode.InnerText = this.debitSum.FormatCurrency(this.culture);
                    sumLineNode.AppendChild(sumItemNode);
                    sumItemNode = sumItemNode.Clone();
                    sumItemNode.InnerText = this.creditSum.FormatCurrency(this.culture);
                    sumLineNode.AppendChild(sumItemNode);
                    sumLineNode.AppendChild(this.PrintDocument.CreateElement("td")); // remote
                }

                // saldo
                XmlNode saldoLineNode = this.PrintDocument.CreateElement("tr");
                saldoLineNode.SetAttribute("topLine", true);
                dataNode.AppendChild(saldoLineNode);
                XmlNode saldoItemNode = this.PrintDocument.CreateElement("td");
                saldoItemNode.InnerText = "Saldo";
                saldoItemNode.SetAttribute("align", "right");
                saldoLineNode.AppendChild(saldoItemNode);
                saldoLineNode.AppendChild(this.PrintDocument.CreateElement("td")); // id
                saldoLineNode.AppendChild(this.PrintDocument.CreateElement("td")); // text
                if (this.debitSum > this.creditSum)
                {
                    saldoItemNode = this.PrintDocument.CreateElement("td");
                    saldoItemNode.InnerText = (this.debitSum - this.creditSum).FormatCurrency(this.culture);
                    saldoLineNode.AppendChild(saldoItemNode);
                    saldoLineNode.AppendChild(this.PrintDocument.CreateElement("td"));
                }
                else
                {
                    saldoLineNode.AppendChild(this.PrintDocument.CreateElement("td"));
                    saldoItemNode = this.PrintDocument.CreateElement("td");
                    saldoItemNode.InnerText = (this.creditSum - this.debitSum).FormatCurrency(this.culture);
                    saldoLineNode.AppendChild(saldoItemNode);
                }

                // empty remote account column
                saldoLineNode.AppendChild(this.PrintDocument.CreateElement("td"));
            }

            tableNode.ParentNode.RemoveChild(tableNode);
        }

        private void AddAccountSeparator(XmlNode tableNode)
        {
            if (this.firstAccount)
            {
                // skip separator for (before) first account
                this.firstAccount = false;
                return;
            }

            XmlNode separatorNode;
            if (this.PageBreakBetweenAccounts)
            {
                separatorNode = this.PrintDocument.CreateElement("newPage");
            }
            else
            {
                separatorNode = this.PrintDocument.CreateElement("move");
                separatorNode.SetAttribute("relY", "5");
            }

            tableNode.ParentNode.InsertBefore(separatorNode, tableNode);
        }

        private void AddBookingEntries(
            AccountingDataJournalBooking entry, ulong accountIdentifier, XmlNode dataNode)
        {
            XmlNode lineTemplate = this.PrintDocument.CreateElement("tr");
            lineTemplate.SetAttribute("topLine", true);
            lineTemplate.AddTableNode(entry.Date.ToDateTime().ToString("d", this.culture));
            lineTemplate.AddTableNode(entry.ID.ToString(CultureInfo.InvariantCulture));

            var debitEntries = entry.Debit.Where(x => x.Account == accountIdentifier);
            foreach (var debitEntry in debitEntries)
            {
                var lineNode = lineTemplate.Clone();
                lineNode.AddTableNode(debitEntry.Text);
                double debitValue = Convert.ToDouble(debitEntry.Value) / 100;
                lineNode.AddTableNode(debitValue.FormatCurrency(this.culture));
                this.debitSum += debitValue;
                lineNode.AddTableNode(string.Empty);
                lineNode.AddTableNode(
                    entry.Credit.Count == 1
                        ? entry.Credit[0].Account.ToString(CultureInfo.InvariantCulture)
                        : "Diverse");
                dataNode.AppendChild(lineNode);
            }

            var creditEntries = entry.Credit.Where(x => x.Account == accountIdentifier);
            foreach (var creditEntry in creditEntries)
            {
                var lineNode = lineTemplate.Clone();
                lineNode.AddTableNode(creditEntry.Text);
                lineNode.AddTableNode(string.Empty);
                double creditValue = Convert.ToDouble(creditEntry.Value) / 100;
                lineNode.AddTableNode(creditValue.FormatCurrency(this.culture));
                this.creditSum += creditValue;
                lineNode.AddTableNode(
                    entry.Debit.Count == 1
                        ? entry.Debit[0].Account.ToString(CultureInfo.InvariantCulture)
                        : "Diverse");
                dataNode.AppendChild(lineNode);
            }
        }
    }
}
