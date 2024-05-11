// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Model;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Caliburn.Micro;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Extensions;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Infrastructure;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Presentation;
using lg2de.SimpleAccounting.Properties;
using lg2de.SimpleAccounting.UnitTests.Presentation;
using NSubstitute;
using Xunit;
using MessageBoxOptions = System.Windows.MessageBoxOptions;

public class ProjectDataTests
{
    [CulturedFact("en")]
    public async Task LoadFromFileAsync_UserWantsAutoSaveFile_AutoSaveFileLoaded()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings { SecuredDrives = ["K:\\"] };
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi);
        dialogs.ShowMessageBox(
                Arg.Is<string>(
                    s => s.Contains("automatically generated backup file for project", StringComparison.Ordinal)),
                Arg.Any<string>(),
                MessageBoxButton.YesNo, MessageBoxImage.Question,
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.Yes);
        fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(new AccountingData().Serialize());
        fileSystem.FileExists("the.fileName").Returns(true);
        fileSystem.FileExists("the.fileName~").Returns(true);

        (await sut.Awaiting(x => x.LoadFromFileAsync("the.fileName")).Should()
                .CompleteWithinAsync(5.Seconds()))
            .Which.Should().Be(OperationResult.Completed);

        using var _ = new AssertionScope();
        sut.FileName.Should().Be("the.fileName");
        sut.IsModified.Should().BeTrue("changes are (still) not yet saved");
        sut.Settings.RecentProject.Should().Be("the.fileName");
        sut.Settings.RecentProjects.OfType<string>().Should().Equal("the.fileName");
        fileSystem.Received(1).ReadAllTextFromFile("the.fileName~");
    }

    [CulturedFact("en")]
    public async Task LoadFromFileAsync_UserDoesNotWantAutoSaveFileExists_ProjectFileLoaded()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings { SecuredDrives = ["K:\\"] };
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi);
        dialogs.ShowMessageBox(
                Arg.Is<string>(
                    s => s.Contains("automatically generated backup file for project", StringComparison.Ordinal)),
                Arg.Any<string>(),
                MessageBoxButton.YesNo, MessageBoxImage.Question,
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.No);
        fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(new AccountingData().Serialize());
        fileSystem.FileExists("the.fileName").Returns(true);
        fileSystem.FileExists("the.fileName~").Returns(true);

        (await sut.Awaiting(x => x.LoadFromFileAsync("the.fileName")).Should()
                .CompleteWithinAsync(10.Seconds()))
            .Which.Should().Be(OperationResult.Completed);

        using var _ = new AssertionScope();
        sut.FileName.Should().Be("the.fileName");
        sut.IsModified.Should().BeFalse();
        sut.Settings.RecentProject.Should().Be("the.fileName");
        sut.Settings.RecentProjects.OfType<string>().Should().Equal("the.fileName");
        fileSystem.Received(1).ReadAllTextFromFile("the.fileName");
        fileSystem.Received(1).FileDelete("the.fileName~");
    }

    [Theory]
    [InlineData("Cryptomator File System")]
    [InlineData("cryptoFs")]
    public async Task LoadFromFileAsync_NewFileOnSecureDrive_StoreOpenedAndFileLoaded(
        string cryptoDriveIdentifier)
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings { SecuredDrives = ["K:\\"] };
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi);
        fileSystem.FileExists(Arg.Is("K:\\the.fileName")).Returns(true);
        fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(new AccountingData().Serialize());
        fileSystem.GetDrives().Returns(
            _ =>
            {
                var func1 = new Func<string>(() => "Normal");
                var func2 = new Func<string>(() => cryptoDriveIdentifier);
                return new[] { (FilePath: "C:\\", GetFormat: func1), (FilePath: "K:\\", GetFormat: func2) };
            });

        (await sut.Awaiting(x => x.LoadFromFileAsync("K:\\the.fileName")).Should()
                .CompleteWithinAsync(10.Seconds()))
            .Which.Should().Be(OperationResult.Completed);

        sut.Settings.SecuredDrives.Should().BeEquivalentTo(new object[] { "K:\\" });
        dialogs.DidNotReceive().ShowMessageBox(
            Arg.Is<string>(s => s.Contains("Cryptomator", StringComparison.Ordinal)),
            Arg.Any<string>(),
            Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
            Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
        fileSystem.Received(1).ReadAllTextFromFile("K:\\the.fileName");
    }

    [CulturedFact("en")]
    public async Task LoadFromFileAsync_UserDoesNotWantSaveCurrentProject_LoadingAborted()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings { SecuredDrives = ["K:\\"] };
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi)
        {
            FileName = "old.fileName", IsModified = true
        };
        dialogs.ShowMessageBox(
                Arg.Is<string>(s => s.Contains("Project data has been changed.", StringComparison.Ordinal)),
                Arg.Any<string>(),
                MessageBoxButton.YesNo, MessageBoxImage.Question,
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.Cancel);
        fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(new AccountingData().Serialize());
        fileSystem.FileExists("new.fileName").Returns(true);

        (await sut.Awaiting(x => x.LoadFromFileAsync("the.fileName")).Should()
                .CompleteWithinAsync(10.Seconds()))
            .Which.Should().Be(OperationResult.Aborted);

        using var _ = new AssertionScope();
        sut.FileName.Should().Be("old.fileName", "the new file was not loaded");
        sut.IsModified.Should().BeTrue("changes are (still) not yet saved");
        fileSystem.DidNotReceive().ReadAllTextFromFile("the.fileName");
    }

    [Fact]
    public void Dispose_HappyPath_Completed()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi);

        sut.Invoking(x => x.Dispose()).Should().NotThrow();
    }

    [Fact]
    public async Task LoadFromFileAsync_HappyPath_FileLoaded()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings { SecuredDrives = ["K:\\"] };
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi);
        fileSystem.FileExists("the.fileName").Returns(true);
        fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(new AccountingData().Serialize());

        (await sut.Awaiting(x => x.LoadFromFileAsync("the.fileName")).Should()
                .CompleteWithinAsync(10.Seconds()))
            .Which.Should().Be(OperationResult.Completed);

        using var _ = new AssertionScope();
        sut.FileName.Should().Be("the.fileName");
        sut.IsModified.Should().BeFalse();
        sut.Settings.RecentProject.Should().Be("the.fileName");
        sut.Settings.RecentProjects.OfType<string>().Should().Equal("the.fileName");
        fileSystem.Received(1).ReadAllTextFromFile("the.fileName");
    }

    [Fact]
    public async Task LoadFromFileAsync_OtherProjectLoaded_ReservationFileRemoved()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var sut = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi)
        {
            FileName = "fileName1"
        };
        fileSystem.FileExists("fileName1#").Returns(true);
        fileSystem.FileExists("fileName2").Returns(true);
        fileSystem.ReadAllTextFromFile("fileName2").Returns(Samples.SampleProject.Serialize());

        (await sut.Awaiting(x => x.LoadFromFileAsync("fileName2")).Should()
                .CompleteWithinAsync(10.Seconds()))
            .Which.Should().Be(OperationResult.Completed);

        fileSystem.Received(1).FileDelete("fileName1#");
    }

    [Fact]
    public async Task LoadFromFileAsync_FullRecentList_NewFileOnTop()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings { SecuredDrives = ["K:\\"] };
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi)
        {
            Settings =
            {
                RecentProjects =
                [
                    "A",
                    "B",
                    "C",
                    "D",
                    "E",
                    "F",
                    "G",
                    "H",
                    "I",
                    "J"
                ]
            }
        };
        fileSystem.FileExists("the.fileName").Returns(true);
        fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(new AccountingData().Serialize());

        (await sut.Awaiting(x => x.LoadFromFileAsync("the.fileName")).Should()
                .CompleteWithinAsync(10.Seconds()))
            .Which.Should().Be(OperationResult.Completed);

        sut.Settings.RecentProjects.OfType<string>().Should()
            .Equal("the.fileName", "A", "B", "C", "D", "E", "F", "G", "H", "I");
    }

    [Fact]
    public async Task LoadFromFileAsync_MigrationRequired_ProjectModified()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings { SecuredDrives = ["K:\\"] };
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi);
        var accountingData = new AccountingData { Years = [new AccountingDataYear { Name = 2020 }] };
        fileSystem.FileExists("the.fileName").Returns(true);
        fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(accountingData.Serialize());

        (await sut.Awaiting(x => x.LoadFromFileAsync("the.fileName")).Should()
                .CompleteWithinAsync(10.Seconds()))
            .Which.Should().Be(OperationResult.Completed);

        sut.IsModified.Should().BeTrue();
    }

    [Fact]
    public async Task LoadFromFileAsync_KnownFileOnSecureDrive_StoreOpenedAndFileLoaded()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings { SecuredDrives = ["K:\\"] };
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi);
        dialogs.ShowMessageBox(
                Arg.Is<string>(s => s.Contains("Cryptomator", StringComparison.Ordinal)),
                Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.Yes);
        bool securedFileAvailable = false;
        fileSystem.FileExists(Arg.Is("K:\\the.fileName")).Returns(_ => securedFileAvailable);
        fileSystem.FileExists(
                Arg.Is<string>(s => s.Contains("cryptomator.exe", StringComparison.InvariantCultureIgnoreCase)))
            .Returns(true);
        Process cryptomator = null;
        processApi.Start(Arg.Any<ProcessStartInfo>()).Returns(
            _ =>
            {
                cryptomator = new Process();
                return cryptomator;
            });
        processApi.IsProcessWindowVisible(Arg.Any<Process>()).Returns(true);
        processApi.GetProcessByName(Arg.Any<string>()).Returns(cryptomator);
        processApi.When(x => x.BringProcessToFront(Arg.Any<Process>())).Do(_ => securedFileAvailable = true);
        fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(new AccountingData().Serialize());

        (await sut.Awaiting(x => x.LoadFromFileAsync("K:\\the.fileName")).Should()
                .CompleteWithinAsync(5.Seconds()))
            .Which.Should().Be(OperationResult.Completed);

        fileSystem.Received(1).ReadAllTextFromFile("K:\\the.fileName");
    }

    [Fact]
    public async Task OnProjectChanged_AutoSaveFileChanged_ChangeIgnored()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        Action<string> registeredCallback = null;
        fileSystem
            .When(x => x.StartMonitoring("the.fileName", Arg.Any<Action<string>>()))
            .Do(x => registeredCallback = x.ArgAt<Action<string>>(1));
        var clock = Substitute.For<IClock>();
        clock.Now().Returns(new DateTime(2024, 2, 17, 1, 2, 3, DateTimeKind.Local));
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi);
        fileSystem.FileExists("the.fileName").Returns(true);
        fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(Samples.SampleProject.Serialize());
        await sut.LoadFromFileAsync("the.fileName");

        registeredCallback.Invoke("the.fileName~");

        dialogs.DidNotReceive().ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
            Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
    }

    [Fact]
    public async Task OnProjectChanged_ProjectFileChangedUserDiscards_ProjectClosed()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        dialogs.ShowMessageBox(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MessageBoxButton>(), MessageBoxImage.Warning,
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.No);
        var fileSystem = Substitute.For<IFileSystem>();
        Action<string> registeredCallback = null;
        fileSystem
            .When(x => x.StartMonitoring("the.fileName", Arg.Any<Action<string>>()))
            .Do(x => registeredCallback = x.ArgAt<Action<string>>(1));
        var clock = Substitute.For<IClock>();
        clock.Now().Returns(new DateTime(2024, 2, 17, 1, 2, 3, DateTimeKind.Local));
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi);
        fileSystem.FileExists("the.fileName").Returns(true);
        fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(Samples.SampleProject.Serialize());
        await sut.LoadFromFileAsync("the.fileName");

        registeredCallback.Invoke("the.fileName");

        await sut.ProjectChangedHandlerTask;
        fileSystem.DidNotReceive().WriteAllTextIntoFile("the.fileName", Arg.Any<string>());
        sut.FileName.Should().Be("<new>");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task OnProjectChanged_ProjectFileChangedUserAccepts_ProjectClosed(int numberOfFileChanges)
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        dialogs.ShowMessageBox(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MessageBoxButton>(), MessageBoxImage.Warning,
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.Yes);
        var fileSystem = Substitute.For<IFileSystem>();
        Action<string> registeredCallback = null;
        fileSystem
            .When(x => x.StartMonitoring("the.fileName", Arg.Any<Action<string>>()))
            .Do(x => registeredCallback = x.ArgAt<Action<string>>(1));
        var clock = Substitute.For<IClock>();
        clock.Now().Returns(new DateTime(2024, 2, 17, 1, 2, 3, DateTimeKind.Local));
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi);
        fileSystem.FileExists("the.fileName").Returns(true);
        fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(Samples.SampleProject.Serialize());
        await sut.LoadFromFileAsync("the.fileName");

        // We would expect to get only one notification for changing a file.
        // But we see that changing a file most times create two notifications.
        // Here we simulate different scenarios.
        for (int i = 0; i < numberOfFileChanges; i++)
        {
            registeredCallback.Invoke("the.fileName");
        }

        await sut.ProjectChangedHandlerTask;
        fileSystem.Received(1).WriteAllTextIntoFile("the_20240217010203.acml", Arg.Any<string>());
        sut.FileName.Should().Be("the_20240217010203.acml");
    }

    [Fact]
    public async Task OnProjectChanged_ProjectFileChangedWhileSaving_NotReportedAsUnexpectedChange()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        dialogs.ShowMessageBox(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MessageBoxButton>(), MessageBoxImage.Warning,
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.Yes);
        var fileSystem = Substitute.For<IFileSystem>();
        Action<string> registeredCallback = null;
        fileSystem
            .When(x => x.StartMonitoring("the.fileName", Arg.Any<Action<string>>()))
            .Do(x => registeredCallback = x.ArgAt<Action<string>>(1));
        fileSystem
            .When(x => x.WriteAllTextIntoFile("the.fileName", Arg.Any<string>()))
            .Do(_ => registeredCallback.Invoke("the.fileName"));
        var clock = Substitute.For<IClock>();
        clock.Now().Returns(new DateTime(2024, 2, 17, 1, 2, 3, DateTimeKind.Local));
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi);
        fileSystem.FileExists("the.fileName").Returns(true);
        fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(Samples.SampleProject.Serialize());
        await sut.LoadFromFileAsync("the.fileName");

        (await sut.Awaiting(x => x.SaveProjectAsync()).Should().CompleteWithinAsync(1.Seconds()))
            .Which.Should().BeTrue();

        await sut.ProjectChangedHandlerTask;
        dialogs.DidNotReceive().ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
            Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
    }

    [Fact]
    public async Task TryCloseAsync_NotModified_ReturnsTrue()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var sut = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);

        (await sut.Awaiting(x => x.TryCloseAsync()).Should().CompleteWithinAsync(1.Seconds())).Which.Should().BeTrue();

        dialogs.DidNotReceive().ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
            Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
    }

    [Fact]
    public async Task TryCloseAsync_AnswerCancel_NotSavedAndReturnsFalse()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var sut = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        dialogs.ShowMessageBox(
                Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.Cancel);
        sut.LoadData(Samples.SampleProject);
        sut.IsModified = true;

        (await sut.Awaiting(x => x.TryCloseAsync()).Should().CompleteWithinAsync(1.Seconds())).Which.Should().BeFalse();

        dialogs.Received(1).ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
            Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
        fileSystem.DidNotReceive().WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task TryCloseAsync_AnswerNo_NotSavedAndReturnsTrue()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var sut = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi)
        {
            IsModified = true
        };
        dialogs.ShowMessageBox(
                Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.No);
        sut.LoadData(Samples.SampleProject);

        (await sut.Awaiting(x => x.TryCloseAsync()).Should().CompleteWithinAsync(1.Seconds())).Which.Should().BeTrue();

        dialogs.Received(1).ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
            Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
        fileSystem.DidNotReceive().WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task TryCloseAsync_AnswerYes_SavedAndReturnsTrue()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var sut = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi)
        {
            IsModified = true
        };
        dialogs.ShowMessageBox(
                Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.Yes);
        sut.LoadData(Samples.SampleProject);

        (await sut.Awaiting(x => x.TryCloseAsync()).Should().CompleteWithinAsync(5.Seconds())).Which.Should().BeTrue();

        dialogs.Received(1).ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
            Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
        fileSystem.Received(1).WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SaveProjectAsync_NotExisting_JustSaved()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi);
        fileSystem.GetLastWriteTime(Arg.Any<string>())
            .Returns(new DateTime(2020, 2, 29, 18, 45, 56, 0, 0, DateTimeKind.Local));
        sut.LoadData(Samples.SampleProject);

        (await sut.Awaiting(x => x.SaveProjectAsync()).Should().CompleteWithinAsync(10.Seconds()))
            .Which.Should().BeTrue();

        fileSystem.DidNotReceive().FileMove(Arg.Any<string>(), Arg.Any<string>());
        fileSystem.Received(1).WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>());
        fileSystem.DidNotReceive().FileDelete(Arg.Any<string>());
    }

    [Fact]
    public async Task SaveProjectAsync_NewProjectSaveAsDialogConfirmed_FileCreated()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        dialogs.ShowSaveFileDialog(Arg.Any<string>()).Returns((DialogResult.OK, "newFile.acml"));
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi);
        sut.NewProject();

        (await sut.Awaiting(x => x.SaveProjectAsync()).Should().CompleteWithinAsync(10.Seconds()))
            .Which.Should().BeTrue();

        dialogs.Received(1).ShowSaveFileDialog(Arg.Any<string>());
        fileSystem.Received(1).WriteAllTextIntoFile("newFile.acml", Arg.Any<string>());
    }

    [Fact]
    public async Task SaveProjectAsync_NewProjectSaveAsDialogCancelled_ProjectRemainsNew()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        dialogs.ShowSaveFileDialog(Arg.Any<string>()).Returns((DialogResult.Cancel, string.Empty));
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi);
        sut.NewProject();

        (await sut.Awaiting(x => x.SaveProjectAsync()).Should().CompleteWithinAsync(10.Seconds()))
            .Which.Should().BeFalse();

        dialogs.Received(1).ShowSaveFileDialog(Arg.Any<string>());
        fileSystem.DidNotReceive().WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>());
        sut.FileName.Should().Be("<new>");
    }

    [Fact]
    public async Task SaveProjectAsync_AutoSaveExisting_AutoSaveFileDeleted()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi);
        fileSystem.GetLastWriteTime(Arg.Any<string>())
            .Returns(new DateTime(2020, 2, 29, 18, 45, 56, DateTimeKind.Local));
        const string fileName = "project.name";
        fileSystem.FileExists(fileName + "~").Returns(true);
        sut.LoadData(Samples.SampleProject);
        sut.FileName = fileName;

        (await sut.Awaiting(x => x.SaveProjectAsync()).Should().CompleteWithinAsync(10.Seconds()))
            .Which.Should().BeTrue();

        fileSystem.DidNotReceive().FileMove(Arg.Any<string>(), Arg.Any<string>());
        fileSystem.Received(1).WriteAllTextIntoFile(fileName, Arg.Any<string>());
        fileSystem.Received(1).FileDelete(fileName + "~");
    }

    [Fact]
    public async Task SaveProjectAsync_ProjectExisting_SavedAfterBackup()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi);
        fileSystem.GetLastWriteTime(Arg.Any<string>())
            .Returns(new DateTime(2020, 2, 29, 18, 45, 56, DateTimeKind.Local));
        const string fileName = "project.name";
        fileSystem.FileExists(fileName).Returns(true);
        sut.LoadData(Samples.SampleProject);
        sut.FileName = fileName;

        (await sut.Awaiting(x => x.SaveProjectAsync()).Should().CompleteWithinAsync(10.Seconds()))
            .Which.Should().BeTrue();

        fileSystem.Received(1).FileMove(fileName, fileName + ".20200229184556");
        fileSystem.Received(1).WriteAllTextIntoFile(fileName, Arg.Any<string>());
        fileSystem.DidNotReceive().FileDelete(Arg.Any<string>());
    }

    [Fact]
    public async Task EditProjectOptions_Changed_ProjectModified()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi);
        sut.NewProject();
        windowManager.ShowDialogAsync(Arg.Any<ProjectOptionsViewModel>()).Returns(true);

        await sut.EditProjectOptionsAsync();

        sut.IsModified.Should().BeTrue();
    }

    [Fact]
    public async Task EditProjectOptions_Unchanged_ProjectNotModified()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi);
        sut.NewProject();
        windowManager.ShowDialogAsync(Arg.Any<ProjectOptionsViewModel>()).Returns(false);

        await sut.EditProjectOptionsAsync();

        sut.IsModified.Should().BeFalse();
    }

    [Fact]
    public async Task CloseYear_NonDefaultConfiguration_SettingsRestored()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi);
        sut.LoadData(Samples.SampleProject);
        sut.Storage.Accounts[^1].Account.Add(
            new AccountDefinition { ID = 99999, Name = "C2", Type = AccountDefinitionType.Carryforward });
        sut.Storage.Setup.Behavior.LastCarryForward = 99999;
        sut.Storage.Setup.Behavior.LastCarryForwardSpecified = true;
        sut.Storage.Setup.Behavior.OpeningTextPattern = OpeningTextOption.AccountName.ToString();
        CloseYearViewModel invokedViewModel = null;
        windowManager
            .ShowDialogAsync(Arg.Any<CloseYearViewModel>(), Arg.Any<object>(), Arg.Any<IDictionary<string, object>>())
            .Returns(
                info =>
                {
                    invokedViewModel = info.Arg<CloseYearViewModel>();
                    return false;
                });

        var result = await sut.Awaiting(x => x.CloseYearAsync()).Should().CompleteWithinAsync(1.Seconds());
        result.Subject.Should().BeFalse();

        invokedViewModel.Should().BeEquivalentTo(
            new { TextOption = new { Option = OpeningTextOption.AccountName }, RemoteAccount = new { ID = 99999 } });
    }

    [Fact]
    public void CrashSave_Unmodified_NothingSaved()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi);
        sut.LoadData(Samples.SampleProject);

        sut.CrashSave();

        fileSystem.DidNotReceive().WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void CrashSave_Modified_NothingSaved()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, clock, processApi);
        sut.LoadData(Samples.SampleProject);
        sut.IsModified = true;

        sut.CrashSave();

        fileSystem.Received(1).WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>());
    }
}
