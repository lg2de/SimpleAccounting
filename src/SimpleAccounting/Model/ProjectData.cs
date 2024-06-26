// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Model;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Caliburn.Micro;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Extensions;
using lg2de.SimpleAccounting.Infrastructure;
using lg2de.SimpleAccounting.Presentation;
using lg2de.SimpleAccounting.Properties;

/// <summary>
///     Implements the storage for all data of the current project.
/// </summary>
/// <remarks>
///     It contains the persistent data according to <see cref="AccountingData" />
///     as well as the current state of the project.
/// </remarks>
[SuppressMessage(
    "Major Code Smell", "S1200:Classes should not be coupled to too many other classes",
    Justification = "This is the wrapper for the model which is generated code. So, this is ok here.")]
internal sealed class ProjectData : IProjectData, IDisposable
{
    private readonly IClock clock;
    private readonly IDialogs dialogs;
    private readonly IFileSystem fileSystem;
    private readonly IProcess processApi;
    private readonly SemaphoreSlim saveSynchronization = new(initialCount: 1, maxCount: 1);
    private readonly IWindowManager windowManager;
    private Task autoSaveTask = Task.CompletedTask;
    private CancellationTokenSource? cancellationTokenSource;
    private IDisposable? fileMonitoringHandle;
    private DateTime lastNotification = DateTime.MinValue;
    private TaskCompletionSource? savingCompleted;
    private AccountingData storage;

    public ProjectData(
        Settings settings,
        IWindowManager windowManager, IDialogs dialogs, IFileSystem fileSystem,
        IClock clock, IProcess processApi)
    {
        this.Settings = settings;
        this.windowManager = windowManager;
        this.dialogs = dialogs;
        this.fileSystem = fileSystem;
        this.clock = clock;
        this.processApi = processApi;

        this.storage = new AccountingData();
        this.CurrentYear = this.storage.Journal.GetOrCreateLatest(this.clock.Now());
    }

    internal Task ProjectChangedHandlerTask { get; private set; } = Task.CompletedTask;

    public void Dispose()
    {
        this.cancellationTokenSource?.Dispose();
    }

    public Settings Settings { get; }

    public string FileName { get; set; } = string.Empty;

    public AccountingData Storage
    {
        get => this.storage;
        private set
        {
            this.storage = value;
            this.CurrentYear = this.Storage.Journal.GetOrCreateLatest(this.clock.Now());
            Execute.OnUIThread(() => this.DataLoaded(this, EventArgs.Empty));
        }
    }

    public AccountingDataJournal CurrentYear { get; private set; }

    public bool ShowInactiveAccounts { get; set; }

    public bool IsModified { get; set; }

    public TimeSpan AutoSaveInterval { get; set; } = TimeSpan.FromMinutes(1);

    public string AutoSaveFileName => Defines.GetAutoSaveFileName(this.FileName);

    public string ReservationFileName => Defines.GetReservationFileName(this.FileName);

    public ulong MaxBookIdent => this.CurrentYear.Booking.Count == 0 ? 0 : this.CurrentYear.Booking.Max(b => b.ID);

    public event EventHandler DataLoaded = (_, _) => { };

    public event EventHandler YearChanged = (_, _) => { };

    public event EventHandler<JournalChangedEventArgs> JournalChanged = (_, _) => { };

    public void NewProject()
    {
        this.IsModified = false;
        this.FileName = "<new>";
        this.LoadData(AccountingData.GetTemplateProject(this.clock.Now().Year));
    }

    public void LoadData(AccountingData accountingData)
    {
        this.Storage = accountingData;
    }

    public async Task<OperationResult> LoadFromFileAsync(string projectFileName)
    {
        if (!await this.TryCloseAsync())
        {
            return OperationResult.Aborted;
        }

        var loader = new ProjectFileLoader(this.Settings, this.dialogs, this.fileSystem, this.processApi);
        var loadResult = await Task.Run(() => loader.LoadAsync(projectFileName));
        if (loadResult != OperationResult.Completed)
        {
            return loadResult;
        }

        this.FileName = projectFileName;
        this.Storage = loader.ProjectData;
        this.IsModified = loader.Migrated;

        this.ActivateMonitoring();

        return OperationResult.Completed;
    }

