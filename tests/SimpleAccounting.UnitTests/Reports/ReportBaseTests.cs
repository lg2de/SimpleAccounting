// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Reports
{
    using System;
    using System.Linq;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Reports;
    using lg2de.SimpleAccounting.UnitTests.Presentation;
    using NSubstitute;
    using Xunit;

    public class ReportBaseTests
    {
        private class TestReport : ReportBase
        {
            public TestReport(AccountingDataJournal yearData)
                : base(AnnualBalanceReport.ResourceName, new AccountingDataSetup(), yearData)
            {
                this.Printer = this.TestingPrinter = Substitute.For<IXmlPrinter>();
            }

            public IXmlPrinter TestingPrinter { get; }

            public void SetPrintingDate(DateTime dateTime)
            {
                this.PrintingDate = dateTime;
            }
        }

        [Fact]
        public void ShowPreview_DocumentName_PrintingNameCorrect()
        {
            var sut = new TestReport(
                Samples.SampleProject.Journal.Last());
            sut.SetPrintingDate(new DateTime(2020, 2, 29));

            sut.ShowPreview("DocumentName");

            sut.TestingPrinter.Received(1).PrintDocument("2020-02-29 DocumentName 2020");
        }
    }
}
