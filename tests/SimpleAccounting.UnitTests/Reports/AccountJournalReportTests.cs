// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Reports
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using FluentAssertions;
    using FluentAssertions.Execution;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Reports;
    using lg2de.SimpleAccounting.UnitTests.Presentation;
    using Xunit;

    [SuppressMessage("ReSharper", "UseStringInterpolation")]
    public class AccountJournalReportTests
    {
        [CulturedTheory("en")]
        [InlineData(true)]
        [InlineData(false)]
        public void CreateReport_SampleData_Converted(bool pageBreakBetweenAccounts)
        {
            var projectData = Samples.SampleProjectData;
            projectData.CurrentYear.Booking.AddRange(Samples.SampleBookings);
            var sut = new AccountJournalReport(projectData) { PageBreakBetweenAccounts = pageBreakBetweenAccounts };

            sut.CreateReport("dummy");

            var year = DateTime.Now.Year;
            var expectedBankAccount = string.Format(
                @"
<data>
  <tr topLine=""True"">
    <td>1/1/{0}</td>
    <td>1</td>
    <td>Open 1</td>
    <td>1000.00</td>
    <td />
    <td>990</td>
  </tr>
  <tr topLine=""True"">
    <td>1/28/{0}</td>
    <td>3</td>
    <td>Salary</td>
    <td>200.00</td>
    <td />
    <td>Various</td>
  </tr>
  <tr topLine=""True"">
    <td>1/29/{0}</td>
    <td>4</td>
    <td>Credit rate</td>
    <td />
    <td>400.00</td>
    <td>5000</td>
  </tr>
  <tr topLine=""True"">
    <td>2/1/{0}</td>
    <td>5</td>
    <td>Shoes</td>
    <td />
    <td>50.00</td>
    <td>Various</td>
  </tr>
  <tr topLine=""True"">
    <td>2/5/{0}</td>
    <td>6</td>
    <td>Rent to friend</td>
    <td />
    <td>99.00</td>
    <td>6000</td>
  </tr>
  <tr topLine=""True"">
    <td align=""right"">Total</td>
    <td />
    <td />
    <td>1200.00</td>
    <td>549.00</td>
    <td />
  </tr>
  <tr topLine=""True"">
    <td align=""right"">Balance</td>
    <td />
    <td />
    <td>651.00</td>
    <td />
    <td />
  </tr>
</data>", year);
            var expectedSalary = string.Format(
                @"
<data>
  <tr topLine=""True"">
    <td>1/28/{0}</td>
    <td>3</td>
    <td>Salary1</td>
    <td />
    <td>120.00</td>
    <td>100</td>
  </tr>
  <tr topLine=""True"">
    <td>1/28/{0}</td>
    <td>3</td>
    <td>Salary2</td>
    <td />
    <td>80.00</td>
    <td>100</td>
  </tr>
  <tr topLine=""True"">
    <td align=""right"">Balance</td>
    <td />
    <td />
    <td />
    <td>200.00</td>
    <td />
  </tr>
</data>", year);
            var expectedShoes = string.Format(
                @"
<data>
  <tr topLine=""True"">
    <td>2/1/{0}</td>
    <td>5</td>
    <td>Shoes1</td>
    <td>20.00</td>
    <td />
    <td>100</td>
  </tr>
  <tr topLine=""True"">
    <td>2/1/{0}</td>
    <td>5</td>
    <td>Shoes2</td>
    <td>30.00</td>
    <td />
    <td>100</td>
  </tr>
  <tr topLine=""True"">
    <td align=""right"">Balance</td>
    <td />
    <td />
    <td>50.00</td>
    <td />
    <td />
  </tr>
</data>", year);
            var expectedCarryforward = string.Format(
                @"
<data>
  <tr topLine=""True"">
    <td>1/1/{0}</td>
    <td>1</td>
    <td>Open 1</td>
    <td />
    <td>1000.00</td>
    <td>100</td>
  </tr>
  <tr topLine=""True"">
    <td>1/1/{0}</td>
    <td>2</td>
    <td>Open 2</td>
    <td>3000.00</td>
    <td />
    <td>5000</td>
  </tr>
  <tr topLine=""True"">
    <td align=""right"">Total</td>
    <td />
    <td />
    <td>3000.00</td>
    <td>1000.00</td>
    <td />
  </tr>
  <tr topLine=""True"">
    <td align=""right"">Balance</td>
    <td />
    <td />
    <td>2000.00</td>
    <td />
    <td />
  </tr>
</data>", year);
            var expectedBankCredit = string.Format(
                @"
<data>
  <tr topLine=""True"">
    <td>1/1/{0}</td>
    <td>2</td>
    <td>Open 2</td>
    <td />
    <td>3000.00</td>
    <td>990</td>
  </tr>
  <tr topLine=""True"">
    <td>1/29/{0}</td>
    <td>4</td>
    <td>Credit rate</td>
    <td>400.00</td>
    <td />
    <td>100</td>
  </tr>
  <tr topLine=""True"">
    <td align=""right"">Total</td>
    <td />
    <td />
    <td>400.00</td>
    <td>3000.00</td>
    <td />
  </tr>
  <tr topLine=""True"">
    <td align=""right"">Balance</td>
    <td />
    <td />
    <td />
    <td>2600.00</td>
    <td />
  </tr>
</data>", year);
            var expectedFriendsDebit = string.Format(
                @"
<data>
  <tr topLine=""True"">
    <td>2/5/{0}</td>
    <td>6</td>
    <td>Rent to friend</td>
    <td>99.00</td>
    <td />
    <td>100</td>
  </tr>
  <tr topLine=""True"">
    <td align=""right"">Balance</td>
    <td />
    <td />
    <td>99.00</td>
    <td />
    <td />
  </tr>
</data>", year);

            sut.DocumentForTests!.Root!.Elements().Select(e => e.Name).Should().Equal(
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
                pageBreakBetweenAccounts ? "newPage" : "move",
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

            var actual = sut.DocumentForTests.XPathSelectElements("//table/data").ToArray();
            actual.Should().HaveCount(6);
            using (new AssertionScope())
            {
                actual[0].Should().BeEquivalentTo(
                    XDocument.Parse(expectedBankAccount).Root,
                    "bank account table should match");
                actual[1].Should().BeEquivalentTo(
                    XDocument.Parse(expectedSalary).Root,
                    "salary table should match");
                actual[2].Should().BeEquivalentTo(
                    XDocument.Parse(expectedShoes).Root,
                    "shoes table should match");
                actual[3].Should().BeEquivalentTo(
                    XDocument.Parse(expectedCarryforward).Root,
                    "carryforward table should match");
                actual[4].Should().BeEquivalentTo(
                    XDocument.Parse(expectedBankCredit).Root,
                    "bank credit table should match");
                actual[5].Should().BeEquivalentTo(
                    XDocument.Parse(expectedFriendsDebit).Root,
                    "friends debit table should match");
            }
        }
    }
}
