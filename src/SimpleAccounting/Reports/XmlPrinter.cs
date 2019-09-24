// <copyright>
//     Copyright (c) Lukas Gr�tzmacher. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using lg2de.SimpleAccounting.Extensions;

namespace lg2de.SimpleAccounting.Reports
{
    internal class XmlPrinter : IXmlPrinter
    {
        public const int DefaultLineHeight = 4;

        private readonly Stack<Pen> penStack;
        private readonly Stack<SolidBrush> solidBrushStack;
        private readonly Stack<Font> fontStack;

        private XmlNode currentNode;
        private int documentLeftMargin, documentRightMargin, documentTopMargin, documentBottomMargin, documentWidth;
        private float documentScale;
        private int cursorX, cursorY;
        private float printFactor;

        public XmlPrinter()
        {
            this.Document = new XmlDocument();
            this.penStack = new Stack<Pen>();
            this.solidBrushStack = new Stack<SolidBrush>();
            this.fontStack = new Stack<Font>();
        }

        public XmlDocument Document { get; }

        internal int DocumentHeight { get; set; }

        public void LoadDocument(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
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
            var doc = new PrintDocument();
            doc.BeginPrint += new PrintEventHandler(this.BeginPrint);
            doc.PrintPage += new PrintPageEventHandler(this.PrintPage);
            var dialog = new PrintPreviewDialog { Document = doc };
            doc.DocumentName = documentName;

            // init
            this.printFactor = (float)(100 / 25.4);
            XmlNode node = this.Document.DocumentElement.Attributes.GetNamedItem("papersize");
            string strPaperSize = "A4";
            if (node != null)
            {
                strPaperSize = node.Value;
            }

            bool bFound = false;
            foreach (PaperSize item in doc.PrinterSettings.PaperSizes)
            {
                if (!item.PaperName.StartsWith(strPaperSize, StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                doc.DefaultPageSettings.PaperSize = item;
                this.documentWidth = Convert.ToInt32(item.Width / this.printFactor);
                this.DocumentHeight = Convert.ToInt32(item.Height / this.printFactor);
                this.documentScale = 1;
                bFound = true;
                break;
            }

            if (!bFound)
            {
                node = this.Document.DocumentElement.Attributes.GetNamedItem("width");
                if (node != null)
                {
                    this.documentWidth = Convert.ToInt32(node.Value);
                }
                else
                {
                    this.documentWidth = 210;
                }

                node = this.Document.DocumentElement.Attributes.GetNamedItem("height");
                if (node != null)
                {
                    this.DocumentHeight = Convert.ToInt32(node.Value);
                }
                else
                {
                    this.DocumentHeight = 297;
                }

                node = this.Document.DocumentElement.Attributes.GetNamedItem("scale");
                if (node != null)
                {
                    this.documentScale = Convert.ToSingle(node.Value);
                }
                else
                {
                    this.documentScale = 1;
                }

                this.printFactor *= this.documentScale;
                doc.DefaultPageSettings.PaperSize = new PaperSize(strPaperSize, Convert.ToInt32(this.documentWidth * this.printFactor), Convert.ToInt32(this.DocumentHeight * this.printFactor));
            }

            doc.DefaultPageSettings.Landscape = false;
            node = this.Document.DocumentElement.Attributes.GetNamedItem("landscape");
            if (node?.Value == "1")
            {
                doc.DefaultPageSettings.Landscape = true;
            }

            if (doc.DefaultPageSettings.Landscape)
            {
                int nDummy = this.documentWidth;
                this.documentWidth = this.DocumentHeight;
                this.DocumentHeight = nDummy;
            }

            this.currentNode = this.Document.DocumentElement.FirstChild;
            node = this.Document.DocumentElement.Attributes.GetNamedItem("left");
            if (node != null)
            {
                this.documentLeftMargin = Convert.ToInt32(node.Value);
            }
            else
            {
                this.documentLeftMargin = 0;
            }

            node = this.Document.DocumentElement.Attributes.GetNamedItem("right");
            if (node != null)
            {
                this.documentRightMargin = Convert.ToInt32(node.Value);
            }
            else
            {
                this.documentRightMargin = 0;
            }

            node = this.Document.DocumentElement.Attributes.GetNamedItem("top");
            if (node != null)
            {
                this.documentTopMargin = Convert.ToInt32(node.Value);
            }
            else
            {
                this.documentTopMargin = 0;
            }

            node = this.Document.DocumentElement.Attributes.GetNamedItem("bottom");
            if (node != null)
            {
                this.documentBottomMargin = Convert.ToInt32(node.Value);
            }
            else
            {
                this.documentBottomMargin = 0;
            }

            this.cursorX = this.documentLeftMargin;
            this.cursorY = this.documentTopMargin;

            // transform
            this.TransformDocument();
            // show
            dialog.WindowState = FormWindowState.Maximized;
            dialog.ShowDialog();
        }

        internal void LoadXml(string xml)
        {
            this.Document.LoadXml(xml);
        }

        internal void TransformDocument()
        {
            this.TransformNodes(this.Document.DocumentElement.FirstChild);
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
                    XmlNode attr = transformingNode.Attributes.GetNamedItem("absX");
                    if (attr != null)
                    {
                        this.cursorX = this.documentLeftMargin + Convert.ToInt32(attr.Value);
                    }

                    attr = transformingNode.Attributes.GetNamedItem("absY");
                    if (attr != null)
                    {
                        this.cursorY = this.documentTopMargin + Convert.ToInt32(attr.Value);
                    }

                    attr = transformingNode.Attributes.GetNamedItem("relX");
                    if (attr != null)
                    {
                        this.cursorX += Convert.ToInt32(attr.Value);
                    }

                    attr = transformingNode.Attributes.GetNamedItem("relY");
                    if (attr != null)
                    {
                        this.cursorY += Convert.ToInt32(attr.Value);
                    }
                }
                else if (transformingNode.Name == "rectangle")
                {
                    this.TransformRectangle(transformingNode);
                }
                else if (transformingNode.Name == "table")
                {
                    this.TransformTable(transformingNode);
                }
                else if (transformingNode.Name == "newpage")
                {
                    this.cursorY = this.documentTopMargin;
                }

                this.TransformNodes(transformingNode.FirstChild);

                if (nextNode != null
                    && this.cursorY >= (this.DocumentHeight - this.documentBottomMargin))
                {
                    XmlNode newPage = this.Document.CreateElement("newpage");
                    nextNode.ParentNode.InsertBefore(newPage, nextNode);
                    this.cursorY = this.documentTopMargin;
                }
            }
        }

