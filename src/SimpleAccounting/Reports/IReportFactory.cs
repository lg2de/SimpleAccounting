// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    using System.Collections.Generic;
    using lg2de.SimpleAccounting.Model;

    internal interface IReportFactory
    {
        IAccountJournalReport CreateAccountJournal(IProjectData projectData);

        ITotalJournalReport CreateTotalJournal(IProjectData projectData);

        IAnnualBalanceReport CreateAnnualBalance(IProjectData projectData);

        ITotalsAndBalancesReport CreateTotalsAndBalances(
            IProjectData projectData, IEnumerable<AccountingDataAccountGroup> accounts);
    }
}
