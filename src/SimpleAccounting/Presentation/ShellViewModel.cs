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
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Input;
    using Caliburn.Micro;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Properties;
    using lg2de.SimpleAccounting.Reports;
    using Octokit;

#pragma warning disable S4055 // string literals => pending translation

    [SuppressMessage("Critical Code Smell", "S2365:Properties should not make collection or array copies", Justification = "<Pending>")]
    internal class ShellViewModel : Conductor<IScreen>
    {
        private const string GithubDomain = "github.com";
        private const string OrganizationName = "lg2de";
        private const string ProjectName = "SimpleAccounting";
        private const int MaxRecentProjects = 10;
        private const double CentFactor = 100.0;
        private static readonly string ProjectUrl = $"https://{GithubDomain}/{OrganizationName}/{ProjectName}";
        private static readonly string NewIssueUrl = $"{ProjectUrl}/issues/new?template=bug-report.md";

        private readonly IWindowManager windowManager;
        private readonly IReportFactory reportFactory;
        private readonly IMessageBox messageBox;
        private readonly IFileSystem fileSystem;
        private readonly string version;

        private AccountingData accountingData;
        private AccountingDataJournal currentModelJournal;
        private string fileName = "";
        private bool showInactiveAccounts;
        private FullJournalViewModel selectedFullJournalEntry;
        private AccountJournalViewModel selectedAccountJournalEntry;
        private AccountViewModel selectedAccount;

        public ShellViewModel(
            IWindowManager windowManager,
            IReportFactory reportFactory,
            IMessageBox messageBox,
            IFileSystem fileSystem)
        {
            this.windowManager = windowManager;
            this.reportFactory = reportFactory;
            this.messageBox = messageBox;
            this.fileSystem = fileSystem;

            this.version = this.GetType().Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        }

        public ObservableCollection<MenuViewModel> RecentProjects { get; }
            = new ObservableCollection<MenuViewModel>();

        public ObservableCollection<MenuViewModel> BookingYears { get; }
            = new ObservableCollection<MenuViewModel>();

        public List<AccountViewModel> AllAccounts { get; } = new List<AccountViewModel>();
        public ObservableCollection<AccountViewModel> AccountList { get; }
            = new ObservableCollection<AccountViewModel>();

        public AccountViewModel SelectedAccount
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

        public FullJournalViewModel SelectedFullJournalEntry
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

        public AccountJournalViewModel SelectedAccountJournalEntry
        {
            get => this.selectedAccountJournalEntry;
            set
            {
                this.selectedAccountJournalEntry = value;
                this.NotifyOfPropertyChange();
            }
        }

        public ICommand NewProjectCommand => new RelayCommand(_ =>
        {
            if (!this.CheckSaveProject())
            {
                return;
            }

            this.fileName = "<new>";
            this.LoadProjectData(GetTemplateProject());
        });

        public ICommand OpenProjectCommand => new RelayCommand(_ =>
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = "Accounting project files (*.acml)|*.acml",
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            this.LoadProjectFromFile(openFileDialog.FileName);
        });

        public ICommand SaveProjectCommand => new RelayCommand(
            _ => this.SaveProject(),
            _ => this.IsDocumentModified);

        public ICommand CloseApplicationCommand => new RelayCommand(_ => this.TryClose());

        public ICommand AddBookingsCommand => new RelayCommand(_ =>
        {
            var bookingModel = new AddBookingViewModel(
                this,
                this.currentModelJournal.DateStart.ToDateTime(),
                this.currentModelJournal.DateEnd.ToDateTime())
            {
                BookingNumber = this.GetMaxBookIdent() + 1
            };
            bookingModel.Accounts.AddRange(
                this.ShowInactiveAccounts
                ? this.accountingData.AllAccounts
                : this.accountingData.AllAccounts.Where(x => x.Active));

            this.accountingData.Setup?.BookingTemplates?.Template
                .Select(t => new BookingTemplate { Text = t.Text, Credit = t.Credit, Debit = t.Debit, Value = t.Value / CentFactor })
                .ToList().ForEach(bookingModel.BindingTemplates.Add);
            this.windowManager.ShowDialog(bookingModel);
        }, _ => this.IsCurrentYearOpen);

        public ICommand ImportBookingsCommand => new RelayCommand(_ =>
        {
            var min = this.currentModelJournal.DateStart.ToDateTime();
            var max = this.currentModelJournal.DateEnd.ToDateTime();

            var importModel = new ImportBookingsViewModel(
                this.messageBox,
                this,
                this.accountingData.AllAccounts)
            {
                BookingNumber = this.GetMaxBookIdent() + 1,
                RangeMin = min,
                RangMax = max,
                Journal = this.currentModelJournal
            };
            this.windowManager.ShowDialog(importModel);
        }, _ => this.IsCurrentYearOpen);

        public ICommand CloseYearCommand => new RelayCommand(
            _ => this.CloseYear(),
            _ => this.IsCurrentYearOpen);

        public ICommand TotalJournalReportCommand => new RelayCommand(
            _ =>
            {
                var report = new TotalJournalReport(
                    this.currentModelJournal,
                    this.accountingData.Setup,
                    CultureInfo.CurrentUICulture);
                report.CreateReport();
                report.ShowPreview("Journal");
            },
            _ => this.FullJournal.Any());

        public ICommand AccountJournalReportCommand => new RelayCommand(
            _ =>
            {
                var report = this.reportFactory.CreateAccountJournal(
                    this.accountingData.Accounts.SelectMany(a => a.Account),
                    this.currentModelJournal,
                    this.accountingData.Setup,
                    CultureInfo.CurrentUICulture);
                report.PageBreakBetweenAccounts = this.accountingData.Setup?.Reports?.AccountJournalReport?.PageBreakBetweenAccounts ?? false;
                report.CreateReport();
                report.ShowPreview("Kontoblätter");
            },
            _ => this.FullJournal.Any());

        public ICommand TotalsAndBalancesReportCommand => new RelayCommand(
            _ =>
            {
                var report = new TotalsAndBalancesReport(
                    this.currentModelJournal,
                    this.accountingData.Accounts,
                    this.accountingData.Setup,
                    CultureInfo.CurrentUICulture);
                report.CreateReport();
                report.ShowPreview("Summen und Salden");
            },
            _ => this.FullJournal.Any());

        public ICommand AssetBalancesReportCommand => new RelayCommand(
            _ =>
            {
                var accountGroups = new List<AccountingDataAccountGroup>();
                foreach (var group in this.accountingData.Accounts)
                {
                    var assertAccounts = group.Account
                        .Where(a => a.Type == AccountDefinitionType.Asset).ToList();
                    if (assertAccounts.Count <= 0)
                    {
                        // ignore group
                        continue;
                    }

                    accountGroups.Add(new AccountingDataAccountGroup
                    {
                        Name = group.Name,
                        Account = assertAccounts
                    });
                }

                var report = new TotalsAndBalancesReport(
                    this.currentModelJournal,
                    accountGroups,
                    this.accountingData.Setup,
                    CultureInfo.CurrentUICulture);
                this.accountingData.Setup.Reports?.TotalsAndBalancesReport?.ForEach(report.Signatures.Add);
                report.CreateReport();
                report.ShowPreview("Bestandskontosalden");
            },
            _ => this.FullJournal.Any());

        public ICommand AnnualBalanceReportCommand => new RelayCommand(
            _ =>
            {
                var report = new AnnualBalanceReport(
                    this.currentModelJournal,
                    this.accountingData.AllAccounts,
                    this.accountingData.Setup,
                    CultureInfo.CurrentUICulture);
                report.CreateReport();
                report.ShowPreview("Jahresbilanz");
            },
            _ => this.FullJournal.Any());

        public ICommand HelpAboutCommand => new RelayCommand(
            _ => Process.Start(new ProcessStartInfo(ProjectUrl) { UseShellExecute = true }));

        public ICommand HelpFeedbackCommand => new RelayCommand(
            _ => Process.Start(new ProcessStartInfo(NewIssueUrl) { UseShellExecute = true }));

        public ICommand HelpCheckForUpdateCommand => new RelayCommand(this.OnCheckForUpdate);

        public ICommand AccountSelectionCommand => new RelayCommand(o =>
        {
            if (o is AccountViewModel account)
            {
                this.SelectedAccount = account;
                this.RefreshAccountJournal();
            }
        });

        public ICommand NewAccountCommand => new RelayCommand(_ =>
        {
            var accountVm = new AccountViewModel
            {
                DisplayName = "Account erstellen",
                Group = this.accountingData.Accounts.FirstOrDefault(),
                Groups = this.accountingData.Accounts,
                IsAvalidIdentifierFunc = id => this.AllAccounts.All(a => a.Identifier != id)
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
        });

        public ICommand EditAccountCommand => new RelayCommand(o =>
        {
            if (!(o is AccountViewModel account))
            {
                return;
            }

            var vm = account.Clone();
            vm.DisplayName = "Account bearbeiten";
            var invalidIds = this.AllAccounts.Select(x => x.Identifier).Where(x => x != account.Identifier).ToList();
            vm.IsAvalidIdentifierFunc = id => !invalidIds.Contains(id);
            var result = this.windowManager.ShowDialog(vm);
            if (result != true)
            {
                return;
            }

            // update database
            var accountData = this.accountingData.AllAccounts.Single(x => x.ID == account.Identifier);
            accountData.Name = vm.Name;
            accountData.Type = vm.Type;
            accountData.Active = vm.IsActivated;
            if (account.Identifier != vm.Identifier)
            {
                accountData.ID = vm.Identifier;
                account.Group.Account = account.Group.Account.OrderBy(x => x.ID).ToList();

                this.accountingData.Journal.ForEach(j => j.Booking?.ForEach(b =>
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
        });

        internal Settings Settings { get; set; } = Settings.Default;

        internal bool IsDocumentModified { get; set; }

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

            if (this.fileSystem.FileExists(this.Settings.RecentProject))
            {
                this.LoadProjectFromFile(this.Settings.RecentProject);
            }

            foreach (var project in this.Settings.RecentProjects ?? new StringCollection())
            {
                if (!this.fileSystem.FileExists(project))
                {
                    continue;
                }

                var item = new MenuViewModel(
                    project,
                    new RelayCommand(_ => this.LoadProjectFromFile(project)));
                this.RecentProjects.Add(item);
            }
        }

        internal void AddBooking(AccountingDataJournalBooking booking, bool refreshJournal = true)
        {
            this.currentModelJournal.Booking.Add(booking);
            this.IsDocumentModified = true;

            if (refreshJournal)
            {
                this.RefreshFullJournal();
            }

            this.SelectedFullJournalEntry = this.FullJournal.FirstOrDefault(x => x.Identifier == booking.ID);

            if (!booking.Debit.Any(x => x.Account == this.SelectedAccount?.Identifier)
                && !booking.Credit.Any(x => x.Account == this.SelectedAccount?.Identifier))
            {
                return;
            }

            this.RefreshAccountJournal();
            this.SelectedAccountJournalEntry = this.AccountJournal.FirstOrDefault(x => x.Identifier == booking.ID);
        }

        [ExcludeFromCodeCoverage]
#pragma warning disable S3168 // "async" methods should not return "void"
        internal async void OnCheckForUpdate(object _)
#pragma warning restore S3168
        {
            const string Caption = "Update-Prüfung";
            IEnumerable<Release> releases;
            try
            {
                var client = new GitHubClient(new ProductHeaderValue(ProjectName));
                releases = await client.Repository.Release.GetAll(OrganizationName, ProjectName);
            }
            catch (Exception exception)
            {
                this.messageBox.Show(
                    $"Abfrage neuer Versionen fehlgeschlagen:\n{exception.Message}",
                    Caption,
                    icon: MessageBoxImage.Error);
                return;
            }

            var newRelease = this.GetNewRelease(this.version, releases);
            if (newRelease == null)
            {
                this.messageBox.Show("Sie verwenden die neueste Version.", Caption);
                return;
            }

            var result = this.messageBox.Show(
                $"Wollen Sie auf die neue Version {newRelease.TagName} aktualisieren?",
                Caption,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            if (!this.CheckSaveProject())
            {
                return;
            }

            var stream = this.GetType().Assembly.GetManifestResourceStream(
                "lg2de.SimpleAccounting.UpdateApplication.ps1");
            using var reader = new StreamReader(stream);
            var script = reader.ReadToEnd();
            string scriptPath = Path.Combine(Path.GetTempPath(), "UpdateApplication.ps1");
            File.WriteAllText(scriptPath, script);

            var asset = newRelease.Assets.FirstOrDefault(
                x => x.Name.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase));
            if (asset == null)
            {
                // asset not found :(
                return;
            }

            string assetUrl = asset.BrowserDownloadUrl;
            string targetFolder = Path.GetDirectoryName(this.GetType().Assembly.Location);
            int processId = Process.GetCurrentProcess().Id;
            var info = new ProcessStartInfo(
                "powershell",
                $"-File {scriptPath} -assetUrl {assetUrl} -targetFolder {targetFolder} -processId {processId}");
            Process.Start(info);

            // The user was asked whether saving the project.
            // We do not want to ask again.
            this.IsDocumentModified = false;

            this.TryClose();
        }

        internal Release GetNewRelease(string currentVersion, IEnumerable<Release> releases)
        {
            bool isPreRelease = currentVersion.Contains("beta", StringComparison.InvariantCultureIgnoreCase);
            var candidates = releases.Where(x => !x.Draft);
            if (!isPreRelease)
            {
                candidates = candidates.Where(x => !x.Prerelease);
            }

            return candidates.SingleOrDefault(x => IsGreater(x.TagName, currentVersion));

            static bool IsGreater(string tag, string current)
            {
                if (tag == current)
                {
                    return false;
                }

                var tagElements = tag.Split('-');
                var tagMain = tagElements[0];
                var tagBeta = tagElements.Length > 1 ? tagElements[1] : string.Empty;
                var currentElements = current.Split('-');
                var currentMain = currentElements[0];
                var currentBeta = currentElements.Length > 1 ? currentElements[1] : string.Empty;

                if (tagMain.CompareTo(currentMain) > 0)
                {
                    // new release
                    return true;
                }

                if (tagMain == currentMain)
                {
                    // same target version -> check beta
                    if (string.IsNullOrEmpty(tagBeta))
                    {
                        // update from beta to release?
                        return !string.IsNullOrEmpty(currentBeta);
                    }

                    return tagBeta.CompareTo(currentBeta) > 0;
                }

                // older release version
                return false;
            }
        }

        private ulong GetMaxBookIdent()
        {
            if (this.currentModelJournal.Booking?.Any() == false)
            {
                return 0;
            }

            return this.currentModelJournal.Booking.Max(b => b.ID);
        }

        private void CloseYear()
        {
            var viewModel = new CloseYearViewModel(this.currentModelJournal);
            viewModel.Accounts.AddRange(
                this.accountingData.AllAccounts.Where(x => x.Active && x.Type == AccountDefinitionType.Carryforward));

            var result = this.windowManager.ShowDialog(viewModel);
            if (result != true)
            {
                return;
            }

            this.currentModelJournal.Closed = true;

            var carryForwardAccount =
                this.accountingData.AllAccounts.Single(a => a.Type == AccountDefinitionType.Carryforward && a.Active);

            var newYearJournal = new AccountingDataJournal
            {
                DateStart = this.currentModelJournal.DateStart + 10000,
                DateEnd = this.currentModelJournal.DateEnd + 10000,
                Booking = new List<AccountingDataJournalBooking>()
            };
            newYearJournal.Year = newYearJournal.DateStart.ToDateTime().Year.ToString(CultureInfo.InvariantCulture);
            this.accountingData.Journal.Add(newYearJournal);

            ulong bookingId = 1;

            // Asset Accounts (Bestandskonten), Credit and Debit Accounts
            var accounts = this.accountingData.AllAccounts.Where(a => a.Type == AccountDefinitionType.Asset || a.Type == AccountDefinitionType.Credit || a.Type == AccountDefinitionType.Debit);
            foreach (var account in accounts)
            {
                if (this.currentModelJournal.Booking == null)
                {
                    continue;
                }

                var creditAmount = this.currentModelJournal.Booking
                    .SelectMany(b => b.Credit.Where(x => x.Account == account.ID))
                    .Sum(x => x.Value);
                var debitAmount = this.currentModelJournal.Booking
                    .SelectMany(b => b.Debit.Where(x => x.Account == account.ID))
                    .Sum(x => x.Value);

                if (creditAmount == 0 && debitAmount == 0 || creditAmount == debitAmount)
                {
                    // nothing to do
                    continue;
                }

                var newBooking = new AccountingDataJournalBooking
                {
                    Date = newYearJournal.DateStart,
                    ID = bookingId,
                    Debit = new List<BookingValue>(),
                    Credit = new List<BookingValue>(),
                    Opening = true
                };
                newYearJournal.Booking.Add(newBooking);
                var newDebit = new BookingValue
                {
                    Value = Math.Abs(creditAmount - debitAmount),
                    Text = $"Eröffnungsbetrag {bookingId}"
                };
                newBooking.Debit.Add(newDebit);
                var newCredit = new BookingValue
                {
                    Value = newDebit.Value,
                    Text = newDebit.Text
                };
                newBooking.Credit.Add(newCredit);
                if (creditAmount > debitAmount)
                {
                    newCredit.Account = account.ID;
                    newDebit.Account = carryForwardAccount.ID;
                }
                else
                {
                    newDebit.Account = account.ID;
                    newCredit.Account = carryForwardAccount.ID;
                }

                bookingId++;
            }

            this.IsDocumentModified = true;
            this.SelectBookingYear(newYearJournal.Year);

            this.UpdateBookingYears();
        }

        private string BuildAccountDescription(ulong accountNumber)
        {
            var account = this.accountingData.AllAccounts.Single(a => a.ID == accountNumber);
            return account.FormatName();
        }

        private void UpdateDisplayName()
        {
            if (string.IsNullOrEmpty(this.fileName) || this.currentModelJournal == null)
            {
                this.DisplayName = $"SimpleAccounting {this.version}";
            }
            else
            {
                this.DisplayName = $"SimpleAccounting {this.version} - {this.fileName} - {this.currentModelJournal.Year}";
            }
        }

        private void SelectBookingYear(string newYearName)
        {
            this.currentModelJournal = this.accountingData.Journal.Single(y => y.Year == newYearName);
            this.UpdateDisplayName();
            this.RefreshFullJournal();
            var firstBooking = this.currentModelJournal.Booking.FirstOrDefault();
            if (firstBooking != null)
            {
                var firstAccount =
                    firstBooking.Credit.Select(x => x.Account)
                    .Concat(firstBooking.Debit.Select(x => x.Account))
                    .Min();
                this.SelectedAccount = this.AccountList.Single(x => x.Identifier == firstAccount);
                this.RefreshAccountJournal();
            }
            else if (this.AccountList.Any())
            {
                this.SelectedAccount = this.AccountList.First();
                this.RefreshAccountJournal();
            }
            else
            {
                this.AccountJournal.Clear();
            }
        }

        internal void LoadProjectFromFile(string projectFileName)
        {
            if (!this.CheckSaveProject())
            {
                return;
            }

            this.IsDocumentModified = false;
            this.fileName = projectFileName;

            try
            {
                var projectData = AccountingData.LoadFromFile(this.fileName);
                if (projectData.Migrate())
                {
                    this.IsDocumentModified = true;
                }

                this.LoadProjectData(projectData);

                Settings.Default.RecentProject = this.fileName;
                Settings.Default.RecentProjects.Remove(this.fileName);
                Settings.Default.RecentProjects.Insert(0, this.fileName);
                while (Settings.Default.RecentProjects.Count > MaxRecentProjects)
                {
                    Settings.Default.RecentProjects.RemoveAt(MaxRecentProjects);
                }

                Settings.Default.Save();
            }
            catch (InvalidOperationException e)
            {
                this.messageBox.Show($"Failed to load file '{this.fileName}':\n{e.Message}", "Load");
            }
        }

        internal void LoadProjectData(AccountingData projectData)
        {
            this.accountingData = projectData;
            this.UpdateBookingYears();

            this.AllAccounts.Clear();
            foreach (var accountGroup in this.accountingData.Accounts)
            {
                foreach (var account in accountGroup.Account)
                {
                    var accountModel = new AccountViewModel
                    {
                        Identifier = account.ID,
                        Name = account.Name,
                        Group = accountGroup,
                        Groups = this.accountingData.Accounts,
                        Type = account.Type,
                        IsActivated = account.Active
                    };
                    this.AllAccounts.Add(accountModel);
                }
            }

            this.RefreshAccountList();

            // select last booking year after loading
            this.BookingYears.Last().Command.Execute(null);
        }

        private static AccountingData GetTemplateProject()
        {
            var year = (ushort)DateTime.Now.Year;
            var defaultAccounts = new List<AccountDefinition>
            {
                new AccountDefinition
                {
                    ID = 100, Name = "Bank account", Type = AccountDefinitionType.Asset
                },
                new AccountDefinition
                {
                    ID = 400, Name = "Salary", Type = AccountDefinitionType.Income
                },
                new AccountDefinition
                {
                    ID = 600, Name = "Food", Type = AccountDefinitionType.Expense
                },
                new AccountDefinition
                {
                    ID = 990, Name = "Carryforward", Type = AccountDefinitionType.Carryforward
                }
            };
            var accountJournal = new AccountingDataJournal
            {
                Year = year.ToString(CultureInfo.InvariantCulture),
                DateStart = (uint)year * 10000 + 101,
                DateEnd = (uint)year * 10000 + 1231,
                Booking = new List<AccountingDataJournalBooking>()
            };
            return new AccountingData
            {
                Accounts = new List<AccountingDataAccountGroup>
                {
                    new AccountingDataAccountGroup
                    {
                        Name = "Default",
                        Account = defaultAccounts
                    }
                },
                Journal = new List<AccountingDataJournal> { accountJournal }
            };
        }

        private void UpdateBookingYears()
        {
            this.BookingYears.Clear();
            foreach (var year in this.accountingData.Journal)
            {
                var menu = new MenuViewModel(
                    year.Year.ToString(CultureInfo.InvariantCulture),
                    new RelayCommand(_ => this.SelectBookingYear(year.Year)));
                this.BookingYears.Add(menu);
            }
        }

        private bool CheckSaveProject()
        {
            if (!this.IsDocumentModified)
            {
                // no need to save the project
                return true;
            }

            var result = this.messageBox.Show(
                "Die Datenbasis hat sich geändert.\nWollen Sie Speichern?",
                "Programm beenden",
                MessageBoxButton.YesNoCancel);
            if (result == MessageBoxResult.Cancel)
            {
                return false;
            }
            else if (result == MessageBoxResult.Yes)
            {
                this.SaveProject();
                return true;
            }
            else
            {
                return true;
            }
        }

        private void SaveProject()
        {
            if (this.fileName == "<new>")
            {
                using var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Accounting project files (*.acml)|*.acml",
                    RestoreDirectory = true
                };

                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                this.fileName = saveFileDialog.FileName;
            }

            DateTime fileDate = File.GetLastWriteTime(this.fileName);
            string backupFileName = $"{this.fileName}.{fileDate:yyyyMMddHHmmss}";
            try
            {
                File.Move(this.fileName, backupFileName);
            }
            catch (FileNotFoundException)
            {
                // ignored
            }

            this.accountingData.SaveToFile(this.fileName);
            this.IsDocumentModified = false;
        }

        private void RefreshFullJournal()
        {
            this.FullJournal.Clear();
            if (this.currentModelJournal.Booking == null)
            {
                return;
            }

            foreach (var booking in this.currentModelJournal.Booking.OrderBy(b => b.Date))
            {
                var item = new FullJournalViewModel { Date = booking.Date.ToDateTime(), Identifier = booking.ID };
                var debitAccounts = booking.Debit;
                var creditAccounts = booking.Credit;
                if (debitAccounts.Count == 1 && creditAccounts.Count == 1)
                {
                    var debit = debitAccounts[0];
                    item.Text = debit.Text;
                    item.Value = Convert.ToDouble(debit.Value) / CentFactor;
                    item.DebitAccount = this.BuildAccountDescription(debit.Account);
                    item.CreditAccount = this.BuildAccountDescription(creditAccounts[0].Account);
                    this.FullJournal.Add(item);
                    continue;
                }

                foreach (var debitEntry in debitAccounts)
                {
                    var debitItem = item.Clone();
                    debitItem.Text = debitEntry.Text;
                    debitItem.Value = Convert.ToDouble(debitEntry.Value) / CentFactor;
                    debitItem.DebitAccount = this.BuildAccountDescription(debitEntry.Account);
                    this.FullJournal.Add(debitItem);
                }

                foreach (var creditEntry in creditAccounts)
                {
                    var creditItem = item.Clone();
                    creditItem.Text = creditEntry.Text;
                    creditItem.Value = Convert.ToDouble(creditEntry.Value) / CentFactor;
                    creditItem.CreditAccount = this.BuildAccountDescription(creditEntry.Account);
                    this.FullJournal.Add(creditItem);
                }
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
            this.AccountJournal.Clear();
            if (this.currentModelJournal.Booking == null)
            {
                return;
            }

            var accountNumber = this.SelectedAccount.Identifier;
            double creditSum = 0;
            double debitSum = 0;
            var entries =
                this.currentModelJournal.Booking.Where(b => b.Credit.Any(x => x.Account == accountNumber))
                .Concat(this.currentModelJournal.Booking.Where(b => b.Debit.Any(x => x.Account == accountNumber)));
            foreach (var entry in entries.OrderBy(x => x.Date).ThenBy(x => x.ID))
            {
                var item = new AccountJournalViewModel { Date = entry.Date.ToDateTime() };
                this.AccountJournal.Add(item);
                item.Identifier = entry.ID;
                var debitEntry = entry.Debit.FirstOrDefault(x => x.Account == accountNumber);
                if (debitEntry != null)
                {
                    item.Text = debitEntry.Text;
                    item.DebitValue = Convert.ToDouble(debitEntry.Value) / CentFactor;
                    debitSum += item.DebitValue;
                    item.RemoteAccount = entry.Credit.Count == 1
                        ? this.BuildAccountDescription(entry.Credit.Single().Account)
                        : "Diverse";
                }
                else
                {
                    var creditEntry = entry.Credit.FirstOrDefault(x => x.Account == accountNumber);
                    item.Text = creditEntry.Text;
                    item.CreditValue = Convert.ToDouble(creditEntry.Value) / CentFactor;
                    creditSum += item.CreditValue;
                    item.RemoteAccount = entry.Debit.Count == 1
                        ? this.BuildAccountDescription(entry.Debit.Single().Account)
                        : "Diverse";
                }
            }

            if (debitSum < double.Epsilon && creditSum < double.Epsilon)
            {
                // no summary required
                return;
            }

            var sumItem = new AccountJournalViewModel();
            this.AccountJournal.Add(sumItem);
            sumItem.IsSummary = true;
            sumItem.Text = "Summe";
            sumItem.DebitValue = debitSum;
            sumItem.CreditValue = creditSum;

            var saldoItem = new AccountJournalViewModel();
            this.AccountJournal.Add(saldoItem);
            saldoItem.IsSummary = true;
            saldoItem.Text = "Saldo";
            if (debitSum > creditSum)
            {
                saldoItem.DebitValue = debitSum - creditSum;
            }
            else
            {
                saldoItem.CreditValue = creditSum - debitSum;
            }
        }
    }
}