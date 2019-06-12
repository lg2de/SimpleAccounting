// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting
{
    internal class AddBookingDesignViewModel : AddBookingViewModel
    {
        public AddBookingDesignViewModel()
            : base(null)
        {
            this.BookingNumber = 42;
            this.BookingText = "shoes";
            this.BookingValue = 169.95;

            this.Accounts.Add(new AccountingDataAccount { ID = 100, Name = "Cash" });
            this.Accounts.Add(new AccountingDataAccount { ID = 600, Name = "Shopping" });

            this.DebitAccount = 100;
            this.CreditAccount = 600;
        }
    }
}
