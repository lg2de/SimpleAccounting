// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.IntegrationTests.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using FluentAssertions;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Presentation;
    using lg2de.SimpleAccounting.Properties;
    using NSubstitute;
    using Xunit;

    public class ImportBookingsViewModelTests
    {
        [Fact]
        public void ImportBookings_SampleInput_DataImportedAndFiltered()
        {
            #region project definition

            var project = new AccountingData
            {
                Accounts = new List<AccountingDataAccountGroup>
                {
                    new AccountingDataAccountGroup
                    {
                        Name = "Default",
                        Account = new List<AccountDefinition>
                        {
                            new AccountDefinition
                            {
                                ID = 100,
                                Name = "Bank account",
                                Type = AccountDefinitionType.Asset,
                                ImportMapping = new AccountDefinitionImportMapping
                                {
                                    Columns = new List<AccountDefinitionImportMappingColumn>
                                    {
                                        new AccountDefinitionImportMappingColumn
                                        {
                                            Source = "Date",
                                            Target =
                                                AccountDefinitionImportMappingColumnTarget
                                                    .Date
                                        },
                                        new AccountDefinitionImportMappingColumn
                                        {
                                            Source = "Name",
                                            Target =
                                                AccountDefinitionImportMappingColumnTarget
                                                    .Name
                                        },
                                        new AccountDefinitionImportMappingColumn
                                        {
                                            Source = "Text",
                                            Target =
                                                AccountDefinitionImportMappingColumnTarget
                                                    .Text
                                        },
                                        new AccountDefinitionImportMappingColumn
                                        {
                                            Source = "Value",
                                            Target =
                                                AccountDefinitionImportMappingColumnTarget
                                                    .Value
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                Journal = new List<AccountingDataJournal>
                {
                    new AccountingDataJournal
                    {
                        Year = "2000",
                        DateStart = 20000101,
                        DateEnd = 20001231,
                        Booking = new List<AccountingDataJournalBooking>()
                    }
                }
            };
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

            #endregion

            var dialogs = Substitute.For<IDialogs>();
            var accounts = project.AllAccounts.ToList();
            var bankAccount = accounts.Single(x => x.Name == "Bank account");
            var projectData = new ProjectData(new Settings(), null!, null!, null!, null!);
            projectData.Load(project);
            var sut = new ImportBookingsViewModel(dialogs, projectData)
            {
                SelectedAccount = bankAccount, SelectedAccountNumber = bankAccount.ID, IsForceEnglish = true
            };

            var fileName = Path.GetTempFileName();
            var stream = this.GetType().Assembly.GetManifestResourceStream(
                "lg2de.SimpleAccounting.IntegrationTests.Ressources.import.csv");
            using var reader = new StreamReader(stream!);
            var script = reader.ReadToEnd();
            File.WriteAllText(fileName, script);

            sut.LoadFromFile(fileName);

            File.Delete(fileName);

            dialogs.DidNotReceive().ShowMessageBox(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(),
                Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(),
                Arg.Any<MessageBoxOptions>());
            sut.LoadedData.Should().NotContain(x => x.Name == "Shopping Mall", "entry is already imported");
            sut.LoadedData.Should().BeEquivalentTo(
                new { Date = new DateTime(2000, 1, 10), Name = "Name1", Text = "Text1", Value = 12.34 },
                new { Date = new DateTime(2000, 12, 1), Name = "Name2", Text = "Text2", Value = 23.45 },
                new { Date = new DateTime(2000, 12, 31), Name = "Name3", Text = "Text3", Value = -42.42 });
            sut.ImportDataFiltered.Should().BeEquivalentTo(
                new
                {
                    Identifier = 2,
                    Date = new DateTime(2000, 12, 1),
                    Name = "Name2",
                    Text = "Text2",
                    Value = 23.45
                },
                new
                {
                    Identifier = 3,
                    Date = new DateTime(2000, 12, 31),
                    Name = "Name3",
                    Text = "Text3",
                    Value = -42.42
                });

            // set start date to year begin to import data skipped before
            sut.StartDate = new DateTime(2000, 1, 1);

            // note that all identifiers will be changed
            sut.ImportDataFiltered.Should().BeEquivalentTo(
                new
                {
                    Identifier = 2,
                    Date = new DateTime(2000, 1, 10),
                    Name = "Name1",
                    Text = "Text1",
                    Value = 12.34
                },
                new { Identifier = 1, Date = new DateTime(2000, 1, 15), Text = "Shopping Mall - Shoes", Value = -50 },
                new
                {
                    Identifier = 3,
                    Date = new DateTime(2000, 12, 1),
                    Name = "Name2",
                    Text = "Text2",
                    Value = 23.45
                },
                new
                {
                    Identifier = 4,
                    Date = new DateTime(2000, 12, 31),
                    Name = "Name3",
                    Text = "Text3",
                    Value = -42.42
                });
        }
    }
}
