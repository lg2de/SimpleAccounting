// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Infrastructure;

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using lg2de.SimpleAccounting.Infrastructure;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.UnitTests.Presentation;
using Xunit;

public class ImportFileLoaderTests
{
    public static TheoryData<Encoding> GetEncodings()
    {
        return new TheoryData<Encoding>(Encoding.UTF8, Encoding.Latin1);
    }

    [Theory]
    [MemberData(nameof(GetEncodings))]
    public void Load_SampleInput_DataImported([NotNull] Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(encoding);

        var project = Samples.SampleProject;
        var accounts = project.AllAccounts.ToList();
        var bankAccount = accounts.Single(x => x.Name == "Bank account");
        bankAccount.ImportMapping.Patterns =
        [
            new AccountDefinitionImportMappingPatternsPattern
            {
                // correct expression, but wrong value 
                Expression = "Text1", AccountID = Samples.Carryforward, ValueSpecified = true, Value = 999
            },
            new AccountDefinitionImportMappingPatternsPattern { Expression = "Text1", AccountID = Samples.Shoes }
        ];
        bankAccount.ImportMapping.Columns.Single(x => x.Source == "Text").IgnorePattern = "ignore.*this";
        var input = encoding.GetBytes(
            """
            Date,Name,Text,Value
            2000-01-15,Shopping Mall,Shoes,-50.00
            2000-12-01,Name1,Text1 with German Ü,12.34
            2000-12-31,Name2,Text2a ignore only this Text2b,-42.42
            """);
        var cultureRegardingFileFormat = new CultureInfo("en-us");
        var sut = new ImportFileLoader(input, cultureRegardingFileFormat, accounts, bankAccount.ImportMapping);

        var result = sut.Load().ToList();

        result.Should().BeEquivalentTo(
            new object[]
            {
                new
                {
                    Date = new DateTime(2000, 1, 15, 0, 0, 0, DateTimeKind.Local),
                    Name = "Shopping Mall",
                    Text = "Shoes",
                    Value = -50
                },
                new
                {
                    Date = new DateTime(2000, 12, 1, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name1",
                    Text = "Text1 with German Ü",
                    Value = 12.34,
                    RemoteAccount = new { ID = 600 } // because of mapping pattern
                },
                new
                {
                    Date = new DateTime(2000, 12, 31, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name2",
                    Text = "Text2a Text2b",
                    Value = -42.42
                }
            });
    }

    [Theory]
    [InlineData("en-us", ";", ".")]
    [InlineData("de-de", ",", ",")]
    public void Load_SeparatorDifferentFromCulture_SeparatorDetected(
        string cultureName, string separator, string decimalSeparator)
    {
        var project = Samples.SampleProject;
        var accounts = project.AllAccounts.ToList();
        var bankAccount = accounts.Single(x => x.Name == "Bank account");
        var input = Encoding.UTF8.GetBytes(
            $"""
             Date{separator}Name{separator}Text{separator}Value
             2000-01-15{separator}Shopping Mall{separator}Shoes{separator}"-50{decimalSeparator}25"
             """);
        var sut = new ImportFileLoader(input, new CultureInfo(cultureName), accounts, bankAccount.ImportMapping);

        var result = sut.Load().ToList();

        result.Should().BeEquivalentTo(
        [
            new { Date = new DateTime(2000, 1, 15), Name = "Shopping Mall", Text = "Shoes", Value = -50.25 }
        ]);
    }

    [Theory]
    [InlineData("de", "15.01.2000", "-1.234,5")]
    [InlineData("de", "15/01/2000", "-1234,5")]
    [InlineData("de", "01/15/2000", "-1,234.5")]
    [InlineData("en", "15.01.2000", "-1.234,5")]
    [InlineData("fr", "01/15/2000", "-1234.5")]
    public void Load_FormatDifferentFromCulture_FormatDetected(string cultureName, string date, string value)
    {
        var project = Samples.SampleProject;
        var accounts = project.AllAccounts.ToList();
        var bankAccount = accounts.Single(x => x.Name == "Bank account");
        var input = Encoding.UTF8.GetBytes(
            $"""
             Date;Name;Text;Value
             {date};Shopping Mall;Shoes;{value}
             """);
        var sut = new ImportFileLoader(input, new CultureInfo(cultureName), accounts, bankAccount.ImportMapping);

        var result = sut.Load().ToList();

        result.Should().BeEquivalentTo([new { Date = new DateTime(2000, 1, 15), Value = -1234.5 }]);
    }

    [Fact]
    public void Load_CultureConfigured_ConfiguredCultureUsed()
    {
        var project = Samples.SampleProject;
        var accounts = project.AllAccounts.ToList();
        var bankAccount = accounts.Single(x => x.Name == "Bank account");
        bankAccount.ImportMapping.Culture = "en-US";

        // the detection would choose a culture using comma as decimal separator
        var input = Encoding.UTF8.GetBytes(
            """
            Date;Name;Text;Value
            2000-01-15;Shopping Mall;Shoes;1,5
            """);
        var sut = new ImportFileLoader(input, new CultureInfo("de"), accounts, bankAccount.ImportMapping);

        var result = sut.Load().ToList();

        result.Should().BeEquivalentTo([new { Date = new DateTime(2000, 1, 15), Value = 15.0 }]);
    }

    [Theory]
    [InlineData("", "Failed to read initial record.")]
    [InlineData("foo", "Missing or incomplete file header.")]
    public void Load_InvalidData_Throws(string input, string expectedError)
    {
        var project = Samples.SampleProject;
        var accounts = project.AllAccounts.ToList();
        var bankAccount = accounts.Single(x => x.Name == "Bank account");
        bankAccount.ImportMapping.Patterns =
        [
            new AccountDefinitionImportMappingPatternsPattern
            {
                // correct expression, but wrong value 
                Expression = "Text1", AccountID = Samples.Carryforward, ValueSpecified = true, Value = 999
            },
            new AccountDefinitionImportMappingPatternsPattern { Expression = "Text1", AccountID = Samples.Shoes }
        ];
        bankAccount.ImportMapping.Columns.Single(x => x.Source == "Text").IgnorePattern = "ignore.*this";
        var bytes = Encoding.UTF8.GetBytes(input);
        var cultureRegardingFileFormat = new CultureInfo("en-us");
        var sut = new ImportFileLoader(bytes, cultureRegardingFileFormat, accounts, bankAccount.ImportMapping);

        sut.Invoking(c => c.Load().ToArray()).Should().Throw<InvalidOperationException>().WithMessage(expectedError);
    }
}
