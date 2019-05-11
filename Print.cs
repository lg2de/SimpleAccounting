// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.Xml;

namespace lg2de.SimpleAccounting
{
    class PrintClass
    {
        XmlDocument m_Document;
        XmlNode m_CurrentNode;
        int m_nDocumentLeftMargin, m_nDocumentRightMargin, m_nDocumentTopMargin, m_nDocumentBottomMargin, m_nDocumentWidth, m_nDocumentHeight;
        float m_nDocumentScale;
        int m_nCursorX, m_nCursorY;
        Stack<Pen> m_PenStack;
        Stack<SolidBrush> m_SolidBrushStack;
        Stack<Font> m_FontStack;

        float m_nResFactor;

        public PrintClass()
        {
            m_Document = new XmlDocument();
            m_PenStack = new Stack<Pen>();
            m_SolidBrushStack = new Stack<SolidBrush>();
            m_FontStack = new Stack<Font>();
        }
        public XmlDocument Document
        {
            get { return m_Document; }
        }
        public void SetDocument(string strXML)
        {
            m_Document.LoadXml(strXML);
        }
        public void SetDocument(XmlDocument doc)
        {
            m_Document = (XmlDocument)doc.Clone();
        }
        public void LoadDocument(string strFileName)
        {
            m_Document.Load(strFileName);
        }
        public void PrintDocument(string documentName)
        {
            PrintDocument doc = new PrintDocument();
            doc.BeginPrint += new PrintEventHandler(this.BeginPrint);
            doc.PrintPage += new PrintPageEventHandler(this.PrintPage);
            PrintPreviewDialog pdlg = new PrintPreviewDialog();
            pdlg.Document = doc;
            doc.DocumentName = documentName;

            // init
            m_nResFactor = (float)( 100 / 25.4 );
            XmlNode node = m_Document.DocumentElement.Attributes.GetNamedItem("papersize");
            string strPaperSize = "A4";
            if ( node != null )
                strPaperSize = node.Value;
            bool bFound = false;
            foreach ( PaperSize item in doc.PrinterSettings.PaperSizes )
            {
                if ( !item.PaperName.StartsWith(strPaperSize, StringComparison.CurrentCultureIgnoreCase) )
                    continue;
                doc.DefaultPageSettings.PaperSize = item;
                m_nDocumentWidth = Convert.ToInt32(item.Width / m_nResFactor);
                m_nDocumentHeight = Convert.ToInt32(item.Height / m_nResFactor);
                m_nDocumentScale = 1;
                bFound = true;
                break;
            }
            if ( !bFound )
            {
                node = m_Document.DocumentElement.Attributes.GetNamedItem("width");
                if ( node != null )
                    m_nDocumentWidth = Convert.ToInt32(node.Value);
                else
                    m_nDocumentWidth = 210;
                node = m_Document.DocumentElement.Attributes.GetNamedItem("height");
                if ( node != null )
                    m_nDocumentHeight = Convert.ToInt32(node.Value);
                else
                    m_nDocumentHeight = 297;
                node = m_Document.DocumentElement.Attributes.GetNamedItem("scale");
                if ( node != null )
                    m_nDocumentScale = Convert.ToSingle(node.Value);
                else
                    m_nDocumentScale = 1;
                m_nResFactor *= m_nDocumentScale;
                doc.DefaultPageSettings.PaperSize = new PaperSize(strPaperSize, Convert.ToInt32(m_nDocumentWidth * m_nResFactor), Convert.ToInt32(m_nDocumentHeight * m_nResFactor));
            }

            doc.DefaultPageSettings.Landscape = false;
            node = m_Document.DocumentElement.Attributes.GetNamedItem("landscape");
            if ( node != null )
            {
                if ( node.Value == "1" )
                    doc.DefaultPageSettings.Landscape = true;
            }
            if ( doc.DefaultPageSettings.Landscape )
            {
                int nDummy = m_nDocumentWidth;
                m_nDocumentWidth = m_nDocumentHeight;
                m_nDocumentHeight = nDummy;
            }

            m_CurrentNode = m_Document.DocumentElement.FirstChild;
            node = m_Document.DocumentElement.Attributes.GetNamedItem("left");
            if ( node != null )
                m_nDocumentLeftMargin = Convert.ToInt32(node.Value);
            else
                m_nDocumentLeftMargin = 0;
            node = m_Document.DocumentElement.Attributes.GetNamedItem("right");
            if ( node != null )
                m_nDocumentRightMargin = Convert.ToInt32(node.Value);
            else
                m_nDocumentRightMargin = 0;
            node = m_Document.DocumentElement.Attributes.GetNamedItem("top");
            if ( node != null )
                m_nDocumentTopMargin = Convert.ToInt32(node.Value);
            else
                m_nDocumentTopMargin = 0;
            node = m_Document.DocumentElement.Attributes.GetNamedItem("bottom");
            if ( node != null )
                m_nDocumentBottomMargin = Convert.ToInt32(node.Value);
            else
                m_nDocumentBottomMargin = 0;
            m_nCursorX = m_nDocumentLeftMargin;
            m_nCursorY = m_nDocumentTopMargin;

            // transform
            TransformDocument();
            // show
            pdlg.WindowState = FormWindowState.Maximized;
            pdlg.ShowDialog();
        }

