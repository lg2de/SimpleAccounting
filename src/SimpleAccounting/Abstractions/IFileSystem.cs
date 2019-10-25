// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Abstractions
{
    /// <summary>
    ///     Defines abstraction for file system access not available in .NET framework.
    /// </summary>
    internal interface IFileSystem
    {
        /// <summary>
        ///     Gets a value indicating whether the specified file exists.
        /// </summary>
        /// <param name="filePath">The path to the file to be checked.</param>
        /// <returns>Returns <c>true</c> if the file exists.</returns>
        bool FileExists(string filePath);
    }
}