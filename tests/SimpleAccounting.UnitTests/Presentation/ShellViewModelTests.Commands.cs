// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using System;
using System.Collections.Generic;
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
using NSubstitute;
using Xunit;

public partial class ShellViewModelTests
{
    [CulturedFact("en")]
    public void AccountSelectionCommand_SampleBookingsBankAccount_AccountJournalUpdated()
    {
        var sut = CreateSut();
        var project = Samples.SampleProject;
        project.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        sut.ProjectData.Load(project);

        sut.Accounts.AccountSelectionCommand.Execute(sut.Accounts.AccountList.Single(x => x.Identifier == 100));

        sut.AccountJournal.Items.Should().BeEquivalentTo(
            new[]
            {
                new { Text = "Open 1", RemoteAccount = "990 (Carryforward)", CreditValue = 0, DebitValue = 1000 },
                new { Text = "Salary", RemoteAccount = "Various", CreditValue = 0, DebitValue = 200 },
                new
                {
                    Text = "Credit rate",
                    RemoteAccount = "5000 (Bank credit)",
                    CreditValue = 400,
                    DebitValue = 0
                },
                new { Text = "Shoes", RemoteAccount = "Various", CreditValue = 50, DebitValue = 0 },
                new
                {
                    Text = "Rent to friend",
                    RemoteAccount = "6000 (Friends debit)",
                    CreditValue = 99,
                    DebitValue = 0
                },
                new { Text = "Total", RemoteAccount = string.Empty, CreditValue = 549, DebitValue = 1200 },
                new { Text = "Balance", RemoteAccount = string.Empty, CreditValue = 0, DebitValue = 651 }
            });
    }

    [CulturedFact("en")]
    public void AccountSelectionCommand_SampleBookingsSalary_AccountJournalUpdated()
    {
        var sut = CreateSut();
        var project = Samples.SampleProject;
        project.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        sut.ProjectData.Load(project);

        sut.Accounts.AccountSelectionCommand.Execute(sut.Accounts.AccountList.Single(x => x.Identifier == 400));

        sut.AccountJournal.Items.Should().BeEquivalentTo(
            new[]
            {
                new { Text = "Salary1", RemoteAccount = "100 (Bank account)", CreditValue = 120, DebitValue = 0 },
                new { Text = "Salary2", RemoteAccount = "100 (Bank account)", CreditValue = 80, DebitValue = 0 },
                new { Text = "Total", RemoteAccount = string.Empty, CreditValue = 200, DebitValue = 0 },
                new { Text = "Balance", RemoteAccount = string.Empty, CreditValue = 200, DebitValue = 0 }
            });
    }

    [CulturedFact("en")]
    public void AccountSelectionCommand_SampleBookingsShoes_AccountJournalUpdated()
    {
        var sut = CreateSut();
        var project = Samples.SampleProject;
        project.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        sut.ProjectData.Load(project);

        sut.Accounts.AccountSelectionCommand.Execute(sut.Accounts.AccountList.Single(x => x.Identifier == 600));

        sut.AccountJournal.Items.Should().BeEquivalentTo(
            new[]
            {
                new { Text = "Shoes1", RemoteAccount = "100 (Bank account)", CreditValue = 0, DebitValue = 20 },
                new { Text = "Shoes2", RemoteAccount = "100 (Bank account)", CreditValue = 0, DebitValue = 30 },
                new { Text = "Total", RemoteAccount = string.Empty, CreditValue = 0, DebitValue = 50 },
                new { Text = "Balance", RemoteAccount = string.Empty, CreditValue = 0, DebitValue = 50 }
            });
    }

    [Fact]
    public void NewProjectCommand_ProjectInitialized()
    {
        var sut = CreateSut();

        sut.Menu.NewProjectCommand.Execute(null);

        sut.Accounts.AccountList.Should().NotBeEmpty();
        sut.FullJournal.Items.Should().BeEmpty();
        sut.AccountJournal.Items.Should().BeEmpty();
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
        windowManager
            .ShowDialogAsync(Arg.Do<object>(UpdateAction), Arg.Any<object>(), Arg.Any<IDictionary<string, object>>())
            .Returns(true);
        sut.ProjectData.Load(Samples.SampleProject);

        sut.NewAccountCommand.Execute(null);

        sut.Accounts.AccountList.Select(x => x.Name).Should().Equal(
            "Bank account", "Salary", "New Account", "Shoes", "Carryforward", "Bank credit", "Friends debit",
            "Active empty Asset", "Active empty Income", "Active empty Expense", "Active empty Credit",
            "Active empty Debit", "Active empty Carryforward");
    }

