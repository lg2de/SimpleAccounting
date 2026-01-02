// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Caliburn.Micro;
using lg2de.SimpleAccounting.Extensions;
using lg2de.SimpleAccounting.Infrastructure;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Properties;

internal class EditBookingViewModel : Screen
{
    private readonly IProjectData projectData;

    private ulong creditAccount;
    private ulong debitAccount;
    private BookingTemplate? selectedTemplate;

    public EditBookingViewModel(IProjectData projectData, DateTime date, bool editMode)
    {
        this.projectData = projectData;
        this.Date = date;
        this.EditMode = editMode;

        this.DateStart = this.projectData.CurrentYear.DateStart.ToDateTime();
        this.DateEnd = this.projectData.CurrentYear.DateEnd.ToDateTime();
        this.BookingIdentifier = this.projectData.MaxBookIdent + 1;
        this.ValueLabel = string.Format(
            CultureInfo.CurrentUICulture,
            Resources.Label_ValueWithCurrencyX, this.projectData.Storage.Setup.Currency);

        if (this.Date > this.DateEnd)
        {
            this.Date = this.DateEnd;
        }

        if (this.Date < this.DateStart)
        {
            this.Date = this.DateStart;
        }

        this.CreditSplitEntries.CollectionChanged +=
            (_, _) =>
            {
                this.NotifyOfPropertyChange(nameof(this.DebitSplitAllowed));
                this.NotifyOfPropertyChange(nameof(this.IsEasyBookingEnabled));
            };
        this.DebitSplitEntries.CollectionChanged +=
            (_, _) =>
            {
                this.NotifyOfPropertyChange(nameof(this.CreditSplitAllowed));
                this.NotifyOfPropertyChange(nameof(this.IsEasyBookingEnabled));
            };
    }

    public bool NewMode => !this.EditMode;

    public bool EditMode { get; }

    public List<AccountDefinition> Accounts { get; } = [];

    public List<AccountDefinition> IncomeAccounts { get; private set; } = [];

    public List<AccountDefinition> IncomeRemoteAccounts { get; private set; } = [];

    public List<AccountDefinition> ExpenseAccounts { get; private set; } = [];

    public List<AccountDefinition> ExpenseRemoteAccounts { get; private set; } = [];

    public string ValueLabel { get; }

    public DateTime Date { get; set; }

    public DateTime DateStart { get; }

    public DateTime DateEnd { get; }

    public ulong BookingIdentifier { get; set; }

    public ObservableCollection<BookingTemplate> BindingTemplates { get; } = [];

    public BookingTemplate? SelectedTemplate
    {
        get => this.selectedTemplate;
        set
        {
            this.selectedTemplate = value;
            if (this.selectedTemplate == null)
            {
                return;
            }

            if (this.selectedTemplate.Debit > 0)
            {
                this.DebitAccount = this.selectedTemplate.Debit;
            }

            if (this.selectedTemplate.Credit > 0)
            {
                this.CreditAccount = this.selectedTemplate.Credit;
            }

            if (this.selectedTemplate.Value > 0)
            {
                this.BookingValue = this.selectedTemplate.Value;
            }
        }
    }

    public string BookingText { get; set; } = string.Empty;

    public double BookingValue { get; set; }

    public bool IsOpening { get; set; }

    public bool IsFollowup { get; set; }

    public bool IsEasyBookingEnabled => this.DebitSplitAllowed && this.CreditSplitAllowed;

    public ulong CreditAccount
    {
        get => this.creditAccount;
        set
        {
            if (this.creditAccount == value)
            {
                return;
            }

            this.creditAccount = value;
            this.NotifyOfPropertyChange();
        }
    }

    public ulong DebitAccount
    {
        get => this.debitAccount;
        set
        {
            if (this.debitAccount == value)
            {
                return;
            }

            this.debitAccount = value;
            this.NotifyOfPropertyChange();
        }
    }

    public ObservableCollection<SplitBookingViewModel> CreditSplitEntries { get; } = [];

    public bool CreditSplitAllowed => this.DebitSplitEntries.Count == 0;

    public ObservableCollection<SplitBookingViewModel> DebitSplitEntries { get; } = [];

    public bool DebitSplitAllowed => this.CreditSplitEntries.Count == 0;

    public int DebitIndex { get; set; } = -1;

    public int CreditIndex { get; set; } = -1;

    public int IncomeIndex { get; set; } = -1;

    public int IncomeRemoteIndex { get; set; } = -1;

    public int ExpenseIndex { get; set; } = -1;

    public int ExpenseRemoteIndex { get; set; } = -1;

    public uint PageIndex { get; set; }

    public ICommand AddCommand => new AsyncCommand(
        () =>
        {
            var newBooking = this.CreateJournalEntry();
            this.projectData.AddBooking(newBooking, updateJournal: true);

            // update for next booking
            this.BookingIdentifier++;
            this.NotifyOfPropertyChange(nameof(this.BookingIdentifier));
        },
        this.IsDataValid);

