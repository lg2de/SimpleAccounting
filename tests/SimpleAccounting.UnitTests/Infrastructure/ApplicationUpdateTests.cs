// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Infrastructure;

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using FluentAssertions;
using FluentAssertions.Extensions;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Infrastructure;
using lg2de.SimpleAccounting.Presentation;
using lg2de.SimpleAccounting.Properties;
using lg2de.SimpleAccounting.UnitTests.Extensions;
using NSubstitute;
using Xunit;

public class ApplicationUpdateTests
{
    [CulturedFact("en")]
    public async Task StartUpdateProcess_UpdateProcessFailed_UpdateAborted()
    {
        var dialogs = Substitute.For<IDialogs>();
        var windowsManager = Substitute.For<IWindowManager>();
        var fileSystem = Substitute.For<IFileSystem>();
        var httpClient = Substitute.For<IHttpClient>();
        httpClient.GetStringAsync(Arg.Any<Uri>()).Returns(
            "<ReleaseData><Release FileName=\"package-name.zip\"><EnglishDescription>eng</EnglishDescription></Release></ReleaseData>");
        var processApi = Substitute.For<IProcess>();
        processApi.Start(Arg.Any<ProcessStartInfo>())
            .Returns(Process.Start(new ProcessStartInfo("cmd.exe", "/c exit 5") { RedirectStandardError = true }));
        var sut = new ApplicationUpdate(dialogs, windowsManager, fileSystem, httpClient, processApi, null!)
        {
            WaitTimeMilliseconds = 1000
        };
        var releases = GithubReleaseExtensionTests.CreateRelease("2.1", "package-name.zip");
        await sut.AskForUpdateAsync(releases, "2.0", CultureInfo.InvariantCulture);

        sut.StartUpdateProcess("package-name.zip", dryRun: false).Should().BeFalse();
        dialogs.Received(1).ShowMessageBox(
            Arg.Is<string>(s => s.Contains("code 5.", StringComparison.InvariantCulture)),
            Resources.Header_CheckForUpdates, icon: MessageBoxImage.Error);
    }