    [Fact]
    public void EditAccountCommand_Abort_AllDataUpdated()
    {
        var sut = CreateSut(out IWindowManager windowManager);
        sut.ProjectData.Load(Samples.SampleProject);

        sut.EditAccountCommand.Execute(sut.Accounts.AccountList[0]);

        sut.ProjectData.IsModified.Should().BeFalse();
        windowManager.Received(1).ShowDialogAsync(
            Arg.Any<object>(), Arg.Any<object>(), Arg.Any<IDictionary<string, object>>());
    }

    [CulturedFact("en")]
    public void EditAccountCommand_Confirmed_AllDataUpdated()
    {
        var sut = CreateSut(out IWindowManager windowManager);
        windowManager
            .ShowDialogAsync(
                Arg.Do<object>(
                    model =>
                    {
                        var vm = (AccountViewModel)model;
                        vm.Identifier += 1000;
                    }), Arg.Any<object>(), Arg.Any<IDictionary<string, object>>())
            .Returns(true);
        sut.ProjectData.Load(Samples.SampleProject);
        var booking = new AccountingDataJournalBooking
        {
            Date = DateTime.Now.ToAccountingDate(),
            ID = 1,
            Credit = [new BookingValue { Account = 990, Text = "Init", Value = 42 }],
            Debit = [new BookingValue { Account = 100, Text = "Init", Value = 42 }]
        };
        sut.ProjectData.CurrentYear!.Booking.Add(booking);
        booking = new AccountingDataJournalBooking
        {
            Date = DateTime.Now.ToAccountingDate(),
            ID = 2,
            Credit = [new BookingValue { Account = 100, Text = "Back", Value = 5 }],
            Debit = [new BookingValue { Account = 990, Text = "Back", Value = 5 }]
        };
        sut.ProjectData.CurrentYear!.Booking.Add(booking);
        sut.Accounts.AccountSelectionCommand.Execute(
            sut.Accounts.AccountList.FirstOrDefault(x => x.Identifier == 990));

        sut.EditAccountCommand.Execute(sut.Accounts.AccountList[0]);

        using (new AssertionScope())
        {
            sut.ProjectData.IsModified.Should().BeTrue();
            sut.Accounts.AccountList.Select(x => x.Name).Should().Equal(
                "Salary", "Shoes", "Carryforward", "Bank account", "Bank credit", "Friends debit",
                "Active empty Asset", "Active empty Income", "Active empty Expense", "Active empty Credit",
                "Active empty Debit", "Active empty Carryforward");
            sut.FullJournal.Items.Should().BeEquivalentTo(
                new[]
                {
                    new { CreditAccount = "990 (Carryforward)", DebitAccount = "1100 (Bank account)" },
                    new { CreditAccount = "1100 (Bank account)", DebitAccount = "990 (Carryforward)" }
                });
            sut.AccountJournal.Items.Should().BeEquivalentTo(
                new object[]
                {
                    new { RemoteAccount = "1100 (Bank account)" }, new { RemoteAccount = "1100 (Bank account)" },
                    new { Text = "Total" }, new { Text = "Balance" }
                });
        }
    }

    [Fact]
    public void EditAccountCommand_NullParameter_JustIgnored()
    {
        var sut = CreateSut(out IWindowManager windowManager);
        sut.ProjectData.Load(Samples.SampleProject);

        sut.EditAccountCommand.Execute(null);

        sut.ProjectData.IsModified.Should().BeFalse();
        windowManager.DidNotReceive().ShowDialogAsync(
            Arg.Any<object>(), Arg.Any<object>(), Arg.Any<IDictionary<string, object>>());
    }

