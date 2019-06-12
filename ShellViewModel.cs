// <copyright>
//     Copyright (c) Lukas Gr�tzmacher. All rights reserved.
// </copyright>

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using Caliburn.Micro;
using lg2de.SimpleAccounting.Properties;

namespace lg2de.SimpleAccounting
{
    public class ShellViewModel : Conductor<IScreen>
    {
        private readonly IWindowManager windowManager;
        private AccountingData accountingData;
        string fileName = "";

        string bookingYearName = "";
        private AccountingDataJournal currentJournal;
        private string firmName;

        public ShellViewModel(IWindowManager windowManager)
        {
            this.windowManager = windowManager;

            this.DisplayName = "SimpleAccounting";
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

        public ICommand OpenProjectCommand => new RelayCommand(_ =>
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Projektdateien (*.bxml)|*.bxml";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                this.LoadProject(openFileDialog.FileName);
            }
        });

        public ICommand SaveProjectCommand => new RelayCommand(
            _ => this.SaveProject(),
            _ => this.IsDocumentChanged);

        public ICommand CloseApplicationCommand => new RelayCommand(_ => this.TryClose(null));

        public ICommand AddBookingsCommand => new RelayCommand(_ =>
        {
            var bookingModel = new AddBookingViewModel(this)
            {
                BookingNumber = this.GetMaxBookIdent() + 1
            };
            bookingModel.Accounts.AddRange(this.accountingData.Accounts);
            bookingModel.BindingTemplates.Add(new BookingTemplate { Text = "Geld abheben", Credit = 110, Debit = 100 });
            this.windowManager.ShowDialog(bookingModel);
        });

        public ICommand CloseYearCommand => new RelayCommand(_ => this.CloseYear());

        public ICommand JournalReportCommand => new RelayCommand(_ =>
        {
            var report = new JournalReport(this.currentJournal, this.firmName, this.bookingYearName);
            var yearNode = this.accountingData.Years.Single(y => y.Name.ToString() == this.bookingYearName);
            report.CreateReport(yearNode.DateStart.ToDateTime(), yearNode.DateStart.ToDateTime());
        });

        public ICommand TotalsBalancesReportCommand => new RelayCommand(_ =>
        {
            var report = new TotalsBalancesReport(this.currentJournal, this.accountingData.Accounts, this.firmName, this.bookingYearName);
            var yearNode = this.accountingData.Years.Single(y => y.Name.ToString() == this.bookingYearName);
            report.CreateReport(yearNode.DateStart.ToDateTime(), yearNode.DateStart.ToDateTime());
        });

        public ICommand AnnualBalanceReportCommand => new RelayCommand(_ =>
        {
            var report = new AnnualBalanceReport(this.currentJournal, this.accountingData.Accounts, this.firmName, this.bookingYearName);
            report.CreateReport();
        });

        public ICommand AccountSelectionCommand => new RelayCommand(o =>
        {
            var account = o as AccountViewModel;
            this.BuildAccountJournal(account.Identifier);
        });

        private bool IsDocumentChanged { get; set; }

        public override void CanClose(Action<bool> callback)
        {
            if (!this.IsDocumentChanged)
            {
                base.CanClose(callback);
                return;
            }

            DialogResult ret = MessageBox.Show(
                "Die Datenbasis hat sich ge�ndert.\nWollen Sie Speichern?",
                "Programm beenden",
                MessageBoxButtons.YesNoCancel);
            if (ret == DialogResult.Cancel)
            {
                callback(false);
                return;
            }
            else if (ret == DialogResult.Yes)
            {
                this.SaveProject();
            }

            base.CanClose(callback);
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            if (File.Exists(Settings.Default.RecentProject))
            {
                this.LoadProject(Settings.Default.RecentProject);
            }

            foreach (var project in Settings.Default.RecentProjects)
            {
                if (!File.Exists(project))
                {
                    continue;
                }

                var item = new MenuViewModel(
                    project,
                    new RelayCommand(_ => this.LoadProject(project)));
                this.RecentProjects.Add(item);
            }
        }

        internal void AddBooking(AccountingDataJournalBooking booking)
        {
            this.currentJournal.Booking.Add(booking);
            this.IsDocumentChanged = true;

            this.RefreshJournal();
        }

        private ulong GetMaxBookIdent()
        {
            if (!this.currentJournal.Booking.Any())
            {
                return 0;
            }

            return this.currentJournal.Booking.Max(b => b.ID);
        }

