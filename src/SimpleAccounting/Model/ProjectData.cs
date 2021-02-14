// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Model
{
    using System.Linq;

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
        public string FileName { get; set; } = string.Empty;

        public AccountingData? All { get; set; }

        public AccountingDataJournal? CurrentYear { get; set; }

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

        internal bool IsCurrentYearOpen
        {
            get
            {
                if (this.CurrentYear == null)
                {
                    return false;
                }

                return !this.CurrentYear.Closed;
            }
        }

        public void AddBooking(AccountingDataJournalBooking booking)
        {
            this.CurrentYear!.Booking.Add(booking);
            this.IsModified = true;
        }
    }
}
