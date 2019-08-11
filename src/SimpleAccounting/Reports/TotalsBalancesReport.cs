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
        private readonly List<AccountingDataAccountGroup> accountGroups;
        private readonly string firmName;
        private readonly string bookingYearName;

        public TotalsBalancesReport(
            AccountingDataJournal journal,
            IEnumerable<AccountingDataAccountGroup> accountGroups,
            string firmName,
            string bookingYearName)
        {
            this.journal = journal;
            this.accountGroups = accountGroups.ToList();
            this.firmName = firmName;
            this.bookingYearName = bookingYearName;
        }

        public IXmlPrinter Printer { get; internal set; } = new XmlPrinter();

        public void CreateReport(DateTime dateStart, DateTime dateEnd)
        {
            this.Printer.LoadDocument("TotalsBalances.xml");

            XmlDocument doc = this.Printer.Document;

            XmlNode firmNode = doc.SelectSingleNode("//text[@ID=\"firm\"]");
            firmNode.InnerText = this.firmName;

            XmlNode rangeNode = doc.SelectSingleNode("//text[@ID=\"range\"]");
            rangeNode.InnerText = dateStart.ToString("d") + " - " + dateEnd.ToString("d");

            var dateNode = doc.SelectSingleNode("//text[@ID=\"date\"]");
            dateNode.InnerText = "Dresden, " + DateTime.Now.ToLongDateString();

            XmlNode dataNode = doc.SelectSingleNode("//table/data");

            double totalOpeningCredit = 0, totalOpeningDebit = 0;
            double totalSumCredit = 0, totalSumDebit = 0;
            double totalSaldoCredit = 0, totalSaldoDebit = 0;
            foreach (var accountGroup in this.accountGroups)
            {
                double groupOpeningCredit = 0, groupOpeningDebit = 0;
                double groupSumCredit = 0, groupSumDebit = 0;
                double groupSaldoCredit = 0, groupSaldoDebit = 0;
                int groupCount = 0;
                foreach (var account in accountGroup.Account)
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
                    double sumCredit = this.journal.Booking
                        .Where(b => !b.Opening)
                        .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                        .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
                    double sumDebit = this.journal.Booking
                        .Where(b => !b.Opening)
                        .SelectMany(x => x.Debit.Where(y => y.Account == account.ID))
                        .DefaultIfEmpty().Sum(x => x?.Value ?? 0);

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
                    dataItemNode.InnerText = FormatValue(openingDebit);
                    dataLineNode.AppendChild(dataItemNode);
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = FormatValue(openingCredit);
                    dataLineNode.AppendChild(dataItemNode);

                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = FormatValue(sumDebit);
                    dataLineNode.AppendChild(dataItemNode);
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = FormatValue(sumCredit);
                    dataLineNode.AppendChild(dataItemNode);

                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = FormatValue(saldoDebit);
                    dataLineNode.AppendChild(dataItemNode);
                    dataItemNode = dataItemNode.Clone();
                    dataItemNode.InnerText = FormatValue(saldoCredit);
                    dataLineNode.AppendChild(dataItemNode);

                    dataNode.AppendChild(dataLineNode);

                    groupOpeningCredit += openingCredit;
                    groupOpeningDebit += openingDebit;
                    groupSumCredit += sumCredit;
                    groupSumDebit += sumDebit;
                    groupSaldoCredit += saldoCredit;
                    groupSaldoDebit += saldoDebit;
                    groupCount++;

                    totalOpeningCredit += openingCredit;
                    totalOpeningDebit += openingDebit;
                    totalSumCredit += sumCredit;
                    totalSumDebit += sumDebit;
                    totalSaldoCredit += saldoCredit;
                    totalSaldoDebit += saldoDebit;
                }

                if (groupCount > 0 && this.accountGroups.Count > 1)
                {
                    XmlNode groupLineNode = doc.CreateElement("tr");
                    XmlNode groupItemNode = doc.CreateElement("td");
                    groupLineNode.SetAttribute("topline", "1");
                    groupLineNode.SetAttribute("lineheight", "6");

                    groupItemNode.InnerText = string.Empty;
                    groupLineNode.AppendChild(groupItemNode);

                    groupItemNode = groupItemNode.Clone();
                    groupItemNode.InnerText = accountGroup.Name;
                    groupLineNode.AppendChild(groupItemNode);

                    groupItemNode = groupItemNode.Clone();
                    groupItemNode.InnerText = string.Empty;
                    groupLineNode.AppendChild(groupItemNode);

                    groupItemNode = groupItemNode.Clone();
                    groupItemNode.InnerText = FormatValue(groupOpeningDebit);
                    groupLineNode.AppendChild(groupItemNode);
                    groupItemNode = groupItemNode.Clone();
                    groupItemNode.InnerText = FormatValue(groupOpeningCredit);
                    groupLineNode.AppendChild(groupItemNode);

                    groupItemNode = groupItemNode.Clone();
                    groupItemNode.InnerText = FormatValue(groupSumDebit);
                    groupLineNode.AppendChild(groupItemNode);
                    groupItemNode = groupItemNode.Clone();
                    groupItemNode.InnerText = FormatValue(groupSumCredit);
                    groupLineNode.AppendChild(groupItemNode);

                    groupItemNode = groupItemNode.Clone();
                    groupItemNode.InnerText = FormatValue(groupSaldoDebit);
                    groupLineNode.AppendChild(groupItemNode);
                    groupItemNode = groupItemNode.Clone();
                    groupItemNode.InnerText = FormatValue(groupSaldoCredit);
                    groupLineNode.AppendChild(groupItemNode);

                    groupLineNode.ChildNodes[1].SetAttribute("align", "right");
                    dataNode.AppendChild(groupLineNode);
                }
            }

            XmlNode totalLineNode = doc.CreateElement("tr");
            XmlNode totalItemNode = doc.CreateElement("td");
            totalLineNode.SetAttribute("topline", "1");

            totalItemNode.InnerText = string.Empty;
            totalLineNode.AppendChild(totalItemNode);

            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = "Total";
            totalLineNode.AppendChild(totalItemNode);

            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = string.Empty;
            totalLineNode.AppendChild(totalItemNode);

            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = FormatValue(totalOpeningDebit);
            totalLineNode.AppendChild(totalItemNode);
            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = FormatValue(totalOpeningCredit);
            totalLineNode.AppendChild(totalItemNode);

            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = FormatValue(totalSumDebit);
            totalLineNode.AppendChild(totalItemNode);
            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = FormatValue(totalSumCredit);
            totalLineNode.AppendChild(totalItemNode);

            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = FormatValue(totalSaldoDebit);
            totalLineNode.AppendChild(totalItemNode);
            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = FormatValue(totalSaldoCredit);
            totalLineNode.AppendChild(totalItemNode);

            totalLineNode.ChildNodes[1].SetAttribute("align", "right");
            dataNode.AppendChild(totalLineNode);

            this.Printer.PrintDocument(DateTime.Now.ToString("yyyy-MM-dd") + " Summen und Salden " + this.bookingYearName);
        }

        private static string FormatValue(double value)
        {
            if (value <= 0)
            {
                return string.Empty;
            }

            return (value / 100).ToString("0.00");
        }
    }
}
