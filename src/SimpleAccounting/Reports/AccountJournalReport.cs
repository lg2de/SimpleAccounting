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
    using System.Xml.Linq;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Model;

    internal class AccountJournalReport
    {
        public const string ResourceName = "AccountJournal.xml";

        private readonly IEnumerable<AccountDefinition> accounts;
        private readonly AccountingDataJournal journal;
        private readonly AccountingDataSetup setup;
        private readonly CultureInfo culture;

        private XmlPrinter printer;

        public AccountJournalReport(
            IEnumerable<AccountDefinition> accounts,
            AccountingDataJournal journal,
            AccountingDataSetup setup,
            CultureInfo culture)
        {
            this.accounts = accounts.OrderBy(a => a.ID);
            this.journal = journal;
            this.setup = setup;
            this.culture = culture;
        }

        /// <summary>
        ///     Gets or sets a value indicating whether account reports should be separated by page break.
        /// </summary>
        public bool PageBreakBetweenAccounts { get; set; }

        internal XDocument Document => XDocument.Parse(this.printer.Document.OuterXml);

        public void CreateReport(DateTime dateStart, DateTime dateEnd)
        {
            this.printer = new XmlPrinter();
            this.printer.LoadDocument(ResourceName);

            XmlDocument doc = this.printer.Document;

            XmlNode firmNode = doc.SelectSingleNode("//text[@ID=\"firm\"]");
            firmNode.InnerText = this.setup.Name;

            XmlNode rangeNode = doc.SelectSingleNode("//text[@ID=\"range\"]");
            rangeNode.InnerText = dateStart.ToString("d", this.culture) + " - " + dateEnd.ToString("d", this.culture);

            var dateNode = doc.SelectSingleNode("//text[@ID=\"date\"]");
            dateNode.InnerText = this.setup.Location + ", " + DateTime.Now.ToString("D", this.culture);

            XmlNode tableNode = doc.SelectSingleNode("//table");

            bool firstAccount = true;

            foreach (var account in this.accounts)
            {
                var accountEntries = this.journal.Booking
                    .Where(x => x.Debit.Any(a => a.Account == account.ID) || x.Credit.Any(a => a.Account == account.ID))
                    .OrderBy(x => x.Date)
                    .ToList();

                if (accountEntries.Count == 0)
                {
                    // ignore
                    continue;
                }

                if (!firstAccount)
                {
                    XmlNode separatorNode;
                    if (this.PageBreakBetweenAccounts)
                    {
                        separatorNode = doc.CreateElement("newPage");
                    }
                    else
                    {
                        separatorNode = doc.CreateElement("move");
                        separatorNode.SetAttribute("relY", "5");
                    }

                    tableNode.ParentNode.InsertBefore(separatorNode, tableNode);
                }

                var titleFont = doc.CreateElement("font");
                titleFont.SetAttribute("size", 10);
                titleFont.SetAttribute("bold", 1);
                tableNode.ParentNode.InsertBefore(titleFont, tableNode);

                var titleNode = doc.CreateElement("text");
                titleNode.InnerText = account.FormatName();
                titleFont.AppendChild(titleNode);

                var moveNode = doc.CreateElement("move");
                moveNode.SetAttribute("relY", "5");
                tableNode.ParentNode.InsertBefore(moveNode, tableNode);

                var newTable = tableNode.CloneNode(deep: true);
                tableNode.ParentNode.InsertBefore(newTable, tableNode);

                var dataNode = newTable.SelectSingleNode("data");

                double nCreditSum = 0;
                double nDebitSum = 0;

                foreach (var entry in accountEntries)
                {
                    XmlNode dataLineNode = doc.CreateElement("tr");
                    dataLineNode.SetAttribute("topLine", true);
                    dataNode.AppendChild(dataLineNode);
                    XmlNode dataItemNode = doc.CreateElement("td");
                    dataItemNode.InnerText = entry.Date.ToDateTime().ToString("d", this.culture);
                    dataLineNode.AppendChild(dataItemNode);
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = entry.ID.ToString();
                    dataLineNode.AppendChild(dataItemNode);

                    var debitEntry = entry.Debit.FirstOrDefault(x => x.Account == account.ID);
                    if (debitEntry != null)
                    {
                        dataItemNode = dataItemNode.Clone();
                        dataItemNode.InnerText = debitEntry.Text;
                        dataLineNode.AppendChild(dataItemNode);

                        double nValue = Convert.ToDouble(debitEntry.Value) / 100;
                        nDebitSum += nValue;
                        dataItemNode = dataItemNode.Clone();
                        dataItemNode.InnerText = nValue.ToString("0.00", this.culture);
                        dataLineNode.AppendChild(dataItemNode);

                        dataItemNode = doc.CreateElement("td");
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
                    }
                    else
                    {
                        var creditEntry = entry.Credit.FirstOrDefault(x => x.Account == account.ID);

                        dataItemNode = dataItemNode.Clone();
                        dataItemNode.InnerText = creditEntry.Text;
                        dataLineNode.AppendChild(dataItemNode);

                        dataItemNode = doc.CreateElement("td");
                        dataLineNode.AppendChild(dataItemNode);

                        double nValue = Convert.ToDouble(creditEntry.Value) / 100;
                        nCreditSum += nValue;
                        dataItemNode = dataItemNode.Clone();
                        dataItemNode.InnerText = nValue.ToString("0.00", this.culture);
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
                    }
                }

                // sum
                if (nDebitSum > 0 && nCreditSum > 0)
                {
                    XmlNode sumLineNode = doc.CreateElement("tr");
                    sumLineNode.SetAttribute("topLine", true);
                    dataNode.AppendChild(sumLineNode);
                    XmlNode sumItemNode = doc.CreateElement("td");
                    sumItemNode.InnerText = "Summen";
                    sumItemNode.SetAttribute("align", "right");
                    sumLineNode.AppendChild(sumItemNode);
                    sumLineNode.AppendChild(doc.CreateElement("td")); // id
                    sumLineNode.AppendChild(doc.CreateElement("td")); // text
                    sumItemNode = doc.CreateElement("td");
                    sumItemNode.InnerText = nDebitSum.ToString("0.00", this.culture);
                    sumLineNode.AppendChild(sumItemNode);
                    sumItemNode = sumItemNode.Clone();
                    sumItemNode.InnerText = nCreditSum.ToString("0.00", this.culture);
                    sumLineNode.AppendChild(sumItemNode);
                    sumLineNode.AppendChild(doc.CreateElement("td")); // remote
                }

                // saldo
                XmlNode saldoLineNode = doc.CreateElement("tr");
                saldoLineNode.SetAttribute("topLine", true);
                dataNode.AppendChild(saldoLineNode);
                XmlNode saldoItemNode = doc.CreateElement("td");
                saldoItemNode.InnerText = "Saldo";
                saldoItemNode.SetAttribute("align", "right");
                saldoLineNode.AppendChild(saldoItemNode);
                saldoLineNode.AppendChild(doc.CreateElement("td")); // id
                saldoLineNode.AppendChild(doc.CreateElement("td")); // text
                if (nDebitSum > nCreditSum)
                {
                    saldoItemNode = doc.CreateElement("td");
                    saldoItemNode.InnerText = (nDebitSum - nCreditSum).ToString("0.00", this.culture);
                    saldoLineNode.AppendChild(saldoItemNode);
                    saldoLineNode.AppendChild(doc.CreateElement("td"));
                }
                else
                {
                    saldoLineNode.AppendChild(doc.CreateElement("td"));
                    saldoItemNode = doc.CreateElement("td");
                    saldoItemNode.InnerText = (nCreditSum - nDebitSum).ToString("0.00", this.culture);
                    saldoLineNode.AppendChild(saldoItemNode);
                }

                // empty remote account column
                saldoLineNode.AppendChild(doc.CreateElement("td"));

                firstAccount = false;
            }

            tableNode.ParentNode.RemoveChild(tableNode);
        }

        public void ShowPreview(string documentName)
        {
            this.printer.PrintDocument(documentName);
        }
    }
}