// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Model
{
    using System;
    using System.Linq;
    using Caliburn.Micro;
    using JetBrains.Annotations;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Presentation;

    /// <summary>
    ///     Implements the storage for all data of the current project.
    /// </summary>
    /// <remarks>
    ///     It contains the persistent data according to <see cref="AccountingData"/>
    ///     as well as the current state of the project.
    /// </remarks>
    internal class ProjectData
    {
        private readonly IWindowManager windowManager;
        private readonly IMessageBox messageBox;
        private AccountingData all;

        public ProjectData(IWindowManager windowManager, IMessageBox messageBox)
        {
            this.windowManager = windowManager;
            this.messageBox = messageBox;

            this.all = new AccountingData();
            this.CurrentYear = this.all.Journal.SafeGetLatest();
        }

        public event EventHandler<JournalChangedEventArgs> JournalChanged = (_, __) => { };

        public string FileName { get; set; } = string.Empty;

        public AccountingData All
        {
            get => this.all;
            set
            {
                this.all = value;
                this.CurrentYear = this.All.Journal.SafeGetLatest();
            }
        }

        public AccountingDataJournal CurrentYear { get; set; }

        public bool IsModified { get; set; }

        internal ulong MaxBookIdent => !this.CurrentYear.Booking.Any() ? 0 : this.CurrentYear.Booking.Max(b => b.ID);

        public void AddBooking(AccountingDataJournalBooking booking)
        {
            this.CurrentYear.Booking.Add(booking);

            this.IsModified = true;

            this.JournalChanged(this, new JournalChangedEventArgs(booking.ID, booking.GetAccounts()));
        }

        public void ShowAddBookingDialog(bool showInactiveAccounts)
        {
            var bookingModel = new EditBookingViewModel(
                this,
                DateTime.Today,
                this.CurrentYear.DateStart.ToDateTime(),
                this.CurrentYear.DateEnd.ToDateTime(),
                editMode: false) { BookingIdentifier = this.MaxBookIdent + 1 };
            var allAccounts = this.All.AllAccounts;
            bookingModel.Accounts.AddRange(showInactiveAccounts ? allAccounts : allAccounts.Where(x => x.Active));

            this.All.Setup?.BookingTemplates?.Template
                .Select(
                    t => new BookingTemplate
                    {
                        Text = t.Text, Credit = t.Credit, Debit = t.Debit, Value = t.Value.ToViewModel()
                    })
                .ToList().ForEach(bookingModel.BindingTemplates.Add);

            this.windowManager.ShowDialog(bookingModel);
        }

        public void ShowEditBookingDialog(ulong bookingId, bool showInactiveAccounts)
        {
            var journalIndex =
                this.CurrentYear.Booking.FindIndex(x => { return x.ID == bookingId; });
            if (journalIndex < 0)
            {
                // summary item selected => ignore
                return;
            }

            var journalEntry = this.CurrentYear.Booking[journalIndex];

            var bookingModel = new EditBookingViewModel(
                this,
                journalEntry.Date.ToDateTime(),
                this.CurrentYear.DateStart.ToDateTime(),
                this.CurrentYear.DateEnd.ToDateTime(),
                editMode: true)
            {
                BookingIdentifier = journalEntry.ID,
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
                bookingModel.BookingText = theDebit.Text;
            }

            var allAccounts = this.All.AllAccounts;
            bookingModel.Accounts.AddRange(showInactiveAccounts ? allAccounts : allAccounts.Where(x => x.Active));

            var result = this.windowManager.ShowDialog(bookingModel);
            if (result != true)
            {
                return;
            }

            // replace entry
            journalEntry = bookingModel.CreateJournalEntry();
            this.CurrentYear.Booking[journalIndex] = journalEntry;

            this.IsModified = true;

            this.JournalChanged(this, new JournalChangedEventArgs(journalEntry.ID, journalEntry.GetAccounts()));
        }

        public void ShowImportDialog()
        {
            var importModel = new ImportBookingsViewModel(this.messageBox, this);
            this.windowManager.ShowDialog(importModel);
        }
    }
}
