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
        public IAccountJournalReport CreateAccountJournal(IProjectData projectData)
        {
            return new AccountJournalReport(projectData);
        }

        public ITotalJournalReport CreateTotalJournal(IProjectData projectData)
        {
            return new TotalJournalReport(projectData);
        }

        public IAnnualBalanceReport CreateAnnualBalance(IProjectData projectData)
        {
            return new AnnualBalanceReport(projectData);
        }

        public ITotalsAndBalancesReport CreateTotalsAndBalances(
            IProjectData projectData,
            IEnumerable<AccountingDataAccountGroup> accounts)
        {
            return new TotalsAndBalancesReport(projectData, accounts);
        }
    }
}
