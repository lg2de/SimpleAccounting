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
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows.Forms;
    using System.Xml;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Extensions;

    [SuppressMessage("Minor Code Smell", "S1192:String literals should not be duplicated", Justification = "<Pending>")]
    internal class XmlPrinter : IXmlPrinter
    {
        public const int DefaultLineHeight = 4;
        private readonly Stack<Font> fontStack;

        private readonly Stack<Pen> penStack;
        private readonly Stack<SolidBrush> solidBrushStack;

        private XmlNode currentNode;
        private XmlNode pageTextsNode;

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
            string fullResourceName = assembly.GetManifestResourceNames()
                .SingleOrDefault(str => str.EndsWith(resourceName, StringComparison.InvariantCultureIgnoreCase));

            if (fullResourceName == null)
            {
                throw new ArgumentException($"The report {resourceName} is invalid.");
            }

            using (Stream stream = assembly.GetManifestResourceStream(fullResourceName))
            {
                this.Document.Load(stream);
            }
        }

        public void PrintDocument(string documentName)
        {
            var printDocument = new PrintDocument { DocumentName = documentName };

            var paperSizes = printDocument.PrinterSettings.PaperSizes;
            this.SetupDocument(printDocument, paperSizes.OfType<PaperSize>());

            this.TransformDocument();

            // The setup and cleanup procedured must be executed by event handlers
            // because they are executed for preview AND real printing.
            printDocument.BeginPrint += (_, printArgs) => this.SetupGraphics();
            printDocument.EndPrint += (_, printArgs) => this.CleanupGraphics();

            using (var dialog = new PrintPreviewDialog
            {
                Document = printDocument, WindowState = FormWindowState.Maximized,
            })
            {
                dialog.ShowDialog();
            }
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

            this.printFactor = (float)(100 / 25.4);
            string documentPaperSize = this.Document.DocumentElement.GetAttribute<string>("paperSize", "A4");
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
                this.DocumentWidth = this.Document.DocumentElement.GetAttribute("width", 210);
                this.DocumentHeight = this.Document.DocumentElement.GetAttribute("height", 297);
                var documentScale = this.Document.DocumentElement.GetAttribute("scale", 1.0);
                this.printFactor *= documentScale;
                int width = this.ToPhysical(this.DocumentWidth);
                int height = this.ToPhysical(this.DocumentHeight);
                printDocument.DefaultPageSettings.PaperSize = new PaperSize(documentPaperSize, width, height);
            }

            printDocument.DefaultPageSettings.Landscape =
                this.Document.DocumentElement.GetAttribute("landscape", false);
            if (printDocument.DefaultPageSettings.Landscape)
            {
                // "rotate" paper
                int newHeight = this.DocumentWidth;
                this.DocumentWidth = this.DocumentHeight;
                this.DocumentHeight = newHeight;
            }

            this.DocumentLeftMargin = this.Document.DocumentElement.GetAttribute("left", 0);
            this.DocumentTopMargin = this.Document.DocumentElement.GetAttribute("top", 0);
            this.DocumentBottomMargin = this.Document.DocumentElement.GetAttribute("bottom", 0);

            this.ProcessNewPage();
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
            Justification = "Stack items will be disposed explicitely.")]
        internal void SetupGraphics()
        {
            var defaultPen = new Pen(Color.Black);
            var defaultBrush = new SolidBrush(Color.Black);
            var defaultFont = new Font("Arial", 10);
            this.penStack.Push(defaultPen);
            this.solidBrushStack.Push(defaultBrush);
            this.fontStack.Push(defaultFont);

            this.currentNode = this.Document.DocumentElement.FirstChild;
        }

        internal void CleanupGraphics()
        {
            this.penStack.Pop().Dispose();
            this.solidBrushStack.Pop().Dispose();
            this.fontStack.Pop().Dispose();
        }

        internal void TransformDocument()
        {
            this.currentNode = this.Document.DocumentElement.FirstChild;
            this.TransformNodes(this.currentNode);
            this.ProcessPageTexts();
        }

        internal void TransformNodes(XmlNode firstNode)
        {
            XmlNode nextNode = firstNode;
            while (nextNode != null)
            {
                XmlNode transformingNode = nextNode;
                nextNode = nextNode.NextSibling;

                if (transformingNode.Name == "move")
                {
                    this.ProcessMoveNode(transformingNode);
                }
                else if (transformingNode.Name == "rectangle")
                {
                    this.TransformRectangle(transformingNode);
                }
                else if (transformingNode.Name == "table")
                {
                    this.TransformTable(transformingNode);
                }
                else if (transformingNode.Name == "pageTexts")
                {
                    this.pageTextsNode = transformingNode;
                    this.InsertPageTexts(transformingNode, 1);
                    transformingNode.ParentNode.RemoveChild(transformingNode);
                    continue;
                }
                else if (transformingNode.Name == "newPage")
                {
                    this.ProcessNewPage();
                }

                this.TransformNodes(transformingNode.FirstChild);

                if (nextNode != null
                    && this.CursorY >= this.DocumentHeight - this.DocumentBottomMargin)
                {
                    XmlNode newPage = this.Document.CreateElement("newPage");
                    nextNode.ParentNode.InsertBefore(newPage, nextNode);

                    this.ProcessNewPage();
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

                if (this.currentNode.Name == "move")
                {
                    this.ProcessMoveNode(this.currentNode);
                }
                else if (this.currentNode.Name == "text")
                {
                    this.PrintTextNode(graphics);
                }
                else if (this.currentNode.Name == "line")
                {
                    this.PrintLineNode(graphics);
                }
                else if (this.currentNode.Name == "font")
                {
                    this.PrintFontNode(graphics);
                }
                else if (this.currentNode.Name == "newPage")
                {
                    graphics.HasMorePages = true;
                    this.currentNode = this.currentNode.NextSibling;
                    return;
                }

                if (this.currentNode.NextSibling != null)
                {
                    this.currentNode = this.currentNode.NextSibling;
                    continue;
                }

                if (this.currentNode.ParentNode != null
                    && this.currentNode.ParentNode.Name != "xEport")
                {
                    this.currentNode = this.currentNode.ParentNode.NextSibling;
                }

                // step back in stack
                return;
            }
        }

        private void TransformRectangle(XmlNode rectNode)
        {
            var x1 = rectNode.GetAttribute<float>("relFromX");
            var y1 = rectNode.GetAttribute<float>("relFromY");
            var x2 = rectNode.GetAttribute<float>("relToX");
            var y2 = rectNode.GetAttribute<float>("relToY");

            XmlNode lineNode = this.Document.CreateElement("line");
            lineNode.SetAttribute("relFromX", x1);
            lineNode.SetAttribute("relFromY", y1);
            lineNode.SetAttribute("relToX", x2);
            lineNode.SetAttribute("relToY", y1);
            rectNode.ParentNode.InsertBefore(lineNode, rectNode);

            lineNode = this.Document.CreateElement("line");
            lineNode.SetAttribute("relFromX", x2);
            lineNode.SetAttribute("relFromY", y1);
            lineNode.SetAttribute("relToX", x2);
            lineNode.SetAttribute("relToY", y2);
            rectNode.ParentNode.InsertBefore(lineNode, rectNode);

            lineNode = this.Document.CreateElement("line");
            lineNode.SetAttribute("relFromX", x2);
            lineNode.SetAttribute("relFromY", y2);
            lineNode.SetAttribute("relToX", x1);
            lineNode.SetAttribute("relToY", y2);
            rectNode.ParentNode.InsertBefore(lineNode, rectNode);

            lineNode = this.Document.CreateElement("line");
            lineNode.SetAttribute("relFromX", x1);
            lineNode.SetAttribute("relFromY", y2);
            lineNode.SetAttribute("relToX", x1);
            lineNode.SetAttribute("relToY", y1);
            rectNode.ParentNode.InsertBefore(lineNode, rectNode);

            rectNode.ParentNode.RemoveChild(rectNode);
        }

        private void TransformTable(XmlNode tableNode)
        {
            int tableLineHeight = tableNode.GetAttribute("lineHeight", DefaultLineHeight);

            XmlNodeList columnNodes = tableNode.SelectNodes("columns/column");
            XmlNodeList dataNodes = tableNode.SelectNodes("data/tr");

            // if table can not be started on page - create new one
            if (this.CursorY + tableLineHeight * 2 > this.DocumentHeight - this.DocumentBottomMargin)
            {
                XmlNode newPage = this.Document.CreateElement("newPage");
                tableNode.ParentNode.InsertBefore(newPage, tableNode);
                this.CursorY = this.DocumentTopMargin;
            }

            this.TransformTableHeader(tableNode);

            for (int nodeIndex = 0; nodeIndex < dataNodes.Count; nodeIndex++)
            {
                this.TransformTableRow(tableNode, tableLineHeight, columnNodes, dataNodes, nodeIndex);
            }

            tableNode.ParentNode.RemoveChild(tableNode);
        }

        private void TransformTableHeader(XmlNode tableNode)
        {
            XmlNode columnsRoot =
                tableNode.SelectSingleNode("columns")
                ?? throw new InvalidOperationException($"The table must define columns.");
            XmlNodeList columnNodes =
                tableNode.SelectNodes("columns/column")
                ?? throw new InvalidOperationException($"The table must define at least one column.");

            int tableLineHeight = tableNode.GetAttribute("lineHeight", DefaultLineHeight);
            int headerLineHeight = columnsRoot.GetAttribute("lineHeight", tableLineHeight);

            int xPosition = 0;
            foreach (XmlNode columnNode in columnNodes)
            {
                var width = columnNode.GetAttribute<int>("width");

                XmlNode textNode = this.Document.CreateElement("text");
                textNode.InnerText = columnNode.InnerText;

                var columnWidth = columnNode.GetAttribute<int>("width");
                int xAdoption = 0;
                var align = columnNode.GetAttribute<string>("align");
                if (!string.IsNullOrEmpty(align))
                {
                    textNode.SetAttribute("align", align);
                    if (align == "right")
                    {
                        xAdoption = columnWidth;
                    }
                    else if (align == "center")
                    {
                        xAdoption = columnWidth / 2;
                    }
                }

                textNode.SetAttribute("relX", xPosition + xAdoption);

                this.CreateFrame(columnNode, tableNode, xPosition, 0, xPosition + width, headerLineHeight);
                tableNode.ParentNode.InsertBefore(textNode, tableNode);

                xPosition += width;
            }

            this.CreateFrame(columnsRoot, tableNode, 0, 0, xPosition, headerLineHeight);

            XmlNode moveNode = this.Document.CreateElement("move");
            moveNode.SetAttribute("relY", headerLineHeight);
            tableNode.ParentNode.InsertBefore(moveNode, tableNode);
            this.CursorY += headerLineHeight;
        }

        private void TransformTableRow(XmlNode tableNode, int tableLineHeight, XmlNodeList columnNodes,
            XmlNodeList dataNodes, int nodeIndex)
        {
            var dataNode = dataNodes[nodeIndex];

            XmlNodeList rowNodes = dataNode.SelectNodes("td");
            int nInnerLineCount = 1;
            for (int i = 0; i < rowNodes.Count; i++)
            {
                XmlNode rowNode = rowNodes[i];
                string strText = rowNode.InnerText;
                nInnerLineCount += strText.Length / 40;
            }

            var lineHeight = dataNode.GetAttribute("lineHeight", tableLineHeight * nInnerLineCount);

            // check whether oversized line still fits into page
            if (this.CursorY + lineHeight > this.DocumentHeight - this.DocumentBottomMargin)
            {
                StartNewPage();
            }

            int xPosition = 0;
            for (int columnIndex = 0; columnIndex < rowNodes.Count; columnIndex++)
            {
                XmlNode columnNode = columnNodes[columnIndex];
                XmlNode rowNode = rowNodes[columnIndex];
                XmlNode textNode = this.Document.CreateElement("text");
                string strText = rowNode.InnerText;

                // line break
                for (int j = 40; j < strText.Length; j += 41)
                {
                    strText = strText.Insert(j, "\n");
                }

                textNode.InnerText = strText;
                int colmnWidth = columnNode.GetAttribute<int>("width");
                int xAdoption = 0;
                var align = rowNode.Attributes.GetNamedItem("align")
                            ?? columnNode.Attributes.GetNamedItem("align");
                if (align != null)
                {
                    textNode.SetAttribute("align", align.Value);
                    if (align.Value == "right")
                    {
                        xAdoption = colmnWidth;
                    }
                    else if (align.Value == "center")
                    {
                        xAdoption = colmnWidth / 2;
                    }
                }

                textNode.SetAttribute("relX", (xPosition + xAdoption));
                tableNode.ParentNode.InsertBefore(textNode, tableNode);
                this.CreateFrame(columnNode, textNode, xPosition, 0, xPosition + colmnWidth, lineHeight);
                xPosition += colmnWidth;
            }

            this.CreateFrame(dataNode, tableNode, 0, 0, xPosition, lineHeight);

            this.CursorY += lineHeight;

            // check whether next line exists and fits into page
            if (nodeIndex + 1 < dataNodes.Count
                && this.CursorY + tableLineHeight > this.DocumentHeight - this.DocumentBottomMargin)
            {
                StartNewPage();
                return;
            }

            // move cursor to next line
            XmlNode moveNode = this.Document.CreateElement("move");
            moveNode.SetAttribute("relY", lineHeight);
            tableNode.ParentNode.InsertBefore(moveNode, tableNode);

            void StartNewPage()
            {
                // start new page with table header
                XmlNode newPage = this.Document.CreateElement("newPage");
                tableNode.ParentNode.InsertBefore(newPage, tableNode);
                this.CursorY = this.DocumentTopMargin;
                this.TransformTableHeader(tableNode);
            }
        }

        private void ProcessMoveNode(XmlNode node)
        {
            var absX = node.GetAttribute<int>("absX");
            var absY = node.GetAttribute<int>("absY");
            var relX = node.GetAttribute<int>("relX");
            var relY = node.GetAttribute<int>("relY");
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

            var pageWraps = this.Document.SelectNodes("//newPage");
            foreach (XmlNode pageWrap in pageWraps)
            {
                pageNumber++;
                this.InsertPageTexts(pageWrap, pageNumber);
            }
        }

        private void InsertPageTexts(XmlNode currentNode, int pageNumber)
        {
            var insertParent = currentNode;
            foreach (XmlNode child in this.pageTextsNode.ChildNodes)
            {
                var copiedChild = insertParent.OwnerDocument.ImportNode(child, deep: true);
                var textElements = copiedChild.SelectNodes("//text");
                foreach (XmlElement element in textElements)
                {
                    element.InnerText = element.InnerText.Replace("{page}", pageNumber.ToString());
                }

                insertParent.ParentNode.InsertAfter(copiedChild, insertParent);
                insertParent = copiedChild;
            }
        }

        private XmlNode CreateLineNode(int fromX, int fromY, int toX, int toY)
        {
            XmlNode newNode = this.Document.CreateElement("line");
            if (fromX != 0)
            {
                newNode.SetAttribute("relFromX", fromX);
            }

            if (fromY != 0)
            {
                newNode.SetAttribute("relFromY", fromY);
            }

            if (toX != 0)
            {
                newNode.SetAttribute("relToX", toX);
            }

            if (toY != 0)
            {
                newNode.SetAttribute("relToY", toY);
            }

            return newNode;
        }

        private void CreateFrame(XmlNode referenceNode, XmlNode positionNode, int x1, int y1, int x2, int y2)
        {
            if (referenceNode == null)
            {
                throw new ArgumentNullException(nameof(referenceNode));
            }

            if (positionNode?.ParentNode == null)
            {
                throw new ArgumentNullException(nameof(positionNode));
            }

            var leftLine = referenceNode.GetAttribute<bool>("leftLine");
            if (leftLine)
            {
                XmlNode line = this.CreateLineNode(x1, y1, x1, y2);
                positionNode.ParentNode.InsertBefore(line, positionNode);
            }

            var rightLine = referenceNode.GetAttribute<bool>("rightLine");
            if (rightLine)
            {
                XmlNode line = this.CreateLineNode(x2, y1, x2, y2);
                positionNode.ParentNode.InsertBefore(line, positionNode);
            }

            var topLine = referenceNode.GetAttribute<bool>("topLine");
            if (topLine)
            {
                XmlNode line = this.CreateLineNode(x1, y1, x2, y1);
                positionNode.ParentNode.InsertBefore(line, positionNode);
            }

            var bottomLine = referenceNode.GetAttribute<bool>("bottomLine");
            if (bottomLine)
            {
                XmlNode line = this.CreateLineNode(x1, y2, x2, y2);
                positionNode.ParentNode.InsertBefore(line, positionNode);
            }
        }

        private void PrintTextNode(IGraphics graphics)
        {
            SolidBrush drawBrush = this.solidBrushStack.Peek();
            Font drawFont = this.fontStack.Peek();

            var absX = this.currentNode.GetAttribute<int?>("absX");
            var absY = this.currentNode.GetAttribute<int?>("absY");
            var relX = this.currentNode.GetAttribute<int>("relX");
            var relY = this.currentNode.GetAttribute<int>("relY");
            var align = this.currentNode.GetAttribute("align", "left");
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

            string text = this.currentNode.InnerText;
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
            Pen drawPen = this.penStack.Peek();

            var absFromX = this.currentNode.GetAttribute<int?>("absFromX");
            var absFromY = this.currentNode.GetAttribute<int?>("absFromY");
            var relFromX = this.currentNode.GetAttribute<int?>("relFromX");
            var relFromY = this.currentNode.GetAttribute<int?>("relFromY");
            var absToX = this.currentNode.GetAttribute<int?>("absToX");
            var absToY = this.currentNode.GetAttribute<int?>("absToY");
            var relToX = this.currentNode.GetAttribute<int?>("relToX");
            var relToY = this.currentNode.GetAttribute<int?>("relToY");
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

        private void PrintFontNode(IGraphics graphics)
        {
            Font drawFont = this.fontStack.Peek();

            XmlNode nodeBold = this.currentNode.Attributes.GetNamedItem("bold");
            string fontName = this.currentNode.GetAttribute<string>("name") ?? drawFont.Name;
            float fontSize = this.currentNode.GetAttribute<float?>("size") ?? drawFont.SizeInPoints;
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

#pragma warning disable CA2000 // Dispose objects before losing scope
            var newFont = new Font(fontName, fontSize, fontStyle);
#pragma warning restore CA2000 // The font stack will be disposed explicitely.

            if (this.currentNode.ChildNodes.Count > 0)
            {
                // change font temporary for subnodes
                this.fontStack.Push(newFont);
                var stackNode = this.currentNode;
                this.currentNode = this.currentNode.FirstChild;
                this.PrintNodes(graphics);
                this.currentNode = stackNode;
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
