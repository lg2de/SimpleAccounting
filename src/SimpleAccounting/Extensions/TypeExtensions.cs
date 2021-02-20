// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Extensions
{
    using System;
    using System.Reflection;

    internal static class TypeExtensions
    {
        public static string GetInformationalVersion(this Type type)
        {
            return type.Assembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? "UNKNOWN";
        }
    }
}