        private void TransformRectangle(XmlNode rectNode)
        {
            float nX1 = Convert.ToSingle(rectNode.Attributes.GetNamedItem("relFromX").Value);
            float nY1 = Convert.ToSingle(rectNode.Attributes.GetNamedItem("relFromY").Value);
            float nX2 = Convert.ToSingle(rectNode.Attributes.GetNamedItem("relToX").Value);
            float nY2 = Convert.ToSingle(rectNode.Attributes.GetNamedItem("relToY").Value);

            XmlNode lineNode = this.Document.CreateElement("line");
            lineNode.SetAttribute("relFromX", nX1.ToString());
            lineNode.SetAttribute("relFromY", nY1.ToString());
            lineNode.SetAttribute("relToX", nX2.ToString());
            lineNode.SetAttribute("relToY", nY1.ToString());
            rectNode.ParentNode.InsertBefore(lineNode, rectNode);

            lineNode = this.Document.CreateElement("line");
            lineNode.SetAttribute("relFromX", nX2.ToString());
            lineNode.SetAttribute("relFromY", nY1.ToString());
            lineNode.SetAttribute("relToX", nX2.ToString());
            lineNode.SetAttribute("relToY", nY2.ToString());
            rectNode.ParentNode.InsertBefore(lineNode, rectNode);

            lineNode = this.Document.CreateElement("line");
            lineNode.SetAttribute("relFromX", nX2.ToString());
            lineNode.SetAttribute("relFromY", nY2.ToString());
            lineNode.SetAttribute("relToX", nX1.ToString());
            lineNode.SetAttribute("relToY", nY2.ToString());
            rectNode.ParentNode.InsertBefore(lineNode, rectNode);

            lineNode = this.Document.CreateElement("line");
            lineNode.SetAttribute("relFromX", nX1.ToString());
            lineNode.SetAttribute("relFromY", nY2.ToString());
            lineNode.SetAttribute("relToX", nX1.ToString());
            lineNode.SetAttribute("relToY", nY1.ToString());
            rectNode.ParentNode.InsertBefore(lineNode, rectNode);

            rectNode.ParentNode.RemoveChild(rectNode);
        }

