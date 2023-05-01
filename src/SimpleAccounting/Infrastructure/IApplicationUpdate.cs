// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Infrastructure;

using System.Threading.Tasks;

/// <summary>
///     Defines abstraction for the application update process.
/// </summary>
internal interface IApplicationUpdate
{
    Task<bool> IsUpdateAvailableAsync(string currentVersion);

    bool StartUpdateProcess();
}
