// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    /// <summary>
    ///     Defines abstraction for a journal item.
    /// </summary>
    public interface IJournalItem
    {
        ulong Identifier { get; }
    }
}
