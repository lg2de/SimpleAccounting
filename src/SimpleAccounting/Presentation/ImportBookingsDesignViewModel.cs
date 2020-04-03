// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

#pragma warning disable CA1303 // Do not pass literals as localized parameters
namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using lg2de.SimpleAccounting.Model;

    [SuppressMessage(
        "Major Code Smell", "S109:Magic numbers should not be used",
        Justification = "Design view model defines useful values")]
    [SuppressMessage(
        "Major Code Smell", "S4055:Literals should not be passed as localized parameters",
        Justification = "Design view model defines useful values")]
    internal class ImportBookingsDesignViewModel : ImportBookingsViewModel
    {
        private static readonly List<AccountDefinition> SampleAccounts = new List<AccountDefinition>
        {
            new AccountDefinition
            {
                ID = 100,
                Name = "Cash",
                ImportMapping = new AccountDefinitionImportMapping
                {
                    Columns = new List<AccountDefinitionImportMappingColumn>
                    {
                        new AccountDefinitionImportMappingColumn
                        {
                            Source = "A", Target = AccountDefinitionImportMappingColumnTarget.Date
                        },
                        new AccountDefinitionImportMappingColumn
                        {
                            Source = "B", Target = AccountDefinitionImportMappingColumnTarget.Value
                        }
                    }
                }
            },
            new AccountDefinition { ID = 600, Name = "Shopping" }
        };

        public ImportBookingsDesignViewModel()
            : base(null, null, SampleAccounts)
        {
            this.SelectedAccountNumber = 100;

            var item = new ImportEntryViewModel
            {
                Accounts = SampleAccounts,
                Date = DateTime.Now,
                Identifier = 42,
                Name = "McX",
                RemoteAccount = SampleAccounts.Single(x => x.ID == 600),
                Text = "Shoes",
                Value = 99.95
            };
            this.ImportData.Add(item);
        }
    }
}
