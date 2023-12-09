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
    private readonly IBusy? busy;
    private readonly Func<Task>? asyncCommand1;
    private readonly Func<object?, Task>? asyncCommand2;
    private readonly Func<bool>? canExecute;

    public AsyncCommand(IBusy busy, Action command)
    {
        this.busy = busy;
        this.asyncCommand1 = () =>
        {
            command();
            return Task.CompletedTask;
        };
    }

    public AsyncCommand(IBusy busy, Func<Task> command)
    {
        this.busy = busy;
        this.asyncCommand1 = command;
    }

    public AsyncCommand(Action command)
    {
        this.asyncCommand1 = () =>
        {
            command();
            return Task.CompletedTask;
        };
    }

    public AsyncCommand(Action<object?> command)
    {
        this.asyncCommand2 = x =>
        {
            command(x);
            return Task.CompletedTask;
        };
    }

    public AsyncCommand(Action command, Func<bool> canExecute)
    {
        this.asyncCommand1 = () =>
        {
            command();
            return Task.CompletedTask;
        };
        this.canExecute = canExecute;
    }

    public AsyncCommand(Func<Task> command)
    {
        this.asyncCommand1 = command;
    }

    public AsyncCommand(Func<object?, Task> command)
    {
        this.asyncCommand2 = command;
    }

    public AsyncCommand(Func<Task> command, Func<bool> canExecute)
    {
        this.asyncCommand1 = command;
        this.canExecute = canExecute;
    }

    public AsyncCommand(Func<object?, Task> command, Func<bool> canExecute)
    {
        this.asyncCommand2 = command;
        this.canExecute = canExecute;
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
        return this.canExecute?.Invoke() ?? true;
    }

    public async Task ExecuteAsync(object? parameter)
    {
        if (this.busy != null)
        {
            this.busy.IsBusy = true;
        }

        RaiseCanExecuteChanged();

        if (this.asyncCommand1 != null)
        {
            await this.asyncCommand1();
        }

        if (this.asyncCommand2 != null)
        {
            await this.asyncCommand2(parameter);
        }

        if (this.busy != null)
        {
            this.busy.IsBusy = false;
        }

        RaiseCanExecuteChanged();
    }

    private static void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}
