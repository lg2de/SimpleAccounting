// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace SimpleAccounting.UnitTests.Presentation
{
    using System;
    using Caliburn.Micro;
    using FluentAssertions;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Presentation;
    using lg2de.SimpleAccounting.Reports;
    using NSubstitute;
    using Xunit;

    public class AddBookingViewModelTests
    {
        private static DateTime YearBegin = new DateTime(DateTime.Now.Year, 1, 1);
        private static DateTime YearEnd = new DateTime(DateTime.Now.Year, 12, 31);

        [Fact]
        public void OnInitialize_Initialized()
        {
            var sut = new AddBookingViewModel(null, YearBegin, YearEnd);

            ((IActivate)sut).Activate();

            sut.DisplayName.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void BookCommand_FirstBooking_BookingNumberIncremented()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var parent = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem);
            parent.LoadProjectData(Samples.SampleProject);
            var sut = new AddBookingViewModel(parent, YearBegin, YearEnd);

            var oldNumber = sut.BookingNumber;
            sut.CreditAccount = 100;
            sut.DebitAccount = 990;

            using (var monitor = sut.Monitor())
            {
                sut.BookCommand.Execute(null);

                sut.BookingNumber.Should().Be(oldNumber + 1);
                monitor.Should().RaisePropertyChangeFor(m => m.BookingNumber);
            }
        }

        [Fact]
        public void BookCommand_InvalidYear_CannotExecute()
        {
            var sut = new AddBookingViewModel(null, YearBegin, YearEnd)
            {
                BookingNumber = 1,
                BookingText = "abc",
                CreditIndex = 1,
                DebitIndex = 2,
                BookingValue = 42,
                Date = YearEnd + TimeSpan.FromDays(1)
            };

            sut.BookCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void BookCommand_MissingCredit_CannotExecute()
        {
            var sut = new AddBookingViewModel(null, YearBegin, YearEnd)
            {
                BookingNumber = 1,
                BookingText = "abc",
                DebitIndex = 2,
                BookingValue = 42
            };

            sut.BookCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void BookCommand_MissingDebit_CannotExecute()
        {
            var sut = new AddBookingViewModel(null, YearBegin, YearEnd)
            {
                BookingNumber = 1,
                BookingText = "abc",
                CreditIndex = 1,
                BookingValue = 42
            };

            sut.BookCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void BookCommand_MissingNumber_CannotExecute()
        {
            var sut = new AddBookingViewModel(null, YearBegin, YearEnd)
            {
                BookingText = "abc",
                CreditIndex = 1,
                DebitIndex = 2,
                BookingValue = 42
            };

            sut.BookCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void BookCommand_MissingText_CannotExecute()
        {
            var sut = new AddBookingViewModel(null, YearBegin, YearEnd)
            {
                BookingNumber = 1,
                CreditIndex = 1,
                DebitIndex = 2,
                BookingValue = 42
            };

            sut.BookCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void BookCommand_MissingValue_CannotExecute()
        {
            var sut = new AddBookingViewModel(null, YearBegin, YearEnd)
            {
                BookingNumber = 1,
                BookingText = "abc",
                CreditIndex = 1,
                DebitIndex = 2
            };

            sut.BookCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void BookCommand_SameAccount_CannotExecute()
        {
            var sut = new AddBookingViewModel(null, YearBegin, YearEnd)
            {
                BookingNumber = 1,
                BookingText = "abc",
                CreditIndex = 1,
                DebitIndex = 1,
                BookingValue = 42
            };

            sut.BookCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void BookCommand_ValidValues_CanExecute()
        {
            var sut = new AddBookingViewModel(null, YearBegin, YearEnd)
            {
                BookingNumber = 1,
                BookingText = "abc",
                CreditIndex = 1,
                DebitIndex = 2,
                BookingValue = 42
            };

            sut.BookCommand.CanExecute(null).Should().BeTrue();
        }
    }
}