// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace lg2de.SimpleAccounting
{
    internal class JournalReport
    {
        private readonly AccountingDataJournal journal;
        private readonly string firmName;
        private readonly string bookingYearName;

        public JournalReport(AccountingDataJournal journal, string firmName, string bookingYearName)
        {
            this.journal = journal;
            this.firmName = firmName;
            this.bookingYearName = bookingYearName;
        }

        public void CreateReport(DateTime dateStart, DateTime dateEnd)
        {
            var print = new PrintClass();
            string fileName = Application.ExecutablePath;
            fileName = fileName.Substring(0, fileName.LastIndexOf("\\"));
            fileName = fileName.Substring(0, fileName.LastIndexOf("\\"));
            fileName = fileName.Substring(0, fileName.LastIndexOf("\\") + 1);
            fileName += "Journal.xml";
            print.LoadDocument(fileName);

            XmlDocument doc = print.Document;

            XmlNode firmNode = doc.SelectSingleNode("//text[@ID=\"firm\"]");
            firmNode.InnerText = this.firmName;

            XmlNode rangeNode = doc.SelectSingleNode("//text[@ID=\"range\"]");
            rangeNode.InnerText = dateStart.ToString("d") + " - " + dateEnd.ToString("d");

            var dateNode = doc.SelectSingleNode("//text[@ID=\"date\"]");
            dateNode.InnerText = "Dresden, " + DateTime.Now.ToLongDateString();

            XmlNode dataNode = doc.SelectSingleNode("//table/data");

            var journalEntries = this.journal.Booking.OrderBy(b => b.Date);
            foreach (var entry in journalEntries)
            {
                XmlNode dataLineNode = doc.CreateElement("tr");
                XmlNode dataItemNode = doc.CreateElement("td");
                dataLineNode.SetAttribute("topline", "1");
                dataItemNode.InnerText = entry.Date.ToDateTime().ToString("d");
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
                    dataItemNode.InnerText = nValue.ToString("0.00");
                    dataLineNode.AppendChild(dataItemNode);
                    strAccountNumber = credit.Account.ToString();
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = strAccountNumber;
                    dataLineNode.AppendChild(dataItemNode);
                    nValue = Convert.ToDouble(credit.Value) / 100;
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = nValue.ToString("0.00");
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
                    dataItemNode.InnerText = nValue.ToString("0.00");
                    dataLineNode.AppendChild(dataItemNode);
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = "";
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
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = "";
                    dataLineNode.AppendChild(dataItemNode);
                    dataLineNode.AppendChild(dataItemNode.Clone());
                    string strAccountNumber = creditEntry.Account.ToString();
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = strAccountNumber;
                    dataLineNode.AppendChild(dataItemNode);
                    double nValue = Convert.ToDouble(creditEntry.Value) / 100;
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = nValue.ToString("0.00");
                    dataLineNode.AppendChild(dataItemNode);
                    dataNode.AppendChild(dataLineNode);

                    dataLineNode = doc.CreateElement("tr");
                    dataItemNode = doc.CreateElement("td");
                    dataLineNode.AppendChild(dataItemNode);
                    dataLineNode.AppendChild(dataItemNode.Clone());
                }
            }

            print.PrintDocument(DateTime.Now.ToString("yyyy-MM-dd") + " Journal " + this.bookingYearName);
        }
    }
}