        void CloseYear()
        {
            var accountingYear = this.accountingData.Years.Single(y => y.Name.ToString() == this.bookingYearName);
            if (accountingYear.Closed)
            {
                // nothing to do
                return;
            }

            var result = MessageBox.Show(
                "Wollen Sie das Jahr " + this.bookingYearName + " abschlie�en?",
                "Jahresabschlu�",
                MessageBoxButtons.YesNo);
            if (result != DialogResult.Yes)
            {
                return;
            }

            accountingYear.Closed = true;

            var newYear = (ushort)(Convert.ToUInt16(this.bookingYearName) + 1);

            var newYearEntry = new AccountingDataYear
            {
                Name = newYear,
                DateStart = accountingYear.DateStart + 10000,
                DateEnd = accountingYear.DateEnd + 10000
            };
            this.accountingData.Years.Add(newYearEntry);
            var newYearJournal = new AccountingDataJournal { Year = newYear };
            this.accountingData.Journal.Add(newYearJournal);

            ulong bookingId = 1;

            // Asset Accounts (Bestandskonten), Credit and Debit Accounts
            var accounts = this.accountingData.Accounts.Where(a => a.Type == AccountingDataAccountType.Asset || a.Type == AccountingDataAccountType.Credit || a.Type == AccountingDataAccountType.Debit);
            foreach (var account in accounts)
            {
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
                    Date = newYearEntry.DateStart,
                    ID = bookingId,
                    Opening = true
                };
                newYearJournal.Booking.Add(newBooking);
                var newDebit = new BookingValue
                {
                    Value = Math.Abs(creditAmount - debitAmount),
                    Text = "EB-Wert " + bookingId.ToString()
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
                    newDebit.Account = 990;
                }
                else
                {
                    newDebit.Account = account.ID;
                    newCredit.Account = 990;
                }

                bookingId++;
            }

            this.IsDocumentChanged = true;
            this.SelectBookingYear(newYear);

