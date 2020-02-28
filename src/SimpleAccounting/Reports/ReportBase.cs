// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    using System;
    using System.Globalization;
    using System.Xml;
    using System.Xml.Linq;
    using lg2de.SimpleAccounting.Model;

    internal abstract class ReportBase
    {
        private readonly AccountingDataSetup setup;
        private readonly CultureInfo culture;

        protected ReportBase(string resourceName, AccountingDataSetup setup, CultureInfo culture)
        {
            this.setup = setup;
            this.culture = culture;

            this.Printer.LoadDocument(resourceName);
        }

        protected XmlPrinter Printer { get; } = new XmlPrinter();

        internal XDocument Document => XDocument.Parse(this.Printer.Document.OuterXml);

        public void ShowPreview(string documentName)
        {
            this.Printer.PrintDocument(documentName);
        }

        protected void PrepareDocument()
        {
            XmlDocument doc = this.Printer.Document;

            XmlNode firmNode = doc.SelectSingleNode("//text[@ID=\"firm\"]");
            firmNode.InnerText = this.setup.Name;

            var dateNode = doc.SelectSingleNode("//text[@ID=\"date\"]");
            dateNode.InnerText = this.setup.Location + ", " + DateTime.Now.ToString("D", this.culture);
        }
    }
}
