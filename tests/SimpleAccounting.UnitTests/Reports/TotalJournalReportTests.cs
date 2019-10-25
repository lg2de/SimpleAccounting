// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace SimpleAccounting.UnitTests.Reports
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using FluentAssertions;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Reports;
    using Xunit;

    public class TotalJournalReportTests
    {
        [Fact]
        public void CreateReport_SampleData_Converted()
        {
            var journal = new AccountingDataJournal
            {
                Year = 2019,
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
            var sut = new TotalJournalReport(journal, setup, new CultureInfo("en-us"));

            sut.CreateReport(new DateTime(2019, 1, 1), new DateTime(2019, 12, 31));

            var expected = @"
<data>
    <tr topLine=""True"">
        <td>4/1/2019</td>
        <td>1</td>
        <td>EB</td>
        <td>100</td>
        <td>500.00</td>
        <td>900</td>
        <td>500.00</td>
    </tr>
    <tr topLine=""True"">
        <td>5/1/2019</td>
        <td>1</td>
        <td>Shoes</td>
        <td>601</td>
        <td>50.00</td>
        <td/>
        <td/>
    </tr>
    <tr>
        <td/>
        <td/>
        <td>Socks</td>
        <td>602</td>
        <td>50.00</td>
        <td/>
        <td/>
    </tr>
    <tr>
        <td/>
        <td/>
        <td>Buy</td>
        <td/>
        <td/>
        <td>100</td>
        <td>100.00</td>
    </tr>
    <tr topLine=""True"">
        <td>6/1/2019</td>
        <td>1</td>
        <td>Sell</td>
        <td>100</td>
        <td>50.00</td>
        <td/>
        <td/>
    </tr>
    <tr>
        <td/>
        <td/>
        <td>Software</td>
        <td/>
        <td/>
        <td>401</td>
        <td>25.00</td>
    </tr>
    <tr>
        <td/>
        <td/>
        <td>Hardware</td>
        <td/>
        <td/>
        <td>402</td>
        <td>25.00</td>
    </tr>
</data>";
            sut.Document.XPathSelectElement("//table/data")
                .Should().BeEquivalentTo(XDocument.Parse(expected).Root);
        }
    }
}
