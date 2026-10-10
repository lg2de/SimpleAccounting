// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

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

    [Theory]
    [InlineData(false, null)]
    [InlineData(true, "$")]
    public void DisplayCurrency_ShowCurrencyVariations_ProvidedCorrectly(bool show, string expected)
    {
        var projectData = Samples.SampleProjectData;
        projectData.Storage.Setup.Currency = new AccountingDataSetupCurrency { Value = "$", Show = show };
        var sut = new FullJournalViewModel(projectData);

        sut.DisplayCurrency.Should().Be(expected);
    }

    [Fact]
    public void RefreshCurrency_PropertyChangedRaised()
    {
        var sut = new FullJournalViewModel(Samples.SampleProjectData);
        using var monitor = sut.Monitor();

        sut.RefreshCurrency();

        monitor.Should().RaisePropertyChangeFor(x => x.DisplayCurrency);
    }

    private static AccountingDataJournalBooking CreateBooking(uint date, ulong identifier)
    {
        return new AccountingDataJournalBooking
        {
            Date = date,
            ID = identifier,
            Credit = [new BookingValue { Text = "dummy", Account = 100 }],
            Debit = []
        };
    }
}
