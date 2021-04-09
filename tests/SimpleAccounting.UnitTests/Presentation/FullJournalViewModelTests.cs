// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation
{
    using System.Collections.Generic;
    using FluentAssertions;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Presentation;
    using Xunit;

    public class FullJournalViewModelTests
    {
        [Fact]
        public void Rebuild_MultipleBookingsPerDay_OrderedByDateAndIdentifier()
        {
            var projectData = Samples.SampleProjectData;
            uint date = projectData.CurrentYear.DateStart;
            projectData.CurrentYear.Booking.AddRange(
                new[]
                {
                    CreateBooking(date, 5),
                    CreateBooking(date, 4),
                    CreateBooking(date, 5),
                    CreateBooking(date, 4)
                });
            var sut = new FullJournalViewModel(projectData);
            
            sut.Rebuild();

            sut.Items.Should().BeEquivalentTo(
                new[]
                {
                    new { Identifier = 4 }, new { Identifier = 4 }, new { Identifier = 5 }, new { Identifier = 5 }
                }, o => o.WithStrictOrdering());
        }

        private static AccountingDataJournalBooking CreateBooking(uint date, ulong identifier)
        {
            return new AccountingDataJournalBooking
            {
                Date = date,
                ID = identifier,
                Credit = new List<BookingValue> { new BookingValue { Text = "dummy", Account = 100 } },
                Debit = new List<BookingValue>()
            };
        }
    }
}
