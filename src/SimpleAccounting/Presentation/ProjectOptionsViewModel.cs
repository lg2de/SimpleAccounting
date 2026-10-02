// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        this.Organization = data.Setup.Name ?? string.Empty;
        this.Location = data.Setup.Location ?? string.Empty;
        this.Currency = data.Setup.Currency;
        this.PageBreakBetweenAccounts =
            data.Setup.Reports?.AccountJournalReport?.PageBreakBetweenAccounts ?? false;
        this.Signatures = new ObservableCollection<SignatureViewModel>(
            (data.Setup.Reports?.TotalsAndBalancesReport ?? []).Select(x => new SignatureViewModel { Text = x }));
    }

    public string Organization { get; set; }

    public string Location { get; set; }

    public string Currency { get; set; }

    public bool PageBreakBetweenAccounts { get; set; }

    public ObservableCollection<SignatureViewModel> Signatures { get; }

    public ICommand SaveCommand => new AsyncCommand(
        () => this.TryCloseAsync(this.OnSave()),
        () => !string.IsNullOrWhiteSpace(this.Currency));

    public ICommand MoveSignatureUpCommand => new AsyncCommand(o => this.MoveSignature(o, -1));

    public ICommand MoveSignatureDownCommand => new AsyncCommand(o => this.MoveSignature(o, +1));

    protected override async Task OnInitializedAsync(CancellationToken cancellationToken)
    {
        await base.OnInitializedAsync(cancellationToken);

        this.DisplayName = Resources.Header_ProjectOptions;
    }

    internal bool OnSave()
    {
        var setup = this.data.Setup;
        bool changed = false;

        string organization = this.Organization.Trim();
        if ((setup.Name ?? string.Empty) != organization)
        {
            setup.Name = organization;
            changed = true;
        }

        string location = this.Location.Trim();
        if ((setup.Location ?? string.Empty) != location)
        {
            setup.Location = location;
            changed = true;
        }

        string currency = this.Currency.Trim();
        if (setup.Currency != currency)
        {
            setup.Currency = currency;
            changed = true;
        }

        setup.Reports ??= new AccountingDataSetupReports();
        changed |= this.SaveAccountJournalReport(setup.Reports);
        changed |= this.SaveTotalsAndBalancesReport(setup.Reports);

        return changed;
    }

    private bool SaveAccountJournalReport(AccountingDataSetupReports reports)
    {
        if ((reports.AccountJournalReport?.PageBreakBetweenAccounts ?? false) == this.PageBreakBetweenAccounts)
        {
            return false;
        }

        reports.AccountJournalReport ??= new AccountingDataSetupReportsAccountJournalReport();
        reports.AccountJournalReport.PageBreakBetweenAccounts = this.PageBreakBetweenAccounts;
        return true;
    }

    private bool SaveTotalsAndBalancesReport(AccountingDataSetupReports reports)
    {
        List<string> signatures = this.Signatures
            .Select(x => x.Text.Trim())
            .Where(x => x.Length > 0)
            .ToList();
        if ((reports.TotalsAndBalancesReport ?? []).SequenceEqual(signatures))
        {
            return false;
        }

        // avoid empty XML element
        reports.TotalsAndBalancesReport = signatures.Count > 0 ? signatures : null;
        return true;
    }

    private void MoveSignature(object? item, int offset)
    {
        if (item is not SignatureViewModel signature)
        {
            return;
        }

        int index = this.Signatures.IndexOf(signature);
        int newIndex = index + offset;
        if (index < 0 || newIndex < 0 || newIndex >= this.Signatures.Count)
        {
            return;
        }

        this.Signatures.Move(index, newIndex);
    }
}
