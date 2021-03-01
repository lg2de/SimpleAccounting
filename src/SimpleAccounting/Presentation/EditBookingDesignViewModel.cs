// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Properties;

    /// <summary>
    ///     Implements the view model for booking editor to be used in the designer.
    /// </summary>
    [SuppressMessage(
        "Major Code Smell", "S109:Magic numbers should not be used",
        Justification = "Design view model defines useful values")]
    [SuppressMessage(
        "Major Code Smell", "S4055:Literals should not be passed as localized parameters",
        Justification = "Design view model defines useful values")]
    internal class EditBookingDesignViewModel : EditBookingViewModel
    {
        public EditBookingDesignViewModel()
            : base(new ProjectData(new Settings(), null!, null!, null!, null!), DateTime.Now, editMode: false)
        {
            this.BookingIdentifier = 42;
            this.BookingText = "shoes";
            this.BookingValue = 169.95;

            this.Accounts.Add(new AccountDefinition { ID = 100, Name = "Cash", Type = AccountDefinitionType.Asset });
            this.Accounts.Add(
                new AccountDefinition { ID = 600, Name = "Shopping", Type = AccountDefinitionType.Expense });

            this.DebitSplitEntries.Add(new SplitBookingViewModel { AccountNumber = 600, BookingText = "Booking1" });
            this.DebitSplitEntries.Add(new SplitBookingViewModel { AccountNumber = 600, BookingText = "Booking2" });
            this.CreditAccount = 100;
        }
    }
}
