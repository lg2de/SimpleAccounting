// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Abstractions;

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Forms;
using MessageBoxOptions = System.Windows.MessageBoxOptions;

/// <summary>
///     Defines access to a message box.
/// </summary>
public interface IDialogs
{
    [ExcludeFromCodeCoverage]
    [SuppressMessage(
        "Critical Code Smell", "S2360:Optional parameters should not be used",
        Justification = "Signature follows wrapped framework API")]
    MessageBoxResult ShowMessageBox(
        string messageBoxText,
        string caption,
        MessageBoxButton button = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.None,
        MessageBoxResult defaultResult = MessageBoxResult.None,
        MessageBoxOptions options = MessageBoxOptions.None);

    (DialogResult Result, string FileName) ShowOpenFileDialog(string filter, string initialDirectory);

    (DialogResult Result, string FileName) ShowSaveFileDialog(string filter);
}
