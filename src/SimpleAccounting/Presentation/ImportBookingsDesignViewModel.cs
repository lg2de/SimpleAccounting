// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

#pragma warning disable CA1303 // Do not pass literals as localized parameters because design view model defines useful values
namespace lg2de.SimpleAccounting.Presentation;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Properties;

/// <summary>
///     Implements the view model for the booking importer in the designer.
/// </summary>
[SuppressMessage(
    "Major Code Smell", "S109:Magic numbers should not be used",
    Justification = "Design view model defines useful values")]
[SuppressMessage(
    "Major Code Smell", "S4055:Literals should not be passed as localized parameters",
    Justification = "Design view model defines useful values")]
[SuppressMessage(
    "Major Code Smell",
    "S6354:Use a testable date/time provider",
    Justification = "Is ok for design view model.")]
internal class ImportBookingsDesignViewModel : ImportBookingsViewModel
{
    // define some accounts
    private static readonly List<AccountDefinition> SampleAccounts =
    [
        new AccountDefinition
        {
            ID = 100,
            Name = "Cash",
            ImportMapping = new AccountDefinitionImportMapping
            {
                Columns =
                [
                    new AccountDefinitionImportMappingColumn
                    {
                        Source = "A", Target = AccountDefinitionImportMappingColumnTarget.Date
                    },
                    new AccountDefinitionImportMappingColumn
                    {
                        Source = "B", Target = AccountDefinitionImportMappingColumnTarget.Value
                    }
                ]
            }
        },
        new AccountDefinition { ID = 600, Name = "Shopping" },
        new AccountDefinition { ID = 990, Name = "Carryforward" }
    ];

    // build sample project with journal entries
    private static readonly AccountingData SampleData = new()
    {
        Journal =
        [
            new AccountingDataJournal
            {
                DateStart = (uint)(DateTime.Today.Year * 10000 + 101),
                DateEnd = (uint)(DateTime.Today.Year * 10000 + 1231),
                Booking =
                [
                    new AccountingDataJournalBooking
                    {
                        Date = (uint)(DateTime.Today.Year * 10000 + 1231),
                        ID = 999,
                        Credit = [new BookingValue { Account = 100, Text = "End of year", Value = 1234 }],
                        Debit = [new BookingValue { Account = 990, Text = "End of year", Value = 1234 }]
                    }
                ]
            }
        ],
        Accounts = [new AccountingDataAccountGroup { Account = SampleAccounts }]
    };

    public ImportBookingsDesignViewModel()
        : base(null!, null!, new ProjectData(new Settings(), null!, null!, null!, new SystemClock(), null!))
    {
        this.ProjectData.LoadData(SampleData);
        this.SelectedAccountNumber = 100;
        this.StartDate = DateTime.Today;

        this.LoadedData.Add(
            new ImportEntryViewModel(SampleAccounts)
            {
                Date = DateTime.Now - TimeSpan.FromDays(1),
                Name = "Should not be visible!",
                Text = "Should not be visible!",
                RemoteAccount = SampleAccounts.Single(x => x.ID == 600),
                Value = 99.95
            });
        this.LoadedData.Add(
            new ImportEntryViewModel(SampleAccounts)
            {
                Date = DateTime.Now,
                Name = "McX",
                Text = "Shoes",
                RemoteAccount = SampleAccounts.Single(x => x.ID == 600),
                Value = 99.95,
                IsFollowup = true
            });
        this.LoadedData.Add(
            new ImportEntryViewModel(SampleAccounts)
            {
                Date = DateTime.Now + TimeSpan.FromDays(1),
                Name = "McY",
                Text = "More Shoes",
                Value = 159.95,
                IsSkip = true
            });

        this.SetupExisting();
        this.UpdateIdentifierInLoadedData();
    }
}
