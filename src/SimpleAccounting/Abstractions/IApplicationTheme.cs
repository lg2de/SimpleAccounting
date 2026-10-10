// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Abstractions;

/// <summary>
///     Defines abstraction for the visual theme (light or dark) of the application.
/// </summary>
internal interface IApplicationTheme
{
    /// <summary>
    ///     Applies the theme with the specified name to the application.
    /// </summary>
    /// <param name="themeName">
    ///     The name of the theme: <see cref="ApplicationThemes.Light" />, <see cref="ApplicationThemes.Dark" />
    ///     or <see cref="ApplicationThemes.System" /> (also used for unknown names).
    /// </param>
    void Apply(string themeName);
}
