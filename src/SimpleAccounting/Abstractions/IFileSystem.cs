// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Abstractions
{
    using System;

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

        /// <summary>
        ///     Moves a specified file to a new location, providing the option to specify a new file name.
        /// </summary>
        /// <param name="sourceFileName">The name of the file to move. Can include a relative or absolute path.</param>
        /// <param name="destFileName">The new path and name for the file.</param>
        void FileMove(string sourceFileName, string destFileName);

        /// <summary>
        ///     Deletes the specified file.
        /// </summary>
        /// <param name="path">The name of the file to be deleted. Wildcard characters are not supported.</param>
        void FileDelete(string path);

        /// <summary>
        ///     Returns the date and time the specified file or directory was last written to.
        /// </summary>
        /// <param name="path">The file or directory for which to obtain write date and time information.</param>
        /// <returns>
        ///     A DateTime structure set to the date and time that the specified file or directory was last written to.
        ///     This value is expressed in local time.
        /// </returns>
        DateTime GetLastWriteTime(string path);

        /// <summary>
        ///     Creates a new file, writes the specified string to the file, and then closes the file.
        /// If the target file already exists, it is overwritten.
        /// </summary>
        /// <param name="path">The file to write to.</param>
        /// <param name="content">The string to write to the file.</param>
        void WriteAllTextIntoFile(string path, string content);

        /// <summary>
        ///     Opens a text file, reads all the text in the file into a string, and then closes the file.
        /// </summary>
        /// <param name="path">The file to open for reading.</param>
        /// <returns>A string containing all the text in the file.</returns>
        string ReadAllTextFromFile(string path);
    }
}