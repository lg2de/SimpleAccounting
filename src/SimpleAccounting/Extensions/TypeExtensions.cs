// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Extensions;

using System;
using System.Reflection;

/// <summary>
///     Implements extensions on <see cref="Type" />.
/// </summary>
internal static class TypeExtensions
{
    public static string GetInformationalVersion(this Type type)
    {
        string version = type.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                         ?? "UNKNOWN";
        var position = version.IndexOf("+", StringComparison.InvariantCultureIgnoreCase);
        if (position > 0)
        {
            version = version[..position];
        }

        return version;
    }
}
