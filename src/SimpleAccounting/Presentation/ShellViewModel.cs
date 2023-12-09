// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Caliburn.Micro;
using lg2de.SimpleAccounting.Extensions;
using lg2de.SimpleAccounting.Infrastructure;
using lg2de.SimpleAccounting.Model;

internal class ShellViewModel : Screen, IDisposable
{
    private readonly IApplicationUpdate applicationUpdate;
    private readonly string version;

    private Task autoSaveTask = Task.CompletedTask;
    private CancellationTokenSource? cancellationTokenSource;

    public ShellViewModel(
        IProjectData projectData,
        IBusy busy,
        IMenuViewModel menu,
        IFullJournalViewModel fullJournal,
        IAccountJournalViewModel accountJournal,
        IAccountsViewModel accounts,
        IApplicationUpdate applicationUpdate)
    {
        this.ProjectData = projectData;
        this.Busy = busy;
        this.Menu = menu;
        this.FullJournal = fullJournal;
        this.AccountJournal = accountJournal;
        this.Accounts = accounts;
        this.applicationUpdate = applicationUpdate;

        this.version = this.GetType().GetInformationalVersion();

        // TODO SVM is too much responsible
        this.ProjectData.DataLoaded += (_, _) =>
        {
            this.Accounts.OnDataLoaded();
            this.Menu.OnDataLoaded();
        };
        this.ProjectData.YearChanged += (_, _) =>
        {
            this.UpdateDisplayName();
            this.FullJournal.Rebuild();
            this.Accounts.SelectFirstAccount();
        };
        this.ProjectData.JournalChanged += (_, args) =>
        {
            this.FullJournal.Rebuild();
            this.FullJournal.Select(args.ChangedBookingId);

            if (this.Accounts.SelectedAccount == null
                || !args.AffectedAccounts.Contains(this.Accounts.SelectedAccount.Identifier))
            {
                return;
            }

            this.AccountJournal.Rebuild(this.Accounts.SelectedAccount.Identifier);
            this.AccountJournal.Select(args.ChangedBookingId);
        };
        this.Accounts.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(this.Accounts.SelectedAccount))
            {
                this.AccountJournal.Rebuild(this.Accounts.SelectedAccount?.Identifier ?? 0);
            }
        };
    }

    public IBusy Busy { get; }

    public IMenuViewModel Menu { get; }

    public IFullJournalViewModel FullJournal { get; }

    public IAccountJournalViewModel AccountJournal { get; }

    public IAccountsViewModel Accounts { get; }

    public ICommand CloseApplicationCommand => new RelayCommand(_ => this.TryCloseAsync());

    public IAsyncCommand HelpCheckForUpdateCommand => new AsyncCommand(this.Busy, this.OnCheckForUpdateAsync);

    public ICommand NewAccountCommand => new RelayCommand(_ => this.Accounts.ShowNewAccountDialogAsync());

    public ICommand EditAccountCommand =>
        new RelayCommand(commandParameter => this.Accounts.OnEditAccountAsync(commandParameter));

    internal IProjectData ProjectData { get; }

    internal Task LoadingTask { get; private set; } = Task.CompletedTask;

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    public override async Task<bool> CanCloseAsync(CancellationToken cancellationToken = new())
    {
        await base.CanCloseAsync(cancellationToken);

        if (!this.ProjectData.CanDiscardModifiedProject())
        {
            return false;
        }

        this.ProjectData.RemoveAutoSaveFile();

        return true;
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        await base.OnInitializeAsync(cancellationToken);

        this.UpdateDisplayName();
    }

    protected override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);

        var dispatcher = Dispatcher.CurrentDispatcher;
        this.cancellationTokenSource = new CancellationTokenSource();
        if (!string.IsNullOrEmpty(this.ProjectData.Settings.RecentProject))
        {
            // We move execution into thread pool thread.
            // In case there is an auto-save file, the dialog should be shown on top of main window.
            // Therefore OnActivate needs to completed.
            this.LoadingTask = Task.Run(
                async () =>
                {
                    // re-invoke onto UI thread
                    await dispatcher.Invoke(
                        async () =>
                        {
                            this.Busy.IsBusy = true;
                            await this.ProjectData.LoadFromFileAsync(this.ProjectData.Settings.RecentProject);
                            this.Menu.BuildRecentProjectsMenu();
                            this.Busy.IsBusy = false;
                        });
                    this.autoSaveTask = this.ProjectData.AutoSaveAsync(this.cancellationTokenSource.Token);
                });
        }
        else
        {
            this.Menu.BuildRecentProjectsMenu();
            this.autoSaveTask = Task.Run(() => this.ProjectData.AutoSaveAsync(this.cancellationTokenSource.Token));
        }
    }

    [SuppressMessage(
        "Blocker Code Smell", "S4462:Calls to \"async\" methods should not be blocking",
        Justification = "Work-around for missing async Screen")]
    [SuppressMessage(
        "Critical Bug", "S2952:Classes should \"Dispose\" of members from the classes' own \"Dispose\" methods",
        Justification = "FP")]
    protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
    {
        await this.LoadingTask;
        await this.cancellationTokenSource!.CancelAsync();
        await this.autoSaveTask;
        this.cancellationTokenSource.Dispose();
        this.cancellationTokenSource = null;

        this.ProjectData.Settings.Save();

        await base.OnDeactivateAsync(close, cancellationToken);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.cancellationTokenSource?.Dispose();
        }
    }

    private async Task OnCheckForUpdateAsync()
    {
        if (!await this.applicationUpdate.IsUpdateAvailableAsync(this.version))
        {
            return;
        }

        if (!this.ProjectData.CanDiscardModifiedProject())
        {
            return;
        }

        // starts separate process to update application in-place
        // Now we need to close this application.
        if (!this.applicationUpdate.StartUpdateProcess())
        {
            return;
        }

        // The user was asked whether saving the project (CanDiscardModifiedProject).
        // It may have answered "No". So, the project may still be modified.
        // We do not want to ask again, and he doesn't want to save.
        this.ProjectData.IsModified = false;

        await this.TryCloseAsync();
    }

    private void UpdateDisplayName()
    {
        this.DisplayName = string.IsNullOrEmpty(this.ProjectData.FileName)
            ? $"SimpleAccounting {this.version}"
            : $"SimpleAccounting {this.version} - {this.ProjectData.FileName} - {this.ProjectData.CurrentYear.Year}";
    }
}
