// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System.Windows;

    public class AccountJournalItemViewModel : JournalItemBaseViewModel
    {
        public double CreditValue { get; set; }

        public double DebitValue { get; set; }

        public string RemoteAccount { get; set; } = string.Empty;

        public bool IsSummary { get; set; }

        public Visibility SummaryVisibility => this.IsSummary ? Visibility.Hidden : Visibility.Visible;
    }
}
