// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Input;
    using System.Windows.Threading;
    using Caliburn.Micro;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Infrastructure;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Properties;
    using lg2de.SimpleAccounting.Reports;

    internal class ShellViewModel : Conductor<IScreen>, IBusy, IDisposable
    {
        private readonly IApplicationUpdate applicationUpdate;
        private readonly IFileSystem fileSystem;
        private readonly IMessageBox messageBox;
        private readonly IProcess processApi;
        private readonly IReportFactory reportFactory;
        private readonly string version;
        private readonly IWindowManager windowManager;

        private Task autoSaveTask = Task.CompletedTask;
        private CancellationTokenSource? cancellationTokenSource;
        private bool isBusy;
        private AccountViewModel? selectedAccount;
        private bool showInactiveAccounts;

        public ShellViewModel(
            IWindowManager windowManager,
            IReportFactory reportFactory,
            IApplicationUpdate applicationUpdate,
            IMessageBox messageBox,
            IFileSystem fileSystem,
            IProcess processApi)
        {
            this.windowManager = windowManager;
            this.reportFactory = reportFactory;
            this.applicationUpdate = applicationUpdate;
            this.messageBox = messageBox;
            this.fileSystem = fileSystem;
            this.processApi = processApi;

            this.version = this.GetType().Assembly
                               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                           ?? "UNKNOWN";

            this.FullJournal = new FullJournalViewModel(this.ProjectData);
            this.AccountJournal = new AccountJournalViewModel(this.ProjectData);
        }

        public ObservableCollection<MenuViewModel> RecentProjects { get; }
            = new ObservableCollection<MenuViewModel>();

        public ObservableCollection<MenuViewModel> BookingYears { get; }
            = new ObservableCollection<MenuViewModel>();

        // move to ProjectData
        public List<AccountViewModel> AllAccounts { get; } = new List<AccountViewModel>();

        // TODO introduce AccountsViewModel
        public ObservableCollection<AccountViewModel> AccountList { get; }
            = new ObservableCollection<AccountViewModel>();

        public FullJournalViewModel FullJournal { get; }

        public AccountJournalViewModel AccountJournal { get; }

        // TODO move to AccountsViewModel
        public AccountViewModel? SelectedAccount
        {
            get => this.selectedAccount;
            set
            {
                this.selectedAccount = value;
                this.NotifyOfPropertyChange();
            }
        }

        // TODO move to AccountsViewModel
        public bool ShowInactiveAccounts
        {
            get => this.showInactiveAccounts;
            set
            {
                this.showInactiveAccounts = value;
                this.RefreshAccountList();
            }
        }

        public ICommand NewProjectCommand => new RelayCommand(
            _ =>
            {
                if (!this.CheckSaveProject())
                {
                    return;
                }

                this.ProjectData.FileName = "<new>";
                this.LoadProjectData(AccountingData.GetTemplateProject());
            });

        public ICommand OpenProjectCommand => new RelayCommand(_ => this.OnOpenProject());

        public ICommand SaveProjectCommand => new RelayCommand(
            _ => this.SaveProject(), _ => this.ProjectData.IsModified);

        public ICommand SwitchCultureCommand => new RelayCommand(
            cultureName =>
            {
                this.Settings.Culture = cultureName.ToString();
                this.NotifyOfPropertyChange(nameof(this.IsGermanCulture));
                this.NotifyOfPropertyChange(nameof(this.IsEnglishCulture));
                this.NotifyOfPropertyChange(nameof(this.IsSystemCulture));
                this.messageBox.Show(
                    Resources.Information_CultureChangeRestartRequired,
                    Resources.Header_SettingsChanged,
                    icon: MessageBoxImage.Information);
            });

        public bool IsGermanCulture => this.Settings.Culture == "de";
        public bool IsEnglishCulture => this.Settings.Culture == "en";
        public bool IsSystemCulture => this.Settings.Culture == string.Empty;

        public ICommand CloseApplicationCommand => new RelayCommand(_ => this.TryClose());

        public ICommand AddBookingsCommand => new RelayCommand(
            _ => this.OnAddBookings(), _ => this.ProjectData.IsCurrentYearOpen);

        public ICommand EditBookingCommand => new RelayCommand(
            this.OnEditBooking, _ => this.ProjectData.IsCurrentYearOpen);

        public ICommand ImportBookingsCommand => new RelayCommand(
            _ => this.OnImportBookings(), _ => this.ProjectData.IsCurrentYearOpen);

        public ICommand CloseYearCommand => new RelayCommand(
            _ => this.CloseYear(), _ => this.ProjectData.IsCurrentYearOpen);

        public ICommand TotalJournalReportCommand => new RelayCommand(
            _ => this.OnTotalJournalReport(), _ => this.FullJournal.Items.Any());

        public ICommand AccountJournalReportCommand => new RelayCommand(
            _ => this.OnAccountJournalReport(), _ => this.FullJournal.Items.Any());

        public ICommand TotalsAndBalancesReportCommand => new RelayCommand(
            _ => this.OnTotalsAndBalancesReport(), _ => this.FullJournal.Items.Any());

        public ICommand AssetBalancesReportCommand => new RelayCommand(
            _ => this.OnAssetBalancesReport(), _ => this.FullJournal.Items.Any());

        public ICommand AnnualBalanceReportCommand => new RelayCommand(
            _ => this.OnAnnualBalanceReport(), _ => this.FullJournal.Items.Any());

        public ICommand HelpAboutCommand => new RelayCommand(
            _ => this.processApi.Start(new ProcessStartInfo(Defines.ProjectUrl) { UseShellExecute = true }));

        public ICommand HelpFeedbackCommand => new RelayCommand(
            _ => this.processApi.Start(new ProcessStartInfo(Defines.NewIssueUrl) { UseShellExecute = true }));

        public IAsyncCommand HelpCheckForUpdateCommand => new AsyncCommand(this, this.OnCheckForUpdateAsync);

        public ICommand AccountSelectionCommand => new RelayCommand(
            o =>
            {
                if (o is AccountViewModel account)
                {
                    this.SelectedAccount = account;
                    this.RefreshAccountJournal();
                }
            });

        // TODO move to AccountsViewModel
        public ICommand NewAccountCommand => new RelayCommand(_ => this.OnNewAccount());

        public ICommand EditAccountCommand => new RelayCommand(this.OnEditAccount);

        internal Settings Settings { get; set; } = Settings.Default;

        internal ProjectData ProjectData { get; } = new ProjectData();

        internal Task LoadingTask { get; private set; } = Task.CompletedTask;

        internal TimeSpan AutoSaveInterval { get; set; } = TimeSpan.FromMinutes(1);

        internal string AutoSaveFileName => Defines.GetAutoSaveFileName(this.ProjectData.FileName);

        public bool IsBusy
        {
            get => this.isBusy;
            set
            {
                if (value == this.isBusy)
                {
                    return;
                }

                this.isBusy = value;
                this.NotifyOfPropertyChange();
            }
        }

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

            if (!this.CheckSaveProject())
            {
                callback(false);
                return;
            }

            if (this.fileSystem.FileExists(this.AutoSaveFileName))
            {
                // remove auto backup
                this.fileSystem.FileDelete(this.AutoSaveFileName);
            }

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
                                this.IsBusy = true;
                                await this.LoadProjectFromFileAsync(this.Settings.RecentProject);
                                this.BuildRecentProjectsMenu();
                                this.IsBusy = false;
                            });
                        this.autoSaveTask = this.AutoSaveAsync();
                    });
            }
            else
            {
                this.BuildRecentProjectsMenu();
                this.autoSaveTask = Task.Run(this.AutoSaveAsync);
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

        internal void RebuildJournals(ulong bookingId, Func<ulong, bool> accountJournalAffected)
        {
            this.FullJournal.Rebuild();
            this.FullJournal.Select(bookingId);

            if (!accountJournalAffected(this.SelectedAccount!.Identifier))
            {
                return;
            }

            this.RefreshAccountJournal();
            this.AccountJournal.Select(bookingId);
        }

        internal void InvokeLoadProjectFile(string fileName)
        {
            this.IsBusy = true;
            Task.Run(
                async () =>
                {
                    await this.LoadProjectFromFileAsync(fileName);
                    await Execute.OnUIThreadAsync(() => this.IsBusy = false);
                });
        }

        // TODO move to ProjectData
        internal async Task<OperationResult> LoadProjectFromFileAsync(string projectFileName)
        {
            if (!this.CheckSaveProject())
            {
                return OperationResult.Aborted;
            }

            this.ProjectData.IsModified = false;

            var loader = new ProjectFileLoader(this.messageBox, this.fileSystem, this.processApi, this.Settings);
            var loadResult = await Task.Run(() => loader.LoadAsync(projectFileName));
            if (loadResult != OperationResult.Completed)
            {
                return loadResult;
            }

            this.ProjectData.FileName = projectFileName;
            this.LoadProjectData(loader.ProjectData);
            this.ProjectData.IsModified = loader.Migrated;

            return OperationResult.Completed;
        }

        // TODO move to ProjectData
        internal void LoadProjectData(AccountingData newData)
        {
            this.ProjectData.All = newData;
            this.UpdateBookingYears();

            this.AllAccounts.Clear();
            foreach (var accountGroup in this.ProjectData.All.Accounts ?? new List<AccountingDataAccountGroup>())
            {
                foreach (var account in accountGroup.Account)
                {
                    var accountModel = new AccountViewModel
                    {
                        Identifier = account.ID,
                        Name = account.Name,
                        Group = accountGroup,
                        Groups = this.ProjectData.All.Accounts!,
                        Type = account.Type,
                        IsActivated = account.Active
                    };
                    this.AllAccounts.Add(accountModel);
                }
            }

            this.RefreshAccountList();

            // select last booking year after loading
            this.BookingYears.LastOrDefault()?.Command.Execute(null);
        }

        // TODO move to ProjectData
        internal bool CheckSaveProject()
        {
            if (!this.ProjectData.IsModified)
            {
                // no need to save the project
                return true;
            }

            var result = this.messageBox.Show(
                Resources.Question_SaveBeforeProceed,
                Resources.Header_Shutdown,
                MessageBoxButton.YesNoCancel);
            if (result == MessageBoxResult.Cancel)
            {
                return false;
            }

            if (result == MessageBoxResult.Yes)
            {
                this.SaveProject();
                return true;
            }

            // TODO Not saving but continue cannot work correctly this way!?
            return result == MessageBoxResult.No;
        }

        // TODO move to ProjectData
        internal void SaveProject()
        {
            if (this.ProjectData.FileName == "<new>")
            {
                using var saveFileDialog = new SaveFileDialog
                {
                    Filter = Resources.FileFilter_MainProject, RestoreDirectory = true
                };

                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                this.ProjectData.FileName = saveFileDialog.FileName;
            }

            DateTime fileDate = this.fileSystem.GetLastWriteTime(this.ProjectData.FileName);
            string backupFileName = $"{this.ProjectData.FileName}.{fileDate:yyyyMMddHHmmss}";
            if (this.fileSystem.FileExists(this.ProjectData.FileName))
            {
                this.fileSystem.FileMove(this.ProjectData.FileName, backupFileName);
            }

            this.fileSystem.WriteAllTextIntoFile(this.ProjectData.FileName, this.ProjectData.All!.Serialize());
            this.ProjectData.IsModified = false;

            if (this.fileSystem.FileExists(this.AutoSaveFileName))
            {
                this.fileSystem.FileDelete(this.AutoSaveFileName);
            }
        }

        private async Task OnCheckForUpdateAsync()
        {
            if (!await this.applicationUpdate.IsUpdateAvailableAsync(this.version))
            {
                return;
            }

            if (!this.CheckSaveProject())
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

        private void OnOpenProject()
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = Resources.FileFilter_MainProject, RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            this.InvokeLoadProjectFile(openFileDialog.FileName);
        }

        private void BuildRecentProjectsMenu()
        {
            // ReSharper disable once ConstantNullCoalescingCondition - FP
            foreach (var project in this.Settings.RecentProjects ?? new StringCollection())
            {
                var command = new AsyncCommand(this, () => this.OnLoadRecentProjectAsync(project));
                this.RecentProjects.Add(new MenuViewModel(project, command));
            }
        }

        private async Task OnLoadRecentProjectAsync(string project)
        {
            var loadResult = await this.LoadProjectFromFileAsync(project);
            if (loadResult != OperationResult.Failed)
            {
                return;
            }

            // failed to load, remove from menu
            // keep in menu if aborted (e.g. SecureDrive not available)
            var item = this.RecentProjects.FirstOrDefault(x => x.Header == project);
            this.RecentProjects.Remove(item);
            this.Settings.RecentProjects.Remove(project);
        }

        private async Task AutoSaveAsync()
        {
            try
            {
                while (true)
                {
                    await Task.Delay(this.AutoSaveInterval, this.cancellationTokenSource!.Token);
                    if (!this.ProjectData.IsModified)
                    {
                        continue;
                    }

                    this.fileSystem.WriteAllTextIntoFile(this.AutoSaveFileName, this.ProjectData.All!.Serialize());
                }
            }
            catch (OperationCanceledException)
            {
                // expected behavior
            }
        }

        private void UpdateDisplayName()
        {
            this.DisplayName = string.IsNullOrEmpty(this.ProjectData.FileName) || this.ProjectData.CurrentYear == null
                ? $"SimpleAccounting {this.version}"
                : $"SimpleAccounting {this.version} - {this.ProjectData.FileName} - {this.ProjectData.CurrentYear.Year}";
        }

        // TODO move to ProjectData or AccountsViewModel
        private void OnNewAccount()
        {
            var accountVm = new AccountViewModel
            {
                DisplayName = Resources.Header_CreateAccount,
                Group = this.ProjectData.All!.Accounts.First(),
                Groups = this.ProjectData.All.Accounts,
                IsValidIdentifierFunc = id => this.AllAccounts.All(a => a.Identifier != id)
            };
            var result = this.windowManager.ShowDialog(accountVm);
            if (result != true)
            {
                return;
            }

            // update database
            var newAccount = new AccountDefinition
            {
                ID = accountVm.Identifier,
                Name = accountVm.Name,
                Type = accountVm.Type,
                Active = accountVm.IsActivated
            };
            accountVm.Group.Account.Add(newAccount);
            accountVm.Group.Account = accountVm.Group.Account.OrderBy(x => x.ID).ToList();

            // update view
            this.AllAccounts.Add(accountVm);
            this.RefreshAccountList();

            this.ProjectData.IsModified = true;
        }

        // TODO move to ProjectData or AccountsViewModel
        private void OnEditAccount(object commandParameter)
        {
            if (!(commandParameter is AccountViewModel account))
            {
                return;
            }

            var vm = account.Clone();
            vm.DisplayName = Resources.Header_EditAccount;
            var invalidIds = this.AllAccounts.Select(x => x.Identifier).Where(x => x != account.Identifier)
                .ToList();
            vm.IsValidIdentifierFunc = id => !invalidIds.Contains(id);
            var result = this.windowManager.ShowDialog(vm);
            if (result != true)
            {
                return;
            }

            // update database
            var accountData = this.ProjectData.All!.AllAccounts.Single(x => x.ID == account.Identifier);
            accountData.Name = vm.Name;
            accountData.Type = vm.Type;
            accountData.Active = vm.IsActivated;
            if (account.Identifier != vm.Identifier)
            {
                accountData.ID = vm.Identifier;
                account.Group!.Account = account.Group.Account.OrderBy(x => x.ID).ToList();

                this.ProjectData.All.Journal.ForEach(
                    j => j.Booking?.ForEach(
                        b =>
                        {
                            b.Credit.ForEach(c => UpdateAccount(c, account.Identifier, vm.Identifier));
                            b.Debit.ForEach(d => UpdateAccount(d, account.Identifier, vm.Identifier));
                        }));
            }

            // update view
            account.Name = vm.Name;
            account.Group = vm.Group;
            account.Type = vm.Type;
            account.Identifier = vm.Identifier;
            account.IsActivated = vm.IsActivated;
            this.RefreshAccountList();
            account.Refresh();
            this.FullJournal.Rebuild();
            this.RefreshAccountJournal();

            this.ProjectData.IsModified = true;

            static void UpdateAccount(BookingValue entry, ulong oldIdentifier, ulong newIdentifier)
            {
                if (entry.Account == oldIdentifier)
                {
                    entry.Account = newIdentifier;
                }
            }
        }

        // TODO move to ProjectData
        private void OnAddBookings()
        {
            var yearStart = this.ProjectData.CurrentYear!.DateStart.ToDateTime();
            var yearEnd = this.ProjectData.CurrentYear.DateEnd.ToDateTime();
            var bookingModel = new EditBookingViewModel(
                this,
                DateTime.Today,
                yearStart,
                yearEnd,
                editMode: false) { BookingIdentifier = this.ProjectData.MaxBookIdent + 1 };
            var allAccounts = this.ProjectData.All!.AllAccounts;
            bookingModel.Accounts.AddRange(this.ShowInactiveAccounts ? allAccounts : allAccounts.Where(x => x.Active));

            // ReSharper disable ConstantConditionalAccessQualifier
            this.ProjectData.All.Setup?.BookingTemplates?.Template
                .Select(
                    t => new BookingTemplate
                    {
                        Text = t.Text, Credit = t.Credit, Debit = t.Debit, Value = t.Value.ToViewModel()
                    })
                .ToList().ForEach(bookingModel.BindingTemplates.Add);
            // ReSharper restore ConstantConditionalAccessQualifier
            this.windowManager.ShowDialog(bookingModel);
        }

        // TODO move to ProjectData
        private void OnEditBooking(object commandParameter)
        {
            if (!(commandParameter is JournalItemBaseViewModel journalViewModel))
            {
                return;
            }

            var journalIndex =
                this.ProjectData.CurrentYear!.Booking.FindIndex(x => x.ID == journalViewModel.Identifier);
            if (journalIndex < 0)
            {
                // summary item selected => ignore
                return;
            }

            var journalEntry = this.ProjectData.CurrentYear!.Booking[journalIndex];

            var bookingModel = new EditBookingViewModel(
                this,
                journalEntry.Date.ToDateTime(),
                this.ProjectData.CurrentYear!.DateStart.ToDateTime(),
                this.ProjectData.CurrentYear.DateEnd.ToDateTime(),
                editMode: true)
            {
                BookingIdentifier = journalEntry.ID,
                IsFollowup = journalEntry.Followup,
                IsOpening = journalEntry.Opening
            };

            if (journalEntry.Credit.Count > 1)
            {
                journalEntry.Credit.Select(x => x.ToSplitModel()).ToList().ForEach(bookingModel.CreditSplitEntries.Add);
                var theDebit = journalEntry.Debit.First();
                bookingModel.DebitAccount = theDebit.Account;
                bookingModel.BookingText = theDebit.Text;
                bookingModel.BookingValue = theDebit.Value.ToViewModel();
            }
            else if (journalEntry.Debit.Count > 1)
            {
                journalEntry.Debit.Select(x => x.ToSplitModel()).ToList().ForEach(bookingModel.DebitSplitEntries.Add);
                var theCredit = journalEntry.Credit.First();
                bookingModel.CreditAccount = theCredit.Account;
                bookingModel.BookingText = theCredit.Text;
                bookingModel.BookingValue = theCredit.Value.ToViewModel();
            }
            else
            {
                var theDebit = journalEntry.Debit.First();
                bookingModel.DebitAccount = theDebit.Account;
                bookingModel.BookingValue = theDebit.Value.ToViewModel();
                bookingModel.CreditAccount = journalEntry.Credit.First().Account;
                bookingModel.BookingText = journalViewModel.Text;
            }

            var allAccounts = this.ProjectData.All!.AllAccounts;
            bookingModel.Accounts.AddRange(this.ShowInactiveAccounts ? allAccounts : allAccounts.Where(x => x.Active));

            var result = this.windowManager.ShowDialog(bookingModel);
            if (result != true)
            {
                return;
            }

            // replace entry
            journalEntry = bookingModel.CreateJournalEntry();
            this.ProjectData.CurrentYear!.Booking[journalIndex] = journalEntry;

            this.ProjectData.IsModified = true;
            this.RebuildJournals(journalEntry.ID, journalEntry.ContainsAccount);
        }

        // TODO move to ProjectData
        private void OnImportBookings()
        {
            var importModel = new ImportBookingsViewModel(
                this.messageBox,
                this,
                this.ProjectData.CurrentYear!,
                this.ProjectData.All!.AllAccounts,
                this.ProjectData.MaxBookIdent + 1);
            this.windowManager.ShowDialog(importModel);
        }

        private void CloseYear()
        {
            var viewModel = new CloseYearViewModel(this.ProjectData.CurrentYear!);
            this.ProjectData.All!.AllAccounts.Where(x => x.Active && x.Type == AccountDefinitionType.Carryforward)
                .ToList().ForEach(viewModel.Accounts.Add);

            var result = this.windowManager.ShowDialog(viewModel);
            if (result != true || viewModel.RemoteAccount == null)
            {
                return;
            }

            var newYearJournal = this.ProjectData.All.CloseYear(this.ProjectData.CurrentYear!, viewModel.RemoteAccount);

            this.ProjectData.IsModified = true;
            this.SelectBookingYear(newYearJournal.Year);

            this.UpdateBookingYears();
        }

        private void SelectBookingYear(string newYearName)
        {
            this.ProjectData.CurrentYear = this.ProjectData.All!.Journal.Single(y => y.Year == newYearName);
            this.UpdateDisplayName();
            this.FullJournal.Rebuild();
            var firstBooking = this.ProjectData.CurrentYear.Booking?.FirstOrDefault();
            if (!this.AccountList.Any())
            {
                this.AccountJournal.Items.Clear();
                return;
            }

            if (firstBooking != null)
            {
                var firstAccount = firstBooking
                    .Credit.Select(x => x.Account)
                    .Concat(firstBooking.Debit.Select(x => x.Account))
                    .Min();
                this.SelectedAccount = this.AccountList.Single(x => x.Identifier == firstAccount);
                this.RefreshAccountJournal();
            }
            else
            {
                this.SelectedAccount = this.AccountList.First();
                this.RefreshAccountJournal();
            }
        }

        private void UpdateBookingYears()
        {
            this.BookingYears.Clear();
            if (this.ProjectData.All?.Journal == null)
            {
                return;
            }

            foreach (var year in this.ProjectData.All.Journal)
            {
                var menu = new MenuViewModel(
                    year.Year.ToString(CultureInfo.InvariantCulture),
                    new AsyncCommand(this, () => this.SelectBookingYear(year.Year)));
                this.BookingYears.Add(menu);
            }
        }

        private void RefreshAccountList()
        {
            IEnumerable<AccountViewModel> accounts = this.AllAccounts;
            if (!this.ShowInactiveAccounts)
            {
                accounts = accounts.Where(x => x.IsActivated);
            }

            var sorted = accounts.OrderBy(x => x.Identifier).ToList();

            this.AccountList.Clear();
            sorted.ForEach(this.AccountList.Add);
        }

        private void RefreshAccountJournal()
        {
            if (this.SelectedAccount == null)
            {
                return;
            }

            this.AccountJournal.Rebuild(this.SelectedAccount.Identifier);
        }

        // TODO refactor reports to use ReportData
        private void OnTotalJournalReport()
        {
            var report = this.reportFactory.CreateTotalJournal(
                this.ProjectData.CurrentYear!,
                this.ProjectData.All!.Setup);
            report.CreateReport(Resources.Header_Journal);
            report.ShowPreview(Resources.Header_Journal);
        }

        private void OnAccountJournalReport()
        {
            var report = this.reportFactory.CreateAccountJournal(
                this.ProjectData.CurrentYear!,
                this.ProjectData.All!.Accounts.SelectMany(a => a.Account),
                this.ProjectData.All.Setup);
            report.PageBreakBetweenAccounts =
                this.ProjectData.All.Setup?.Reports?.AccountJournalReport?.PageBreakBetweenAccounts ?? false;
            report.CreateReport(Resources.Header_AccountSheets);
            report.ShowPreview(Resources.Header_AccountSheets);
        }

        private void OnTotalsAndBalancesReport()
        {
            var report = this.reportFactory.CreateTotalsAndBalances(
                this.ProjectData.CurrentYear!,
                this.ProjectData.All!.Accounts,
                this.ProjectData.All.Setup);
            report.CreateReport(Resources.Header_TotalsAndBalances);
            report.ShowPreview(Resources.Header_TotalsAndBalances);
        }

        private void OnAssetBalancesReport()
        {
            var accountGroups = new List<AccountingDataAccountGroup>();
            foreach (var group in this.ProjectData.All!.Accounts)
            {
                var assertAccounts = group.Account
                    .Where(a => a.Type == AccountDefinitionType.Asset).ToList();
                if (assertAccounts.Count <= 0)
                {
                    // ignore group
                    continue;
                }

                accountGroups.Add(new AccountingDataAccountGroup { Name = group.Name, Account = assertAccounts });
            }

            var report = this.reportFactory.CreateTotalsAndBalances(
                this.ProjectData.CurrentYear!,
                accountGroups,
                this.ProjectData.All.Setup);
            // ReSharper disable ConstantConditionalAccessQualifier
            this.ProjectData.All.Setup?.Reports?.TotalsAndBalancesReport?.ForEach(report.Signatures.Add);
            // ReSharper restore ConstantConditionalAccessQualifier
            report.CreateReport(Resources.Header_AssetBalances);
            report.ShowPreview(Resources.Header_AssetBalances);
        }

        private void OnAnnualBalanceReport()
        {
            var report = this.reportFactory.CreateAnnualBalance(
                this.ProjectData.CurrentYear!,
                this.ProjectData.All!.AllAccounts,
                this.ProjectData.All.Setup);
            string title = Resources.Header_AnnualBalance;
            report.CreateReport(title);
            report.ShowPreview(title);
        }
    }
}
