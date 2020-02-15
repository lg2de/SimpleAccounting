// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System.Collections.Generic;
    using System.Linq;
    using lg2de.SimpleAccounting.Model;

    internal class AccountDesignViewModel : AccountViewModel
    {
        public AccountDesignViewModel()
        {
            this.Identifier = 42;
            this.Name = "Bla";
            this.Type = AccountDefinitionType.Asset;
            this.IsActivated = true;

            this.Groups = new List<AccountingDataAccountGroup>
                { new AccountingDataAccountGroup { Name = "MyGroup" } };
            this.Group = this.Groups.First();
        }
    }
}
