// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Reports;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Xml.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Reports;
using NSubstitute;
using Xunit;

public class XmlPrinterTests
{
    private static readonly List<PaperSize> PaperSizes =
        [new PaperSize("A4", (int)(210 / 0.254), (int)(297 / 0.254))];

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
    public void LoadDocument_UnknownReport_Throws()
    {
        var sut = new XmlPrinter();

        sut.Invoking(x => x.LoadDocument("XXX")).Should().Throw<ArgumentException>()
            .WithMessage("*XXX*");
    }

    [Fact]
    public void PrintNodes_LineAbsolute_LinePrinted()
    {
        var sut = new XmlPrinter();
        sut.LoadXml("<root><line absFromX=\"10\" absFromY=\"20\" absToX=\"30\" absToY=\"40\" /></root>");
        // TODO SetupDocument is required for print factor only
        var document = new PrintDocument();
        sut.SetupDocument(document, PaperSizes);
        sut.SetupGraphics();
        sut.CursorX = 12;
        sut.CursorY = 14;

        var graphics = Substitute.For<IGraphics>();
        sut.PrintNodes(graphics);

        using (new AssertionScope())
        {
            graphics.Received(1).DrawLine(
                Arg.Any<Pen>(),
                39, // 10*(100/25.4)
                79, // 20*(100/25.4)
                118, // 30*(100/25.4)
                157); // 40*(100/25.4)
        }

        sut.CleanupGraphics();
    }

    [Fact]
    public void PrintNodes_LineRelative_LinePrinted()
    {
        var sut = new XmlPrinter();
        sut.LoadXml("<root><line relFromX=\"10\" relFromY=\"20\" relToX=\"30\" relToY=\"40\" /></root>");
        // TODO SetupDocument is required for print factor only
        var document = new PrintDocument();
        sut.SetupDocument(document, PaperSizes);
        sut.SetupGraphics();
        sut.CursorX = 12;
        sut.CursorY = 14;

        var graphics = Substitute.For<IGraphics>();
        sut.PrintNodes(graphics);

        using (new AssertionScope())
        {
            graphics.Received(1).DrawLine(
                Arg.Any<Pen>(),
                87, // 22*(100/25.4)
                134, // 34*(100/25.4)
                165, // 52*(100/25.4)
                213); // 74*(100/25.4)
        }

        sut.CleanupGraphics();
    }

    [Fact]
    public void PrintNodes_Move_CursorUpdated()
    {
        var sut = new XmlPrinter();
        sut.LoadXml("<root><move absX=\"10\" absY=\"20\" /></root>");
        sut.SetupGraphics();
        sut.CursorX = 5;
        sut.CursorY = 8;

        var graphics = Substitute.For<IGraphics>();
        sut.PrintNodes(graphics);

        using (new AssertionScope())
        {
            graphics.HasMorePages.Should().BeFalse();
            sut.CursorX.Should().Be(10);
            sut.CursorY.Should().Be(20);
        }

        sut.CleanupGraphics();
    }

    [Fact]
    public void PrintNodes_NewPage_MorePagesRequested()
    {
        var sut = new XmlPrinter();
        sut.LoadXml("<root><move absX=\"10\" absY=\"20\" /><newPage /></root>");
        sut.SetupGraphics();
        sut.CursorX = 5;
        sut.CursorY = 8;

        var graphics = Substitute.For<IGraphics>();
        sut.PrintNodes(graphics);

        graphics.HasMorePages.Should().BeTrue();

        sut.CleanupGraphics();
    }

    [Fact]
    public void PrintNodes_TextAbsolute_TextPrintedCursorUnchanged()
    {
        var sut = new XmlPrinter();
        sut.LoadXml("<root><text absX=\"10\" absY=\"20\">The text.</text></root>");
        // TODO SetupDocument is required for print factor only
        var document = new PrintDocument();
        sut.SetupDocument(document, PaperSizes);
        sut.SetupGraphics();
        sut.CursorX = 5;
        sut.CursorY = 8;

        var graphics = Substitute.For<IGraphics>();
        sut.PrintNodes(graphics);

        using (new AssertionScope())
        {
            graphics.Received(1).DrawString(
                "The text.",
                Arg.Any<Font>(),
                Arg.Any<Brush>(),
                39, // 10*(100/25.4)
                79, // 20*(100/25.4)
                StringAlignment.Near);
            sut.CursorX.Should().Be(5);
            sut.CursorY.Should().Be(8);
        }

        sut.CleanupGraphics();
    }

