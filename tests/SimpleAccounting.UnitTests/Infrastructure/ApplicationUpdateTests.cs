// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Infrastructure;

using System.Diagnostics;
using System.IO;
using System.Windows;
using FluentAssertions;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Infrastructure;
using lg2de.SimpleAccounting.Properties;
using lg2de.SimpleAccounting.UnitTests.Extensions;
using NSubstitute;
using Xunit;

public class ApplicationUpdateTests
{
    [Fact]
    public void AskForUpdate_NoNewVersion_AppIsUpToDate()
    {
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        var sut = new ApplicationUpdate(dialogs, fileSystem, processApi);
        var releases = GithubReleaseExtensionTests.CreateRelease("2.0");

        sut.AskForUpdate(releases, "2.0").Should().BeFalse();
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
    public void AskForUpdate_NewVersionNo_NoUpdate()
    {
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        var sut = new ApplicationUpdate(dialogs, fileSystem, processApi);
        var releases = GithubReleaseExtensionTests.CreateRelease("2.1");
        dialogs.ShowMessageBox(
                Arg.Any<string>(),
                Arg.Any<string>(),
                MessageBoxButton.YesNo, Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.No);

        sut.AskForUpdate(releases, "2.0").Should().BeFalse();
        dialogs.DidNotReceive().ShowMessageBox(
            Arg.Any<string>(),
            Arg.Any<string>(),
            MessageBoxButton.OK, Arg.Any<MessageBoxImage>(),
            Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
        dialogs.Received(1).ShowMessageBox(
            Arg.Any<string>(),
            Arg.Any<string>(),
            MessageBoxButton.YesNo, Arg.Any<MessageBoxImage>(),
            Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
    }

    [Fact]
    public void AskForUpdate_NewVersionYes_StartUpdate()
    {
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        var sut = new ApplicationUpdate(dialogs, fileSystem, processApi);
        var releases = GithubReleaseExtensionTests.CreateRelease("2.1");
        dialogs.ShowMessageBox(
                Arg.Any<string>(),
                Arg.Any<string>(),
                MessageBoxButton.YesNo, Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.Yes);

        sut.AskForUpdate(releases, "2.0").Should().BeTrue();
        dialogs.DidNotReceive().ShowMessageBox(
            Arg.Any<string>(),
            Arg.Any<string>(),
            MessageBoxButton.OK, Arg.Any<MessageBoxImage>(),
            Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
        dialogs.Received(1).ShowMessageBox(
            Arg.Any<string>(),
            Arg.Any<string>(),
            MessageBoxButton.YesNo, Arg.Any<MessageBoxImage>(),
            Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
    }

    [Fact]
    public void StartUpdateProcess_NewVersionYes_StartUpdate()
    {
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        processApi.Start(Arg.Any<ProcessStartInfo>())
            .Returns(Process.Start(new ProcessStartInfo("cmd.exe", "/c ping 127.0.0.1")));
        var sut = new ApplicationUpdate(dialogs, fileSystem, processApi)
        {
            WaitTimeMilliseconds = 0
        };
        var releases = GithubReleaseExtensionTests.CreateRelease("2.1");
        dialogs.ShowMessageBox(
                Arg.Any<string>(),
                Arg.Any<string>(),
                MessageBoxButton.YesNo, Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.Yes);
        sut.AskForUpdate(releases, "2.0").Should().BeTrue();

        sut.StartUpdateProcess().Should().BeTrue();

        fileSystem.Received(1).WriteAllTextIntoFile(
            Arg.Is<string>(x => x.Contains(Path.GetTempPath())), Arg.Any<string>());
        processApi.Received(1).Start(Arg.Is<ProcessStartInfo>(i => i.FileName == "powershell"));
    }

    [CulturedFact("en")]
    public void StartUpdateProcess_UpdateProcessFailed_UpdateAborted()
    {
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        processApi.Start(Arg.Any<ProcessStartInfo>())
            .Returns(Process.Start(new ProcessStartInfo("cmd.exe", "/c exit 5")));
        var sut = new ApplicationUpdate(dialogs, fileSystem, processApi)
        {
            WaitTimeMilliseconds = 1000
        };
        var releases = GithubReleaseExtensionTests.CreateRelease("2.1");
        sut.AskForUpdate(releases, "2.0");

        sut.StartUpdateProcess().Should().BeFalse();
        dialogs.Received(1).ShowMessageBox(
            Arg.Is<string>(s => s.Contains("code 5.")), Resources.Header_CheckForUpdates, icon: MessageBoxImage.Error);
    }
}
