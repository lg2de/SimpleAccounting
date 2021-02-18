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
    using lg2de.SimpleAccounting.Properties;

    internal class TotalsAndBalancesReport : ReportBase, ITotalsAndBalancesReport
    {
        public const string ResourceName = "TotalsAndBalances.xml";

        private readonly List<AccountingDataAccountGroup> accountGroups;
        private int accountsPerGroup;
        private long groupOpeningCredit;
        private long groupOpeningDebit;
        private long groupBalanceCredit;
        private long groupBalanceDebit;
        private long groupTotalCredit;
        private long groupTotalDebit;

        private long overallOpeningCredit;
        private long overallOpeningDebit;
        private long overallBalanceCredit;
        private long overallBalanceDebit;
        private long overallTotalCredit;
        private long overallTotalDebit;

        public TotalsAndBalancesReport(
            IProjectData projectData,
            IEnumerable<AccountingDataAccountGroup> accountGroups)
            : base(ResourceName, projectData)
        {
            this.accountGroups = accountGroups.ToList();
        }

        public List<string> Signatures { get; } = new List<string>();

        public void CreateReport(string title)
        {
            this.PreparePrintDocument(title);

            XmlNode dataNode = this.PrintDocument.SelectSingleNode("//table/data");

            this.overallOpeningCredit = 0;
            this.overallOpeningDebit = 0;
            this.overallTotalCredit = 0;
            this.overallTotalDebit = 0;
            this.overallBalanceCredit = 0;
            this.overallBalanceDebit = 0;

            foreach (var accountGroup in this.accountGroups)
            {
                this.groupOpeningCredit = 0;
                this.groupOpeningDebit = 0;
                this.groupTotalCredit = 0;
                this.groupTotalDebit = 0;
                this.groupBalanceCredit = 0;
                this.groupBalanceDebit = 0;
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
                groupLineNode.SetAttribute("topLine", true);
                const int summaryLineHeight = 6;
                groupLineNode.SetAttribute("lineHeight", summaryLineHeight);

                groupLineNode.AddTableNode(string.Empty);

                groupLineNode.AddTableNode(accountGroup.Name).SetAttribute("align", "right");

                groupLineNode.AddTableNode(string.Empty);

                groupLineNode.AddTableNode(FormatValue(this.groupOpeningDebit));
                groupLineNode.AddTableNode(FormatValue(this.groupOpeningCredit));

                groupLineNode.AddTableNode(FormatValue(this.groupTotalDebit));
                groupLineNode.AddTableNode(FormatValue(this.groupTotalCredit));

                groupLineNode.AddTableNode(FormatValue(this.groupBalanceDebit));
                groupLineNode.AddTableNode(FormatValue(this.groupBalanceCredit));

                dataNode.AppendChild(groupLineNode);
            }

            XmlNode totalLineNode = this.PrintDocument.CreateElement("tr");
            totalLineNode.SetAttribute("topLine", true);

            totalLineNode.AddTableNode(string.Empty);

            totalLineNode.AddTableNode(Resources.Word_Total).SetAttribute("align", "right");

            totalLineNode.AddTableNode(string.Empty);

            totalLineNode.AddTableNode(FormatValue(this.overallOpeningDebit));
            totalLineNode.AddTableNode(FormatValue(this.overallOpeningCredit));

            totalLineNode.AddTableNode(FormatValue(this.overallTotalDebit));
            totalLineNode.AddTableNode(FormatValue(this.overallTotalCredit));

            totalLineNode.AddTableNode(FormatValue(this.overallBalanceDebit));
            totalLineNode.AddTableNode(FormatValue(this.overallBalanceCredit));

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

        private static string FormatValue(long value)
        {
            if (value <= 0)
            {
                return string.Empty;
            }

            return value.FormatCurrency();
        }

        private void ProcessAccount(XmlNode dataNode, AccountDefinition account)
        {
            if (this.YearData.Booking.All(
                b =>
                    b.Debit.All(x => x.Account != account.ID) && b.Credit.All(x => x.Account != account.ID)))
            {
                return;
            }

            this.accountsPerGroup++;
            var lastBookingDate = this.YearData.Booking
                .Where(x => x.Debit.Any(a => a.Account == account.ID) || x.Credit.Any(a => a.Account == account.ID))
                .Select(x => x.Date).DefaultIfEmpty().Max();
            long balanceCredit = this.YearData.Booking
                .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
            long balanceDebit = this.YearData.Booking
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
            long totalCredit = this.YearData.Booking
                .Where(b => !b.Opening)
                .SelectMany(x => x.Credit.Where(y => y.Account == account.ID))
                .DefaultIfEmpty().Sum(x => x?.Value ?? 0);
            long totalDebit = this.YearData.Booking
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

            if (balanceCredit > balanceDebit)
            {
                balanceCredit -= balanceDebit;
                balanceDebit = 0;
            }
            else
            {
                balanceDebit -= balanceCredit;
                balanceCredit = 0;
            }

            XmlNode dataLineNode = this.PrintDocument.CreateElement("tr");
            dataLineNode.SetAttribute("topLine", true);

            dataLineNode.AddTableNode(account.ID.ToString(CultureInfo.InvariantCulture));

            dataLineNode.AddTableNode(account.Name);

            dataLineNode.AddTableNode(lastBookingDate.ToDateTime().ToString("d", CultureInfo.CurrentCulture));

            dataLineNode.AddTableNode(FormatValue(openingDebit));
            dataLineNode.AddTableNode(FormatValue(openingCredit));

            dataLineNode.AddTableNode(FormatValue(totalDebit));
            dataLineNode.AddTableNode(FormatValue(totalCredit));

            dataLineNode.AddTableNode(FormatValue(balanceDebit));
            dataLineNode.AddTableNode(FormatValue(balanceCredit));

            dataNode.AppendChild(dataLineNode);

            this.groupOpeningCredit += openingCredit;
            this.groupOpeningDebit += openingDebit;
            this.groupTotalCredit += totalCredit;
            this.groupTotalDebit += totalDebit;
            this.groupBalanceCredit += balanceCredit;
            this.groupBalanceDebit += balanceDebit;

            this.overallOpeningCredit += openingCredit;
            this.overallOpeningDebit += openingDebit;
            this.overallTotalCredit += totalCredit;
            this.overallTotalDebit += totalDebit;
            this.overallBalanceCredit += balanceCredit;
            this.overallBalanceDebit += balanceDebit;
        }
    }
}
