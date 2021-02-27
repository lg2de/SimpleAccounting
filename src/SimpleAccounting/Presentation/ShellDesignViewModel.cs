// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Properties;

    /// <summary>
    ///     Implements the root view model for the designer.
    /// </summary>
    [SuppressMessage(
        "Major Code Smell", "S109:Magic numbers should not be used",
        Justification = "Design view model defines useful values")]
    [SuppressMessage(
        "Major Code Smell", "S4055:Literals should not be passed as localized parameters",
        Justification = "Design view model defines useful values")]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    internal class ShellDesignViewModel : ShellViewModel
    {
        private static readonly Settings DesignSettings = new Settings();
        private static readonly ProjectData DesignProject = new ProjectData(DesignSettings, null!, null!, null!, null!);

        public ShellDesignViewModel()
            : base(
                DesignSettings,
                DesignProject,
                new MenuViewModel(DesignSettings, DesignProject, null!, null!, null!),
                new FullJournalViewModel(DesignProject),
                new AccountJournalViewModel(DesignProject),
                new AccountsViewModel(null!, DesignProject), null!)
        {
            var menuItem = new MenuItemViewModel("c:\\Test.acml", null!);
            this.Menu.RecentProjects.Add(menuItem);

            // load sample accounts and journal
            this.LoadAccounts();
            this.LoadJournal();
        }

        private void LoadAccounts()
        {
            this.Accounts.LoadAccounts(
                new List<AccountingDataAccountGroup>
                {
                    new AccountingDataAccountGroup
                    {
                        Name = "Bestandskonten",
                        Account = new List<AccountDefinition>
                        {
                            new AccountDefinition
                            {
                                ID = 100, Name = "Kasse", Type = AccountDefinitionType.Asset
                            }
                        }
                    }
                });
        }

        private void LoadJournal()
        {
            var journalItem = new FullJournalItemViewModel
            {
                Identifier = 41,
                Date = DateTime.Now,
                Text = "Booking1",
                Value = 123.4,
                CreditAccount = "100",
                DebitAccount = "400"
            };
            this.FullJournal.Items.Add(journalItem);
            journalItem = new FullJournalItemViewModel
            {
                Identifier = 42,
                Date = DateTime.Now,
                Text = "Booking2",
                Value = 1.444,
                CreditAccount = "101",
                DebitAccount = "401",
                IsFollowup = true
            };
            this.FullJournal.Items.Add(journalItem);
            journalItem = new FullJournalItemViewModel
            {
                Identifier = 43,
                Date = DateTime.Now,
                Text = "Booking3",
                Value = 99,
                CreditAccount = "101",
                DebitAccount = "401",
            };
            this.FullJournal.Items.Add(journalItem);
            journalItem = new FullJournalItemViewModel
            {
                Identifier = 44,
                Date = DateTime.Now,
                Text = "Booking4",
                Value = -99,
                CreditAccount = "101",
                DebitAccount = "401",
            };
            this.FullJournal.Items.Add(journalItem);
            this.FullJournal.Items.UpdateRowHighlighting();

            var accountJournalItem = new AccountJournalItemViewModel
            {
                Identifier = 42,
                Date = DateTime.Now,
                Text = "Booking1",
                CreditValue = 123.4,
                RemoteAccount = "Div."
            };
            this.AccountJournal.Items.Add(accountJournalItem);
            accountJournalItem = new AccountJournalItemViewModel
            {
                Identifier = 44,
                Date = DateTime.Now,
                Text = "Booking",
                CreditValue = 1.222,
                RemoteAccount = "Div."
            };
            this.AccountJournal.Items.Add(accountJournalItem);
            accountJournalItem = new AccountJournalItemViewModel
            {
                IsSummary = true, Text = "Summe", CreditValue = 123.4, RemoteAccount = "Div."
            };
            this.AccountJournal.Items.Add(accountJournalItem);
            this.AccountJournal.Items.UpdateRowHighlighting();
        }
    }
}
