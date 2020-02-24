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

#pragma warning disable S4055 // string literals => pending translation

    internal class TotalsAndBalancesReport
    {
        public const string ResourceName = "TotalsAndBalances.xml";

        private readonly AccountingDataJournal journal;
        private readonly List<AccountingDataAccountGroup> accountGroups;
        private readonly AccountingDataSetup setup;
        private readonly CultureInfo culture;
        private readonly IXmlPrinter printer = new XmlPrinter();

        private double totalOpeningCredit, totalOpeningDebit;
        private double totalSumCredit, totalSumDebit;
        private double totalSaldoCredit, totalSaldoDebit;
        private double groupOpeningCredit, groupOpeningDebit;
        private double groupSumCredit, groupSumDebit;
        private double groupSaldoCredit, groupSaldoDebit;
        private int accountsPerGroup;

        public TotalsAndBalancesReport(
            AccountingDataJournal journal,
            List<AccountingDataAccountGroup> accountGroups,
            AccountingDataSetup setup,
            CultureInfo culture)
        {
            this.journal = journal;
            this.accountGroups = accountGroups;
            this.setup = setup;
            this.culture = culture;
        }

        public List<string> Signatures { get; } = new List<string>();

        internal XDocument Document => XDocument.Parse(this.printer.Document.OuterXml);

        public void CreateReport(DateTime dateStart, DateTime dateEnd)
        {
            this.printer.LoadDocument(ResourceName);

            XmlDocument doc = this.printer.Document;

            XmlNode firmNode = doc.SelectSingleNode("//text[@ID=\"firm\"]");
            firmNode.InnerText = this.setup.Name;

            XmlNode rangeNode = doc.SelectSingleNode("//text[@ID=\"range\"]");
            rangeNode.InnerText = dateStart.ToString("d", this.culture) + " - " + dateEnd.ToString("d", this.culture);

            var dateNode = doc.SelectSingleNode("//text[@ID=\"date\"]");
            dateNode.InnerText = this.setup.Location + ", " + DateTime.Now.ToLongDateString();

            XmlNode dataNode = doc.SelectSingleNode("//table/data");

            this.totalOpeningCredit = 0;
            this.totalOpeningDebit = 0;
            this.totalSumCredit = 0;
            this.totalSumDebit = 0;
            this.totalSaldoCredit = 0;
            this.totalSaldoDebit = 0;
            foreach (var accountGroup in this.accountGroups)
            {
                this.groupOpeningCredit = 0;
                this.groupOpeningDebit = 0;
                this.groupSumCredit = 0;
                this.groupSumDebit = 0;
                this.groupSaldoCredit = 0;
                this.groupSaldoDebit = 0;
                this.accountsPerGroup = 0;
                foreach (var account in accountGroup.Account)
                {
                    this.ProcessAccount(dataNode, account);
                }

                if (this.accountsPerGroup <= 0 || this.accountGroups.Count <= 1)
                {
                    // There is no account in group or we only one group at all.
                    continue;
                }

                XmlNode groupLineNode = doc.CreateElement("tr");
                XmlNode groupItemNode = doc.CreateElement("td");
                groupLineNode.SetAttribute("topLine", true);
                groupLineNode.SetAttribute("lineHeight", 6);

                groupItemNode.InnerText = string.Empty;
                groupLineNode.AppendChild(groupItemNode);

                groupItemNode = groupItemNode.Clone();
                groupItemNode.InnerText = accountGroup.Name;
                groupLineNode.AppendChild(groupItemNode);

                groupItemNode = groupItemNode.Clone();
                groupItemNode.InnerText = string.Empty;
                groupLineNode.AppendChild(groupItemNode);

                groupItemNode = groupItemNode.Clone();
                groupItemNode.InnerText = this.FormatValue(this.groupOpeningDebit);
                groupLineNode.AppendChild(groupItemNode);
                groupItemNode = groupItemNode.Clone();
                groupItemNode.InnerText = this.FormatValue(this.groupOpeningCredit);
                groupLineNode.AppendChild(groupItemNode);

                groupItemNode = groupItemNode.Clone();
                groupItemNode.InnerText = this.FormatValue(this.groupSumDebit);
                groupLineNode.AppendChild(groupItemNode);
                groupItemNode = groupItemNode.Clone();
                groupItemNode.InnerText = this.FormatValue(this.groupSumCredit);
                groupLineNode.AppendChild(groupItemNode);

                groupItemNode = groupItemNode.Clone();
                groupItemNode.InnerText = this.FormatValue(this.groupSaldoDebit);
                groupLineNode.AppendChild(groupItemNode);
                groupItemNode = groupItemNode.Clone();
                groupItemNode.InnerText = this.FormatValue(this.groupSaldoCredit);
                groupLineNode.AppendChild(groupItemNode);

                groupLineNode.ChildNodes[1].SetAttribute("align", "right");
                dataNode.AppendChild(groupLineNode);
            }

            XmlNode totalLineNode = doc.CreateElement("tr");
            XmlNode totalItemNode = doc.CreateElement("td");
            totalLineNode.SetAttribute("topLine", true);

            totalItemNode.InnerText = string.Empty;
            totalLineNode.AppendChild(totalItemNode);

            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = "Total";
            totalLineNode.AppendChild(totalItemNode);

            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = string.Empty;
            totalLineNode.AppendChild(totalItemNode);

            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = this.FormatValue(this.totalOpeningDebit);
            totalLineNode.AppendChild(totalItemNode);
            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = this.FormatValue(this.totalOpeningCredit);
            totalLineNode.AppendChild(totalItemNode);

            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = this.FormatValue(this.totalSumDebit);
            totalLineNode.AppendChild(totalItemNode);
            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = this.FormatValue(this.totalSumCredit);
            totalLineNode.AppendChild(totalItemNode);

            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = this.FormatValue(this.totalSaldoDebit);
            totalLineNode.AppendChild(totalItemNode);
            totalItemNode = totalItemNode.Clone();
            totalItemNode.InnerText = this.FormatValue(this.totalSaldoCredit);
            totalLineNode.AppendChild(totalItemNode);

            totalLineNode.ChildNodes[1].SetAttribute("align", "right");
            dataNode.AppendChild(totalLineNode);

            var signatures = doc.SelectSingleNode("//signatures");
            foreach (var signature in this.Signatures)
            {
                var move = doc.CreateElement("move");
                move.SetAttribute("relY", "20");
                signatures.ParentNode.InsertBefore(move, signatures);

                var line = doc.CreateElement("line");
                line.SetAttribute("relToX", "100");
                signatures.ParentNode.InsertBefore(line, signatures);

                var text = doc.CreateElement("text");
                text.InnerText = signature;
                signatures.ParentNode.InsertBefore(text, signatures);
            }

            signatures.ParentNode.RemoveChild(signatures);
        }

        private void ProcessAccount(XmlNode dataNode, AccountDefinition account)
        {
            if (this.journal.Booking.All(b => b.Debit.All(x => x.Account != account.ID) && b.Credit.All(x => x.Account != account.ID)))
            {
                return;
            }

            this.accountsPerGroup++;
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

            XmlNode dataLineNode = this.printer.Document.CreateElement("tr");
            XmlNode dataItemNode = this.printer.Document.CreateElement("td");
            dataLineNode.SetAttribute("topLine", true);

            dataItemNode.InnerText = account.ID.ToString(CultureInfo.InvariantCulture);
            dataLineNode.AppendChild(dataItemNode);

            dataItemNode = dataItemNode.Clone();
            dataItemNode.InnerText = account.Name;
            dataLineNode.AppendChild(dataItemNode);

            dataItemNode = dataItemNode.Clone();
            dataItemNode.InnerText = lastBookingDate.ToDateTime().ToString("d", this.culture);
            dataLineNode.AppendChild(dataItemNode);

            dataItemNode = dataItemNode.Clone();
            dataItemNode.InnerText = this.FormatValue(openingDebit);
            dataLineNode.AppendChild(dataItemNode);
            dataItemNode = dataItemNode.Clone();
            dataItemNode.InnerText = this.FormatValue(openingCredit);
            dataLineNode.AppendChild(dataItemNode);

            dataItemNode = dataItemNode.Clone();
            dataItemNode.InnerText = this.FormatValue(sumDebit);
            dataLineNode.AppendChild(dataItemNode);
            dataItemNode = dataItemNode.Clone();
            dataItemNode.InnerText = this.FormatValue(sumCredit);
            dataLineNode.AppendChild(dataItemNode);

            dataItemNode = dataItemNode.Clone();
            dataItemNode.InnerText = this.FormatValue(saldoDebit);
            dataLineNode.AppendChild(dataItemNode);
            dataItemNode = dataItemNode.Clone();
            dataItemNode.InnerText = this.FormatValue(saldoCredit);
            dataLineNode.AppendChild(dataItemNode);

            dataNode.AppendChild(dataLineNode);

            this.groupOpeningCredit += openingCredit;
            this.groupOpeningDebit += openingDebit;
            this.groupSumCredit += sumCredit;
            this.groupSumDebit += sumDebit;
            this.groupSaldoCredit += saldoCredit;
            this.groupSaldoDebit += saldoDebit;

            this.totalOpeningCredit += openingCredit;
            this.totalOpeningDebit += openingDebit;
            this.totalSumCredit += sumCredit;
            this.totalSumDebit += sumDebit;
            this.totalSaldoCredit += saldoCredit;
            this.totalSaldoDebit += saldoDebit;
        }

        public void ShowPreview(string documentName)
        {
            this.printer.PrintDocument(documentName);
        }

        private string FormatValue(double value)
        {
            if (value <= 0)
            {
                return string.Empty;
            }

            return (value / 100).ToString("0.00", this.culture);
        }
    }
}
