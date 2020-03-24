// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using Caliburn.Micro;
    using FluentAssertions;
    using FluentAssertions.Execution;
    using FluentAssertions.Extensions;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Presentation;
    using lg2de.SimpleAccounting.Properties;
    using lg2de.SimpleAccounting.Reports;
    using NSubstitute;
    using Octokit;
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
        public void OnActivate_SampleProject_JournalsUpdates()
        {
            var sut = CreateSut();
            AccountingData project = Samples.SampleProject;
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
                new { Text = "Salary", CreditAccount = (string)null, DebitAccount = "100 (Bank account)" },
                new { Text = "Salary1", CreditAccount = "400 (Salary)", DebitAccount = (string)null },
                new { Text = "Salary2", CreditAccount = "400 (Salary)", DebitAccount = (string)null },
                new { Text = "Credit rate", CreditAccount = "100 (Bank account)", DebitAccount = "5000 (Bank credit)" },
                new { Text = "Shoes1", CreditAccount = (string)null, DebitAccount = "600 (Shoes)" },
                new { Text = "Shoes2", CreditAccount = (string)null, DebitAccount = "600 (Shoes)" },
                new { Text = "Shoes", CreditAccount = "100 (Bank account)", DebitAccount = (string)null },
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
        public void OnDeactivate_HappyPath_Completes()
        {
            var sut = CreateSut();
            ((IActivate)sut).Activate();

            var task = Task.Run(() => ((IDeactivate)sut).Deactivate(close: true));

            task.Awaiting(x => x).Should().CompleteWithin(1.Seconds());
        }

        [Fact]
        public void NewProjectCommand_ProjectInitialized()
        {
            var sut = CreateSut();

            sut.NewProjectCommand.Execute(null);

            sut.AccountList.Should().NotBeEmpty();
            sut.FullJournal.Should().BeEmpty();
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
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem);
            sut.LoadProjectData(Samples.SampleProject);
            sut.IsDocumentModified = true;

            sut.SaveProjectCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void ShowInactiveAccounts_SetTrue_InactiveAccountsGetVisible()
        {
            var sut = CreateSut();
            AccountingData project = Samples.SampleProject;
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

        [Fact]
        public void AccountSelectionCommand_SampleBookings_AccountJournalUpdated()
        {
            var sut = CreateSut();
            AccountingData project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            sut.LoadProjectData(project);

            sut.AccountSelectionCommand.Execute(sut.AccountList.Single(x => x.Identifier == 100));

            sut.AccountJournal.Should().BeEquivalentTo(
                new { Text = "Open 1", RemoteAccount = "990 (Carryforward)", CreditValue = 0, DebitValue = 1000 },
                new { Text = "Salary", RemoteAccount = "Diverse", CreditValue = 0, DebitValue = 200 },
                new { Text = "Credit rate", RemoteAccount = "5000 (Bank credit)", CreditValue = 400, DebitValue = 0 },
                new { Text = "Shoes", RemoteAccount = "Diverse", CreditValue = 50, DebitValue = 0 },
                new
                {
                    Text = "Rent to friend",
                    RemoteAccount = "6000 (Friends debit)",
                    CreditValue = 99,
                    DebitValue = 0
                },
                new { Text = "Summe", RemoteAccount = (string)null, CreditValue = 549, DebitValue = 1200 },
                new { Text = "Saldo", RemoteAccount = (string)null, CreditValue = 0, DebitValue = 651 });
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
        public void AddBookingsCommand_ShowInactiveAccounts_DialogInitialized()
        {
            var windowManager = Substitute.For<IWindowManager>();
            AddBookingViewModel vm = null;
            windowManager.ShowDialog(Arg.Do<object>(model => vm = model as AddBookingViewModel));
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem);
            sut.LoadProjectData(Samples.SampleProject);
            sut.ShowInactiveAccounts = true;

            sut.AddBookingsCommand.Execute(null);

            using var _ = new AssertionScope();
            vm.BookingNumber.Should().Be(1);
            vm.Accounts.Should().BeEquivalentTo(Samples.SampleProject.AllAccounts);
        }

        [Fact]
        public void AddBookingsCommand_HideInactiveAccounts_DialogInitialized()
        {
            var windowManager = Substitute.For<IWindowManager>();
            AddBookingViewModel vm = null;
            windowManager.ShowDialog(Arg.Do<object>(model => vm = model as AddBookingViewModel));
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem);
            sut.LoadProjectData(Samples.SampleProject);
            sut.ShowInactiveAccounts = false;

            sut.AddBookingsCommand.Execute(null);

            using var _ = new AssertionScope();
            vm.BookingNumber.Should().Be(1);
            vm.Accounts.Should().BeEquivalentTo(Samples.SampleProject.AllAccounts.Where(x => x.Active));
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
            windowManager.ShowDialog(
                Arg.Any<CloseYearViewModel>(),
                Arg.Any<object>(),
                Arg.Any<IDictionary<string, object>>()).Returns(info =>
            {
                var vm = info.Arg<CloseYearViewModel>();
                vm.RemoteAccount = vm.Accounts.First();
                return true;
            });
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem);
            AccountingData project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            sut.LoadProjectData(project);

            sut.CloseYearCommand.Execute(null);

            windowManager.Received(1).ShowDialog(
                Arg.Any<CloseYearViewModel>(),
                Arg.Any<object>(),
                Arg.Any<IDictionary<string, object>>());
            var thisYear = DateTime.Now.Year;
            using var _ = new AssertionScope();
            sut.BookingYears.Select(x => x.Header).Should()
                .Equal("2000", thisYear.ToString(), (thisYear + 1).ToString());
            sut.FullJournal.Should().BeEquivalentTo(
                new
                {
                    Identifier = 1,
                    Date = new DateTime(thisYear + 1, 1, 1),
                    Text = "Eröffnungsbetrag 1",
                    Value = 651,
                    CreditAccount = "990 (Carryforward)",
                    DebitAccount = "100 (Bank account)"
                },
                new
                {
                    Identifier = 2,
                    Date = new DateTime(thisYear + 1, 1, 1),
                    Text = "Eröffnungsbetrag 2",
                    Value = 2600,
                    CreditAccount = "5000 (Bank credit)",
                    DebitAccount = "990 (Carryforward)"
                },
                new
                {
                    Identifier = 3,
                    Date = new DateTime(thisYear + 1, 1, 1),
                    Text = "Eröffnungsbetrag 3",
                    Value = 99,
                    CreditAccount = "990 (Carryforward)",
                    DebitAccount = "6000 (Friends debit)"
                });
            sut.AccountJournal.Should().BeEquivalentTo(
                new
                {
                    Identifier = 1,
                    Date = new DateTime(thisYear + 1, 1, 1),
                    Text = "Eröffnungsbetrag 1",
                    DebitValue = 651,
                    CreditValue = 0,
                    RemoteAccount = "990 (Carryforward)"
                },
                new
                {
                    Text = "Summe",
                    IsSummary = true,
                    DebitValue = 651,
                    CreditValue = 0
                },
                new
                {
                    Text = "Saldo",
                    IsSummary = true,
                    DebitValue = 651,
                    CreditValue = 0
                });
        }

        [Fact]
        public void CloseYearCommand_SecondCarryForwardAccount_OpeningsWithSelectedAccount()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            windowManager.ShowDialog(
                Arg.Any<CloseYearViewModel>(),
                Arg.Any<object>(),
                Arg.Any<IDictionary<string, object>>()).Returns(info =>
            {
                var vm = info.Arg<CloseYearViewModel>();
                vm.RemoteAccount = vm.Accounts.Last();
                return true;
            });
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem);
            AccountingData project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            project.Accounts.First().Account.Add(new AccountDefinition
            {
                ID = 999,
                Name = "MyCarryForward",
                Type = AccountDefinitionType.Carryforward
            });
            sut.LoadProjectData(project);

            sut.CloseYearCommand.Execute(null);

            windowManager.Received(1).ShowDialog(
                Arg.Any<CloseYearViewModel>(),
                Arg.Any<object>(),
                Arg.Any<IDictionary<string, object>>());
            var thisYear = DateTime.Now.Year;
            using var _ = new AssertionScope();
            sut.BookingYears.Select(x => x.Header).Should()
                .Equal("2000", thisYear.ToString(), (thisYear + 1).ToString());
            sut.FullJournal.Should().BeEquivalentTo(
                new
                {
                    Identifier = 1,
                    Date = new DateTime(thisYear + 1, 1, 1),
                    Text = "Eröffnungsbetrag 1",
                    Value = 651,
                    CreditAccount = "999 (MyCarryForward)",
                    DebitAccount = "100 (Bank account)"
                },
                new
                {
                    Identifier = 2,
                    Date = new DateTime(thisYear + 1, 1, 1),
                    Text = "Eröffnungsbetrag 2",
                    Value = 2600,
                    CreditAccount = "5000 (Bank credit)",
                    DebitAccount = "999 (MyCarryForward)"
                },
                new
                {
                    Identifier = 3,
                    Date = new DateTime(thisYear + 1, 1, 1),
                    Text = "Eröffnungsbetrag 3",
                    Value = 99,
                    CreditAccount = "999 (MyCarryForward)",
                    DebitAccount = "6000 (Friends debit)"
                });
            sut.AccountJournal.Should().BeEquivalentTo(
                new
                {
                    Identifier = 1,
                    Date = new DateTime(thisYear + 1, 1, 1),
                    Text = "Eröffnungsbetrag 1",
                    DebitValue = 651,
                    CreditValue = 0,
                    RemoteAccount = "999 (MyCarryForward)"
                },
                new
                {
                    Text = "Summe",
                    IsSummary = true,
                    DebitValue = 651,
                    CreditValue = 0
                },
                new
                {
                    Text = "Saldo",
                    IsSummary = true,
                    DebitValue = 651,
                    CreditValue = 0
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
        public void TotalJournalReportCommand_JournalWithEntries_CanExecute()
        {
            var sut = CreateSut();
            sut.FullJournal.Add(new FullJournalViewModel());

            sut.TotalJournalReportCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AccountJournalReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut();

            sut.AccountJournalReportCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void AccountJournalReportCommand_JournalWithEntries_CanExecute()
        {
            var sut = CreateSut();
            sut.FullJournal.Add(new FullJournalViewModel());

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
        public void TotalsAndBalancesReportCommand_JournalWithEntries_CanExecute()
        {
            var sut = CreateSut();
            sut.FullJournal.Add(new FullJournalViewModel());

            sut.TotalsAndBalancesReportCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AssetBalancesReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut();

            sut.AssetBalancesReportCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void AssetBalancesReportCommand_JournalWithEntries_CanExecute()
        {
            var sut = CreateSut();
            sut.FullJournal.Add(new FullJournalViewModel());

            sut.AssetBalancesReportCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AnnualBalanceReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut();

            sut.AnnualBalanceReportCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void AnnualBalanceReportCommand_JournalWithEntries_CanExecute()
        {
            var sut = CreateSut();
            sut.FullJournal.Add(new FullJournalViewModel());

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

            sut.AccountList.Select(x => x.Name).Should().Equal(
                "Bank account", "Salary", "New Account", "Shoes", "Carryforward", "Bank credit", "Friends debit");
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
            sut.AccountSelectionCommand.Execute(sut.AccountList.FirstOrDefault(x => x.Identifier == 990));

            sut.EditAccountCommand.Execute(sut.AccountList.First());

            using (new AssertionScope())
            {
                sut.AccountList.Select(x => x.Name).Should().Equal(
                    "Salary", "Shoes", "Carryforward", "Bank account", "Bank credit", "Friends debit");
                sut.FullJournal.Should().BeEquivalentTo(
                    new { CreditAccount = "990 (Carryforward)", DebitAccount = "1100 (Bank account)" },
                    new { CreditAccount = "1100 (Bank account)", DebitAccount = "990 (Carryforward)" });
                sut.AccountJournal.Should().BeEquivalentTo(
                    new { RemoteAccount = "1100 (Bank account)" },
                    new { RemoteAccount = "1100 (Bank account)" },
                    new { Text = "Summe" },
                    new { Text = "Saldo" });
            }
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
                new
                {
                    Text = "Summe",
                    IsSummary = true,
                    CreditValue = 0.0,
                    DebitValue = 0.42
                },
                new
                {
                    Text = "Saldo",
                    IsSummary = true,
                    CreditValue = 0.0,
                    DebitValue = 0.42
                });
            monitor.Should().RaisePropertyChangeFor(x => x.SelectedAccountJournalEntry);
            sut.SelectedAccountJournalEntry.Should().BeEquivalentTo(new { Identifier = 4567 });
        }

        [Fact]
        public void CheckSaveProject_NotModified_ReturnsTrue()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem);

            sut.CheckSaveProject().Should().BeTrue();

            messageBox.DidNotReceive().Show(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
        }

        [Fact]
        public void CheckSaveProject_AnswerYes_SavedAndReturnsTrue()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            messageBox.Show(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
                .Returns(MessageBoxResult.Yes);
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem)
            {
                IsDocumentModified = true
            };
            sut.LoadProjectData(Samples.SampleProject);

            sut.CheckSaveProject().Should().BeTrue();

            messageBox.Received(1).Show(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
            fileSystem.Received(1).WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public void CheckSaveProject_AnswerNo_NotSavedAndReturnsTrue()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            messageBox.Show(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
                .Returns(MessageBoxResult.No);
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem)
            {
                IsDocumentModified = true
            };
            sut.LoadProjectData(Samples.SampleProject);

            sut.CheckSaveProject().Should().BeTrue();

            messageBox.Received(1).Show(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
            fileSystem.DidNotReceive().WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public void CheckSaveProject_Cancel_NotSavedAndReturnsFalse()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            messageBox.Show(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
                .Returns(MessageBoxResult.Cancel);
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem)
            {
                IsDocumentModified = true
            };
            sut.LoadProjectData(Samples.SampleProject);

            sut.CheckSaveProject().Should().BeFalse();

            messageBox.Received(1).Show(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
            fileSystem.DidNotReceive().WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public void SaveProject_NotExisting_JustSaved()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem);
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
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem);
            fileSystem.GetLastWriteTime(Arg.Any<string>()).Returns(new DateTime(2020, 2, 29, 18, 45, 56));
            string fileName = "project.name";
            fileSystem.FileExists(fileName).Returns(true);
            sut.LoadProjectData(Samples.SampleProject);
            sut.FileName = fileName;

            sut.SaveProject();

            fileSystem.Received(1).FileMove(fileName, fileName + ".20200229184556");
            fileSystem.Received(1).WriteAllTextIntoFile(fileName, Arg.Any<string>());
            fileSystem.DidNotReceive().FileDelete(Arg.Any<string>());
        }

        [Fact]
        public void SaveProject_AutoSaveExisting_AutoSaveFileDeleted()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem);
            fileSystem.GetLastWriteTime(Arg.Any<string>()).Returns(new DateTime(2020, 2, 29, 18, 45, 56));
            string fileName = "project.name";
            fileSystem.FileExists(fileName + "~").Returns(true);
            sut.LoadProjectData(Samples.SampleProject);
            sut.FileName = fileName;

            sut.SaveProject();

            fileSystem.DidNotReceive().FileMove(Arg.Any<string>(), Arg.Any<string>());
            fileSystem.Received(1).WriteAllTextIntoFile(fileName, Arg.Any<string>());
            fileSystem.Received(1).FileDelete(fileName + "~");
        }

        [Theory]
        [InlineData("2.0.0", "2.0.0", null)]
        [InlineData("2.0.0", "2.0.1", "2.0.1")]
        [InlineData("2.0.0", "2.1.0", "2.1.0")]
        [InlineData("2.1.0", "2.0.0", null)]
        [InlineData("2.0.0-beta1", "2.0.0-beta1", null)]
        [InlineData("2.0.0-beta1", "2.0.0-beta2", "2.0.0-beta2")]
        [InlineData("2.0.0-beta1", "2.0.1-beta1", "2.0.1-beta1")]
        [InlineData("2.0.0-beta2", "2.0.0-beta1", null)]
        [InlineData("2.0.0", "2.0.0-beta1", null)] // release is greater than beta
        [InlineData("2.0.0-beta1", "2.0.0", "2.0.0")] // update to release
        [InlineData("2.0.0", "2.0.1-beta1", null)] // do not update to beta
        public void GetNewRelease_TestScenarios(
            string currentVersion,
            string availableVersion,
            string expectedVersion)
        {
            var sut = CreateSut();

            var result = sut.GetNewRelease(currentVersion, CreateRelease(availableVersion));

            if (string.IsNullOrEmpty(expectedVersion))
            {
                result.Should().BeNull();
            }
            else
            {
                result.Should().BeEquivalentTo(new { TagName = expectedVersion });
            }
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

        private static IReadOnlyList<Release> CreateRelease(string tag)
        {
            var tagProperty = typeof(Release).GetProperty(nameof(Release.TagName));
            var prereleaseProperty = typeof(Release).GetProperty(nameof(Release.Prerelease));
            var release = new Release();
            tagProperty.SetValue(release, tag);
            if (tag.Contains("beta"))
            {
                prereleaseProperty.SetValue(release, true);
            }

            return new List<Release> { release };
        }
    }
}
