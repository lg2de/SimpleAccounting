// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

using System.Collections.Generic;
using lg2de.SimpleAccounting.Model;

namespace lg2de.SimpleAccounting.Presentation
{
    public class ImportEntryViewModel : JournalBaseViewModel
    {
        public IEnumerable<AccountDefinition> Accounts { get; set; }

        public string Name { get; set; }

        public double Value { get; set; }

        public AccountDefinition RemoteAccount { get; set; }
    }
}
