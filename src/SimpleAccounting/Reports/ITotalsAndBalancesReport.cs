// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    using System.Collections.Generic;

    /// <summary>
    ///     Defines the abstraction for the totals and balances report.
    /// </summary>
    internal interface ITotalsAndBalancesReport
    {
        List<string> Signatures { get; }

        void CreateReport(string title);

        void ShowPreview(string documentName);
    }
}
