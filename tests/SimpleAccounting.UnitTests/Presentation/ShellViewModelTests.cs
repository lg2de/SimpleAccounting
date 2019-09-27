// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace SimpleAccounting.UnitTests.Presentation
{
    using System;
    using System.Collections.Generic;
    using Caliburn.Micro;
    using FluentAssertions;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Presentation;
    using NSubstitute;
    using Xunit;

    public class ShellViewModelTests
    {
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
            sut.Initialize();

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
            sut.Initialize();

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
            sut.Initialize();

            sut.ImportBookingsCommand.Execute(null);

            vm.Should().BeEquivalentTo(new
            {
                BookingNumber = 1,
                RangeMin = new DateTime(DateTime.Now.Year, 1, 1),
                RangMax = new DateTime(DateTime.Now.Year, 12, 31)
            });
        }

        [Fact]
        public void AddBooking_FirstBooking_JournalUpdated()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var sut = new ShellViewModel(windowManager);
            sut.Initialize();

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
