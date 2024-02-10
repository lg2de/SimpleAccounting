// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Abstractions;

using System;
using System.Diagnostics.CodeAnalysis;

/// <summary>
///     Default implementation of <see cref="IClock"/> using real time.
/// </summary>
[SuppressMessage(
    "Major Code Smell", "S6354:Use a testable date/time provider",
    Justification = "This is the one and only implementation according pattern.")]
internal class SystemClock : IClock
{
    public DateTime Now()
    {
        return DateTime.Now;
    }
}
