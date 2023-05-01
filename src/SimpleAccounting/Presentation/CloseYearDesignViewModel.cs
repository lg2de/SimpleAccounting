// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System.Diagnostics.CodeAnalysis;
using lg2de.SimpleAccounting.Model;

/// <summary>
///     Implements the design view model for <see cref="CloseYearViewModel"/>.
/// </summary>
[SuppressMessage(
    "Major Code Smell", "S109:Magic numbers should not be used",
    Justification = "Design view model defines useful values")]
[SuppressMessage(
    "Major Code Smell", "S4055:Literals should not be passed as localized parameters",
    Justification = "Design view model defines useful values")]
internal sealed class CloseYearDesignViewModel : CloseYearViewModel
{
    public CloseYearDesignViewModel()
        : base(new AccountingDataJournal { Year = "2020" })
    {
        this.Accounts.Add(new AccountDefinition { ID = 990, Name = "My CarryForward" });
        this.OnInitialize();
    }
}
