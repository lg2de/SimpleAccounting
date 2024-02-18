// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Abstractions;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using JetBrains.Annotations;

/// <summary>
///     Redirects <see cref="IFileSystem" /> into real implementations from .NET framework.
/// </summary>
/// <remarks>
///     The reason for this class is testability.
///     This is why it is excluded from code coverage.
/// </remarks>
[ExcludeFromCodeCoverage(
    Justification = "The abstraction is for unit testing only. This is the simple implementation.")]
[UsedImplicitly]
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

    public string ReadAllTextFromFile(string path)
    {
        return File.ReadAllText(path);
    }

    public byte[] ReadAllBytesFromFile(string path)
    {
        return File.ReadAllBytes(path);
    }

    public IEnumerable<(string RootPath, Func<string> GetFormat)> GetDrives()
    {
        foreach (var driveInfo in DriveInfo.GetDrives())
        {
            (string RootPath, Func<string> GetFormat) info;
            try
            {
                info = (RootPath: driveInfo.RootDirectory.FullName, GetFormat: () => driveInfo.DriveFormat);
            }
            catch (IOException)
            {
                continue;
            }

            yield return info;
        }
    }

    public IDisposable StartMonitoring(string filePath, Action<string> changedCallback)
    {
        return new FileSystemWatchWrapper(filePath, changedCallback);
    }

    private sealed class FileSystemWatchWrapper : IDisposable
    {
        private readonly Action<string> changedCallback;
        private readonly FileSystemWatcher fileSystemWatcher;

        public FileSystemWatchWrapper(string filePath, Action<string> changedCallback)
        {
            this.changedCallback = changedCallback;
            var directoryName = Path.GetDirectoryName(filePath)!;
            var fileName = Path.GetFileName(filePath);

            this.fileSystemWatcher = new FileSystemWatcher(directoryName, fileName + "*");
            this.fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;
            this.fileSystemWatcher.EnableRaisingEvents = true;

            this.fileSystemWatcher.Changed += this.OnFileChanged;
        }

        public void Dispose()
        {
            this.fileSystemWatcher.Changed -= this.OnFileChanged;
            this.fileSystemWatcher.Dispose();
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            this.changedCallback(e.Name!);
        }
    }
}
