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

    [SuppressMessage("ReSharper", "LocalizableElement")]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    internal class ShellViewModel : Conductor<IScreen>, IBusy, IDisposable
    {
        private readonly IApplicationUpdate applicationUpdate;
        private readonly IFileSystem fileSystem;
        private readonly IMessageBox messageBox;
        private readonly IProcess processApi;
        private readonly IReportFactory reportFactory;
        private readonly string version;
        private readonly IWindowManager windowManager;

        private AccountingData? accountingData;

        private Task autoSaveTask = Task.CompletedTask;
        private CancellationTokenSource? cancellationTokenSource;
        private AccountingDataJournal? currentModelJournal;
        private bool isBusy;
        private AccountViewModel? selectedAccount;
        private AccountJournalViewModel? selectedAccountJournalEntry;
        private FullJournalViewModel? selectedFullJournalEntry;
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
        }

        public ObservableCollection<MenuViewModel> RecentProjects { get; }
            = new ObservableCollection<MenuViewModel>();

        public ObservableCollection<MenuViewModel> BookingYears { get; }
            = new ObservableCollection<MenuViewModel>();

        public List<AccountViewModel> AllAccounts { get; } = new List<AccountViewModel>();

        public ObservableCollection<AccountViewModel> AccountList { get; }
            = new ObservableCollection<AccountViewModel>();

        public AccountViewModel? SelectedAccount
        {
            get => this.selectedAccount;
            set
            {
                this.selectedAccount = value;
                this.NotifyOfPropertyChange();
            }
        }

        public bool ShowInactiveAccounts
        {
            get => this.showInactiveAccounts;
            set
            {
                this.showInactiveAccounts = value;
                this.RefreshAccountList();
            }
        }

        public ObservableCollection<FullJournalViewModel> FullJournal { get; }
            = new ObservableCollection<FullJournalViewModel>();

        public FullJournalViewModel? SelectedFullJournalEntry
        {
            get => this.selectedFullJournalEntry;
            set
            {
                this.selectedFullJournalEntry = value;
                this.NotifyOfPropertyChange();
            }
        }

        public ObservableCollection<AccountJournalViewModel> AccountJournal { get; }
            = new ObservableCollection<AccountJournalViewModel>();

        public AccountJournalViewModel? SelectedAccountJournalEntry
        {
            get => this.selectedAccountJournalEntry;
            set
            {
                this.selectedAccountJournalEntry = value;
                this.NotifyOfPropertyChange();
            }
        }

        public ICommand NewProjectCommand => new RelayCommand(
            _ =>
            {
                if (!this.CheckSaveProject())
                {
                    return;
                }

                this.FileName = "<new>";
                this.LoadProjectData(AccountingData.GetTemplateProject());
            });

        public ICommand OpenProjectCommand => new RelayCommand(_ => this.OnOpenProject());

        public ICommand SaveProjectCommand => new RelayCommand(_ => this.SaveProject(), _ => this.IsDocumentModified);

        public ICommand SwitchCultureCommand => new RelayCommand(
            cultureName =>
            {
                this.Settings.Culture = cultureName.ToString();
                this.messageBox.Show(
                    Resources.Information_CultureChangeRestartRequired,
                    Resources.Header_SettingsChanged,
                    icon: MessageBoxImage.Information);
            });

        public ICommand CloseApplicationCommand => new RelayCommand(_ => this.TryClose());

        public ICommand AddBookingsCommand => new RelayCommand(_ => this.OnAddBookings(), _ => this.IsCurrentYearOpen);

        public ICommand EditBookingCommand => new RelayCommand(this.OnEditBooking, _ => this.IsCurrentYearOpen);

        public ICommand ImportBookingsCommand => new RelayCommand(
            _ => this.OnImportBookings(), _ => this.IsCurrentYearOpen);

        public ICommand CloseYearCommand => new RelayCommand(
            _ => this.CloseYear(), _ => this.IsCurrentYearOpen);

        public ICommand TotalJournalReportCommand => new RelayCommand(
            _ => this.OnTotalJournalReport(), _ => this.FullJournal.Any());

        public ICommand AccountJournalReportCommand => new RelayCommand(
            _ => this.OnAccountJournalReport(), _ => this.FullJournal.Any());

        public ICommand TotalsAndBalancesReportCommand => new RelayCommand(
            _ => this.OnTotalsAndBalancesReport(), _ => this.FullJournal.Any());

        public ICommand AssetBalancesReportCommand => new RelayCommand(
            _ => this.OnAssetBalancesReport(), _ => this.FullJournal.Any());

        public ICommand AnnualBalanceReportCommand => new RelayCommand(
            _ => this.OnAnnualBalanceReport(), _ => this.FullJournal.Any());

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

        public ICommand NewAccountCommand => new RelayCommand(_ => this.OnNewAccount());

        public ICommand EditAccountCommand => new RelayCommand(this.OnEditAccount);

        internal Settings Settings { get; set; } = Settings.Default;

        internal string FileName { get; set; } = string.Empty;

        internal bool IsDocumentModified { get; set; }

        internal Task LoadingTask { get; private set; } = Task.CompletedTask;

        internal TimeSpan AutoSaveInterval { get; set; } = TimeSpan.FromMinutes(1);

        private string AutoSaveFileName => Defines.GetAutoSaveFileName(this.FileName);

        private bool IsCurrentYearOpen
        {
            get
            {
                if (this.currentModelJournal == null)
                {
                    return false;
                }

                return !this.currentModelJournal.Closed;
            }
        }

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

        internal void AddBooking(AccountingDataJournalBooking booking, bool refreshJournal = true)
        {
            this.currentModelJournal!.Booking.Add(booking);
            this.IsDocumentModified = true;

            if (refreshJournal)
            {
                this.RefreshFullJournal();
            }

            this.SelectedFullJournalEntry = this.FullJournal.FirstOrDefault(x => x.Identifier == booking.ID);

            if (booking.Debit.All(x => x.Account != this.SelectedAccount?.Identifier)
                && booking.Credit.All(x => x.Account != this.SelectedAccount?.Identifier))
            {
                return;
            }

            this.RefreshAccountJournal();
            this.SelectedAccountJournalEntry = this.AccountJournal.FirstOrDefault(x => x.Identifier == booking.ID);
        }

        internal async Task<OperationResult> LoadProjectFromFileAsync(string projectFileName)
        {
            if (!this.CheckSaveProject())
            {
                return OperationResult.Aborted;
            }

            this.IsDocumentModified = false;

            var loader = new ProjectFileLoader(this.messageBox, this.fileSystem, this.processApi, this.Settings);
            var loadResult = await Task.Run(() => loader.LoadAsync(projectFileName));
            if (loadResult != OperationResult.Completed)
            {
                return loadResult;
            }

            this.LoadProjectData(loader.ProjectData);
            this.FileName = projectFileName;
            this.IsDocumentModified = loader.Migrated;

            return OperationResult.Completed;
        }

        internal void LoadProjectData(AccountingData projectData)
        {
            this.accountingData = projectData;
            this.UpdateBookingYears();

            this.AllAccounts.Clear();
            foreach (var accountGroup in this.accountingData.Accounts ?? new List<AccountingDataAccountGroup>())
            {
                foreach (var account in accountGroup.Account)
                {
                    var accountModel = new AccountViewModel
                    {
                        Identifier = account.ID,
                        Name = account.Name,
                        Group = accountGroup,
                        Groups = this.accountingData.Accounts!,
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

        internal bool CheckSaveProject()
        {
            if (!this.IsDocumentModified)
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

            return result == MessageBoxResult.No;
        }

        internal void SaveProject()
        {
            if (this.FileName == "<new>")
            {
                using var saveFileDialog = new SaveFileDialog
                {
                    Filter = Resources.FileDialogFilter, RestoreDirectory = true
                };

                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                this.FileName = saveFileDialog.FileName;
            }

            DateTime fileDate = this.fileSystem.GetLastWriteTime(this.FileName);
            string backupFileName = $"{this.FileName}.{fileDate:yyyyMMddHHmmss}";
            if (this.fileSystem.FileExists(this.FileName))
            {
                this.fileSystem.FileMove(this.FileName, backupFileName);
            }

            this.fileSystem.WriteAllTextIntoFile(this.FileName, this.accountingData!.Serialize());
            this.IsDocumentModified = false;

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
            this.IsDocumentModified = false;

            this.TryClose();
        }

        private void OnOpenProject()
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = Resources.FileDialogFilter, RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            this.IsBusy = true;
            string fileName = openFileDialog.FileName;
            Task.Run(
                async () =>
                {
                    await this.LoadProjectFromFileAsync(fileName);
                    await Execute.OnUIThreadAsync(() => this.IsBusy = false);
                });
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
                    if (!this.IsDocumentModified)
                    {
                        continue;
                    }

                    this.fileSystem.WriteAllTextIntoFile(this.AutoSaveFileName, this.accountingData!.Serialize());
                }
            }
            catch (OperationCanceledException)
            {
                // expected behavior
            }
        }

        private void UpdateDisplayName()
        {
            this.DisplayName = string.IsNullOrEmpty(this.FileName) || this.currentModelJournal == null
                ? $"SimpleAccounting {this.version}"
                : $"SimpleAccounting {this.version} - {this.FileName} - {this.currentModelJournal.Year}";
        }

        private void OnNewAccount()
        {
            var accountVm = new AccountViewModel
            {
                DisplayName = Resources.Header_CreateAccount,
                Group = this.accountingData!.Accounts.First(),
                Groups = this.accountingData.Accounts,
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

            this.IsDocumentModified = true;
        }

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
            var accountData = this.accountingData!.AllAccounts.Single(x => x.ID == account.Identifier);
            accountData.Name = vm.Name;
            accountData.Type = vm.Type;
            accountData.Active = vm.IsActivated;
            if (account.Identifier != vm.Identifier)
            {
                accountData.ID = vm.Identifier;
                account.Group!.Account = account.Group.Account.OrderBy(x => x.ID).ToList();

                this.accountingData.Journal.ForEach(
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
            this.RefreshFullJournal();
            this.RefreshAccountJournal();

            this.IsDocumentModified = true;

            static void UpdateAccount(BookingValue entry, ulong oldIdentifier, ulong newIdentifier)
            {
                if (entry.Account == oldIdentifier)
                {
                    entry.Account = newIdentifier;
                }
            }
        }

        private void OnAddBookings()
        {
            var bookingModel = new EditBookingViewModel(
                this,
                this.currentModelJournal!.DateStart.ToDateTime(),
                this.currentModelJournal.DateEnd.ToDateTime(),
                editMode: false) { BookingIdentifier = this.GetMaxBookIdent() + 1 };
            var allAccounts = this.accountingData!.AllAccounts;
            bookingModel.Accounts.AddRange(this.ShowInactiveAccounts ? allAccounts : allAccounts.Where(x => x.Active));

            // ReSharper disable ConstantConditionalAccessQualifier
            this.accountingData.Setup?.BookingTemplates?.Template
                .Select(
                    t => new BookingTemplate
                    {
                        Text = t.Text, Credit = t.Credit, Debit = t.Debit, Value = t.Value.ToViewModel()
                    })
                .ToList().ForEach(bookingModel.BindingTemplates.Add);
            // ReSharper restore ConstantConditionalAccessQualifier
            this.windowManager.ShowDialog(bookingModel);
        }

        private void OnEditBooking(object commandParameter)
        {
            if (!(commandParameter is JournalBaseViewModel journalViewModel))
            {
                return;
            }

            var journalIndex = this.currentModelJournal!.Booking.FindIndex(x => x.ID == journalViewModel.Identifier);
            if (journalIndex < 0)
            {
                // summary item selected => ignore
                return;
            }

            var journalEntry = this.currentModelJournal!.Booking[journalIndex];

            var bookingModel = new EditBookingViewModel(
                this,
                this.currentModelJournal!.DateStart.ToDateTime(),
                this.currentModelJournal.DateEnd.ToDateTime(),
                editMode: true)
            {
                BookingIdentifier = journalEntry.ID,
                Date = journalEntry.Date.ToDateTime(),
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

            var allAccounts = this.accountingData!.AllAccounts;
            bookingModel.Accounts.AddRange(this.ShowInactiveAccounts ? allAccounts : allAccounts.Where(x => x.Active));

            var result = this.windowManager.ShowDialog(bookingModel);
            if (result != true)
            {
                return;
            }

            // replace entry
            journalEntry = bookingModel.CreateJournalEntry();
            this.currentModelJournal!.Booking[journalIndex] = journalEntry;

            this.IsDocumentModified = true;
            this.RefreshFullJournal();
            if (journalEntry.Credit.Concat(journalEntry.Debit).Any(x => x.Account == this.SelectedAccount?.Identifier))
            {
                this.RefreshAccountJournal();
            }
        }

        private void OnImportBookings()
        {
            var importModel = new ImportBookingsViewModel(
                this.messageBox,
                this,
                this.currentModelJournal!,
                this.accountingData!.AllAccounts,
                this.GetMaxBookIdent() + 1);
            this.windowManager.ShowDialog(importModel);
        }

        private ulong GetMaxBookIdent()
        {
            if (this.currentModelJournal?.Booking == null || !this.currentModelJournal.Booking.Any())
            {
                return 0;
            }

            return this.currentModelJournal.Booking.Max(b => b.ID);
        }

        private void CloseYear()
        {
            var viewModel = new CloseYearViewModel(this.currentModelJournal!);
            this.accountingData!.AllAccounts.Where(x => x.Active && x.Type == AccountDefinitionType.Carryforward)
                .ToList().ForEach(viewModel.Accounts.Add);

            var result = this.windowManager.ShowDialog(viewModel);
            if (result != true || viewModel.RemoteAccount == null)
            {
                return;
            }

            var newYearJournal = this.accountingData.CloseYear(this.currentModelJournal!, viewModel.RemoteAccount);

            this.IsDocumentModified = true;
            this.SelectBookingYear(newYearJournal.Year);

            this.UpdateBookingYears();
        }

        private void SelectBookingYear(string newYearName)
        {
            this.currentModelJournal = this.accountingData!.Journal.Single(y => y.Year == newYearName);
            this.UpdateDisplayName();
            this.RefreshFullJournal();
            var firstBooking = this.currentModelJournal.Booking?.FirstOrDefault();
            if (!this.AccountList.Any())
            {
                this.AccountJournal.Clear();
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
            if (this.accountingData?.Journal == null)
            {
                return;
            }

            foreach (var year in this.accountingData.Journal)
            {
                var menu = new MenuViewModel(
                    year.Year.ToString(CultureInfo.InvariantCulture),
                    new AsyncCommand(this, () => this.SelectBookingYear(year.Year)));
                this.BookingYears.Add(menu);
            }
        }

        // TODO move to FullJournalViewModel
        private void RefreshFullJournal()
        {
            this.FullJournal.Clear();
            if (this.currentModelJournal?.Booking == null)
            {
                return;
            }

            foreach (var booking in this.currentModelJournal.Booking.OrderBy(b => b.Date))
            {
                var item = new FullJournalViewModel
                {
                    Date = booking.Date.ToDateTime(), Identifier = booking.ID, IsFollowup = booking.Followup
                };
                var debitAccounts = booking.Debit;
                var creditAccounts = booking.Credit;
                if (debitAccounts.Count == 1 && creditAccounts.Count == 1)
                {
                    var debit = debitAccounts[0];
                    item.Text = debit.Text;
                    item.Value = debit.Value.ToViewModel();
                    item.DebitAccount = this.accountingData!.GetAccountName(debit);
                    item.CreditAccount = this.accountingData.GetAccountName(creditAccounts[0]);
                    this.FullJournal.Add(item);
                    continue;
                }

                foreach (var debitEntry in debitAccounts)
                {
                    var debitItem = item.Clone();
                    debitItem.Text = debitEntry.Text;
                    debitItem.Value = debitEntry.Value.ToViewModel();
                    debitItem.DebitAccount = this.accountingData!.GetAccountName(debitEntry);
                    this.FullJournal.Add(debitItem);
                }

                foreach (var creditEntry in creditAccounts)
                {
                    var creditItem = item.Clone();
                    creditItem.Text = creditEntry.Text;
                    creditItem.Value = creditEntry.Value.ToViewModel();
                    creditItem.CreditAccount = this.accountingData!.GetAccountName(creditEntry);
                    this.FullJournal.Add(creditItem);
                }
            }

            this.FullJournal.UpdateRowHighlighting();
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

        // TODO move to AccountJournalViewModel
        private void RefreshAccountJournal()
        {
            this.AccountJournal.Clear();
            if (this.accountingData == null
                || this.currentModelJournal?.Booking == null
                || this.SelectedAccount == null)
            {
                return;
            }

            var accountNumber = this.SelectedAccount.Identifier;
            double creditSum = 0;
            double debitSum = 0;
            var relevantBookings = this.currentModelJournal
                .Booking.Where(b => b.Credit.Any(x => x.Account == accountNumber))
                .Concat(this.currentModelJournal.Booking.Where(b => b.Debit.Any(x => x.Account == accountNumber)))
                .OrderBy(x => x.Date).ThenBy(x => x.ID);
            foreach (var booking in relevantBookings)
            {
                var debitEntries = booking.Debit.Where(x => x.Account == accountNumber);
                foreach (var debitEntry in debitEntries)
                {
                    var item = new AccountJournalViewModel
                    {
                        Identifier = booking.ID,
                        Date = booking.Date.ToDateTime(),
                        Text = debitEntry.Text,
                        DebitValue = debitEntry.Value.ToViewModel()
                    };

                    debitSum += item.DebitValue;
                    item.RemoteAccount = booking.Credit.Count == 1
                        ? this.accountingData.GetAccountName(booking.Credit.Single())
                        : Resources.Word_Various;
                    this.AccountJournal.Add(item);
                }

                var creditEntries = booking.Credit.Where(x => x.Account == accountNumber);
                foreach (var creditEntry in creditEntries)
                {
                    var item = new AccountJournalViewModel
                    {
                        Identifier = booking.ID,
                        Date = booking.Date.ToDateTime(),
                        Text = creditEntry.Text,
                        CreditValue = creditEntry.Value.ToViewModel()
                    };

                    creditSum += item.CreditValue;
                    item.RemoteAccount = booking.Debit.Count == 1
                        ? this.accountingData.GetAccountName(booking.Debit.Single())
                        : Resources.Word_Various;
                    this.AccountJournal.Add(item);
                }
            }

            this.AccountJournal.UpdateRowHighlighting();

            if (debitSum + creditSum < double.Epsilon)
            {
                // no summary required
                return;
            }

            var sumItem = new AccountJournalViewModel();
            this.AccountJournal.Add(sumItem);
            sumItem.IsSummary = true;
            sumItem.Text = Resources.Word_Total;
            sumItem.DebitValue = debitSum;
            sumItem.CreditValue = creditSum;

            var saldoItem = new AccountJournalViewModel();
            this.AccountJournal.Add(saldoItem);
            saldoItem.IsSummary = true;
            saldoItem.Text = Resources.Word_Balance;
            if (debitSum > creditSum)
            {
                saldoItem.DebitValue = debitSum - creditSum;
            }
            else
            {
                saldoItem.CreditValue = creditSum - debitSum;
            }
        }

        private void OnTotalJournalReport()
        {
            var report = this.reportFactory.CreateTotalJournal(
                this.currentModelJournal!,
                this.accountingData!.Setup,
                CultureInfo.CurrentUICulture);
            report.CreateReport(Resources.Header_Journal);
            report.ShowPreview(Resources.Header_Journal);
        }

        private void OnAccountJournalReport()
        {
            var report = this.reportFactory.CreateAccountJournal(
                this.currentModelJournal!,
                this.accountingData!.Accounts.SelectMany(a => a.Account),
                this.accountingData.Setup, CultureInfo.CurrentUICulture);
            report.PageBreakBetweenAccounts =
                this.accountingData.Setup?.Reports?.AccountJournalReport?.PageBreakBetweenAccounts ?? false;
            report.CreateReport(Resources.Header_AccountSheets);
            report.ShowPreview(Resources.Header_AccountSheets);
        }

        private void OnTotalsAndBalancesReport()
        {
            var report = this.reportFactory.CreateTotalsAndBalances(
                this.currentModelJournal!,
                this.accountingData!.Accounts,
                this.accountingData.Setup,
                CultureInfo.CurrentUICulture);
            report.CreateReport(Resources.Header_TotalsAndBalances);
            report.ShowPreview(Resources.Header_TotalsAndBalances);
        }

        private void OnAssetBalancesReport()
        {
            var accountGroups = new List<AccountingDataAccountGroup>();
            foreach (var group in this.accountingData!.Accounts)
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
                this.currentModelJournal!,
                accountGroups,
                this.accountingData.Setup,
                CultureInfo.CurrentUICulture);
            // ReSharper disable ConstantConditionalAccessQualifier
            this.accountingData.Setup?.Reports?.TotalsAndBalancesReport?.ForEach(report.Signatures.Add);
            // ReSharper restore ConstantConditionalAccessQualifier
            report.CreateReport(Resources.Header_AssetBalances);
            report.ShowPreview(Resources.Header_AssetBalances);
        }

        private void OnAnnualBalanceReport()
        {
            var report = this.reportFactory.CreateAnnualBalance(
                this.currentModelJournal!,
                this.accountingData!.AllAccounts,
                this.accountingData.Setup,
                CultureInfo.CurrentUICulture);
            string title = Resources.Header_AnnualBalance;
            report.CreateReport(title);
            report.ShowPreview(title);
        }
    }
}
