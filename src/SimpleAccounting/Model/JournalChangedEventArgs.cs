// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Model
{
    using System;
    using System.Collections.Generic;

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
