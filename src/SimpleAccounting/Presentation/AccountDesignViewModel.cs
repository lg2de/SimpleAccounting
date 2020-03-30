// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using lg2de.SimpleAccounting.Model;

    [SuppressMessage("Major Code Smell", "S109:Magic numbers should not be used",
        Justification = "Design view model defines useful values")]
    [SuppressMessage("Major Code Smell", "S4055:Literals should not be passed as localized parameters",
        Justification = "Design view model defines useful values")]
    internal class AccountDesignViewModel : AccountViewModel
    {
        public AccountDesignViewModel()
        {
            this.Identifier = 42;
            this.Name = "Bla";
            this.Type = AccountDefinitionType.Asset;
            this.IsActivated = true;

            this.Groups = new List<AccountingDataAccountGroup> { new AccountingDataAccountGroup { Name = "MyGroup" } };
            this.Group = this.Groups.First();
        }
    }
}
