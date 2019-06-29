﻿// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

using System.Windows;

namespace lg2de.SimpleAccounting.Presentation
{
    public class AccountJournalViewModel : JournalBaseViewModel
    {
        public double CreditValue { get; set; }

        public double DebitValue { get; set; }

        public string RemoteAccount { get; set; }

        public bool IsSummary { get; set; }

        public Visibility SummaryVisibility => this.IsSummary ? Visibility.Hidden : Visibility.Visible;
    }
}