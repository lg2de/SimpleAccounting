// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Extensions;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Infrastructure;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Presentation;
using lg2de.SimpleAccounting.Properties;
using lg2de.SimpleAccounting.Reports;
using NSubstitute;
using Xunit;

public partial class ShellViewModelTests
{
    [Fact]
    public async Task OnInitialize_NoProject_Initialized()
    {
        var sut = CreateSut();

        await ((IActivate)sut).ActivateAsync();

        sut.DisplayName.Should().NotBeNullOrWhiteSpace();
    }

    [WpfFact]
    public async Task OnActivate_NewProject_ProjectLoadedAndAutoSaveActive()
    {
        var sut = CreateSut(out IFileSystem fileSystem);
        sut.ProjectData.AutoSaveInterval = 100.Milliseconds();
        sut.ProjectData.FileName = "new.project";
        var fileSaved = new TaskCompletionSource<bool>();
        fileSystem
            .When(x => x.WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>()))
            .Do(_ => fileSaved.SetResult(true));

        await ((IActivate)sut).ActivateAsync();
        sut.LoadingTask.Status.Should().Be(TaskStatus.RanToCompletion);
        sut.ProjectData.Load(new AccountingData());
        sut.ProjectData.IsModified = true;
        await fileSaved.Awaiting(x => x.Task).Should().CompleteWithinAsync(1.Seconds());

