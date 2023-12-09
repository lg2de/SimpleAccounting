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
    Task ExecuteAsync(object? parameter);
}
