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
                                    ID = 100, Name = "Bank account", Type = AccountDefinitionType.Asset
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
                            Name = (ushort)year,
                            DateStart = year * 10000 + 101,
                            DateEnd = year * 10000 + 1231
                        }
                    },
                    Journal = new List<AccountingDataJournal>
                    {
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