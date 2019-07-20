// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

using System;
using System.Linq;
using lg2de.SimpleAccounting.Model;

namespace lg2de.SimpleAccounting.Presentation
{
    internal class ImportBookingsDesignViewModel : ImportBookingsViewModel
    {
        public ImportBookingsDesignViewModel()
            : base(null, null)
        {
            this.Accounts.Add(new AccountDefinition { ID = 100, Name = "Cash" });
            this.Accounts.Add(new AccountDefinition { ID = 600, Name = "Shopping" });

            this.ImportAccount = 100;

            var item = new ImportEntryViewModel
            {
                Accounts = this.Accounts,
                Date = DateTime.Now,
                Identifier = 42,
                Name = "McX",
                RemoteAccount = this.Accounts.Single(x => x.ID == 600),
                Text = "Shoes",
                Value = 99.95
            };
            this.ImportData.Add(item);
        }
    }
}
