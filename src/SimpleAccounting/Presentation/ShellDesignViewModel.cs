// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using lg2de.SimpleAccounting.Model;

    [SuppressMessage(
        "Major Code Smell", "S109:Magic numbers should not be used",
        Justification = "Design view model defines useful values")]
    [SuppressMessage(
        "Major Code Smell", "S4055:Literals should not be passed as localized parameters",
        Justification = "Design view model defines useful values")]
    internal class ShellDesignViewModel : ShellViewModel
    {
        public ShellDesignViewModel()
            : base(null, null, null, null, null)
        {
            var menuItem = new MenuViewModel("c:\\Test.acml", null);
            this.RecentProjects.Add(menuItem);

            var accountItem = new AccountViewModel
            {
                Identifier = 100,
                Name = "Kasse",
                Group = new AccountingDataAccountGroup { Name = "Bestandskonten" },
                Type = AccountDefinitionType.Asset
            };
            this.AccountList.Add(accountItem);
            this.SelectedAccount = accountItem;

            var journalItem = new FullJournalViewModel
            {
                Identifier = 42,
                Date = DateTime.Now,
                Text = "Booking1",
                Value = 123.4,
                CreditAccount = "100",
                DebitAccount = "400"
            };
            this.FullJournal.Add(journalItem);
            journalItem = new FullJournalViewModel
            {
                Identifier = 42,
                Date = DateTime.Now,
                Text = "Booking2",
                Value = 1.444,
                CreditAccount = "101",
                DebitAccount = "401"
            };
            this.FullJournal.Add(journalItem);

            var accountJournalItem = new AccountJournalViewModel
            {
                Identifier = 42,
                Date = DateTime.Now,
                Text = "Booking1",
                CreditValue = 123.4,
                RemoteAccount = "Div."
            };
            this.AccountJournal.Add(accountJournalItem);
            accountJournalItem = new AccountJournalViewModel
            {
                Identifier = 44,
                Date = DateTime.Now,
                Text = "Booking",
                CreditValue = 1.222,
                RemoteAccount = "Div."
            };
            this.AccountJournal.Add(accountJournalItem);
            accountJournalItem = new AccountJournalViewModel
            {
                IsSummary = true, Text = "Summe", CreditValue = 123.4, RemoteAccount = "Div."
            };
            this.AccountJournal.Add(accountJournalItem);
        }
    }
}
