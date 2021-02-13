// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Model
{
    /// <summary>
    ///     Implements the storage for all data of the current project.
    /// </summary>
    /// <remarks>
    ///     It contains the persistent data according to <see cref="AccountingData"/>
    ///     as well as the current state of the project.
    /// </remarks>
    // TODO remove all ? if possible.
    public class ProjectData
    {
        public AccountingData? All { get; set; }

        public AccountingDataJournal? CurrentYear { get; set; }

        public bool IsModified { get; set; }

        public void AddBooking(AccountingDataJournalBooking booking)
        {
            this.CurrentYear!.Booking.Add(booking);
            this.IsModified = true;
        }
    }
}
