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
            IEnumerable<AccountDefinition> accounts,
            AccountingDataJournal journal,
            AccountingDataSetup setup,
            CultureInfo culture);
    }
}
