// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Reports;

using System;
using System.Linq;
using System.Xml;
using FluentAssertions;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Reports;
using lg2de.SimpleAccounting.UnitTests.Presentation;
using NSubstitute;
using Xunit;

public class ReportBaseTests
{
    private class TestReport : ReportBase
    {
        public TestReport(IXmlPrinter printer, ProjectData projectData)
            : base(printer, string.Empty, projectData)
        {
        }

        public new XmlDocument PrintDocument => base.PrintDocument;

        public void SetPrintingDate(DateTime dateTime)
        {
            this.PrintingDate = dateTime;
        }

        public new void PreparePrintDocument(string title, DateTime printDate)
        {
            base.PreparePrintDocument(title, printDate);
        }
    }

    [Fact]
    public void PreparePrintDocument_EmptyXml_Completed()
    {
        var printer = Substitute.For<IXmlPrinter>();
        printer.Document.Returns(new XmlDocument());
        var sut = new TestReport(printer, Samples.SampleProjectData);

        sut.Invoking(x => x.PreparePrintDocument("TheTitle", DateTime.Now)).Should().NotThrow();
    }

    [CulturedFact("en")]
    public void PreparePrintDocument_SampleXml_PlaceholdersUpdates()
    {
        var printer = Substitute.For<IXmlPrinter>();
        var xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(
            @"
<root>
  <text ID=""title"">XXX</text>
  <text ID=""pageTitle"">XXX</text>
  <text ID=""firm"">XXX</text>
  <text>- {yearName} -</text>
  <text ID=""range"">XXX</text>
  <text ID=""date"">XXX</text>
</root>");
        printer.Document.Returns(xmlDocument);
        var projectData = Samples.SampleProjectData;
        projectData.Storage.Setup.Name = "TheName";
        projectData.Storage.Setup.Location = "TheLocation";
        projectData.SelectYear(projectData.Storage.Journal.First().Year);
        var sut = new TestReport(printer, projectData);

        sut.PreparePrintDocument("TheTitle", new DateTime(2021, 5, 5));

        sut.PrintDocument.OuterXml.Should().Be(
            "<root><text ID=\"title\">TheTitle</text>"
            + "<text ID=\"pageTitle\">- TheTitle -</text>"
            + "<text ID=\"firm\">TheName</text>"
            + "<text>- 2000 -</text>"
            + "<text ID=\"range\">1/1/2000 - 12/31/2000</text>"
            + "<text ID=\"date\">TheLocation, Wednesday, May 5, 2021</text></root>");
    }

    [Fact]
    public void ShowPreview_DocumentName_PrintingNameCorrect()
    {
        var printer = Substitute.For<IXmlPrinter>();
        var sut = new TestReport(printer, Samples.SampleProjectData);
        sut.SetPrintingDate(new DateTime(2020, 2, 29));

        sut.ShowPreview("DocumentName");

        var year = Samples.SampleProject.Journal.Last().Year;
        printer.Received(1).PrintDocument($"2020-02-29 DocumentName {year}");
    }
}