        using var _ = new AssertionScope();
        sut.ProjectData.IsModified.Should().BeTrue();
        fileSystem.Received(1).WriteAllTextIntoFile("new.project~", Arg.Any<string>());
    }

    [WpfFact]
    public async Task OnActivate_RecentProject_ProjectLoadedAndModifiedProjectAutoSaved()
    {
        var sut = CreateSut(out IFileSystem fileSystem);
        sut.ProjectData.AutoSaveInterval = 100.Milliseconds();
        sut.ProjectData.Settings.RecentProject = "recent.project";
        var sample = new AccountingData
        {
            Accounts =
            [
                new AccountingDataAccountGroup { Account = [new AccountDefinition { ID = 1, Name = "TheAccount" }] }
            ]
        };
        fileSystem.FileExists("recent.project").Returns(true);
        fileSystem.ReadAllTextFromFile("recent.project").Returns(sample.Serialize());
        var fileSaved = new TaskCompletionSource<bool>();
        fileSystem
            .When(x => x.WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>()))
            .Do(_ => fileSaved.SetResult(true));
        await ((IActivate)sut).ActivateAsync();
        await sut.Awaiting(x => x.LoadingTask).Should().CompleteWithinAsync(1.Seconds());
        sut.ProjectData.IsModified = true;

        await fileSaved.Awaiting(x => x.Task).Should().CompleteWithinAsync(
            1.Seconds(), "file should be saved by auto-save task");

        using var _ = new AssertionScope();
        sut.ProjectData.IsModified.Should()
            .BeTrue("the project is ONLY auto-saved and not saved to real project file");
        sut.Accounts.AccountList.Should().BeEquivalentTo(new[] { new { Name = "TheAccount" } });
        fileSystem.DidNotReceive().WriteAllTextIntoFile("recent.project", Arg.Any<string>());
        fileSystem.Received(1).WriteAllTextIntoFile("recent.project~", Arg.Any<string>());
    }

    [WpfFact]
    public async Task OnActivate_RecentProject_ProjectLoadedAndUnmodifiedProjectNotAutoSaved()
    {
        var sut = CreateSut(out IFileSystem fileSystem);
        sut.ProjectData.AutoSaveInterval = 10.Milliseconds();
        sut.ProjectData.Settings.RecentProject = "recent.project";
        var sample = new AccountingData
        {
            Accounts =
            [
                new AccountingDataAccountGroup { Account = [new AccountDefinition { ID = 1, Name = "TheAccount" }] }
            ]
        };
        fileSystem.FileExists("recent.project").Returns(true);
        fileSystem.ReadAllTextFromFile("recent.project").Returns(sample.Serialize());
        var fileSaved = new TaskCompletionSource<bool>();
        fileSystem
            .When(x => x.WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>()))
            .Do(_ => fileSaved.SetResult(true));
        await ((IActivate)sut).ActivateAsync();
        await sut.Awaiting(x => x.LoadingTask).Should().CompleteWithinAsync(10.Seconds());
        sut.ProjectData.IsModified = false;

        var delayTask = Task.Delay(200.Milliseconds());
        var completedTask = await Task.WhenAny(fileSaved.Task, delayTask);
        completedTask.Should().Be(delayTask, "file should not be saved");

        fileSystem.DidNotReceive().WriteAllTextIntoFile("recent.project~", Arg.Any<string>());
    }

    [WpfFact]
    public async Task OnActivate_TwoRecentProjectsOneOnSecuredDrive_AllProjectListed()
    {
        var busy = Substitute.For<IBusy>();
        var clock = Substitute.For<IClock>();
        var windowManager = Substitute.For<IWindowManager>();
        var applicationUpdate = Substitute.For<IApplicationUpdate>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings
        {
            RecentProject = "k:\\file2", RecentProjects = ["c:\\file1", "k:\\file2"], SecuredDrives = ["K:\\"]
        };
        var projectData = new ProjectData(settings, windowManager, dialogs, fileSystem, processApi);
        var accountsViewModel = new AccountsViewModel(windowManager, projectData);
        var sut =
            new ShellViewModel(
                projectData, busy,
                new MenuViewModel(projectData, busy, null!, clock, null!, null!), new FullJournalViewModel(projectData),
                new AccountJournalViewModel(projectData), accountsViewModel, applicationUpdate);
        dialogs.ShowMessageBox(
                Arg.Is<string>(s => s.Contains("Cryptomator", StringComparison.Ordinal)),
                Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.Yes);
        fileSystem.FileExists(Arg.Is("c:\\file1")).Returns(true);
        bool securedFileAvailable = false;
        fileSystem.FileExists(Arg.Is("k:\\file2")).Returns(_ => securedFileAvailable);
        var cryptomator = new Process();
        processApi.GetProcessByName(Arg.Any<string>()).Returns(cryptomator);
        processApi.When(x => x.BringProcessToFront(cryptomator)).Do(_ => securedFileAvailable = true);

        await ((IActivate)sut).ActivateAsync();
        await sut.LoadingTask;

        sut.Menu.RecentProjects.Select(x => x.Header).Should().Equal("c:\\file1", "k:\\file2");
        dialogs.Received(1).ShowMessageBox(
            Arg.Is<string>(s => s.Contains("Cryptomator", StringComparison.Ordinal)),
            Arg.Any<string>(),
            Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
            Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
    }

    [CulturedFact("en")]
    public async Task OnActivate_SampleProject_JournalsUpdates()
    {
        var sut = CreateSut();
        var project = Samples.SampleProject;
        project.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        sut.ProjectData.Load(project);

        await ((IActivate)sut).ActivateAsync();

        using var _ = new AssertionScope();
        sut.Accounts.AccountList.Select(x => x.Name).Should().Equal(
            "Bank account", "Salary", "Shoes", "Carryforward", "Bank credit",
            "Friends debit", "Active empty Asset", "Active empty Income", "Active empty Expense",
            "Active empty Credit", "Active empty Debit", "Active empty Carryforward");

        sut.FullJournal.Items.Should().BeEquivalentTo(
            new[]
            {
                new { Text = "Open 1", CreditAccount = "990 (Carryforward)", DebitAccount = "100 (Bank account)" },
                new { Text = "Open 2", CreditAccount = "5000 (Bank credit)", DebitAccount = "990 (Carryforward)" },
                new { Text = "Salary", CreditAccount = string.Empty, DebitAccount = "100 (Bank account)" },
                new { Text = "Salary1", CreditAccount = "400 (Salary)", DebitAccount = string.Empty },
                new { Text = "Salary2", CreditAccount = "400 (Salary)", DebitAccount = string.Empty },
                new
                {
                    Text = "Credit rate",
                    CreditAccount = "100 (Bank account)",
                    DebitAccount = "5000 (Bank credit)"
                },
                new { Text = "Shoes1", CreditAccount = string.Empty, DebitAccount = "600 (Shoes)" },
                new { Text = "Shoes2", CreditAccount = string.Empty, DebitAccount = "600 (Shoes)" },
                new { Text = "Shoes", CreditAccount = "100 (Bank account)", DebitAccount = string.Empty },
                new
                {
                    Text = "Rent to friend",
                    CreditAccount = "100 (Bank account)",
                    DebitAccount = "6000 (Friends debit)"
                }
            });
        sut.AccountJournal.Items.Should().BeEquivalentTo(
            new object[]
            {
                new { Text = "Open 1", RemoteAccount = "990 (Carryforward)", IsEvenRow = false },
                new { Text = "Salary", RemoteAccount = "Various", IsEvenRow = true },
                new { Text = "Credit rate", RemoteAccount = "5000 (Bank credit)", IsEvenRow = false },
                new { Text = "Shoes", RemoteAccount = "Various", IsEvenRow = true },
                new { Text = "Rent to friend", RemoteAccount = "6000 (Friends debit)", IsEvenRow = false },
                new { Text = "Total", IsEvenRow = false }, new { Text = "Balance", IsEvenRow = false }
            });
    }

    [Fact]
    public async Task OnActivate_TwoRecentProjectsOneExisting_AllProjectListed()
    {
        var sut = CreateSut(out IFileSystem fileSystem);
        sut.ProjectData.Settings.RecentProjects = ["file1", "file2"];
        fileSystem.FileExists(Arg.Is("file1")).Returns(true);
        fileSystem.FileExists(Arg.Is("file2")).Returns(false);

        await ((IActivate)sut).ActivateAsync();

        // even the file is not available currently it should not be removed immediately from menu
        sut.Menu.RecentProjects.Select(x => x.Header).Should().Equal("file1", "file2");
    }

    [Fact]
    public async Task RecentFileCommand_NonExisting_ProjectRemovedFromList()
    {
        var sut = CreateSut(out IFileSystem fileSystem);
        sut.ProjectData.Settings.RecentProjects = ["file1", "file2"];
        fileSystem.FileExists(Arg.Is("file1")).Returns(true);
        fileSystem.FileExists(Arg.Is("file2")).Returns(false);
        fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(new AccountingData().Serialize());
        await ((IActivate)sut).ActivateAsync();
        sut.Menu.RecentProjects.Select(x => x.Header).Should().Equal("file1", "file2");

        foreach (var viewModel in sut.Menu.RecentProjects.ToList())
        {
            await viewModel.Command.ExecuteAsync(null);
        }

        sut.Menu.RecentProjects.Select(x => x.Header).Should().BeEquivalentTo("file1");
        sut.ProjectData.Settings.RecentProjects.OfType<string>().Should().BeEquivalentTo("file1");
    }

    [Fact]
    public async Task RecentFileCommand_FileOnSecureDriveNotStarted_ProjectKept()
    {
        var sut = CreateSut(out IDialogs dialogs, out IFileSystem fileSystem);
        sut.ProjectData.Settings.RecentProjects = ["K:\\file1", "file2"];
        sut.ProjectData.Settings.SecuredDrives = ["K:\\"];
        fileSystem.FileExists(Arg.Is("K:\\file1")).Returns(false);
        fileSystem.FileExists(Arg.Is("file2")).Returns(true);
        fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(new AccountingData().Serialize());
        dialogs.ShowMessageBox(
                Arg.Is<string>(s => s.Contains("Cryptomator", StringComparison.Ordinal)),
                Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.No);
        await ((IActivate)sut).ActivateAsync();
        sut.Menu.RecentProjects.Select(x => x.Header).Should().Equal("K:\\file1", "file2");

        foreach (var viewModel in sut.Menu.RecentProjects.ToList())
        {
            await viewModel.Command.ExecuteAsync(null);
        }

        sut.Menu.RecentProjects.Select(x => x.Header).Should().BeEquivalentTo("K:\\file1", "file2");
        sut.ProjectData.Settings.RecentProjects.OfType<string>().Should().BeEquivalentTo("K:\\file1", "file2");
    }

    [Fact]
    public async Task OnDeactivate_HappyPath_Completes()
    {
        var sut = CreateSut();
        await ((IActivate)sut).ActivateAsync();

        var task = Task.Run(() => ((IDeactivate)sut).DeactivateAsync(close: true));

        await task.Awaiting(x => x).Should().CompleteWithinAsync(10.Seconds());
    }

    [CulturedFact("en")]
    public void AddBooking_FirstBooking_JournalsUpdated()
    {
        var sut = CreateSut();
        sut.ProjectData.Load(Samples.SampleProject);
        var booking = new AccountingDataJournalBooking
        {
            Date = Samples.BaseDate + 401,
            ID = 4567,
            Credit = [new BookingValue { Account = 990, Text = "Init", Value = 42 }],
            Debit = [new BookingValue { Account = 100, Text = "Init", Value = 42 }]
        };

        using var fullJournalMonitor = sut.FullJournal.Monitor();
        using var accountJournalMonitor = sut.AccountJournal.Monitor();
        sut.ProjectData.AddBooking(booking, updateJournal: true);

        using var _ = new AssertionScope();
        sut.FullJournal.Items.Should().BeEquivalentTo(
            new[]
            {
                new
                {
                    Identifier = 4567,
                    Date = new DateTime(DateTime.Now.Year, 4, 1, 0, 0, 0, DateTimeKind.Local),
                    Text = "Init",
                    Value = 0.42,
                    CreditAccount = "990 (Carryforward)",
                    DebitAccount = "100 (Bank account)"
                }
            });
        fullJournalMonitor.Should().RaisePropertyChangeFor(x => x.SelectedItem);
        sut.FullJournal.SelectedItem.Should().BeEquivalentTo(new { Identifier = 4567 });
        sut.AccountJournal.Items.Should().BeEquivalentTo(
            new object[]
            {
                new
                {
                    Identifier = 4567,
                    Date = new DateTime(DateTime.Now.Year, 4, 1, 0, 0, 0, DateTimeKind.Local),
                    Text = "Init",
                    CreditValue = 0.0,
                    DebitValue = 0.42,
                    RemoteAccount = "990 (Carryforward)"
                },
                new { Text = "Total", IsSummary = true, CreditValue = 0.0, DebitValue = 0.42 },
                new { Text = "Balance", IsSummary = true, CreditValue = 0.0, DebitValue = 0.42 }
            });
        accountJournalMonitor.Should().RaisePropertyChangeFor(x => x.SelectedItem);
        sut.AccountJournal.SelectedItem.Should().BeEquivalentTo(new { Identifier = 4567 });
    }

    [CulturedFact("en")]
    public void AddBooking_BookingWithoutCurrentAccount_AccountJournalUnchanged()
    {
        var sut = CreateSut();
        sut.ProjectData.Load(Samples.SampleProject);
        var booking = new AccountingDataJournalBooking
        {
            Date = Samples.BaseDate + 401,
            ID = 4567,
            Credit = [new BookingValue { Account = 990, Text = "Init", Value = 42 }],
            Debit = [new BookingValue { Account = 100, Text = "Init", Value = 42 }]
        };
        sut.Accounts.SelectedAccount = sut.Accounts.AccountList[^1];

        using var monitor1 = sut.AccountJournal.Monitor();
        using var monitor2 = sut.AccountJournal.Items.Monitor();
        sut.ProjectData.AddBooking(booking, updateJournal: true);

        using var _ = new AssertionScope();
        monitor1.Should().NotRaisePropertyChangeFor(x => x.SelectedItem);
        monitor2.Should().NotRaise(nameof(sut.AccountJournal.Items.CollectionChanged));
    }

    [Fact]
    public void ShowInactiveAccounts_SetTrue_InactiveAccountsGetVisible()
    {
        var sut = CreateSut();
        var project = Samples.SampleProject;
        project.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        sut.ProjectData.Load(project);

        sut.Accounts.ShowInactiveAccounts = true;

        using var _ = new AssertionScope();

        sut.Accounts.AccountList.Select(x => x.Name).Should().Equal(
            "Bank account", "Salary", "Shoes", "Carryforward", "Bank credit", "Friends debit",
            "Active empty Asset", "Active empty Income", "Active empty Expense", "Active empty Credit",
            "Active empty Debit", "Active empty Carryforward",
            "Inactive");
    }

    [Fact]
    public async Task CanClose_UnmodifiedProject_CloseConfirmed()
    {
        var sut = CreateSut();

        var result = await sut.CanCloseAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanClose_ModifiedProjectAbort_CloseRejected()
    {
        var sut = CreateSut();
        sut.ProjectData.IsModified = true;

        var result = await sut.CanCloseAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanClose_AutoSaveFileExists_FileRemoved()
    {
        var sut = CreateSut(out IFileSystem fileSystem);
        fileSystem.FileExists(sut.ProjectData.AutoSaveFileName).Returns(true);

        var result = await sut.CanCloseAsync();

        result.Should().BeTrue();
        fileSystem.Received(1).FileDelete(sut.ProjectData.AutoSaveFileName);
    }

    [Fact]
    public void Dispose_HappyPath_Completed()
    {
        var sut = CreateSut();

        sut.Invoking(x => x.Dispose()).Should().NotThrow();
    }

    private static ShellViewModel CreateSut()
    {
        var busy = Substitute.For<IBusy>();
        var windowManager = Substitute.For<IWindowManager>();
        var reportFactory = Substitute.For<IReportFactory>();
        var clock = Substitute.For<IClock>();
        var applicationUpdate = Substitute.For<IApplicationUpdate>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var projectData = new ProjectData(settings, windowManager, dialogs, fileSystem, processApi);
        var accountsViewModel = new AccountsViewModel(windowManager, projectData);
        var sut =
            new ShellViewModel(
                projectData,
                busy,
                new MenuViewModel(projectData, busy, reportFactory, clock, processApi, dialogs),
                new FullJournalViewModel(projectData),
                new AccountJournalViewModel(projectData), accountsViewModel, applicationUpdate);
        return sut;
    }

    private static ShellViewModel CreateSut(out IWindowManager windowManager)
    {
        var busy = Substitute.For<IBusy>();
        windowManager = Substitute.For<IWindowManager>();
        var reportFactory = Substitute.For<IReportFactory>();
        var clock = Substitute.For<IClock>();
        var applicationUpdate = Substitute.For<IApplicationUpdate>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var projectData = new ProjectData(settings, windowManager, dialogs, fileSystem, processApi);
        var accountsViewModel = new AccountsViewModel(windowManager, projectData);
        var sut =
            new ShellViewModel(
                projectData, busy,
                new MenuViewModel(projectData, busy, reportFactory, clock, processApi, dialogs),
                new FullJournalViewModel(projectData), new AccountJournalViewModel(projectData),
                accountsViewModel, applicationUpdate);
        return sut;
    }

    private static ShellViewModel CreateSut(out IWindowManager windowManager, out IClock clock)
    {
        var busy = Substitute.For<IBusy>();
        windowManager = Substitute.For<IWindowManager>();
        var reportFactory = Substitute.For<IReportFactory>();
        clock = Substitute.For<IClock>();
        var applicationUpdate = Substitute.For<IApplicationUpdate>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var projectData = new ProjectData(settings, windowManager, dialogs, fileSystem, processApi);
        var accountsViewModel = new AccountsViewModel(windowManager, projectData);
        var sut =
            new ShellViewModel(
                projectData, busy,
                new MenuViewModel(projectData, busy, reportFactory, clock, processApi, dialogs),
                new FullJournalViewModel(projectData), new AccountJournalViewModel(projectData),
                accountsViewModel, applicationUpdate);
        return sut;
    }

    private static ShellViewModel CreateSut(out IApplicationUpdate applicationUpdate, out IDialogs dialogs)
    {
        var busy = Substitute.For<IBusy>();
        var windowManager = Substitute.For<IWindowManager>();
        var reportFactory = Substitute.For<IReportFactory>();
        var clock = Substitute.For<IClock>();
        applicationUpdate = Substitute.For<IApplicationUpdate>();
        dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var projectData = new ProjectData(settings, windowManager, dialogs, fileSystem, processApi);
        var accountsViewModel = new AccountsViewModel(windowManager, projectData);
        var sut =
            new ShellViewModel(
                projectData, busy,
                new MenuViewModel(projectData, busy, reportFactory, clock, processApi, dialogs),
                new FullJournalViewModel(projectData), new AccountJournalViewModel(projectData),
                accountsViewModel, applicationUpdate);
        return sut;
    }

    private static ShellViewModel CreateSut(out IFileSystem fileSystem)
    {
        var busy = Substitute.For<IBusy>();
        var windowManager = Substitute.For<IWindowManager>();
        var reportFactory = Substitute.For<IReportFactory>();
        var clock = Substitute.For<IClock>();
        var applicationUpdate = Substitute.For<IApplicationUpdate>();
        var dialogs = Substitute.For<IDialogs>();
        fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var projectData = new ProjectData(settings, windowManager, dialogs, fileSystem, processApi);
        var accountsViewModel = new AccountsViewModel(windowManager, projectData);
        var sut =
            new ShellViewModel(
                projectData, busy,
                new MenuViewModel(projectData, busy, reportFactory, clock, processApi, dialogs),
                new FullJournalViewModel(projectData), new AccountJournalViewModel(projectData),
                accountsViewModel, applicationUpdate);
        return sut;
    }

    private static ShellViewModel CreateSut(out IDialogs dialogs, out IFileSystem fileSystem)
    {
        var busy = Substitute.For<IBusy>();
        var windowManager = Substitute.For<IWindowManager>();
        var reportFactory = Substitute.For<IReportFactory>();
        var clock = Substitute.For<IClock>();
        var applicationUpdate = Substitute.For<IApplicationUpdate>();
        dialogs = Substitute.For<IDialogs>();
        fileSystem = Substitute.For<IFileSystem>();
        var processApi = Substitute.For<IProcess>();
        var settings = new Settings();
        var projectData = new ProjectData(settings, windowManager, dialogs, fileSystem, processApi);
        var accountsViewModel = new AccountsViewModel(windowManager, projectData);
        var sut =
            new ShellViewModel(
                projectData, busy,
                new MenuViewModel(projectData, busy, reportFactory, clock, processApi, dialogs),
                new FullJournalViewModel(projectData), new AccountJournalViewModel(projectData),
                accountsViewModel, applicationUpdate);
        return sut;
    }
}
