// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using Caliburn.Micro;

    public class SplitBookingViewModel : Screen
    {
        private ulong accountNumber;

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

        public string BookingText { get; set; } = string.Empty;
    }
}
