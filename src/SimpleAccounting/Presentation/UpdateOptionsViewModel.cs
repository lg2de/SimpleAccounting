// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System.Collections.Generic;
using Caliburn.Micro;
using lg2de.SimpleAccounting.Infrastructure;

/// <summary>
///     Implements the view model to allow selection of update options.
/// </summary>
public class UpdateOptionsViewModel(string text) : Screen
{
    public string Text { get; } = text;

    public IList<OptionItem> Options { get; } = [];

    public void AddOption(string option, IAsyncCommand command)
    {
        this.Options.Add(new OptionItem(option, command));
    }

    public class OptionItem(string text, IAsyncCommand command)
    {
        public string Text { get; } = text;

        public IAsyncCommand Command { get; } = command;
    }
}
