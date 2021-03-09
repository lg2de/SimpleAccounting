// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Drawing.Printing;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Forms;
    using System.Xml;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Extensions;

    /// <summary>
    ///     Implements the conversion of XML into print preview (Windows Forms).
    /// </summary>
    internal class XmlPrinter : IXmlPrinter
    {
        private const int A4Width = 210;
        private const int A4Height = 297;
        private const double DefaultPrintFactor = 100 / 25.4; // mm > inch
        private const int DefaultLineHeight = 4;
        private const int Two = 2;

        private const string DocumentElementNode = "xEport";
        private const string PaperSizeNode = "paperSize";
        private const string LandscapeNode = "landscape";

        private const string WidthNode = "width";
        private const string HeightNode = "height";
        private const string LeftNode = "left";
        private const string TopNode = "top";
        private const string BottomNode = "bottom";
        private const string AlignNode = "align";

        private const string ScaleNode = "scale";
        private const string LineHeightNode = "lineHeight";

        private const string NewPageNode = "newPage";
        private const string MoveNode = "move";
        private const string RectangleNode = "rectangle";
        private const string TableNode = "table";
        private const string PageTextsNode = "pageTexts";
        private const string TextNode = "text";
        private const string LineNode = "line";
        private const string FontNode = "font";

        private const string RelativeXNode = "relX";
        private const string RelativeYNode = "relY";
        private const string RelativeFromXNode = "relFromX";
        private const string RelativeFromYNode = "relFromY";
        private const string RelativeToXNode = "relToX";
        private const string RelativeToYNode = "relToY";

        private const string AbsoluteXNode = "absX";
        private const string AbsoluteYNode = "absY";
        private const string AbsoluteFromXNode = "absFromX";
        private const string AbsoluteFromYNode = "absFromY";
        private const string AbsoluteToXNode = "absToX";
        private const string AbsoluteToYNode = "absToY";

        private readonly Stack<Font> fontStack;
        private readonly Stack<Pen> penStack;
        private readonly Stack<SolidBrush> solidBrushStack;

        private XmlNode? currentNode;
        private XmlNode? pageTextsNode;

        /// <summary>
        ///     Defines factor to translate logical position to physical position.
        /// </summary>
        private double printFactor;

        public XmlPrinter()
        {
            this.Document = new XmlDocument();
            this.penStack = new Stack<Pen>();
            this.solidBrushStack = new Stack<SolidBrush>();
            this.fontStack = new Stack<Font>();
        }

        internal int DocumentWidth { get; set; }
        internal int DocumentHeight { get; set; }

        internal int DocumentLeftMargin { get; set; }
        internal int DocumentTopMargin { get; set; }
        internal int DocumentBottomMargin { get; set; }

        internal int CursorX { get; set; }
        internal int CursorY { get; set; }

        public XmlDocument Document { get; }

        public void LoadDocument(string resourceName)
        {
            var assembly = this.GetType().Assembly;
            var fullResourceName = assembly.GetManifestResourceNames()
                .SingleOrDefault(str => str.EndsWith(resourceName, StringComparison.InvariantCultureIgnoreCase));
            if (fullResourceName == null)
            {
                throw new ArgumentException($"The report {resourceName} is invalid.");
            }

            using var stream = assembly.GetManifestResourceStream(fullResourceName);
            this.Document.Load(stream!);
        }

        public void PrintDocument(string documentName)
        {
            var printDocument = new PrintDocument { DocumentName = documentName };

            var paperSizes = printDocument.PrinterSettings.PaperSizes;
            this.SetupDocument(printDocument, paperSizes.OfType<PaperSize>());

            // The setup and cleanup procedures must be executed manually and by event handlers
            // because they are required for transformation, for preview AND real printing.
            this.TransformDocument();
            printDocument.BeginPrint += (_, printArgs) => this.SetupGraphics();
            printDocument.EndPrint += (_, printArgs) => this.CleanupGraphics();

            using var dialog = new PrintPreviewDialog
            {
                Document = printDocument, WindowState = FormWindowState.Maximized,
            };
            dialog.ShowDialog();
        }

        internal void LoadXml(string xml)
        {
            this.Document.LoadXml(xml);
        }

        internal void SetupDocument(PrintDocument printDocument, IEnumerable<PaperSize> paperSizes)
        {
            printDocument.PrintPage += (_, printArgs) =>
            {
                this.ProcessNewPage();

                var graphics = new DrawingGraphics(printArgs);
                this.PrintNodes(graphics);
            };

            this.printFactor = DefaultPrintFactor;
            var documentPaperSize = this.Document.DocumentElement.GetAttribute<string>(PaperSizeNode, "A4");
            var paperSize = paperSizes.FirstOrDefault(
                s => s.PaperName.StartsWith(documentPaperSize, StringComparison.CurrentCultureIgnoreCase));
            if (paperSize != null)
            {
                printDocument.DefaultPageSettings.PaperSize = paperSize;
                this.DocumentWidth = this.ToLogical(paperSize.Width);
                this.DocumentHeight = this.ToLogical(paperSize.Height);
            }
            else
            {
                this.DocumentWidth = this.Document.DocumentElement.GetAttribute(WidthNode, A4Width);
                this.DocumentHeight = this.Document.DocumentElement.GetAttribute(HeightNode, A4Height);
                var documentScale = this.Document.DocumentElement.GetAttribute(ScaleNode, 1.0);
                this.printFactor *= documentScale;
                int width = this.ToPhysical(this.DocumentWidth);
                int height = this.ToPhysical(this.DocumentHeight);
                printDocument.DefaultPageSettings.PaperSize = new PaperSize(documentPaperSize, width, height);
            }

            printDocument.DefaultPageSettings.Landscape =
                this.Document.DocumentElement.GetAttribute(LandscapeNode, false);
            if (printDocument.DefaultPageSettings.Landscape)
            {
                // "rotate" paper
                int newHeight = this.DocumentWidth;
                this.DocumentWidth = this.DocumentHeight;
                this.DocumentHeight = newHeight;
            }

            this.DocumentLeftMargin = this.Document.DocumentElement.GetAttribute(LeftNode, 0);
            this.DocumentTopMargin = this.Document.DocumentElement.GetAttribute(TopNode, 0);
            this.DocumentBottomMargin = this.Document.DocumentElement.GetAttribute(BottomNode, 0);

            this.ProcessNewPage();
        }

        [SuppressMessage(
            "Reliability", "CA2000:Dispose objects before losing scope",
            Justification = "Stack items will be disposed explicitly.")]
        internal void SetupGraphics()
        {
            var defaultPen = new Pen(Color.Black);
            var defaultBrush = new SolidBrush(Color.Black);
            var defaultFont = new Font("Arial", 10);
            this.penStack.Push(defaultPen);
            this.solidBrushStack.Push(defaultBrush);
            this.fontStack.Push(defaultFont);

            this.currentNode = this.Document.DocumentElement!.FirstChild;
        }

        internal void CleanupGraphics()
        {
            this.penStack.Pop().Dispose();
            this.solidBrushStack.Pop().Dispose();
            this.fontStack.Pop().Dispose();
        }

        internal void TransformDocument()
        {
            this.SetupGraphics();

            this.currentNode = this.Document.DocumentElement!.FirstChild;
            TransformNodes(this.currentNode);
            this.ProcessPageTexts();
            
            this.CleanupGraphics();

            void TransformNodes(XmlNode firstNode)
            {
                XmlNode? nextNode = firstNode;
                while (nextNode != null)
                {
                    XmlNode transformingNode = nextNode;
                    nextNode = nextNode.NextSibling;

                    switch (transformingNode.Name)
                    {
                    case MoveNode:
                        this.ProcessMoveNode(transformingNode);
                        break;
                    case RectangleNode:
                        this.TransformRectangle(transformingNode);
                        break;
                    case TableNode:
                        this.TransformTable(transformingNode);
                        break;
                    case PageTextsNode:
                        this.pageTextsNode = transformingNode;
                        this.InsertPageTexts(transformingNode, 1);
                        transformingNode.ParentNode!.RemoveChild(transformingNode);
                        continue;
                    case NewPageNode:
                        this.ProcessNewPage();
                        break;
                    case FontNode:
                        this.ProcessFontNode(transformingNode, () => TransformNodes(transformingNode.FirstChild));
                        continue;
                    default:
                        // nothing to do
                        break;
                    }

                    TransformNodes(transformingNode.FirstChild);

                    if (nextNode != null
                        && this.CursorY >= this.DocumentHeight - this.DocumentBottomMargin)
                    {
                        XmlNode newPage = this.Document.CreateElement(NewPageNode);
                        nextNode.ParentNode!.InsertBefore(newPage, nextNode);

                        this.ProcessNewPage();
                    }
                }
            }
        }

        internal void PrintNodes(IGraphics graphics)
        {
            while (this.currentNode != null)
            {
                if (graphics.HasMorePages)
                {
                    return;
                }

                switch (this.currentNode.Name)
                {
                case MoveNode:
                    this.ProcessMoveNode(this.currentNode);
                    break;
                case TextNode:
                    this.PrintTextNode(graphics);
                    break;
                case LineNode:
                    this.PrintLineNode(graphics);
                    break;
                case FontNode:
                    this.ProcessFontNode(this.currentNode, () =>
                    {
                        var stackNode = this.currentNode;
                        this.currentNode = this.currentNode.FirstChild;
                        this.PrintNodes(graphics);
                        this.currentNode = stackNode;
                    });
                    break;
                case NewPageNode:
                    graphics.HasMorePages = true;
                    this.currentNode = this.currentNode.NextSibling;
                    return;
                default:
                    throw new NotSupportedException($"Missing handler for {this.currentNode.Name}");
                }

                if (this.currentNode.NextSibling != null)
                {
                    this.currentNode = this.currentNode.NextSibling;
                    continue;
                }

                if (this.currentNode.ParentNode != null
                    && this.currentNode.ParentNode.Name != DocumentElementNode)
                {
                    this.currentNode = this.currentNode.ParentNode.NextSibling;
                }

                // step back in stack
                return;
            }
        }

        private void TransformRectangle(XmlNode rectNode)
        {
            var x1 = rectNode.GetAttribute<float>(RelativeFromXNode);
            var y1 = rectNode.GetAttribute<float>(RelativeFromYNode);
            var x2 = rectNode.GetAttribute<float>(RelativeToXNode);
            var y2 = rectNode.GetAttribute<float>(RelativeToYNode);

            XmlNode lineNode = this.Document.CreateElement(LineNode);
            lineNode.SetAttribute(RelativeFromXNode, x1);
            lineNode.SetAttribute(RelativeFromYNode, y1);
            lineNode.SetAttribute(RelativeToXNode, x2);
            lineNode.SetAttribute(RelativeToYNode, y1);
            rectNode.ParentNode!.InsertBefore(lineNode, rectNode);

            lineNode = this.Document.CreateElement(LineNode);
            lineNode.SetAttribute(RelativeFromXNode, x2);
            lineNode.SetAttribute(RelativeFromYNode, y1);
            lineNode.SetAttribute(RelativeToXNode, x2);
            lineNode.SetAttribute(RelativeToYNode, y2);
            rectNode.ParentNode.InsertBefore(lineNode, rectNode);

            lineNode = this.Document.CreateElement(LineNode);
            lineNode.SetAttribute(RelativeFromXNode, x2);
            lineNode.SetAttribute(RelativeFromYNode, y2);
            lineNode.SetAttribute(RelativeToXNode, x1);
            lineNode.SetAttribute(RelativeToYNode, y2);
            rectNode.ParentNode.InsertBefore(lineNode, rectNode);

            lineNode = this.Document.CreateElement(LineNode);
            lineNode.SetAttribute(RelativeFromXNode, x1);
            lineNode.SetAttribute(RelativeFromYNode, y2);
            lineNode.SetAttribute(RelativeToXNode, x1);
            lineNode.SetAttribute(RelativeToYNode, y1);
            rectNode.ParentNode.InsertBefore(lineNode, rectNode);

            rectNode.ParentNode.RemoveChild(rectNode);
        }

        private void TransformTable(XmlNode tableNode)
        {
            int tableLineHeight = tableNode.GetAttribute(LineHeightNode, DefaultLineHeight);

            var columnNodes =
                tableNode.SelectNodes("columns/column")
                ?? throw new InvalidOperationException("The table must define columns.");
            var dataNodes =
                tableNode.SelectNodes("data/tr")
                ?? throw new InvalidOperationException("The table must define rows.");

            // if table can not be started on page - create new one
            if (this.CursorY + tableLineHeight * Two > this.DocumentHeight - this.DocumentBottomMargin)
            {
                XmlNode newPage = this.Document.CreateElement(NewPageNode);
                tableNode.ParentNode!.InsertBefore(newPage, tableNode);
                this.CursorY = this.DocumentTopMargin;
            }

            this.TransformTableHeader(tableNode);

            for (int rowIndex = 0; rowIndex < dataNodes.Count; rowIndex++)
            {
                this.TransformTableRow(tableNode, tableLineHeight, columnNodes, dataNodes, rowIndex);
            }

            tableNode.ParentNode!.RemoveChild(tableNode);
        }

        private void TransformTableHeader(XmlNode tableNode)
        {
            XmlNode columnsRoot =
                tableNode.SelectSingleNode("columns")
                ?? throw new InvalidOperationException("The table must define columns.");
            XmlNodeList columnNodes =
                tableNode.SelectNodes("columns/column")
                ?? throw new InvalidOperationException("The table must define at least one column.");

            int tableLineHeight = tableNode.GetAttribute(LineHeightNode, DefaultLineHeight);
            int headerLineHeight = columnsRoot.GetAttribute(LineHeightNode, tableLineHeight);

            int xPosition = 0;
            foreach (var columnNode in columnNodes.OfType<XmlNode>())
            {
                var width = columnNode.GetAttribute<int>(WidthNode);

                XmlNode textNode = this.Document.CreateElement(TextNode);
                textNode.InnerText = columnNode.InnerText;

                var columnWidth = columnNode.GetAttribute<int>(WidthNode);
                int xAdoption = 0;
                var align = columnNode.GetAttribute<string>(AlignNode);
                if (!string.IsNullOrEmpty(align))
                {
                    textNode.SetAttribute(AlignNode, align);
                    xAdoption = align switch
                    {
                        "right" => columnWidth,
                        "center" => columnWidth / Two,
                        _ => 0
                    };
                }

                textNode.SetAttribute(RelativeXNode, xPosition + xAdoption);

                this.CreateFrame(columnNode, tableNode, xPosition, 0, xPosition + width, headerLineHeight);
                tableNode.ParentNode!.InsertBefore(textNode, tableNode);

                xPosition += width;
            }

            this.CreateFrame(columnsRoot, tableNode, 0, 0, xPosition, headerLineHeight);

            XmlNode moveNode = this.Document.CreateElement(MoveNode);
            moveNode.SetAttribute(RelativeYNode, headerLineHeight);
            tableNode.ParentNode!.InsertBefore(moveNode, tableNode);
            this.CursorY += headerLineHeight;
        }

        private void TransformTableRow(
            XmlNode tableNode, int tableLineHeight, XmlNodeList columnDefinitions,
            XmlNodeList dataRows, int rowIndex)
        {
            var dataRow = dataRows[rowIndex];
            var rowColumn = dataRow.SelectNodes("td");
            if (rowColumn == null)
            {
                // no data found
                return;
            }

            var wrappedTexts = new string[columnDefinitions.Count];
            for (int columnIndex = 0; columnIndex < columnDefinitions.Count; columnIndex++)
            {
                var columnDefinition = columnDefinitions[columnIndex];
                var columnText = columnIndex < rowColumn.Count ? rowColumn[columnIndex].InnerText : string.Empty;
                int maxWidth = columnDefinition.GetAttribute<int>(WidthNode);
                var currentFont = this.fontStack.Peek();
                wrappedTexts[columnIndex] = columnText.Wrap(this.ToPhysical(maxWidth), currentFont, 1.35);
            }

            var linesFactor = 1.0;
            linesFactor += wrappedTexts.Max(x => x.Count(c => c == '\n')) * 0.8;
            var automaticHeight = (int)(tableLineHeight * linesFactor);
            var lineHeight = dataRow.GetAttribute(LineHeightNode, automaticHeight);

            // check if oversized line still fits in page
            if (this.CursorY + lineHeight > this.DocumentHeight - this.DocumentBottomMargin)
            {
                StartNewPage();
            }

            int xPosition = 0;
            for (int columnIndex = 0; columnIndex < rowColumn.Count; columnIndex++)
            {
                var columnDefinition = columnDefinitions[columnIndex];
                var columnData = rowColumn[columnIndex];
                var wrappedText = wrappedTexts[columnIndex];
                XmlNode textNode = this.Document.CreateElement(TextNode);
                textNode.InnerText = wrappedText;
                int columnWidth = columnDefinition.GetAttribute<int>(WidthNode);
                int xAdoption = 0;
                var align = columnData.Attributes!.GetNamedItem(AlignNode)
                            ?? columnDefinition.Attributes!.GetNamedItem(AlignNode);
                if (align != null)
                {
                    textNode.SetAttribute(AlignNode, align.Value);
                    xAdoption = align.Value switch
                    {
                        "right" => columnWidth,
                        "center" => columnWidth / Two,
                        _ => 0
                    };
                }

                textNode.SetAttribute(RelativeXNode, (xPosition + xAdoption));
                tableNode.ParentNode!.InsertBefore(textNode, tableNode);
                this.CreateFrame(columnDefinition, textNode, xPosition, 0, xPosition + columnWidth, lineHeight);
                xPosition += columnWidth;
            }

            this.CreateFrame(dataRow, tableNode, 0, 0, xPosition, lineHeight);

            this.CursorY += lineHeight;

            // check whether next line exists and fits into page
            if (rowIndex + 1 < dataRows.Count
                && this.CursorY + tableLineHeight > this.DocumentHeight - this.DocumentBottomMargin)
            {
                StartNewPage();
                return;
            }

            // move cursor to next line
            XmlNode moveNode = this.Document.CreateElement(MoveNode);
            moveNode.SetAttribute(RelativeYNode, lineHeight);
            tableNode.ParentNode!.InsertBefore(moveNode, tableNode);

            void StartNewPage()
            {
                // start new page with table header
                XmlNode newPage = this.Document.CreateElement(NewPageNode);
                tableNode.ParentNode!.InsertBefore(newPage, tableNode);
                this.CursorY = this.DocumentTopMargin;
                this.TransformTableHeader(tableNode);
            }
        }

        private void ProcessMoveNode(XmlNode node)
        {
            var absX = node.GetAttribute<int>(AbsoluteXNode);
            var absY = node.GetAttribute<int>(AbsoluteYNode);
            var relX = node.GetAttribute<int>(RelativeXNode);
            var relY = node.GetAttribute<int>(RelativeYNode);
            if (absX != 0)
            {
                this.CursorX = this.DocumentLeftMargin + absX;
            }

            if (absY != 0)
            {
                this.CursorY = this.DocumentTopMargin + absY;
            }

            this.CursorX += relX;
            this.CursorY += relY;
        }

        private void ProcessNewPage()
        {
            this.CursorX = this.DocumentLeftMargin;
            this.CursorY = this.DocumentTopMargin;
        }

        private void ProcessPageTexts()
        {
            if (this.pageTextsNode == null)
            {
                // nothing to do
                return;
            }

            // page number 1 already assigned immediately
            int pageNumber = 1;

            var pageWraps = this.Document.SelectNodes($"//{NewPageNode}");
            if (pageWraps == null)
            {
                return;
            }

            foreach (var pageWrap in pageWraps.OfType<XmlNode>())
            {
                pageNumber++;
                this.InsertPageTexts(pageWrap, pageNumber);
            }
        }

        private void InsertPageTexts(XmlNode baseNode, int pageNumber)
        {
            var insertParent = baseNode;
            foreach (var child in this.pageTextsNode!.ChildNodes.OfType<XmlNode>())
            {
                var copiedChild = insertParent.OwnerDocument!.ImportNode(child, deep: true);
                var textElements = copiedChild.SelectNodes($"//{TextNode}");
                if (textElements == null)
                {
                    continue;
                }

                string pageNumberText = pageNumber.ToString(CultureInfo.InvariantCulture);
                foreach (var element in textElements.OfType<XmlElement>())
                {
                    element.InnerText = element.InnerText.Replace(
                        "{page}", pageNumberText, StringComparison.InvariantCultureIgnoreCase);
                }

                insertParent.ParentNode!.InsertAfter(copiedChild, insertParent);
                insertParent = copiedChild;
            }
        }

        private XmlNode CreateLineNode(int fromX, int fromY, int toX, int toY)
        {
            XmlNode newNode = this.Document.CreateElement(LineNode);
            if (fromX != 0)
            {
                newNode.SetAttribute(RelativeFromXNode, fromX);
            }

            if (fromY != 0)
            {
                newNode.SetAttribute(RelativeFromYNode, fromY);
            }

            if (toX != 0)
            {
                newNode.SetAttribute(RelativeToXNode, toX);
            }

            if (toY != 0)
            {
                newNode.SetAttribute(RelativeToYNode, toY);
            }

            return newNode;
        }

        private void CreateFrame(XmlNode referenceNode, XmlNode positionNode, int x1, int y1, int x2, int y2)
        {
            if (referenceNode == null)
            {
                throw new ArgumentNullException(nameof(referenceNode));
            }

            if (positionNode == null)
            {
                throw new ArgumentNullException(nameof(positionNode));
            }

            var leftLine = referenceNode.GetAttribute<bool>("leftLine");
            if (leftLine)
            {
                XmlNode line = this.CreateLineNode(x1, y1, x1, y2);
                positionNode.ParentNode!.InsertBefore(line, positionNode);
            }

            var rightLine = referenceNode.GetAttribute<bool>("rightLine");
            if (rightLine)
            {
                XmlNode line = this.CreateLineNode(x2, y1, x2, y2);
                positionNode.ParentNode!.InsertBefore(line, positionNode);
            }

            var topLine = referenceNode.GetAttribute<bool>("topLine");
            if (topLine)
            {
                XmlNode line = this.CreateLineNode(x1, y1, x2, y1);
                positionNode.ParentNode!.InsertBefore(line, positionNode);
            }

            var bottomLine = referenceNode.GetAttribute<bool>("bottomLine");
            if (bottomLine)
            {
                XmlNode line = this.CreateLineNode(x1, y2, x2, y2);
                positionNode.ParentNode!.InsertBefore(line, positionNode);
            }
        }

        private void PrintTextNode(IGraphics graphics)
        {
            if (this.currentNode == null)
            {
                throw new InvalidOperationException("The current node is uninitialized.");
            }

            SolidBrush drawBrush = this.solidBrushStack.Peek();
            Font drawFont = this.fontStack.Peek();

            var absX = this.currentNode.GetAttribute<int?>(AbsoluteXNode);
            var absY = this.currentNode.GetAttribute<int?>(AbsoluteYNode);
            var relX = this.currentNode.GetAttribute<int>(RelativeXNode);
            var relY = this.currentNode.GetAttribute<int>(RelativeYNode);
            var align = this.currentNode.GetAttribute(AlignNode, LeftNode);
            int nX = this.CursorX;
            int nY = this.CursorY;
            if (absX.HasValue)
            {
                nX = this.DocumentLeftMargin + absX.Value;
            }

            if (absY.HasValue)
            {
                nY = this.DocumentTopMargin + absY.Value;
            }

            nX += relX;
            nY += relY;

            string text = this.currentNode.InnerText.Translate();
            var alignment = align switch
            {
                "center" => StringAlignment.Center,
                "right" => StringAlignment.Far,
                _ => StringAlignment.Near,
            };
            graphics.DrawString(
                text,
                drawFont,
                drawBrush,
                this.ToPhysical(nX),
                this.ToPhysical(nY),
                alignment);
        }

        private void PrintLineNode(IGraphics graphics)
        {
            if (this.currentNode == null)
            {
                throw new InvalidOperationException("The current node is uninitialized.");
            }

            Pen drawPen = this.penStack.Peek();

            var absFromX = this.currentNode.GetAttribute<int?>(AbsoluteFromXNode);
            var absFromY = this.currentNode.GetAttribute<int?>(AbsoluteFromYNode);
            var relFromX = this.currentNode.GetAttribute<int?>(RelativeFromXNode);
            var relFromY = this.currentNode.GetAttribute<int?>(RelativeFromYNode);
            var absToX = this.currentNode.GetAttribute<int?>(AbsoluteToXNode);
            var absToY = this.currentNode.GetAttribute<int?>(AbsoluteToYNode);
            var relToX = this.currentNode.GetAttribute<int?>(RelativeToXNode);
            var relToY = this.currentNode.GetAttribute<int?>(RelativeToYNode);
            int x1 = this.CursorX;
            int y1 = this.CursorY;
            int x2 = this.CursorX;
            int y2 = this.CursorY;
            if (absFromX.HasValue)
            {
                x1 = this.DocumentLeftMargin + absFromX.Value;
            }

            if (absFromY.HasValue)
            {
                y1 = this.DocumentTopMargin + absFromY.Value;
            }

            if (relFromX.HasValue)
            {
                x1 += relFromX.Value;
            }

            if (relFromY.HasValue)
            {
                y1 += relFromY.Value;
            }

            if (absToX.HasValue)
            {
                x2 = this.DocumentLeftMargin + absToX.Value;
            }

            if (absToY.HasValue)
            {
                y2 = this.DocumentTopMargin + absToY.Value;
            }

            if (relToX.HasValue)
            {
                x2 += relToX.Value;
            }

            if (relToY.HasValue)
            {
                y2 += relToY.Value;
            }

            graphics.DrawLine(
                drawPen,
                this.ToPhysical(x1),
                this.ToPhysical(y1),
                this.ToPhysical(x2),
                this.ToPhysical(y2));
        }

        private void ProcessFontNode(XmlNode fontNode, Action recurseAction)
        {
            Font drawFont = this.fontStack.Peek();

            XmlNode nodeBold = fontNode.Attributes!.GetNamedItem("bold");
            var fontName = fontNode.GetAttribute("name", drawFont.Name);
            var fontSize = fontNode.GetAttribute("size", drawFont.SizeInPoints);
            FontStyle fontStyle = drawFont.Style;

            if (nodeBold != null)
            {
                if (nodeBold.Value == "1")
                {
                    fontStyle |= FontStyle.Bold;
                }
                else
                {
                    fontStyle &= ~FontStyle.Bold;
                }
            }

            var newFont = new Font(fontName, fontSize, fontStyle);

            if (fontNode.ChildNodes.Count > 0)
            {
                // change font temporary for sub-nodes
                this.fontStack.Push(newFont);
                recurseAction();
                this.fontStack.Pop().Dispose();
            }
            else
            {
                // change current font
                this.fontStack.Pop().Dispose();
                this.fontStack.Push(newFont);
            }
        }

        private int ToLogical(int input)
        {
            return Convert.ToInt32(input / this.printFactor);
        }

        private int ToPhysical(int input)
        {
            return Convert.ToInt32(input * this.printFactor);
        }
    }
}
