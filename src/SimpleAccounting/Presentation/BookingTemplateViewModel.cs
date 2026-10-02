// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System.ComponentModel;
using Caliburn.Micro;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Properties;

/// <summary>
///     Implements the view model to edit a single booking template of the project.
/// </summary>
/// <remarks>
///     The accounts are optional. An unset account is represented by <see cref="NoAccount"/>.
/// </remarks>
public sealed class BookingTemplateViewModel : PropertyChangedBase, IDataErrorInfo
{
    public static readonly AccountDefinition NoAccount = new() { ID = 0, Name = Resources.ProjectOptions_NoAccount };

    public string Text { get; set; } = string.Empty;

    public double? Value { get; set; }

    public AccountDefinition DebitAccount
    {
        get;
        set
        {
            field = value;
            this.NotifyOfPropertyChange();

            // re-validate the other account
            this.NotifyOfPropertyChange(nameof(this.CreditAccount));
        }
    } = NoAccount;

    public AccountDefinition CreditAccount
    {
        get;
        set
        {
            field = value;
            this.NotifyOfPropertyChange();

            // re-validate the other account
            this.NotifyOfPropertyChange(nameof(this.DebitAccount));
        }
    } = NoAccount;

    public bool IsValid => this.ValidateText() == null && this.ValidateAccounts() == null;

    string IDataErrorInfo.Error => this.ValidateText() ?? this.ValidateAccounts() ?? string.Empty;

    string IDataErrorInfo.this[string columnName] =>
        columnName switch
        {
            nameof(this.Text) => this.ValidateText(),
            nameof(this.DebitAccount) or nameof(this.CreditAccount) => this.ValidateAccounts(),
            _ => null
        } ?? string.Empty;

    private string? ValidateText()
    {
        return string.IsNullOrWhiteSpace(this.Text) ? Resources.ProjectOptions_TemplateTextRequired : null;
    }

    private string? ValidateAccounts()
    {
        return this.DebitAccount.ID > 0 && this.DebitAccount.ID == this.CreditAccount.ID
            ? Resources.ProjectOptions_TemplateSameAccounts
            : null;
    }
}
