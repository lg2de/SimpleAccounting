// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using lg2de.SimpleAccounting.Model;

    /// <summary>
    ///     Implements <see cref="IReportFactory"/> using real report implementations.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class ReportFactory : IReportFactory
    {
        public IAccountJournalReport CreateAccountJournal(IProjectData projectData)
        {
            return new AccountJournalReport(new XmlPrinter(), projectData);
        }

        public ITotalJournalReport CreateTotalJournal(IProjectData projectData)
        {
            return new TotalJournalReport(new XmlPrinter(), projectData);
        }

        public IAnnualBalanceReport CreateAnnualBalance(IProjectData projectData)
        {
            return new AnnualBalanceReport(new XmlPrinter(), projectData);
        }

        public ITotalsAndBalancesReport CreateTotalsAndBalances(
            IProjectData projectData,
            IEnumerable<AccountingDataAccountGroup> accounts)
        {
            return new TotalsAndBalancesReport(new XmlPrinter(), projectData, accounts);
        }
    }
}