    [Fact]
    public void PrintNodes_TextRelative_TextPrinted()
    {
        var sut = new XmlPrinter();
        sut.LoadXml("<root><text relX=\"10\" relY=\"20\" align=\"center\">The text.</text></root>");
        // TODO SetupDocument is required for print factor only
        var document = new PrintDocument();
        sut.SetupDocument(document, PaperSizes);
        sut.SetupGraphics();
        sut.CursorX = 5;
        sut.CursorY = 8;

        var graphics = Substitute.For<IGraphics>();
        sut.PrintNodes(graphics);

        graphics.Received(1).DrawString(
            "The text.",
            Arg.Any<Font>(),
            Arg.Any<Brush>(),
            59, // 15*(100/25.4)
            110, // 28*(100/25.4)
            StringAlignment.Center);

        sut.CleanupGraphics();
    }

    [Fact]
    public void PrintNodes_TextRight_TextPrintedAtCursor()
    {
        var sut = new XmlPrinter();
        sut.LoadXml("<root><text align=\"right\">The text.</text></root>");
        // TODO SetupDocument is required for print factor only
        var document = new PrintDocument();
        sut.SetupDocument(document, PaperSizes);
        sut.SetupGraphics();
        sut.CursorX = 5;
        sut.CursorY = 8;

        var graphics = Substitute.For<IGraphics>();
        sut.PrintNodes(graphics);

        graphics.Received(1).DrawString(
            "The text.",
            Arg.Any<Font>(),
            Arg.Any<Brush>(),
            20, // 5*(100/25.4)
            31, // 8*(100/25.4)
            StringAlignment.Far);

        sut.CleanupGraphics();
    }

    [Fact]
    public void PrintNodes_TextWithFontChange_TextPrintedWithFont()
    {
        var sut = new XmlPrinter();
        sut.LoadXml(
            "<root>" +
            "<text>text1</text>" +
            "<font size=\"20\" />" +
            "<text>text2</text>" +
            "</root>");
        // TODO SetupDocument is required for print factor only
        var document = new PrintDocument();
        sut.SetupDocument(document, PaperSizes);
        sut.SetupGraphics();

        var graphics = Substitute.For<IGraphics>();
        sut.PrintNodes(graphics);

        graphics.Received(1).DrawString(
            "text1",
            Arg.Is<Font>(x => Math.Abs(x.SizeInPoints - 10) < 0.1),
            Arg.Any<Brush>(),
            Arg.Any<float>(),
            Arg.Any<float>(),
            Arg.Any<StringAlignment>());
        graphics.Received(1).DrawString(
            "text2",
            Arg.Is<Font>(x => Math.Abs(x.SizeInPoints - 20) < 0.1),
            Arg.Any<Brush>(),
            Arg.Any<float>(),
            Arg.Any<float>(),
            Arg.Any<StringAlignment>());

        sut.CleanupGraphics();
    }

    [Fact]
    public void PrintNodes_TextWithFontStack_TextPrintedWithFont()
    {
        var sut = new XmlPrinter();
        sut.LoadXml(
            "<root>" +
            "<text>text1</text>" +
            "<font bold=\"1\"><text>text2</text></font>" +
            "</root>");
        // TODO SetupDocument is required for print factor only
        var document = new PrintDocument();
        sut.SetupDocument(document, PaperSizes);
        sut.SetupGraphics();

        var graphics = Substitute.For<IGraphics>();
        sut.PrintNodes(graphics);

        graphics.Received(1).DrawString(
            "text1",
            Arg.Is<Font>(x => x.Style == FontStyle.Regular),
            Arg.Any<Brush>(),
            Arg.Any<float>(),
            Arg.Any<float>(),
            Arg.Any<StringAlignment>());
        graphics.Received(1).DrawString(
            "text2",
            Arg.Is<Font>(x => x.Style == FontStyle.Bold),
            Arg.Any<Brush>(),
            Arg.Any<float>(),
            Arg.Any<float>(),
            Arg.Any<StringAlignment>());

        sut.CleanupGraphics();
    }

