// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Model;

using System;
using System.Threading.Tasks;
using lg2de.SimpleAccounting.Infrastructure;
using lg2de.SimpleAccounting.Presentation;
using lg2de.SimpleAccounting.Properties;

/// <summary>
///     Defines the interface for the runtime information of a single project loaded.
/// </summary>
internal interface IProjectData
{
    Settings Settings { get; }

    string FileName { get; set; }

    string AutoSaveFileName { get; }

    string ReservationFileName { get; }

    AccountingData Storage { get; }

    AccountingDataJournal CurrentYear { get; }

    bool ShowInactiveAccounts { get; set; }

    bool IsModified { get; set; }

    ulong MaxBookIdent { get; }

    TimeSpan AutoSaveInterval { get; set; }

    event EventHandler DataLoaded;

    event EventHandler YearChanged;

    event EventHandler<JournalChangedEventArgs> JournalChanged;

    void NewProject();

    void LoadData(AccountingData accountingData);

    Task<OperationResult> LoadFromFileAsync(string projectFileName);

    Task<bool> SaveProjectAsync();

    void CrashSave();

    Task<bool> TryCloseAsync();

    Task EditProjectOptionsAsync();

    Task ShowAddBookingDialogAsync(DateTime today, bool showInactiveAccounts);

    Task ShowEditBookingDialogAsync(IJournalItem item, bool showInactiveAccounts);

    Task ShowDuplicateBookingDialogAsync(IJournalItem item, bool showInactiveAccounts);

    Task ShowImportDialogAsync();

    Task<bool> CloseYearAsync();

    void TriggerJournalChanged();

    void AddBooking(AccountingDataJournalBooking booking, bool updateJournal);

    void SelectYear(string yearName);
}
