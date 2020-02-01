// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    using System;

    internal interface IAccountJournalReport
    {
        bool PageBreakBetweenAccounts { get; set; }

        void CreateReport(DateTime dateStart, DateTime dateEnd);

        void ShowPreview(string documentName);
    }
}