            this.UpdateBookingYears();
        }

        string BuildAccountDescription(string strAccountNumber)
        {
            var nAccountNumber = Convert.ToUInt32(strAccountNumber);
            string strAccountName = this.accountingData.Accounts.Single(a => a.ID == nAccountNumber).Name;
            return strAccountNumber + " (" + strAccountName + ")";
        }

        void SelectBookingYear(ushort newYear)
        {
            this.bookingYearName = newYear.ToString();
            this.currentJournal = this.accountingData.Journal.Single(y => y.Year == newYear);
            this.DisplayName = $"SimpleAccounting - {this.fileName} - {this.bookingYearName}";
            this.AccountJournal.Clear();
            this.RefreshJournal();
        }

        public void LoadProject(string fileName)
        {
            if (this.IsDocumentChanged)
            {
                var result = MessageBox.Show(
                    "Die Datenbasis hat sich ge�ndert.\nWollen Sie Speichern?",
                    "Programm beenden",
                    MessageBoxButtons.YesNoCancel);
                if (result == DialogResult.Cancel)
                {
                    return;
                }
                else if (result == DialogResult.Yes)
                {
                    this.SaveProject();
                }
            }

            this.IsDocumentChanged = false;
            this.Accounts.Clear();

            this.fileName = fileName;
            this.accountingData = AccountingData.LoadFromFile(this.fileName);
            this.firmName = this.accountingData.Setup.Name;
            this.UpdateBookingYears();

            foreach (var account in this.accountingData.Accounts)
            {
                var acountModel = new AccountViewModel { Identifier = account.ID, Name = account.Name };
                this.Accounts.Add(acountModel);
            }

            // select last booking year after loading
            this.BookingYears.LastOrDefault()?.Command.Execute(null);

            Settings.Default.RecentProject = fileName;
            Settings.Default.RecentProjects.Remove(fileName);
            Settings.Default.RecentProjects.Insert(0, fileName);
            while (Settings.Default.RecentProjects.Count > 10)
            {
                Settings.Default.RecentProjects.RemoveAt(10);
            }

            Settings.Default.Save();
        }

        private void UpdateBookingYears()
        {
            this.BookingYears.Clear();
            foreach (var year in this.accountingData.Years)
            {
                var bookingYear = new MenuViewModel(
                    year.Name.ToString(),
                    new RelayCommand(_ => this.SelectBookingYear(year.Name)));
                this.BookingYears.Add(bookingYear);
            }
        }

        void SaveProject()
        {
            DateTime fileDate = File.GetLastWriteTime(this.fileName);
            string backupFileName = this.fileName + "." + fileDate.ToString("yyyyMMddHHmmss");
            try
            {
                File.Move(this.fileName, backupFileName);
            }
            catch (FileNotFoundException)
            {
            }

            this.accountingData.SaveToFile(this.fileName);
            this.IsDocumentChanged = false;
        }

        void RefreshJournal()
        {
            this.Journal.Clear();
            bool bColorStatus = false;
            foreach (var booking in this.currentJournal.Booking.OrderBy(b => b.Date))
            {
                var item = new JournalViewModel { Date = booking.Date.ToDateTime() };
                //if (bColorStatus)
                //{
                //    item.BackColor = Color.LightGreen;
                //}

                bColorStatus = !bColorStatus;

                item.Identifier = booking.ID;
                var debitAccounts = booking.Debit;
                var creditAccounts = booking.Credit;
                if (debitAccounts.Count == 1 && creditAccounts.Count == 1)
                {
                    var debit = debitAccounts[0];
                    item.Text = debit.Text;
                    item.Value = Convert.ToDouble(debit.Value) / 100;
                    string accountNumber = debit.Account.ToString();
                    item.DebitAccount = this.BuildAccountDescription(accountNumber);
                    accountNumber = creditAccounts[0].Account.ToString();
                    item.CreditAccount = this.BuildAccountDescription(accountNumber);
                    this.Journal.Add(item);
                    continue;
                }

                foreach (var debitEntry in debitAccounts)
                {
                    var debitItem = item.Clone();
                    debitItem.Text = debitEntry.Text;
                    debitItem.Value = Convert.ToDouble(debitEntry.Value) / 100;
                    string strAccountNumber = debitEntry.Account.ToString();
                    debitItem.DebitAccount = this.BuildAccountDescription(strAccountNumber);
                    this.Journal.Add(debitItem);
                }

                foreach (var creditEntry in creditAccounts)
                {
                    var creditItem = item.Clone();
                    creditItem.Text = creditEntry.Text;
                    creditItem.Value = Convert.ToDouble(creditEntry.Value) / 100;
                    string strAccountNumber = creditEntry.Account.ToString();
                    creditItem.CreditAccount = this.BuildAccountDescription(strAccountNumber);
                    this.Journal.Add(creditItem);
                }
            }
        }

        void BuildAccountJournal(ulong accountNumber)
        {
            this.AccountJournal.Clear();
            double nCreditSum = 0;
            double nDebitSum = 0;
            bool bColorStatus = false;
            var entries =
                this.currentJournal.Booking.Where(b => b.Credit.Any(x => x.Account == accountNumber))
                .Concat(this.currentJournal.Booking.Where(b => b.Debit.Any(x => x.Account == accountNumber)));
            foreach (var entry in entries.OrderBy(x => x.Date))
            {
                var item = new AccountJournalViewModel { Date = entry.Date.ToDateTime() };
                this.AccountJournal.Add(item);
                //if (bColorStatus)
                //{
                //    item.BackColor = Color.LightGreen;
                //}

                bColorStatus = !bColorStatus;

                item.Identifier = entry.ID;
                var debitEntry = entry.Debit.FirstOrDefault(x => x.Account == accountNumber);
                if (debitEntry != null)
                {
                    item.Text = debitEntry.Text;
                    item.DebitValue = Convert.ToDouble(debitEntry.Value) / 100;
                    nDebitSum += item.DebitValue;
                    if (entry.Credit.Count == 1)
                    {
                        string creditAccount = entry.Credit[0].Account.ToString();
                        item.RemoteAccount = this.BuildAccountDescription(creditAccount);
                    }
                    else
                    {
                        item.RemoteAccount = "Diverse";
                    }
                }
                else
                {
                    var creditEntry = entry.Credit.FirstOrDefault(x => x.Account == accountNumber);
                    item.Text = creditEntry.Text;
                    item.CreditValue = Convert.ToDouble(creditEntry.Value) / 100;
                    nCreditSum += item.CreditValue;
                    if (entry.Debit.Count == 1)
                    {
                        string debitAccount = entry.Debit[0].Account.ToString();
                        item.RemoteAccount = this.BuildAccountDescription(debitAccount);
                    }
                    else
                    {
                        item.RemoteAccount = "Diverse";
                    }
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