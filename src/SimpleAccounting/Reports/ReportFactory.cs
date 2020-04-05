// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using lg2de.SimpleAccounting.Model;

    [ExcludeFromCodeCoverage]
    internal class ReportFactory : IReportFactory
    {
        public IAccountJournalReport CreateAccountJournal(
            AccountingDataJournal journal,
            IEnumerable<AccountDefinition> accounts,
            AccountingDataSetup setup,
            CultureInfo culture)
        {
            return new AccountJournalReport(journal, accounts, setup, culture);
        }

        public IAnnualBalanceReport CreateAnnualBalance(
            AccountingDataJournal journal,
            IEnumerable<AccountDefinition> accounts,
            AccountingDataSetup setup,
            CultureInfo culture)
        {
            return new AnnualBalanceReport(journal, accounts, setup, culture);
        }

        public ITotalsAndBalancesReport CreateTotalsAndBalances(
            AccountingDataJournal journal,
            IEnumerable<AccountingDataAccountGroup> accounts,
            AccountingDataSetup setup,
            CultureInfo culture)
        {
            return new TotalsAndBalancesReport(journal, accounts, setup, culture);
        }
    }
}