        void SetNodeAttribute(XmlNode node, string strName, string strValue)
        {
            XmlAttribute attr = node.OwnerDocument.CreateAttribute(strName);
            attr.Value = strValue;
            node.Attributes.SetNamedItem(attr);
        }

        void TransformDocument()
        {
            TransformNodes(m_Document.DocumentElement.FirstChild);
            string strText = m_Document.InnerXml;
        }
        void TransformNodes(XmlNode firstNode)
        {
            XmlNode nextNode = firstNode;
            while ( nextNode != null )
            {
                XmlNode currentNode = nextNode;
                nextNode = nextNode.NextSibling;

                if ( currentNode.Name == "move" )
                {
                    XmlNode attr = currentNode.Attributes.GetNamedItem("absX");
                    if ( attr != null )
                        m_nCursorX = m_nDocumentLeftMargin + Convert.ToInt32(attr.Value);
                    attr = currentNode.Attributes.GetNamedItem("absY");
                    if ( attr != null )
                        m_nCursorY = m_nDocumentTopMargin + Convert.ToInt32(attr.Value);
                    attr = currentNode.Attributes.GetNamedItem("relX");
                    if ( attr != null )
                        m_nCursorX += Convert.ToInt32(attr.Value);
                    attr = currentNode.Attributes.GetNamedItem("relY");
                    if ( attr != null )
                        m_nCursorY += Convert.ToInt32(attr.Value);
                }
                else if ( currentNode.Name == "rectangle" )
                    TransformRectangle(currentNode);
                else if ( currentNode.Name == "table" )
                    TransformTable(currentNode);
                else if ( currentNode.Name == "newpage" )
                    m_nCursorY = m_nDocumentTopMargin;

                TransformNodes(currentNode.FirstChild);

                if ( m_nCursorY >= ( m_nDocumentHeight - m_nDocumentBottomMargin ) )
                {
                    XmlNode newPage = m_Document.CreateElement("newpage");
                    currentNode.ParentNode.InsertBefore(newPage, currentNode);
                    m_nCursorY = m_nDocumentTopMargin;
                }
            }
        }
        void TransformRectangle(XmlNode rectNode)
        {
            float nX1 = Convert.ToSingle(rectNode.Attributes.GetNamedItem("relFromX").Value);
            float nY1 = Convert.ToSingle(rectNode.Attributes.GetNamedItem("relFromY").Value);
            float nX2 = Convert.ToSingle(rectNode.Attributes.GetNamedItem("relToX").Value);
            float nY2 = Convert.ToSingle(rectNode.Attributes.GetNamedItem("relToY").Value);

            XmlNode lineNode = m_Document.CreateElement("line");
            SetNodeAttribute(lineNode, "relFromX", nX1.ToString());
            SetNodeAttribute(lineNode, "relFromY", nY1.ToString());
            SetNodeAttribute(lineNode, "relToX", nX2.ToString());
            SetNodeAttribute(lineNode, "relToY", nY1.ToString());
            rectNode.ParentNode.InsertBefore(lineNode, rectNode);

            lineNode = m_Document.CreateElement("line");
            SetNodeAttribute(lineNode, "relFromX", nX2.ToString());
            SetNodeAttribute(lineNode, "relFromY", nY1.ToString());
            SetNodeAttribute(lineNode, "relToX", nX2.ToString());
            SetNodeAttribute(lineNode, "relToY", nY2.ToString());
            rectNode.ParentNode.InsertBefore(lineNode, rectNode);

            lineNode = m_Document.CreateElement("line");
            SetNodeAttribute(lineNode, "relFromX", nX2.ToString());
            SetNodeAttribute(lineNode, "relFromY", nY2.ToString());
            SetNodeAttribute(lineNode, "relToX", nX1.ToString());
            SetNodeAttribute(lineNode, "relToY", nY2.ToString());
            rectNode.ParentNode.InsertBefore(lineNode, rectNode);

            lineNode = m_Document.CreateElement("line");
            SetNodeAttribute(lineNode, "relFromX", nX1.ToString());
            SetNodeAttribute(lineNode, "relFromY", nY2.ToString());
            SetNodeAttribute(lineNode, "relToX", nX1.ToString());
            SetNodeAttribute(lineNode, "relToY", nY1.ToString());
            rectNode.ParentNode.InsertBefore(lineNode, rectNode);

            rectNode.ParentNode.RemoveChild(rectNode);
        }

