// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Reports;

using System.Xml.Linq;
using System.Xml.XPath;
using FluentAssertions;
using FluentAssertions.Execution;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Reports;
using lg2de.SimpleAccounting.UnitTests.Presentation;
using NSubstitute;
using Xunit;

public class AnnualBalanceReportTests
{
    [CulturedFact("en")]
    public void CreateReport_SampleData_Converted()
    {
        var projectData = Samples.SampleProjectData;
        projectData.CurrentYear.Booking.AddRange(Samples.SampleBookings);
        var clock = Substitute.For<IClock>();
        var sut = new AnnualBalanceReport(new XmlPrinter(), projectData, clock);

        sut.CreateReport();

        using var _ = new AssertionScope();

        sut.DocumentForTests.XPathSelectElement("//text[@ID='saldo']")?.Value.Should().Be("150.00");

        var expected = @"
<data target=""income"">
  <tr>
    <td />
    <td>00400 Salary</td>
    <td>200.00</td>
  </tr>
</data>";
        sut.DocumentForTests.XPathSelectElement("//table/data[@target='income']")
            .Should().BeEquivalentTo(XDocument.Parse(expected).Root);

        expected = @"
<data target=""expense"">
  <tr>
    <td />
    <td>00600 Shoes</td>
    <td>-50.00</td>
  </tr>
</data>";
        sut.DocumentForTests.XPathSelectElement("//table/data[@target='expense']")
            .Should().BeEquivalentTo(XDocument.Parse(expected).Root);

        expected = @"
<data target=""receivable"">
  <tr>
    <td />
    <td>06000 Friends debit</td>
    <td>99.00</td>
  </tr>
</data>";
        sut.DocumentForTests.XPathSelectElement("//table/data[@target='receivable']")
            .Should().BeEquivalentTo(XDocument.Parse(expected).Root);

        expected = @"
<data target=""liability"">
  <tr>
    <td />
    <td>05000 Bank credit</td>
    <td>-2600.00</td>
  </tr>
</data>";
        sut.DocumentForTests.XPathSelectElement("//table/data[@target='liability']")
            .Should().BeEquivalentTo(XDocument.Parse(expected).Root);

        expected = @"
 <data target=""asset"">
  <tr>
    <td />
    <td>00100 Bank account</td>
    <td>651.00</td>
  </tr>
  <tr>
    <td />
    <td>Receivables</td>
    <td>99.00</td>
  </tr>
  <tr>
    <td />
    <td>Liabilities</td>
    <td>-2600.00</td>
  </tr>
</data>";
        sut.DocumentForTests.XPathSelectElement("//table/data[@target='asset']")
            .Should().BeEquivalentTo(XDocument.Parse(expected).Root);
    }
}
