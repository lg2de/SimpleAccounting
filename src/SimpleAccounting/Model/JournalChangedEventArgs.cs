// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Model
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     Implements the information relevant for the notification of change in the journal.
    /// </summary>
    public class JournalChangedEventArgs : EventArgs
    {
        public JournalChangedEventArgs(ulong changedBookingId, IReadOnlyCollection<ulong> affectedAccounts)
        {
            this.ChangedBookingId = changedBookingId;
            this.AffectedAccounts = affectedAccounts;
        }

        public ulong ChangedBookingId { get; }

        public IReadOnlyCollection<ulong> AffectedAccounts { get; }
    }
}
