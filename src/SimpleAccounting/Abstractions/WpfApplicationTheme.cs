// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Abstractions;

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using lg2de.SimpleAccounting.Presentation;

/// <summary>
///     Implements <see cref="IApplicationTheme" /> using <see cref="Application.ThemeMode" />
///     which switches the Fluent theme (including title bar and window backdrop) without restart.
/// </summary>
[ExcludeFromCodeCoverage(
    Justification = "The abstraction is for unit testing only. This is the simple implementation.")]
internal class WpfApplicationTheme : IApplicationTheme
{
    public void Apply(string themeName)
    {
        if (Application.Current == null)
        {
            return;
        }

        // The API is marked as experimental but it is the supported way to switch the Fluent theme.
#pragma warning disable WPF0001
        Application.Current.ThemeMode = themeName switch
        {
            ApplicationThemes.Light => ThemeMode.Light,
            ApplicationThemes.Dark => ThemeMode.Dark,
            _ => ThemeMode.System
        };
#pragma warning restore WPF0001

        // switching the theme restores the Mica backdrop
        NeutralWindowBackground.ApplyToAllWindows();
    }
}
