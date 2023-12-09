// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports;

/// <summary>
///     Defines abstraction for the account journal report.
/// </summary>
internal interface IAccountJournalReport
{
    bool PageBreakBetweenAccounts { get; set; }

    void CreateReport(string title);

    void ShowPreview(string documentName);
}
