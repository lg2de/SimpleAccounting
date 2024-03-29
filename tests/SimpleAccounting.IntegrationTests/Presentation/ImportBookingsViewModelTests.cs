// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.IntegrationTests.Presentation;

using System;
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
    [Theory]
    [InlineData("import-utf8-bom")]
    [InlineData("import-utf8-no-bom")]
    [InlineData("import-win1252-no-bom")]
    public void ImportBookings_SampleInput_DataImportedAndFiltered(string fileName)
    {
        AccountingData project = GetProject();

        var dialogs = Substitute.For<IDialogs>();
        var clock = Substitute.For<IClock>();
        var accounts = project.AllAccounts.ToList();
        var bankAccount = accounts.Single(x => x.Name == "Bank account");
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        projectData.LoadData(project);
        var sut = new ImportBookingsViewModel(dialogs, null!, projectData)
        {
            SelectedAccount = bankAccount, SelectedAccountNumber = bankAccount.ID, IsForceEnglish = true
        };

        var stream = this.GetType().Assembly.GetManifestResourceStream(
            $"lg2de.SimpleAccounting.IntegrationTests.Ressources.{fileName}.csv");
        var bytes = new byte[stream!.Length];
        stream.Read(bytes, 0, bytes.Length).Should().Be(bytes.Length);

        sut.LoadFromBytes(bytes, "dummy");

        dialogs.DidNotReceive().ShowMessageBox(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<MessageBoxButton>(),
            Arg.Any<MessageBoxImage>(),
            Arg.Any<MessageBoxResult>(),
            Arg.Any<MessageBoxOptions>());
        sut.LoadedData.Should().NotContain(x => x.Name == "Shopping Mall", "entry is already imported");
        sut.LoadedData.Should().BeEquivalentTo(
            new[]
            {
                new
                {
                    Date = new DateTime(2000, 1, 10, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name1",
                    Text = "Text1 with German Ü and Copyright ©",
                    Value = 12.34
                },
                new
                {
                    Date = new DateTime(2000, 12, 1, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name2",
                    Text = "Text2",
                    Value = 23.45
                },
                new
                {
                    Date = new DateTime(2000, 12, 1, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name3",
                    Text = "Text3",
                    Value = 23.46
                },
                new
                {
                    Date = new DateTime(2000, 12, 31, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name4",
                    Text = "Text4",
                    Value = -42.42
                }
            }, o => o.WithStrictOrdering());
        sut.ImportDataFiltered.Should().BeEquivalentTo(
            new[]
            {
                new
                {
                    Identifier = 2,
                    Date = new DateTime(2000, 12, 1, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name2",
                    Text = "Text2",
                    Value = 23.45
                },
                new
                {
                    Identifier = 3,
                    Date = new DateTime(2000, 12, 1, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name3",
                    Text = "Text3",
                    Value = 23.46
                },
                new
                {
                    Identifier = 4,
                    Date = new DateTime(2000, 12, 31, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name4",
                    Text = "Text4",
                    Value = -42.42
                }
            }, o => o.WithStrictOrdering());

        // set start date to year begin to import data skipped before
        sut.StartDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Local);

        // note that all identifiers will be changed
        sut.ImportDataFiltered.Should().BeEquivalentTo(
            new object[]
            {
                new
                {
                    Identifier = 2,
                    Date = new DateTime(2000, 1, 10, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name1",
                    Text = "Text1 with German Ü and Copyright ©",
                    Value = 12.34
                },
                new
                {
                    Identifier = 1,
                    Date = new DateTime(2000, 1, 15, 0, 0, 0, DateTimeKind.Local),
                    Text = "Shopping Mall - Shoes",
                    Value = -50
                },
                new
                {
                    Identifier = 3,
                    Date = new DateTime(2000, 12, 1, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name2",
                    Text = "Text2",
                    Value = 23.45
                },
                new
                {
                    Identifier = 4,
                    Date = new DateTime(2000, 12, 1, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name3",
                    Text = "Text3",
                    Value = 23.46
                },
                new
                {
                    Identifier = 5,
                    Date = new DateTime(2000, 12, 31, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name4",
                    Text = "Text4",
                    Value = -42.42
                }
            }, o => o.WithStrictOrdering());
    }

    private static AccountingData GetProject()
    {
        var project = new AccountingData
        {
            Accounts =
            [
                new AccountingDataAccountGroup
                {
                    Name = "Default",
                    Account =
                    [
                        new AccountDefinition
                        {
                            ID = 100,
                            Name = "Bank account",
                            Type = AccountDefinitionType.Asset,
                            ImportMapping = new AccountDefinitionImportMapping
                            {
                                Columns =
                                [
                                    new AccountDefinitionImportMappingColumn
                                    {
                                        Source = "Date", Target = AccountDefinitionImportMappingColumnTarget.Date
                                    },
                                    new AccountDefinitionImportMappingColumn
                                    {
                                        Source = "Name", Target = AccountDefinitionImportMappingColumnTarget.Name
                                    },
                                    new AccountDefinitionImportMappingColumn
                                    {
                                        Source = "Text", Target = AccountDefinitionImportMappingColumnTarget.Text
                                    },
                                    new AccountDefinitionImportMappingColumn
                                    {
                                        Source = "Value", Target = AccountDefinitionImportMappingColumnTarget.Value
                                    }
                                ]
                            }
                        }
                    ]
                }
            ],
            Journal =
            [
                new AccountingDataJournal { Year = "2000", DateStart = 2000_0101, DateEnd = 2000_1231, Booking = [] }
            ]
        };
        var dataJournal = project.Journal[0];
        dataJournal.Booking.Add(
            new AccountingDataJournalBooking
            {
                ID = 1,
                Date = 2000_0115,
                Credit = [new BookingValue { Account = 100, Text = "Shopping Mall - Shoes", Value = 5000 }],
                Debit =
                [
                    new BookingValue { Account = 600, Text = "Shopping Mall - Shoes", Value = 5000 }
                ]
            });
        return project;
    }

    [Fact]
    public void ImportBookings_SampleInputReverse_DataImportedAndFiltered()
    {
        AccountingData project = GetProject();

        var clock = Substitute.For<IClock>();
        var dialogs = Substitute.For<IDialogs>();
        var accounts = project.AllAccounts.ToList();
        var bankAccount = accounts.Single(x => x.Name == "Bank account");
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        projectData.LoadData(project);
        var sut = new ImportBookingsViewModel(dialogs, null!, projectData)
        {
            SelectedAccount = bankAccount,
            SelectedAccountNumber = bankAccount.ID,
            IsForceEnglish = true,
            IsReverseOrder = true
        };

        var stream = this.GetType().Assembly.GetManifestResourceStream(
            "lg2de.SimpleAccounting.IntegrationTests.Ressources.import-utf8-bom.csv");
        var bytes = new byte[stream!.Length];
        stream.Read(bytes, 0, bytes.Length).Should().Be(bytes.Length);

        sut.LoadFromBytes(bytes, "dummy");

        dialogs.DidNotReceive().ShowMessageBox(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<MessageBoxButton>(),
            Arg.Any<MessageBoxImage>(),
            Arg.Any<MessageBoxResult>(),
            Arg.Any<MessageBoxOptions>());
        sut.LoadedData.Should().NotContain(x => x.Name == "Shopping Mall", "entry is already imported");
        sut.LoadedData.Should().BeEquivalentTo(
            new[]
            {
                new
                {
                    Date = new DateTime(2000, 12, 31, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name4",
                    Text = "Text4",
                    Value = -42.42
                },
                new
                {
                    Date = new DateTime(2000, 12, 1, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name3",
                    Text = "Text3",
                    Value = 23.46
                },
                new
                {
                    Date = new DateTime(2000, 12, 1, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name2",
                    Text = "Text2",
                    Value = 23.45
                },
                new
                {
                    Date = new DateTime(2000, 1, 10, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name1",
                    Text = "Text1 with German Ü and Copyright ©",
                    Value = 12.34
                },
            }, o => o.WithStrictOrdering());
        sut.ImportDataFiltered.Should().BeEquivalentTo(
            new[]
            {
                new
                {
                    Identifier = 2,
                    Date = new DateTime(2000, 12, 1, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name3",
                    Text = "Text3",
                    Value = 23.46
                },
                new
                {
                    Identifier = 3,
                    Date = new DateTime(2000, 12, 1, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name2",
                    Text = "Text2",
                    Value = 23.45
                },
                new
                {
                    Identifier = 4,
                    Date = new DateTime(2000, 12, 31, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name4",
                    Text = "Text4",
                    Value = -42.42
                }
            }, o => o.WithStrictOrdering());

        // set start date to year begin to import data skipped before
        sut.StartDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Local);

        // note that all identifiers will be changed
        sut.ImportDataFiltered.Should().BeEquivalentTo(
            new object[]
            {
                new
                {
                    Identifier = 2,
                    Date = new DateTime(2000, 1, 10, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name1",
                    Text = "Text1 with German Ü and Copyright ©",
                    Value = 12.34
                },
                new
                {
                    Identifier = 1,
                    Date = new DateTime(2000, 1, 15, 0, 0, 0, DateTimeKind.Local),
                    Text = "Shopping Mall - Shoes",
                    Value = -50
                },
                new
                {
                    Identifier = 3,
                    Date = new DateTime(2000, 12, 1, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name3",
                    Text = "Text3",
                    Value = 23.46
                },
                new
                {
                    Identifier = 4,
                    Date = new DateTime(2000, 12, 1, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name2",
                    Text = "Text2",
                    Value = 23.45
                },
                new
                {
                    Identifier = 5,
                    Date = new DateTime(2000, 12, 31, 0, 0, 0, DateTimeKind.Local),
                    Name = "Name4",
                    Text = "Text4",
                    Value = -42.42
                }
            }, o => o.WithStrictOrdering());
    }
}
