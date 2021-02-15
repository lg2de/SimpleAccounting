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

    internal class TotalJournalReport : ReportBase, ITotalJournalReport
    {
        public const string ResourceName = "TotalJournal.xml";

        public TotalJournalReport(ProjectData projectData)
            : base(ResourceName, projectData)
        {
        }

        public void CreateReport(string title)
        {
            this.PreparePrintDocument(title);

            XmlNode dataNode = this.PrintDocument.SelectSingleNode("//table/data");

            var journalEntries = this.YearData.Booking.OrderBy(b => b.Date);
            foreach (var entry in journalEntries)
            {
                XmlNode dataLineNode = this.PrintDocument.CreateElement("tr");
                dataLineNode.SetAttribute("topLine", true);
                dataLineNode.AddTableNode(entry.Date.ToDateTime().ToString("d", CultureInfo.CurrentCulture));
                dataLineNode.AddTableNode(entry.ID.ToString(CultureInfo.InvariantCulture));

                if (entry.Debit.Count == 1
                    && entry.Credit.Count == 1
                    && entry.Debit.First().Text == entry.Credit.First().Text)
                {
                    var credit = entry.Credit.Single();
                    var debit = entry.Debit.Single();
                    dataLineNode.AddTableNode(debit.Text);
                    string strAccountNumber = debit.Account.ToString(CultureInfo.InvariantCulture);
                    dataLineNode.AddTableNode(strAccountNumber);
                    dataLineNode.AddTableNode(debit.Value.FormatCurrency());
                    strAccountNumber = credit.Account.ToString(CultureInfo.InvariantCulture);
                    dataLineNode.AddTableNode(strAccountNumber);
                    dataLineNode.AddTableNode(credit.Value.FormatCurrency());
                    dataNode.AppendChild(dataLineNode);
                    continue;
                }

                foreach (var debitEntry in entry.Debit)
                {
                    dataLineNode.AddTableNode(debitEntry.Text);
                    string strAccountNumber = debitEntry.Account.ToString(CultureInfo.InvariantCulture);
                    dataLineNode.AddTableNode(strAccountNumber);
                    dataLineNode.AddTableNode(debitEntry.Value.FormatCurrency());
                    dataLineNode.AddTableNode(string.Empty);
                    dataLineNode.AddTableNode(string.Empty);
                    dataNode.AppendChild(dataLineNode);

                    dataLineNode = this.PrintDocument.CreateElement("tr");
                    dataLineNode.AddTableNode(string.Empty);
                    dataLineNode.AddTableNode(string.Empty);
                }

                foreach (var creditEntry in entry.Credit)
                {
                    dataLineNode.AddTableNode(creditEntry.Text);
                    dataLineNode.AddTableNode(string.Empty);
                    dataLineNode.AddTableNode(string.Empty);
                    string strAccountNumber = creditEntry.Account.ToString(CultureInfo.InvariantCulture);
                    dataLineNode.AddTableNode(strAccountNumber);
                    dataLineNode.AddTableNode(creditEntry.Value.FormatCurrency());
                    dataNode.AppendChild(dataLineNode);

                    dataLineNode = this.PrintDocument.CreateElement("tr");
                    dataLineNode.AddTableNode(string.Empty);
                    dataLineNode.AddTableNode(string.Empty);
                }
            }
        }
    }
}
