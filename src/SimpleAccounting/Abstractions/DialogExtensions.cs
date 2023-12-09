// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Abstractions;

using System.Windows.Forms;

/// <summary>
///     Implements extensions on <see cref="IDialogs"/>.
/// </summary>
internal static class DialogExtensions
{
    public static (DialogResult Result, string FileName) ShowOpenFileDialog(this IDialogs dialogs, string filter)
    {
        return dialogs.ShowOpenFileDialog(filter, string.Empty);
    }
}
