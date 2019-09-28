// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace SimpleAccounting.UnitTests.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Caliburn.Micro;
    using FluentAssertions;
    using FluentAssertions.Execution;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Presentation;
    using NSubstitute;
    using Xunit;

    public class ShellViewModelTests
    {
        private static AccountingData SampleProject => new AccountingData
        {
            Accounts = new List<AccountingDataAccountGroup>
            {
                new AccountingDataAccountGroup
                {
                    Name = "Default",
                    Account = new List<AccountDefinition>
                    {
                        new AccountDefinition
                        {
                            ID = 100, Name = "Bank account", Type = AccountDefinitionType.Asset
                        },
                        new AccountDefinition
                        {
                            ID = 990, Name = "Carryforward", Type = AccountDefinitionType.Carryforward
                        }
                    }
                }
            },
            Years = new List<AccountingDataYear>
            {
                new AccountingDataYear { Name = 2019, DateStart = 20190101, DateEnd = 20191231}
            },
            Journal = new List<AccountingDataJournal>
            {
                new AccountingDataJournal { Year = 2019, Booking = new List<AccountingDataJournalBooking>()}
            }
        };

        [Fact]
        public void NewProjectCommand_ProjectInitialized()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var sut = new ShellViewModel(windowManager);

            sut.NewProjectCommand.Execute(null);

            sut.Accounts.Should().NotBeEmpty();
            sut.Journal.Should().BeEmpty();
            sut.AccountJournal.Should().BeEmpty();
        }

        [Fact]
        public void SaveProjectCommand_Initialized_CannotExecute()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var sut = new ShellViewModel(windowManager);

            sut.SaveProjectCommand.CanExecute(null).Should()
                .BeFalse("default instance does not contain modified document");
        }

        [Fact]
        public void SaveProjectCommand_DocumentModified_CanExecute()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var sut = new ShellViewModel(windowManager);
            sut.LoadProjectData(SampleProject);

            sut.AddBooking(new AccountingDataJournalBooking(), refreshJournal: false);

            sut.SaveProjectCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AddBookingsCommand_BookingNumberInitialized()
        {
            var windowManager = Substitute.For<IWindowManager>();
            AddBookingViewModel vm = null;
            windowManager.ShowDialog(Arg.Do<object>(model => vm = model as AddBookingViewModel));
            var sut = new ShellViewModel(windowManager);
            sut.LoadProjectData(SampleProject);

            sut.AddBookingsCommand.Execute(null);

            vm.BookingNumber.Should().Be(1);
        }

        [Fact]
        public void ImportBookingsCommand_BookingNumberInitialized()
        {
            var windowManager = Substitute.For<IWindowManager>();
            ImportBookingsViewModel vm = null;
            windowManager.ShowDialog(Arg.Do<object>(model => vm = model as ImportBookingsViewModel));
            var sut = new ShellViewModel(windowManager);
            sut.LoadProjectData(SampleProject);

            sut.ImportBookingsCommand.Execute(null);

            using (new AssertionScope())
            {
                vm.Should().BeEquivalentTo(new
                {
                    BookingNumber = 1,
                    RangeMin = new DateTime(DateTime.Now.Year, 1, 1),
                    RangMax = new DateTime(DateTime.Now.Year, 12, 31)
                });
                vm.Accounts.Should().NotBeEmpty();
            }
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
                return;
            })).Returns(true);
            var sut = new ShellViewModel(windowManager);
            sut.LoadProjectData(SampleProject);

            sut.NewAccountCommand.Execute(null);

            sut.Accounts.Select(x => x.Name).Should()
                .Equal("Bank account", "New Account", "Carryforward");
        }

        [Fact]
        public void EditAccountCommand_AllDataUpdated()
        {
            var windowManager = Substitute.For<IWindowManager>();
            windowManager.ShowDialog(Arg.Do<object>(model =>
            {
                var vm = model as AccountViewModel;
                vm.Identifier += 1000;
                return;
            })).Returns(true);
            var sut = new ShellViewModel(windowManager);
            sut.LoadProjectData(SampleProject);
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
                sut.Accounts.Select(x => x.Name).Should().Equal("Carryforward", "Bank account");
                sut.Journal.Should().BeEquivalentTo(
                    new { CreditAccount = "990 (Carryforward)", DebitAccount = "1100 (Bank account)" },
                    new { CreditAccount = "1100 (Bank account)", DebitAccount = "990 (Carryforward)" });
            }
        }

        [Fact]
        public void AddBooking_FirstBooking_JournalUpdated()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var sut = new ShellViewModel(windowManager);
            sut.LoadProjectData(SampleProject);

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
