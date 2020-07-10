// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using CsvHelper.Configuration;
    using FluentAssertions;
    using lg2de.SimpleAccounting.Infrastructure;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Presentation;
    using lg2de.SimpleAccounting.UnitTests.Presentation;
    using Xunit;

    public class ImportFileLoaderTests
    {
        [Fact]
        public void ImportBookings_SampleInput_DataImported()
        {
            var project = Samples.SampleProject;
            var accounts = project.AllAccounts.ToList();
            var bankAccount = accounts.Single(x => x.Name == "Bank account");
            bankAccount.ImportMapping.Patterns = new List<AccountDefinitionImportMappingPattern>
            {
                new AccountDefinitionImportMappingPattern { Expression = "Text1", AccountID = 600 }
            };
            bankAccount.ImportMapping.Columns.Single(x => x.Source == "Text").IgnorePattern = "ignore.*this";
            var sut = new ImportFileLoader("dummy", new CultureInfo("en-us"), accounts, bankAccount.ImportMapping);

            var input = @"
Date,Name,Text,Value
2000-01-15,Shopping Mall,Shoes,-50.00
2000-12-01,Name1,Text1,12.34
2000-12-31,Name2,Text2a ignore only this Text2b,-42.42";
            List<ImportEntryViewModel> result;
            using (var inputStream = new StringReader(input))
            {
                result = sut.ImportBookings(inputStream, new CsvConfiguration(new CultureInfo("en-us"))).ToList();
            }

            result.Should().BeEquivalentTo(
                new { Date = new DateTime(2000, 1, 15), Name = "Shopping Mall", Text = "Shoes", Value = -50 },
                new
                {
                    Date = new DateTime(2000, 12, 1),
                    Name = "Name1",
                    Text = "Text1",
                    Value = 12.34,
                    RemoteAccount = new { ID = 600 } // because of mapping pattern
                },
                new { Date = new DateTime(2000, 12, 31), Name = "Name2", Text = "Text2a Text2b", Value = -42.42 });
        }

    }
}
