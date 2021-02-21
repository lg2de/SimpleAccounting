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

    /// <summary>
    ///     Implements the view model for the full journal.
    /// </summary>
    internal class FullJournalViewModel : Screen, IFullJournalViewModel
    {
        private readonly IProjectData projectData;
        private FullJournalItemViewModel? selectedItem;

        public FullJournalViewModel(IProjectData projectData)
        {
            this.projectData = projectData;
        }

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

        public void Rebuild()
        {
            this.Items.Clear();

            foreach (var booking in this.projectData.CurrentYear.Booking.OrderBy(b => b.Date))
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
                    item.DebitAccount = this.projectData.Storage.GetAccountName(debit);
                    item.CreditAccount = this.projectData.Storage.GetAccountName(creditAccounts[0]);
                    this.Items.Add(item);
                    continue;
                }

                foreach (var debitEntry in debitAccounts)
                {
                    var debitItem = item.Clone();
                    debitItem.Text = debitEntry.Text;
                    debitItem.Value = debitEntry.Value.ToViewModel();
                    debitItem.DebitAccount = this.projectData.Storage.GetAccountName(debitEntry);
                    this.Items.Add(debitItem);
                }

                foreach (var creditEntry in creditAccounts)
                {
                    var creditItem = item.Clone();
                    creditItem.Text = creditEntry.Text;
                    creditItem.Value = creditEntry.Value.ToViewModel();
                    creditItem.CreditAccount = this.projectData.Storage.GetAccountName(creditEntry);
                    this.Items.Add(creditItem);
                }
            }

            this.Items.UpdateRowHighlighting();
        }

        public void Select(ulong bookingId)
        {
            this.SelectedItem = this.Items.FirstOrDefault(x => x.Identifier == bookingId);
        }
    }
}
