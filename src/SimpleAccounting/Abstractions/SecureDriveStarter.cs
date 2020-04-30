// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Abstractions
{
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Win32;

    internal class SecureDriveStarter
    {
#pragma warning disable S1075 // URIs should not be hardcoded
        private const string SecureDriveApp = "Cryptomator";

        private const string SecureDriveAppExe = SecureDriveApp + ".exe";

        private const string SecureDriveAppKey =
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Cryptomator_is1";

        private const string SecureDriveAppFallbackPath = "C:\\Program Files\\Cryptomator\\";
#pragma warning restore S1075 // URIs should not be hardcoded

        const int WaitMilliseconds = 500;

        private readonly IFileSystem fileSystem;
        private readonly IProcess processApi;
        private readonly string projectFileName;

        private Process? applicationProcess;

        public SecureDriveStarter(IFileSystem fileSystem, IProcess processApi, string projectFileName)
        {
            this.fileSystem = fileSystem;
            this.processApi = processApi;
            this.projectFileName = projectFileName;
        }

        public async Task<bool> StartApplicationAsync()
        {
            // check whether process is already running
            this.applicationProcess = this.GetSecureDriveProcess();

            if (this.applicationProcess == null
                && !await this.StartProcessAsync())
            {
                // the application is NOT running and could NOT be started
                return false;
            }

            // bring to front to force user to unlock drive
            this.processApi.BringProcessToFront(this.applicationProcess!);

            // which for the project file to be available
            while (true)
            {
                if (this.fileSystem.FileExists(this.projectFileName))
                {
                    // file IS available
                    // minimize the drive application and focus SimpleAccounting
                    this.processApi.MinimizeProcess(this.applicationProcess!);
                    this.processApi.BringProcessToFront(this.processApi.GetCurrentProcess());
                    return true;
                }

                // just wait a bit...
                await Task.Delay(WaitMilliseconds);
            }
        }

        private Process GetSecureDriveProcess()
        {
            return this.processApi.GetProcessByName(SecureDriveApp);
        }

        private async Task<bool> StartProcessAsync()
        {
            var localMachine = Registry.LocalMachine;
            using var fileKey = localMachine.OpenSubKey(SecureDriveAppKey);
            var directory = fileKey?.GetValue("InstallLocation").ToString() ?? SecureDriveAppFallbackPath;
            string filePath = Path.Combine(directory, SecureDriveAppExe);
            if (!this.fileSystem.FileExists(filePath))
            {
                return false;
            }

            var info = new ProcessStartInfo(filePath);
            this.applicationProcess = this.processApi.Start(info);
            if (this.applicationProcess == null)
            {
                return false;
            }

            while (true)
            {
                if (this.processApi.IsProcessWindowVisible(this.applicationProcess))
                {
                    break;
                }

                await Task.Delay(WaitMilliseconds);

                this.applicationProcess = this.GetSecureDriveProcess();
                if (this.applicationProcess == null || this.applicationProcess.HasExited)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
