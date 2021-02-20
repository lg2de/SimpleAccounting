// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Model
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using lg2de.SimpleAccounting.Infrastructure;

    internal interface IProjectData
    {
        string FileName { get; set; }

        AccountingData Storage { get; }
        
        AccountingDataJournal CurrentYear { get; }
        
        bool IsModified { get; set; } // TODO setter should be internal
        
        ulong MaxBookIdent { get; }
        
        TimeSpan AutoSaveInterval { get; set; }
        
        string AutoSaveFileName { get; }

        event EventHandler DataLoaded;

        event EventHandler YearChanged; 

        event EventHandler<JournalChangedEventArgs> JournalChanged;

        void NewProject();
        
        void Load(AccountingData accountingData);
        
        Task<OperationResult> LoadFromFileAsync(string projectFileName);
        
        void SaveProject();
        
        Task AutoSaveAsync(CancellationToken cancellationToken);
        
        void RemoveAutoSaveFile();
        
        void ShowAddBookingDialog(bool showInactiveAccounts);
        
        void ShowEditBookingDialog(ulong bookingId, bool showInactiveAccounts);
        
        void ShowImportDialog();
        
        bool CloseYear();
        
        bool CheckSaveProject();
        
        void TriggerJournalChanged();
        
        void AddBooking(AccountingDataJournalBooking booking);
        
        void SelectYear(string yearName);
    }
}