    public async Task<bool> SaveProjectAsync()
    {
        if (this.FileName == "<new>")
        {
            (DialogResult result, string fileName) =
                this.dialogs.ShowSaveFileDialog(Resources.FileFilter_MainProject);
            if (result != DialogResult.OK)
            {
                return false;
            }

            this.FileName = fileName;
        }

        this.savingCompleted = new TaskCompletionSource();
        try
        {
            await this.saveSynchronization.WaitAsync();

            var fileDate = this.fileSystem.GetLastWriteTime(this.FileName);
            var backupFileName = $"{this.FileName}.{fileDate:yyyyMMddHHmmss}";
            if (this.fileSystem.FileExists(this.FileName))
            {
                this.fileSystem.FileMove(this.FileName, backupFileName);
            }

            this.fileSystem.WriteAllTextIntoFile(this.FileName, this.Storage.Serialize());
            this.IsModified = false;

            if (this.fileSystem.FileExists(this.AutoSaveFileName))
            {
                this.fileSystem.FileDelete(this.AutoSaveFileName);
            }

            this.Settings.SetRecentProject(this.FileName);
        }
        finally
        {
            await Task.WhenAny(this.savingCompleted.Task, Task.Delay(TimeSpan.FromSeconds(1)));
            this.savingCompleted = null;
            this.saveSynchronization.Release();
        }

        this.ActivateMonitoring();

        return true;
    }

    [SuppressMessage(
        "Blocker Code Smell", "S4462:Calls to \"async\" methods should not be blocking",
        Justification = "We need to block the UI to show the exception before the application is closing.")]
    public void CrashSave()
    {
        if (!this.IsModified)
        {
            return;
        }

        this.cancellationTokenSource?.Cancel();
        this.autoSaveTask.Wait(TimeSpan.FromSeconds(2));
        this.fileSystem.WriteAllTextIntoFile(this.AutoSaveFileName, this.Storage.Serialize());
    }

    public async Task<bool> TryCloseAsync()
    {
        if (!await this.TryDiscardModifiedProjectAsync())
        {
            return false;
        }

        if (this.cancellationTokenSource != null)
        {
            await this.cancellationTokenSource.CancelAsync();
            await this.autoSaveTask;

            this.cancellationTokenSource.Dispose();
            this.cancellationTokenSource = null;

            this.fileMonitoringHandle?.Dispose();
            this.fileMonitoringHandle = null;
        }

        if (this.fileSystem.FileExists(this.AutoSaveFileName))
        {
            this.fileSystem.FileDelete(this.AutoSaveFileName);
        }

        if (this.fileSystem.FileExists(this.ReservationFileName))
        {
            this.fileSystem.FileDelete(this.ReservationFileName);
        }

        this.NewProject();
        return true;
    }

    public async Task EditProjectOptionsAsync()
    {
        var vm = new ProjectOptionsViewModel(this.Storage);
        if (await this.windowManager.ShowDialogAsync(vm) != true)
        {
            return;
        }

        this.IsModified = true;
    }

    public void AddBooking(AccountingDataJournalBooking booking, bool updateJournal)
    {
        this.CurrentYear.Booking.Add(booking);

        this.IsModified = true;

        if (updateJournal)
        {
            this.JournalChanged(this, new JournalChangedEventArgs(booking.ID, booking.GetAccounts()));
        }
    }

    public void SelectYear(string yearName)
    {
        this.CurrentYear = this.Storage.Journal.Single(y => y.Year == yearName);
        this.YearChanged(this, EventArgs.Empty);
    }

    public async Task ShowAddBookingDialogAsync(DateTime today, bool showInactiveAccounts)
    {
        var bookingModel =
            new EditBookingViewModel(this, today, editMode: false);
        var allAccounts = this.Storage.AllAccounts;
        bookingModel.Accounts.AddRange(showInactiveAccounts ? allAccounts : allAccounts.Where(x => x.Active));
        var bookingTemplates = this.Storage.Setup?.BookingTemplates;
        if (bookingTemplates != null)
        {
            bookingModel.AddTemplates(bookingTemplates);
        }

        await this.windowManager.ShowDialogAsync(bookingModel);
    }