    [Fact]
    public void SetupDocument_A4_DocumentInitialized()
    {
        var sut = new XmlPrinter();
        var document = new PrintDocument();
        sut.LoadXml("<root paperSize=\"A4\" />");

        sut.SetupDocument(document, PaperSizes);

        using (new AssertionScope())
        {
            sut.DocumentWidth.Should().Be(210);
            sut.DocumentHeight.Should().Be(297);
        }
    }

    [Fact]
    public void SetupDocument_A4Landscape_DocumentInitialized()
    {
        var sut = new XmlPrinter();
        var document = new PrintDocument();
        sut.LoadXml("<root paperSize=\"A4\" landscape=\"true\" />");

        sut.SetupDocument(document, PaperSizes);

        using (new AssertionScope())
        {
            sut.DocumentWidth.Should().Be(297);
            sut.DocumentHeight.Should().Be(210);
        }
    }

    [Fact]
    public void SetupDocument_Custom_DocumentInitialized()
    {
        var sut = new XmlPrinter();
        var document = new PrintDocument();
        sut.LoadXml("<root paperSize=\"custom\" width=\"10\" height=\"20\" />");

        sut.SetupDocument(document, PaperSizes);

        using (new AssertionScope())
        {
            sut.DocumentWidth.Should().Be(10);
            sut.DocumentHeight.Should().Be(20);
        }
    }

    [Fact]
    public void SetupDocument_Margins_DocumentInitialized()
    {
        var sut = new XmlPrinter();
        var document = new PrintDocument();
        sut.LoadXml("<root left=\"1\" top=\"2\" bottom=\"3\" />");

        sut.SetupDocument(document, PaperSizes);

        using (new AssertionScope())
        {
            sut.DocumentLeftMargin.Should().Be(1);
            sut.DocumentTopMargin.Should().Be(2);
            sut.DocumentBottomMargin.Should().Be(3);
        }
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

        var graphics = Substitute.For<IGraphics>();
        sut.TransformDocument(graphics);

        XDocument.Parse(sut.Document.OuterXml).Should().BeEquivalentTo(
            XDocument.Parse(
                "<root>"
                + "<text relX=\"0\">C1</text>"
                + "<move relY=\"4\" />"
                + "<text relX=\"0\">1</text>"
                + "<newPage />"
                + "<text relX=\"0\">C1</text>"
                + "<move relY=\"4\" />"
                + "<text relX=\"0\">2</text>"
                + "<move relY=\"4\" />"
                + "</root>"));
    }

    [Fact]
    public void TransformDocument_MoveAbsoluteAndRelative_CursorUpdated()
    {
        var sut = new XmlPrinter();
        sut.LoadXml("<root><move absX=\"10\" absY=\"20\" relX=\"3\" relY=\"4\" /></root>");

        // initialize cursor with irrelevant values
        sut.CursorX = 5;
        sut.CursorY = 8;

        var graphics = Substitute.For<IGraphics>();
        sut.TransformDocument(graphics);

        using (new AssertionScope())
        {
            sut.CursorX.Should().Be(13);
            sut.CursorY.Should().Be(24);
        }
    }

    [Fact]
    public void TransformDocument_MoveAbsolute_CursorUpdated()
    {
        var sut = new XmlPrinter();
        sut.LoadXml("<root><move absX=\"10\" absY=\"20\" /></root>");
        sut.CursorX = 5;
        sut.CursorY = 8;

        var graphics = Substitute.For<IGraphics>();
        sut.TransformDocument(graphics);

        using (new AssertionScope())
        {
            sut.CursorX.Should().Be(10);
            sut.CursorY.Should().Be(20);
        }
    }

    [Fact]
    public void TransformDocument_MoveRelativeOnly_CursorUpdated()
    {
        var sut = new XmlPrinter();
        sut.LoadXml("<root><move relX=\"10\" relY=\"20\" /></root>");
        sut.CursorX = 5;
        sut.CursorY = 8;

        var graphics = Substitute.For<IGraphics>();
        sut.TransformDocument(graphics);

        using (new AssertionScope())
        {
            sut.CursorX.Should().Be(15);
            sut.CursorY.Should().Be(28);
        }
    }

