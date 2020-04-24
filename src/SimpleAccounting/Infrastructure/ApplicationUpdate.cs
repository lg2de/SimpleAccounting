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
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Presentation;
    using Octokit;

    [SuppressMessage(
        "Major Code Smell",
        "S4055:Literals should not be passed as localized parameters")]
    [SuppressMessage("ReSharper", "LocalizableElement")]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    internal class ApplicationUpdate : IApplicationUpdate
    {
        private readonly IMessageBox messageBox;
        private readonly string version;
        private Release newRelease;

        public ApplicationUpdate(IMessageBox messageBox, string version)
        {
            this.messageBox = messageBox;
            this.version = version;
        }

        public async Task<bool> UpdateAvailableAsync()
        {
            const string caption = "Update-Prüfung";
            IEnumerable<Release> releases;
            try
            {
                var client = new GitHubClient(new ProductHeaderValue(Defines.ProjectName));
                releases = await client.Repository.Release.GetAll(Defines.OrganizationName, Defines.ProjectName);
            }
            catch (Exception exception)
            {
                this.messageBox.Show(
                    $"Abfrage neuer Versionen fehlgeschlagen:\n{exception.Message}",
                    caption,
                    icon: MessageBoxImage.Error);
                return false;
            }

            this.newRelease = releases.GetNewRelease(this.version);
            if (this.newRelease == null)
            {
                this.messageBox.Show("Sie verwenden die neueste Version.", caption);
                return false;
            }

            var result = this.messageBox.Show(
                $"Wollen Sie auf die neue Version {this.newRelease.TagName} aktualisieren?",
                caption,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            return result == MessageBoxResult.Yes;
        }

        public void StartUpdate()
        {
            if (this.newRelease == null)
            {
                throw new InvalidOperationException();
            }

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
            File.WriteAllText(scriptPath, script);

            var asset = this.newRelease.Assets.FirstOrDefault(
                x => x.Name.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase));
            if (asset == null)
            {
                // asset not found :(
                return;
            }

            string assetUrl = asset.BrowserDownloadUrl;
            string targetFolder = Path.GetDirectoryName(this.GetType().Assembly.Location);
            int processId = Process.GetCurrentProcess().Id;
            var info = new ProcessStartInfo(
                "powershell",
                $"-File {scriptPath} -assetUrl {assetUrl} -targetFolder {targetFolder} -processId {processId}");
            Process.Start(info);

        }
    }
}
