// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Infrastructure;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Extensions;
using lg2de.SimpleAccounting.Properties;
using Octokit;

internal class ApplicationUpdate : IApplicationUpdate
{
    private readonly IFileSystem fileSystem;
    private readonly IDialogs dialogs;
    private readonly IProcess process;

    private Release? newRelease;

    public ApplicationUpdate(IDialogs dialogs, IFileSystem fileSystem, IProcess process)
    {
        this.dialogs = dialogs;
        this.fileSystem = fileSystem;
        this.process = process;
    }

    internal int WaitTimeMilliseconds { get; set; } = 5000;

    public async Task<bool> IsUpdateAvailableAsync(string currentVersion)
    {
        IEnumerable<Release> releases = await this.GetAllReleasesAsync();
        return this.AskForUpdate(releases, currentVersion);
    }

    public bool StartUpdateProcess()
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
            return false;
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
            return false;
        }

        string assetUrl = asset.BrowserDownloadUrl;
        var targetFolder = Path.GetDirectoryName(this.GetType().Assembly.Location);
        int processId = this.process.GetCurrentProcessId();
        var info = new ProcessStartInfo(
            "powershell",
            $"-File {scriptPath} -assetUrl {assetUrl} -targetFolder {targetFolder} -processId {processId}");
        var updateProcess = this.process.Start(info);

        var exited = updateProcess?.WaitForExit(this.WaitTimeMilliseconds) == true;
        if (!exited)
        {
            return true;
        }

        var message = string.Format(
            CultureInfo.CurrentUICulture, Resources.Update_ProcessFailed, updateProcess.ExitCode);
        this.dialogs.ShowMessageBox(message, Resources.Header_CheckForUpdates, icon: MessageBoxImage.Error);
        return false;
    }

    [SuppressMessage(
        "Minor Code Smell", "S2221:\"Exception\" should not be caught when not required by called methods",
        Justification = "catch exceptions from external library")]
    internal async Task<IEnumerable<Release>> GetAllReleasesAsync()
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
                    this.dialogs.ShowMessageBox(
                        Resources.Update_QueryVersionsFailed + $"\n{exception.Message}",
                        Resources.Header_CheckForUpdates,
                        icon: MessageBoxImage.Error);
                    return Enumerable.Empty<Release>();
                }
            });
    }

    internal bool AskForUpdate(IEnumerable<Release> releases, string currentVersion)
    {
        this.newRelease = releases.GetNewRelease(currentVersion);
        string caption = Resources.Header_CheckForUpdates;
        if (this.newRelease == null)
        {
            this.dialogs.ShowMessageBox(Resources.Update_UpToDate, caption);
            return false;
        }

        string message = string.Format(
            CultureInfo.CurrentUICulture, Resources.Question_UpdateToVersionX, this.newRelease.TagName);
        var result = this.dialogs.ShowMessageBox(
            message,
            caption,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        return result == MessageBoxResult.Yes;
    }
}
