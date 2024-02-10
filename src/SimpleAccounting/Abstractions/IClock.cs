// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Abstractions;

using System;

/// <summary>
///     Defines abstraction for getting current date and time.
/// </summary>
/// <remarks>
///     S6354: Use a testable (date) time provider instead.
/// </remarks>
internal interface IClock
{
    DateTime Now();
}
