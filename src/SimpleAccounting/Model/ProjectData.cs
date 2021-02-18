// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Model
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Forms;
    using Caliburn.Micro;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Infrastructure;
    using lg2de.SimpleAccounting.Presentation;
    using lg2de.SimpleAccounting.Properties;

    /// <summary>
    ///     Implements the storage for all data of the current project.
    /// </summary>
    /// <remarks>
    ///     It contains the persistent data according to <see cref="AccountingData" />
    ///     as well as the current state of the project.
    /// </remarks>
    internal class ProjectData
    {
        private readonly IWindowManager windowManager;
        private readonly IMessageBox messageBox;
        private readonly IFileSystem fileSystem;
        private readonly IProcess processApi;
        private AccountingData storage;

        public ProjectData(IWindowManager windowManager, IMessageBox messageBox, IFileSystem fileSystem, IProcess processApi)
        {
            this.windowManager = windowManager;
            this.messageBox = messageBox;
            this.fileSystem = fileSystem;
            this.processApi = processApi;

            this.storage = new AccountingData();
            this.CurrentYear = this.storage.Journal.SafeGetLatest();
        }

        public string FileName { get; set; } = string.Empty;

        public AccountingData Storage
        {
            get => this.storage;
            private set
            {
                this.storage = value;
                this.CurrentYear = this.Storage.Journal.SafeGetLatest();
                Execute.OnUIThread(() => this.DataLoaded(this, EventArgs.Empty));
            }
        }

        public AccountingDataJournal CurrentYear { get; set; }

        public bool IsModified { get; set; }

        internal string AutoSaveFileName => Defines.GetAutoSaveFileName(this.FileName);
        
        internal ulong MaxBookIdent => !this.CurrentYear.Booking.Any() ? 0 : this.CurrentYear.Booking.Max(b => b.ID);

        public event EventHandler<JournalChangedEventArgs> JournalChanged = (_, __) => { };

        public event EventHandler DataLoaded = (_, __) => { };

        public void Load(AccountingData accountingData)
        {
            this.Storage = accountingData;
        }
        
        internal async Task<OperationResult> LoadFromFileAsync(string projectFileName, Settings settings)
        {
            if (!this.CheckSaveProject())
            {
                return OperationResult.Aborted;
            }

            this.IsModified = false;

            var loader = new ProjectFileLoader(this.messageBox, this.fileSystem, this.processApi, settings);
            var loadResult = await Task.Run(() => loader.LoadAsync(projectFileName));
            if (loadResult != OperationResult.Completed)
            {
                return loadResult;
            }

            this.FileName = projectFileName;
            this.Storage = loader.ProjectData;
            this.IsModified = loader.Migrated;

            return OperationResult.Completed;
        }
        
        
        internal bool CheckSaveProject()
        {
            if (!this.IsModified)
            {
                // no need to save the project
                return true;
            }

            var result = this.messageBox.Show(
                Resources.Question_SaveBeforeProceed,
                Resources.Header_Shutdown,
                MessageBoxButton.YesNoCancel);
            if (result == MessageBoxResult.Cancel)
            {
                return false;
            }

            if (result == MessageBoxResult.Yes)
            {
                this.SaveProject();
                return true;
            }

            // TODO Not saving but continue cannot work correctly this way!?
            return result == MessageBoxResult.No;
        }
        
        internal void SaveProject()
        {
            if (this.FileName == "<new>")
            {
                using var saveFileDialog = new SaveFileDialog
                {
                    Filter = Resources.FileFilter_MainProject, RestoreDirectory = true
                };

                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                this.FileName = saveFileDialog.FileName;
            }

            DateTime fileDate = this.fileSystem.GetLastWriteTime(this.FileName);
            string backupFileName = $"{this.FileName}.{fileDate:yyyyMMddHHmmss}";
            if (this.fileSystem.FileExists(this.FileName))
            {
                this.fileSystem.FileMove(this.FileName, backupFileName);
            }

            this.fileSystem.WriteAllTextIntoFile(this.FileName, this.Storage.Serialize());
            this.IsModified = false;

            if (this.fileSystem.FileExists(this.AutoSaveFileName))
            {
                this.fileSystem.FileDelete(this.AutoSaveFileName);
            }
        }
        
        public void AddBooking(AccountingDataJournalBooking booking)
        {
            this.CurrentYear.Booking.Add(booking);

            this.IsModified = true;

            this.JournalChanged(this, new JournalChangedEventArgs(booking.ID, booking.GetAccounts()));
        }

        public void ShowAddBookingDialog(bool showInactiveAccounts)
        {
            var bookingModel =
                new EditBookingViewModel(this, DateTime.Today, editMode: false);
            var allAccounts = this.Storage.AllAccounts;
            bookingModel.Accounts.AddRange(showInactiveAccounts ? allAccounts : allAccounts.Where(x => x.Active));

            this.Storage.Setup?.BookingTemplates?.Template
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
            var journalIndex = this.CurrentYear.Booking.FindIndex(x => x.ID == bookingId);
            if (journalIndex < 0)
            {
                // summary item selected => ignore
                return;
            }

            var journalEntry = this.CurrentYear.Booking[journalIndex];

            var bookingModel = new EditBookingViewModel(
                this,
                journalEntry.Date.ToDateTime(),
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

            var allAccounts = this.Storage.AllAccounts;
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

        public void TriggerJournalChanged()
        {
            this.JournalChanged(
                this, new JournalChangedEventArgs(0, this.storage.AllAccounts.Select(x => x.ID).ToList()));
        }
    }
}
