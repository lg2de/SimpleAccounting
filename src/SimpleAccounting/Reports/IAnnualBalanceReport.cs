// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    /// <summary>
    ///     Defines abstraction for the annual balance report.
    /// </summary>
    internal interface IAnnualBalanceReport
    {
        void CreateReport(string title);

        void ShowPreview(string documentName);
    }
}
