// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using lg2de.SimpleAccounting.Infrastructure;
using lg2de.SimpleAccounting.Model;

/// <summary>
///     Implements the view model for a single item in the import dialog.
/// </summary>
public class ImportEntryViewModel : JournalItemBaseViewModel
{
    private AccountDefinition? remoteAccount;
    private bool isSkip;

    public ImportEntryViewModel(IEnumerable<AccountDefinition> accounts)
    {
        this.Accounts = accounts.Where(a => a.Active);
    }

    public IEnumerable<AccountDefinition> Accounts { get; }

    public string Name { get; set; } = string.Empty;

    public double Value { get; set; }

    public bool IsSkip
    {
        get => this.isSkip;
        set
        {
            if (value == this.isSkip)
            {
                return;
            }

            this.isSkip = value;
            this.NotifyOfPropertyChange();
        }
    }

    public AccountDefinition? RemoteAccount
    {
        get => this.remoteAccount;
        set
        {
            this.remoteAccount = value;
            this.NotifyOfPropertyChange();
        }
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the import candidate was already booked.
    /// </summary>
    public bool IsExisting { get; set; }

    public bool IsCandidate => !this.IsExisting;

    public ICommand ResetRemoteAccountCommand => new AsyncCommand(
        () => this.RemoteAccount = null,
        () => this.RemoteAccount != null);

    internal string BuildText()
    {
        // build booking text from name and/or text
        if (string.IsNullOrWhiteSpace(this.Text))
        {
            return this.Name;
        }

        if (string.IsNullOrWhiteSpace(this.Name))
        {
            return this.Text;
        }

        return $"{this.Name} - {this.Text}";
    }
}
