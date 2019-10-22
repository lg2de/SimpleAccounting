// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace SimpleAccounting.UnitTests.Presentation
{
    using System.Collections.Generic;
    using lg2de.SimpleAccounting.Model;

    internal class Samples
    {
        public static AccountingData SampleProject => new AccountingData
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
                new AccountingDataYear { Name = 2019, DateStart = 20190101, DateEnd = 20191231 }
            },
            Journal = new List<AccountingDataJournal>
            {
                new AccountingDataJournal { Year = 2019, Booking = new List<AccountingDataJournalBooking>() }
            }
        };
    }
}