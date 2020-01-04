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
                    Years = new List<AccountingDataYear>
                    {
                        new AccountingDataYear
                        {
                            Name = 2000,
                            DateStart = 20000101,
                            DateEnd = 20001231,
                            Closed = true
                        },
                        new AccountingDataYear
                        {
                            Name = (ushort)year,
                            DateStart = year * 10000 + 101,
                            DateEnd = year * 10000 + 1231
                        }
                    },
                    Journal = new List<AccountingDataJournal>
                    {
                        new AccountingDataJournal
                        {
                            Year = 2000,
                            Booking = new List<AccountingDataJournalBooking>()
                        },
                        new AccountingDataJournal
                        {
                            Year = (ushort)year,
                            Booking = new List<AccountingDataJournalBooking>()
                        }
                    }
                };
            }
        }
    }
}