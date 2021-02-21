// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System.Collections.ObjectModel;
    using System.Linq;
    using Caliburn.Micro;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Properties;

    /// <summary>
    ///     Implements the view model for the account journal.
    /// </summary>
    internal class AccountJournalViewModel : Screen, IAccountJournalViewModel
    {
        private readonly IProjectData projectData;
        private AccountJournalItemViewModel? selectedItem;

        public AccountJournalViewModel(IProjectData projectData)
        {
            this.projectData = projectData;
        }

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

        public void Rebuild(ulong accountNumber)
        {
            this.Items.Clear();

            var relevantBookings =
                this.projectData.CurrentYear.Booking
                    .Where(b => b.Credit.Any(x => x.Account == accountNumber))
                    .Concat(
                        this.projectData.CurrentYear.Booking.Where(b => b.Debit.Any(x => x.Account == accountNumber)))
                    .OrderBy(x => x.Date).ThenBy(x => x.ID);
            double creditSum = 0;
            double debitSum = 0;
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
                        ? this.projectData.Storage.GetAccountName(booking.Credit.Single())
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
                        ? this.projectData.Storage.GetAccountName(booking.Debit.Single())
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

            var balanceItem = new AccountJournalItemViewModel();
            this.Items.Add(balanceItem);
            balanceItem.IsSummary = true;
            balanceItem.Text = Resources.Word_Balance;
            if (debitSum > creditSum)
            {
                balanceItem.DebitValue = debitSum - creditSum;
            }
            else
            {
                balanceItem.CreditValue = creditSum - debitSum;
            }
        }

        public void Select(ulong bookingId)
        {
            this.SelectedItem = this.Items.FirstOrDefault(x => x.Identifier == bookingId);
        }
    }
}
