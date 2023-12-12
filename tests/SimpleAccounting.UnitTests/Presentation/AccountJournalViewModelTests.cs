// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using System.Linq;
using FluentAssertions;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Presentation;
using Xunit;

public class AccountJournalViewModelTests
{
    private const ulong TestAccountNumber = Samples.BankAccount;
    private const ulong OtherAccountNumber = Samples.Carryforward;

    [CulturedTheory("en")]
    [InlineData(true)]
    [InlineData(false)]
    public void Rebuild_Variations_FollowupBuildCorrectly(bool followup)
    {
        var projectData = Samples.SampleProjectData;
        uint date = projectData.CurrentYear.DateStart;
        projectData.CurrentYear.Booking.AddRange(
            collection: new[]
            {
                CreateBooking(date, identifier: 1, creditCount: 1, debitCount: 1, followup),
                CreateBooking(date, identifier: 2, creditCount: 1, debitCount: 2, followup),
                CreateBooking(date, identifier: 3, creditCount: 2, debitCount: 1, followup)
            });
        var sut = new AccountJournalViewModel(projectData: projectData);

        sut.Rebuild(accountNumber: TestAccountNumber);

        sut.Items.Should().BeEquivalentTo(
            expectation: new[]
            {
                new { Identifier = 1, RemoteAccount = "990 (Carryforward)", IsFollowup = followup },
                new { Identifier = 2, RemoteAccount = "Various", IsFollowup = followup },
                new { Identifier = 3, RemoteAccount = "Various", IsFollowup = followup }
            }, config: o => o.WithStrictOrdering());
    }

    [Fact]
    public void Rebuild_MultipleBookingsPerDay_OrderedByDateAndIdentifier()
    {
        var projectData = Samples.SampleProjectData;
        uint date = projectData.CurrentYear.DateStart;
        projectData.CurrentYear.Booking.AddRange(
            collection: new[]
            {
                CreateBooking(date, identifier: 5, creditCount: 1, debitCount: 1, followup: false),
                CreateBooking(date, identifier: 4, creditCount: 1, debitCount: 1, followup: false),
                CreateBooking(date, identifier: 5, creditCount: 1, debitCount: 1, followup: false),
                CreateBooking(date, identifier: 4, creditCount: 1, debitCount: 1, followup: false)
            });
        var sut = new AccountJournalViewModel(projectData: projectData);

        sut.Rebuild(accountNumber: TestAccountNumber);

        sut.Items.Should().BeEquivalentTo(
            expectation: new[]
            {
                new { Identifier = 4 }, new { Identifier = 4 }, new { Identifier = 5 }, new { Identifier = 5 }
            }, config: o => o.WithStrictOrdering());
    }

    private static AccountingDataJournalBooking CreateBooking(
        uint date, ulong identifier, int creditCount, int debitCount, bool followup)
    {
        bool isCreditFocused = creditCount == 1 && debitCount == 2 || debitCount > creditCount;
        var accountNumber = isCreditFocused ? TestAccountNumber : OtherAccountNumber;
        var creditList = Enumerable.Range(start: 1, count: creditCount)
            .Select(selector: _ => new BookingValue { Text = "dummy", Account = accountNumber }).ToList();
        accountNumber = isCreditFocused ? OtherAccountNumber : TestAccountNumber;
        var debitList = Enumerable.Range(start: 1, count: debitCount)
            .Select(selector: _ => new BookingValue { Text = "dummy", Account = accountNumber }).ToList();
        return new AccountingDataJournalBooking
        {
            Date = date,
            ID = identifier,
            Credit = creditList,
            Debit = debitList,
            Followup = followup
        };
    }
}
