// <copyright>
//     Copyright (c) Lukas Gr�tzmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Input;
    using Caliburn.Micro;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Properties;
    using lg2de.SimpleAccounting.Reports;

    [SuppressMessage("Critical Code Smell", "S2365:Properties should not make collection or array copies", Justification = "<Pending>")]
    internal class ShellViewModel : Conductor<IScreen>
    {
        private readonly IWindowManager windowManager;
        private readonly IReportFactory reportFactory;
        private readonly IMessageBox messageBox;
        private readonly IFileSystem fileSystem;

        private AccountingData accountingData;
        private string fileName = "";
        private AccountingDataJournal currentJournal;

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
        }

        public ObservableCollection<MenuViewModel> RecentProjects { get; }
            = new ObservableCollection<MenuViewModel>();

        public ObservableCollection<MenuViewModel> BookingYears { get; }
            = new ObservableCollection<MenuViewModel>();

        public ObservableCollection<AccountViewModel> Accounts { get; }
            = new ObservableCollection<AccountViewModel>();

        public ObservableCollection<JournalViewModel> Journal { get; }
            = new ObservableCollection<JournalViewModel>();

        public ObservableCollection<AccountJournalViewModel> AccountJournal { get; }
            = new ObservableCollection<AccountJournalViewModel>();

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
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Acconting project files (*.bxml)|*.bxml";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                this.LoadProjectFromFile(openFileDialog.FileName);
            }
        });

        public ICommand SaveProjectCommand => new RelayCommand(
            _ => this.SaveProject(),
            _ => this.IsDocumentChanged);

        public ICommand CloseApplicationCommand => new RelayCommand(_ => this.TryClose(null));

        public ICommand AddBookingsCommand => new RelayCommand(_ =>
        {
            var bookingModel = new AddBookingViewModel(
                this,
                this.currentJournal.DateStart.ToDateTime(),
                this.currentJournal.DateEnd.ToDateTime())
            {
                BookingNumber = this.GetMaxBookIdent() + 1
            };
            bookingModel.Accounts.AddRange(this.accountingData.AllAccounts);
            this.accountingData.Setup?.BookingTemplates?.Template
                .Select(t => new BookingTemplate { Text = t.Text, Credit = t.Credit, Debit = t.Debit, Value = t.Value / 100.0 })
                .ToList().ForEach(bookingModel.BindingTemplates.Add);
            this.windowManager.ShowDialog(bookingModel);
        }, _ => this.IsCurrentYearOpen);

        public ICommand ImportBookingsCommand => new RelayCommand(_ =>
        {
            var min = this.currentJournal.DateStart.ToDateTime();
            var max = this.currentJournal.DateEnd.ToDateTime();

            var importModel = new ImportBookingsViewModel(
                this.messageBox,
                this,
                this.accountingData.AllAccounts)
            {
                BookingNumber = this.GetMaxBookIdent() + 1,
                RangeMin = min,
                RangMax = max,
                Journal = this.currentJournal
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
                    this.currentJournal,
                    this.accountingData.Setup,
                    CultureInfo.CurrentUICulture);
                report.CreateReport(this.currentJournal.DateStart.ToDateTime(), this.currentJournal.DateEnd.ToDateTime());
                report.ShowPreview($"{DateTime.Now:yyyy-MM-dd} Journal {this.currentJournal.Year}");
            },
            _ => this.Journal.Any());

        public ICommand AccountJournalReportCommand => new RelayCommand(
            _ =>
            {
                var report = this.reportFactory.CreateAccountJournal(
                    this.accountingData.Accounts.SelectMany(a => a.Account),
                    this.currentJournal,
                    this.accountingData.Setup,
                    CultureInfo.CurrentUICulture);
                report.PageBreakBetweenAccounts = this.accountingData.Setup?.Reports?.AccountJournalReport?.PageBreakBetweenAccounts ?? false;
                report.CreateReport(this.currentJournal.DateStart.ToDateTime(), this.currentJournal.DateEnd.ToDateTime());
                report.ShowPreview($"{DateTime.Now:yyyy-MM-dd} Kontobl�tter {this.currentJournal.Year}");
            },
            _ => this.Journal.Any());

        public ICommand TotalsAndBalancesReportCommand => new RelayCommand(
            _ =>
            {
                var report = new TotalsAndBalancesReport(
                    this.currentJournal,
                    this.accountingData.Accounts,
                    this.accountingData.Setup,
                    this.currentJournal.Year.ToString(CultureInfo.InvariantCulture));
                report.CreateReport(this.currentJournal.DateStart.ToDateTime(), this.currentJournal.DateEnd.ToDateTime());
            },
            _ => this.Journal.Any());

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
                    this.currentJournal,
                    accountGroups,
                    this.accountingData.Setup,
                    this.currentJournal.Year.ToString(CultureInfo.InvariantCulture));
                this.accountingData.Setup.Reports?.TotalsAndBalancesReport?.ForEach(report.Signatures.Add);
                report.CreateReport(this.currentJournal.DateStart.ToDateTime(), this.currentJournal.DateEnd.ToDateTime());
            },
            _ => this.Journal.Any());

        public ICommand AnnualBalanceReportCommand => new RelayCommand(
            _ =>
            {
                var report = new AnnualBalanceReport(
                    this.currentJournal,
                    this.accountingData.AllAccounts,
                    this.accountingData.Setup,
                    this.currentJournal.Year.ToString(CultureInfo.InvariantCulture));
                report.CreateReport();
            },
            _ => this.Journal.Any());

        public ICommand AccountSelectionCommand => new RelayCommand(o =>
        {
            if (o is AccountViewModel account)
            {
                this.BuildAccountJournal(account.Identifier);
            }
        });

        public ICommand NewAccountCommand => new RelayCommand(_ =>
        {
            var accountVm = new AccountViewModel
            {
                DisplayName = "Account erstellen",
                Group = this.accountingData.Accounts[0], // TODO make selectable
                IsAvalidIdentifierFunc = id => this.Accounts.All(a => a.Identifier != id)
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
                Type = accountVm.Type
            };
            accountVm.Group.Account.Add(newAccount);
            accountVm.Group.Account = accountVm.Group.Account.OrderBy(x => x.ID).ToList();

            // update view
            this.Accounts.Add(accountVm);
            var sorted = this.Accounts.OrderBy(x => x.Identifier).ToList();
            this.Accounts.Clear();
            sorted.ForEach(this.Accounts.Add);

            this.IsDocumentChanged = true;
        });

        public ICommand EditAccountCommand => new RelayCommand(o =>
        {
            var account = o as AccountViewModel;
            if (account == null)
            {
                return;
            }

            var vm = account.Clone();
            vm.DisplayName = "Account bearbeiten";
            var invalidIds = this.Accounts.Select(x => x.Identifier).Where(x => x != account.Identifier).ToList();
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
            account.Type = vm.Type;
            if (account.Identifier != vm.Identifier)
            {
                account.Identifier = vm.Identifier;
                var sorted = this.Accounts.OrderBy(x => x.Identifier).ToList();
                this.Accounts.Clear();
                sorted.ForEach(this.Accounts.Add);
            }

            account.Refresh();
            this.RefreshJournal();

            this.IsDocumentChanged = true;

            void UpdateAccount(BookingValue entry, ulong oldIdentifier, ulong newIdentifier)
            {
                if (entry.Account == oldIdentifier)
                {
                    entry.Account = newIdentifier;
                }
            }
        });

        internal Settings Settings { get; set; } = Settings.Default;

        private bool IsDocumentChanged { get; set; }

        private bool IsCurrentYearOpen
        {
            get
            {
                if (this.currentJournal == null)
                {
                    return false;
                }

                return !this.currentJournal.Closed;
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

            this.DisplayName = "SimpleAccounting";
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
            this.currentJournal.Booking.Add(booking);
            this.IsDocumentChanged = true;

            if (refreshJournal)
            {
                this.RefreshJournal();
            }
        }

        private ulong GetMaxBookIdent()
        {
            if (this.currentJournal.Booking?.Any() == false)
            {
                return 0;
            }

            return this.currentJournal.Booking.Max(b => b.ID);
        }

        private void CloseYear()
        {
            var result = this.messageBox.Show(
                $"Wollen Sie das Jahr {this.currentJournal.Year} abschlie�en?",
                "Jahresabschluss",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            this.currentJournal.Closed = true;

            var carryForwardAccount =
                this.accountingData.AllAccounts.Single(a => a.Type == AccountDefinitionType.Carryforward && a.Active);

            var newYearJournal = new AccountingDataJournal
            {
                DateStart = this.currentJournal.DateStart + 10000,
                DateEnd = this.currentJournal.DateEnd + 10000,
                Booking = new List<AccountingDataJournalBooking>()
            };
            newYearJournal.Year = newYearJournal.DateStart.ToDateTime().Year.ToString(CultureInfo.InvariantCulture);
            this.accountingData.Journal.Add(newYearJournal);

            ulong bookingId = 1;

            // Asset Accounts (Bestandskonten), Credit and Debit Accounts
            var accounts = this.accountingData.AllAccounts.Where(a => a.Type == AccountDefinitionType.Asset || a.Type == AccountDefinitionType.Credit || a.Type == AccountDefinitionType.Debit);
            foreach (var account in accounts)
            {
                if (this.currentJournal.Booking == null)
                {
                    continue;
                }

                var creditAmount = this.currentJournal.Booking
                    .SelectMany(b => b.Credit.Where(x => x.Account == account.ID))
                    .Sum(x => x.Value);
                var debitAmount = this.currentJournal.Booking
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
                    Text = $"Er�ffnungsbetrag {bookingId}"
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

            this.IsDocumentChanged = true;
            this.SelectBookingYear(newYearJournal.Year);

            this.UpdateBookingYears();
        }

        private string BuildAccountDescription(ulong accountNumber)
        {
            var account = this.accountingData.AllAccounts.Single(a => a.ID == accountNumber);
            return account.FormatName();
        }

        private void SelectBookingYear(string newYearName)
        {
            this.currentJournal = this.accountingData.Journal.Single(y => y.Year == newYearName);
            this.DisplayName = $"SimpleAccounting - {this.fileName} - {this.currentJournal.Year}";
            this.AccountJournal.Clear();
            this.RefreshJournal();
        }

        internal void LoadProjectFromFile(string projectFileName)
        {
            if (!this.CheckSaveProject())
            {
                return;
            }

            this.IsDocumentChanged = false;
            this.fileName = projectFileName;

            try
            {
                var projectData = AccountingData.LoadFromFile(this.fileName);
                if (projectData.Migrate())
                {
                    this.IsDocumentChanged = true;
                }

                this.LoadProjectData(projectData);

                Settings.Default.RecentProject = this.fileName;
                Settings.Default.RecentProjects.Remove(this.fileName);
                Settings.Default.RecentProjects.Insert(0, this.fileName);
                while (Settings.Default.RecentProjects.Count > 10)
                {
                    Settings.Default.RecentProjects.RemoveAt(10);
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

            this.Accounts.Clear();
            foreach (var accountGroup in this.accountingData.Accounts)
            {
                foreach (var account in accountGroup.Account)
                {
                    var accountModel = new AccountViewModel
                    {
                        Identifier = account.ID,
                        Name = account.Name,
                        Group = accountGroup,
                        Type = account.Type
                    };
                    this.Accounts.Add(accountModel);
                }
            }

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
            if (!this.IsDocumentChanged)
            {
                // no need to save the project
                return true;
            }

            var result = this.messageBox.Show(
                "Die Datenbasis hat sich ge�ndert.\nWollen Sie Speichern?",
                "Programm beenden",
                MessageBoxButton.YesNoCancel);
            if (result == MessageBoxResult.Cancel)
            {
                return false;
            }
            else if (result == MessageBoxResult.Yes)
            {
                this.SaveProject();
            }

            return true;
        }

        private void SaveProject()
        {
            if (this.fileName == "<new>")
            {
                using (var saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Acconting project files (*.bxml)|*.bxml";
                    saveFileDialog.RestoreDirectory = true;

                    if (saveFileDialog.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }

                    this.fileName = saveFileDialog.FileName;
                }
            }

            DateTime fileDate = File.GetLastWriteTime(this.fileName);
            string backupFileName = this.fileName + "." + fileDate.ToString("yyyyMMddHHmmss");
            try
            {
                File.Move(this.fileName, backupFileName);
            }
            catch (FileNotFoundException)
            {
                // ignored
            }

            this.accountingData.SaveToFile(this.fileName);
            this.IsDocumentChanged = false;
        }

        private void RefreshJournal()
        {
            this.Journal.Clear();
            if (this.currentJournal.Booking == null)
            {
                return;
            }

            foreach (var booking in this.currentJournal.Booking.OrderBy(b => b.Date))
            {
                var item = new JournalViewModel { Date = booking.Date.ToDateTime(), Identifier = booking.ID };
                var debitAccounts = booking.Debit;
                var creditAccounts = booking.Credit;
                if (debitAccounts.Count == 1 && creditAccounts.Count == 1)
                {
                    var debit = debitAccounts[0];
                    item.Text = debit.Text;
                    item.Value = Convert.ToDouble(debit.Value) / 100;
                    item.DebitAccount = this.BuildAccountDescription(debit.Account);
                    item.CreditAccount = this.BuildAccountDescription(creditAccounts[0].Account);
                    this.Journal.Add(item);
                    continue;
                }

                foreach (var debitEntry in debitAccounts)
                {
                    var debitItem = item.Clone();
                    debitItem.Text = debitEntry.Text;
                    debitItem.Value = Convert.ToDouble(debitEntry.Value) / 100;
                    debitItem.DebitAccount = this.BuildAccountDescription(debitEntry.Account);
                    this.Journal.Add(debitItem);
                }

                foreach (var creditEntry in creditAccounts)
                {
                    var creditItem = item.Clone();
                    creditItem.Text = creditEntry.Text;
                    creditItem.Value = Convert.ToDouble(creditEntry.Value) / 100;
                    creditItem.CreditAccount = this.BuildAccountDescription(creditEntry.Account);
                    this.Journal.Add(creditItem);
                }
            }
        }

        private void BuildAccountJournal(ulong accountNumber)
        {
            this.AccountJournal.Clear();
            if (this.currentJournal.Booking == null)
            {
                return;
            }

            double nCreditSum = 0;
            double nDebitSum = 0;
            var entries =
                this.currentJournal.Booking.Where(b => b.Credit.Any(x => x.Account == accountNumber))
                .Concat(this.currentJournal.Booking.Where(b => b.Debit.Any(x => x.Account == accountNumber)));
            foreach (var entry in entries.OrderBy(x => x.Date))
            {
                var item = new AccountJournalViewModel { Date = entry.Date.ToDateTime() };
                this.AccountJournal.Add(item);
                item.Identifier = entry.ID;
                var debitEntry = entry.Debit.FirstOrDefault(x => x.Account == accountNumber);
                if (debitEntry != null)
                {
                    item.Text = debitEntry.Text;
                    item.DebitValue = Convert.ToDouble(debitEntry.Value) / 100;
                    nDebitSum += item.DebitValue;
                    item.RemoteAccount = entry.Credit.Count == 1
                        ? this.BuildAccountDescription(entry.Credit.Single().Account)
                        : "Diverse";
                }
                else
                {
                    var creditEntry = entry.Credit.FirstOrDefault(x => x.Account == accountNumber);
                    item.Text = creditEntry.Text;
                    item.CreditValue = Convert.ToDouble(creditEntry.Value) / 100;
                    nCreditSum += item.CreditValue;
                    item.RemoteAccount = entry.Debit.Count == 1
                        ? this.BuildAccountDescription(entry.Debit.Single().Account)
                        : "Diverse";
                }
            }

            var sumItem = new AccountJournalViewModel();
            this.AccountJournal.Add(sumItem);
            sumItem.IsSummary = true;
            sumItem.Text = "Summe";
            sumItem.DebitValue = nDebitSum;
            sumItem.CreditValue = nCreditSum;

            var saldoItem = new AccountJournalViewModel();
            this.AccountJournal.Add(saldoItem);
            saldoItem.IsSummary = true;
            saldoItem.Text = "Saldo";
            if (nDebitSum > nCreditSum)
            {
                saldoItem.DebitValue = nDebitSum - nCreditSum;
            }
            else
            {
                saldoItem.CreditValue = nCreditSum - nDebitSum;
            }
        }
    }
}