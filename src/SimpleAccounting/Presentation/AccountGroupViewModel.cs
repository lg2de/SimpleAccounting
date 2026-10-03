// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System;
using System.ComponentModel;
using Caliburn.Micro;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Properties;

/// <summary>
///     Implements the view model to edit a single account group of the project.
/// </summary>
public sealed class AccountGroupViewModel : PropertyChangedBase, IDataErrorInfo
{
    public AccountGroupViewModel(AccountingDataAccountGroup? model, Func<AccountGroupViewModel, bool> isNameUnique)
    {
        this.Model = model;
        this.IsNameUnique = isNameUnique;
        this.Name = model?.Name ?? string.Empty;
        this.AccountCount = model?.Account?.Count ?? 0;
    }

    public string Name
    {
        get;
        set
        {
            field = value;
            this.NotifyOfPropertyChange();
        }
    }

    public int AccountCount { get; }

    public bool IsValid => this.ValidateName() == null;

    /// <summary>
    ///     Gets the existing group in the project, or <c>null</c> for a new group.
    /// </summary>
    internal AccountingDataAccountGroup? Model { get; }

    private Func<AccountGroupViewModel, bool> IsNameUnique { get; }

    string IDataErrorInfo.Error => this.ValidateName() ?? string.Empty;

    string IDataErrorInfo.this[string columnName] =>
        (columnName == nameof(this.Name) ? this.ValidateName() : null) ?? string.Empty;

    /// <summary>
    ///     Forces the view to validate the name again, e.g. after the name of another group has been changed.
    /// </summary>
    internal void RevalidateName()
    {
        this.NotifyOfPropertyChange(nameof(this.Name));
    }

    private string? ValidateName()
    {
        if (string.IsNullOrWhiteSpace(this.Name))
        {
            return Resources.ProjectOptions_GroupNameRequired;
        }

        return this.IsNameUnique(this) ? null : Resources.ProjectOptions_GroupNameDuplicated;
    }
}
