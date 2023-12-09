// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Infrastructure;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using FluentAssertions;
using FluentAssertions.Extensions;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Infrastructure;
using lg2de.SimpleAccounting.Properties;
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

    [Fact]
    public async Task LoadAsync_UserDoesNotWantToStartSecureDriveApp_ReturnsAborted()
    {
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings { SecuredDrives = ["K:\\"] };
        var sut = new ProjectFileLoader(settings, dialogs, fileSystem, processApi);
        dialogs.ShowMessageBox(
                Arg.Is<string>(s => s.Contains("Cryptomator")),
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
