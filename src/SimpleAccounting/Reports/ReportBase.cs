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

    /// <summary>
    ///     Implements the base class for the reports.
    /// </summary>
    internal class ReportBase
    {
        protected const int TitleSize = 10;
        private readonly AccountingDataSetup setup;

        protected ReportBase(string resourceName, IProjectData projectData)
        {
            this.setup = projectData.Storage.Setup;
            this.YearData = projectData.CurrentYear;

            this.Printer.LoadDocument(resourceName);
        }

        protected IXmlPrinter Printer { get; set; } = new XmlPrinter();

        protected AccountingDataJournal YearData { get; }

        protected DateTime PrintingDate { get; set; } = DateTime.Now;

        protected XmlDocument PrintDocument => this.Printer.Document;

        internal XDocument DocumentForTests => XDocument.Parse(this.Printer.Document.OuterXml);

        public void ShowPreview(string documentName)
        {
            this.Printer.PrintDocument($"{this.PrintingDate:yyyy-MM-dd} {documentName} {this.YearData.Year}");
        }

        protected void PreparePrintDocument(string title)
        {
            var textNode = this.PrintDocument.SelectSingleNode("//text[@ID=\"title\"]");
            if (textNode != null)
            {
                textNode.InnerText = title;
            }

            textNode = this.PrintDocument.SelectSingleNode("//text[@ID=\"pageTitle\"]");
            if (textNode != null)
            {
                textNode.InnerText = $"- {title} -";
            }

            textNode = this.PrintDocument.SelectSingleNode("//text[@ID=\"firm\"]");
            textNode.InnerText = this.setup?.Name;

            textNode = this.PrintDocument.SelectSingleNode("//text[@ID=\"yearName\"]");
            if (textNode != null)
            {
                textNode.InnerText = this.YearData.Year;
            }

            textNode = this.PrintDocument.SelectSingleNode("//text[@ID=\"range\"]");
            if (textNode != null)
            {
                string startDate = this.YearData.DateStart.ToDateTime().ToString("d", CultureInfo.CurrentCulture);
                string endDate = this.YearData.DateEnd.ToDateTime().ToString("d", CultureInfo.CurrentCulture);
                textNode.InnerText = $"{startDate} - {endDate}";
            }

            textNode = this.PrintDocument.SelectSingleNode("//text[@ID=\"date\"]");
            textNode.InnerText = this.setup?.Location + ", " + DateTime.Now.ToString("D", CultureInfo.CurrentCulture);
        }
    }
}
