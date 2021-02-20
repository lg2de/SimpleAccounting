// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Caliburn.Micro;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Model;
    using NSubstitute;

    internal static class Samples
    {
        public static readonly uint BaseDate = (uint)DateTime.Now.Year * 10000;

        public static AccountingData SampleProject
        {
            get
            {
                var year = (uint)DateTime.Now.Year;
                return new AccountingData
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
                                },
                                new AccountDefinition
                                {
                                    ID = 400, Name = "Salary", Type = AccountDefinitionType.Income
                                },
                                new AccountDefinition
                                {
                                    ID = 600, Name = "Shoes", Type = AccountDefinitionType.Expense
                                },
                                new AccountDefinition
                                {
                                    ID = 990, Name = "Carryforward", Type = AccountDefinitionType.Carryforward
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
                                    ID = 5000, Name = "Bank credit", Type = AccountDefinitionType.Credit
                                },
                                new AccountDefinition
                                {
                                    ID = 6000, Name = "Friends debit", Type = AccountDefinitionType.Debit
                                },
                                new AccountDefinition { ID = 9999, Name = "Inactive", Active = false }
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
                var projectData = new ProjectData(windowManager, dialogs, fileSystem, processApi);
                projectData.Load(SampleProject);
                return projectData;
            }
        }

        public static IEnumerable<AccountingDataJournalBooking> SampleBookings
        {
            get
            {
                yield return new AccountingDataJournalBooking
                {
                    ID = 1,
                    Date = BaseDate + 101,
                    Credit = new List<BookingValue>
                    {
                        new BookingValue { Account = 990, Text = "Open 1", Value = 100000 }
                    },
                    Debit = new List<BookingValue>
                    {
                        new BookingValue { Account = 100, Text = "Open 1", Value = 100000 }
                    },
                    Opening = true
                };

                yield return new AccountingDataJournalBooking
                {
                    ID = 2,
                    Date = BaseDate + 101,
                    Credit = new List<BookingValue>
                    {
                        new BookingValue { Account = 5000, Text = "Open 2", Value = 300000 }
                    },
                    Debit = new List<BookingValue>
                    {
                        new BookingValue { Account = 990, Text = "Open 2", Value = 300000 }
                    },
                    Opening = true
                };

                yield return new AccountingDataJournalBooking
                {
                    ID = 3,
                    Date = BaseDate + 128,
                    Credit = new List<BookingValue>
                    {
                        new BookingValue { Account = 400, Text = "Salary1", Value = 12000 },
                        new BookingValue { Account = 400, Text = "Salary2", Value = 8000 }
                    },
                    Debit = new List<BookingValue>
                    {
                        new BookingValue { Account = 100, Text = "Salary", Value = 20000 }
                    }
                };
                yield return new AccountingDataJournalBooking
                {
                    ID = 4,
                    Date = BaseDate + 129,
                    Credit = new List<BookingValue>
                    {
                        new BookingValue { Account = 100, Text = "Credit rate", Value = 40000 },
                    },
                    Debit = new List<BookingValue>
                    {
                        new BookingValue { Account = 5000, Text = "Credit rate", Value = 40000 }
                    }
                };

                yield return new AccountingDataJournalBooking
                {
                    ID = 5,
                    Date = BaseDate + 201,
                    Credit = new List<BookingValue>
                    {
                        new BookingValue { Account = 100, Text = "Shoes", Value = 5000 }
                    },
                    Debit = new List<BookingValue>
                    {
                        new BookingValue { Account = 600, Text = "Shoes1", Value = 2000 },
                        new BookingValue { Account = 600, Text = "Shoes2", Value = 3000 }
                    }
                };

                yield return new AccountingDataJournalBooking
                {
                    ID = 6,
                    Date = BaseDate + 205,
                    Credit = new List<BookingValue>
                    {
                        new BookingValue { Account = 100, Text = "Rent to friend", Value = 9900 }
                    },
                    Debit = new List<BookingValue>
                    {
                        new BookingValue { Account = 6000, Text = "Rent to friend", Value = 9900 }
                    }
                };
            }
        }
    }
}