    [Fact]
    public void TransformDocument_NewPage_CursorReset()
    {
        var sut = new XmlPrinter();
        sut.LoadXml("<root><move absX=\"10\" absY=\"20\" relX=\"3\" relY=\"4\" /><newPage/></root>");

        // initialize cursor with irrelevant values
        sut.CursorX = 5;
        sut.CursorY = 8;

        var graphics = Substitute.For<IGraphics>();
        sut.TransformDocument(graphics);

        using (new AssertionScope())
        {
            sut.CursorX.Should().Be(0);
            sut.CursorY.Should().Be(0);
        }
    }

    [Fact]
    public void TransformDocument_PageTexts_ConvertedToLines()
    {
        var sut = new XmlPrinter { DocumentHeight = 10 };
        sut.LoadXml(
            "<root>"
            + "<pageTexts><font><text>page {pageNumber}</text></font></pageTexts>"
            + "<table><columns>"
            + "<column width=\"10\">C1</column>"
            + "</columns><data>"
            + "<tr><td>1</td></tr>"
            + "<tr><td>2</td></tr>"
            + "</data></table>"
            + "</root>");

        var graphics = Substitute.For<IGraphics>();
        sut.TransformDocument(graphics);

        XDocument.Parse(sut.Document.OuterXml).Should().BeEquivalentTo(
            XDocument.Parse(
                "<root>"
                + "<font><text>page 1</text></font>"
                + "<text relX=\"0\">C1</text>"
                + "<move relY=\"4\" />"
                + "<text relX=\"0\">1</text>"
                + "<newPage />"
                + "<font><text>page 2</text></font>"
                + "<text relX=\"0\">C1</text>"
                + "<move relY=\"4\" />"
                + "<text relX=\"0\">2</text>"
                + "<move relY=\"4\" />"
                + "</root>"));
    }

