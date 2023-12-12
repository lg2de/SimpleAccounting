// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Infrastructure;

using System.Threading.Tasks;
using System.Windows.Input;

/// <summary>
///     Defines abstraction for asynchronous commands in view models.
/// </summary>
public interface IAsyncCommand : ICommand
{
    /// <summary>
    ///     Executes the command asynchronously with the specified parameter.
    /// </summary>
    /// <param name="parameter">The command parameter.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAsync(object? parameter);
}
