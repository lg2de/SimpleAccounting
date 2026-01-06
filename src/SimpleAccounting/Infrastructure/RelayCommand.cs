// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Infrastructure;

using System;
using System.Windows.Input;

/// <summary>
///     Implements <see cref="ICommand" /> for synchronous commands.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Predicate<object?>? canExecute;
    private readonly Action<object?> execute;

    public RelayCommand(Action<object?> execute)
    {
        this.execute = execute;
    }

    public RelayCommand(Action<object?> execute, Predicate<object?> canExecute)
    {
        this.execute = execute;
        this.canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter)
    {
        return this.canExecute == null || this.canExecute(parameter);
    }

    public void Execute(object? parameter)
    {
        this.execute(parameter);
    }
}
