// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using FluentAssertions;
using lg2de.SimpleAccounting.Presentation;
using Xunit;

public class SplitBookingViewModelTests
{
    [Fact]
    public void IsBookingTextErrorThickness_BlankText_ErrorShown()
    {
        var sut = new SplitBookingViewModel { BookingText = " " };

        sut.IsBookingTextErrorThickness.Should().Be(1);
    }
        
    [Fact]
    public void IsBookingTextErrorThickness_ValueText_ErrorHidden()
    {
        var sut = new SplitBookingViewModel { BookingText = "X" };

        sut.IsBookingTextErrorThickness.Should().Be(0);
    }

    [Fact]
    public void IsBookingTextErrorThickness_BlankTextRemoved_ErrorDisappears()
    {
        var sut = new SplitBookingViewModel { BookingText = " " };
        using var monitor = sut.Monitor();

        sut.BookingText = "X";
            
        sut.IsBookingTextErrorThickness.Should().Be(0);
        monitor.Should().RaisePropertyChangeFor(x => x.IsBookingTextErrorThickness);
    }

    [Fact]
    public void IsBookingTextErrorThickness_TextRemoved_ErrorAppears()
    {
        var sut = new SplitBookingViewModel { BookingText = "X" };
        using var monitor = sut.Monitor();

        sut.BookingText = " ";
            
        sut.IsBookingTextErrorThickness.Should().Be(1);
        monitor.Should().RaisePropertyChangeFor(x => x.IsBookingTextErrorThickness);
    }
}
