// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

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
        this.Type = AccountDefinitionType.Credit;
        this.IsActivated = true;

        this.Groups = new List<AccountingDataAccountGroup> { new() { Name = "MyGroup" } };
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

        this.ImportRemoteAccounts = new List<AccountDefinition> { new() { ID = 100, Name = "Bank" } };
        this.ImportPatterns.Add(
            new ImportPatternViewModel
            {
                Expression = "RegEx", Value = 29.95, Account = this.ImportRemoteAccounts[0]
            });
    }
}
