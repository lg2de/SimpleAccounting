// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System.Diagnostics.CodeAnalysis;

/// <summary>
///     Implements the view to create new booking entry or edit existing one.
/// </summary>
public partial class EditBookingView
{
    internal const uint DebitCreditPageIndex = 0;
    internal const uint IncomePageIndex = 1;
    internal const uint ExpensePageIndex = 2;

    [ExcludeFromCodeCoverage(Justification = "The view class will not be tested.")]
    public EditBookingView()
    {
        this.InitializeComponent();
    }
}
