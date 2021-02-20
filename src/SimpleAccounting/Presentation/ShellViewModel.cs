// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
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
    using lg2de.SimpleAccounting.Properties;

    internal class ShellViewModel : Screen, IDisposable
    {
        private readonly IApplicationUpdate applicationUpdate;
        private readonly string version;

        private Task autoSaveTask = Task.CompletedTask;
        private CancellationTokenSource? cancellationTokenSource;

        public ShellViewModel(
            Settings settings,
            IProjectData projectData,
            IMenuViewModel menu,
            IFullJournalViewModel fullJournal,
            IAccountJournalViewModel accountJournal,
            IAccountsViewModel accounts,
            IApplicationUpdate applicationUpdate)
        {
            this.Settings = settings;
            this.ProjectData = projectData;
            this.Menu = menu;
            this.FullJournal = fullJournal;
            this.AccountJournal = accountJournal;
            this.Accounts = accounts;
            this.applicationUpdate = applicationUpdate;

            this.version = this.GetType().GetInformationalVersion();

            this.ProjectData.YearChanged += (_, __) =>
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

        public IMenuViewModel Menu { get; }
        
        public IFullJournalViewModel FullJournal { get; }

        public IAccountJournalViewModel AccountJournal { get; }

        public IAccountsViewModel Accounts { get; }

        public ICommand CloseApplicationCommand => new RelayCommand(_ => this.TryClose());

        public IAsyncCommand HelpCheckForUpdateCommand => new AsyncCommand(this.Menu, this.OnCheckForUpdateAsync);

        public ICommand NewAccountCommand => new RelayCommand(_ => this.Accounts.ShowNewAccountDialog());

        public ICommand EditAccountCommand => new RelayCommand(this.Accounts.OnEditAccount);

        internal Settings Settings { get; }
        
        internal IProjectData ProjectData { get; }

        internal Task LoadingTask { get; private set; } = Task.CompletedTask;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override void CanClose(Action<bool> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (!this.ProjectData.CheckSaveProject())
            {
                callback(false);
                return;
            }

            this.ProjectData.RemoveAutoSaveFile();

            base.CanClose(callback);
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            this.UpdateDisplayName();
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            var dispatcher = Dispatcher.CurrentDispatcher;
            this.cancellationTokenSource = new CancellationTokenSource();
            if (!string.IsNullOrEmpty(this.Settings.RecentProject))
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
                                this.Menu.IsBusy = true;
                                await this.ProjectData.LoadFromFileAsync(this.Settings.RecentProject);
                                this.Menu.BuildRecentProjectsMenu();
                                this.Menu.IsBusy = false;
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
        protected override void OnDeactivate(bool close)
        {
            this.LoadingTask.Wait();
            this.cancellationTokenSource!.Cancel();
            this.autoSaveTask.Wait();
            this.cancellationTokenSource.Dispose();
            this.cancellationTokenSource = null;

            this.Settings.Save();

            base.OnDeactivate(close);
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

            if (!this.ProjectData.CheckSaveProject())
            {
                return;
            }

            // starts separate process to update application in-place
            // Now we need to close this application.
            this.applicationUpdate.StartUpdateProcess();

            // The user was asked whether saving the project (CheckSaveProject).
            // It may have answered "No". So, the project may still be modified.
            // We do not want to ask again, and he doesn't want to save.
            this.ProjectData.IsModified = false;

            this.TryClose();
        }

        private void UpdateDisplayName()
        {
            this.DisplayName = string.IsNullOrEmpty(this.ProjectData.FileName)
                ? $"SimpleAccounting {this.version}"
                : $"SimpleAccounting {this.version} - {this.ProjectData.FileName} - {this.ProjectData.CurrentYear.Year}";
        }
    }
}
