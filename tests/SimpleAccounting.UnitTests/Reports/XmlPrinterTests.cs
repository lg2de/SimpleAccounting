// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace SimpleAccounting.UnitTests.Reports
{
    using System;
    using System.Xml.Linq;
    using FluentAssertions;
    using lg2de.SimpleAccounting.Reports;
    using Xunit;

    public class XmlPrinterTests
    {
        [Fact]
        public void LoadDocument_UnknownReport_Throws()
        {
            var sut = new XmlPrinter();

            sut.Invoking(x => x.LoadDocument("XXX")).Should().Throw<ArgumentException>()
                .WithMessage("*XXX*");
        }

        [Fact]
        public void LoadDocument_AccountJournalReport_Loaded()
        {
            var sut = new XmlPrinter();

            sut.Invoking(x => x.LoadDocument(AccountJournalReport.ResourceName)).Should().NotThrow();
        }

        [Fact]
        public void LoadDocument_AnnualBalanceReport_Loaded()
        {
            var sut = new XmlPrinter();

            sut.Invoking(x => x.LoadDocument(AnnualBalanceReport.ResourceName)).Should().NotThrow();
        }

        [Fact]
        public void LoadDocument_TotalJournalReport_Loaded()
        {
            var sut = new XmlPrinter();

            sut.Invoking(x => x.LoadDocument(TotalJournalReport.ResourceName)).Should().NotThrow();
        }

        [Fact]
        public void LoadDocument_TotalsAndBalancesReport_Loaded()
        {
            var sut = new XmlPrinter();

            sut.Invoking(x => x.LoadDocument(TotalsAndBalancesReport.ResourceName)).Should().NotThrow();
        }

        [Fact]
        public void TransformDocument_Rectangle_ConvertedToLines()
        {
            var sut = new XmlPrinter();
            sut.LoadXml("<root><rectangle relFromX=\"10\" relFromY=\"20\" relToX=\"30\" relToY=\"40\" /></root>");

            sut.TransformDocument();

            XDocument.Parse(sut.Document.OuterXml).Should().BeEquivalentTo(
                XDocument.Parse(
                "<root>"
                + "<line relFromX=\"10\" relFromY=\"20\" relToX=\"30\" relToY=\"20\" />"
                + "<line relFromX=\"30\" relFromY=\"20\" relToX=\"30\" relToY=\"40\" />"
                + "<line relFromX=\"30\" relFromY=\"40\" relToX=\"10\" relToY=\"40\" />"
                + "<line relFromX=\"10\" relFromY=\"40\" relToX=\"10\" relToY=\"20\" />"
                + "</root>"));
        }

        [Fact]
        public void TransformDocument_Table_ConvertedToTexts()
        {
            var sut = new XmlPrinter { DocumentHeight = 100 };
            sut.LoadXml(
                "<root>"
                + "<table><columns>"
                + "<column width=\"10\">C1</column>"
                + "<column width=\"20\">C2</column>"
                + "</columns><data>"
                + "<tr><td>1</td><td>2</td></tr>"
                + "</data></table>"
                + "</root>");

            sut.TransformDocument();

            XDocument.Parse(sut.Document.OuterXml).Should().BeEquivalentTo(
                XDocument.Parse(
                "<root>"
                + "<text relX=\"0\">C1</text>"
                + "<text relX=\"10\">C2</text>"
                + "<move relY=\"4\" />" // DefaultLineHeight
                + "<text relX=\"0\">1</text>"
                + "<text relX=\"10\">2</text>"
                + "<move relY=\"4\" />"
                + "</root>"));
        }

        [Fact]
        public void TransformDocument_LongTable_NewPageWithHeader()
        {
            var sut = new XmlPrinter { DocumentHeight = 10 };
            sut.LoadXml(
                "<root>"
                + "<table><columns>"
                + "<column width=\"10\">C1</column>"
                + "</columns><data>"
                + "<tr><td>1</td></tr>"
                + "<tr><td>2</td></tr>"
                + "</data></table>"
                + "</root>");

            sut.TransformDocument();

            XDocument.Parse(sut.Document.OuterXml).Should().BeEquivalentTo(
                XDocument.Parse(
                "<root>"
                + "<text relX=\"0\">C1</text>"
                + "<move relY=\"4\" />"
                + "<text relX=\"0\">1</text>"
                + "<newpage />"
                + "<text relX=\"0\">C1</text>"
                + "<move relY=\"4\" />"
                + "<text relX=\"0\">2</text>"
                + "<move relY=\"4\" />"
                + "</root>"));
        }

        [Fact]
        public void TransformDocument_PageTexts_ConvertedToLines()
        {
            var sut = new XmlPrinter { DocumentHeight = 10 };
            sut.LoadXml(
                "<root>"
                + "<pageTexts><font><text>page {page}</text></font></pageTexts>"
                + "<table><columns>"
                + "<column width=\"10\">C1</column>"
                + "</columns><data>"
                + "<tr><td>1</td></tr>"
                + "<tr><td>2</td></tr>"
                + "</data></table>"
                + "</root>");

            sut.TransformDocument();

            XDocument.Parse(sut.Document.OuterXml).Should().BeEquivalentTo(
                XDocument.Parse(
                "<root>"
                + "<font><text>page 1</text></font>"
                + "<text relX=\"0\">C1</text>"
                + "<move relY=\"4\" />"
                + "<text relX=\"0\">1</text>"
                + "<newpage />"
                + "<font><text>page 2</text></font>"
                + "<text relX=\"0\">C1</text>"
                + "<move relY=\"4\" />"
                + "<text relX=\"0\">2</text>"
                + "<move relY=\"4\" />"
                + "</root>"));
        }
    }
}
