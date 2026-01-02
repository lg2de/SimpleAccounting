// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using lg2de.SimpleAccounting.Model;

/// <summary>
///     Implements the design view model for <see cref="CloseYearViewModel"/>.
/// </summary>
[SuppressMessage(
    "Major Code Smell", "S109:Magic numbers should not be used",
    Justification = "Design view model defines useful values")]
[SuppressMessage(
    "Blocker Code Smell", "S4462:Calls to \"async\" methods should not be blocking",
    Justification = "In the designer we need to complete immediately.")]
internal sealed class CloseYearDesignViewModel : CloseYearViewModel
{
    public CloseYearDesignViewModel()
        : base(new AccountingDataJournal { Year = "2020" })
    {
        this.Accounts.Add(new AccountDefinition { ID = 990, Name = "My CarryForward" });
        this.OnInitializedAsync(CancellationToken.None).Wait();
    }
}
