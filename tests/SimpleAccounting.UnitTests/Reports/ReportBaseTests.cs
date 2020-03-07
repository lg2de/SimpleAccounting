// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Reports
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Reports;
    using lg2de.SimpleAccounting.UnitTests.Presentation;
    using NSubstitute;
    using Xunit;

    public class ReportBaseTests
    {
        [Fact]
        public void ShowPreview_DocumentName_PrintingNameCorrect()
        {
            var sut = new TestReport(
                Samples.SampleProject.Journal.Last(),
                new CultureInfo("en-us"));
            sut.SetPrintingDate(new DateTime(2020, 2, 29));

            sut.ShowPreview("DocumentName");

            sut.TestingPrinter.Received(1).PrintDocument("2020-02-29 DocumentName 2020");
        }

        private class TestReport : ReportBase
        {
            public TestReport(AccountingDataJournal yearData, CultureInfo culture)
                : base(AnnualBalanceReport.ResourceName, new AccountingDataSetup(), yearData, culture)
            {
                this.Printer = this.TestingPrinter = Substitute.For<IXmlPrinter>();
            }

            public IXmlPrinter TestingPrinter { get; }

            public void SetPrintingDate(DateTime dateTime)
            {
                this.PrintingDate = dateTime;
            }
        }
    }
}
