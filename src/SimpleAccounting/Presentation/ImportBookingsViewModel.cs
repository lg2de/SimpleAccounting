// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Input;
    using Caliburn.Micro;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Infrastructure;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Properties;

    internal class ImportBookingsViewModel : Screen
    {
        private readonly List<AccountDefinition> accounts;
        private readonly IMessageBox messageBox;
        private readonly ProjectData projectData;
        private bool isBusy;
        private ulong selectedAccountNumber;
        private DateTime startDate;

        public ImportBookingsViewModel(
            IMessageBox messageBox,
            ProjectData projectData)
        {
            this.messageBox = messageBox;
            this.projectData = projectData;
            this.accounts = projectData.Storage.AllAccounts.ToList();
            this.FirstBookingNumber = projectData.MaxBookIdent + 1;

            this.RangeMin = this.projectData.CurrentYear.DateStart.ToDateTime();
            this.RangeMax = this.projectData.CurrentYear.DateEnd.ToDateTime();
            this.StartDate = this.RangeMin;

            // ReSharper disable once VirtualMemberCallInConstructor
            this.DisplayName = Resources.ImportData_Title;
        }

        public IEnumerable<AccountDefinition> ImportAccounts => this.accounts
            .Where(
                a =>
                    a.ImportMapping?.Columns.Any(x => x.Target == AccountDefinitionImportMappingColumnTarget.Date) ==
                    true
                    && a.ImportMapping?.Columns.Any(
                        x => x.Target == AccountDefinitionImportMappingColumnTarget.Value) ==
                    true);

        public DateTime RangeMin { get; }

        public DateTime RangeMax { get; }

        private ulong FirstBookingNumber { get; }

        public ulong SelectedAccountNumber
        {
            get => this.selectedAccountNumber;
            set
            {
                if (this.selectedAccountNumber == value)
                {
                    return;
                }

                this.selectedAccountNumber = value;
                this.NotifyOfPropertyChange();

                this.SetupExisting();
                var last = this.ExistingData.LastOrDefault();
                if (last != null)
                {
                    this.StartDate = last.Date + TimeSpan.FromDays(1);
                    this.NotifyOfPropertyChange(nameof(this.StartDate));
                }
            }
        }

        public AccountDefinition? SelectedAccount { get; set; }

        public DateTime StartDate
        {
            get => this.startDate;
            set
            {
                if (value.Equals(this.startDate))
                {
                    return;
                }

                this.startDate = value;
                this.UpdateIdentifierInLoadedData();
                this.NotifyOfPropertyChange();
                this.NotifyOfPropertyChange(nameof(this.ImportDataFiltered));
            }
        }

        public List<ImportEntryViewModel> LoadedData { get; } = new List<ImportEntryViewModel>();

        public List<ImportEntryViewModel> ExistingData { get; } = new List<ImportEntryViewModel>();

        public ObservableCollection<ImportEntryViewModel> ImportDataFiltered =>
            new ObservableCollection<ImportEntryViewModel>(
                this.LoadedData
                    .Concat(this.ExistingData)
                    .Where(x => x.Date >= this.StartDate)
                    .OrderBy(x => x.Date));

        public bool IsForceEnglish { get; set; }

        public bool IsBusy
        {
            get => this.isBusy;
            private set
            {
                if (value == this.isBusy)
                {
                    return;
                }

                this.isBusy = value;
                this.NotifyOfPropertyChange();
            }
        }

        public ICommand LoadDataCommand => new RelayCommand(
            _ =>
            {
                this.IsBusy = true;

                using var openFileDialog = new System.Windows.Forms.OpenFileDialog
                {
                    Filter = Resources.FileFilter_ImportData, RestoreDirectory = true
                };

                if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                this.OnLoadData(openFileDialog.FileName);

                this.IsBusy = false;
            }, _ => this.SelectedAccount != null);

        public ICommand BookAllCommand => new RelayCommand(
            _ => this.ProcessData(),
            _ => this.LoadedData.All(x => x.RemoteAccount != null || x.IsSkip || x.IsExisting));

        public ICommand BookMappedCommand => new RelayCommand(
            _ => this.ProcessData(),
            _ => this.LoadedData.Any(x => x.RemoteAccount != null));

        protected void UpdateIdentifierInLoadedData()
        {
            var bookingNumber = this.FirstBookingNumber;
            foreach (var entry in this.ImportDataFiltered.Where(x => !x.IsExisting))
            {
                entry.Identifier = bookingNumber++;
            }
        }

        protected void SetupExisting()
        {
            this.ExistingData.Clear();
            this.ExistingData.AddRange(
                this.projectData.CurrentYear.Booking
                    .Where(
                        x => x.Credit.Any(c => c.Account == this.selectedAccountNumber)
                             || x.Debit.Any(d => d.Account == this.selectedAccountNumber))
                    .Select(ToViewModel));
            this.NotifyOfPropertyChange(nameof(this.ImportDataFiltered));

            ImportEntryViewModel ToViewModel(AccountingDataJournalBooking entry)
            {
                var value = entry.Credit.Sum(x => x.Value).ToViewModel();
                var me = entry.Debit.First();
                var remotes = entry.Credit;
                if (entry.Credit.Any(c => c.Account == this.selectedAccountNumber))
                {
                    me = entry.Credit.First();
                    remotes = entry.Debit;
                    value = -value;
                }

                ulong remoteIdentifier = 0;
                if (remotes.Count == 1)
                {
                    remoteIdentifier = remotes.First().Account;
                }

                return new ImportEntryViewModel(this.accounts)
                {
                    IsExisting = true,
                    Identifier = entry.ID,
                    Date = entry.Date.ToDateTime(),
                    Text = me.Text,
                    Name = Resources.ImportData_AlreadyBooked,
                    Value = value,
                    RemoteAccount = this.accounts.FirstOrDefault(x => x.ID == remoteIdentifier)
                };
            }
        }

        [SuppressMessage(
            "Minor Code Smell", "S2221:\"Exception\" should not be caught when not required by called methods",
            Justification = "Exception while processing external file must not cause crash at all")]
        internal void OnLoadData(string fileName)
        {
            try
            {
                this.LoadedData.Clear();

                // TODO use all culture configuration
                var cultureInfo = CultureInfo.CurrentUICulture;
                if (this.IsForceEnglish)
                {
                    cultureInfo = new CultureInfo("en-us");
                }

                var filteredAccounts = this.accounts.Where(
                    x => x.ID != this.selectedAccountNumber && x.Type != AccountDefinitionType.Carryforward).ToList();
                using var loader = new ImportFileLoader(
                    fileName, cultureInfo, filteredAccounts, this.SelectedAccount!.ImportMapping);

                foreach (var item in loader.Load())
                {
                    if (item.Date < this.RangeMin || item.Date > this.RangeMax)
                    {
                        // ignore data outside booking year
                        continue;
                    }

                    if (this.ExistingData.Any(
                        x =>
                            x.Date == item.Date
                            && Math.Abs(x.Value - item.Value) < double.Epsilon
                            && x.Text == item.BuildText()))
                    {
                        // ignore already existing
                        continue;
                    }

                    this.LoadedData.Add(item);
                }

                if (!this.LoadedData.Any())
                {
                    this.messageBox.Show(
                        string.Format(
                            CultureInfo.CurrentUICulture, Resources.ImportData_NoRelevantDataFoundInX, fileName),
                        Resources.ImportData_MessageTitle);
                }

                this.UpdateIdentifierInLoadedData();
                this.NotifyOfPropertyChange(nameof(this.ImportDataFiltered));
            }
            catch (Exception e)
            {
                string message = string.Format(
                    CultureInfo.CurrentUICulture, Resources.Information_FailedToLoadX, fileName) + "\n" + e.Message;
                this.messageBox.Show(message, Resources.ImportData_MessageTitle);
            }
        }

        internal void ProcessData()
        {
            foreach (var importing in this.ImportDataFiltered)
            {
                if (importing.IsSkip)
                {
                    // ignore
                    continue;
                }

                if (importing.RemoteAccount == null)
                {
                    // mapping missing - abort
                    break;
                }

                var newBooking = new AccountingDataJournalBooking
                {
                    Date = importing.Date.ToAccountingDate(),
                    ID = importing.Identifier,
                    Followup = importing.IsFollowup
                };
                var creditValue = new BookingValue
                {
                    Text = importing.BuildText(), Value = Math.Abs(importing.Value.ToModelValue())
                };

                // start debit with clone of credit
                var debitValue = creditValue.Clone();

                // set accounts according to the value
                if (importing.Value > 0)
                {
                    creditValue.Account = importing.RemoteAccount.ID;
                    debitValue.Account = this.SelectedAccountNumber;
                }
                else
                {
                    creditValue.Account = this.SelectedAccountNumber;
                    debitValue.Account = importing.RemoteAccount.ID;
                }

                newBooking.Credit = new List<BookingValue> { creditValue };
                newBooking.Debit = new List<BookingValue> { debitValue };
                this.projectData.AddBooking(newBooking);
            }

            this.TryClose();
        }
    }
}
