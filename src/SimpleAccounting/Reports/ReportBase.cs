// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports
{
    using System;
    using System.Globalization;
    using System.Xml;
    using System.Xml.Linq;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Model;

    internal abstract class ReportBase
    {
        private readonly AccountingDataSetup setup;
        private readonly CultureInfo culture;

        protected ReportBase(
            string resourceName,
            AccountingDataSetup setup,
            AccountingDataJournal yearData,
            CultureInfo culture)
        {
            this.setup = setup;
            this.YearData = yearData;
            this.culture = culture;

            this.Printer.LoadDocument(resourceName);
        }

        protected XmlPrinter Printer { get; } = new XmlPrinter();

        protected AccountingDataJournal YearData { get; private set; }

        protected XmlDocument PrintDocument => this.Printer.Document;

        internal XDocument DocumentForTests => XDocument.Parse(this.Printer.Document.OuterXml);

        public void ShowPreview(string documentName)
        {
            this.Printer.PrintDocument($"{DateTime.Now:yyyy-MM-dd} {documentName} {this.YearData.Year}");
        }

        protected void PreparePrintDocument()
        {
            var textNode = this.PrintDocument.SelectSingleNode("//text[@ID=\"firm\"]");
            textNode.InnerText = this.setup.Name;

            textNode = this.PrintDocument.SelectSingleNode("//text[@ID=\"yearName\"]");
            if (textNode != null)
            {
                textNode.InnerText = this.YearData.Year;
            }

            textNode = this.PrintDocument.SelectSingleNode("//text[@ID=\"range\"]");
            if (textNode != null)
            {
                string startDate = this.YearData.DateStart.ToDateTime().ToString("d", this.culture);
                string endDate = this.YearData.DateEnd.ToDateTime().ToString("d", this.culture);
                textNode.InnerText = $"{startDate} - {endDate}";
            }

            textNode = this.PrintDocument.SelectSingleNode("//text[@ID=\"date\"]");
            textNode.InnerText = this.setup.Location + ", " + DateTime.Now.ToString("D", this.culture);
        }
    }
}
