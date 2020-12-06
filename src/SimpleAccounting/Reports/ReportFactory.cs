// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using lg2de.SimpleAccounting.Model;

    [ExcludeFromCodeCoverage]
    internal class ReportFactory : IReportFactory
    {
        public IAccountJournalReport CreateAccountJournal(
            AccountingDataJournal journal,
            IEnumerable<AccountDefinition> accounts,
            AccountingDataSetup setup)
        {
            return new AccountJournalReport(journal, accounts, setup);
        }

        public ITotalJournalReport CreateTotalJournal(
            AccountingDataJournal journal,
            AccountingDataSetup setup)
        {
            return new TotalJournalReport(journal, setup);
        }

        public IAnnualBalanceReport CreateAnnualBalance(
            AccountingDataJournal journal,
            IEnumerable<AccountDefinition> accounts,
            AccountingDataSetup setup)
        {
            return new AnnualBalanceReport(journal, accounts, setup);
        }

        public ITotalsAndBalancesReport CreateTotalsAndBalances(
            AccountingDataJournal journal,
            IEnumerable<AccountingDataAccountGroup> accounts,
            AccountingDataSetup setup)
        {
            return new TotalsAndBalancesReport(journal, accounts, setup);
        }
    }
}