    public async Task ShowDuplicateBookingDialogAsync(IJournalItem item, bool showInactiveAccounts)
    {
        if (item.StorageIndex < 0)
        {
            // summary item selected => ignore
            return;
        }

        var journalEntry = this.CurrentYear.Booking[item.StorageIndex];

        var bookingModel = this.BookingFromJournal(journalEntry, editMode: false, showInactiveAccounts);

        await this.windowManager.ShowDialogAsync(bookingModel);
    }

    public async Task ShowEditBookingDialogAsync(IJournalItem item, bool showInactiveAccounts)
    {
        if (item.StorageIndex < 0)
        {
            // summary item selected => ignore
            return;
        }

        var journalEntry = this.CurrentYear.Booking[item.StorageIndex];

        var bookingModel = this.BookingFromJournal(journalEntry, editMode: true, showInactiveAccounts);
        bookingModel.BookingIdentifier = journalEntry.ID;

        var result = await this.windowManager.ShowDialogAsync(bookingModel);
        if (result != true)
        {
            return;
        }

        // replace entry
        journalEntry = bookingModel.CreateJournalEntry();
        this.CurrentYear.Booking[item.StorageIndex] = journalEntry;

        this.IsModified = true;

        this.JournalChanged(this, new JournalChangedEventArgs(journalEntry.ID, journalEntry.GetAccounts()));
    }

    public Task ShowImportDialogAsync()
    {
        var importModel = new ImportBookingsViewModel(this.dialogs, this.fileSystem, this);
        return this.windowManager.ShowDialogAsync(
            importModel,
            settings: WindowsDialogs.SizeToContentManualSettings);
    }

    public async Task<bool> CloseYearAsync()
    {
        var viewModel = new CloseYearViewModel(this.CurrentYear);
        this.Storage.AllAccounts.Where(x => x is { Active: true, Type: AccountDefinitionType.Carryforward })
            .ToList().ForEach(viewModel.Accounts.Add);

        // restore project options
        var textOption = viewModel.TextOptions.Single(
            x => x.Option == this.Storage.Setup.Behavior.ParsedOpeningTextPattern);
        viewModel.TextOption = textOption;
        var remoteAccount =
            viewModel.Accounts.FirstOrDefault(x => x.ID == this.Storage.Setup.Behavior.LastCarryForward);
        if (remoteAccount != null)
        {
            viewModel.RemoteAccount = remoteAccount;
        }

        // show dialog
        var result = await this.windowManager.ShowDialogAsync(viewModel);
        if (result != true || viewModel.RemoteAccount == null)
        {
            // abort
            return false;
        }

        // proceed closing year
        this.Storage.CloseYear(this.CurrentYear, viewModel.RemoteAccount, viewModel.TextOption.Option);

        this.Storage.Setup.Behavior.LastCarryForwardSpecified = true;
        this.Storage.Setup.Behavior.LastCarryForward = viewModel.RemoteAccount.ID;
        this.Storage.Setup.Behavior.OpeningTextPattern = viewModel.TextOption.Option.ToString();

        this.IsModified = true;

        return true;
    }

    public void TriggerJournalChanged()
    {
        this.JournalChanged(
            this, new JournalChangedEventArgs(0, this.storage.AllAccounts.Select(x => x.ID).ToList()));
    }

    private void ActivateMonitoring()
    {
        if (this.cancellationTokenSource != null)
        {
            // already active
            return;
        }

        this.cancellationTokenSource = new CancellationTokenSource();
        this.autoSaveTask = this.AutoSaveAsync(this.cancellationTokenSource.Token);
        this.fileMonitoringHandle = this.fileSystem.StartMonitoring(this.FileName, this.OnProjectChanged);
    }

