// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace SimpleAccounting.UnitTests.Presentation
{
    using System;
    using System.Collections.Generic;
    using lg2de.SimpleAccounting.Model;

    internal class Samples
    {
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
                                            new AccountDefinitionImportMappingColumn { Source = "Date", Target = AccountDefinitionImportMappingColumnTarget.Date },
                                            new AccountDefinitionImportMappingColumn { Source = "Name", Target = AccountDefinitionImportMappingColumnTarget.Name },
                                            new AccountDefinitionImportMappingColumn { Source = "Text", Target = AccountDefinitionImportMappingColumnTarget.Text },
                                            new AccountDefinitionImportMappingColumn { Source = "Value", Target = AccountDefinitionImportMappingColumnTarget.Value }
                                        }
                                    }
                                },
                                new AccountDefinition
                                {
                                    ID = 400,
                                    Name = "Salary",
                                    Type = AccountDefinitionType.Income
                                },
                                new AccountDefinition
                                {
                                    ID = 600,
                                    Name = "Shoes",
                                    Type = AccountDefinitionType.Expense
                                },
                                new AccountDefinition
                                {
                                    ID = 990,
                                    Name = "Carryforward",
                                    Type = AccountDefinitionType.Carryforward
                                }
                            }
                        }
                    },
                    Journal = new List<AccountingDataJournal>
                    {
                        new AccountingDataJournal
                        {
                            Year = 2000,
                            DateStart = 20000101,
                            DateEnd = 20001231,
                            Closed = true,
                            Booking = new List<AccountingDataJournalBooking>()
                        },
                        new AccountingDataJournal
                        {
                            Year = (ushort)year,
                            DateStart = year * 10000 + 101,
                            DateEnd = year * 10000 + 1231,
                            Booking = new List<AccountingDataJournalBooking>()
                        }
                    }
                };
            }
        }

        public static IEnumerable<AccountingDataJournalBooking> SampleBookings
        {
            get
            {
                var baseDate = (uint)DateTime.Now.Year * 10000;

                yield return new AccountingDataJournalBooking
                {
                    ID = 1,
                    Date = baseDate + 101,
                    Credit = new List<BookingValue>
                    {
                        new BookingValue { Account = 990, Text = "Open", Value = 100000 }
                    },
                    Debit = new List<BookingValue>
                    {
                        new BookingValue { Account = 100, Text = "Open", Value = 100000 }
                    }
                };

                yield return new AccountingDataJournalBooking
                {
                    ID = 2,
                    Date = baseDate + 131,
                    Credit = new List<BookingValue>
                    {
                        new BookingValue { Account = 400, Text = "Salary1", Value = 10000 },
                        new BookingValue { Account = 400, Text = "Salary2", Value = 10000 }
                    },
                    Debit = new List<BookingValue>
                    {
                        new BookingValue { Account = 100, Text = "Salary", Value = 20000 }
                    }
                };
                yield return new AccountingDataJournalBooking
                {
                    ID = 3,
                    Date = baseDate + 201,
                    Credit = new List<BookingValue>
                    {
                        new BookingValue { Account = 100, Text = "Shoes", Value = 10000 }
                    },
                    Debit = new List<BookingValue>
                    {
                        new BookingValue { Account = 600, Text = "Shoes1", Value = 5000 },
                        new BookingValue { Account = 600, Text = "Shoes2", Value = 5000 }
                    }
                };
            }
        }
    }
}