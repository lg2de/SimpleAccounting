// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Model;

    internal class TotalJournalReport
    {
        public const string ResourceName = "TotalJournal.xml";

        private readonly AccountingDataJournal journal;
        private readonly AccountingDataSetup setup;
        private readonly CultureInfo culture;

        private XmlPrinter printer;

        public TotalJournalReport(
            AccountingDataJournal journal,
            AccountingDataSetup setup,
            CultureInfo culture)
        {
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

            XmlNode dataNode = doc.SelectSingleNode("//table/data");

            var journalEntries = this.journal.Booking.OrderBy(b => b.Date);
            foreach (var entry in journalEntries)
            {
                XmlNode dataLineNode = doc.CreateElement("tr");
                XmlNode dataItemNode = doc.CreateElement("td");
                dataLineNode.SetAttribute("topLine", true);
                dataItemNode.InnerText = entry.Date.ToDateTime().ToString("d", this.culture);
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
                    dataItemNode.InnerText = nValue.ToString("0.00", this.culture);
                    dataLineNode.AppendChild(dataItemNode);
                    strAccountNumber = credit.Account.ToString();
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = strAccountNumber;
                    dataLineNode.AppendChild(dataItemNode);
                    nValue = Convert.ToDouble(credit.Value) / 100;
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = nValue.ToString("0.00", this.culture);
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
                    dataItemNode.InnerText = nValue.ToString("0.00", this.culture);
                    dataLineNode.AppendChild(dataItemNode);
                    dataItemNode = doc.CreateElement("td");
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
                    dataItemNode = doc.CreateElement("td");
                    dataLineNode.AppendChild(dataItemNode);
                    dataLineNode.AppendChild(dataItemNode.Clone());
                    string strAccountNumber = creditEntry.Account.ToString();
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = strAccountNumber;
                    dataLineNode.AppendChild(dataItemNode);
                    double nValue = Convert.ToDouble(creditEntry.Value) / 100;
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = nValue.ToString("0.00", this.culture);
                    dataLineNode.AppendChild(dataItemNode);
                    dataNode.AppendChild(dataLineNode);

                    dataLineNode = doc.CreateElement("tr");
                    dataItemNode = doc.CreateElement("td");
                    dataLineNode.AppendChild(dataItemNode);
                    dataLineNode.AppendChild(dataItemNode.Clone());
                }
            }
        }

        public void ShowPreview(string documentName)
        {
            this.printer.PrintDocument(documentName);
        }
    }
}