        private void TransformTableHeader(XmlNode tableNode)
        {
            XmlNode columnsRoot = tableNode.SelectSingleNode("columns");
            XmlNodeList columnNodes = tableNode.SelectNodes("columns/column");

            int tableLineHeight = tableNode.GetAttribute<int>("lineheight", DefaultLineHeight);

            int nHeaderLineHeight = tableLineHeight;
            XmlNode headerLineHeightNode = columnsRoot.Attributes.GetNamedItem("lineheight");
            if (headerLineHeightNode != null)
            {
                nHeaderLineHeight = Convert.ToInt32(headerLineHeightNode.Value);
            }

            int xPosition = 0;
            foreach (XmlNode columnNode in columnNodes)
            {
                XmlNode width = columnNode.Attributes.GetNamedItem("width");

                XmlNode textNode = this.Document.CreateElement("text");
                textNode.InnerText = columnNode.InnerText;

                XmlNode widthNode = columnNode.Attributes.GetNamedItem("width");
                int colmnWidth = Convert.ToInt32(widthNode.Value);
                int xAdoption = 0;
                var align = columnNode.Attributes.GetNamedItem("align");
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

                textNode.SetAttribute("relX", (xPosition + xAdoption).ToString());

                tableNode.ParentNode.InsertBefore(textNode, tableNode);

                this.CreateFrame(columnNode, tableNode, xPosition, 0, xPosition + Convert.ToInt32(width.Value), nHeaderLineHeight);
                xPosition += Convert.ToInt32(width.Value);
            }

            this.CreateFrame(columnsRoot, tableNode, 0, 0, xPosition, nHeaderLineHeight);

            XmlNode moveNode = this.Document.CreateElement("move");
            moveNode.SetAttribute("relY", nHeaderLineHeight.ToString());
            tableNode.ParentNode.InsertBefore(moveNode, tableNode);
            this.cursorY += nHeaderLineHeight;
        }

        private void TransformTable(XmlNode tableNode)
        {
            int tableLineHeight = tableNode.GetAttribute<int>("lineheight", DefaultLineHeight);

            XmlNode columnsRoot = tableNode.SelectSingleNode("columns");
            XmlNodeList columnNodes = tableNode.SelectNodes("columns/column");
            XmlNode dataRoot = tableNode.SelectSingleNode("data");
            XmlNodeList dataNodes = tableNode.SelectNodes("data/tr");

            // if table can not be started on page - create new one
            if ((this.cursorY + tableLineHeight * 2) > (this.DocumentHeight - this.documentBottomMargin))
            {
                XmlNode newPage = this.Document.CreateElement("newpage");
                tableNode.ParentNode.InsertBefore(newPage, tableNode);
                this.cursorY = this.documentTopMargin;
            }

            this.TransformTableHeader(tableNode);

            foreach (XmlNode dataNode in dataNodes)
            {
                if ((this.cursorY + tableLineHeight) > (this.DocumentHeight - this.documentBottomMargin))
                {
                    XmlNode newPage = this.Document.CreateElement("newpage");
                    tableNode.ParentNode.InsertBefore(newPage, tableNode);
                    this.cursorY = this.documentTopMargin;
                    this.TransformTableHeader(tableNode);
                }

                XmlNodeList rowNodes = dataNode.SelectNodes("td");
                int nInnerLineCount = 1;
                for (int i = 0; i < rowNodes.Count; i++)
                {
                    XmlNode rowNode = rowNodes[i];
                    string strText = rowNode.InnerText;
                    nInnerLineCount += strText.Length / 40;
                }

                int nLineHeight = tableLineHeight * nInnerLineCount;
                XmlNode lineHeightNode = dataNode.Attributes.GetNamedItem("lineheight");
                if (lineHeightNode != null)
                {
                    nLineHeight = Convert.ToInt32(lineHeightNode.Value);
                }

                int xPosition = 0;
                for (int i = 0; i < rowNodes.Count; i++)
                {
                    XmlNode columnNode = columnNodes[i];
                    XmlNode rowNode = rowNodes[i];
                    XmlNode textNode = this.Document.CreateElement("text");
                    string strText = rowNode.InnerText;

                    // line break
                    for (int j = 40; j < strText.Length; j += 41)
                    {
                        strText = strText.Insert(j, "\n");
                    }

                    textNode.InnerText = strText;
                    XmlNode widthNode = columnNode.Attributes.GetNamedItem("width");
                    int colmnWidth = Convert.ToInt32(widthNode.Value);
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

                    textNode.SetAttribute("relX", (xPosition + xAdoption).ToString());
                    tableNode.ParentNode.InsertBefore(textNode, tableNode);
                    this.CreateFrame(columnNode, textNode, xPosition, 0, xPosition + colmnWidth, nLineHeight);
                    xPosition += colmnWidth;
                }

                this.CreateFrame(dataNode, tableNode, 0, 0, xPosition, nLineHeight);

                XmlNode moveNode = this.Document.CreateElement("move");
                moveNode.SetAttribute("relY", nLineHeight.ToString());
                tableNode.ParentNode.InsertBefore(moveNode, tableNode);
                this.cursorY += nLineHeight;
            }

            tableNode.ParentNode.RemoveChild(tableNode);
        }

