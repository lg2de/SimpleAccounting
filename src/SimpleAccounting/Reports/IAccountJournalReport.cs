// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    internal interface IAccountJournalReport
    {
        bool PageBreakBetweenAccounts { get; set; }

        void CreateReport();

        void ShowPreview(string documentName);
    }
}
