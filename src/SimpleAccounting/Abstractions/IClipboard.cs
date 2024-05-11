// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Abstractions;

/// <summary>
///     Defines abstraction for the clipboard.
/// </summary>
public interface IClipboard
{
    void SetText(string text);
}
