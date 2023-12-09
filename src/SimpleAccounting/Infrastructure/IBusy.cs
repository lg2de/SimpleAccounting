// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Infrastructure;

/// <summary>
///     Defines abstraction for a busy indicator in view models.
/// </summary>
public interface IBusy
{
    bool IsBusy { get; set; }
}