        void TransformTableHeader(XmlNode tableNode)
        {
            XmlNode columnsRoot = tableNode.SelectSingleNode("columns");
            XmlNodeList columnNodes = tableNode.SelectNodes("columns/column");

            int nTableLineHeight = Convert.ToInt32(tableNode.Attributes.GetNamedItem("lineheight").Value);

            int nHeaderLineHeight = nTableLineHeight;
            XmlNode headerLineHeightNode = columnsRoot.Attributes.GetNamedItem("lineheight");
            if ( headerLineHeightNode != null )
                nHeaderLineHeight = Convert.ToInt32(headerLineHeightNode.Value);

            int nTableWidth = 0;
            foreach ( XmlNode columnNode in columnNodes )
            {
                XmlNode width = columnNode.Attributes.GetNamedItem("width");

                XmlNode textNode = m_Document.CreateElement("text");
                textNode.InnerText = columnNode.InnerText;

                SetNodeAttribute(textNode, "relX", nTableWidth.ToString());

                tableNode.ParentNode.InsertBefore(textNode, tableNode);

                CreateFrame(columnNode, tableNode, nTableWidth, 0, nTableWidth + Convert.ToInt32(width.Value), nHeaderLineHeight);
                nTableWidth += Convert.ToInt32(width.Value);
            }
            CreateFrame(columnsRoot, tableNode, 0, 0, nTableWidth, nHeaderLineHeight);

            XmlNode moveNode = m_Document.CreateElement("move");
            SetNodeAttribute(moveNode, "relY", nHeaderLineHeight.ToString());
            tableNode.ParentNode.InsertBefore(moveNode, tableNode);
            m_nCursorY += nHeaderLineHeight;
        }
        void TransformTable(XmlNode tableNode)
        {
            int nTableLineHeight = Convert.ToInt32(tableNode.Attributes.GetNamedItem("lineheight").Value);

            XmlNode columnsRoot = tableNode.SelectSingleNode("columns");
            XmlNodeList columnNodes = tableNode.SelectNodes("columns/column");
            XmlNode dataRoot = tableNode.SelectSingleNode("data");
            XmlNodeList dataNodes = tableNode.SelectNodes("data/tr");

            // if table can not be started on page - create new one
            if ( ( m_nCursorY + nTableLineHeight * 2 ) > ( m_nDocumentHeight - m_nDocumentBottomMargin ) )
            {
                XmlNode newPage = m_Document.CreateElement("newpage");
                tableNode.ParentNode.InsertBefore(newPage, tableNode);
                m_nCursorY = m_nDocumentTopMargin;
            }

            TransformTableHeader(tableNode);

            foreach ( XmlNode dataNode in dataNodes )
            {
                if ( ( m_nCursorY + nTableLineHeight ) > ( m_nDocumentHeight - m_nDocumentBottomMargin ) )
                {
                    XmlNode newPage = m_Document.CreateElement("newpage");
                    tableNode.ParentNode.InsertBefore(newPage, tableNode);
                    m_nCursorY = m_nDocumentTopMargin;
                    TransformTableHeader(tableNode);
                }

                XmlNode textStyleNode = dataNode.Attributes.GetNamedItem("textStyle");
                string strRowTextStyle = "";
                if ( textStyleNode != null )
                    strRowTextStyle = textStyleNode.Value = "bold";

                XmlNodeList rowNodes = dataNode.SelectNodes("td");
                int nInnerLineCount = 1;
                for ( int i = 0; i < rowNodes.Count; i++ )
                {
                    XmlNode rowNode = rowNodes[i];
                    XmlNode textNode = m_Document.CreateElement("text");
                    string strText = rowNode.InnerText;
                    nInnerLineCount += strText.Length / 40;
                }

                int nLineHeight = nTableLineHeight*nInnerLineCount;
                XmlNode lineHeightNode = dataNode.Attributes.GetNamedItem("lineheight");
                if ( lineHeightNode != null )
                    nLineHeight = Convert.ToInt32(lineHeightNode.Value);

                int nTableWidth = 0;
                for ( int i = 0; i < rowNodes.Count; i++ )
                {
                    XmlNode rowNode = rowNodes[i];
                    XmlNode textNode = m_Document.CreateElement("text");
                    string strText = rowNode.InnerText;

                    // line break
                    for (int j = 40; j < strText.Length; j += 41)
                    {
                        strText = strText.Insert(j, "\n");
                    }

                    textNode.InnerText = strText;
                    XmlNode width = columnNodes[i].Attributes.GetNamedItem("width");
                    SetNodeAttribute(textNode, "relX", nTableWidth.ToString());
                    tableNode.ParentNode.InsertBefore(textNode, tableNode);
                    int nWidth = Convert.ToInt32(width.Value);
                    CreateFrame(columnNodes[i], textNode, nTableWidth, 0, nTableWidth + nWidth, nLineHeight);
                    nTableWidth += nWidth;
                }
                CreateFrame(dataNode, tableNode, 0, 0, nTableWidth, nLineHeight);

                XmlNode moveNode = m_Document.CreateElement("move");
                SetNodeAttribute(moveNode, "relY", nLineHeight.ToString());
                tableNode.ParentNode.InsertBefore(moveNode, tableNode);
                m_nCursorY += nLineHeight;
            }

            tableNode.ParentNode.RemoveChild(tableNode);
        }

