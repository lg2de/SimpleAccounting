// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using lg2de.SimpleAccounting.Infrastructure;

/// <summary>
///     Implements the view model for a single menu entry.
/// </summary>
public class MenuItemViewModel
{
    public MenuItemViewModel(string header, IAsyncCommand command)
    {
        this.Header = header;
        this.Command = command;
    }

    public string Header { get; }

    public IAsyncCommand Command { get; }
}
