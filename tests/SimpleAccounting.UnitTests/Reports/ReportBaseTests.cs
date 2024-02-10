// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Reports;

using System;
using System.Xml;
using System.Xml.Linq;
using FluentAssertions;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Reports;
using lg2de.SimpleAccounting.UnitTests.Presentation;
using NSubstitute;
using Xunit;

public class ReportBaseTests
{
    private class TestReport : ReportBase
    {
        public TestReport(IXmlPrinter printer, IProjectData projectData, IClock clock)
            : base(string.Empty, printer, projectData, clock)
        {
        }

        public new XmlDocument PrintDocument => base.PrintDocument;

        public new void PreparePrintDocument()
        {
            base.PreparePrintDocument();
        }
    }

    [CulturedFact("en")]
    public void PreparePrintDocument_SampleXml_PlaceholdersUpdates()
    {
        var printer = Substitute.For<IXmlPrinter>();
        var clock = Substitute.For<IClock>();
        clock.Now().Returns(new DateTime(2021, 5, 5, 0, 0, 0, DateTimeKind.Local));
        var xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(
            """
            <root>
              <text>#Organization#</text>
              <text>- #YearName# -</text>
              <text>#TimeRange#</text>
              <text>#CurrentDate#</text>
            </root>
            """);
        printer.Document.Returns(xmlDocument);
        var projectData = Samples.SampleProjectData;
        projectData.Storage.Setup.Name = "TheName";
        projectData.Storage.Setup.Location = "TheLocation";
        projectData.SelectYear(projectData.Storage.Journal[0].Year);
        var sut = new TestReport(printer, projectData, clock);

        sut.PreparePrintDocument();

        XDocument.Parse(sut.PrintDocument.OuterXml).Should().BeEquivalentTo(
            XDocument.Parse(
                """
                <root>
                  <text>TheName</text>
                  <text>- 2000 -</text>
                  <text>1/1/2000 - 12/31/2000</text>
                  <text>TheLocation, Wednesday, May 5, 2021</text>
                </root>
                """));
    }

    [Fact]
    public void PreparePrintDocument_EmptyXml_Completed()
    {
        var printer = Substitute.For<IXmlPrinter>();
        var clock = Substitute.For<IClock>();
        printer.Document.Returns(new XmlDocument());
        var sut = new TestReport(printer, Samples.SampleProjectData, clock);

        sut.Invoking(x => x.PreparePrintDocument()).Should().NotThrow();
    }

    [Fact]
    public void ShowPreview_DocumentName_PrintingNameCorrect()
    {
        var printer = Substitute.For<IXmlPrinter>();
        var clock = Substitute.For<IClock>();
        clock.Now().Returns(new DateTime(2020, 2, 29, 0, 0, 0, DateTimeKind.Local));
        var sut = new TestReport(printer, Samples.SampleProjectData, clock);

        sut.ShowPreview("DocumentName");

        var year = Samples.SampleProject.Journal[^1].Year;
        printer.Received(1).PrintDocument($"2020-02-29 DocumentName {year}");
    }
}
