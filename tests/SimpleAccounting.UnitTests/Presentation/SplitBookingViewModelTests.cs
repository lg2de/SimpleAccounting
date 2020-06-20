// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation
{
    using System.Collections.Generic;
    using FluentAssertions;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Presentation;
    using Xunit;

    public class SplitBookingViewModelTests
    {
        [Fact]
        public void IsConsistent_Expected_ReturnsTrue()
        {
            var sut = new List<SplitBookingViewModel>
            {
                new SplitBookingViewModel { BookingText = "X", BookingValue = 1, AccountIndex = 1 },
                new SplitBookingViewModel { BookingText = "Y", BookingValue = 2, AccountIndex = 2 }
            };

            sut.IsConsistent(3).Should().BeTrue();
        }

        [Fact]
        public void IsConsistent_AnyItemZero_ReturnsFalse()
        {
            var sut = new List<SplitBookingViewModel>
            {
                new SplitBookingViewModel { BookingText = "X", BookingValue = 0, AccountIndex = 1 },
                new SplitBookingViewModel { BookingText = "Y", BookingValue = 1, AccountIndex = 2 }
            };

            sut.IsConsistent(1).Should().BeFalse();
        }

        [Fact]
        public void IsConsistent_AnyItemWithoutText_ReturnsFalse()
        {
            var sut = new List<SplitBookingViewModel>
            {
                new SplitBookingViewModel { BookingText = "", BookingValue = 1, AccountIndex = 1 },
                new SplitBookingViewModel { BookingText = "Y", BookingValue = 2, AccountIndex = 2 }
            };

            sut.IsConsistent(3).Should().BeFalse();
        }

        [Fact]
        public void IsConsistent_AnyItemWithoutAccount_ReturnsFalse()
        {
            var sut = new List<SplitBookingViewModel>
            {
                new SplitBookingViewModel { BookingText = "X", BookingValue = 1, AccountIndex = -1 },
                new SplitBookingViewModel { BookingText = "Y", BookingValue = 2, AccountIndex = 2 }
            };

            sut.IsConsistent(3).Should().BeFalse();
        }

        [Fact]
        public void IsConsistent_DifferentSum_ReturnsFalse()
        {
            var sut = new List<SplitBookingViewModel>
            {
                new SplitBookingViewModel { BookingText = "X", BookingValue = 1, AccountIndex = 1 },
                new SplitBookingViewModel { BookingText = "Y", BookingValue = 2, AccountIndex = 2 }
            };

            sut.IsConsistent(9).Should().BeFalse();
        }
    }
}
