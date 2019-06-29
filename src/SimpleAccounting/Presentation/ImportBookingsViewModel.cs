// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Caliburn.Micro;
using CsvHelper;
using CsvHelper.Configuration;
using lg2de.SimpleAccounting.Extensions;
using lg2de.SimpleAccounting.Model;

namespace lg2de.SimpleAccounting.Presentation
{
    internal class ImportBookingsViewModel : Screen
    {
        private readonly ShellViewModel parent;

        private ulong importAccount;

        public ImportBookingsViewModel(ShellViewModel parent)
        {
            this.parent = parent;

            this.DisplayName = "Import von Kontodaten";
        }

        public List<AccountingDataAccount> Accounts { get; }
            = new List<AccountingDataAccount>();

        public DateTime RangeMin { get; internal set; }

        public DateTime RangMax { get; internal set; }

        public AccountingDataJournal Journal { get; internal set; }

        public ulong BookingNumber { get; internal set; }

        public ulong ImportAccount
        {
            get => this.importAccount;
            set
            {
                if (this.importAccount == value)
                {
                    return;
                }

                this.importAccount = value;
                this.NotifyOfPropertyChange();
            }
        }

        public AccountingDataAccount SelectedAccount { get; set; }

        public ObservableCollection<ImportEntryViewModel> ImportData { get; }
            = new ObservableCollection<ImportEntryViewModel>();
        public ICommand LoadDataCommand => new RelayCommand(_ =>
        {
            using (var openFileDialog = new System.Windows.Forms.OpenFileDialog())
            {
                openFileDialog.Filter = "Booking data files (*.csv)|*.csv";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                using (var reader = new StreamReader(openFileDialog.FileName))
                {
                    this.ImportBookings(reader);
                }
            }

        }, _ => this.SelectedAccount != null);

        public ICommand BookCommand => new RelayCommand(_ =>
        {
            foreach (var item in this.ImportData)
            {
                var newBooking = new AccountingDataJournalBooking
                {
                    Date = item.Date.ToAccountingDate(),
                    ID = item.Identifier
                };
                var creditValue = new BookingValue
                {
                    Text = item.Text,
                    Value = (int)Math.Abs(Math.Round(item.Value * 100))
                };
                var debitValue = creditValue.Clone();
                if (item.Value > 0)
                {
                    creditValue.Account = item.RemoteAccount.ID;
                    debitValue.Account = this.ImportAccount;
                }
                else
                {
                    creditValue.Account = this.ImportAccount;
                    debitValue.Account = item.RemoteAccount.ID;
                }

                newBooking.Credit = new List<BookingValue> { creditValue };
                newBooking.Debit = new List<BookingValue> { debitValue };
                this.parent.AddBooking(newBooking);
            }

            this.TryClose(null);
        }, _ => this.ImportData.All(x => x.RemoteAccount != null));

        internal void ImportBookings(TextReader reader)
        {
            var lastEntry = this.Journal.Booking
                .Where(x => x.Credit.Any(c => c.Account == this.ImportAccount) || x.Debit.Any(c => c.Account == this.ImportAccount))
                .OrderBy(x => x.Date)
                .LastOrDefault();
            if (lastEntry != null)
            {
                this.RangeMin = lastEntry.Date.ToDateTime() + TimeSpan.FromDays(1);
            }

            var configuration = new Configuration { Encoding = Encoding.GetEncoding(850) };
            using (var csv = new CsvReader(reader, configuration))
            {
                csv.Read();
                var header = csv.ReadHeader();
                while (csv.Read())
                {
                    var date = csv.GetField<DateTime>("Buchungsdatum");
                    if (date < this.RangeMin || date > this.RangMax)
                    {
                        continue;
                    }

                    var item = new ImportEntryViewModel
                    {
                        Date = date,
                        Accounts = this.Accounts,
                        Identifier = this.BookingNumber++,
                        Name = csv.GetField("Name 1"),
                        Text = csv.GetField("Verwendungszweck"),
                        Value = csv.GetField<double>("Betrag")
                    };
                    this.ImportData.Add(item);
                }
            }
        }
    }
}
