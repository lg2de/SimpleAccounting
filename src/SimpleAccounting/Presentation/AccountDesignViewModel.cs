// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using lg2de.SimpleAccounting.Model;

    /// <summary>
    ///     Implements the designer view model for <see cref="AccountViewModel"/>.
    /// </summary>
    [SuppressMessage(
        "Major Code Smell", "S109:Magic numbers should not be used",
        Justification = "Design view model defines useful values")]
    [SuppressMessage(
        "Major Code Smell", "S4055:Literals should not be passed as localized parameters",
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

            this.IsImportActive = true;
            this.ImportDateSource = "Date column";
            this.ImportDateIgnorePattern = "ignore date";
            this.ImportNameSource = "Name column";
            this.ImportNameIgnorePattern = "ignore name";
            this.ImportTextSource = "Text column";
            this.ImportTextIgnorePattern = "ignore text";
            this.ImportValueSource = "Value column";
            this.ImportValueIgnorePattern = "ignore value";

            var accounts = new List<AccountDefinition> { new AccountDefinition { ID = 100, Name = "Bank" } };
            this.ImportPatterns.Add(new ImportPatternViewModel(accounts, "RegEx") { Value = 29.95, Account = accounts.First()});
        }
    }
}
