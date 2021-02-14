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

            this.all = new AccountingData().Init();
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

        internal ulong MaxBookIdent
        {
            get
            {
                if (this.CurrentYear?.Booking == null || !this.CurrentYear.Booking.Any())
                {
                    return 0;
                }

                return this.CurrentYear.Booking.Max(b => b.ID);
            }
        }

        public void AddBooking(AccountingDataJournalBooking booking)
        {
            this.CurrentYear.Booking.Add(booking);

            this.IsModified = true;

            var affectedAccounts = booking
                .Credit.Select(x => x.Account)
                .Concat(booking.Debit.Select(x => x.Account))
                .Distinct();
            this.JournalChanged?.Invoke(this, new JournalChangedEventArgs(booking.ID, affectedAccounts.ToList()));
        }

        public void ShowImportDialog()
        {
            var importModel = new ImportBookingsViewModel(this.messageBox, this);
            this.windowManager.ShowDialog(importModel);
        }

        // TODO temporary index to be removed
        internal void UpdateBooking(int temporaryIndex, AccountingDataJournalBooking booking)
        {
            //var index = this.CurrentYear.Booking.FindIndex(x => x.ID == booking.ID)
            this.CurrentYear.Booking[temporaryIndex] = booking;

            this.IsModified = true;

            var affectedAccounts = booking
                .Credit.Select(x => x.Account)
                .Concat(booking.Debit.Select(x => x.Account))
                .Distinct();
            this.JournalChanged?.Invoke(this, new JournalChangedEventArgs(booking.ID, affectedAccounts.ToList()));
        }
    }
}