    [Fact]
    public void AddBookingsCommand_ClosedYear_CannotExecute()
    {
        var sut = CreateSut();
        sut.ProjectData.Load(Samples.SampleProject);
        sut.Menu.BookingYears[0].Command.Execute(null);
        sut.ProjectData.CurrentYear.Closed.Should().BeTrue();

        sut.Menu.AddBookingsCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void AddBookingsCommand_OpenYear_CanExecute()
    {
        var sut = CreateSut();
        sut.ProjectData.Load(Samples.SampleProject);
        sut.Menu.BookingYears[^1].Command.Execute(null);
        sut.ProjectData.CurrentYear.Closed.Should().BeFalse();

        sut.Menu.AddBookingsCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void AddBookingsCommand_HappyPath_DataTodayFromClock()
    {
        var sut = CreateSut(out IWindowManager windowManager, out IClock clock);
        EditBookingViewModel vm = null;
        windowManager.ShowDialogAsync(
            Arg.Do<object>(model => vm = model as EditBookingViewModel), Arg.Any<object>(),
            Arg.Any<IDictionary<string, object>>());
        clock.Now().Returns(new DateTime(2024, 2, 2, 5, 6, 7, DateTimeKind.Local));
        sut.ProjectData.Load(Samples.SampleProject);
        sut.Menu.BookingYears[^1].Command.Execute(null);

        sut.Menu.AddBookingsCommand.Execute(null);

        vm.Date.Should().Be(new DateTime(2024, 2, 2, 0, 0, 0, DateTimeKind.Local));
    }

    [Fact]
    public void AddBookingsCommand_ShowInactiveAccounts_DialogInitialized()
    {
        var sut = CreateSut(out IWindowManager windowManager);
        EditBookingViewModel vm = null;
        windowManager.ShowDialogAsync(
            Arg.Do<object>(model => vm = model as EditBookingViewModel), Arg.Any<object>(),
            Arg.Any<IDictionary<string, object>>());
        sut.ProjectData.Load(Samples.SampleProject);
        sut.Accounts.ShowInactiveAccounts = true;

        sut.Menu.AddBookingsCommand.Execute(null);

        using var _ = new AssertionScope();
        vm.BookingIdentifier.Should().Be(1);
        vm.Accounts.Should().BeEquivalentTo(Samples.SampleProject.AllAccounts);
    }

    [Fact]
    public void AddBookingsCommand_HideInactiveAccounts_OnlyActiveAccountsVisible()
    {
        var sut = CreateSut(out IWindowManager windowManager);
        EditBookingViewModel vm = null;
        windowManager.ShowDialogAsync(
            Arg.Do<object>(model => vm = model as EditBookingViewModel), Arg.Any<object>(),
            Arg.Any<IDictionary<string, object>>());
        sut.ProjectData.Load(Samples.SampleProject);
        sut.Accounts.ShowInactiveAccounts = false;

        sut.Menu.AddBookingsCommand.Execute(null);

        using var _ = new AssertionScope();
        vm.BookingIdentifier.Should().Be(1);
        vm.Accounts.Should().BeEquivalentTo(Samples.SampleProject.AllAccounts.Where(x => x.Active));
    }

    [Fact]
    public void AddBookingsCommand_BookingTemplatesDefined_BookingTemplatesTransformed()
    {
        var sut = CreateSut(out IWindowManager windowManager);
        EditBookingViewModel vm = null;
        windowManager.ShowDialogAsync(
            Arg.Do<object>(model => vm = model as EditBookingViewModel), Arg.Any<object>(),
            Arg.Any<IDictionary<string, object>>());
        sut.ProjectData.Load(Samples.SampleProject);
        sut.ProjectData.Storage.Setup.BookingTemplates = new AccountingDataSetupBookingTemplates
        {
            Template =
            [
                new AccountingDataSetupBookingTemplatesTemplate { Text = "Template1" },
                new AccountingDataSetupBookingTemplatesTemplate
                {
                    Text = "Template2",
                    Credit = 1,
                    Debit = 2,
                    Value = 3,
                    CreditSpecified = true,
                    DebitSpecified = true,
                    ValueSpecified = true
                }
            ]
        };

        sut.Menu.AddBookingsCommand.Execute(null);

        using var _ = new AssertionScope();
        vm.BindingTemplates.Should().BeEquivalentTo(
            new[]
            {
                new { Text = "Template1", Credit = 0, Debit = 0, Value = 0.0 },
                new { Text = "Template2", Credit = 1, Debit = 2, Value = 0.03 }
            });
    }

    [Fact]
    public void EditBookingCommand_NoSave_DialogInitialized()
    {
        var sut = CreateSut(out IWindowManager windowManager);
        EditBookingViewModel vm = null;
        windowManager.ShowDialogAsync(
            Arg.Do<object>(model => vm = model as EditBookingViewModel), Arg.Any<object>(),
            Arg.Any<IDictionary<string, object>>());
        var project = Samples.SampleProject;
        project.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        sut.ProjectData.Load(project);

        sut.Menu.EditBookingCommand.Execute(sut.FullJournal.Items[^1]);

        using var _ = new AssertionScope();
        sut.ProjectData.IsModified.Should().BeFalse("the project remains unchanged");
        vm.BookingIdentifier.Should().Be(6);
        vm.BookingText.Should().Be("Rent to friend");
        vm.Accounts.Should().BeEquivalentTo(Samples.SampleProject.AllAccounts.Where(x => x.Active));
    }

    [Fact]
    public void EditBookingCommand_SplitBookingCredit_SplitViewModelInitialized()
    {
        var sut = CreateSut(out IWindowManager windowManager);
        EditBookingViewModel vm = null;
        windowManager.ShowDialogAsync(
            Arg.Do<object>(model => vm = model as EditBookingViewModel), Arg.Any<object>(),
            Arg.Any<IDictionary<string, object>>());
        var project = Samples.SampleProject;
        project.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        sut.ProjectData.Load(project);

        sut.Menu.EditBookingCommand.Execute(sut.FullJournal.Items.First(x => x.Identifier == 3));

        using var _ = new AssertionScope();
        vm.CreditSplitEntries.Should().BeEquivalentTo(
            new[]
            {
                new { AccountNumber = Samples.Salary, BookingText = "Salary1", BookingValue = 120 },
                new { AccountNumber = Samples.Salary, BookingText = "Salary2", BookingValue = 80 }
            });
    }

    [Fact]
    public void EditBookingCommand_SplitBookingDebit_SplitViewModelInitialized()
    {
        var sut = CreateSut(out IWindowManager windowManager);
        EditBookingViewModel vm = null;
        windowManager.ShowDialogAsync(
            Arg.Do<object>(model => vm = model as EditBookingViewModel), Arg.Any<object>(),
            Arg.Any<IDictionary<string, object>>());
        var project = Samples.SampleProject;
        project.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        sut.ProjectData.Load(project);

        sut.Menu.EditBookingCommand.Execute(sut.FullJournal.Items.First(x => x.Identifier == 5));

        using var _ = new AssertionScope();
        vm.DebitSplitEntries.Should().BeEquivalentTo(
            new[]
            {
                new { AccountNumber = Samples.Shoes, BookingText = "Shoes1", BookingValue = 20 },
                new { AccountNumber = Samples.Shoes, BookingText = "Shoes2", BookingValue = 30 }
            });
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
        windowManager
            .ShowDialogAsync(Arg.Do<object>(UpdateAction), Arg.Any<object>(), Arg.Any<IDictionary<string, object>>())
            .Returns(true);
        var project = Samples.SampleProject;
        project.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        sut.ProjectData.Load(project);

        sut.Menu.EditBookingCommand.Execute(sut.FullJournal.Items[^1]);

        using var _ = new AssertionScope();
        sut.ProjectData.IsModified.Should().BeTrue("the project changed");
        sut.FullJournal.Items[^1].Should().BeEquivalentTo(
            new { Identifier = 106, Value = 199.0, Text = "Rent to friend Paul" });
    }

    [Fact]
    public void EditBookingCommand_NullParameter_JustIgnored()
    {
        var sut = CreateSut(out IWindowManager windowManager);
        var project = Samples.SampleProject;
        project.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        sut.ProjectData.Load(project);

        sut.Menu.EditBookingCommand.Execute(null);

        using var _ = new AssertionScope();
        sut.ProjectData.IsModified.Should().BeFalse("the project remains unchanged");
        windowManager.DidNotReceive().ShowDialogAsync(
            Arg.Any<object>(), Arg.Any<object>(), Arg.Any<IDictionary<string, object>>());
    }

    [Fact]
    public void DuplicateBookingsCommand_NoSave_DialogInitialized()
    {
        var sut = CreateSut(out IWindowManager windowManager);
        EditBookingViewModel vm = null;
        windowManager.ShowDialogAsync(
            Arg.Do<object>(model => vm = model as EditBookingViewModel), Arg.Any<object>(),
            Arg.Any<IDictionary<string, object>>());
        var project = Samples.SampleProject;
        project.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        sut.ProjectData.Load(project);

        sut.Menu.DuplicateBookingsCommand.Execute(sut.FullJournal.Items[^1]);

        using var _ = new AssertionScope();
        sut.ProjectData.IsModified.Should().BeFalse("the project remains unchanged");
        vm.BookingIdentifier.Should().Be(sut.FullJournal.Items[^1].Identifier + 1);
        vm.BookingText.Should().Be("Rent to friend");
        vm.Accounts.Should().BeEquivalentTo(Samples.SampleProject.AllAccounts.Where(x => x.Active));
    }

    [Fact]
    public void ImportBookingsCommand_BookingNumberInitialized()
    {
        var sut = CreateSut(out IWindowManager windowManager);
        ImportBookingsViewModel vm = null;
        windowManager.ShowDialogAsync(
            Arg.Do<object>(model => vm = model as ImportBookingsViewModel), Arg.Any<object>(),
            Arg.Any<IDictionary<string, object>>());
        sut.ProjectData.Load(Samples.SampleProject);

        sut.Menu.ImportBookingsCommand.Execute(null);

        using (new AssertionScope())
        {
            vm.Should().BeEquivalentTo(
                new
                {
                    FirstBookingNumber = 1,
                    RangeMin = new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 0, DateTimeKind.Local),
                    RangeMax = new DateTime(DateTime.Now.Year, 12, 31, 0, 0, 0, DateTimeKind.Local)
                });
            vm.ImportAccounts.Should().NotBeEmpty();
        }
    }

    [Fact]
    public void ImportBookingsCommand_ClosedYear_CannotExecute()
    {
        var sut = CreateSut();
        sut.ProjectData.Load(Samples.SampleProject);
        sut.Menu.BookingYears[0].Command.Execute(null);

        sut.Menu.ImportBookingsCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ImportBookingsCommand_OpenYear_CanExecute()
    {
        var sut = CreateSut();
        sut.ProjectData.Load(Samples.SampleProject);
        sut.Menu.BookingYears[^1].Command.Execute(null);

        sut.Menu.ImportBookingsCommand.CanExecute(null).Should().BeTrue();
    }

    [CulturedFact("en")]
    public void CloseYearCommand_HappyPath_YearClosedAndNewAdded()
    {
        var sut = CreateSut(out IWindowManager windowManager);
        windowManager.ShowDialogAsync(
            Arg.Any<CloseYearViewModel>(),
            Arg.Any<object>(),
            Arg.Any<IDictionary<string, object>>()).Returns(
            info =>
            {
                var vm = info.Arg<CloseYearViewModel>();
                vm.RemoteAccount = vm.Accounts[0];
                return true;
            });
        var project = Samples.SampleProject;
        project.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        sut.ProjectData.Load(project);

        sut.Menu.CloseYearCommand.Execute(null);

        windowManager.Received(1).ShowDialogAsync(
            Arg.Any<CloseYearViewModel>(),
            Arg.Any<object>(),
            Arg.Any<IDictionary<string, object>>());
        var thisYear = DateTime.Now.Year;
        using var _ = new AssertionScope();
        sut.Menu.BookingYears.Select(x => x.Header).Should()
            .Equal(
                "2000", thisYear.ToString(CultureInfo.InvariantCulture),
                (thisYear + 1).ToString(CultureInfo.InvariantCulture));
        sut.FullJournal.Items.Should().BeEquivalentTo(
            new[]
            {
                new
                {
                    Identifier = 1,
                    Date = new DateTime(thisYear + 1, 1, 1, 0, 0, 0, DateTimeKind.Local),
                    Text = "Opening value 1",
                    Value = 651,
                    CreditAccount = "990 (Carryforward)",
                    DebitAccount = "100 (Bank account)"
                },
                new
                {
                    Identifier = 2,
                    Date = new DateTime(thisYear + 1, 1, 1, 0, 0, 0, DateTimeKind.Local),
                    Text = "Opening value 2",
                    Value = 2600,
                    CreditAccount = "5000 (Bank credit)",
                    DebitAccount = "990 (Carryforward)"
                },
                new
                {
                    Identifier = 3,
                    Date = new DateTime(thisYear + 1, 1, 1, 0, 0, 0, DateTimeKind.Local),
                    Text = "Opening value 3",
                    Value = 99,
                    CreditAccount = "990 (Carryforward)",
                    DebitAccount = "6000 (Friends debit)"
                }
            });
        sut.AccountJournal.Items.Should().BeEquivalentTo(
            new object[]
            {
                new
                {
                    Identifier = 1,
                    Date = new DateTime(thisYear + 1, 1, 1, 0, 0, 0, DateTimeKind.Local),
                    Text = "Opening value 1",
                    DebitValue = 651,
                    CreditValue = 0,
                    RemoteAccount = "990 (Carryforward)"
                },
                new { Text = "Total", IsSummary = true, DebitValue = 651, CreditValue = 0 },
                new { Text = "Balance", IsSummary = true, DebitValue = 651, CreditValue = 0 }
            });
    }

    [CulturedFact("en")]
    public void CloseYearCommand_SecondCarryForwardAccount_OpeningsWithSelectedAccount()
    {
        const ulong myCarryForwardNumber = 999;
        var sut = CreateSut(out IWindowManager windowManager);
        windowManager.ShowDialogAsync(
            Arg.Any<CloseYearViewModel>(),
            Arg.Any<object>(),
            Arg.Any<IDictionary<string, object>>()).Returns(
            info =>
            {
                var vm = info.Arg<CloseYearViewModel>();
                vm.RemoteAccount = vm.Accounts.Single(x => x.ID == myCarryForwardNumber);
                return true;
            });
        var project = Samples.SampleProject;
        project.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        project.Accounts[0].Account.Add(
            new AccountDefinition
            {
                ID = myCarryForwardNumber, Name = "MyCarryForward", Type = AccountDefinitionType.Carryforward
            });
        sut.ProjectData.Load(project);

        sut.Menu.CloseYearCommand.Execute(null);

        windowManager.Received(1).ShowDialogAsync(
            Arg.Any<CloseYearViewModel>(),
            Arg.Any<object>(),
            Arg.Any<IDictionary<string, object>>());
        var thisYear = DateTime.Now.Year;
        using var _ = new AssertionScope();
        sut.Menu.BookingYears.Select(x => x.Header).Should()
            .Equal(
                "2000", thisYear.ToString(CultureInfo.InvariantCulture),
                (thisYear + 1).ToString(CultureInfo.InvariantCulture));
        sut.FullJournal.Items.Should().BeEquivalentTo(
            new[]
            {
                new
                {
                    Identifier = 1,
                    Date = new DateTime(thisYear + 1, 1, 1, 0, 0, 0, DateTimeKind.Local),
                    Text = "Opening value 1",
                    Value = 651,
                    CreditAccount = "999 (MyCarryForward)",
                    DebitAccount = "100 (Bank account)"
                },
                new
                {
                    Identifier = 2,
                    Date = new DateTime(thisYear + 1, 1, 1, 0, 0, 0, DateTimeKind.Local),
                    Text = "Opening value 2",
                    Value = 2600,
                    CreditAccount = "5000 (Bank credit)",
                    DebitAccount = "999 (MyCarryForward)"
                },
                new
                {
                    Identifier = 3,
                    Date = new DateTime(thisYear + 1, 1, 1, 0, 0, 0, DateTimeKind.Local),
                    Text = "Opening value 3",
                    Value = 99,
                    CreditAccount = "999 (MyCarryForward)",
                    DebitAccount = "6000 (Friends debit)"
                }
            });
        sut.AccountJournal.Items.Should().BeEquivalentTo(
            new object[]
            {
                new
                {
                    Identifier = 1,
                    Date = new DateTime(thisYear + 1, 1, 1, 0, 0, 0, DateTimeKind.Local),
                    Text = "Opening value 1",
                    DebitValue = 651,
                    CreditValue = 0,
                    RemoteAccount = "999 (MyCarryForward)"
                },
                new { Text = "Total", IsSummary = true, DebitValue = 651, CreditValue = 0 },
                new { Text = "Balance", IsSummary = true, DebitValue = 651, CreditValue = 0 }
            });
    }

    [Fact]
    public async Task HelpCheckForUpdateCommand_StartProcessFailed_Aborted()
    {
        var sut = CreateSut(out IApplicationUpdate applicationUpdate, out var dialogs);
        dialogs.ShowMessageBox(
                Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.No);
        sut.ProjectData.IsModified = true;
        applicationUpdate.GetUpdatePackageAsync(Arg.Any<string>(), Arg.Any<CultureInfo>()).Returns("true");
        applicationUpdate.StartUpdateProcess("foo.zip").Returns(false);
        var monitor = sut.Monitor();

        await sut.Awaiting(x => x.HelpCheckForUpdateCommand.ExecuteAsync(null)).Should()
            .CompleteWithinAsync(1.Seconds());

        sut.ProjectData.IsModified.Should().BeTrue("unsaved project remains unsaved");
        monitor.Should().NotRaise(nameof(sut.Deactivated));
    }

    [Fact]
    public async Task HelpCheckForUpdateCommand_StartProcessSucceed_ProjectClosed()
    {
        var sut = CreateSut(out IApplicationUpdate applicationUpdate, out var dialogs);
        dialogs.ShowMessageBox(
                Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
            .Returns(MessageBoxResult.No);
        sut.ProjectData.IsModified = true;
        applicationUpdate.GetUpdatePackageAsync(Arg.Any<string>(), Arg.Any<CultureInfo>()).Returns("package-name.zip");
        applicationUpdate.StartUpdateProcess("package-name.zip").Returns(true);
        var monitor = sut.Monitor();

        await sut.Awaiting(x => x.HelpCheckForUpdateCommand.ExecuteAsync(null)).Should()
            .CompleteWithinAsync(1.Seconds());

        sut.ProjectData.IsModified.Should().BeFalse();
        monitor.Should().NotRaise(nameof(sut.Deactivated));
    }

    [Fact]
    public async Task HelpCheckForUpdateCommand_NoUpdateAvailable_UpdateProcessNotStarted()
    {
        var sut = CreateSut(out IApplicationUpdate applicationUpdate, out _);
        applicationUpdate.GetUpdatePackageAsync(Arg.Any<string>(), Arg.Any<CultureInfo>()).Returns("false");

        await sut.Awaiting(x => x.HelpCheckForUpdateCommand.ExecuteAsync(null)).Should()
            .CompleteWithinAsync(1.Seconds());

        applicationUpdate.DidNotReceive().StartUpdateProcess("foo.zip");
    }

    [Fact]
    public async Task HelpCheckForUpdateCommand_UserDoesNotWantToSave_UpdateProcessNotStarted()
    {
        var sut = CreateSut(out IApplicationUpdate applicationUpdate, out _);
        applicationUpdate.GetUpdatePackageAsync(Arg.Any<string>(), Arg.Any<CultureInfo>()).Returns("true");
        sut.ProjectData.IsModified = true;

        await sut.Awaiting(x => x.HelpCheckForUpdateCommand.ExecuteAsync(null)).Should()
            .CompleteWithinAsync(1.Seconds());

        applicationUpdate.DidNotReceive().StartUpdateProcess("foo.zip");
    }
}
