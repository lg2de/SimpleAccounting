// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using lg2de.SimpleAccounting.Extensions;
using lg2de.SimpleAccounting.Model;

namespace lg2de.SimpleAccounting.Reports
{
    internal class TotalsBalancesReport
    {
        private readonly AccountingDataJournal journal;
        private readonly List<AccountingDataAccount> accounts;
        private readonly string firmName;
        private readonly string bookingYearName;

        public TotalsBalancesReport(
            AccountingDataJournal journal,
            List<AccountingDataAccount> accounts,
            string firmName,
            string bookingYearName)
        {
            this.journal = journal;
            this.accounts = accounts;
            this.firmName = firmName;
            this.bookingYearName = bookingYearName;
        }

        public void CreateReport(DateTime dateStart, DateTime dateEnd)
        {
            var print = new PrintClass();
            print.LoadDocument("TotalsBalances.xml");

            XmlDocument doc = print.Document;

            XmlNode firmNode = doc.SelectSingleNode("//text[@ID=\"firm\"]");
            firmNode.InnerText = this.firmName;

            XmlNode rangeNode = doc.SelectSingleNode("//text[@ID=\"range\"]");
            rangeNode.InnerText = dateStart.ToString("d") + " - " + dateEnd.ToString("d");

            var dateNode = doc.SelectSingleNode("//text[@ID=\"date\"]");
            dateNode.InnerText = "Dresden, " + DateTime.Now.ToLongDateString();

            XmlNode dataNode = doc.SelectSingleNode("//table/data");

            double totalOpeningCredit = 0, totalOpeningDebit = 0;
            double totalSumSectionCredit = 0, totalSumSectionDebit = 0;
            double totalSumEndCredit = 0, totalSumEndDebit = 0;
            double totalSaldoCredit = 0, totalSaldoDebit = 0;
            foreach (var account in this.accounts)
            {
                if (this.journal.Booking.All(b => b.Debit.All(x => x.Account != account.ID) && b.Credit.All(x => x.Account != account.ID)))
                {
                    continue;
                }

                var lastBookingDate = this.journal.Booking
                    .Where(x => x.Debit.Any(a => a.Account == account.ID) || x.Credit.Any(a => a.Account == account.ID))
                    .Select(x => x.Date).DefaultIfEmpty().Max();
                double saldoCredit = this.journal.Booking
                    .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                double saldoDebit = this.journal.Booking
                    .SelectMany(x => x.Debit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                double openingCredit = this.journal.Booking
                    .Where(b => b.Opening)
                    .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                double openingDebit = this.journal.Booking
                    .Where(b => b.Opening)
                    .SelectMany(x => x.Debit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                double sumSectionCredit = this.journal.Booking
                    .Where(b => !b.Opening)
                    .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                double sumSectionDebit = this.journal.Booking
                    .Where(b => !b.Opening)
                    .SelectMany(x => x.Debit.Where(y => y.Account == account.ID))
                    .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                // currently identical
                double sumEndCredit = sumSectionCredit, sumEndDebit = sumSectionDebit;

                if (openingCredit > openingDebit)
                {
                    openingCredit -= openingDebit;
                    openingDebit = 0;
                }
                else
                {
                    openingDebit -= openingCredit;
                    openingCredit = 0;
                }

                if (saldoCredit > saldoDebit)
                {
                    saldoCredit -= saldoDebit;
                    saldoDebit = 0;
                }
                else
                {
                    saldoDebit -= saldoCredit;
                    saldoCredit = 0;
                }

                XmlNode dataLineNode = doc.CreateElement("tr");
                XmlNode dataItemNode = doc.CreateElement("td");
                dataLineNode.SetAttribute("topline", "1");

                dataItemNode.InnerText = account.ID.ToString();
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = account.Name;
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = lastBookingDate.ToDateTime().ToString("d");
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = openingDebit > 0 ? (openingDebit / 100).ToString("0.00") : "";
                dataLineNode.AppendChild(dataItemNode);
                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = openingCredit > 0 ? (openingCredit / 100).ToString("0.00") : "";
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = sumSectionDebit > 0 ? (sumSectionDebit / 100).ToString("0.00") : "";
                dataLineNode.AppendChild(dataItemNode);
                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = sumSectionCredit > 0 ? (sumSectionCredit / 100).ToString("0.00") : "";
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = sumEndDebit > 0 ? (sumEndDebit / 100).ToString("0.00") : "";
                dataLineNode.AppendChild(dataItemNode);
                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = sumEndCredit > 0 ? (sumEndCredit / 100).ToString("0.00") : "";
                dataLineNode.AppendChild(dataItemNode);

                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = saldoDebit > 0 ? (saldoDebit / 100).ToString("0.00") : "";
                dataLineNode.AppendChild(dataItemNode);
                dataItemNode = dataItemNode.Clone();
                dataItemNode.InnerText = saldoCredit > 0 ? (saldoCredit / 100).ToString("0.00") : "";
                dataLineNode.AppendChild(dataItemNode);

                dataNode.AppendChild(dataLineNode);

                totalOpeningCredit += openingCredit;
                totalOpeningDebit += openingDebit;
                totalSumSectionCredit += sumSectionCredit;
                totalSumSectionDebit += sumSectionDebit;
                totalSumEndCredit += sumEndCredit;
                totalSumEndDebit += sumEndDebit;
                totalSaldoCredit += saldoCredit;
                totalSaldoDebit += saldoDebit;
            }

            XmlNode totalLineNode = doc.CreateElement("tr");
            XmlNode totalItemNode = doc.CreateElement("td");
            totalLineNode.SetAttribute("topline", "1");

            totalItemNode.InnerText = "";
            totalLineNode.AppendChild(totalItemNode);

            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = "Total";
            totalLineNode.AppendChild(totalItemNode);

            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = "";
            totalLineNode.AppendChild(totalItemNode);

            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = totalOpeningDebit > 0 ? (totalOpeningDebit / 100).ToString("0.00") : "";
            totalLineNode.AppendChild(totalItemNode);
            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = totalOpeningCredit > 0 ? (totalOpeningCredit / 100).ToString("0.00") : "";
            totalLineNode.AppendChild(totalItemNode);

            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = totalSumSectionDebit > 0 ? (totalSumSectionDebit / 100).ToString("0.00") : "";
            totalLineNode.AppendChild(totalItemNode);
            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = totalSumSectionCredit > 0 ? (totalSumSectionCredit / 100).ToString("0.00") : "";
            totalLineNode.AppendChild(totalItemNode);

            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = totalSumEndDebit > 0 ? (totalSumEndDebit / 100).ToString("0.00") : "";
            totalLineNode.AppendChild(totalItemNode);
            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = totalSumEndCredit > 0 ? (totalSumEndCredit / 100).ToString("0.00") : "";
            totalLineNode.AppendChild(totalItemNode);

            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = totalSaldoDebit > 0 ? (totalSaldoDebit / 100).ToString("0.00") : "";
            totalLineNode.AppendChild(totalItemNode);
            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = totalSaldoCredit > 0 ? (totalSaldoCredit / 100).ToString("0.00") : "";
            totalLineNode.AppendChild(totalItemNode);

            dataNode.AppendChild(totalLineNode);

            print.PrintDocument(DateTime.Now.ToString("yyyy-MM-dd") + " Summen und Salden " + this.bookingYearName);
        }
    }
}
