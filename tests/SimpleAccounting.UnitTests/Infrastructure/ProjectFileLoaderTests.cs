// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Infrastructure;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Infrastructure;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Properties;
using lg2de.SimpleAccounting.UnitTests.Presentation;
using NSubstitute;
using Xunit;

public class ProjectFileLoaderTests
{
    [Fact]
    public async Task LoadAsync_FileNotExists_ReturnsFailed()
    {
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectFileLoader(settings, dialogs, fileSystem, processApi);

        var result = await sut.Awaiting(x => x.LoadAsync("the.fileName")).Should().CompleteWithinAsync(1.Seconds());

        result.Subject.Should().Be(OperationResult.Failed);
    }

    [CulturedFact("en")]
    public async Task LoadAsync_UserDoesNotWantToOpenReservedProject_ReturnsAborted()
    {
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectFileLoader(settings, dialogs, fileSystem, processApi);
        const string reservationFileName = "the.fileName#";
        fileSystem.FileExists(reservationFileName).Returns(true);
        fileSystem.ReadAllTextFromFile(reservationFileName).Returns(new ReservationData().Serialize());
        dialogs.ShowMessageBox(
                Arg.Is<string>(s => s.Contains("by a different user", StringComparison.Ordinal)),
                Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.No);

        var result = await sut.Awaiting(x => x.LoadAsync("the.fileName")).Should().CompleteWithinAsync(1.Seconds());

        result.Subject.Should().Be(OperationResult.Aborted);
        dialogs.Received(1).ShowMessageBox(
            Arg.Is<string>(s => s.Contains("by a different user", StringComparison.Ordinal)),
            Arg.Any<string>(),
            Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
            Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
    }

    [CulturedFact("en")]
    public async Task LoadAsync_ReservationForCurrentUserAndHost_ReturnsCompleted()
    {
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectFileLoader(settings, dialogs, fileSystem, processApi);
        const string projectFileName = "the.fileName";
        const string reservationFileName = "the.fileName#";
        fileSystem.FileExists(reservationFileName).Returns(true);
        var reservationData =
            new ReservationData { UserName = Environment.UserName, MachineName = Environment.MachineName };
        string reservationXml = reservationData.Serialize();
        fileSystem.ReadAllTextFromFile(reservationFileName).Returns(reservationXml);
        fileSystem.FileExists(projectFileName).Returns(true);
        fileSystem.ReadAllTextFromFile(projectFileName).Returns(Samples.SampleProject.Serialize());

        var result = await sut.Awaiting(x => x.LoadAsync(projectFileName)).Should().CompleteWithinAsync(1.Seconds());

        result.Subject.Should().Be(OperationResult.Completed);
        dialogs.DidNotReceive().ShowMessageBox(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
            Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
        // Such file must be written finally similar to the file read initially.
        fileSystem.Received(1).WriteAllTextIntoFile(reservationFileName, reservationXml);
    }

    [Fact]
    public async Task LoadAsync_UserDoesNotWantToStartSecureDriveApp_ReturnsAborted()
    {
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings { SecuredDrives = ["K:\\"] };
        var sut = new ProjectFileLoader(settings, dialogs, fileSystem, processApi);
        dialogs.ShowMessageBox(
                Arg.Is<string>(s => s.Contains("Cryptomator", StringComparison.Ordinal)),
                Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.No);

        var result = await sut.Awaiting(x => x.LoadAsync("K:\\the.fileName")).Should()
            .CompleteWithinAsync(1.Seconds());

        result.Subject.Should().Be(OperationResult.Aborted);
        processApi.DidNotReceive().Start(Arg.Any<ProcessStartInfo>());
        dialogs.Received(1).ShowMessageBox(
            Arg.Is<string>(s => s.Contains("Cryptomator", StringComparison.Ordinal)),
            Arg.Any<string>(),
            Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
            Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
    }
}
