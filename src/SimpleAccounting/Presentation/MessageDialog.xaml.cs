// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using AppResources = lg2de.SimpleAccounting.Properties.Resources;

/// <summary>
///     Implements a message box in the style of the application (Fluent theme, light or dark).
///     It replaces <see cref="MessageBox" /> which is always shown in the classic Win32 style.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "This view cannot be tested.")]
public partial class MessageDialog
{
    private MessageBoxResult result;

    private MessageDialog(
        string messageBoxText, string caption,
        MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
    {
        this.InitializeComponent();

        this.Title = caption;
        this.MessageText.Text = messageBoxText;
        this.SetupIcon(icon);

        var results = GetResults(button);
        if (!results.Contains(defaultResult))
        {
            defaultResult = results[0];
        }

        // closing the window without button has the same meaning as the Win32 message box
        this.result = results.Contains(MessageBoxResult.Cancel) ? MessageBoxResult.Cancel
            : results.Contains(MessageBoxResult.No) ? MessageBoxResult.No
            : MessageBoxResult.OK;

        // ESC triggers "Cancel", or "OK" if this is the only button
        this.ButtonsPanel.ItemsSource = results
            .Select(x => this.CreateButton(
                x, x == defaultResult, x == MessageBoxResult.Cancel || results.Count == 1))
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
            Content = buttonResult switch
            {
                MessageBoxResult.Yes => AppResources.Word_Yes,
                MessageBoxResult.No => AppResources.Word_No,
                MessageBoxResult.Cancel => AppResources.Word_Cancel,
                _ => AppResources.Word_Ok
            },
            MinWidth = 100,
            IsDefault = isDefault,
            IsCancel = isCancel
        };
        if (isDefault)
        {
            button.SetResourceReference(StyleProperty, "PrimaryButtonStyle");
        }

        button.Click += (_, _) =>
        {
            this.result = buttonResult;
            this.Close();
        };

        return button;
    }
}
