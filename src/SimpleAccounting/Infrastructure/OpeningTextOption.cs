// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Infrastructure;

/// <summary>
///     Defines the options to format the opening text.
/// </summary>
public enum OpeningTextOption
{
    /// <summary>
    ///     Use simple increasing number for all opening bookings.
    /// </summary>
    Numbered,

    /// <summary>
    ///     Build text from localized template and the name of the account.
    /// </summary>
    AccountName
}
