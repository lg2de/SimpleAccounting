// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using Caliburn.Micro;

    /// <summary>
    ///     Implements the view model for one of multiple entries in a split booking.
    /// </summary>
    public class SplitBookingViewModel : Screen
    {
        private ulong accountNumber;
        private string bookingText = string.Empty;

        public ulong AccountNumber
        {
            get => this.accountNumber;
            set
            {
                if (this.accountNumber == value)
                {
                    return;
                }

                this.accountNumber = value;
                this.NotifyOfPropertyChange();
            }
        }

        public int AccountIndex { get; set; } = -1;

        public string BookingText
        {
            get => this.bookingText;
            set
            {
                if (value == this.bookingText)
                {
                    return;
                }

                this.bookingText = value;
                this.NotifyOfPropertyChange();
                this.NotifyOfPropertyChange(nameof(this.IsBookingTextErrorThickness));
            }
        }

        public double BookingValue { get; set; }

        public int IsBookingTextErrorThickness => string.IsNullOrWhiteSpace(this.BookingText) ? 1 : 0;
    }
}
