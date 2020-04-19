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
            this.process = GetSecureDriveProcess();
            if (this.process == null
                && !await this.StartProcessAsync())
            {
                return false;
            }

            WinApi.BringProcessToFront(this.process);

            while (true)
            {
                if (this.fileSystem.FileExists(this.projectFileName))
                {
                    WinApi.MinimizeProcess(this.process);
                    WinApi.BringProcessToFront(Process.GetCurrentProcess());
                    return true;
                }

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
