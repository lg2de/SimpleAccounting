// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Infrastructure;

using System.Globalization;
using System.Threading.Tasks;

/// <summary>
///     Defines abstraction for the application update process.
/// </summary>
internal interface IApplicationUpdate
{
    Task<string> GetUpdatePackageAsync(bool userInvoked, string currentVersion, CultureInfo cultureInfo);

    bool StartUpdateProcess(string packageName, bool dryRun);
}
