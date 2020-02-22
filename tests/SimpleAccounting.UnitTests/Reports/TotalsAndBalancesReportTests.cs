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
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Reports;
    using lg2de.SimpleAccounting.UnitTests.Presentation;
    using Xunit;

    public class TotalsAndBalancesReportTests
    {
        [Fact]
        public void CreateReport_SampleData_Converted()
        {
            AccountingData project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            var setup = new AccountingDataSetup();
            AccountingDataJournal journal = project.Journal.Last();
            var sut = new TotalsAndBalancesReport(journal, project.Accounts, setup, new CultureInfo("en-us"));

            sut.CreateReport(journal.DateStart.ToDateTime(), journal.DateEnd.ToDateTime());

            var expected = @"
 <data>
  <tr topLine=""True"">
    <td>100</td>
    <td>Bank account</td>
    <td>2/1/2020</td>
    <td>1000.00</td>
    <td></td>
    <td>200.00</td>
    <td>450.00</td>
    <td>750.00</td>
    <td></td>
  </tr>
  <tr topLine=""True"">
    <td>400</td>
    <td>Salary</td>
    <td>1/28/2020</td>
    <td></td>
    <td></td>
    <td></td>
    <td>200.00</td>
    <td></td>
    <td>200.00</td>
  </tr>
  <tr topLine=""True"">
    <td>600</td>
    <td>Shoes</td>
    <td>2/1/2020</td>
    <td></td>
    <td></td>
    <td>50.00</td>
    <td></td>
    <td>50.00</td>
    <td></td>
  </tr>
  <tr topLine=""True"">
    <td>990</td>
    <td>Carryforward</td>
    <td>1/1/2020</td>
    <td>2000.00</td>
    <td></td>
    <td></td>
    <td></td>
    <td>2000.00</td>
    <td></td>
  </tr>
  <tr topLine=""True"">
    <td>5000</td>
    <td>Bank credit</td>
    <td>1/29/2020</td>
    <td></td>
    <td>3000.00</td>
    <td>400.00</td>
    <td></td>
    <td></td>
    <td>2600.00</td>
  </tr>
  <tr topLine=""True"">
    <td></td>
    <td align=""right"">Total</td>
    <td></td>
    <td>3000.00</td>
    <td>3000.00</td>
    <td>650.00</td>
    <td>650.00</td>
    <td>2800.00</td>
    <td>2800.00</td>
  </tr>
</data>";
            sut.Document.XPathSelectElement("//table/data")
                .Should().BeEquivalentTo(XDocument.Parse(expected).Root);
        }
    }
}
