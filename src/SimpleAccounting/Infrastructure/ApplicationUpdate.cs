// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Extensions;
    using Octokit;

    [SuppressMessage(
        "Major Code Smell",
        "S4055:Literals should not be passed as localized parameters")]
    [SuppressMessage("ReSharper", "LocalizableElement")]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    internal class ApplicationUpdate : IApplicationUpdate
    {
        private const string Caption = "Update-Prüfung";
        private readonly IFileSystem fileSystem;
        private readonly IMessageBox messageBox;
        private readonly IProcess process;

        private Release? newRelease;

        public ApplicationUpdate(IMessageBox messageBox, IFileSystem fileSystem, IProcess process)
        {
            this.messageBox = messageBox;
            this.fileSystem = fileSystem;
            this.process = process;
        }

        public async Task<bool> IsUpdateAvailableAsync(string currentVersion)
        {
            IEnumerable<Release> releases = await this.GetAllReleasesAsync();
            return this.AskForUpdate(releases, currentVersion);
        }

        public void StartUpdateProcess()
        {
            if (this.newRelease == null)
            {
                throw new InvalidOperationException();
            }

            // load script from resource and write into temp file
            var stream = this.GetType().Assembly.GetManifestResourceStream(
                "lg2de.SimpleAccounting.UpdateApplication.ps1");
            if (stream == null)
            {
                // script not found :(
                return;
            }

            using var reader = new StreamReader(stream);
            var script = reader.ReadToEnd();
            string scriptPath = Path.Combine(Path.GetTempPath(), "UpdateApplication.ps1");
            this.fileSystem.WriteAllTextIntoFile(scriptPath, script);

            // select and download the new version
            var asset = this.newRelease.Assets.FirstOrDefault(
                x => x.Name.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase));
            if (asset == null)
            {
                // asset not found :(
                return;
            }

            string assetUrl = asset.BrowserDownloadUrl;
            var targetFolder = Path.GetDirectoryName(this.GetType().Assembly.Location);
            int processId = this.process.GetCurrentProcessId();
            var info = new ProcessStartInfo(
                "powershell",
                $"-File {scriptPath} -assetUrl {assetUrl} -targetFolder {targetFolder} -processId {processId}");
            this.process.Start(info);
        }

        internal bool AskForUpdate(IEnumerable<Release> releases, string currentVersion)
        {
            this.newRelease = releases.GetNewRelease(currentVersion);
            if (this.newRelease == null)
            {
                this.messageBox.Show("Sie verwenden die neueste Version.", Caption);
                return false;
            }

            var result = this.messageBox.Show(
                $"Wollen Sie auf die neue Version {this.newRelease.TagName} aktualisieren?",
                Caption,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            return result == MessageBoxResult.Yes;
        }

        [SuppressMessage(
            "Minor Code Smell", "S2221:\"Exception\" should not be caught when not required by called methods",
            Justification = "catch exceptions from external library")]
        private async Task<IEnumerable<Release>> GetAllReleasesAsync()
        {
            return await Task.Run(
                async () =>
                {
                    try
                    {
                        var productInformation = new ProductHeaderValue(Defines.ProjectName);
                        var client = new GitHubClient(productInformation);
                        return await client.Repository.Release.GetAll(Defines.OrganizationName, Defines.ProjectName);
                    }
                    catch (Exception exception)
                    {
                        this.messageBox.Show(
                            $"Abfrage neuer Versionen fehlgeschlagen:\n{exception.Message}",
                            Caption,
                            icon: MessageBoxImage.Error);
                        return Enumerable.Empty<Release>();
                    }
                });
        }
    }
}
