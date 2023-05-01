// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports;

/// <summary>
///     Defines abstraction for the total (overall) journal report.
/// </summary>
internal interface ITotalJournalReport
{
    void CreateReport(string title);

    void ShowPreview(string documentName);
}
