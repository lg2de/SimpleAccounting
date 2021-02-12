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

    public class FullJournalViewModel : Screen
    {
        private FullJournalItemViewModel? selectedItem;

        public ObservableCollection<FullJournalItemViewModel> Items { get; }
            = new ObservableCollection<FullJournalItemViewModel>();

        public FullJournalItemViewModel? SelectedItem
        {
            get => this.selectedItem;
            set
            {
                this.selectedItem = value;
                this.NotifyOfPropertyChange();
            }
        }

        public void Refresh(IEnumerable<AccountingDataJournalBooking>? bookings, [NotNull] AccountingData accountingData)
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

            foreach (var booking in bookings.OrderBy(b => b.Date))
            {
                var item = new FullJournalItemViewModel
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
                    item.DebitAccount = accountingData.GetAccountName(debit);
                    item.CreditAccount = accountingData.GetAccountName(creditAccounts[0]);
                    this.Items.Add(item);
                    continue;
                }

                foreach (var debitEntry in debitAccounts)
                {
                    var debitItem = item.Clone();
                    debitItem.Text = debitEntry.Text;
                    debitItem.Value = debitEntry.Value.ToViewModel();
                    debitItem.DebitAccount = accountingData.GetAccountName(debitEntry);
                    this.Items.Add(debitItem);
                }

                foreach (var creditEntry in creditAccounts)
                {
                    var creditItem = item.Clone();
                    creditItem.Text = creditEntry.Text;
                    creditItem.Value = creditEntry.Value.ToViewModel();
                    creditItem.CreditAccount = accountingData.GetAccountName(creditEntry);
                    this.Items.Add(creditItem);
                }
            }

            this.Items.UpdateRowHighlighting();
        }
    }
}
