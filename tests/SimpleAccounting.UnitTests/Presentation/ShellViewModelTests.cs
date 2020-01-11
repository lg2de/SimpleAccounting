﻿// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace SimpleAccounting.UnitTests.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Windows;
    using Caliburn.Micro;
    using FluentAssertions;
    using FluentAssertions.Execution;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Presentation;
    using lg2de.SimpleAccounting.Properties;
    using NSubstitute;
    using Xunit;

    public class ShellViewModelTests
    {
        [Fact]
        public void OnInitialize_NoProject_Initialized()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, messageBox, fileSystem);

            ((IActivate)sut).Activate();

            sut.DisplayName.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void OnActivate_TwoRecentProjectsOneExisting_ExistingProjectListed()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, messageBox, fileSystem)
            {
                Settings = new Settings { RecentProjects = new StringCollection { "file1", "file2" } }
            };
            fileSystem.FileExists(Arg.Is("file1")).Returns(true);

            ((IActivate)sut).Activate();

            sut.RecentProjects?.Select(x => x.Header).Should().Equal("file1");
        }

        [Fact]
        public void OnActivate_SampleProject_JournalUpdates()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, messageBox, fileSystem);
            AccountingData project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            sut.LoadProjectData(project);

            ((IActivate)sut).Activate();

            sut.Journal.Should().BeEquivalentTo(
                new { Text = "Open", CreditAccount = "990 (Carryforward)", DebitAccount = "100 (Bank account)" },
                new { Text = "Salary", CreditAccount = (string)null, DebitAccount = "100 (Bank account)" },
                new { Text = "Salary1", CreditAccount = "400 (Salary)", DebitAccount = (string)null },
                new { Text = "Salary2", CreditAccount = "400 (Salary)", DebitAccount = (string)null },
                new { Text = "Shoes1", CreditAccount = (string)null, DebitAccount = "600 (Shoes)" },
                new { Text = "Shoes2", CreditAccount = (string)null, DebitAccount = "600 (Shoes)" },
                new { Text = "Shoes", CreditAccount = "100 (Bank account)", DebitAccount = (string)null });
        }

        [Fact]
        public void NewProjectCommand_ProjectInitialized()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, messageBox, fileSystem);

            sut.NewProjectCommand.Execute(null);

            sut.Accounts.Should().NotBeEmpty();
            sut.Journal.Should().BeEmpty();
            sut.AccountJournal.Should().BeEmpty();
        }

        [Fact]
        public void SaveProjectCommand_Initialized_CannotExecute()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, messageBox, fileSystem);

            sut.SaveProjectCommand.CanExecute(null).Should()
                .BeFalse("default instance does not contain modified document");
        }

        [Fact]
        public void SaveProjectCommand_DocumentModified_CanExecute()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, messageBox, fileSystem);
            sut.LoadProjectData(Samples.SampleProject);

            sut.AddBooking(new AccountingDataJournalBooking(), refreshJournal: false);

            sut.SaveProjectCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AccountSelectionCommand_SampleBookings_JournalCorrect()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, messageBox, fileSystem);
            AccountingData project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            sut.LoadProjectData(project);

            sut.AccountSelectionCommand.Execute(sut.Accounts.Single(x => x.Identifier == 100));

            sut.AccountJournal.Should().BeEquivalentTo(
                new { Text = "Open", RemoteAccount = "990 (Carryforward)", CreditValue = 0, DebitValue = 1000 },
                new { Text = "Salary", RemoteAccount = "Diverse", CreditValue = 0, DebitValue = 200 },
                new { Text = "Shoes", RemoteAccount = "Diverse", CreditValue = 100, DebitValue = 0 },
                new { Text = "Summe", RemoteAccount = (string)null, CreditValue = 100, DebitValue = 1200 },
                new { Text = "Saldo", RemoteAccount = (string)null, CreditValue = 0, DebitValue = 1100 });

        }

        [Fact]
        public void AddBookingsCommand_NoProject_CannotExecute()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, messageBox, fileSystem);

            sut.AddBookingsCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void AddBookingsCommand_OpenYear_CanExecute()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, messageBox, fileSystem);
            sut.LoadProjectData(Samples.SampleProject);
            sut.BookingYears.Last().Command.Execute(null);

            sut.AddBookingsCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AddBookingsCommand_ClosedYear_CannotExecute()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, messageBox, fileSystem);
            sut.LoadProjectData(Samples.SampleProject);
            sut.BookingYears.First().Command.Execute(null);

            sut.AddBookingsCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void AddBookingsCommand_BookingNumberInitialized()
        {
            var windowManager = Substitute.For<IWindowManager>();
            AddBookingViewModel vm = null;
            windowManager.ShowDialog(Arg.Do<object>(model => vm = model as AddBookingViewModel));
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, messageBox, fileSystem);
            sut.LoadProjectData(Samples.SampleProject);

            sut.AddBookingsCommand.Execute(null);

            vm.BookingNumber.Should().Be(1);
        }

        [Fact]
        public void ImportBookingsCommand_NoProject_CannotExecute()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, messageBox, fileSystem);

            sut.ImportBookingsCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void ImportBookingsCommand_OpenYear_CanExecute()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, messageBox, fileSystem);
            sut.LoadProjectData(Samples.SampleProject);
            sut.BookingYears.Last().Command.Execute(null);

            sut.ImportBookingsCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void ImportBookingsCommand_ClosedYear_CannotExecute()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, messageBox, fileSystem);
            sut.LoadProjectData(Samples.SampleProject);
            sut.BookingYears.First().Command.Execute(null);

            sut.ImportBookingsCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void ImportBookingsCommand_BookingNumberInitialized()
        {
            var windowManager = Substitute.For<IWindowManager>();
            ImportBookingsViewModel vm = null;
            windowManager.ShowDialog(Arg.Do<object>(model => vm = model as ImportBookingsViewModel));
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, messageBox, fileSystem);
            sut.LoadProjectData(Samples.SampleProject);

            sut.ImportBookingsCommand.Execute(null);

            using (new AssertionScope())
            {
                vm.Should().BeEquivalentTo(new
                {
                    BookingNumber = 1,
                    RangeMin = new DateTime(DateTime.Now.Year, 1, 1),
                    RangMax = new DateTime(DateTime.Now.Year, 12, 31)
                });
                vm.ImportAccounts.Should().NotBeEmpty();
            }
        }

        [Fact]
        public void CloseYearCommand_EmptyProject_CannotExecute()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, messageBox, fileSystem);

            sut.CloseYearCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void CloseYearCommand_DefaultProject_CanExecute()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, messageBox, fileSystem);
            sut.LoadProjectData(Samples.SampleProject);

            sut.CloseYearCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void CloseYearCommand_CurrentYearClosed_CannotExecute()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var messageBox = Substitute.For<IMessageBox>();
            messageBox.Show(Arg.Any<string>(), Arg.Any<string>(), MessageBoxButton.YesNo, Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>()).Returns(MessageBoxResult.Yes);
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, messageBox, fileSystem);
            sut.LoadProjectData(Samples.SampleProject);
            sut.BookingYears.First().Command.Execute(null);

            sut.CloseYearCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void NewAccountCommand_AccountCreatedAndSorted()
        {
            var windowManager = Substitute.For<IWindowManager>();
            windowManager.ShowDialog(Arg.Do<object>(model =>
            {
                var vm = model as AccountViewModel;
                vm.Name = "New Account";
                vm.Identifier = 500;
            })).Returns(true);
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, messageBox, fileSystem);
            sut.LoadProjectData(Samples.SampleProject);

            sut.NewAccountCommand.Execute(null);

            sut.Accounts.Select(x => x.Name).Should()
                .Equal("Bank account", "Salary", "New Account", "Shoes", "Carryforward");
        }

        [Fact]
        public void EditAccountCommand_AllDataUpdated()
        {
            var windowManager = Substitute.For<IWindowManager>();
            windowManager.ShowDialog(Arg.Do<object>(model =>
            {
                var vm = model as AccountViewModel;
                vm.Identifier += 1000;
            })).Returns(true);
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, messageBox, fileSystem);
            sut.LoadProjectData(Samples.SampleProject);
            var booking = new AccountingDataJournalBooking
            {
                Date = 20190201,
                ID = 1,
                Credit = new List<BookingValue> { new BookingValue { Account = 990, Text = "Init", Value = 42 } },
                Debit = new List<BookingValue> { new BookingValue { Account = 100, Text = "Init", Value = 42 } }
            };
            sut.AddBooking(booking);
            booking = new AccountingDataJournalBooking
            {
                Date = 20190301,
                ID = 2,
                Credit = new List<BookingValue> { new BookingValue { Account = 100, Text = "Back", Value = 5 } },
                Debit = new List<BookingValue> { new BookingValue { Account = 990, Text = "Back", Value = 5 } }
            };
            sut.AddBooking(booking);

            sut.EditAccountCommand.Execute(sut.Accounts.First());

            using (new AssertionScope())
            {
                sut.Accounts.Select(x => x.Name).Should().Equal("Salary", "Shoes", "Carryforward", "Bank account");
                sut.Journal.Should().BeEquivalentTo(
                    new { CreditAccount = "990 (Carryforward)", DebitAccount = "1100 (Bank account)" },
                    new { CreditAccount = "1100 (Bank account)", DebitAccount = "990 (Carryforward)" });
            }
        }

        [Fact]
        public void AddBooking_FirstBooking_JournalUpdated()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, messageBox, fileSystem);
            sut.LoadProjectData(Samples.SampleProject);

            var booking = new AccountingDataJournalBooking
            {
                Date = 20190401,
                ID = 1,
                Credit = new List<BookingValue> { new BookingValue { Account = 990, Text = "Init", Value = 42 } },
                Debit = new List<BookingValue> { new BookingValue { Account = 100, Text = "Init", Value = 42 } }
            };
            sut.AddBooking(booking);

            sut.Journal.Should().BeEquivalentTo(
                new
                {
                    Identifier = 1,
                    Date = new DateTime(2019, 4, 1),
                    Text = "Init",
                    Value = 0.42,
                    CreditAccount = "990 (Carryforward)",
                    DebitAccount = "100 (Bank account)"
                });
        }
    }
}