    private void OnProjectChanged(string fileName)
    {
        if (fileName.EndsWith(Defines.AutoSaveFileSuffix, StringComparison.OrdinalIgnoreCase))
        {
            // ignore changes according to auto-save
            return;
        }

        if (this.savingCompleted != null)
        {
            // we are currently in saving the (own) project
            this.savingCompleted.TrySetResult();
            return;
        }

        if ((this.clock.Now() - this.lastNotification) < TimeSpan.FromMilliseconds(100))
        {
            // debounce the file system watcher
            return;
        }

        this.lastNotification = this.clock.Now();

        this.ProjectChangedHandlerTask = this.ProjectChangedHandlerTask.ContinueWith(
            async _ =>
            {
                var result = this.dialogs.ShowMessageBox(
                    Resources.Question_ProjectChanged, Resources.Header_ProjectChanged, MessageBoxButton.YesNo,
                    MessageBoxImage.Warning, MessageBoxResult.Yes);
                if (result == MessageBoxResult.Yes)
                {
                    string newFileName =
                        Path.GetFileNameWithoutExtension(this.FileName)
                        + "_"
                        + this.clock.Now().ToString("yyyyMMddHHmmss", CultureInfo.CurrentUICulture);
                    this.FileName = Path.Combine(Path.GetDirectoryName(this.FileName)!, newFileName + ".acml");
                    await this.SaveProjectAsync();
                    await this.LoadFromFileAsync(this.FileName);
                }
                else
                {
                    this.IsModified = false;
                    this.NewProject();
                }
            });
    }

    private async Task AutoSaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                await Task.Delay(this.AutoSaveInterval, cancellationToken);
                await AutoSaveCycleAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // expected behavior
        }

        async Task AutoSaveCycleAsync()
        {
            if (!this.IsModified)
            {
                return;
            }

            try
            {
                await this.saveSynchronization.WaitAsync(cancellationToken);
                if (!this.IsModified)
                {
                    return;
                }

                this.fileSystem.WriteAllTextIntoFile(this.AutoSaveFileName, this.Storage.Serialize());
            }
            finally
            {
                this.saveSynchronization.Release();
            }
        }
    }

    private async Task<bool> TryDiscardModifiedProjectAsync()
    {
        if (!this.IsModified)
        {
            // no need to save the project
            return true;
        }

        var result = this.dialogs.ShowMessageBox(
            Resources.Question_SaveBeforeProceed,
            Resources.Header_Shutdown,
            MessageBoxButton.YesNoCancel);
        switch (result)
        {
        case MessageBoxResult.Yes:
            return await this.SaveProjectAsync();
        case MessageBoxResult.No:
            // User wants to discard changes.
            return true;
        default:
            // abort
            return false;
        }
    }

    private EditBookingViewModel BookingFromJournal(
        AccountingDataJournalBooking journalEntry, bool editMode, bool showInactiveAccounts)
    {
        var bookingModel = new EditBookingViewModel(this, journalEntry.Date.ToDateTime(), editMode)
        {
            IsFollowup = journalEntry.Followup, IsOpening = journalEntry.Opening
        };

        if (journalEntry.Credit.Count > 1)
        {
            journalEntry.Credit.Select(x => x.ToSplitModel()).ToList().ForEach(bookingModel.CreditSplitEntries.Add);
            var theDebit = journalEntry.Debit[0];
            bookingModel.DebitAccount = theDebit.Account;
            bookingModel.BookingText = theDebit.Text;
            bookingModel.BookingValue = theDebit.Value.ToViewModel();
        }
        else if (journalEntry.Debit.Count > 1)
        {
            journalEntry.Debit.Select(x => x.ToSplitModel()).ToList().ForEach(bookingModel.DebitSplitEntries.Add);
            var theCredit = journalEntry.Credit[0];
            bookingModel.CreditAccount = theCredit.Account;
            bookingModel.BookingText = theCredit.Text;
            bookingModel.BookingValue = theCredit.Value.ToViewModel();
        }
        else
        {
            var theDebit = journalEntry.Debit[0];
            bookingModel.DebitAccount = theDebit.Account;
            bookingModel.BookingValue = theDebit.Value.ToViewModel();
            bookingModel.CreditAccount = journalEntry.Credit[0].Account;
            bookingModel.BookingText = theDebit.Text;
        }

        var allAccounts = this.Storage.AllAccounts;
        bookingModel.Accounts.AddRange(showInactiveAccounts ? allAccounts : allAccounts.Where(x => x.Active));

        return bookingModel;
    }
}
