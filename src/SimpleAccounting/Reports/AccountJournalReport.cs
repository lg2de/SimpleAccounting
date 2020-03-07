// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Xml;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Model;

#pragma warning disable S4055 // string literals => pending translation

    internal class AccountJournalReport : ReportBase, IAccountJournalReport
    {
        public const string ResourceName = "AccountJournal.xml";

        private readonly IEnumerable<AccountDefinition> accounts;
        private readonly CultureInfo culture;
        private bool firstAccount;
        private double creditSum;
        private double debitSum;

        public AccountJournalReport(
            IEnumerable<AccountDefinition> accounts,
            AccountingDataJournal yearData,
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

        public void CreateReport()
        {
            this.PreparePrintDocument();

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
                titleFont.SetAttribute("size", 10);
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
                    dataNode.AppendChild(this.CreateBookingEntry(entry, account.ID));
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
                    sumItemNode.InnerText = this.debitSum.ToString("0.00", this.culture);
                    sumLineNode.AppendChild(sumItemNode);
                    sumItemNode = sumItemNode.Clone();
                    sumItemNode.InnerText = this.creditSum.ToString("0.00", this.culture);
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
                    saldoItemNode.InnerText = (this.debitSum - this.creditSum).ToString("0.00", this.culture);
                    saldoLineNode.AppendChild(saldoItemNode);
                    saldoLineNode.AppendChild(this.PrintDocument.CreateElement("td"));
                }
                else
                {
                    saldoLineNode.AppendChild(this.PrintDocument.CreateElement("td"));
                    saldoItemNode = this.PrintDocument.CreateElement("td");
                    saldoItemNode.InnerText = (this.creditSum - this.debitSum).ToString("0.00", this.culture);
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

        private XmlNode CreateBookingEntry(AccountingDataJournalBooking entry, ulong accountIdentifier)
        {
            XmlNode dataLineNode = this.PrintDocument.CreateElement("tr");
            dataLineNode.SetAttribute("topLine", true);
            XmlNode dataItemNode = this.PrintDocument.CreateElement("td");
            dataItemNode.InnerText = entry.Date.ToDateTime().ToString("d", this.culture);
            dataLineNode.AppendChild(dataItemNode);
            dataItemNode = dataItemNode.Clone();
            dataItemNode.InnerText = entry.ID.ToString();
            dataLineNode.AppendChild(dataItemNode);

            var debitEntry = entry.Debit.FirstOrDefault(x => x.Account == accountIdentifier);
            if (debitEntry != null)
            {
                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = debitEntry.Text;
                dataLineNode.AppendChild(dataItemNode);

                double debitValue = Convert.ToDouble(debitEntry.Value) / 100;
                this.debitSum += debitValue;
                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = debitValue.ToString("0.00", this.culture);
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = this.PrintDocument.CreateElement("td");
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataLineNode.AppendChild(dataItemNode);

                if (entry.Credit.Count == 1)
                {
                    dataItemNode.InnerText = entry.Credit[0].Account.ToString();
                }
                else
                {
                    dataItemNode.InnerText = "Diverse";
                }

                return dataLineNode;
            }

            var creditEntry = entry.Credit.FirstOrDefault(x => x.Account == accountIdentifier);

            dataItemNode = dataItemNode.Clone();
            dataItemNode.InnerText = creditEntry.Text;
            dataLineNode.AppendChild(dataItemNode);

            dataItemNode = this.PrintDocument.CreateElement("td");
            dataLineNode.AppendChild(dataItemNode);

            double creditValue = Convert.ToDouble(creditEntry.Value) / 100;
            this.creditSum += creditValue;
            dataItemNode = dataItemNode.Clone();
            dataItemNode.InnerText = creditValue.ToString("0.00", this.culture);
            dataLineNode.AppendChild(dataItemNode);

            dataItemNode = dataItemNode.Clone();
            dataLineNode.AppendChild(dataItemNode);

            if (entry.Credit.Count == 1)
            {
                dataItemNode.InnerText = entry.Debit[0].Account.ToString();
            }
            else
            {
                dataItemNode.InnerText = "Diverse";
            }

            return dataLineNode;
        }
    }
}