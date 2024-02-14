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
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Extensions;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Presentation;
using lg2de.SimpleAccounting.Properties;
using Octokit;

/// <summary>
///     Implements the application update.
/// </summary>
[SuppressMessage(
    "Major Code Smell", "S1200:Classes should not be coupled to too many other classes",
    Justification = "Only interfaces and github API DTOs are referenced.")]
internal class ApplicationUpdate : IApplicationUpdate
{
    private readonly IDialogs dialogs;
    private readonly IFileSystem fileSystem;
    private readonly IHttpClient httpClient;
    private readonly IProcess process;
    private readonly IWindowManager windowManager;

    private Release? newRelease;

    public ApplicationUpdate(
        IDialogs dialogs, IWindowManager windowManager, IFileSystem fileSystem, IHttpClient httpClient,
        IProcess process)
    {
        this.dialogs = dialogs;
        this.windowManager = windowManager;
        this.fileSystem = fileSystem;
        this.httpClient = httpClient;
        this.process = process;
    }

    internal int WaitTimeMilliseconds { get; set; } = 5000;

    public async Task<string> GetUpdatePackageAsync(string currentVersion, CultureInfo cultureInfo)
    {
        IEnumerable<Release> releases = await this.GetAllReleasesAsync();
        return await this.AskForUpdateAsync(releases, currentVersion, cultureInfo);
    }

    public bool StartUpdateProcess(string packageName, bool dryRun)
    {
        // load script from resource and write into temp file
        string resourceName = dryRun
            ? "lg2de.SimpleAccounting.Scripts.UpdateApplicationTest.ps1"
            : "lg2de.SimpleAccounting.Scripts.UpdateApplication.ps1";
        var stream = this.GetType().Assembly.GetManifestResourceStream(resourceName);
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
        var asset = this.newRelease?.Assets.FirstOrDefault(x => x.Name == packageName);

        var assetUrl = asset?.BrowserDownloadUrl ?? "dummy-asset";
        var targetFolder = Path.GetDirectoryName(this.GetType().Assembly.Location);
        int processId = this.process.GetCurrentProcessId();
        string fileName = Environment.ExpandEnvironmentVariables(
            @"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe");
        var arguments = new[]
        {
            "-ExecutionPolicy Bypass", $"-File {scriptPath}", $"-assetUrl {assetUrl}",
            $"-targetFolder {targetFolder}", $"-processId {processId}"
        };
        var info = new ProcessStartInfo(fileName, string.Join(" ", arguments));
        var updateProcess = this.process.Start(info);

        var exited = updateProcess?.WaitForExit(this.WaitTimeMilliseconds) == true;
        if (!exited)
        {
            // succeed
            return true;
        }

        // failed - retry with capturing output
        info.RedirectStandardError = true;
        info.WindowStyle = ProcessWindowStyle.Hidden;
        updateProcess = this.process.Start(info);
        exited = updateProcess?.WaitForExit(this.WaitTimeMilliseconds) == true;
        if (!exited)
        {
            // succeed
            return true;
        }

        var exitCode = updateProcess!.ExitCode;
        var errorOutput = updateProcess.StandardError.ReadToEnd();
        var message = string.Format(
            CultureInfo.CurrentUICulture, Resources.Update_ProcessFailedX2, exitCode, errorOutput);
        this.dialogs.ShowMessageBox(message, Resources.Header_CheckForUpdates, icon: MessageBoxImage.Error);
        return false;
    }

    [SuppressMessage(
        "Minor Code Smell", "S2221:\"Exception\" should not be caught when not required by called methods",
        Justification = "catch exceptions from external library")]
    internal Task<IEnumerable<Release>> GetAllReleasesAsync()
    {
        return Task.Run(
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

    [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "Checked.")]
    internal async Task<string> AskForUpdateAsync(
        IEnumerable<Release> releases, string currentVersion, CultureInfo cultureInfo)
    {
        this.newRelease = releases.GetNewRelease(currentVersion);
        string caption = Resources.Header_CheckForUpdates;
        if (this.newRelease == null)
        {
            this.dialogs.ShowMessageBox(Resources.Update_UpToDate, caption);
            return string.Empty;
        }

        string releaseDataXml;
        try
        {
            releaseDataXml = await this.httpClient.GetStringAsync(
                new Uri("https://lg2de.github.io/SimpleAccounting/ReleaseData.xml"));
        }
        catch (HttpRequestException exception)
        {
            this.dialogs.ShowMessageBox(exception.Message, caption, icon: MessageBoxImage.Error);
            return string.Empty;
        }

        var releaseData = ReleaseData.Deserialize(releaseDataXml);
        var releaseMap = releaseData.Releases?.ToDictionary(
            x => x.FileName?.ToUpperInvariant() ?? "<unknown>",
            x => cultureInfo.Parent.TwoLetterISOLanguageName == "de" ? x.GermanDescription : x.EnglishDescription);
        var vm = new UpdateOptionsViewModel(
            string.Format(cultureInfo, Resources.Question_UpdateToVersionX, this.newRelease.TagName));
        string selectedPackage = string.Empty;
        foreach (string assetName in this.newRelease.Assets.Select(x => x.Name))
        {
            vm.AddOption(
                releaseMap?.GetValueOrDefault(assetName.ToUpperInvariant(), assetName) ?? "<unknown>",
                new AsyncCommand(
                    () =>
                    {
                        selectedPackage = assetName;
                        return vm.TryCloseAsync(dialogResult: true);
                    }));
        }

        var result = await this.windowManager.ShowDialogAsync(vm);
        return result == true ? selectedPackage : string.Empty;
    }
}
