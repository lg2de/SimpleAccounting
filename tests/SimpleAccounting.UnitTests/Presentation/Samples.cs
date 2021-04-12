// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation
{
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
                    Accounts = new List<AccountingDataAccountGroup>
                    {
                        new AccountingDataAccountGroup
                        {
                            Name = "Default",
                            Account = new List<AccountDefinition>
                            {
                                new AccountDefinition
                                {
                                    ID = BankAccount,
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
                                },
                                new AccountDefinition
                                {
                                    ID = Salary, Name = "Salary", Type = AccountDefinitionType.Income
                                },
                                new AccountDefinition
                                {
                                    ID = Shoes, Name = "Shoes", Type = AccountDefinitionType.Expense
                                },
                                new AccountDefinition
                                {
                                    ID = Carryforward, Name = "Carryforward", Type = AccountDefinitionType.Carryforward
                                }
                            }
                        },
                        new AccountingDataAccountGroup
                        {
                            Name = "Second",
                            Account = new List<AccountDefinition>
                            {
                                new AccountDefinition
                                {
                                    ID = BankCredit, Name = "Bank credit", Type = AccountDefinitionType.Credit
                                },
                                new AccountDefinition
                                {
                                    ID = FriendsDebit, Name = "Friends debit", Type = AccountDefinitionType.Debit
                                },
                                new AccountDefinition { ID = Inactive, Name = "Inactive", Active = false }
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
                            Closed = true,
                            Booking = new List<AccountingDataJournalBooking>()
                        },
                        new AccountingDataJournal
                        {
                            Year = year.ToString(CultureInfo.InvariantCulture),
                            DateStart = year * 10000 + 101,
                            DateEnd = year * 10000 + 1231,
                            Booking = new List<AccountingDataJournalBooking>()
                        }
                    }
                };
                accountingData.Accounts.Last().Account.AddRange(
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
                var processApi = Substitute.For<IProcess>();
                var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, processApi);
                projectData.Load(SampleProject);
                return projectData;
            }
        }

        [SuppressMessage("ReSharper", "RedundantAssignment")]
        [SuppressMessage("ReSharper", "S1854", Justification = "The 'useless' assignment of 'bookingIdent' helps to build code in future.")]
        public static IEnumerable<AccountingDataJournalBooking> SampleBookings
        {
            get
            {
                ulong bookingIdent = 1;
                yield return new AccountingDataJournalBooking
                {
                    ID = bookingIdent++,
                    Date = BaseDate + 101,
                    Credit = new List<BookingValue>
                    {
                        new BookingValue { Account = Carryforward, Text = "Open 1", Value = 100000 }
                    },
                    Debit = new List<BookingValue>
                    {
                        new BookingValue { Account = BankAccount, Text = "Open 1", Value = 100000 }
                    },
                    Opening = true
                };

                yield return new AccountingDataJournalBooking
                {
                    ID = bookingIdent++,
                    Date = BaseDate + 101,
                    Credit = new List<BookingValue>
                    {
                        new BookingValue { Account = BankCredit, Text = "Open 2", Value = 300000 }
                    },
                    Debit = new List<BookingValue>
                    {
                        new BookingValue { Account = Carryforward, Text = "Open 2", Value = 300000 }
                    },
                    Opening = true
                };

                yield return new AccountingDataJournalBooking
                {
                    ID = bookingIdent++,
                    Date = BaseDate + 128,
                    Credit = new List<BookingValue>
                    {
                        new BookingValue { Account = Salary, Text = "Salary1", Value = 12000 },
                        new BookingValue { Account = Salary, Text = "Salary2", Value = 8000 }
                    },
                    Debit = new List<BookingValue>
                    {
                        new BookingValue { Account = BankAccount, Text = "Salary", Value = 20000 }
                    }
                };
                yield return new AccountingDataJournalBooking
                {
                    ID = bookingIdent++,
                    Date = BaseDate + 129,
                    Credit = new List<BookingValue>
                    {
                        new BookingValue { Account = BankAccount, Text = "Credit rate", Value = 40000 },
                    },
                    Debit = new List<BookingValue>
                    {
                        new BookingValue { Account = BankCredit, Text = "Credit rate", Value = 40000 }
                    }
                };

                yield return new AccountingDataJournalBooking
                {
                    ID = bookingIdent++,
                    Date = BaseDate + 201,
                    Credit = new List<BookingValue>
                    {
                        new BookingValue { Account = BankAccount, Text = "Shoes", Value = BankCredit }
                    },
                    Debit = new List<BookingValue>
                    {
                        new BookingValue { Account = Shoes, Text = "Shoes1", Value = 2000 },
                        new BookingValue { Account = Shoes, Text = "Shoes2", Value = 3000 }
                    }
                };

                yield return new AccountingDataJournalBooking
                {
                    ID = bookingIdent++,
                    Date = BaseDate + 205,
                    Credit = new List<BookingValue>
                    {
                        new BookingValue { Account = BankAccount, Text = "Rent to friend", Value = 9900 }
                    },
                    Debit = new List<BookingValue>
                    {
                        new BookingValue { Account = FriendsDebit, Text = "Rent to friend", Value = 9900 }
                    }
                };
            }
        }
    }
}
