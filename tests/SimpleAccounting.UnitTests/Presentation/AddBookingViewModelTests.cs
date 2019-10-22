// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace SimpleAccounting.UnitTests.Presentation
{
    using Caliburn.Micro;
    using FluentAssertions;
    using lg2de.SimpleAccounting.Presentation;
    using NSubstitute;
    using Xunit;

    public class AddBookingViewModelTests
    {
        [Fact]
        public void BookCommand_FirstBooking_BookingNumberIncremented()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var messageBox = Substitute.For<IMessageBox>();
            var parent = new ShellViewModel(windowManager, messageBox);
            parent.LoadProjectData(Samples.SampleProject);
            var sut = new AddBookingViewModel(parent);

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
    }
}