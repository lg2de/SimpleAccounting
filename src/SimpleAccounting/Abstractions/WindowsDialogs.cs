// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Abstractions;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Caliburn.Micro;
using JetBrains.Annotations;
using lg2de.SimpleAccounting.Presentation;
using Application = System.Windows.Application;

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
        MessageBoxResult defaultResult = MessageBoxResult.None)
    {
        MessageBoxResult result = MessageBoxResult.None;
        Execute.OnUIThread(() =>
        {
            // the message belongs to the active window, e.g. an open dialog, instead of the main window
            var owner =
                Application.Current.Windows.OfType<Window>()
                    .FirstOrDefault(x => x is { IsActive: true, IsVisible: true })
                ?? Application.Current.MainWindow!;
            owner.Activate();
            result = MessageDialog.Show(
                owner,
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
