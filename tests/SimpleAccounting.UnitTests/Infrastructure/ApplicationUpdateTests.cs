// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Infrastructure
{
    using System.Diagnostics;
    using System.IO;
    using System.Windows;
    using FluentAssertions;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Infrastructure;
    using lg2de.SimpleAccounting.Presentation;
    using lg2de.SimpleAccounting.UnitTests.Extensions;
    using NSubstitute;
    using Xunit;

    public class ApplicationUpdateTests
    {
        [Fact]
        public void AskForUpdate_NoNewVersion_AppIsUpToDate()
        {
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var processApi = Substitute.For<IProcess>();
            var sut = new ApplicationUpdate(messageBox, fileSystem, processApi, "2.0");
            var releases = GithubReleaseExtensionTests.CreateRelease("2.0");

            sut.AskForUpdate(releases).Should().BeFalse();
            messageBox.Received(1).Show(
                Arg.Any<string>(),
                Arg.Any<string>(),
                MessageBoxButton.OK, Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
            messageBox.DidNotReceive().Show(
                Arg.Any<string>(),
                Arg.Any<string>(),
                MessageBoxButton.YesNo, Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
        }

        [Fact]
        public void AskForUpdate_NewVersionNo_NoUpdate()
        {
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var processApi = Substitute.For<IProcess>();
            var sut = new ApplicationUpdate(messageBox, fileSystem, processApi, "2.0");
            var releases = GithubReleaseExtensionTests.CreateRelease("2.1");
            messageBox.Show(
                Arg.Any<string>(),
                Arg.Any<string>(),
                MessageBoxButton.YesNo, Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
                .Returns(MessageBoxResult.No);

            sut.AskForUpdate(releases).Should().BeFalse();
            messageBox.DidNotReceive().Show(
                Arg.Any<string>(),
                Arg.Any<string>(),
                MessageBoxButton.OK, Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
            messageBox.Received(1).Show(
                Arg.Any<string>(),
                Arg.Any<string>(),
                MessageBoxButton.YesNo, Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
        }

        [Fact]
        public void AskForUpdate_NewVersionYes_StartUpdate()
        {
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var processApi = Substitute.For<IProcess>();
            var sut = new ApplicationUpdate(messageBox, fileSystem, processApi, "2.0");
            var releases = GithubReleaseExtensionTests.CreateRelease("2.1");
            messageBox.Show(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    MessageBoxButton.YesNo, Arg.Any<MessageBoxImage>(),
                    Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
                .Returns(MessageBoxResult.Yes);

            sut.AskForUpdate(releases).Should().BeTrue();
            messageBox.DidNotReceive().Show(
                Arg.Any<string>(),
                Arg.Any<string>(),
                MessageBoxButton.OK, Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
            messageBox.Received(1).Show(
                Arg.Any<string>(),
                Arg.Any<string>(),
                MessageBoxButton.YesNo, Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
        }

        [Fact]
        public void StartUpdateProcess_NewVersionYes_StartUpdate()
        {
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var processApi = Substitute.For<IProcess>();
            var sut = new ApplicationUpdate(messageBox, fileSystem, processApi, "2.0");
            var releases = GithubReleaseExtensionTests.CreateRelease("2.1");
            messageBox.Show(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    MessageBoxButton.YesNo, Arg.Any<MessageBoxImage>(),
                    Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
                .Returns(MessageBoxResult.Yes);
            sut.AskForUpdate(releases).Should().BeTrue();

            sut.StartUpdateProcess();

            fileSystem.Received(1).WriteAllTextIntoFile(
                Arg.Is<string>(x => x.Contains(Path.GetTempPath())), Arg.Any<string>());
            processApi.Received(1).Start(Arg.Is<ProcessStartInfo>(i => i.FileName == "powershell"));
        }
    }
}
