﻿// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
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
        public void OnInitialize_NoProject_Initialized()
        {
            var sut = CreateSut();

            ((IActivate)sut).Activate();

            sut.DisplayName.Should().NotBeNullOrWhiteSpace();
        }

        [WpfFact]
        public async Task OnActivate_NewProject_ProjectLoadedAndAutoSaveActive()
        {
            var sut = CreateSut(out IFileSystem fileSystem);
            sut.AutoSaveInterval = 100.Milliseconds();
            sut.FileName = "new.project";
            var fileSaved = new TaskCompletionSource<bool>();
            fileSystem
                .When(x => x.WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>()))
                .Do(x => fileSaved.SetResult(true));

            ((IActivate)sut).Activate();
            sut.LoadingTask.Status.Should().Be(TaskStatus.RanToCompletion);
            sut.LoadProjectData(new AccountingData());
            sut.IsDocumentModified = true;
            await fileSaved.Awaiting(x => x.Task).Should().CompleteWithinAsync(1.Seconds());

            using var _ = new AssertionScope();
            sut.IsDocumentModified.Should().BeTrue();
            fileSystem.Received(1).WriteAllTextIntoFile("new.project~", Arg.Any<string>());
        }

        [WpfFact]
        public async Task OnActivate_RecentProject_ProjectLoadedAndAutoSaveActive()
        {
            var sut = CreateSut(out IFileSystem fileSystem);
            sut.AutoSaveInterval = 100.Milliseconds();
            sut.Settings.RecentProject = "recent.project";
            var sample = new AccountingData
            {
                Accounts = new List<AccountingDataAccountGroup>
                {
                    new AccountingDataAccountGroup
                    {
                        Account = new List<AccountDefinition>
                        {
                            new AccountDefinition { ID = 1, Name = "TheAccount" }
                        }
                    }
                }
            };
            fileSystem.ReadAllTextFromFile("recent.project").Returns(sample.Serialize());
            var fileSaved = new TaskCompletionSource<bool>();
            fileSystem
                .When(x => x.WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>()))
                .Do(x => fileSaved.SetResult(true));

            ((IActivate)sut).Activate();
            await sut.Awaiting(x => x.LoadingTask).Should().CompleteWithinAsync(1.Seconds());
            sut.IsDocumentModified = true;
            await fileSaved.Awaiting(x => x.Task).Should().CompleteWithinAsync(1.Seconds());

            using var _ = new AssertionScope();
            sut.IsDocumentModified.Should().BeTrue();
            sut.AccountList.Should().BeEquivalentTo(new { Name = "TheAccount" });
            fileSystem.Received(1).WriteAllTextIntoFile("recent.project~", Arg.Any<string>());
        }

        [WpfFact]
        public async Task OnActivate_TwoRecentProjectsOneOnSecuredDrive_AllProjectListed()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var applicationUpdate = Substitute.For<IApplicationUpdate>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var processApi = Substitute.For<IProcess>();
            var sut =
                new ShellViewModel(windowManager, reportFactory, applicationUpdate, messageBox, fileSystem, processApi)
                {
                    Settings = new Settings
                    {
                        RecentProject = "k:\\file2",
                        RecentProjects = new StringCollection { "c:\\file1", "k:\\file2" },
                        SecuredDrives = new StringCollection { "K:\\" }
                    }
                };
            messageBox.Show(
                    Arg.Is<string>(s => s.Contains("Cryptomator")),
                    Arg.Any<string>(),
                    Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                    Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
                .Returns(MessageBoxResult.Yes);
            fileSystem.FileExists(Arg.Is("c:\\file1")).Returns(true);
            bool securedFileAvailable = false;
            fileSystem.FileExists(Arg.Is("k:\\file2")).Returns(info => securedFileAvailable);
            var cryptomator = new Process();
            processApi.GetProcessByName(Arg.Any<string>()).Returns(cryptomator);
            processApi.When(x => x.BringProcessToFront(cryptomator)).Do(info => securedFileAvailable = true);

            ((IActivate)sut).Activate();
            await sut.LoadingTask;

            sut.RecentProjects?.Select(x => x.Header).Should().Equal("c:\\file1", "k:\\file2");
            messageBox.Received(1).Show(
                Arg.Is<string>(s => s.Contains("Cryptomator")),
                Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
        }

        [Fact]
        public void OnActivate_SampleProject_JournalsUpdates()
        {
            var sut = CreateSut();
            var project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            sut.LoadProjectData(project);

            ((IActivate)sut).Activate();

            using var _ = new AssertionScope();
            sut.AccountList.Should().BeEquivalentTo(
                new { Name = "Bank account" },
                new { Name = "Salary" },
                new { Name = "Shoes" },
                new { Name = "Carryforward" },
                new { Name = "Bank credit" },
                new { Name = "Friends debit" });

            sut.FullJournal.Should().BeEquivalentTo(
                new { Text = "Open 1", CreditAccount = "990 (Carryforward)", DebitAccount = "100 (Bank account)" },
                new { Text = "Open 2", CreditAccount = "5000 (Bank credit)", DebitAccount = "990 (Carryforward)" },
                new { Text = "Salary", CreditAccount = string.Empty, DebitAccount = "100 (Bank account)" },
                new { Text = "Salary1", CreditAccount = "400 (Salary)", DebitAccount = string.Empty },
                new { Text = "Salary2", CreditAccount = "400 (Salary)", DebitAccount = string.Empty },
                new { Text = "Credit rate", CreditAccount = "100 (Bank account)", DebitAccount = "5000 (Bank credit)" },
                new { Text = "Shoes1", CreditAccount = string.Empty, DebitAccount = "600 (Shoes)" },
                new { Text = "Shoes2", CreditAccount = string.Empty, DebitAccount = "600 (Shoes)" },
                new { Text = "Shoes", CreditAccount = "100 (Bank account)", DebitAccount = string.Empty },
                new
                {
                    Text = "Rent to friend",
                    CreditAccount = "100 (Bank account)",
                    DebitAccount = "6000 (Friends debit)"
                });
            sut.AccountJournal.Should().BeEquivalentTo(
                new { Text = "Open 1", RemoteAccount = "990 (Carryforward)" },
                new { Text = "Salary", RemoteAccount = "Diverse" },
                new { Text = "Credit rate", RemoteAccount = "5000 (Bank credit)" },
                new { Text = "Shoes", RemoteAccount = "Diverse" },
                new { Text = "Rent to friend", RemoteAccount = "6000 (Friends debit)" },
                new { Text = "Summe" },
                new { Text = "Saldo" });
        }

        [Fact]
        public void OnActivate_TwoRecentProjectsOneExisting_ExistingProjectListed()
        {
            var sut = CreateSut(out IFileSystem fileSystem);
            sut.Settings = new Settings { RecentProjects = new StringCollection { "file1", "file2" } };
            fileSystem.FileExists(Arg.Is("file1")).Returns(true);

            ((IActivate)sut).Activate();

            sut.RecentProjects?.Select(x => x.Header).Should().Equal("file1");
        }

        [Fact]
        public void OnDeactivate_HappyPath_Completes()
        {
            var sut = CreateSut();
            ((IActivate)sut).Activate();

            var task = Task.Run(() => ((IDeactivate)sut).Deactivate(close: true));

            task.Awaiting(x => x).Should().CompleteWithin(1.Seconds());
        }

        [Fact]
        public void AddBooking_FirstBooking_JournalsUpdated()
        {
            var sut = CreateSut();
            sut.LoadProjectData(Samples.SampleProject);
            var booking = new AccountingDataJournalBooking
            {
                Date = Samples.BaseDate + 401,
                ID = 4567,
                Credit = new List<BookingValue> { new BookingValue { Account = 990, Text = "Init", Value = 42 } },
                Debit = new List<BookingValue> { new BookingValue { Account = 100, Text = "Init", Value = 42 } }
            };

            using var monitor = sut.Monitor();
            sut.AddBooking(booking);

            using var _ = new AssertionScope();
            sut.FullJournal.Should().BeEquivalentTo(
                new
                {
                    Identifier = 4567,
                    Date = new DateTime(DateTime.Now.Year, 4, 1),
                    Text = "Init",
                    Value = 0.42,
                    CreditAccount = "990 (Carryforward)",
                    DebitAccount = "100 (Bank account)"
                });
            monitor.Should().RaisePropertyChangeFor(x => x.SelectedFullJournalEntry);
            sut.SelectedFullJournalEntry.Should().BeEquivalentTo(new { Identifier = 4567 });
            sut.AccountJournal.Should().BeEquivalentTo(
                new
                {
                    Identifier = 4567,
                    Date = new DateTime(DateTime.Now.Year, 4, 1),
                    Text = "Init",
                    CreditValue = 0.0,
                    DebitValue = 0.42,
                    RemoteAccount = "990 (Carryforward)"
                },
                new { Text = "Summe", IsSummary = true, CreditValue = 0.0, DebitValue = 0.42 },
                new { Text = "Saldo", IsSummary = true, CreditValue = 0.0, DebitValue = 0.42 });
            monitor.Should().RaisePropertyChangeFor(x => x.SelectedAccountJournalEntry);
            sut.SelectedAccountJournalEntry.Should().BeEquivalentTo(new { Identifier = 4567 });
        }

        [Fact]
        public void CheckSaveProject_AnswerNo_NotSavedAndReturnsTrue()
        {
            var sut = CreateSut(out var messageBox, out var fileSystem);
            sut.IsDocumentModified = true;
            messageBox.Show(
                    Arg.Any<string>(), Arg.Any<string>(),
                    Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                    Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
                .Returns(MessageBoxResult.No);
            sut.LoadProjectData(Samples.SampleProject);

            sut.CheckSaveProject().Should().BeTrue();

            messageBox.Received(1).Show(
                Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
            fileSystem.DidNotReceive().WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public void CheckSaveProject_AnswerYes_SavedAndReturnsTrue()
        {
            var sut = CreateSut(out var messageBox, out var fileSystem);
            sut.IsDocumentModified = true;
            messageBox.Show(
                    Arg.Any<string>(), Arg.Any<string>(),
                    Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                    Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
                .Returns(MessageBoxResult.Yes);
            sut.LoadProjectData(Samples.SampleProject);

            sut.CheckSaveProject().Should().BeTrue();

            messageBox.Received(1).Show(
                Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
            fileSystem.Received(1).WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public void CheckSaveProject_Cancel_NotSavedAndReturnsFalse()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var applicationUpdate = Substitute.For<IApplicationUpdate>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var processApi = Substitute.For<IProcess>();
            messageBox.Show(
                    Arg.Any<string>(), Arg.Any<string>(),
                    Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                    Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
                .Returns(MessageBoxResult.Cancel);
            var sut = new ShellViewModel(
                windowManager, reportFactory, applicationUpdate, messageBox, fileSystem, processApi)
            {
                Settings = new Settings(),
                IsDocumentModified = true
            };
            sut.LoadProjectData(Samples.SampleProject);

            sut.CheckSaveProject().Should().BeFalse();

            messageBox.Received(1).Show(
                Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
            fileSystem.DidNotReceive().WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public void CheckSaveProject_NotModified_ReturnsTrue()
        {
            var sut = CreateSut(out IMessageBox messageBox);

            sut.CheckSaveProject().Should().BeTrue();

            messageBox.DidNotReceive().Show(
                Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
        }

        [Fact]
        public async Task LoadProjectFromFileAsync_HappyPath_FileLoaded()
        {
            var sut = CreateSut(out IFileSystem fileSystem);
            fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(new AccountingData().Serialize());

            await sut.LoadProjectFromFileAsync("the.fileName");

            using var _ = new AssertionScope();
            sut.FileName.Should().Be("the.fileName");
            sut.IsDocumentModified.Should().BeFalse();
            sut.Settings.RecentProject.Should().Be("the.fileName");
            sut.Settings.RecentProjects.OfType<string>().Should().Equal("the.fileName");
            fileSystem.Received(1).ReadAllTextFromFile("the.fileName");
        }

        [Fact]
        public async Task LoadProjectFromFileAsync_AutoSaveFileExistsYes_AutoSaveFileLoaded()
        {
            var sut = CreateSut(out var messageBox, out var fileSystem);
            messageBox.Show(
                    Arg.Any<string>(), Arg.Any<string>(),
                    MessageBoxButton.YesNo, MessageBoxImage.Question,
                    Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
                .Returns(MessageBoxResult.Yes);
            fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(new AccountingData().Serialize());
            fileSystem.FileExists("the.fileName~").Returns(true);

            await sut.LoadProjectFromFileAsync("the.fileName");

            using var _ = new AssertionScope();
            sut.FileName.Should().Be("the.fileName");
            sut.IsDocumentModified.Should().BeTrue("changes are (still) not yet saved");
            sut.Settings.RecentProject.Should().Be("the.fileName");
            sut.Settings.RecentProjects.OfType<string>().Should().Equal("the.fileName");
            fileSystem.Received(1).ReadAllTextFromFile("the.fileName~");
        }

        [Fact]
        public async Task LoadProjectFromFileAsync_AutoSaveFileExistsNo_AutoSaveFileLoaded()
        {
            var sut = CreateSut(out var messageBox, out var fileSystem);
            messageBox.Show(
                    Arg.Any<string>(), Arg.Any<string>(),
                    MessageBoxButton.YesNo, MessageBoxImage.Question,
                    Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
                .Returns(MessageBoxResult.No);
            fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(new AccountingData().Serialize());
            fileSystem.FileExists("the.fileName~").Returns(true);

            await sut.Awaiting(x => x.LoadProjectFromFileAsync("the.fileName")).Should()
                .CompleteWithinAsync(1.Seconds());

            using var _ = new AssertionScope();
            sut.FileName.Should().Be("the.fileName");
            sut.IsDocumentModified.Should().BeFalse();
            sut.Settings.RecentProject.Should().Be("the.fileName");
            sut.Settings.RecentProjects.OfType<string>().Should().Equal("the.fileName");
            fileSystem.Received(1).ReadAllTextFromFile("the.fileName");
        }

        [Fact]
        public async Task LoadProjectFromFileAsync_NewFileOnSecureDrive_StoreOpenedAndFileLoaded()
        {
            var sut = CreateSut(out var messageBox, out var fileSystem);
            fileSystem.FileExists(Arg.Is("K:\\the.fileName")).Returns(true);
            fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(new AccountingData().Serialize());
            fileSystem.GetDrives().Returns(
                new[] { (FilePath: "C:\\", Format: "Normal"), (FilePath: "K:\\", Format: "Cryptomator File System") });

            await sut.Awaiting(x => x.LoadProjectFromFileAsync("K:\\the.fileName")).Should()
                .CompleteWithinAsync(1.Seconds());

            sut.Settings.SecuredDrives.Should().Equal(new object[] { "K:\\" });
            messageBox.DidNotReceive().Show(
                Arg.Is<string>(s => s.Contains("Cryptomator")),
                Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
            fileSystem.Received(1).ReadAllTextFromFile("K:\\the.fileName");
        }

        [Fact]
        public async Task LoadProjectFromFileAsync_KnownFileOnSecureDrive_StoreOpenedAndFileLoaded()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var applicationUpdate = Substitute.For<IApplicationUpdate>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var processApi = Substitute.For<IProcess>();
            var sut = new ShellViewModel(
                windowManager, reportFactory, applicationUpdate, messageBox, fileSystem, processApi)
            {
                Settings = new Settings { SecuredDrives = new StringCollection { "K:\\" } }
            };
            messageBox.Show(
                    Arg.Is<string>(s => s.Contains("Cryptomator")),
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

            await sut.Awaiting(x => x.LoadProjectFromFileAsync("K:\\the.fileName")).Should()
                .CompleteWithinAsync(1.Seconds());

            fileSystem.Received(1).ReadAllTextFromFile("K:\\the.fileName");
        }

        [Fact]
        public async Task LoadProjectFromFileAsync_FullRecentList_NewFileOnTop()
        {
            var sut = CreateSut(out IFileSystem fileSystem);
            sut.Settings.RecentProjects = new StringCollection
            {
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
            };
            fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(new AccountingData().Serialize());

            await sut.LoadProjectFromFileAsync("the.fileName");

            sut.Settings.RecentProjects.OfType<string>().Should()
                .Equal("the.fileName", "A", "B", "C", "D", "E", "F", "G", "H", "I");
        }

        [Fact]
        public async Task LoadProjectFromFileAsync_MigrationRequired_ProjectModified()
        {
            var sut = CreateSut(out IFileSystem fileSystem);
            var accountingData = new AccountingData
            {
                Years = new List<AccountingDataYear> { new AccountingDataYear { Name = 2020 } }
            };
            fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(accountingData.Serialize());

            await sut.LoadProjectFromFileAsync("the.fileName");

            sut.IsDocumentModified.Should().BeTrue();
        }

        [Fact]
        public void SaveProject_AutoSaveExisting_AutoSaveFileDeleted()
        {
            var sut = CreateSut(out IFileSystem fileSystem);
            fileSystem.GetLastWriteTime(Arg.Any<string>()).Returns(new DateTime(2020, 2, 29, 18, 45, 56));
            var fileName = "project.name";
            fileSystem.FileExists(fileName + "~").Returns(true);
            sut.LoadProjectData(Samples.SampleProject);
            sut.FileName = fileName;

            sut.SaveProject();

            fileSystem.DidNotReceive().FileMove(Arg.Any<string>(), Arg.Any<string>());
            fileSystem.Received(1).WriteAllTextIntoFile(fileName, Arg.Any<string>());
            fileSystem.Received(1).FileDelete(fileName + "~");
        }

        [Fact]
        public void SaveProject_NotExisting_JustSaved()
        {
            var sut = CreateSut(out IFileSystem fileSystem);
            fileSystem.GetLastWriteTime(Arg.Any<string>()).Returns(new DateTime(2020, 2, 29, 18, 45, 56));
            sut.LoadProjectData(Samples.SampleProject);

            sut.SaveProject();

            fileSystem.DidNotReceive().FileMove(Arg.Any<string>(), Arg.Any<string>());
            fileSystem.Received(1).WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>());
            fileSystem.DidNotReceive().FileDelete(Arg.Any<string>());
        }

        [Fact]
        public void SaveProject_ProjectExisting_SavedAfterBackup()
        {
            var sut = CreateSut(out IFileSystem fileSystem);
            fileSystem.GetLastWriteTime(Arg.Any<string>()).Returns(new DateTime(2020, 2, 29, 18, 45, 56));
            var fileName = "project.name";
            fileSystem.FileExists(fileName).Returns(true);
            sut.LoadProjectData(Samples.SampleProject);
            sut.FileName = fileName;

            sut.SaveProject();

            fileSystem.Received(1).FileMove(fileName, fileName + ".20200229184556");
            fileSystem.Received(1).WriteAllTextIntoFile(fileName, Arg.Any<string>());
            fileSystem.DidNotReceive().FileDelete(Arg.Any<string>());
        }

        [Fact]
        public void ShowInactiveAccounts_SetTrue_InactiveAccountsGetVisible()
        {
            var sut = CreateSut();
            var project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            sut.LoadProjectData(project);

            sut.ShowInactiveAccounts = true;

            using var _ = new AssertionScope();

            sut.AccountList.Should().BeEquivalentTo(
                new { Name = "Bank account" },
                new { Name = "Salary" },
                new { Name = "Shoes" },
                new { Name = "Carryforward" },
                new { Name = "Bank credit" },
                new { Name = "Friends debit" },
                new { Name = "Inactive" });
        }

        private static ShellViewModel CreateSut()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var applicationUpdate = Substitute.For<IApplicationUpdate>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var processApi = Substitute.For<IProcess>();
            var sut = new ShellViewModel(
                windowManager, reportFactory, applicationUpdate, messageBox, fileSystem, processApi)
            {
                Settings = new Settings()
            };
            return sut;
        }

        private static ShellViewModel CreateSut(out IWindowManager windowManager)
        {
            windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var applicationUpdate = Substitute.For<IApplicationUpdate>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var processApi = Substitute.For<IProcess>();
            var sut = new ShellViewModel(
                windowManager, reportFactory, applicationUpdate, messageBox, fileSystem, processApi)
            {
                Settings = new Settings()
            };
            return sut;
        }

        private static ShellViewModel CreateSut(out IReportFactory reportFactory)
        {
            var windowManager = Substitute.For<IWindowManager>();
            reportFactory = Substitute.For<IReportFactory>();
            var applicationUpdate = Substitute.For<IApplicationUpdate>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var processApi = Substitute.For<IProcess>();
            var sut = new ShellViewModel(
                windowManager, reportFactory, applicationUpdate, messageBox, fileSystem, processApi)
            {
                Settings = new Settings()
            };
            return sut;
        }

        private static ShellViewModel CreateSut(out IApplicationUpdate applicationUpdate)
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            applicationUpdate = Substitute.For<IApplicationUpdate>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var processApi = Substitute.For<IProcess>();
            var sut = new ShellViewModel(
                windowManager, reportFactory, applicationUpdate, messageBox, fileSystem, processApi)
            {
                Settings = new Settings()
            };
            return sut;
        }

        private static ShellViewModel CreateSut(out IMessageBox messageBox)
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var applicationUpdate = Substitute.For<IApplicationUpdate>();
            messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var processApi = Substitute.For<IProcess>();
            var sut = new ShellViewModel(
                windowManager, reportFactory, applicationUpdate, messageBox, fileSystem, processApi)
            {
                Settings = new Settings()
            };
            return sut;
        }

        private static ShellViewModel CreateSut(out IFileSystem fileSystem)
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var applicationUpdate = Substitute.For<IApplicationUpdate>();
            var messageBox = Substitute.For<IMessageBox>();
            fileSystem = Substitute.For<IFileSystem>();
            var processApi = Substitute.For<IProcess>();
            var sut = new ShellViewModel(
                windowManager, reportFactory, applicationUpdate, messageBox, fileSystem, processApi)
            {
                Settings = new Settings()
            };
            return sut;
        }

        private static ShellViewModel CreateSut(out IProcess processApi)
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var applicationUpdate = Substitute.For<IApplicationUpdate>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            processApi = Substitute.For<IProcess>();
            var sut = new ShellViewModel(
                windowManager, reportFactory, applicationUpdate, messageBox, fileSystem, processApi)
            {
                Settings = new Settings()
            };
            return sut;
        }

        private static ShellViewModel CreateSut(out IMessageBox messageBox, out IFileSystem fileSystem)
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var applicationUpdate = Substitute.For<IApplicationUpdate>();
            messageBox = Substitute.For<IMessageBox>();
            fileSystem = Substitute.For<IFileSystem>();
            var processApi = Substitute.For<IProcess>();
            var sut = new ShellViewModel(
                windowManager, reportFactory, applicationUpdate, messageBox, fileSystem, processApi)
            {
                Settings = new Settings()
            };
            return sut;
        }

    }
}
