// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Reports;

using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Reports;
using lg2de.SimpleAccounting.UnitTests.Presentation;
using NSubstitute;
using Xunit;

public class AnnualBalanceReportTests
{
    [CulturedFact(["en"])]
    public void CreateReport_SampleData_Converted()
    {
        var projectData = Samples.SampleProjectData;
        projectData.CurrentYear.Booking.AddRange(Samples.SampleBookings);
        var clock = Substitute.For<IClock>();
        var sut = new AnnualBalanceReport(new XmlPrinter(), projectData, clock);

        sut.CreateReport();

        using var _ = new AssertionScope();

        sut.DocumentForTests.XPathSelectElement("//text[@ID='balance']")!.Value.Should().Be("150.00");

        var expected = @"
<data target=""income"">
  <tr>
    <td />
    <td>00400 Salary</td>
    <td>200.00</td>
  </tr>
</data>";
        sut.DocumentForTests.XPathSelectElement("//table/data[@target='income']")
            .Should().BeEquivalentTo(XDocument.Parse(expected).Root);

        expected = @"
<data target=""expense"">
  <tr>
    <td />
    <td>00600 Shoes</td>
    <td>-50.00</td>
  </tr>
</data>";
        sut.DocumentForTests.XPathSelectElement("//table/data[@target='expense']")
            .Should().BeEquivalentTo(XDocument.Parse(expected).Root);

        expected = @"
<data target=""receivable"">
  <tr>
    <td />
    <td>06000 Friends debit</td>
    <td>99.00</td>
  </tr>
</data>";
        sut.DocumentForTests.XPathSelectElement("//table/data[@target='receivable']")
            .Should().BeEquivalentTo(XDocument.Parse(expected).Root);

        expected = @"
<data target=""liability"">
  <tr>
    <td />
    <td>05000 Bank credit</td>
    <td>-2600.00</td>
  </tr>
</data>";
        sut.DocumentForTests.XPathSelectElement("//table/data[@target='liability']")
            .Should().BeEquivalentTo(XDocument.Parse(expected).Root);

        expected = @"
 <data target=""asset"">
  <tr>
    <td />
    <td>00100 Bank account</td>
    <td>651.00</td>
  </tr>
  <tr>
    <td />
    <td>Receivables</td>
    <td>99.00</td>
  </tr>
  <tr>
    <td />
    <td>Liabilities</td>
    <td>-2600.00</td>
  </tr>
</data>";
        sut.DocumentForTests.XPathSelectElement("//table/data[@target='asset']")
            .Should().BeEquivalentTo(XDocument.Parse(expected).Root);
    }

    [CulturedFact(["de"])]
    public void CreateReport_CurrencyShown_ValuesWithCurrency()
    {
        var projectData = Samples.SampleProjectData;
        projectData.Storage.Setup.Currency = new AccountingDataSetupCurrency { Value = "€", Show = true };
        projectData.CurrentYear.Booking.AddRange(Samples.SampleBookings);
        var clock = Substitute.For<IClock>();
        var sut = new AnnualBalanceReport(new XmlPrinter(), projectData, clock);

        sut.CreateReport();

        using var _ = new AssertionScope();

        sut.DocumentForTests.XPathSelectElement("//text[@ID='balance']")!.Value.Should().Be("150,00 €");

        // The value is the last cell of the data rows.
        string[] GetValues(string target) =>
            sut.DocumentForTests.XPathSelectElements($"//table/data[@target='{target}']/tr/td[last()]")
                .Select(x => x.Value).ToArray();
        GetValues("income").Should().Equal("200,00 €");
        GetValues("expense").Should().Equal("-50,00 €");
        GetValues("receivable").Should().Equal("99,00 €");
        GetValues("liability").Should().Equal("-2.600,00 €");
        GetValues("asset").Should().Equal("651,00 €", "99,00 €", "-2.600,00 €");

        // The totals are written into the last column header of each table.
        sut.DocumentForTests.XPathSelectElements("//table/columns/column[position()=4]")
            .Select(x => x.Value).Should().Equal("200,00 €", "-50,00 €", "99,00 €", "-2.600,00 €", "-1.850,00 €");
    }
}
