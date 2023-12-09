// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Infrastructure;

using System;
using System.Threading.Tasks;
using System.Windows.Input;

/// <summary>
///     Implements <see cref="ICommand"/> for synchronous commands.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Predicate<object?>? canExecute;
    private readonly Action<object?>? execute;
    private readonly Func<object?, Task>? asyncExecute;

    public RelayCommand(Action<object?> execute)
    {
        this.execute = execute;
    }

    public RelayCommand(Func<object?, Task> execute)
    {
        this.asyncExecute = execute;
    }

    public RelayCommand(Action<object?> execute, Predicate<object?> canExecute)
    {
        this.execute = execute;
        this.canExecute = canExecute;
    }

    public RelayCommand(Func<object?, Task> execute, Predicate<object?> canExecute)
    {
        this.asyncExecute = execute;
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
        this.execute?.Invoke(parameter);
        this.asyncExecute?.Invoke(parameter);
    }
}
