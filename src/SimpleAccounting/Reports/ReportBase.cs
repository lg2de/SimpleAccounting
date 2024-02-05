// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports;

using System;
using System.Globalization;
using System.Linq;
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

    private readonly IXmlPrinter printer;
    private readonly AccountingDataSetup setup;

    protected ReportBase(IXmlPrinter printer, string resourceName, IProjectData projectData)
    {
        this.printer = printer;
        this.setup = projectData.Storage.Setup;
        this.YearData = projectData.CurrentYear;

        this.printer.LoadDocument(resourceName);
    }

    protected AccountingDataJournal YearData { get; }

    protected DateTime PrintingDate { get; set; } = DateTime.Now;

    protected XmlDocument PrintDocument => this.printer.Document;

    internal XDocument DocumentForTests => XDocument.Parse(this.printer.Document.OuterXml);

    public void ShowPreview(string documentName)
    {
        this.printer.PrintDocument($"{this.PrintingDate:yyyy-MM-dd} {documentName} {this.YearData.Year}");
    }

    protected void PreparePrintDocument(DateTime printDate)
    {
        this.UpdatePlaceholder("Organization", this.setup.Name);
        this.UpdatePlaceholder("YearName", this.YearData.Year);
        string startDate = this.YearData.DateStart.ToDateTime().ToString("d", CultureInfo.CurrentCulture);
        string endDate = this.YearData.DateEnd.ToDateTime().ToString("d", CultureInfo.CurrentCulture);
        this.UpdatePlaceholder("TimeRange", $"{startDate} - {endDate}");
        this.UpdatePlaceholder(
            "CurrentDate", this.setup.Location + ", " + printDate.ToString("D", CultureInfo.CurrentCulture));
    }

    private void UpdatePlaceholder(string name, string value)
    {
        string placeholder = $"#{name}#";
        var elements = this.PrintDocument.SelectNodes($"//*[contains(text(),'{placeholder}')]")!;
        foreach (var element in elements.OfType<XmlElement>())
        {
            element.InnerText = element.InnerText.Replace(
                placeholder, value, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
