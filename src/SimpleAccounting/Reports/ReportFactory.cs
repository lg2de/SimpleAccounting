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
            IEnumerable<AccountDefinition> accounts,
            AccountingDataJournal journal,
            AccountingDataSetup setup,
            CultureInfo culture)
        {
            return new AccountJournalReport(accounts, journal, setup, culture);
        }
    }
}
