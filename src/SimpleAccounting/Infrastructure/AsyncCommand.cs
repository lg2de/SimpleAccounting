// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Infrastructure;

using System;
using System.Threading.Tasks;
using System.Windows.Input;

/// <summary>
///     Implements <see cref="IAsyncCommand"/> for easier handling of asynchronous commands in view models.
/// </summary>
public class AsyncCommand : IAsyncCommand
{
    private readonly IBusy busy;
    private readonly Func<Task> command;

    public AsyncCommand(IBusy busy, Action command)
    {
        this.busy = busy;
        this.command = () =>
        {
            command();
            return Task.CompletedTask;
        };
    }

    public AsyncCommand(IBusy busy, Func<Task> command)
    {
        this.busy = busy;
        this.command = command;
    }

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public async void Execute(object? parameter)
    {
        await this.ExecuteAsync(parameter);
    }

    public bool CanExecute(object? parameter)
    {
        return true;
    }

    public async Task ExecuteAsync(object? parameter)
    {
        this.busy.IsBusy = true;
        RaiseCanExecuteChanged();
        await this.command();
        this.busy.IsBusy = false;
        RaiseCanExecuteChanged();
    }

    private static void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}
