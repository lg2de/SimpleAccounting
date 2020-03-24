// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Abstractions
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Text;

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

        public void FileMove(string sourceFileName, string destFileName)
        {
            File.Move(sourceFileName, destFileName);
        }

        public void FileDelete(string path)
        {
            File.Delete(path);
        }

        public DateTime GetLastWriteTime(string path)
        {
            return File.GetLastWriteTime(path);
        }

        public void WriteAllTextIntoFile(string path, string content)
        {
            File.WriteAllText(path, content, Encoding.UTF8);
        }
    }
}