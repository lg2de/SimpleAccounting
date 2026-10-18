// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using AppResources = lg2de.SimpleAccounting.Properties.Resources;

/// <summary>
///     Implements a message box in the style of the application (Fluent theme, light or dark).
///     It replaces <see cref="MessageBox" /> which is always shown in the classic Win32 style.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "This view cannot be tested.")]
public partial class MessageDialog
{
    private const uint SystemCommandClose = 0xF060;
    private const uint MenuItemByCommand = 0x0;
    private const uint MenuItemGrayed = 0x1;

    private const string CopySeparator = "---------------------------";

    private readonly List<MessageBoxResult> results;

    // Like the Win32 message box, the question "Yes/No" can be closed by the buttons only.
    private bool isCloseAllowed;

    private MessageBoxResult result;

    private MessageDialog(
        string messageBoxText, string caption,
        MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
    {
        this.InitializeComponent();

        this.Title = caption;
        this.MessageText.Text = messageBoxText;
        this.SetupIcon(icon);

        this.results = GetResults(button);
        if (!this.results.Contains(defaultResult))
        {
            defaultResult = this.results[0];
        }

        this.isCloseAllowed = button != MessageBoxButton.YesNo;
        this.result = GetCloseResult(button);

        // ESC triggers "Cancel", or "OK" if this is the only button
        this.ButtonsPanel.ItemsSource = this.results
            .Select(x => this.CreateButton(
                x, x == defaultResult, x == MessageBoxResult.Cancel || this.results.Count == 1))
            .ToList();
    }

    /// <summary>
    ///     Shows the message box modal to the owner window.
    /// </summary>
    public static MessageBoxResult Show(
        Window? owner,
        string messageBoxText, string caption,
        MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
    {
        var dialog = new MessageDialog(messageBoxText, caption, button, icon, defaultResult);
        if (owner is { IsVisible: true })
        {
            dialog.Owner = owner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        PlaySound(icon);
        dialog.ShowDialog();
        return dialog.result;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        if (this.isCloseAllowed)
        {
            return;
        }

        // disables the close button of the title bar (and the related entry of the system menu)
        var handle = new WindowInteropHelper(this).Handle;
        _ = EnableMenuItem(
            GetSystemMenu(handle, revert: false), SystemCommandClose, MenuItemByCommand | MenuItemGrayed);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        // prevents closing by Alt+F4
        e.Cancel = !this.isCloseAllowed;
        base.OnClosing(e);
    }

    /// <summary>
    ///     Gets the result of closing the window without button, which has the same meaning as the Win32 message box.
    /// </summary>
    /// <remarks>
    ///     The question "Yes/No" cannot be closed without button.
    /// </remarks>
    private static MessageBoxResult GetCloseResult(MessageBoxButton button)
    {
        return button is MessageBoxButton.OKCancel or MessageBoxButton.YesNoCancel
            ? MessageBoxResult.Cancel
            : MessageBoxResult.OK;
    }

    private static List<MessageBoxResult> GetResults(MessageBoxButton button)
    {
        return button switch
        {
            MessageBoxButton.OKCancel => [MessageBoxResult.OK, MessageBoxResult.Cancel],
            MessageBoxButton.YesNo => [MessageBoxResult.Yes, MessageBoxResult.No],
            MessageBoxButton.YesNoCancel => [MessageBoxResult.Yes, MessageBoxResult.No, MessageBoxResult.Cancel],
            _ => [MessageBoxResult.OK]
        };
    }

    private static string GetButtonText(MessageBoxResult buttonResult)
    {
        return buttonResult switch
        {
            MessageBoxResult.Yes => AppResources.Word_Yes,
            MessageBoxResult.No => AppResources.Word_No,
            MessageBoxResult.Cancel => AppResources.Word_Cancel,
            _ => AppResources.Word_Ok
        };
    }

    private static void PlaySound(MessageBoxImage icon)
    {
        var sound = icon switch
        {
            MessageBoxImage.Error => SystemSounds.Hand,
            MessageBoxImage.Warning => SystemSounds.Exclamation,
            MessageBoxImage.Question => SystemSounds.Question,
            MessageBoxImage.Information => SystemSounds.Asterisk,
            _ => null
        };
        sound?.Play();
    }

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetSystemMenu(IntPtr handle, [MarshalAs(UnmanagedType.Bool)] bool revert);

    [LibraryImport("user32.dll")]
    private static partial int EnableMenuItem(IntPtr menu, uint item, uint flags);

    private void SetupIcon(MessageBoxImage icon)
    {
        // glyphs of "Segoe Fluent Icons" with the semantic colors of the Fluent theme
        (string Glyph, string Brush)? symbol = icon switch
        {
            MessageBoxImage.Error => ("", "SystemFillColorCriticalBrush"),
            MessageBoxImage.Warning => ("", "SystemFillColorCautionBrush"),
            MessageBoxImage.Question => ("", "AccentTextFillColorPrimaryBrush"),
            MessageBoxImage.Information => ("", "AccentTextFillColorPrimaryBrush"),
            _ => null
        };
        if (symbol == null)
        {
            return;
        }

        this.IconText.Text = symbol.Value.Glyph;
        this.IconText.SetResourceReference(TextBlock.ForegroundProperty, symbol.Value.Brush);
        this.IconText.Visibility = Visibility.Visible;
    }

    private Button CreateButton(MessageBoxResult buttonResult, bool isDefault, bool isCancel)
    {
        var button = new Button
        {
            Content = GetButtonText(buttonResult), IsDefault = isDefault, IsCancel = isCancel
        };
        if (isDefault)
        {
            button.SetResourceReference(StyleProperty, "PrimaryButtonStyle");
        }

        button.Click += (_, _) =>
        {
            this.result = buttonResult;
            this.isCloseAllowed = true;
            this.Close();
        };

        return button;
    }

    private void OnCopyExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        // same format as the Win32 message box
        var builder = new StringBuilder();
        builder.AppendLine(CopySeparator);
        builder.AppendLine(this.Title);
        builder.AppendLine(CopySeparator);
        builder.AppendLine(this.MessageText.Text);
        builder.AppendLine(CopySeparator);
        builder.AppendLine(string.Join("   ", this.results.Select(GetButtonText)));
        builder.AppendLine(CopySeparator);

        try
        {
            Clipboard.SetText(builder.ToString());
        }
        catch (ExternalException)
        {
            // The clipboard is used by another application. Like the Win32 message box, we ignore this.
        }

        e.Handled = true;
    }
}