        void BeginPrint(object sender, PrintEventArgs e)
        {
            m_CurrentNode = m_Document.DocumentElement.FirstChild;
            m_nCursorX = m_nDocumentLeftMargin;
            m_nCursorY = m_nDocumentTopMargin;
            m_PenStack.Clear();
            m_PenStack.Push(new Pen(Color.Black));
            m_SolidBrushStack.Clear();
            m_SolidBrushStack.Push(new SolidBrush(Color.Black));
            m_FontStack.Clear();
            m_FontStack.Push(new Font("Arial", 10));
        }
        void PrintPage(object sender, PrintPageEventArgs e)
        {
            m_nCursorY = m_nDocumentTopMargin;
            PrintNodes(sender, e);
        }
        XmlNode CreateLineNode(int nX1, int nY1, int nX2, int nY2)
        {
            XmlNode newNode = m_Document.CreateElement("line");
            SetNodeAttribute(newNode, "relFromX", nX1.ToString());
            SetNodeAttribute(newNode, "relFromY", nY1.ToString());
            SetNodeAttribute(newNode, "relToX", nX2.ToString());
            SetNodeAttribute(newNode, "relToY", nY2.ToString());
            return newNode;
        }
        void CreateFrame(XmlNode referenceNode, XmlNode positionNode, int nX1, int nY1, int nX2, int nY2)
        {
            XmlNode attr = referenceNode.Attributes.GetNamedItem("leftline");
            if ( attr != null && attr.Value == "1" )
            {
                XmlNode leftline = CreateLineNode(nX1, nY1, nX1, nY2);
                positionNode.ParentNode.InsertBefore(leftline, positionNode);
            }
            attr = referenceNode.Attributes.GetNamedItem("rightline");
            if ( attr != null && attr.Value == "1" )
            {
                XmlNode rightline = CreateLineNode(nX2, nY1, nX2, nY2);
                positionNode.ParentNode.InsertBefore(rightline, positionNode);
            }
            attr = referenceNode.Attributes.GetNamedItem("topline");
            if ( attr != null && attr.Value == "1" )
            {
                XmlNode topline = CreateLineNode(nX1, nY1, nX2, nY1);
                positionNode.ParentNode.InsertBefore(topline, positionNode);
            }
            attr = referenceNode.Attributes.GetNamedItem("bottomline");
            if ( attr != null && attr.Value == "1" )
            {
                XmlNode bottomline = CreateLineNode(nX1, nY2, nX2, nY2);
                positionNode.ParentNode.InsertBefore(bottomline, positionNode);
            }
        }
        void PrintNodes(object sender, PrintPageEventArgs e)
        {
            while ( m_CurrentNode != null )
            {
                if ( e.HasMorePages )
                    return;

                Pen drawPen = m_PenStack.Peek();
                SolidBrush drawBrush = m_SolidBrushStack.Peek();
                Font drawFont = m_FontStack.Peek();

                XmlNode drawNode = m_CurrentNode;
                m_CurrentNode = m_CurrentNode.NextSibling;

                if ( drawNode.Name == "move" )
                {
                    XmlNode nodeAbsX = drawNode.Attributes.GetNamedItem("absX");
                    XmlNode nodeAbsY = drawNode.Attributes.GetNamedItem("absY");
                    XmlNode nodeRelX = drawNode.Attributes.GetNamedItem("relX");
                    XmlNode nodeRelY = drawNode.Attributes.GetNamedItem("relY");
                    if ( nodeAbsX != null )
                        m_nCursorX = m_nDocumentLeftMargin + Convert.ToInt32(nodeAbsX.Value);
                    if ( nodeAbsY != null )
                        m_nCursorY = m_nDocumentTopMargin + Convert.ToInt32(nodeAbsY.Value);
                    if ( nodeRelX != null )
                        m_nCursorX += Convert.ToInt32(nodeRelX.Value);
                    if ( nodeRelY != null )
                        m_nCursorY += Convert.ToInt32(nodeRelY.Value);
                }
                else if ( drawNode.Name == "text" )
                {
                    XmlNode nodeAbsX = drawNode.Attributes.GetNamedItem("absX");
                    XmlNode nodeAbsY = drawNode.Attributes.GetNamedItem("absY");
                    XmlNode nodeRelX = drawNode.Attributes.GetNamedItem("relX");
                    XmlNode nodeRelY = drawNode.Attributes.GetNamedItem("relY");
                    XmlNode nodeAlign = drawNode.Attributes.GetNamedItem("align");
                    int nX = m_nCursorX;
                    int nY = m_nCursorY;
                    if ( nodeAbsX != null )
                        nX = m_nDocumentLeftMargin + Convert.ToInt32(nodeAbsX.Value);
                    if ( nodeAbsY != null )
                        nY = m_nDocumentTopMargin + Convert.ToInt32(nodeAbsY.Value);
                    if ( nodeRelX != null )
                        nX += Convert.ToInt32(nodeRelX.Value);
                    if ( nodeRelY != null )
                        nY += Convert.ToInt32(nodeRelY.Value);

                    string strText = drawNode.InnerText;
                    StringFormat format = new StringFormat();
                    format.Alignment = StringAlignment.Near;
                    if ( nodeAlign != null )
                    {
                        if ( nodeAlign.Value == "center" )
                            format.Alignment = StringAlignment.Center;
                    }
                    e.Graphics.DrawString(strText, drawFont, drawBrush, nX * m_nResFactor, nY * m_nResFactor, format);
                }
                else if ( drawNode.Name == "line" )
                {
                    XmlNode nodeAbsFromX = drawNode.Attributes.GetNamedItem("absFromX");
                    XmlNode nodeAbsFromY = drawNode.Attributes.GetNamedItem("absFromY");
                    XmlNode nodeRelFromX = drawNode.Attributes.GetNamedItem("relFromX");
                    XmlNode nodeRelFromY = drawNode.Attributes.GetNamedItem("relFromY");
                    XmlNode nodeAbsToX = drawNode.Attributes.GetNamedItem("absToX");
                    XmlNode nodeAbsToY = drawNode.Attributes.GetNamedItem("absToY");
                    XmlNode nodeRelToX = drawNode.Attributes.GetNamedItem("relToX");
                    XmlNode nodeRelToY = drawNode.Attributes.GetNamedItem("relToY");
                    float nX1 = m_nCursorX;
                    float nY1 = m_nCursorY;
                    float nX2 = m_nCursorX;
                    float nY2 = m_nCursorY;
                    if ( nodeAbsFromX != null )
                        nX1 = m_nDocumentLeftMargin + Convert.ToSingle(nodeAbsFromX.Value);
                    if ( nodeAbsFromY != null )
                        nY1 = m_nDocumentTopMargin + Convert.ToSingle(nodeAbsFromY.Value);
                    if ( nodeRelFromX != null )
                        nX1 += Convert.ToSingle(nodeRelFromX.Value);
                    if ( nodeRelFromY != null )
                        nY1 += Convert.ToSingle(nodeRelFromY.Value);
                    if ( nodeAbsToX != null )
                        nX2 = m_nDocumentLeftMargin + Convert.ToSingle(nodeAbsToX.Value);
                    if ( nodeAbsToY != null )
                        nY2 = m_nDocumentTopMargin + Convert.ToSingle(nodeAbsToY.Value);
                    if ( nodeRelToX != null )
                        nX2 += Convert.ToSingle(nodeRelToX.Value);
                    if ( nodeRelToY != null )
                        nY2 += Convert.ToSingle(nodeRelToY.Value);

                    e.Graphics.DrawLine(drawPen, nX1 * m_nResFactor, nY1 * m_nResFactor, nX2 * m_nResFactor, nY2 * m_nResFactor);
                }
                else if ( drawNode.Name == "circle" )
                {
                    XmlNode nodeAbsX = drawNode.Attributes.GetNamedItem("absX");
                    XmlNode nodeAbsY = drawNode.Attributes.GetNamedItem("absY");
                    XmlNode nodeRelX = drawNode.Attributes.GetNamedItem("relX");
                    XmlNode nodeRelY = drawNode.Attributes.GetNamedItem("relY");
                    XmlNode nodeRadX = drawNode.Attributes.GetNamedItem("radX");
                    XmlNode nodeRadY = drawNode.Attributes.GetNamedItem("radY");
                    float nX = m_nCursorX;
                    float nY = m_nCursorY;
                    if ( nodeAbsX != null )
                        nX = m_nDocumentLeftMargin + Convert.ToSingle(nodeAbsX.Value);
                    if ( nodeAbsY != null )
                        nY = m_nDocumentTopMargin + Convert.ToSingle(nodeAbsY.Value);
                    if ( nodeRelX != null )
                        nX += Convert.ToSingle(nodeRelX.Value);
                    if ( nodeRelY != null )
                        nY += Convert.ToSingle(nodeRelY.Value);
                    float nRadX = Convert.ToSingle(nodeRadX.Value);
                    float nRadY = Convert.ToSingle(nodeRadY.Value);
                    nX -= nRadX;
                    nY -= nRadY;
                    nRadX *= 2;
                    nRadY *= 2;
                    string strText = drawNode.InnerText;
                    e.Graphics.DrawEllipse(drawPen, nX * m_nResFactor, nY * m_nResFactor, nRadX * m_nResFactor, nRadY * m_nResFactor);
                }
                else if ( drawNode.Name == "font" )
                {
                    XmlNode nodeName = drawNode.Attributes.GetNamedItem("name");
                    XmlNode nodeSize = drawNode.Attributes.GetNamedItem("size");
                    XmlNode nodeBold = drawNode.Attributes.GetNamedItem("bold");
                    string strFontName = drawFont.Name;
                    float nFontSize = drawFont.SizeInPoints;
                    FontStyle nFontStyle = drawFont.Style;
                    if ( nodeName != null )
                        strFontName = nodeName.Value;
                    if ( nodeSize != null )
                        nFontSize = Convert.ToSingle(nodeSize.Value);
                    if ( nodeBold != null )
                    {
                        if ( nodeBold.Value == "1" )
                            nFontStyle |= FontStyle.Bold;
                        else
                            nFontStyle &= ~FontStyle.Bold;
                    }
                    Font newFont = new Font(strFontName, nFontSize, nFontStyle);
                    if ( drawNode.ChildNodes.Count > 0 )
                    {
                        m_FontStack.Push(newFont);
                        m_CurrentNode = drawNode.FirstChild;
                        PrintNodes(sender, e);
                        m_FontStack.Pop();
                    }
                    else
                    {
                        m_FontStack.Pop();
                        m_FontStack.Push(newFont);
                    }
                }
                else if ( drawNode.Name == "color" )
                {
                    XmlNode nodeRGB = drawNode.Attributes.GetNamedItem("rgb");
                    XmlNode nodeName = drawNode.Attributes.GetNamedItem("name");
                    Pen newPen = (Pen)drawPen.Clone();
                    SolidBrush newBrush = (SolidBrush)drawBrush.Clone();
                    if ( nodeName != null )
                    {
                        newPen.Color = Color.FromName(nodeName.Value);
                        newBrush.Color = Color.FromName(nodeName.Value);
                    }
                    if ( drawNode.ChildNodes.Count > 0 )
                    {
                        m_PenStack.Push(newPen);
                        m_SolidBrushStack.Push(newBrush);
                        m_CurrentNode = drawNode.FirstChild;
                        PrintNodes(sender, e);
                        m_PenStack.Pop();
                        m_SolidBrushStack.Pop();
                    }
                    else
                    {
                        m_PenStack.Pop();
                        m_PenStack.Push(newPen);
                        m_SolidBrushStack.Pop();
                        m_SolidBrushStack.Push(newBrush);
                    }
                }
                else if ( drawNode.Name == "newpage" )
                {
                    e.HasMorePages = true;
                    return;
                }

                if ( m_CurrentNode == null )
                {
                    if ( drawNode.ParentNode != null && drawNode.ParentNode.Name != "xEport" )
                        m_CurrentNode = drawNode.ParentNode.NextSibling;
                    return;
                }
            }
        }
    }
}
