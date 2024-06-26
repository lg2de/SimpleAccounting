// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Caliburn.Micro;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Extensions;
using lg2de.SimpleAccounting.Infrastructure;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Properties;

/// <summary>
///     Implements the root view model of the application.
/// </summary>
internal class ShellViewModel : Screen
{
    private readonly IApplicationUpdate applicationUpdate;
    private readonly IWindowManager windowManager;
    private readonly IProcess processApi;
    private readonly IClipboard clipboard;
    private readonly IClock clock;
    private readonly string version;

    private readonly bool helpSimulateUpdateVisible =
#if DEBUG
        true;
#else
        false;
#endif

    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "checked")]
    public ShellViewModel(
        IProjectData projectData,
        IBusy busy,
        IMenuViewModel menu,
        IFullJournalViewModel fullJournal,
        IAccountJournalViewModel accountJournal,
        IAccountsViewModel accounts,
        IApplicationUpdate applicationUpdate,
        IWindowManager windowManager,
        IProcess processApi,
        IClipboard clipboard,
        IClock clock)
    {
        this.ProjectData = projectData;
        this.Busy = busy;
        this.Menu = menu;
        this.FullJournal = fullJournal;
        this.AccountJournal = accountJournal;
        this.Accounts = accounts;
        this.applicationUpdate = applicationUpdate;
        this.windowManager = windowManager;
        this.processApi = processApi;
        this.clipboard = clipboard;
        this.clock = clock;

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

    public ICommand CloseApplicationCommand => new AsyncCommand(() => this.TryCloseAsync());

    public IAsyncCommand HelpCheckForUpdateCommand => new AsyncCommand(
        this.Busy, () => this.OnCheckForUpdateAsync(userInvoked: true));

    [ExcludeFromCodeCoverage(Justification = "It's for manual testing only.")]
    public IAsyncCommand HelpSimulateUpdate => new AsyncCommand(this.Busy, this.SimulateUpdateAsync);

    [ExcludeFromCodeCoverage(Justification = "It's for manual testing only.")]
    // ReSharper disable once ConvertToAutoProperty
    public bool HelpSimulateUpdateVisible => this.helpSimulateUpdateVisible;

    public ICommand NewAccountCommand => new AsyncCommand(this.Accounts.ShowNewAccountDialogAsync);

    public ICommand EditAccountCommand => new AsyncCommand(x => this.Accounts.OnEditAccountAsync(x));

    internal IProjectData ProjectData { get; }

    internal Task LoadingTask { get; private set; } = Task.CompletedTask;

    public override async Task<bool> CanCloseAsync(CancellationToken cancellationToken = new())
    {
        await base.CanCloseAsync(cancellationToken);

        return await this.ProjectData.TryCloseAsync();
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        await base.OnInitializeAsync(cancellationToken);

        AppDomain.CurrentDomain.UnhandledException +=
            (_, args) => this.OnUnhandledException((Exception)args.ExceptionObject);
        if (Application.Current != null)
        {
            Application.Current.DispatcherUnhandledException += (_, args) => this.OnUnhandledException(args.Exception);
        }

        this.UpdateDisplayName();
    }

    [SuppressMessage(
        "Blocker Code Smell", "S4462:Calls to \"async\" methods should not be blocking",
        Justification = "We need to block the UI to show the exception before the application is closing.")]
    [SuppressMessage(
        "Blocker Code Smell", "S1147:Exit methods should not be called",
        Justification = "This is the final handler for unhandled exceptions.")]
    [ExcludeFromCodeCoverage(Justification = "Cannot be tested")]
    private void OnUnhandledException(Exception exception)
    {
        // run crash save, which is the synchronous version of the cyclic auto save
        this.ProjectData.CrashSave();

        // inform the user
        var vm = new ErrorMessageViewModel(this.processApi, this.clipboard)
        {
            DisplayName = Resources.Header_Termination,
            Introduction = Resources.ShellViewModel_UnhandledException,
            ErrorMessage = exception.Message,
            CallStack = exception.StackTrace ?? string.Empty
        };
        var task = this.windowManager.ShowDialogAsync(vm);
        task.Wait();

        // exit after unrecoverable error
        Environment.Exit(1);
    }

    protected override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);

        var dispatcher = Dispatcher.CurrentDispatcher;
        if (!string.IsNullOrEmpty(this.ProjectData.Settings.RecentProject))
        {
            // We move execution into thread pool thread.
            // In case there is an auto-save file, the dialog should be shown on top of main window.
            // Therefore, OnActivate needs to complete first.
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
                            await this.CheckForUpdateAfterStartAsync();
                            this.Busy.IsBusy = false;
                        });
                },
                cancellationToken);
        }
        else
        {
            this.Menu.BuildRecentProjectsMenu();
            await this.CheckForUpdateAfterStartAsync();
        }
    }

    protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
    {
        await this.LoadingTask;

        await this.ProjectData.TryCloseAsync();

        this.ProjectData.Settings.Save();

        await base.OnDeactivateAsync(close, cancellationToken);
    }

    private async Task CheckForUpdateAfterStartAsync()
    {
        var lastCheckTimeout = this.clock.Now() - this.ProjectData.Settings.LastUpdateCheck;
        if (lastCheckTimeout > TimeSpan.FromDays(1))
        {
            await this.OnCheckForUpdateAsync(userInvoked: false);
            this.ProjectData.Settings.LastUpdateCheck = this.clock.Now();
        }
    }

    private async Task OnCheckForUpdateAsync(bool userInvoked)
    {
        string packageName =
            await this.applicationUpdate.GetUpdatePackageAsync(userInvoked, this.version, CultureInfo.CurrentUICulture);
        if (string.IsNullOrWhiteSpace(packageName))
        {
            return;
        }

        if (!await this.ProjectData.TryCloseAsync())
        {
            return;
        }

        // starts separate process to update application in-place
        // Now we need to close this application.
        if (!this.applicationUpdate.StartUpdateProcess(packageName, dryRun: false))
        {
            return;
        }

        await this.TryCloseAsync();
    }

    [ExcludeFromCodeCoverage(Justification = "It's for manual testing only.")]
    private async Task SimulateUpdateAsync()
    {
        if (!await this.ProjectData.TryCloseAsync())
        {
            return;
        }

        if (!this.applicationUpdate.StartUpdateProcess(string.Empty, dryRun: true))
        {
            return;
        }

        await this.TryCloseAsync();
    }

    private void UpdateDisplayName()
    {
        this.DisplayName = string.IsNullOrEmpty(this.ProjectData.FileName)
            ? $"SimpleAccounting {this.version}"
            : $"SimpleAccounting {this.version} - {this.ProjectData.FileName} - {this.ProjectData.CurrentYear.Year}";
    }
}
