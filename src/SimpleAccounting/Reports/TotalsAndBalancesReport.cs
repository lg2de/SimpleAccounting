// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Xml;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Model;

#pragma warning disable S4055 // string literals => pending translation

    internal class TotalsAndBalancesReport : ReportBase
    {
        public const string ResourceName = "TotalsAndBalances.xml";

        private readonly List<AccountingDataAccountGroup> accountGroups;
        private readonly CultureInfo culture;
        private int accountsPerGroup;
        private long groupOpeningCredit;
        private long groupOpeningDebit;
        private long groupSaldoCredit;
        private long groupSaldoDebit;
        private long groupSumCredit;
        private long groupSumDebit;

        private long totalOpeningCredit;
        private long totalOpeningDebit;
        private long totalSaldoCredit;
        private long totalSaldoDebit;
        private long totalSumCredit;
        private long totalSumDebit;

        public TotalsAndBalancesReport(
            AccountingDataJournal yearData,
            List<AccountingDataAccountGroup> accountGroups,
            AccountingDataSetup setup,
            CultureInfo culture)
            : base(ResourceName, setup, yearData, culture)
        {
            this.accountGroups = accountGroups;
            this.culture = culture;
        }

        public List<string> Signatures { get; } = new List<string>();

        public void CreateReport()
        {
            this.PreparePrintDocument();

            XmlNode dataNode = this.PrintDocument.SelectSingleNode("//table/data");

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

                XmlNode groupLineNode = this.PrintDocument.CreateElement("tr");
                XmlNode groupItemNode = this.PrintDocument.CreateElement("td");
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

            XmlNode totalLineNode = this.PrintDocument.CreateElement("tr");
            XmlNode totalItemNode = this.PrintDocument.CreateElement("td");
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

            var signatures = this.PrintDocument.SelectSingleNode("//signatures");
            foreach (var signature in this.Signatures)
            {
                var move = this.PrintDocument.CreateElement("move");
                move.SetAttribute("relY", "20");
                signatures.ParentNode.InsertBefore(move, signatures);

                var line = this.PrintDocument.CreateElement("line");
                line.SetAttribute("relToX", "100");
                signatures.ParentNode.InsertBefore(line, signatures);

                var text = this.PrintDocument.CreateElement("text");
                text.InnerText = signature;
                text.SetAttribute("tag", "signature");
                signatures.ParentNode.InsertBefore(text, signatures);
            }

            signatures.ParentNode.RemoveChild(signatures);
        }

        private void ProcessAccount(XmlNode dataNode, AccountDefinition account)
        {
            if (this.YearData.Booking.All(b =>
                b.Debit.All(x => x.Account != account.ID) && b.Credit.All(x => x.Account != account.ID)))
            {
                return;
            }

            this.accountsPerGroup++;
            var lastBookingDate = this.YearData.Booking
                .Where(x => x.Debit.Any(a => a.Account == account.ID) || x.Credit.Any(a => a.Account == account.ID))
                .Select(x => x.Date).DefaultIfEmpty().Max();
            long saldoCredit = this.YearData.Booking
                .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
            long saldoDebit = this.YearData.Booking
                .SelectMany(x => x.Debit.Where(y => y.Account == account.ID))
                .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
            long openingCredit = this.YearData.Booking
                .Where(b => b.Opening)
                .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
            long openingDebit = this.YearData.Booking
                .Where(b => b.Opening)
                .SelectMany(x => x.Debit.Where(y => y.Account == account.ID))
                .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
            long sumCredit = this.YearData.Booking
                .Where(b => !b.Opening)
                .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
            long sumDebit = this.YearData.Booking
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

            XmlNode dataLineNode = this.PrintDocument.CreateElement("tr");
            XmlNode dataItemNode = this.PrintDocument.CreateElement("td");
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

        private string FormatValue(long value)
        {
            if (value <= 0)
            {
                return string.Empty;
            }

            return value.FormatCurrency(this.culture);
        }
    }
}
