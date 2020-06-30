// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using CsvHelper.Configuration;
    using FluentAssertions;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Presentation;
    using Xunit;

    public class ImportBookingsViewModelTests
    {
        [Fact]
        public void ImportBookings_SampleData_AccountsFiltered()
        {
            AccountingData project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            var accounts = project.AllAccounts.ToList();
            var sut = new ImportBookingsViewModel(
                null,
                null,
                project.Journal.Last(),
                accounts);

            sut.ImportAccounts.Should().BeEquivalentTo(new { Name = "Bank account" });
        }

        [Fact]
        public void ImportBookings_SampleInput_DataImported()
        {
            AccountingData project = Samples.SampleProject;
            var accounts = project.AllAccounts.ToList();
            var sut = new ImportBookingsViewModel(
                null,
                null,
                project.Journal.First(),
                accounts)
            {
                SelectedAccount = accounts.Single(x => x.Name == "Bank account")
            };
            sut.SelectedAccount.ImportMapping.Patterns = new List<AccountDefinitionImportMappingPattern>
            {
                new AccountDefinitionImportMappingPattern { Expression = "Text1", AccountID = 600 }
            };

            var input = @"
Date;Name;Text;Value
1999-12-31;NameIgnore;TextIgnore;12.34
2000-12-01;Name1;Text1;12.34
2000-12-31;Name2;Text2;-42.42
2001-01-01;Name3;Text3;99.99";
            using (var inputStream = new StringReader(input))
            {
                sut.ImportBookings(
                    inputStream,
                    new Configuration { Delimiter = ";", CultureInfo = new CultureInfo("en-us") });
            }

            sut.LoadedData.Should().BeEquivalentTo(
                new
                {
                    Date = new DateTime(2000, 12, 1),
                    Name = "Name1",
                    Text = "Text1",
                    Value = 12.34,
                    RemoteAccount = new { ID = 600 }
                },
                new { Date = new DateTime(2000, 12, 31), Name = "Name2", Text = "Text2", Value = -42.42 });
        }

        [Fact]
        public void ProcessData_SampleData_DataConverted()
        {
            var parent = new ShellViewModel(null, null, null, null, null, null);
            var project = Samples.SampleProject;
            parent.LoadProjectData(project);
            var accounts = project.AllAccounts.ToList();
            var sut = new ImportBookingsViewModel(
                null,
                parent,
                project.Journal.Last(),
                accounts) { SelectedAccount = accounts.Single(x => x.Name == "Bank account") };
            sut.SelectedAccountNumber = sut.SelectedAccount.ID;
            var remoteAccount = accounts.Single(x => x.ID == 600);
            int year = DateTime.Today.Year;
            sut.LoadedData.Add(
                new ImportEntryViewModel(accounts)
                {
                    Date = new DateTime(year, 1, 1),
                    Identifier = 101,
                    Name = "Name",
                    Text = "Text",
                    Value = 1,
                    RemoteAccount = remoteAccount
                });
            sut.LoadedData.Add(
                new ImportEntryViewModel(accounts)
                {
                    Date = new DateTime(year, 1, 2),
                    Identifier = 102,
                    Text = "Text",
                    Value = 2,
                    RemoteAccount = remoteAccount
                });
            sut.LoadedData.Add(
                new ImportEntryViewModel(accounts)
                {
                    Date = new DateTime(year, 1, 3),
                    Identifier = 103,
                    Name = "Name",
                    Value = -1,
                    RemoteAccount = remoteAccount
                });
            sut.LoadedData.Add(
                new ImportEntryViewModel(accounts)
                {
                    Date = new DateTime(year, 1, 3),
                    Identifier = 104,
                    Name = "Ignore",
                    Value = -2,
                    RemoteAccount = null
                });

            sut.ProcessData();

            parent.FullJournal.Should().BeEquivalentTo(
                new
                {
                    Identifier = 101,
                    Text = "Name - Text",
                    Value = 1,
                    CreditAccount = "600 (Shoes)",
                    DebitAccount = "100 (Bank account)"
                },
                new
                {
                    Identifier = 102,
                    Text = "Text",
                    Value = 2,
                    CreditAccount = "600 (Shoes)",
                    DebitAccount = "100 (Bank account)"
                },
                new
                {
                    Identifier = 103,
                    Text = "Name",
                    Value = 1,
                    CreditAccount = "100 (Bank account)",
                    DebitAccount = "600 (Shoes)"
                });
        }
    }
}
