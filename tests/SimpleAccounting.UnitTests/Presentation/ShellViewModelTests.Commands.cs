// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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
    using lg2de.SimpleAccounting.Infrastructure;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Presentation;
    using lg2de.SimpleAccounting.Reports;
    using NSubstitute;
    using Xunit;

    public partial class ShellViewModelTests
    {
        [Fact]
        public void AccountSelectionCommand_SampleBookingsBankAccount_AccountJournalUpdated()
        {
            var sut = CreateSut();
            var project = Samples.SampleProject;
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
                new { Text = "Summe", RemoteAccount = string.Empty, CreditValue = 549, DebitValue = 1200 },
                new { Text = "Saldo", RemoteAccount = string.Empty, CreditValue = 0, DebitValue = 651 });
        }

        [Fact]
        public void AccountSelectionCommand_SampleBookingsSalary_AccountJournalUpdated()
        {
            var sut = CreateSut();
            var project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            sut.LoadProjectData(project);

            sut.AccountSelectionCommand.Execute(sut.AccountList.Single(x => x.Identifier == 400));

            sut.AccountJournal.Should().BeEquivalentTo(
                new { Text = "Salary1", RemoteAccount = "100 (Bank account)", CreditValue = 120, DebitValue = 0 },
                new { Text = "Salary2", RemoteAccount = "100 (Bank account)", CreditValue = 80, DebitValue = 0 },
                new { Text = "Summe", RemoteAccount = string.Empty, CreditValue = 200, DebitValue = 0 },
                new { Text = "Saldo", RemoteAccount = string.Empty, CreditValue = 200, DebitValue = 0 });
        }

        [Fact]
        public void AccountSelectionCommand_SampleBookingsShoes_AccountJournalUpdated()
        {
            var sut = CreateSut();
            var project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            sut.LoadProjectData(project);

            sut.AccountSelectionCommand.Execute(sut.AccountList.Single(x => x.Identifier == 600));

            sut.AccountJournal.Should().BeEquivalentTo(
                new { Text = "Shoes1", RemoteAccount = "100 (Bank account)", CreditValue = 0, DebitValue = 20 },
                new { Text = "Shoes2", RemoteAccount = "100 (Bank account)", CreditValue = 0, DebitValue = 30 },
                new { Text = "Summe", RemoteAccount = string.Empty, CreditValue = 0, DebitValue = 50 },
                new { Text = "Saldo", RemoteAccount = string.Empty, CreditValue = 0, DebitValue = 50 });
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
        public void SaveProjectCommand_DocumentModified_CanExecute()
        {
            var sut = CreateSut();
            sut.LoadProjectData(Samples.SampleProject);
            sut.IsDocumentModified = true;

            sut.SaveProjectCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void SaveProjectCommand_Initialized_CannotExecute()
        {
            var sut = CreateSut();

            sut.SaveProjectCommand.CanExecute(null).Should()
                .BeFalse("default instance does not contain modified document");
        }

        [Fact]
        public void NewAccountCommand_AccountCreatedAndSorted()
        {
            static void UpdateAction(object parameter)
            {
                var vm = (AccountViewModel)parameter;
                vm.Name = "New Account";
                vm.Identifier = 500;
            }

            var sut = CreateSut(out IWindowManager windowManager);
            windowManager.ShowDialog(Arg.Do<object>(UpdateAction)).Returns(true);
            sut.LoadProjectData(Samples.SampleProject);

            sut.NewAccountCommand.Execute(null);

            sut.AccountList.Select(x => x.Name).Should().Equal(
                "Bank account", "Salary", "New Account", "Shoes", "Carryforward", "Bank credit", "Friends debit");
        }

        [Fact]
        public void EditAccountCommand_Abort_AllDataUpdated()
        {
            var sut = CreateSut(out IWindowManager windowManager);
            sut.LoadProjectData(Samples.SampleProject);

            sut.EditAccountCommand.Execute(sut.AccountList.First());

            sut.IsDocumentModified.Should().BeFalse();
            windowManager.Received(1).ShowDialog(Arg.Any<object>());
        }

        [Fact]
        public void EditAccountCommand_Confirmed_AllDataUpdated()
        {
            var sut = CreateSut(out IWindowManager windowManager);
            windowManager.ShowDialog(
                Arg.Do<object>(
                    model =>
                    {
                        var vm = (AccountViewModel)model;
                        vm.Identifier += 1000;
                    })).Returns(true);
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
                sut.IsDocumentModified.Should().BeTrue();
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
        public void EditAccountCommand_NullParameter_JustIgnored()
        {
            var sut = CreateSut(out IWindowManager windowManager);
            sut.LoadProjectData(Samples.SampleProject);

            sut.EditAccountCommand.Execute(null);

            sut.IsDocumentModified.Should().BeFalse();
            windowManager.DidNotReceive().ShowDialog(Arg.Any<object>());
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
        public void AddBookingsCommand_ShowInactiveAccounts_DialogInitialized()
        {
            var sut = CreateSut(out IWindowManager windowManager);
            EditBookingViewModel vm = null;
            windowManager.ShowDialog(Arg.Do<object>(model => vm = model as EditBookingViewModel));
            sut.LoadProjectData(Samples.SampleProject);
            sut.ShowInactiveAccounts = true;

            sut.AddBookingsCommand.Execute(null);

            using var _ = new AssertionScope();
            vm.BookingIdentifier.Should().Be(1);
            vm.Accounts.Should().BeEquivalentTo(Samples.SampleProject.AllAccounts);
        }

        [Fact]
        public void AddBookingsCommand_HideInactiveAccounts_DialogInitialized()
        {
            var sut = CreateSut(out IWindowManager windowManager);
            EditBookingViewModel vm = null;
            windowManager.ShowDialog(Arg.Do<object>(model => vm = model as EditBookingViewModel));
            sut.LoadProjectData(Samples.SampleProject);
            sut.ShowInactiveAccounts = false;

            sut.AddBookingsCommand.Execute(null);

            using var _ = new AssertionScope();
            vm.BookingIdentifier.Should().Be(1);
            vm.Accounts.Should().BeEquivalentTo(Samples.SampleProject.AllAccounts.Where(x => x.Active));
        }

        [Fact]
        public void EditBookingCommand_NoSave_DialogInitialized()
        {
            var sut = CreateSut(out IWindowManager windowManager);
            EditBookingViewModel vm = null;
            windowManager.ShowDialog(Arg.Do<object>(model => vm = model as EditBookingViewModel));
            var project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            sut.LoadProjectData(project);

            sut.EditBookingCommand.Execute(sut.FullJournal.Last());

            using var _ = new AssertionScope();
            sut.IsDocumentModified.Should().BeFalse("the project remains unchanged");
            vm.BookingIdentifier.Should().Be(6);
            vm.BookingText.Should().Be("Rent to friend");
            vm.Accounts.Should().BeEquivalentTo(Samples.SampleProject.AllAccounts.Where(x => x.Active));
        }

        [Fact]
        public void EditBookingCommand_SplitBookingCredit_SplitViewModelInitialized()
        {
            var sut = CreateSut(out IWindowManager windowManager);
            EditBookingViewModel vm = null;
            windowManager.ShowDialog(Arg.Do<object>(model => vm = model as EditBookingViewModel));
            var project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            sut.LoadProjectData(project);

            sut.EditBookingCommand.Execute(sut.FullJournal.First(x => x.Identifier == 3));

            using var _ = new AssertionScope();
            vm.CreditSplitEntries.Should().BeEquivalentTo(
                new { AccountNumber = 400, BookingText = "Salary1", BookingValue = 120 },
                new { AccountNumber = 400, BookingText = "Salary2", BookingValue = 80 });
        }

        [Fact]
        public void EditBookingCommand_SplitBookingDebit_SplitViewModelInitialized()
        {
            var sut = CreateSut(out IWindowManager windowManager);
            EditBookingViewModel vm = null;
            windowManager.ShowDialog(Arg.Do<object>(model => vm = model as EditBookingViewModel));
            var project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            sut.LoadProjectData(project);

            sut.EditBookingCommand.Execute(sut.FullJournal.First(x => x.Identifier == 5));

            using var _ = new AssertionScope();
            vm.DebitSplitEntries.Should().BeEquivalentTo(
                new { AccountNumber = 600, BookingText = "Shoes1", BookingValue = 20 },
                new { AccountNumber = 600, BookingText = "Shoes2", BookingValue = 30 });
        }

        [Fact]
        public void EditBookingCommand_EntryChanged_JournalsUpdated()
        {
            static void UpdateAction(object parameter)
            {
                var vm = (EditBookingViewModel)parameter;
                vm.BookingIdentifier += 100;
                vm.BookingValue += 100.0;
                vm.BookingText += " Paul";
            }

            var sut = CreateSut(out IWindowManager windowManager);
            windowManager.ShowDialog(Arg.Do<object>(UpdateAction)).Returns(true);
            var project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            sut.LoadProjectData(project);

            sut.EditBookingCommand.Execute(sut.FullJournal.Last());

            using var _ = new AssertionScope();
            sut.IsDocumentModified.Should().BeTrue("the project changed");
            sut.FullJournal.Last().Should().BeEquivalentTo(
                new
                {
                    Identifier = 106,
                    Value = 199.0,
                    Text = "Rent to friend Paul"
                });
        }

        [Fact]
        public void EditBookingCommand_NullParameter_JustIgnored()
        {
            var sut = CreateSut(out IWindowManager windowManager);
            var project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            sut.LoadProjectData(project);

            sut.EditBookingCommand.Execute(null);

            using var _ = new AssertionScope();
            sut.IsDocumentModified.Should().BeFalse("the project remains unchanged");
            windowManager.DidNotReceive().ShowDialog(Arg.Any<object>());
        }

        [Fact]
        public void ImportBookingsCommand_BookingNumberInitialized()
        {
            var sut = CreateSut(out IWindowManager windowManager);
            ImportBookingsViewModel vm = null;
            windowManager.ShowDialog(Arg.Do<object>(model => vm = model as ImportBookingsViewModel));
            sut.LoadProjectData(Samples.SampleProject);

            sut.ImportBookingsCommand.Execute(null);

            using (new AssertionScope())
            {
                vm.Should().BeEquivalentTo(
                    new
                    {
                        FirstBookingNumber = 1,
                        RangeMin = new DateTime(DateTime.Now.Year, 1, 1),
                        RangeMax = new DateTime(DateTime.Now.Year, 12, 31)
                    });
                vm.ImportAccounts.Should().NotBeEmpty();
            }
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
        public void CloseYearCommand_ActionAborted_YearsUnchanged()
        {
            var sut = CreateSut(out IMessageBox messageBox);
            messageBox.Show(
                Arg.Any<string>(),
                Arg.Any<string>(),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No).Returns(MessageBoxResult.No);
            sut.LoadProjectData(Samples.SampleProject);

            sut.CloseYearCommand.Execute(null);

            var thisYear = DateTime.Now.Year;
            sut.BookingYears.Select(x => x.Header).Should().Equal("2000", thisYear.ToString());
        }

        [Fact]
        public void CloseYearCommand_CurrentYearClosed_CannotExecute()
        {
            var sut = CreateSut(out IMessageBox messageBox);
            messageBox.Show(
                Arg.Any<string>(), Arg.Any<string>(), MessageBoxButton.YesNo, Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>()).Returns(MessageBoxResult.Yes);
            sut.LoadProjectData(Samples.SampleProject);
            sut.BookingYears.First().Command.Execute(null);

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
        public void CloseYearCommand_EmptyProject_CannotExecute()
        {
            var sut = CreateSut();

            sut.CloseYearCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void CloseYearCommand_HappyPath_YearClosedAndNewAdded()
        {
            var sut = CreateSut(out IWindowManager windowManager);
            windowManager.ShowDialog(
                Arg.Any<CloseYearViewModel>(),
                Arg.Any<object>(),
                Arg.Any<IDictionary<string, object>>()).Returns(
                info =>
                {
                    var vm = info.Arg<CloseYearViewModel>();
                    vm.RemoteAccount = vm.Accounts.First();
                    return true;
                });
            var project = Samples.SampleProject;
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
                new { Text = "Summe", IsSummary = true, DebitValue = 651, CreditValue = 0 },
                new { Text = "Saldo", IsSummary = true, DebitValue = 651, CreditValue = 0 });
        }

        [Fact]
        public void CloseYearCommand_SecondCarryForwardAccount_OpeningsWithSelectedAccount()
        {
            var sut = CreateSut(out IWindowManager windowManager);
            windowManager.ShowDialog(
                Arg.Any<CloseYearViewModel>(),
                Arg.Any<object>(),
                Arg.Any<IDictionary<string, object>>()).Returns(
                info =>
                {
                    var vm = info.Arg<CloseYearViewModel>();
                    vm.RemoteAccount = vm.Accounts.Last();
                    return true;
                });
            var project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            project.Accounts.First().Account.Add(
                new AccountDefinition { ID = 999, Name = "MyCarryForward", Type = AccountDefinitionType.Carryforward });
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
                new { Text = "Summe", IsSummary = true, DebitValue = 651, CreditValue = 0 },
                new { Text = "Saldo", IsSummary = true, DebitValue = 651, CreditValue = 0 });
        }

        [Fact]
        public void AccountJournalReportCommand_HappyPath_Completed()
        {
            var sut = CreateSut(out IReportFactory reportFactory);
            var accountJournalReport = Substitute.For<IAccountJournalReport>();
            reportFactory.CreateAccountJournal(
                Arg.Any<AccountingDataJournal>(),
                Arg.Any<IEnumerable<AccountDefinition>>(),
                Arg.Any<AccountingDataSetup>(), Arg.Any<CultureInfo>()).Returns(accountJournalReport);
            sut.LoadProjectData(Samples.SampleProject);

            sut.AccountJournalReportCommand.Execute(null);

            accountJournalReport.Received(1)
                .ShowPreview(Arg.Is<string>(document => !string.IsNullOrEmpty(document)));
        }

        [Fact]
        public void AccountJournalReportCommand_JournalWithEntries_CanExecute()
        {
            var sut = CreateSut();
            sut.FullJournal.Add(new FullJournalViewModel());

            sut.AccountJournalReportCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AccountJournalReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut();

            sut.AccountJournalReportCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void AnnualBalanceReportCommand_HappyPath_Completed()
        {
            var sut = CreateSut(out IReportFactory reportFactory);
            var annualBalanceReport = Substitute.For<IAnnualBalanceReport>();
            reportFactory.CreateAnnualBalance(
                Arg.Any<AccountingDataJournal>(),
                Arg.Any<IEnumerable<AccountDefinition>>(),
                Arg.Any<AccountingDataSetup>(),
                Arg.Any<CultureInfo>()).Returns(annualBalanceReport);
            sut.LoadProjectData(Samples.SampleProject);

            sut.AnnualBalanceReportCommand.Execute(null);

            annualBalanceReport.Received(1)
                .ShowPreview(Arg.Is<string>(document => !string.IsNullOrEmpty(document)));
        }

        [Fact]
        public void AnnualBalanceReportCommand_JournalWithEntries_CanExecute()
        {
            var sut = CreateSut();
            sut.FullJournal.Add(new FullJournalViewModel());

            sut.AnnualBalanceReportCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AnnualBalanceReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut();

            sut.AnnualBalanceReportCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void AssetBalancesReportCommand_HappyPath_Completed()
        {
            var sut = CreateSut(out IReportFactory reportFactory);
            var assetBalancesReport = Substitute.For<ITotalsAndBalancesReport>();
            reportFactory.CreateTotalsAndBalances(
                Arg.Any<AccountingDataJournal>(),
                Arg.Any<IEnumerable<AccountingDataAccountGroup>>(),
                Arg.Any<AccountingDataSetup>(),
                Arg.Any<CultureInfo>()).Returns(assetBalancesReport);
            var project = Samples.SampleProject;
            project.Accounts.Add(
                new AccountingDataAccountGroup { Name = "EMPTY", Account = new List<AccountDefinition>() });
            sut.LoadProjectData(project);

            sut.AssetBalancesReportCommand.Execute(null);

            assetBalancesReport.Received(1)
                .ShowPreview(Arg.Is<string>(document => !string.IsNullOrEmpty(document)));
            reportFactory.Received(1).CreateTotalsAndBalances(
                Arg.Any<AccountingDataJournal>(),
                Arg.Is<IEnumerable<AccountingDataAccountGroup>>(x => x.ToList().All(y => y.Name != "EMPTY")),
                Arg.Any<AccountingDataSetup>(),
                Arg.Any<CultureInfo>());
        }

        [Fact]
        public void AssetBalancesReportCommand_JournalWithEntries_CanExecute()
        {
            var sut = CreateSut();
            sut.FullJournal.Add(new FullJournalViewModel());

            sut.AssetBalancesReportCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AssetBalancesReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut();

            sut.AssetBalancesReportCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void TotalJournalReportCommand_HappyPath_Completed()
        {
            var sut = CreateSut(out IReportFactory reportFactory);
            var totalJournalReport = Substitute.For<ITotalJournalReport>();
            reportFactory.CreateTotalJournal(
                Arg.Any<AccountingDataJournal>(),
                Arg.Any<AccountingDataSetup>(),
                Arg.Any<CultureInfo>()).Returns(totalJournalReport);
            sut.LoadProjectData(Samples.SampleProject);

            sut.TotalJournalReportCommand.Execute(null);

            totalJournalReport.Received(1)
                .ShowPreview(Arg.Is<string>(document => !string.IsNullOrEmpty(document)));
        }

        [Fact]
        public void TotalJournalReportCommand_JournalWithEntries_CanExecute()
        {
            var sut = CreateSut();
            sut.FullJournal.Add(new FullJournalViewModel());

            sut.TotalJournalReportCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void TotalJournalReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut();

            sut.TotalJournalReportCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void TotalsAndBalancesReportCommand_HappyPath_Completed()
        {
            var sut = CreateSut(out IReportFactory reportFactory);
            var totalsAndBalancesReport = Substitute.For<ITotalsAndBalancesReport>();
            reportFactory.CreateTotalsAndBalances(
                Arg.Any<AccountingDataJournal>(),
                Arg.Any<IEnumerable<AccountingDataAccountGroup>>(),
                Arg.Any<AccountingDataSetup>(),
                Arg.Any<CultureInfo>()).Returns(totalsAndBalancesReport);
            sut.LoadProjectData(Samples.SampleProject);

            sut.TotalsAndBalancesReportCommand.Execute(null);

            totalsAndBalancesReport.Received(1)
                .ShowPreview(Arg.Is<string>(document => !string.IsNullOrEmpty(document)));
        }

        [Fact]
        public void TotalsAndBalancesReportCommand_JournalWithEntries_CanExecute()
        {
            var sut = CreateSut();
            sut.FullJournal.Add(new FullJournalViewModel());

            sut.TotalsAndBalancesReportCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void TotalsAndBalancesReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut();

            sut.TotalsAndBalancesReportCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void HelpAboutCommand_Execute_ShellProcessInvoked()
        {
            var sut = CreateSut(out IProcess processApi);

            sut.HelpAboutCommand.Execute(null);

            processApi.Received(1).Start(Arg.Is<ProcessStartInfo>(x => x.UseShellExecute));
        }

        [Fact]
        public void HelpFeedbackCommand_Execute_ShellProcessInvoked()
        {
            var sut = CreateSut(out IProcess processApi);

            sut.HelpFeedbackCommand.Execute(null);

            processApi.Received(1).Start(Arg.Is<ProcessStartInfo>(x => x.UseShellExecute));
        }

        [Fact]
        public async Task HelpCheckForUpdateCommand_Execute_InterfaceInvoked()
        {
            var sut = CreateSut(out IApplicationUpdate applicationUpdate);
            applicationUpdate.IsUpdateAvailableAsync(Arg.Any<string>()).Returns(true);

            sut.HelpCheckForUpdateCommand.Execute(null);

            // TODO remove with AsyncCommand
            await Task.Delay(1.Seconds());

            applicationUpdate.Received(1).StartUpdateProcess();
        }

        [Fact]
        public async Task HelpCheckForUpdateCommand_NoUpdateAvailable_UpdateProcessNotStarted()
        {
            var sut = CreateSut(out IApplicationUpdate applicationUpdate);
            applicationUpdate.IsUpdateAvailableAsync(Arg.Any<string>()).Returns(false);

            sut.HelpCheckForUpdateCommand.Execute(null);

            // TODO remove with AsyncCommand
            await Task.Delay(1.Seconds());

            applicationUpdate.DidNotReceive().StartUpdateProcess();
        }

        [Fact]
        public async Task HelpCheckForUpdateCommand_UserDoesNotWantToSave_UpdateProcessNotStarted()
        {
            var sut = CreateSut(out IApplicationUpdate applicationUpdate);
            applicationUpdate.IsUpdateAvailableAsync(Arg.Any<string>()).Returns(true);
            sut.IsDocumentModified = true;

            sut.HelpCheckForUpdateCommand.Execute(null);

            // TODO remove with AsyncCommand
            await Task.Delay(1.Seconds());

            applicationUpdate.DidNotReceive().StartUpdateProcess();
        }
    }
}
