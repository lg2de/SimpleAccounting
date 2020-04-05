// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Reports
{
    using System.Globalization;
    using System.Linq;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using FluentAssertions;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Reports;
    using lg2de.SimpleAccounting.UnitTests.Presentation;
    using Xunit;

    public class TotalJournalReportTests
    {
        [Fact]
        public void CreateReport_SampleData_Converted()
        {
            AccountingData project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            var setup = new AccountingDataSetup();
            AccountingDataJournal journal = project.Journal.Last();
            var sut = new TotalJournalReport(journal, setup, new CultureInfo("en-us"));

            sut.CreateReport("dummy");

            var expected = @"
<data>
  <tr topLine=""True"">
    <td>1/1/2020</td>
    <td>1</td>
    <td>Open 1</td>
    <td>100</td>
    <td>1000.00</td>
    <td>990</td>
    <td>1000.00</td>
  </tr>
  <tr topLine=""True"">
    <td>1/1/2020</td>
    <td>2</td>
    <td>Open 2</td>
    <td>990</td>
    <td>3000.00</td>
    <td>5000</td>
    <td>3000.00</td>
  </tr>
  <tr topLine=""True"">
    <td>1/28/2020</td>
    <td>3</td>
    <td>Salary</td>
    <td>100</td>
    <td>200.00</td>
    <td />
    <td />
  </tr>
  <tr>
    <td />
    <td />
    <td>Salary1</td>
    <td />
    <td />
    <td>400</td>
    <td>100.00</td>
  </tr>
  <tr>
    <td />
    <td />
    <td>Salary2</td>
    <td />
    <td />
    <td>400</td>
    <td>100.00</td>
  </tr>
  <tr topLine=""True"">
    <td>1/29/2020</td>
    <td>4</td>
    <td>Credit rate</td>
    <td>5000</td>
    <td>400.00</td>
    <td>100</td>
    <td>400.00</td>
  </tr>
  <tr topLine=""True"">
    <td>2/1/2020</td>
    <td>5</td>
    <td>Shoes1</td>
    <td>600</td>
    <td>20.00</td>
    <td />
    <td />
  </tr>
  <tr>
    <td />
    <td />
    <td>Shoes2</td>
    <td>600</td>
    <td>30.00</td>
    <td />
    <td />
  </tr>
  <tr>
    <td />
    <td />
    <td>Shoes</td>
    <td />
    <td />
    <td>100</td>
    <td>50.00</td>
  </tr>
  <tr topLine=""True"">
    <td>2/5/2020</td>
    <td>6</td>
    <td>Rent to friend</td>
    <td>6000</td>
    <td>99.00</td>
    <td>100</td>
    <td>99.00</td>
  </tr>
</data>";
            sut.DocumentForTests.XPathSelectElement("//table/data")
                .Should().BeEquivalentTo(XDocument.Parse(expected).Root);
        }
    }
}
