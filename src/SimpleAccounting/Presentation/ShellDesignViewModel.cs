// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System;
using System.Diagnostics.CodeAnalysis;
using lg2de.SimpleAccounting.Abstractions;
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
    private static readonly IClock Clock = new SystemClock();
    private static readonly ProjectData DesignProject = new(new Settings(), null!, null!, null!, Clock, null!);

    public ShellDesignViewModel()
        : base(
            DesignProject,
            new BusyControlModel(),
            new MenuViewModel(DesignProject, null!, null!, Clock, null!, null!),
            new FullJournalViewModel(DesignProject),
            new AccountJournalViewModel(DesignProject), new AccountsViewModel(null!, DesignProject), null!, null!,
            null!,
            null!,
            new SystemClock())
    {
        var menuItem = new MenuItemViewModel("c:\\Test.acml", null!);
        this.Menu.RecentProjects.Add(menuItem);

        // load sample accounts and journal
        this.LoadAccounts();
        this.LoadJournal();
    }

    private void LoadAccounts()
    {
        this.ProjectData.Storage.Accounts =
        [
            new AccountingDataAccountGroup
            {
                Name = "Bestandskonten",
                Account = [new AccountDefinition { ID = 100, Name = "Kasse", Type = AccountDefinitionType.Asset }]
            }
        ];
        this.Accounts.OnDataLoaded();
    }

    [SuppressMessage(
        "Major Code Smell",
        "S6354:Use a testable date/time provider",
        Justification = "Ok for testing code")]
    private void LoadJournal()
    {
        int index = 0;
        var journalItem = new FullJournalItemViewModel(index++)
        {
            Identifier = 41,
            Date = DateTime.Now,
            Text = "Booking 1",
            Value = 123.4,
            CreditAccount = "100",
            DebitAccount = "400"
        };
        this.FullJournal.Items.Add(journalItem);
        journalItem = new FullJournalItemViewModel(index++)
        {
            Identifier = 42,
            Date = DateTime.Now,
            Text = "Booking 2",
            Value = 1.444,
            CreditAccount = "101",
            DebitAccount = "401",
            IsFollowup = true
        };
        this.FullJournal.Items.Add(journalItem);
        journalItem = new FullJournalItemViewModel(index++)
        {
            Identifier = 43,
            Date = DateTime.Now,
            Text = "Booking 3",
            Value = 99,
            CreditAccount = "101",
            DebitAccount = "401",
        };
        this.FullJournal.Items.Add(journalItem);
        journalItem = new FullJournalItemViewModel(index)
        {
            Identifier = 44,
            Date = DateTime.Now,
            Text = "Booking 4",
            Value = -99,
            CreditAccount = "101",
            DebitAccount = "401",
        };
        this.FullJournal.Items.Add(journalItem);
        this.FullJournal.Items.UpdateRowHighlighting();

        index = 0;
        var accountJournalItem = new AccountJournalItemViewModel(index++)
        {
            Identifier = 42,
            Date = DateTime.Now,
            Text = "Booking 2",
            CreditValue = 123.4,
            RemoteAccount = "Div.",
            IsFollowup = true
        };
        this.AccountJournal.Items.Add(accountJournalItem);
        accountJournalItem = new AccountJournalItemViewModel(index++)
        {
            Identifier = 44,
            Date = DateTime.Now,
            Text = "Booking 4",
            CreditValue = 1.222,
            RemoteAccount = "Div."
        };
        this.AccountJournal.Items.Add(accountJournalItem);
        accountJournalItem = new AccountJournalItemViewModel(index)
        {
            IsSummary = true, Text = "Summe", CreditValue = 123.4, RemoteAccount = "Div."
        };
        this.AccountJournal.Items.Add(accountJournalItem);
        this.AccountJournal.Items.UpdateRowHighlighting();
    }
}