    public ICommand SaveCommand => new AsyncCommand(
        () => this.TryCloseAsync(true),
        this.IsDataValid);

    public ICommand DefaultCommand => new AsyncCommand(
        () =>
        {
            if (this.EditMode)
            {
                this.SaveCommand.Execute(null);
            }
            else
            {
                this.AddCommand.Execute(null);
            }
        });

    public AccountingDataJournalBooking CreateJournalEntry()
    {
        var newBooking = new AccountingDataJournalBooking
        {
            Date = this.Date.ToAccountingDate(),
            ID = this.BookingIdentifier,
            Followup = this.IsFollowup,
            Opening = this.IsOpening
        };
        var baseValue = new BookingValue { Text = this.BookingText, Value = this.BookingValue.ToModelValue() };

        if (this.CreditSplitEntries.Any())
        {
            // complete base value for debit...
            baseValue.Account = this.DebitAccount;
            newBooking.Debit = [baseValue];

            // ...and build credit values
            newBooking.Credit = this.CreditSplitEntries.Select(x => x.ToBooking()).ToList();
            if (newBooking.Credit.Count == 1)
            {
                // consistent use of overall text
                newBooking.Credit.Single().Text = this.BookingText;
                newBooking.Debit.Single().Text = this.BookingText;
            }

            return newBooking;
        }

        if (this.DebitSplitEntries.Any())
        {
            // complete base value for credit...
            baseValue.Account = this.CreditAccount;
            newBooking.Credit = [baseValue];

            // ...and build debit values
            newBooking.Debit = this.DebitSplitEntries.Select(x => x.ToBooking()).ToList();

            if (newBooking.Debit.Count == 1)
            {
                // consistent use of overall text
                newBooking.Credit.Single().Text = this.BookingText;
                newBooking.Debit.Single().Text = this.BookingText;
            }

            return newBooking;
        }

        var debitValue = baseValue.Clone();
        baseValue.Account = this.CreditAccount;
        debitValue.Account = this.DebitAccount;
        newBooking.Credit = [baseValue];
        newBooking.Debit = [debitValue];
        return newBooking;
    }

    public void AddTemplates(AccountingDataSetupBookingTemplates bookingTemplates)
    {
        bookingTemplates.Template
            .Select(
                t => new BookingTemplate
                {
                    Text = t.Text, Credit = t.Credit, Debit = t.Debit, Value = t.Value.ToViewModel()
                })
            .ToList().ForEach(this.BindingTemplates.Add);
    }

    protected override async Task OnInitializedAsync(CancellationToken cancellationToken)
    {
        await base.OnInitializedAsync(cancellationToken);

        this.DisplayName = this.EditMode ? Resources.Header_EditBooking : Resources.Header_NewBooking;

        this.IncomeAccounts = this.Accounts.Where(x => x.Type == AccountDefinitionType.Income).ToList();

        this.IncomeRemoteAccounts =
            this.Accounts.Where(x => x.Type != AccountDefinitionType.Income).ToList();

        this.ExpenseAccounts =
            this.Accounts.Where(x => x.Type == AccountDefinitionType.Expense).ToList();

        this.ExpenseRemoteAccounts =
            this.Accounts.Where(x => x.Type != AccountDefinitionType.Expense).ToList();
    }

    private bool IsDataValid()
    {
        if (this.Date < this.DateStart || this.Date > this.DateEnd)
        {
            return false;
        }

        if (this.BookingIdentifier <= 0 || this.BookingValue <= 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(this.BookingText))
        {
            return false;
        }

        if (!this.DebitSplitEntries.IsConsistent(
                (ExpectedSum: this.BookingValue, RemoteAccountNumber: this.CreditAccount)))
        {
            return false;
        }

        if (!this.CreditSplitEntries.IsConsistent(
                (ExpectedSum: this.BookingValue, RemoteAccountNumber: this.DebitAccount)))
        {
            return false;
        }

        switch (this.PageIndex)
        {
        case EditBookingView.DebitCreditPageIndex:
            return this.IsDebitCreditBookingValid();
        case EditBookingView.IncomePageIndex:
            return this.IncomeIndex >= 0 && this.IncomeRemoteIndex >= 0;
        case EditBookingView.ExpensePageIndex:
            return this.ExpenseIndex >= 0 && this.ExpenseRemoteIndex >= 0;

        default:
            throw new InvalidOperationException($"The page index {this.PageIndex} is not implemented.");
        }
    }

    private bool IsDebitCreditBookingValid()
    {
        if (this.DebitSplitEntries.Any())
        {
            // already checked
            return true;
        }

        if (this.CreditSplitEntries.Any())
        {
            // already checked
            return true;
        }

        return this.CreditIndex >= 0 && this.DebitIndex >= 0 && this.CreditIndex != this.DebitIndex;
    }
}
