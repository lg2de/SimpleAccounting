// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Reports;

using System;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using FluentAssertions;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Reports;
using lg2de.SimpleAccounting.UnitTests.Presentation;
using NSubstitute;
using Xunit;

public class TotalsAndBalancesReportTests
{
    [CulturedFact("en")]
    public void CreateReport_SampleData_Converted()
    {
        var projectData = Samples.SampleProjectData;
        projectData.CurrentYear.Booking.AddRange(Samples.SampleBookings);
        var clock = Substitute.For<IClock>();
        var sut = new TotalsAndBalancesReport(
            new XmlPrinter(), projectData, clock, projectData.Storage.Accounts);

        sut.CreateReport("-TotalsAndBalances-");

        var year = Samples.SampleProject.Journal[^1].Year;
        var expected = $@"
<data>
  <tr topLine=""True"">
    <td>100</td>
    <td>Bank account</td>
    <td>2/5/{
        year
    }</td>
    <td>1000.00</td>
    <td></td>
    <td>200.00</td>
    <td>549.00</td>
    <td>651.00</td>
    <td></td>
  </tr>
  <tr topLine=""True"">
    <td>400</td>
    <td>Salary</td>
    <td>1/28/{
        year
    }</td>
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
    <td>2/1/{
        year
    }</td>
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
    <td>1/1/{
        year
    }</td>
    <td>2000.00</td>
    <td></td>
    <td></td>
    <td></td>
    <td>2000.00</td>
    <td></td>
  </tr>
  <tr topLine=""True"" lineHeight=""6"">
    <td></td>
    <td align=""right"">Default</td>
    <td></td>
    <td>3000.00</td>
    <td></td>
    <td>250.00</td>
    <td>749.00</td>
    <td>2701.00</td>
    <td>200.00</td>
  </tr>
  <tr topLine=""True"">
    <td>5000</td>
    <td>Bank credit</td>
    <td>1/29/{
        year
    }</td>
    <td></td>
    <td>3000.00</td>
    <td>400.00</td>
    <td></td>
    <td></td>
    <td>2600.00</td>
  </tr>
  <tr topLine=""True"">
    <td>6000</td>
    <td>Friends debit</td>
    <td>2/5/{
        year
    }</td>
    <td></td>
    <td></td>
    <td>99.00</td>
    <td></td>
    <td>99.00</td>
    <td></td>
  </tr>
  <tr topLine=""True"" lineHeight=""6"">
    <td></td>
    <td align=""right"">Second</td>
    <td></td>
    <td></td>
    <td>3000.00</td>
    <td>499.00</td>
    <td></td>
    <td>99.00</td>
    <td>2600.00</td>
  </tr>
  <tr topLine=""True"">
    <td></td>
    <td align=""right"">Total</td>
    <td></td>
    <td>3000.00</td>
    <td>3000.00</td>
    <td>749.00</td>
    <td>749.00</td>
    <td>2800.00</td>
    <td>2800.00</td>
  </tr>
</data>";
        sut.DocumentForTests.XPathSelectElement("//table/data")
            .Should().BeEquivalentTo(XDocument.Parse(expected).Root);
    }

    [Fact]
    public void CreateReport_SampleWithSignature_SignatureLinesCreated()
    {
        var projectData = Samples.SampleProjectData;
        var clock = Substitute.For<IClock>();
        var sut = new TotalsAndBalancesReport(
            new XmlPrinter(), projectData, clock, projectData.Storage.Accounts);
        sut.Signatures.Add("The Name");

        sut.CreateReport("-TotalsAndBalances-");

        sut.DocumentForTests.XPathSelectElements("//text[@tag='signature']")
            .Select(x => x.Value).Should().Equal("The Name");
    }

    [Theory]
    [InlineData("Report 1")]
    [InlineData("Report 2")]
    public void CreateReport_ReportNames_NameAppliedToHeaderAndTitle(string reportName)
    {
        var projectData = Samples.SampleProjectData;
        var clock = Substitute.For<IClock>();
        var sut = new TotalsAndBalancesReport(
            new XmlPrinter(), projectData, clock, projectData.Storage.Accounts);

        sut.CreateReport(reportName);

        var expectedText = $"- {reportName} {DateTime.Today.Year} -";
        sut.DocumentForTests.XPathSelectElement("//text[@ID='page-header']")?.Value.Should().Be(expectedText);
        expectedText = $"{reportName} {DateTime.Today.Year}";
        sut.DocumentForTests.XPathSelectElement("//text[@ID='report-header']")?.Value.Should().Be(expectedText);
    }
}
