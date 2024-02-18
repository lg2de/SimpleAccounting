// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Caliburn.Micro;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Properties;
using NSubstitute;

[ExcludeFromCodeCoverage]
internal static class Samples
{
    public const int BankAccount = 100;
    public const int Salary = 400;
    public const int Shoes = 600;
    public const int Carryforward = 990;
    public const int BankCredit = 5000;
    public const int FriendsDebit = 6000;
    public const int Inactive = 9999;
    public static readonly uint BaseDate = (uint)DateTime.Now.Year * 10000;

    public static AccountingData SampleProject
    {
        get
        {
            var year = (uint)DateTime.Now.Year;
            var accountingData = new AccountingData
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
                                ID = BankAccount,
                                Name = "Bank account",
                                Type = AccountDefinitionType.Asset,
                                ImportMapping = new AccountDefinitionImportMapping
                                {
                                    Columns =
                                    [
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
                                    ]
                                }
                            },
                            new AccountDefinition { ID = Salary, Name = "Salary", Type = AccountDefinitionType.Income },
                            new AccountDefinition { ID = Shoes, Name = "Shoes", Type = AccountDefinitionType.Expense },
                            new AccountDefinition
                            {
                                ID = Carryforward, Name = "Carryforward", Type = AccountDefinitionType.Carryforward
                            }
                        ]
                    },
                    new AccountingDataAccountGroup
                    {
                        Name = "Second",
                        Account =
                        [
                            new AccountDefinition
                            {
                                ID = BankCredit, Name = "Bank credit", Type = AccountDefinitionType.Credit
                            },

                            new AccountDefinition
                            {
                                ID = FriendsDebit, Name = "Friends debit", Type = AccountDefinitionType.Debit
                            },

                            new AccountDefinition { ID = Inactive, Name = "Inactive", Active = false }
                        ]
                    }
                ],
                Journal =
                [
                    new AccountingDataJournal
                    {
                        Year = "2000",
                        DateStart = 2000_0101,
                        DateEnd = 2000_1231,
                        Closed = true,
                        Booking = []
                    },
                    new AccountingDataJournal
                    {
                        Year = year.ToString(CultureInfo.InvariantCulture),
                        DateStart = year * 10000 + 101,
                        DateEnd = year * 10000 + 1231,
                        Booking = []
                    }
                ]
            };
            accountingData.Accounts[^1].Account.AddRange(
                Enum.GetValues(typeof(AccountDefinitionType)).Cast<AccountDefinitionType>().Select(
                    type => new AccountDefinition
                    {
                        ID = (ulong)(9000 + type), Name = $"Active empty {type}", Type = type, Active = true
                    }));
            return accountingData;
        }
    }

    public static ProjectData SampleProjectData
    {
        get
        {
            var windowManager = Substitute.For<IWindowManager>();
            var dialogs = Substitute.For<IDialogs>();
            var fileSystem = Substitute.For<IFileSystem>();
            var clock = Substitute.For<IClock>();
            var processApi = Substitute.For<IProcess>();
            var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
            projectData.LoadData(SampleProject);
            return projectData;
        }
    }

    public static AccountDefinitionImportMapping SimpleImportConfiguration
    {
        get
        {
            return new AccountDefinitionImportMapping
            {
                Columns =
                [
                    new AccountDefinitionImportMappingColumn
                    {
                        Target = AccountDefinitionImportMappingColumnTarget.Date, Source = "Date"
                    },

                    new AccountDefinitionImportMappingColumn
                    {
                        Target = AccountDefinitionImportMappingColumnTarget.Value, Source = "Value"
                    }
                ]
            };
        }
    }

    public static IEnumerable<AccountingDataJournalBooking> SampleBookings
    {
        get
        {
            yield return new AccountingDataJournalBooking
            {
                // attention, explicitly starting with unsorted ID to test sorting by date and ID
                ID = 2,
                Date = BaseDate + 101,
                Credit = [new BookingValue { Account = BankCredit, Text = "Open 2", Value = 300000 }],
                Debit = [new BookingValue { Account = Carryforward, Text = "Open 2", Value = 300000 }],
                Opening = true
            };

            yield return new AccountingDataJournalBooking
            {
                ID = 1,
                Date = BaseDate + 101,
                Credit = [new BookingValue { Account = Carryforward, Text = "Open 1", Value = 100000 }],
                Debit = [new BookingValue { Account = BankAccount, Text = "Open 1", Value = 100000 }],
                Opening = true
            };

            yield return new AccountingDataJournalBooking
            {
                ID = 3,
                Date = BaseDate + 128,
                Credit =
                [
                    new BookingValue { Account = Salary, Text = "Salary1", Value = 12000 },
                    new BookingValue { Account = Salary, Text = "Salary2", Value = 8000 }
                ],
                Debit = [new BookingValue { Account = BankAccount, Text = "Salary", Value = 20000 }]
            };

            yield return new AccountingDataJournalBooking
            {
                ID = 4,
                Date = BaseDate + 129,
                Credit =
                [
                    new BookingValue { Account = BankAccount, Text = "Credit rate", Value = 40000 }
                ],
                Debit = [new BookingValue { Account = BankCredit, Text = "Credit rate", Value = 40000 }]
            };

            yield return new AccountingDataJournalBooking
            {
                ID = 5,
                Date = BaseDate + 201,
                Credit = [new BookingValue { Account = BankAccount, Text = "Shoes", Value = BankCredit }],
                Debit =
                [
                    new BookingValue { Account = Shoes, Text = "Shoes1", Value = 2000 },
                    new BookingValue { Account = Shoes, Text = "Shoes2", Value = 3000 }
                ]
            };

            yield return new AccountingDataJournalBooking
            {
                ID = 6,
                Date = BaseDate + 205,
                Credit = [new BookingValue { Account = BankAccount, Text = "Rent to friend", Value = 9900 }],
                Debit = [new BookingValue { Account = FriendsDebit, Text = "Rent to friend", Value = 9900 }]
            };
        }
    }
}
