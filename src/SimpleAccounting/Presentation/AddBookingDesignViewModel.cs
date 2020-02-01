// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using lg2de.SimpleAccounting.Model;

    internal class AddBookingDesignViewModel : AddBookingViewModel
    {
        public AddBookingDesignViewModel()
            : base(null, DateTime.Now, DateTime.Now)
        {
            this.BookingNumber = 42;
            this.BookingText = "shoes";
            this.BookingValue = 169.95;

            this.Accounts.Add(new AccountDefinition { ID = 100, Name = "Cash", Type = AccountDefinitionType.Asset });
            this.Accounts.Add(new AccountDefinition { ID = 600, Name = "Shopping", Type = AccountDefinitionType.Expense });

            this.DebitAccount = 600;
            this.CreditAccount = 100;
        }
    }
}
