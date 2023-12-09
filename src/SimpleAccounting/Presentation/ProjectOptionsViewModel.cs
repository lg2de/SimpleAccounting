// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Caliburn.Micro;
using lg2de.SimpleAccounting.Infrastructure;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Properties;

public class ProjectOptionsViewModel : Screen
{
    private readonly AccountingData data;

    public ProjectOptionsViewModel(AccountingData data)
    {
        this.data = data ?? throw new ArgumentNullException(nameof(data));
        this.Currency = data.Setup.Currency;
    }

    public string Currency { get; set; }

    public ICommand SaveCommand => new RelayCommand(
        _ => this.TryCloseAsync(this.OnSave()),
        _ => !string.IsNullOrWhiteSpace(this.Currency));

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        await base.OnInitializeAsync(cancellationToken);

        this.DisplayName = Resources.Header_ProjectOptions;
    }

    internal bool OnSave()
    {
        string currency = this.Currency.Trim();
        if (this.data.Setup.Currency != currency)
        {
            this.data.Setup.Currency = currency;

            // changed
            return true;
        }

        // unchanged
        return false;
    }
}
