// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Abstractions;

using System.Diagnostics.CodeAnalysis;
using System.Windows;

/// <summary>
///     Implements <see cref="IClipboard"/> using <see cref="Clipboard"/>.
/// </summary>
[ExcludeFromCodeCoverage(
    Justification = "The abstraction is for unit testing only. This is the simple implementation.")]
internal class WindowsClipboard : IClipboard
{
    public void SetText(string text)
    {
        Clipboard.SetText(text);
    }
}
