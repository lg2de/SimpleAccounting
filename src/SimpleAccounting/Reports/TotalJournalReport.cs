// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    using System.Globalization;
    using System.Linq;
    using System.Xml;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Model;

#pragma warning disable S4055 // string literals => pending translation

    internal class TotalJournalReport : ReportBase
    {
        public const string ResourceName = "TotalJournal.xml";

        private readonly CultureInfo culture;

        public TotalJournalReport(
            AccountingDataJournal yearData,
            AccountingDataSetup setup,
            CultureInfo culture)
            : base(ResourceName, setup, yearData, culture)
        {
            this.culture = culture;
        }

        public void CreateReport()
        {
            this.PreparePrintDocument();

            XmlNode dataNode = this.PrintDocument.SelectSingleNode("//table/data");

            var journalEntries = this.YearData.Booking.OrderBy(b => b.Date);
            foreach (var entry in journalEntries)
            {
                XmlNode dataLineNode = this.PrintDocument.CreateElement("tr");
                XmlNode dataItemNode = this.PrintDocument.CreateElement("td");
                dataLineNode.SetAttribute("topLine", true);
                dataItemNode.InnerText = entry.Date.ToDateTime().ToString("d", this.culture);
                dataLineNode.AppendChild(dataItemNode);
                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = entry.ID.ToString(CultureInfo.InvariantCulture);
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
                    string strAccountNumber = debit.Account.ToString(CultureInfo.InvariantCulture);
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = strAccountNumber;
                    dataLineNode.AppendChild(dataItemNode);
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = debit.Value.FormatCurrency(this.culture);
                    dataLineNode.AppendChild(dataItemNode);
                    strAccountNumber = credit.Account.ToString(CultureInfo.InvariantCulture);
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = strAccountNumber;
                    dataLineNode.AppendChild(dataItemNode);
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = credit.Value.FormatCurrency(this.culture);
                    dataLineNode.AppendChild(dataItemNode);
                    dataNode.AppendChild(dataLineNode);
                    continue;
                }

                foreach (var debitEntry in entry.Debit)
                {
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = debitEntry.Text;
                    dataLineNode.AppendChild(dataItemNode);
                    string strAccountNumber = debitEntry.Account.ToString(CultureInfo.InvariantCulture);
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = strAccountNumber;
                    dataLineNode.AppendChild(dataItemNode);
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = debitEntry.Value.FormatCurrency(this.culture);
                    dataLineNode.AppendChild(dataItemNode);
                    dataItemNode = this.PrintDocument.CreateElement("td");
                    dataLineNode.AppendChild(dataItemNode);
                    dataLineNode.AppendChild(dataItemNode.Clone());
                    dataNode.AppendChild(dataLineNode);

                    dataLineNode = this.PrintDocument.CreateElement("tr");
                    dataItemNode = this.PrintDocument.CreateElement("td");
                    dataLineNode.AppendChild(dataItemNode);
                    dataLineNode.AppendChild(dataItemNode.Clone());
                }

                foreach (var creditEntry in entry.Credit)
                {
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = creditEntry.Text;
                    dataLineNode.AppendChild(dataItemNode);
                    dataItemNode = this.PrintDocument.CreateElement("td");
                    dataLineNode.AppendChild(dataItemNode);
                    dataLineNode.AppendChild(dataItemNode.Clone());
                    string strAccountNumber = creditEntry.Account.ToString(CultureInfo.InvariantCulture);
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = strAccountNumber;
                    dataLineNode.AppendChild(dataItemNode);
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = creditEntry.Value.FormatCurrency(this.culture);
                    dataLineNode.AppendChild(dataItemNode);
                    dataNode.AppendChild(dataLineNode);

                    dataLineNode = this.PrintDocument.CreateElement("tr");
                    dataItemNode = this.PrintDocument.CreateElement("td");
                    dataLineNode.AppendChild(dataItemNode);
                    dataLineNode.AppendChild(dataItemNode.Clone());
                }
            }
        }
    }
}