        private void BeginPrint(object sender, PrintEventArgs e)
        {
            this.currentNode = this.Document.DocumentElement.FirstChild;
            this.cursorX = this.documentLeftMargin;
            this.cursorY = this.documentTopMargin;
            this.penStack.Clear();
            this.penStack.Push(new Pen(Color.Black));
            this.solidBrushStack.Clear();
            this.solidBrushStack.Push(new SolidBrush(Color.Black));
            this.fontStack.Clear();
            this.fontStack.Push(new Font("Arial", 10));
        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            this.cursorY = this.documentTopMargin;
            this.PrintNodes(sender, e);
        }

        private XmlNode CreateLineNode(int nX1, int nY1, int nX2, int nY2)
        {
            XmlNode newNode = this.Document.CreateElement("line");
            newNode.SetAttribute("relFromX", nX1.ToString());
            newNode.SetAttribute("relFromY", nY1.ToString());
            newNode.SetAttribute("relToX", nX2.ToString());
            newNode.SetAttribute("relToY", nY2.ToString());
            return newNode;
        }

        private void CreateFrame(XmlNode referenceNode, XmlNode positionNode, int nX1, int nY1, int nX2, int nY2)
        {
            XmlNode attr = referenceNode.Attributes.GetNamedItem("leftline");
            if (attr != null && attr.Value == "1")
            {
                XmlNode leftline = this.CreateLineNode(nX1, nY1, nX1, nY2);
                positionNode.ParentNode.InsertBefore(leftline, positionNode);
            }

            attr = referenceNode.Attributes.GetNamedItem("rightline");
            if (attr != null && attr.Value == "1")
            {
                XmlNode rightline = this.CreateLineNode(nX2, nY1, nX2, nY2);
                positionNode.ParentNode.InsertBefore(rightline, positionNode);
            }

            attr = referenceNode.Attributes.GetNamedItem("topline");
            if (attr != null && attr.Value == "1")
            {
                XmlNode topline = this.CreateLineNode(nX1, nY1, nX2, nY1);
                positionNode.ParentNode.InsertBefore(topline, positionNode);
            }

            attr = referenceNode.Attributes.GetNamedItem("bottomline");
            if (attr != null && attr.Value == "1")
            {
                XmlNode bottomline = this.CreateLineNode(nX1, nY2, nX2, nY2);
                positionNode.ParentNode.InsertBefore(bottomline, positionNode);
            }
        }

