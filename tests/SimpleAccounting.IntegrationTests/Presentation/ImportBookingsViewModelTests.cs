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
    using NSubstitute;
    using Xunit;

    public class ImportBookingsViewModelTests
    {
        [Fact]
        public void ImportBookings_SampleInput_DataImported()
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
            #endregion

            var messageBox = Substitute.For<IMessageBox>();
            var accounts = project.AllAccounts.ToList();
            var dataJournal = project.Journal.First();
            var bankAccount = accounts.Single(x => x.Name == "Bank account");
            var sut = new ImportBookingsViewModel(messageBox, null, dataJournal, accounts, 0)
            {
                SelectedAccount = bankAccount, SelectedAccountNumber = bankAccount.ID, IsForceEnglish = true
            };

            var fileName = Path.GetTempFileName();
            var stream = this.GetType().Assembly.GetManifestResourceStream(
                "lg2de.SimpleAccounting.IntegrationTests.Ressources.import.csv");
            stream?.Length.Should().Be(83);
            using var reader = new StreamReader(stream!);
            var script = reader.ReadToEnd();
            File.WriteAllText(fileName, script);
            new FileInfo(fileName).Length.Should().Be(stream.Length);

            sut.OnLoadData(fileName);

            File.Delete(fileName);

            messageBox.DidNotReceive().Show(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(),
                Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(),
                Arg.Any<MessageBoxOptions>());
            sut.LoadedData.Should().BeEquivalentTo(
                new { Date = new DateTime(2000, 12, 1), Name = "Name1", Text = "Text1", Value = 12.34 },
                new { Date = new DateTime(2000, 12, 31), Name = "Name2", Text = "Text2", Value = -42.42 });
        }
    }
}
