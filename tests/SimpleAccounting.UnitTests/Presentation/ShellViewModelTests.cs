// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace SimpleAccounting.UnitTests.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using Caliburn.Micro;
    using FluentAssertions;
    using FluentAssertions.Execution;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Presentation;
    using lg2de.SimpleAccounting.Properties;
    using lg2de.SimpleAccounting.Reports;
    using NSubstitute;
    using Xunit;

    public class ShellViewModelTests
    {
        [Fact]
        public void OnInitialize_NoProject_Initialized()
        {
            var sut = CreateSut();

            ((IActivate)sut).Activate();

            sut.DisplayName.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void OnActivate_TwoRecentProjectsOneExisting_ExistingProjectListed()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem)
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
            var sut = CreateSut();
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
            var sut = CreateSut();

            sut.NewProjectCommand.Execute(null);

            sut.Accounts.Should().NotBeEmpty();
            sut.Journal.Should().BeEmpty();
            sut.AccountJournal.Should().BeEmpty();
        }

        [Fact]
        public void SaveProjectCommand_Initialized_CannotExecute()
        {
            var sut = CreateSut();

            sut.SaveProjectCommand.CanExecute(null).Should()
                .BeFalse("default instance does not contain modified document");
        }

        [Fact]
        public void SaveProjectCommand_DocumentModified_CanExecute()
        {
            var sut = CreateSut();
            sut.LoadProjectData(Samples.SampleProject);

            sut.AddBooking(new AccountingDataJournalBooking(), refreshJournal: false);

            sut.SaveProjectCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AccountSelectionCommand_SampleBookings_JournalCorrect()
        {
            var sut = CreateSut();
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
            var sut = CreateSut();

            sut.AddBookingsCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void AddBookingsCommand_OpenYear_CanExecute()
        {
            var sut = CreateSut();
            sut.LoadProjectData(Samples.SampleProject);
            sut.BookingYears.Last().Command.Execute(null);

            sut.AddBookingsCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AddBookingsCommand_ClosedYear_CannotExecute()
        {
            var sut = CreateSut();
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
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem);
            sut.LoadProjectData(Samples.SampleProject);

            sut.AddBookingsCommand.Execute(null);

            vm.BookingNumber.Should().Be(1);
        }

        [Fact]
        public void ImportBookingsCommand_NoProject_CannotExecute()
        {
            var sut = CreateSut();

            sut.ImportBookingsCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void ImportBookingsCommand_OpenYear_CanExecute()
        {
            var sut = CreateSut();
            sut.LoadProjectData(Samples.SampleProject);
            sut.BookingYears.Last().Command.Execute(null);

            sut.ImportBookingsCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void ImportBookingsCommand_ClosedYear_CannotExecute()
        {
            var sut = CreateSut();
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
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem);
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
            var sut = CreateSut();

            sut.CloseYearCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void CloseYearCommand_DefaultProject_CanExecute()
        {
            var sut = CreateSut();
            sut.LoadProjectData(Samples.SampleProject);

            sut.CloseYearCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void CloseYearCommand_CurrentYearClosed_CannotExecute()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            messageBox.Show(Arg.Any<string>(), Arg.Any<string>(), MessageBoxButton.YesNo, Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>()).Returns(MessageBoxResult.Yes);
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem);
            sut.LoadProjectData(Samples.SampleProject);
            sut.BookingYears.First().Command.Execute(null);

            sut.CloseYearCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void CloseYearCommand_HappyPath_YearClosedAndNewAdded()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            messageBox.Show(
                Arg.Any<string>(),
                Arg.Any<string>(),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No).Returns(MessageBoxResult.Yes);
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem);
            sut.LoadProjectData(Samples.SampleProject);
            var booking = new AccountingDataJournalBooking
            {
                Date = DateTime.Now.ToAccountingDate(),
                ID = 1,
                Credit = new List<BookingValue> { new BookingValue { Account = 990, Text = "Init", Value = 1000 } },
                Debit = new List<BookingValue> { new BookingValue { Account = 100, Text = "Init", Value = 1000 } }
            };
            sut.AddBooking(booking);
            booking = new AccountingDataJournalBooking
            {
                Date = DateTime.Now.ToAccountingDate(),
                ID = 2,
                Credit = new List<BookingValue> { new BookingValue { Account = 400, Text = "Income", Value = 500 } },
                Debit = new List<BookingValue> { new BookingValue { Account = 100, Text = "Income", Value = 500 } }
            };
            sut.AddBooking(booking);
            booking = new AccountingDataJournalBooking
            {
                Date = DateTime.Now.ToAccountingDate(),
                ID = 2,
                Credit = new List<BookingValue> { new BookingValue { Account = 100, Text = "Expense", Value = 800 } },
                Debit = new List<BookingValue> { new BookingValue { Account = 600, Text = "Expense", Value = 800 } }
            };
            sut.AddBooking(booking);

            sut.CloseYearCommand.Execute(null);

            messageBox.Received(1).Show(
                Arg.Any<string>(),
                Arg.Any<string>(),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            var thisYear = DateTime.Now.Year;
            sut.BookingYears.Select(x => x.Header).Should()
                .Equal("2000", thisYear.ToString(), (thisYear + 1).ToString());
            sut.Journal.Should().BeEquivalentTo(
                new
                {
                    Identifier = 1,
                    Date = new DateTime(thisYear + 1, 1, 1),
                    Text = "Eröffnungsbetrag 1",
                    Value = 7.00,
                    CreditAccount = "990 (Carryforward)",
                    DebitAccount = "100 (Bank account)"
                });
        }

        [Fact]
        public void CloseYearCommand_ActionAborted_YearsUnchanged()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            messageBox.Show(
                Arg.Any<string>(),
                Arg.Any<string>(),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No).Returns(MessageBoxResult.No);
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem);
            sut.LoadProjectData(Samples.SampleProject);

            sut.CloseYearCommand.Execute(null);

            var thisYear = DateTime.Now.Year;
            sut.BookingYears.Select(x => x.Header).Should().Equal("2000", thisYear.ToString());
        }

        [Fact]
        public void TotalJournalReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut();

            sut.TotalJournalReportCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void TotalJournalReportCommand_JournalWithEntries_CannotExecute()
        {
            var sut = CreateSut();
            sut.Journal.Add(new JournalViewModel());

            sut.TotalJournalReportCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AccountJournalReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut();

            sut.AccountJournalReportCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void AccountJournalReportCommand_JournalWithEntries_CannotExecute()
        {
            var sut = CreateSut();
            sut.Journal.Add(new JournalViewModel());

            sut.AccountJournalReportCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AccountJournalReportCommand_HappyPath_Completed()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var accountJournalReport = Substitute.For<IAccountJournalReport>();
            reportFactory.CreateAccountJournal(
                Arg.Any<IEnumerable<AccountDefinition>>(),
                Arg.Any<AccountingDataJournal>(),
                Arg.Any<AccountingDataSetup>(),
                Arg.Any<CultureInfo>()).Returns(accountJournalReport);
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem);
            sut.LoadProjectData(Samples.SampleProject);

            sut.AccountJournalReportCommand.Execute(null);

            accountJournalReport.Received(1)
                .ShowPreview(Arg.Is<string>(document => !string.IsNullOrEmpty(document)));
        }

        [Fact]
        public void TotalsAndBalancesReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut();

            sut.TotalsAndBalancesReportCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void TotalsAndBalancesReportCommand_JournalWithEntries_CannotExecute()
        {
            var sut = CreateSut();
            sut.Journal.Add(new JournalViewModel());

            sut.TotalsAndBalancesReportCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AssetBalancesReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut();

            sut.AssetBalancesReportCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void AssetBalancesReportCommand_JournalWithEntries_CannotExecute()
        {
            var sut = CreateSut();
            sut.Journal.Add(new JournalViewModel());

            sut.AssetBalancesReportCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AnnualBalanceReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut();

            sut.AnnualBalanceReportCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void AnnualBalanceReportCommand_JournalWithEntries_CannotExecute()
        {
            var sut = CreateSut();
            sut.Journal.Add(new JournalViewModel());

            sut.AnnualBalanceReportCommand.CanExecute(null).Should().BeTrue();
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
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem);
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
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem);
            sut.LoadProjectData(Samples.SampleProject);
            var booking = new AccountingDataJournalBooking
            {
                Date = DateTime.Now.ToAccountingDate(),
                ID = 1,
                Credit = new List<BookingValue> { new BookingValue { Account = 990, Text = "Init", Value = 42 } },
                Debit = new List<BookingValue> { new BookingValue { Account = 100, Text = "Init", Value = 42 } }
            };
            sut.AddBooking(booking);
            booking = new AccountingDataJournalBooking
            {
                Date = DateTime.Now.ToAccountingDate(),
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
            var sut = CreateSut();
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

        private static ShellViewModel CreateSut()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem);
            return sut;
        }
    }
}
