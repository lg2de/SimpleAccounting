// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Extensions
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using lg2de.SimpleAccounting.Abstractions;
    using Microsoft.Win32;

    internal class SecureDriveStarter
    {
        private const string SecureDriveApp = "Cryptomator";

        private const string SecureDriveAppExe = SecureDriveApp + ".exe";

        private const string SecureDriveAppKey =
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Cryptomator_is1";

        const int WaitMilliseconds = 500;

        private readonly IFileSystem fileSystem;
        private readonly string projectFileName;

        private Process process;

        public SecureDriveStarter(IFileSystem fileSystem, string projectFileName)
        {
            this.fileSystem = fileSystem;
            this.projectFileName = projectFileName;
        }

        public async Task<bool> StartApplicationAsync()
        {
            // check whether process is already running
            this.process = GetSecureDriveProcess();

            if (this.process == null
                && !await this.StartProcessAsync())
            {
                // the application is NOT running and could NOT be started
                return false;
            }

            // bring to front to force user to unlock drive
            WinApi.BringProcessToFront(this.process);

            // which for the project file to be available
            while (true)
            {
                if (this.fileSystem.FileExists(this.projectFileName))
                {
                    // file IS available
                    // minimize the drive application and focus SimpleAccounting
                    WinApi.MinimizeProcess(this.process);
                    WinApi.BringProcessToFront(Process.GetCurrentProcess());
                    return true;
                }

                // just wait a bit...
                await Task.Delay(WaitMilliseconds);
            }
        }

        private static Process GetSecureDriveProcess()
        {
            return Process.GetProcesses().FirstOrDefault(
                x => x.ProcessName.Equals(SecureDriveApp, StringComparison.InvariantCultureIgnoreCase));
        }

        private async Task<bool> StartProcessAsync()
        {
            var localMachine = Registry.LocalMachine;
            using var fileKey = localMachine.OpenSubKey(SecureDriveAppKey);
            var path = fileKey?.GetValue("InstallLocation").ToString();
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            this.process = Process.Start(Path.Combine(path, SecureDriveAppExe));
            if (this.process == null || this.process.HasExited)
            {
                return false;
            }

            while (true)
            {
                if (!this.process.MainWindowHandle.Equals(IntPtr.Zero))
                {
                    break;
                }

                await Task.Delay(WaitMilliseconds);

                this.process = GetSecureDriveProcess();
                if (this.process == null || this.process.HasExited)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
