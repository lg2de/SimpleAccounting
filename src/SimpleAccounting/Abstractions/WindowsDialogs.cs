// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Abstractions;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Forms;
using Caliburn.Micro;
using JetBrains.Annotations;
using lg2de.SimpleAccounting.Presentation;
using Application = System.Windows.Application;
using MessageBoxOptions = System.Windows.MessageBoxOptions;

/// <summary>
///     Default implementation of <see cref="IDialogs" /> using <see cref="MessageDialog" />.
/// </summary>
[ExcludeFromCodeCoverage(
    Justification = "The abstraction is for unit testing only. This is the simple implementation.")]
[UsedImplicitly]
internal class WindowsDialogs : IDialogs
{
    public static IDictionary<string, object> SizeToContentManualSettings { get; } =
        new Dictionary<string, object> { { nameof(Window.SizeToContent), SizeToContent.Manual } };

    public MessageBoxResult ShowMessageBox(
        string messageBoxText,
        string caption,
        MessageBoxButton button = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.None,
        MessageBoxResult defaultResult = MessageBoxResult.None,
        MessageBoxOptions options = MessageBoxOptions.None)
    {
        MessageBoxResult result = MessageBoxResult.None;
        Execute.OnUIThread(
            () =>
            {
                Application.Current.MainWindow!.Activate();
                result = MessageDialog.Show(
                    Application.Current.MainWindow,
                    messageBoxText,
                    caption,
                    button,
                    icon,
                    defaultResult);
            });
        return result;
    }

    public (DialogResult Result, string FileName) ShowOpenFileDialog(string filter, string initialDirectory)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = filter, RestoreDirectory = true, InitialDirectory = initialDirectory
        };

        var result = dialog.ShowDialog();
        return (result, dialog.FileName);
    }

    public (DialogResult Result, string FileName) ShowSaveFileDialog(string filter)
    {
        using var dialog = new SaveFileDialog { Filter = filter, RestoreDirectory = true };

        var result = dialog.ShowDialog();
        return (result, dialog.FileName);
    }
}