        private void PrintNodes(object sender, PrintPageEventArgs e)
        {
            while (this.currentNode != null)
            {
                if (e.HasMorePages)
                {
                    return;
                }

                Pen drawPen = this.penStack.Peek();
                SolidBrush drawBrush = this.solidBrushStack.Peek();
                Font drawFont = this.fontStack.Peek();

                XmlNode drawNode = this.currentNode;
                this.currentNode = this.currentNode.NextSibling;

                if (drawNode.Name == "move")
                {
                    XmlNode nodeAbsX = drawNode.Attributes.GetNamedItem("absX");
                    XmlNode nodeAbsY = drawNode.Attributes.GetNamedItem("absY");
                    XmlNode nodeRelX = drawNode.Attributes.GetNamedItem("relX");
                    XmlNode nodeRelY = drawNode.Attributes.GetNamedItem("relY");
                    if (nodeAbsX != null)
                    {
                        this.cursorX = this.documentLeftMargin + Convert.ToInt32(nodeAbsX.Value);
                    }

                    if (nodeAbsY != null)
                    {
                        this.cursorY = this.documentTopMargin + Convert.ToInt32(nodeAbsY.Value);
                    }

                    if (nodeRelX != null)
                    {
                        this.cursorX += Convert.ToInt32(nodeRelX.Value);
                    }

                    if (nodeRelY != null)
                    {
                        this.cursorY += Convert.ToInt32(nodeRelY.Value);
                    }
                }
                else if (drawNode.Name == "text")
                {
                    XmlNode nodeAbsX = drawNode.Attributes.GetNamedItem("absX");
                    XmlNode nodeAbsY = drawNode.Attributes.GetNamedItem("absY");
                    XmlNode nodeRelX = drawNode.Attributes.GetNamedItem("relX");
                    XmlNode nodeRelY = drawNode.Attributes.GetNamedItem("relY");
                    XmlNode nodeAlign = drawNode.Attributes.GetNamedItem("align");
                    int nX = this.cursorX;
                    int nY = this.cursorY;
                    if (nodeAbsX != null)
                    {
                        nX = this.documentLeftMargin + Convert.ToInt32(nodeAbsX.Value);
                    }

                    if (nodeAbsY != null)
                    {
                        nY = this.documentTopMargin + Convert.ToInt32(nodeAbsY.Value);
                    }

                    if (nodeRelX != null)
                    {
                        nX += Convert.ToInt32(nodeRelX.Value);
                    }

                    if (nodeRelY != null)
                    {
                        nY += Convert.ToInt32(nodeRelY.Value);
                    }

                    string strText = drawNode.InnerText;
                    var format = new StringFormat();
                    switch (nodeAlign?.Value)
                    {
                    case "center":
                        format.Alignment = StringAlignment.Center;
                        break;
                    case "right":
                        format.Alignment = StringAlignment.Far;
                        break;
                    default:
                        format.Alignment = StringAlignment.Near;
                        break;
                    }

                    e.Graphics.DrawString(strText, drawFont, drawBrush, nX * this.printFactor, nY * this.printFactor, format);
                }
                else if (drawNode.Name == "line")
                {
                    XmlNode nodeAbsFromX = drawNode.Attributes.GetNamedItem("absFromX");
                    XmlNode nodeAbsFromY = drawNode.Attributes.GetNamedItem("absFromY");
                    XmlNode nodeRelFromX = drawNode.Attributes.GetNamedItem("relFromX");
                    XmlNode nodeRelFromY = drawNode.Attributes.GetNamedItem("relFromY");
                    XmlNode nodeAbsToX = drawNode.Attributes.GetNamedItem("absToX");
                    XmlNode nodeAbsToY = drawNode.Attributes.GetNamedItem("absToY");
                    XmlNode nodeRelToX = drawNode.Attributes.GetNamedItem("relToX");
                    XmlNode nodeRelToY = drawNode.Attributes.GetNamedItem("relToY");
                    float nX1 = this.cursorX;
                    float nY1 = this.cursorY;
                    float nX2 = this.cursorX;
                    float nY2 = this.cursorY;
                    if (nodeAbsFromX != null)
                    {
                        nX1 = this.documentLeftMargin + Convert.ToSingle(nodeAbsFromX.Value);
                    }

                    if (nodeAbsFromY != null)
                    {
                        nY1 = this.documentTopMargin + Convert.ToSingle(nodeAbsFromY.Value);
                    }

                    if (nodeRelFromX != null)
                    {
                        nX1 += Convert.ToSingle(nodeRelFromX.Value);
                    }

                    if (nodeRelFromY != null)
                    {
                        nY1 += Convert.ToSingle(nodeRelFromY.Value);
                    }

                    if (nodeAbsToX != null)
                    {
                        nX2 = this.documentLeftMargin + Convert.ToSingle(nodeAbsToX.Value);
                    }

                    if (nodeAbsToY != null)
                    {
                        nY2 = this.documentTopMargin + Convert.ToSingle(nodeAbsToY.Value);
                    }

                    if (nodeRelToX != null)
                    {
                        nX2 += Convert.ToSingle(nodeRelToX.Value);
                    }

                    if (nodeRelToY != null)
                    {
                        nY2 += Convert.ToSingle(nodeRelToY.Value);
                    }

                    e.Graphics.DrawLine(drawPen, nX1 * this.printFactor, nY1 * this.printFactor, nX2 * this.printFactor, nY2 * this.printFactor);
                }
                else if (drawNode.Name == "circle")
                {
                    XmlNode nodeAbsX = drawNode.Attributes.GetNamedItem("absX");
                    XmlNode nodeAbsY = drawNode.Attributes.GetNamedItem("absY");
                    XmlNode nodeRelX = drawNode.Attributes.GetNamedItem("relX");
                    XmlNode nodeRelY = drawNode.Attributes.GetNamedItem("relY");
                    XmlNode nodeRadX = drawNode.Attributes.GetNamedItem("radX");
                    XmlNode nodeRadY = drawNode.Attributes.GetNamedItem("radY");
                    float nX = this.cursorX;
                    float nY = this.cursorY;
                    if (nodeAbsX != null)
                    {
                        nX = this.documentLeftMargin + Convert.ToSingle(nodeAbsX.Value);
                    }

                    if (nodeAbsY != null)
                    {
                        nY = this.documentTopMargin + Convert.ToSingle(nodeAbsY.Value);
                    }

                    if (nodeRelX != null)
                    {
                        nX += Convert.ToSingle(nodeRelX.Value);
                    }

                    if (nodeRelY != null)
                    {
                        nY += Convert.ToSingle(nodeRelY.Value);
                    }

                    float nRadX = Convert.ToSingle(nodeRadX.Value);
                    float nRadY = Convert.ToSingle(nodeRadY.Value);
                    nX -= nRadX;
                    nY -= nRadY;
                    nRadX *= 2;
                    nRadY *= 2;
                    e.Graphics.DrawEllipse(drawPen, nX * this.printFactor, nY * this.printFactor, nRadX * this.printFactor, nRadY * this.printFactor);
                }
                else if (drawNode.Name == "font")
                {
                    XmlNode nodeName = drawNode.Attributes.GetNamedItem("name");
                    XmlNode nodeSize = drawNode.Attributes.GetNamedItem("size");
                    XmlNode nodeBold = drawNode.Attributes.GetNamedItem("bold");
                    string strFontName = drawFont.Name;
                    float nFontSize = drawFont.SizeInPoints;
                    FontStyle nFontStyle = drawFont.Style;
                    if (nodeName != null)
                    {
                        strFontName = nodeName.Value;
                    }

                    if (nodeSize != null)
                    {
                        nFontSize = Convert.ToSingle(nodeSize.Value);
                    }

                    if (nodeBold != null)
                    {
                        if (nodeBold.Value == "1")
                        {
                            nFontStyle |= FontStyle.Bold;
                        }
                        else
                        {
                            nFontStyle &= ~FontStyle.Bold;
                        }
                    }

                    var newFont = new Font(strFontName, nFontSize, nFontStyle);
                    if (drawNode.ChildNodes.Count > 0)
                    {
                        this.fontStack.Push(newFont);
                        this.currentNode = drawNode.FirstChild;
                        this.PrintNodes(sender, e);
                        this.fontStack.Pop();
                    }
                    else
                    {
                        this.fontStack.Pop();
                        this.fontStack.Push(newFont);
                    }
                }
                else if (drawNode.Name == "color")
                {
                    XmlNode nodeRGB = drawNode.Attributes.GetNamedItem("rgb");
                    XmlNode nodeName = drawNode.Attributes.GetNamedItem("name");
                    var newPen = (Pen)drawPen.Clone();
                    var newBrush = (SolidBrush)drawBrush.Clone();
                    if (nodeName != null)
                    {
                        newPen.Color = Color.FromName(nodeName.Value);
                        newBrush.Color = Color.FromName(nodeName.Value);
                    }
                    if (drawNode.ChildNodes.Count > 0)
                    {
                        this.penStack.Push(newPen);
                        this.solidBrushStack.Push(newBrush);
                        this.currentNode = drawNode.FirstChild;
                        this.PrintNodes(sender, e);
                        this.penStack.Pop();
                        this.solidBrushStack.Pop();
                    }
                    else
                    {
                        this.penStack.Pop();
                        this.penStack.Push(newPen);
                        this.solidBrushStack.Pop();
                        this.solidBrushStack.Push(newBrush);
                    }
                }
                else if (drawNode.Name == "newpage")
                {
                    e.HasMorePages = true;
                    return;
                }

                if (this.currentNode == null)
                {
                    if (drawNode.ParentNode != null && drawNode.ParentNode.Name != "xEport")
                    {
                        this.currentNode = drawNode.ParentNode.NextSibling;
                    }

                    return;
                }
            }
        }
    }
}