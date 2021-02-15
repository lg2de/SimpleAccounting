// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    using System.Collections.Generic;
    using lg2de.SimpleAccounting.Model;

    internal interface IReportFactory
    {
        IAccountJournalReport CreateAccountJournal(ProjectData projectData);

        ITotalJournalReport CreateTotalJournal(ProjectData projectData);

        IAnnualBalanceReport CreateAnnualBalance(ProjectData projectData);

        ITotalsAndBalancesReport CreateTotalsAndBalances(
            ProjectData projectData, IEnumerable<AccountingDataAccountGroup> accounts);
    }
}
