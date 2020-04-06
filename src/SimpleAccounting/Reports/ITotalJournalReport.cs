// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    internal interface ITotalJournalReport
    {
        void CreateReport(string title);

        void ShowPreview(string documentName);
    }
}