    [Fact]
    public void TransformDocument_Rectangle_ConvertedToLines()
    {
        var sut = new XmlPrinter();
        sut.LoadXml("<root><rectangle relFromX=\"10\" relFromY=\"20\" relToX=\"30\" relToY=\"40\" /></root>");

        var graphics = Substitute.For<IGraphics>();
        sut.TransformDocument(graphics);

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

        var graphics = Substitute.For<IGraphics>();
        sut.TransformDocument(graphics);

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
    public void TransformDocument_TableWithFont_ConvertedToTextsFontKept()
    {
        var sut = new XmlPrinter { DocumentHeight = 100 };
        sut.LoadXml(
            "<root>"
            + "<font size=\"20\">"
            + "<table><columns>"
            + "<column width=\"10\">C1</column>"
            + "<column width=\"20\">C2</column>"
            + "</columns><data>"
            + "<tr><td>1</td><td>2</td></tr>"
            + "</data></table>"
            + "</font>"
            + "</root>");

        var graphics = Substitute.For<IGraphics>();
        sut.TransformDocument(graphics);

        XDocument.Parse(sut.Document.OuterXml).Should().BeEquivalentTo(
            XDocument.Parse(
                "<root>"
                + "<font size=\"20\">"
                + "<text relX=\"0\">C1</text>"
                + "<text relX=\"10\">C2</text>"
                + "<move relY=\"4\" />" // DefaultLineHeight
                + "<text relX=\"0\">1</text>"
                + "<text relX=\"10\">2</text>"
                + "<move relY=\"4\" />"
                + "</font>"
                + "</root>"));
    }

    [Fact]
    public void TransformDocument_TableBottomLine_ConvertedToTexts()
    {
        var sut = new XmlPrinter { DocumentHeight = 100 };
        sut.LoadXml(
            "<root>"
            + "<table><columns>"
            + "<column width=\"10\" bottomLine=\"true\">C1</column>"
            + "<column width=\"20\">C2</column>"
            + "</columns><data>"
            + "<tr><td>1</td><td>2</td></tr>"
            + "</data></table>"
            + "</root>");

        var graphics = Substitute.For<IGraphics>();
        sut.TransformDocument(graphics);

        XDocument.Parse(sut.Document.OuterXml).Should().BeEquivalentTo(
            XDocument.Parse(
                "<root>"
                + "<line relToX=\"10\" relFromY=\"4\" relToY=\"4\" />"
                + "<text relX=\"0\">C1</text>"
                + "<text relX=\"10\">C2</text>"
                + "<move relY=\"4\" />"
                + "<line relToX=\"10\" relFromY=\"4\" relToY=\"4\" />"
                + "<text relX=\"0\">1</text>"
                + "<text relX=\"10\">2</text>"
                + "<move relY=\"4\" />"
                + "</root>"));
    }

    [Fact]
    public void TransformDocument_TableCenterAlign_ConvertedToTexts()
    {
        var sut = new XmlPrinter { DocumentHeight = 100 };
        sut.LoadXml(
            "<root>"
            + "<table><columns>"
            + "<column width=\"10\" align=\"center\">C1</column>"
            + "<column width=\"20\">C2</column>"
            + "</columns><data>"
            + "<tr><td>1</td><td>2</td></tr>"
            + "</data></table>"
            + "</root>");

        var graphics = Substitute.For<IGraphics>();
        sut.TransformDocument(graphics);

        XDocument.Parse(sut.Document.OuterXml).Should().BeEquivalentTo(
            XDocument.Parse(
                "<root>"
                + "<text relX=\"5\" align=\"center\">C1</text>"
                + "<text relX=\"10\">C2</text>"
                + "<move relY=\"4\" />" // DefaultLineHeight
                + "<text relX=\"5\" align=\"center\">1</text>"
                + "<text relX=\"10\">2</text>"
                + "<move relY=\"4\" />"
                + "</root>"));
    }

    [Fact]
    public void TransformDocument_TableLeftLine_ConvertedToTexts()
    {
        var sut = new XmlPrinter { DocumentHeight = 100 };
        sut.LoadXml(
            "<root>"
            + "<table><columns>"
            + "<column width=\"10\" leftLine=\"true\">C1</column>"
            + "<column width=\"20\">C2</column>"
            + "</columns><data>"
            + "<tr><td>1</td><td>2</td></tr>"
            + "</data></table>"
            + "</root>");

        var graphics = Substitute.For<IGraphics>();
        sut.TransformDocument(graphics);

        XDocument.Parse(sut.Document.OuterXml).Should().BeEquivalentTo(
            XDocument.Parse(
                "<root>"
                + "<line relToY=\"4\" />" // DefaultLineHeight
                + "<text relX=\"0\">C1</text>"
                + "<text relX=\"10\">C2</text>"
                + "<move relY=\"4\" />"
                + "<line relToY=\"4\" />"
                + "<text relX=\"0\">1</text>"
                + "<text relX=\"10\">2</text>"
                + "<move relY=\"4\" />"
                + "</root>"));
    }

    [Fact]
    public void TransformDocument_TableRightAlign_ConvertedToTexts()
    {
        var sut = new XmlPrinter { DocumentHeight = 100 };
        sut.LoadXml(
            "<root>"
            + "<table><columns>"
            + "<column width=\"10\" align=\"right\">C1</column>"
            + "<column width=\"20\">C2</column>"
            + "</columns><data>"
            + "<tr><td>1</td><td>2</td></tr>"
            + "</data></table>"
            + "</root>");

        var graphics = Substitute.For<IGraphics>();
        sut.TransformDocument(graphics);

        XDocument.Parse(sut.Document.OuterXml).Should().BeEquivalentTo(
            XDocument.Parse(
                "<root>"
                + "<text relX=\"10\" align=\"right\">C1</text>"
                + "<text relX=\"10\">C2</text>"
                + "<move relY=\"4\" />" // DefaultLineHeight
                + "<text relX=\"10\" align=\"right\">1</text>"
                + "<text relX=\"10\">2</text>"
                + "<move relY=\"4\" />"
                + "</root>"));
    }

    [Fact]
    public void TransformDocument_TableRightLine_ConvertedToTexts()
    {
        var sut = new XmlPrinter { DocumentHeight = 100 };
        sut.LoadXml(
            "<root>"
            + "<table><columns>"
            + "<column width=\"10\" rightLine=\"true\">C1</column>"
            + "<column width=\"20\">C2</column>"
            + "</columns><data>"
            + "<tr><td>1</td><td>2</td></tr>"
            + "</data></table>"
            + "</root>");

        var graphics = Substitute.For<IGraphics>();
        sut.TransformDocument(graphics);

        XDocument.Parse(sut.Document.OuterXml).Should().BeEquivalentTo(
            XDocument.Parse(
                "<root>"
                + "<line relFromX=\"10\" relToX=\"10\" relToY=\"4\" />"
                + "<text relX=\"0\">C1</text>"
                + "<text relX=\"10\">C2</text>"
                + "<move relY=\"4\" />"
                + "<line relFromX=\"10\" relToX=\"10\" relToY=\"4\" />"
                + "<text relX=\"0\">1</text>"
                + "<text relX=\"10\">2</text>"
                + "<move relY=\"4\" />"
                + "</root>"));
    }

    [Fact]
    public void TransformDocument_TableStartAtBottom_NewPageBeforeTable()
    {
        var sut = new XmlPrinter { DocumentHeight = 20 };
        sut.LoadXml(
            "<root>"
            + "<move relY=\"15\" />"
            + "<table><columns>"
            + "<column width=\"10\">C1</column>"
            + "</columns><data>"
            + "<tr><td>1</td></tr>"
            + "<tr><td>2</td></tr>"
            + "</data></table>"
            + "</root>");

        var graphics = Substitute.For<IGraphics>();
        sut.TransformDocument(graphics);

        XDocument.Parse(sut.Document.OuterXml).Should().BeEquivalentTo(
            XDocument.Parse(
                "<root>"
                + "<move relY=\"15\" />"
                + "<newPage />"
                + "<text relX=\"0\">C1</text>"
                + "<move relY=\"4\" />"
                + "<text relX=\"0\">1</text>"
                + "<move relY=\"4\" />"
                + "<text relX=\"0\">2</text>"
                + "<move relY=\"4\" />"
                + "</root>"));
    }

    [Fact]
    public void TransformDocument_TableTopLine_ConvertedToTexts()
    {
        var sut = new XmlPrinter { DocumentHeight = 100 };
        sut.LoadXml(
            "<root>"
            + "<table><columns>"
            + "<column width=\"10\" topLine=\"true\">C1</column>"
            + "<column width=\"20\">C2</column>"
            + "</columns><data>"
            + "<tr><td>1</td><td>2</td></tr>"
            + "</data></table>"
            + "</root>");

        var graphics = Substitute.For<IGraphics>();
        sut.TransformDocument(graphics);

        XDocument.Parse(sut.Document.OuterXml).Should().BeEquivalentTo(
            XDocument.Parse(
                "<root>"
                + "<line relToX=\"10\" />"
                + "<text relX=\"0\">C1</text>"
                + "<text relX=\"10\">C2</text>"
                + "<move relY=\"4\" />"
                + "<line relToX=\"10\" />"
                + "<text relX=\"0\">1</text>"
                + "<text relX=\"10\">2</text>"
                + "<move relY=\"4\" />"
                + "</root>"));
    }

    [Fact]
    public void TransformDocument_TableWithHighLine_NewPageBefore()
    {
        var sut = new XmlPrinter { DocumentHeight = 30 };
        sut.LoadXml(
            "<root>"
            + "<move relY=\"15\" />"
            + "<table><columns>"
            + "<column width=\"10\">C1</column>"
            + "</columns><data>"
            + "<tr><td>1</td></tr>"
            + "<tr lineHeight=\"20\"><td>2</td></tr>"
            + "</data></table>"
            + "</root>");

        var graphics = Substitute.For<IGraphics>();
        sut.TransformDocument(graphics);

        XDocument.Parse(sut.Document.OuterXml).Should().BeEquivalentTo(
            XDocument.Parse(
                "<root>"
                + "<move relY=\"15\" />"
                + "<text relX=\"0\">C1</text>"
                + "<move relY=\"4\" />"
                + "<text relX=\"0\">1</text>"
                + "<move relY=\"4\" />"
                + "<newPage />"
                + "<text relX=\"0\">C1</text>"
                + "<move relY=\"4\" />"
                + "<text relX=\"0\">2</text>"
                + "<move relY=\"20\" />"
                + "</root>"));
    }
}