    [Theory]
    [InlineData(0, "foo", "abc.zip")]
    [InlineData(1, "bar", "def.zip")]
    public async Task AskForUpdateAsync_NewVersionOptionSelection_StartUpdate(
        int selectedIndex, string expectedDescription, string expectedFileName)
    {
        var dialogs = Substitute.For<IDialogs>();
        var windowsManager = Substitute.For<IWindowManager>();
        windowsManager.ShowDialogAsync(Arg.Any<UpdateOptionsViewModel>()).Returns(true);
        UpdateOptionsViewModel.OptionItem option = null;
        windowsManager
            .When(x => x.ShowDialogAsync(Arg.Any<UpdateOptionsViewModel>()))
            .Do(
                x =>
                {
                    option = x.Arg<UpdateOptionsViewModel>().Options[selectedIndex];
                    option.Command.ExecuteAsync(null);
                });
        var fileSystem = Substitute.For<IFileSystem>();
        var httpClient = Substitute.For<IHttpClient>();
        httpClient.GetStringAsync(Arg.Any<Uri>()).Returns(
            "<ReleaseData>"
            + "<Release FileName=\"abc.zip\"><EnglishDescription>foo</EnglishDescription></Release>"
            + "<Release FileName=\"def.zip\"><EnglishDescription>bar</EnglishDescription></Release>"
            + "</ReleaseData>");
        var processApi = Substitute.For<IProcess>();
        var sut = new ApplicationUpdate(dialogs, windowsManager, fileSystem, httpClient, processApi, null!);
        var releases = GithubReleaseExtensionTests.CreateRelease("2.1", "abc.zip", "def.zip");

        var result = await sut.Awaiting(x => x.AskForUpdateAsync(releases, "2.0", CultureInfo.InvariantCulture))
            .Should().CompleteWithinAsync(10.Seconds());

        option.Text.Should().Be(expectedDescription);
        result.Subject.Should().Be(expectedFileName);
        await windowsManager.Received(1).ShowDialogAsync(Arg.Any<UpdateOptionsViewModel>());
        dialogs.DidNotReceive().ShowMessageBox(
            Arg.Any<string>(),
            Arg.Any<string>(),
            MessageBoxButton.OK, Arg.Any<MessageBoxImage>(),
            Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
    }

    [Fact]
    public async Task AskForUpdateAsync_NoNewVersion_AppIsUpToDate()
    {
        var dialogs = Substitute.For<IDialogs>();
        var windowsManager = Substitute.For<IWindowManager>();
        var fileSystem = Substitute.For<IFileSystem>();
        var httpClient = Substitute.For<IHttpClient>();
        var processApi = Substitute.For<IProcess>();
        var sut = new ApplicationUpdate(dialogs, windowsManager, fileSystem, httpClient, processApi, null!);
        var releases = GithubReleaseExtensionTests.CreateRelease("2.0");

        var result = await sut.Awaiting(x => x.AskForUpdateAsync(releases, "2.0", CultureInfo.InvariantCulture))
            .Should().CompleteWithinAsync(10.Seconds());
        result.Subject.Should().BeEmpty();
        dialogs.Received(1).ShowMessageBox(
            Arg.Any<string>(),
            Arg.Any<string>(),
            MessageBoxButton.OK, Arg.Any<MessageBoxImage>(),
            Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
        dialogs.DidNotReceive().ShowMessageBox(
            Arg.Any<string>(),
            Arg.Any<string>(),
            MessageBoxButton.YesNo, Arg.Any<MessageBoxImage>(),
            Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
    }

    [Fact]
    public async Task AskForUpdateAsync_NewVersionNo_NoUpdate()
    {
        var dialogs = Substitute.For<IDialogs>();
        var windowsManager = Substitute.For<IWindowManager>();
        var fileSystem = Substitute.For<IFileSystem>();
        var httpClient = Substitute.For<IHttpClient>();
        httpClient.GetStringAsync(Arg.Any<Uri>()).Returns("<ReleaseData />");
        var processApi = Substitute.For<IProcess>();
        var sut = new ApplicationUpdate(dialogs, windowsManager, fileSystem, httpClient, processApi, null!);
        var releases = GithubReleaseExtensionTests.CreateRelease("2.1");
        dialogs.ShowMessageBox(
                Arg.Any<string>(),
                Arg.Any<string>(),
                MessageBoxButton.YesNo, Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.No);

        var result = await sut.Awaiting(x => x.AskForUpdateAsync(releases, "2.0", CultureInfo.InvariantCulture))
            .Should().CompleteWithinAsync(10.Seconds());

        result.Subject.Should().BeEmpty();
        await windowsManager.Received(1).ShowDialogAsync(Arg.Any<UpdateOptionsViewModel>());
        dialogs.DidNotReceive().ShowMessageBox(
            Arg.Any<string>(),
            Arg.Any<string>(),
            MessageBoxButton.OK, Arg.Any<MessageBoxImage>(),
            Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
    }

    [Fact]
    public async Task StartUpdateProcess_NewVersionOptionSelected_StartUpdate()
    {
        var dialogs = Substitute.For<IDialogs>();
        var windowsManager = Substitute.For<IWindowManager>();
        windowsManager.ShowDialogAsync(Arg.Any<UpdateOptionsViewModel>()).Returns(true);
        windowsManager
            .When(x => x.ShowDialogAsync(Arg.Any<UpdateOptionsViewModel>()))
            .Do(x => x.Arg<UpdateOptionsViewModel>().Options[0].Command.ExecuteAsync(null));
        var fileSystem = Substitute.For<IFileSystem>();
        var httpClient = Substitute.For<IHttpClient>();
        httpClient.GetStringAsync(Arg.Any<Uri>()).Returns(
            "<ReleaseData><Release FileName=\"package-name.zip\"><EnglishDescription>eng</EnglishDescription></Release></ReleaseData>");
        var processApi = Substitute.For<IProcess>();
        processApi.Start(Arg.Any<ProcessStartInfo>())
            .Returns(Process.Start(new ProcessStartInfo("cmd.exe", "/c ping 127.0.0.1")));
        var sut = new ApplicationUpdate(dialogs, windowsManager, fileSystem, httpClient, processApi, null!)
        {
            WaitTimeMilliseconds = 0
        };
        var releases = GithubReleaseExtensionTests.CreateRelease("2.1", "package-name.zip");
        dialogs.ShowMessageBox(
                Arg.Any<string>(),
                Arg.Any<string>(),
                MessageBoxButton.YesNo, Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.Yes);
        var result = await sut.Awaiting(x => x.AskForUpdateAsync(releases, "2.0", CultureInfo.InvariantCulture))
            .Should().CompleteWithinAsync(10.Seconds());
        result.Subject.Should().Be("package-name.zip");

        sut.StartUpdateProcess("package-name.zip", dryRun: false).Should().BeTrue();

        fileSystem.Received(1).WriteAllTextIntoFile(
            Arg.Is<string>(x => x.Contains(Path.GetTempPath(), StringComparison.InvariantCulture)), Arg.Any<string>());
        processApi.Received(1).Start(Arg.Is<ProcessStartInfo>(i => i.FileName.EndsWith("powershell.exe")));
    }
}
