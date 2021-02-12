// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Caliburn.Micro;
    using JetBrains.Annotations;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Properties;

    public class AccountJournalViewModel : Screen
    {
        private AccountJournalItemViewModel? selectedItem;

        public ObservableCollection<AccountJournalItemViewModel> Items { get; }
            = new ObservableCollection<AccountJournalItemViewModel>();

        public AccountJournalItemViewModel? SelectedItem
        {
            get => this.selectedItem;
            set
            {
                this.selectedItem = value;
                this.NotifyOfPropertyChange();
            }
        }
        
        public void Refresh(
            IReadOnlyCollection<AccountingDataJournalBooking>? bookings,
            [NotNull] AccountingData accountingData,
            ulong accountNumber)
        {
            if (accountingData == null)
            {
                throw new ArgumentNullException(nameof(accountingData));
            }

            this.Items.Clear();
            if (bookings == null)
            {
                return;
            }

            double creditSum = 0;
            double debitSum = 0;
            var relevantBookings = bookings
                .Where(b => b.Credit.Any(x => x.Account == accountNumber))
                .Concat(bookings.Where(b => b.Debit.Any(x => x.Account == accountNumber)))
                .OrderBy(x => x.Date).ThenBy(x => x.ID);
            foreach (var booking in relevantBookings)
            {
                var debitEntries = booking.Debit.Where(x => x.Account == accountNumber);
                foreach (var debitEntry in debitEntries)
                {
                    var item = new AccountJournalItemViewModel
                    {
                        Identifier = booking.ID,
                        Date = booking.Date.ToDateTime(),
                        Text = debitEntry.Text,
                        DebitValue = debitEntry.Value.ToViewModel()
                    };

                    debitSum += item.DebitValue;
                    item.RemoteAccount = booking.Credit.Count == 1
                        ? accountingData.GetAccountName(booking.Credit.Single())
                        : Resources.Word_Various;
                    this.Items.Add(item);
                }

                var creditEntries = booking.Credit.Where(x => x.Account == accountNumber);
                foreach (var creditEntry in creditEntries)
                {
                    var item = new AccountJournalItemViewModel
                    {
                        Identifier = booking.ID,
                        Date = booking.Date.ToDateTime(),
                        Text = creditEntry.Text,
                        CreditValue = creditEntry.Value.ToViewModel()
                    };

                    creditSum += item.CreditValue;
                    item.RemoteAccount = booking.Debit.Count == 1
                        ? accountingData.GetAccountName(booking.Debit.Single())
                        : Resources.Word_Various;
                    this.Items.Add(item);
                }
            }

            this.Items.UpdateRowHighlighting();

            if (debitSum + creditSum < double.Epsilon)
            {
                // no summary required
                return;
            }

            var sumItem = new AccountJournalItemViewModel();
            this.Items.Add(sumItem);
            sumItem.IsSummary = true;
            sumItem.Text = Resources.Word_Total;
            sumItem.DebitValue = debitSum;
            sumItem.CreditValue = creditSum;

            var saldoItem = new AccountJournalItemViewModel();
            this.Items.Add(saldoItem);
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
    }
}
