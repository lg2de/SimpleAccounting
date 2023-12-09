// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Model;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using FluentAssertions;
using FluentAssertions.Extensions;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Infrastructure;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Presentation;
using lg2de.SimpleAccounting.Properties;
using lg2de.SimpleAccounting.UnitTests.Presentation;
using NSubstitute;
using Xunit;

public class ProjectDataTests
{
    [Fact]
    public async Task LoadProjectFromFileAsync_KnownFileOnSecureDrive_StoreOpenedAndFileLoaded()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings { SecuredDrives = ["K:\\"] };
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, processApi);
        dialogs.ShowMessageBox(
                Arg.Is<string>(s => s.Contains("Cryptomator", StringComparison.Ordinal)),
                Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.Yes);
        bool securedFileAvailable = false;
        fileSystem.FileExists(Arg.Is("K:\\the.fileName")).Returns(info => securedFileAvailable);
        fileSystem.FileExists(
                Arg.Is<string>(s => s.Contains("cryptomator.exe", StringComparison.InvariantCultureIgnoreCase)))
            .Returns(true);
        Process cryptomator = null;
        processApi.Start(Arg.Any<ProcessStartInfo>()).Returns(
            info =>
            {
                cryptomator = new Process();
                return cryptomator;
            });
        processApi.IsProcessWindowVisible(Arg.Any<Process>()).Returns(true);
        processApi.GetProcessByName(Arg.Any<string>()).Returns(cryptomator);
        processApi.When(x => x.BringProcessToFront(Arg.Any<Process>())).Do(info => securedFileAvailable = true);
        fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(new AccountingData().Serialize());

        (await sut.Awaiting(x => x.LoadFromFileAsync("K:\\the.fileName")).Should()
                .CompleteWithinAsync(5.Seconds()))
            .Which.Should().Be(OperationResult.Completed);

        fileSystem.Received(1).ReadAllTextFromFile("K:\\the.fileName");
    }

    [Fact]
    public void CanDiscardModifiedProject_Cancel_NotSavedAndReturnsFalse()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        dialogs.ShowMessageBox(
                Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.Cancel);
        var sut = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, processApi);
        sut.Load(Samples.SampleProject);
        sut.IsModified = true;

        sut.CanDiscardModifiedProject().Should().BeFalse();

        dialogs.Received(1).ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
            Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
        fileSystem.DidNotReceive().WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void SaveProject_NotExisting_JustSaved()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, processApi);
        fileSystem.GetLastWriteTime(Arg.Any<string>()).Returns(new DateTime(2020, 2, 29, 18, 45, 56, 0, 0, DateTimeKind.Local));
        sut.Load(Samples.SampleProject);

        sut.SaveProject();

        fileSystem.DidNotReceive().FileMove(Arg.Any<string>(), Arg.Any<string>());
        fileSystem.Received(1).WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>());
        fileSystem.DidNotReceive().FileDelete(Arg.Any<string>());
    }

    [Fact]
    public void SaveProject_NewProject_SaveAsDialog()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, processApi);
        sut.NewProject();

        sut.SaveProject();

        dialogs.Received(1).ShowSaveFileDialog(Arg.Any<string>());
        fileSystem.DidNotReceive().WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void EditProjectOptions_Changed_ProjectModified()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, processApi);
        sut.NewProject();
        windowManager.ShowDialog(Arg.Any<ProjectOptionsViewModel>()).Returns(true);

        sut.EditProjectOptions();

        sut.IsModified.Should().BeTrue();
    }

    [Fact]
    public void EditProjectOptions_Unchanged_ProjectNotModified()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, processApi);
        sut.NewProject();
        windowManager.ShowDialog(Arg.Any<ProjectOptionsViewModel>()).Returns(false);

        sut.EditProjectOptions();

        sut.IsModified.Should().BeFalse();
    }

    [Fact]
    public void CloseYear_NonDefaultConfiguration_SettingsRestored()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var sut = new ProjectData(settings, windowManager, dialogs, fileSystem, processApi);
        sut.Load(Samples.SampleProject);
        sut.Storage.Accounts[^1].Account.Add(
            new AccountDefinition { ID = 99999, Name = "C2", Type = AccountDefinitionType.Carryforward });
        sut.Storage.Setup.Behavior.LastCarryForward = 99999;
        sut.Storage.Setup.Behavior.LastCarryForwardSpecified = true;
        sut.Storage.Setup.Behavior.OpeningTextPattern = OpeningTextOption.AccountName.ToString();
        CloseYearViewModel invokedViewModel = null;
        windowManager
            .ShowDialog(Arg.Any<CloseYearViewModel>(), Arg.Any<object>(), Arg.Any<IDictionary<string, object>>())
            .Returns(
                info =>
                {
                    invokedViewModel = info.Arg<CloseYearViewModel>();
                    return false;
                });

        sut.CloseYear().Should().BeFalse();

        invokedViewModel.Should().BeEquivalentTo(
            new
            {
                TextOption = new { Option = OpeningTextOption.AccountName }, RemoteAccount = new { ID = 99999 }
            });
    }
}
