// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Abstractions
{
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    /// <summary>
    ///     Redirects <see cref="IFileSystem"/> into real implementations from .NET framework.
    /// </summary>
    /// <remarks>
    ///     The reason for this class is testability.
    ///     This is why it is excluded from code coverage.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    internal class FileSystem : IFileSystem
    {
        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }
    }
}