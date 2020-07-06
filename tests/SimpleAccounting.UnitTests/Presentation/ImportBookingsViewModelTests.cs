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
        public void SelectedAccountNumber_BankAccountSelected_ExistingBookingsSetUp()
        {
            AccountingData project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            var accounts = project.AllAccounts.ToList();
            var sut = new ImportBookingsViewModel(
                null,
                null,
                project.Journal.Last(),
                accounts,
                0);

            sut.SelectedAccountNumber = 100;

            sut.ImportDataFiltered.Should().BeEmpty("start date should be set after last booking");
            sut.ExistingData.Should().BeEquivalentTo(
                new
                {
                    Date = new DateTime(2020, 1, 1),
                    Identifier = 1,
                    Name = "<bereits gebucht>",
                    Text = "Open 1",
                    Value = 1000,
                    RemoteAccount = new { ID = 990 }
                },
                new
                {
                    Date = new DateTime(2020, 1, 28),
                    Identifier = 3,
                    Name = "<bereits gebucht>",
                    Text = "Salary",
                    Value = 200,
                    RemoteAccount = (AccountDefinition)null
                },
                new
                {
                    Date = new DateTime(2020, 1, 29),
                    Identifier = 4,
                    Name = "<bereits gebucht>",
                    Text = "Credit rate",
                    Value = -400,
                    RemoteAccount = new { ID = 5000 }
                },
                new
                {
                    Date = new DateTime(2020, 2, 1),
                    Identifier = 5,
                    Name = "<bereits gebucht>",
                    Text = "Shoes",
                    Value = -50,
                    RemoteAccount = (AccountDefinition)null
                },
                new
                {
                    Date = new DateTime(2020, 2, 5),
                    Identifier = 6,
                    Name = "<bereits gebucht>",
                    Text = "Rent to friend",
                    Value = -99,
                    RemoteAccount = new { ID = 6000 }
                });
        }

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
                accounts,
                0);

            sut.ImportAccounts.Should().BeEquivalentTo(new { Name = "Bank account" });
        }

        [Fact]
        public void ImportBookings_SampleInput_DataImported()
        {
            AccountingData project = Samples.SampleProject;
            var accounts = project.AllAccounts.ToList();
            var dataJournal = project.Journal.First();
            dataJournal.Booking.Add(
                new AccountingDataJournalBooking
                {
                    ID = 1,
                    Date = 20000115,
                    Credit = new List<BookingValue>
                    {
                        new BookingValue { Account = 100, Text = "Shopping Mall - Shoes", Value = 5000 }
                    },
                    Debit = new List<BookingValue>
                    {
                        new BookingValue { Account = 600, Text = "Shopping Mall - Shoes", Value = 5000 },
                    }
                });
            var bankAccount = accounts.Single(x => x.Name == "Bank account");
            bankAccount.ImportMapping.Patterns = new List<AccountDefinitionImportMappingPattern>
            {
                new AccountDefinitionImportMappingPattern { Expression = "Text1", AccountID = 600 }
            };
            var sut = new ImportBookingsViewModel(
                null,
                null,
                dataJournal,
                accounts,
                0) { SelectedAccount = bankAccount };
            sut.SelectedAccountNumber = bankAccount.ID;

            var input = @"
Date;Name;Text;Value
1999-12-31;NameIgnore;TextIgnore;12.34
2000-01-15;Shopping Mall;Shoes;-50.00
2000-12-01;Name1;Text1;12.34
2000-12-31;Name2;Text2;-42.42
2001-01-01;Name3;Text3;99.99";
            using (var inputStream = new StringReader(input))
            {
                sut.ImportBookings(
                    inputStream,
                    new Configuration { Delimiter = ";", CultureInfo = new CultureInfo("en-us") });
            }

            sut.LoadedData.Should().NotContain(x => x.Name == "Shopping Mall", "entry is already imported");
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
        public void BookAllCommand_EntryNotMapped_CannotExecute()
        {
            AccountingData project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            var accounts = project.AllAccounts.ToList();
            var sut = new ImportBookingsViewModel(
                null,
                null,
                project.Journal.Last(),
                accounts,
                0);
            sut.LoadedData.Add(
                new ImportEntryViewModel(accounts) { RemoteAccount = null, IsSkip = false, IsExisting = false });

            sut.BookAllCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void BookAllCommand_EntryMapped_CanExecute()
        {
            AccountingData project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            var accounts = project.AllAccounts.ToList();
            var sut = new ImportBookingsViewModel(
                null,
                null,
                project.Journal.Last(),
                accounts,
                0);
            sut.LoadedData.Add(
                new ImportEntryViewModel(accounts)
                {
                    RemoteAccount = accounts.First(), IsSkip = false, IsExisting = false
                });

            sut.BookAllCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void BookAllCommand_EntrySkipped_CanExecute()
        {
            AccountingData project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            var accounts = project.AllAccounts.ToList();
            var sut = new ImportBookingsViewModel(
                null,
                null,
                project.Journal.Last(),
                accounts,
                0);
            sut.LoadedData.Add(
                new ImportEntryViewModel(accounts) { RemoteAccount = null, IsSkip = true, IsExisting = false });

            sut.BookAllCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void BookAllCommand_EntryExisting_CannotExecute()
        {
            AccountingData project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            var accounts = project.AllAccounts.ToList();
            var sut = new ImportBookingsViewModel(
                null,
                null,
                project.Journal.Last(),
                accounts,
                0);
            sut.LoadedData.Add(
                new ImportEntryViewModel(accounts) { RemoteAccount = null, IsSkip = false, IsExisting = true });

            sut.BookAllCommand.CanExecute(null).Should().BeTrue();
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
                accounts,
                0) { SelectedAccount = accounts.Single(x => x.Name == "Bank account") };
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
