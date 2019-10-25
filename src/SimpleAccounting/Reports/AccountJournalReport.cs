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

                var titleNode = doc.CreateElement("text");
                titleNode.InnerText = account.FormatName();
                tableNode.ParentNode.InsertBefore(titleNode, tableNode);

                var moveNode = doc.CreateElement("move");
                moveNode.SetAttribute("relY", "5");
                tableNode.ParentNode.InsertBefore(moveNode, tableNode);

                var newTable = tableNode.CloneNode(deep: true);
                tableNode.ParentNode.InsertBefore(newTable, tableNode);

                var dataNode = newTable.SelectSingleNode("data");

                foreach (var entry in accountEntries)
                {
                    XmlNode dataLineNode = doc.CreateElement("tr");
                    dataNode.AppendChild(dataLineNode);
                    XmlNode dataItemNode = doc.CreateElement("td");
                    dataLineNode.SetAttribute("topLine", true);
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

                moveNode = doc.CreateElement("move");
                moveNode.SetAttribute("relY", "5");
                tableNode.ParentNode.InsertBefore(moveNode, tableNode);
            }

            tableNode.ParentNode.RemoveChild(tableNode);
        }

        public void ShowPreview(string documentName)
        {
            this.printer.PrintDocument(documentName);
        }
    }
}