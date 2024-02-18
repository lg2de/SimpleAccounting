// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Model;

/// <summary>
///     Implements <see cref="IReportFactory"/> using real report implementations.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "This is a simple wrapper without test relevance.")]
[UsedImplicitly]
internal class ReportFactory : IReportFactory
{
    private readonly IClock clock;

    public ReportFactory(IClock clock)
    {
        this.clock = clock;
    }

    public IAccountJournalReport CreateAccountJournal(IProjectData projectData)
    {
        return new AccountJournalReport(new XmlPrinter(), projectData, this.clock);
    }

    public ITotalJournalReport CreateTotalJournal(IProjectData projectData)
    {
        return new TotalJournalReport(new XmlPrinter(), projectData, this.clock);
    }

    public IAnnualBalanceReport CreateAnnualBalance(IProjectData projectData)
    {
        return new AnnualBalanceReport(new XmlPrinter(), projectData, this.clock);
    }

    public ITotalsAndBalancesReport CreateTotalsAndBalances(
        IProjectData projectData,
        IEnumerable<AccountingDataAccountGroup> accounts)
    {
        return new TotalsAndBalancesReport(new XmlPrinter(), projectData, this.clock, accounts);
    }
}
