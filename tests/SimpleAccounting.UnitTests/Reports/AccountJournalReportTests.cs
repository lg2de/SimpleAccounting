// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Reports
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using FluentAssertions;
    using FluentAssertions.Execution;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Reports;
    using Xunit;

    public class AccountJournalReportTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CreateReport_SampleData_Converted(bool pageBreakBetweenAccounts)
        {
            var accounts = new List<AccountDefinition>
            {
                new AccountDefinition { ID = 100, Name = "Money" },
                new AccountDefinition { ID = 401, Name = "Selling" },
                new AccountDefinition { ID = 601, Name = "Buying" }
            };
            var journal = new AccountingDataJournal
            {
                Year = "2019",
                Booking = new List<AccountingDataJournalBooking>
                {
                    new AccountingDataJournalBooking
                    {
                        Date = 20190401,
                        ID = 1,
                        Opening = true,
                        Debit = new List<BookingValue>()
                        {
                            new BookingValue { Account = 100, Text = "EB", Value = 50000 }
                        },
                        Credit = new List<BookingValue>()
                        {
                            new BookingValue { Account = 900, Text = "EB", Value = 50000 }
                        }
                    },
                    new AccountingDataJournalBooking
                    {
                        Date = 20190501,
                        ID = 1,
                        Opening = true,
                        Debit = new List<BookingValue>()
                        {
                            new BookingValue { Account = 601, Text = "Shoes", Value = 5000 },
                            new BookingValue { Account = 602, Text = "Socks", Value = 5000 }
                        },
                        Credit = new List<BookingValue>()
                        {
                            new BookingValue { Account = 100, Text = "Buy", Value = 10000 }
                        }
                    },
                    new AccountingDataJournalBooking
                    {
                        Date = 20190601,
                        ID = 1,
                        Opening = true,
                        Debit = new List<BookingValue>()
                        {
                            new BookingValue { Account = 100, Text = "Sell", Value = 5000 }
                        },
                        Credit = new List<BookingValue>()
                        {
                            new BookingValue { Account = 401, Text = "Software", Value = 2500 },
                            new BookingValue { Account = 402, Text = "Hardware", Value = 2500 }
                        }
                    }
                }
            };
            var setup = new AccountingDataSetup();
            var sut = new AccountJournalReport(accounts, journal, setup, new CultureInfo("en-us"));
            sut.PageBreakBetweenAccounts = pageBreakBetweenAccounts;

            sut.CreateReport(new DateTime(2019, 1, 1), new DateTime(2019, 12, 31));

            var expectedMoney = @"
<data>
    <tr topLine=""True"">
        <td>4/1/2019</td>
        <td>1</td>
        <td>EB</td>
        <td>500.00</td>
        <td/>
        <td>900</td>
    </tr>
    <tr topLine=""True"">
        <td>5/1/2019</td>
        <td>1</td>
        <td>Buy</td>
        <td/>
        <td>100.00</td>
        <td>601</td>
    </tr>
    <tr topLine=""True"">
        <td>6/1/2019</td>
        <td>1</td>
        <td>Sell</td>
        <td>50.00</td>
        <td/>
        <td>Diverse</td>
    </tr>
    <tr topLine=""True"">
        <td align=""right"">Summen</td>
        <td />
        <td />
        <td>550.00</td>
        <td>100.00</td>
        <td />
    </tr>
    <tr topLine=""True"">
        <td align=""right"">Saldo</td>
        <td />
        <td />
        <td>450.00</td>
        <td />
        <td />
    </tr>
</data>";
            var expectedSelling = @"
<data>
    <tr topLine=""True"">
        <td>6/1/2019</td>
        <td>1</td>
        <td>Software</td>
        <td/>
        <td>25.00</td>
        <td>Diverse</td>
    </tr>
    <tr topLine=""True"">
        <td align=""right"">Saldo</td>
        <td />
        <td />
        <td />
        <td>25.00</td>
        <td />
    </tr>
</data>";
            var expectedBuying = @"
<data>
    <tr topLine=""True"">
        <td>5/1/2019</td>
        <td>1</td>
        <td>Shoes</td>
        <td>50.00</td>
        <td/>
        <td>100</td>
    </tr>
    <tr topLine=""True"">
        <td align=""right"">Saldo</td>
        <td />
        <td />
        <td>50.00</td>
        <td />
        <td />
    </tr>
</data>";

            var rootElements = sut.Document.Element("xEport").Elements().Select(e => e.Name).Should().Equal(
                "pageTexts",
                "font", // header
                "text",
                "move",
                "font", // firm
                "text",
                "move",
                "text", // time range
                "move",
                "font", // default font
                "font", // account header
                "move",
                "table", // account data
                pageBreakBetweenAccounts ? "newPage" : "move",
                "font", // account header
                "move",
                "table", // account data
                pageBreakBetweenAccounts ? "newPage" : "move",
                "font", // account header
                "move",
                "table", // account data
                "move", // footer
                "text");

            var actual = sut.Document.XPathSelectElements("//table/data").ToArray();
            actual.Should().HaveCount(3);
            using (new AssertionScope())
            {
                actual[0].Should().BeEquivalentTo(
                    XDocument.Parse(expectedMoney).Root,
                    "money table should match");
                actual[1].Should().BeEquivalentTo(
                    XDocument.Parse(expectedSelling).Root,
                    "selling table should match");
                actual[2].Should().BeEquivalentTo(
                    XDocument.Parse(expectedBuying).Root,
                    "buying table should match");
            }
        }
    }
}
