// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    using System.Collections.Generic;
    using System.Globalization;
    using lg2de.SimpleAccounting.Model;

    internal interface IReportFactory
    {
        IAccountJournalReport CreateAccountJournal(
            AccountingDataJournal journal,
            IEnumerable<AccountDefinition> accounts,
            AccountingDataSetup setup,
            CultureInfo culture);

        ITotalJournalReport CreateTotalJournal(
            AccountingDataJournal journal,
            AccountingDataSetup setup,
            CultureInfo culture);

        IAnnualBalanceReport CreateAnnualBalance(
            AccountingDataJournal journal,
            IEnumerable<AccountDefinition> accounts,
            AccountingDataSetup setup,
            CultureInfo culture);

        ITotalsAndBalancesReport CreateTotalsAndBalances(
            AccountingDataJournal journal,
            IEnumerable<AccountingDataAccountGroup> accounts,
            AccountingDataSetup setup,
            CultureInfo culture);
    }
}
