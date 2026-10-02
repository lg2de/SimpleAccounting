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
using lg2de.SimpleAccounting.Extensions;
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

        var templates = data.Setup.BookingTemplates?.Template ?? [];
        var referencedAccounts = templates.SelectMany(x => new[] { GetDebit(x), GetCredit(x) }).ToHashSet();
        this.TemplateAccounts =
        [
            BookingTemplateViewModel.NoAccount,
            ..data.AllAccounts
                .Where(x => (x.Active && x.Type != AccountDefinitionType.Carryforward)
                            || referencedAccounts.Contains(x.ID))
                .OrderBy(x => x.ID)
        ];
        this.BookingTemplates = new ObservableCollection<BookingTemplateViewModel>(
            templates.Select(
                x => new BookingTemplateViewModel
                {
                    Text = x.Text ?? string.Empty,
                    Value = GetValue(x)?.ToViewModel(),
                    DebitAccount = this.FindTemplateAccount(GetDebit(x)),
                    CreditAccount = this.FindTemplateAccount(GetCredit(x))
                }));
    }

    public string Organization { get; set; }

    public string Location { get; set; }

    public string Currency { get; set; }

    public bool PageBreakBetweenAccounts { get; set; }

    public ObservableCollection<SignatureViewModel> Signatures { get; }

    public IReadOnlyList<AccountDefinition> TemplateAccounts { get; }

    public ObservableCollection<BookingTemplateViewModel> BookingTemplates { get; }

    public ICommand SaveCommand => new AsyncCommand(
        () => this.TryCloseAsync(this.OnSave()),
        () => !string.IsNullOrWhiteSpace(this.Currency) && this.BookingTemplates.All(x => x.IsValid));

    public ICommand MoveSignatureUpCommand => new AsyncCommand(o => Move(this.Signatures, o, -1));

    public ICommand MoveSignatureDownCommand => new AsyncCommand(o => Move(this.Signatures, o, +1));

    public ICommand MoveTemplateUpCommand => new AsyncCommand(o => Move(this.BookingTemplates, o, -1));

    public ICommand MoveTemplateDownCommand => new AsyncCommand(o => Move(this.BookingTemplates, o, +1));

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
        changed |= this.SaveBookingTemplates(setup);

        return changed;
    }

    private static long? GetValue(AccountingDataSetupBookingTemplatesTemplate template)
    {
        return template.ValueSpecified || template.Value != 0 ? template.Value : null;
    }

    // Unset accounts are deserialized as 0, which is also the identifier of NoAccount.
    private static ulong GetDebit(AccountingDataSetupBookingTemplatesTemplate template) => template.Debit;

    private static ulong GetCredit(AccountingDataSetupBookingTemplatesTemplate template) => template.Credit;

    private static void Move<T>(ObservableCollection<T> items, object? item, int offset)
    {
        if (item is not T typedItem)
        {
            return;
        }

        int index = items.IndexOf(typedItem);
        int newIndex = index + offset;
        if (index < 0 || newIndex < 0 || newIndex >= items.Count)
        {
            return;
        }

        items.Move(index, newIndex);
    }

    private AccountDefinition FindTemplateAccount(ulong accountId)
    {
        return this.TemplateAccounts.FirstOrDefault(x => x.ID == accountId) ?? BookingTemplateViewModel.NoAccount;
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

    private bool SaveBookingTemplates(AccountingDataSetup setup)
    {
        var oldTemplates = (setup.BookingTemplates?.Template ?? [])
            .Select(x => (x.Text, Value: GetValue(x), Debit: GetDebit(x), Credit: GetCredit(x)));
        var newTemplates = this.BookingTemplates
            .Select(x => (Text: x.Text.Trim(), Value: x.Value?.ToModelValue(), Debit: x.DebitAccount.ID,
                Credit: x.CreditAccount.ID))
            .ToList();
        if (oldTemplates.SequenceEqual(newTemplates))
        {
            return false;
        }

        // avoid empty XML element
        setup.BookingTemplates = newTemplates.Count == 0
            ? null
            : new AccountingDataSetupBookingTemplates
            {
                Template = newTemplates.Select(
                    x => new AccountingDataSetupBookingTemplatesTemplate
                    {
                        Text = x.Text,
                        Value = x.Value ?? 0,
                        ValueSpecified = x.Value.HasValue,
                        Debit = x.Debit,
                        DebitSpecified = x.Debit > 0,
                        Credit = x.Credit,
                        CreditSpecified = x.Credit > 0
                    }).ToList()
            };
        return true;
    }
}
