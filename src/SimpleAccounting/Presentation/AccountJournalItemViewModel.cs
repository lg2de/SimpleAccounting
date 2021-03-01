// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    /// <summary>
    ///     Implements the view model for a single entry in the account journal.
    /// </summary>
    public class AccountJournalItemViewModel : JournalItemBaseViewModel
    {
        public double CreditValue { get; set; }

        public double DebitValue { get; set; }

        public string RemoteAccount { get; set; } = string.Empty;

        public bool IsSummary { get; set; }
    }
}
