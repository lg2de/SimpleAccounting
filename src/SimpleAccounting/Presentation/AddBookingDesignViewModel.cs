// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using lg2de.SimpleAccounting.Model;

    [SuppressMessage(
        "Major Code Smell", "S109:Magic numbers should not be used",
        Justification = "Design view model defines useful values")]
    [SuppressMessage(
        "Major Code Smell", "S4055:Literals should not be passed as localized parameters",
        Justification = "Design view model defines useful values")]
    internal class AddBookingDesignViewModel : AddBookingViewModel
    {
        public AddBookingDesignViewModel()
            : base(null!, DateTime.Now, DateTime.Now)
        {
            this.BookingNumber = 42;
            this.BookingText = "shoes";
            this.BookingValue = 169.95;

            this.Accounts.Add(new AccountDefinition { ID = 100, Name = "Cash", Type = AccountDefinitionType.Asset });
            this.Accounts.Add(
                new AccountDefinition { ID = 600, Name = "Shopping", Type = AccountDefinitionType.Expense });

            this.DebitAccount = 600;
            this.CreditAccount = 100;
        }
    }
}
