// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Windows.Input;
    using Caliburn.Micro;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Infrastructure;
    using lg2de.SimpleAccounting.Model;

    [SuppressMessage("ReSharper", "StringLiteralTypo")] // TODO introduce localization
    internal class EditBookingViewModel : Screen
    {
        private readonly ShellViewModel parent;

        private ulong creditAccount;
        private ulong debitAccount;
        private BookingTemplate? selectedTemplate;

        public EditBookingViewModel(ShellViewModel parent, DateTime dateStart, DateTime dateEnd, bool editMode = false)
        {
            this.EditMode = editMode;
            this.parent = parent;
            this.DateStart = dateStart;
            this.DateEnd = dateEnd;

            this.CreditSplitEntries.CollectionChanged +=
                (sender, args) =>
                {
                    this.NotifyOfPropertyChange(nameof(this.DebitSplitAllowed));
                    this.NotifyOfPropertyChange(nameof(this.IsEasyBookingEnabled));
                };
            this.DebitSplitEntries.CollectionChanged +=
                (sender, args) =>
                {
                    this.NotifyOfPropertyChange(nameof(this.CreditSplitAllowed));
                    this.NotifyOfPropertyChange(nameof(this.IsEasyBookingEnabled));
                };
        }

        public bool NewMode => !this.EditMode;

        public bool EditMode { get; }

        public DateTime Date { get; set; } = DateTime.Today;

        public ulong BookingIdentifier { get; set; }

        public ObservableCollection<BookingTemplate> BindingTemplates { get; }
            = new ObservableCollection<BookingTemplate>();

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

        public List<AccountDefinition> Accounts { get; }
            = new List<AccountDefinition>();

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

        public ObservableCollection<SplitBookingViewModel> CreditSplitEntries { get; } =
            new ObservableCollection<SplitBookingViewModel>();

        public bool CreditSplitAllowed => this.DebitSplitEntries.Count == 0;

        public ObservableCollection<SplitBookingViewModel> DebitSplitEntries { get; } =
            new ObservableCollection<SplitBookingViewModel>();

        public bool DebitSplitAllowed => this.CreditSplitEntries.Count == 0;

        public int DebitIndex { get; set; } = -1;

        public int CreditIndex { get; set; } = -1;

        public int IncomeIndex { get; set; } = -1;

        public int IncomeRemoteIndex { get; set; } = -1;

        public int ExpenseIndex { get; set; } = -1;

        public int ExpenseRemoteIndex { get; set; } = -1;

        public uint PageIndex { get; set; }

        public ICommand AddCommand => new RelayCommand(
            _ =>
            {
                var newBooking = this.CreateJournalEntry();
                this.parent.AddBooking(newBooking);

                // update for next booking
                this.BookingIdentifier++;
                this.NotifyOfPropertyChange(nameof(this.BookingIdentifier));
            },
            _ => this.IsDataValid());

        public ICommand SaveCommand => new RelayCommand(
            _ => this.TryClose(true),
            _ => this.IsDataValid());

        public ICommand DefaultCommand => new RelayCommand(
            _ =>
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

        internal DateTime DateStart { get; }

        internal DateTime DateEnd { get; }

        public AccountingDataJournalBooking CreateJournalEntry()
        {
            var newBooking = new AccountingDataJournalBooking
            {
                Date = this.Date.ToAccountingDate(), ID = this.BookingIdentifier
            };
            var baseValue = new BookingValue { Text = this.BookingText, Value = this.BookingValue.ToModelValue() };

            if (this.CreditSplitEntries.Any())
            {
                // complete base value for debit...
                baseValue.Account = this.DebitAccount;
                newBooking.Debit = new List<BookingValue> { baseValue };

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
                newBooking.Credit = new List<BookingValue> { baseValue };

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
            newBooking.Credit = new List<BookingValue> { baseValue };
            newBooking.Debit = new List<BookingValue> { debitValue };
            return newBooking;
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            this.DisplayName = "Neue Buchung erstellen";
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

#pragma warning disable S2365 // Properties should not make collection or array copies
        public List<AccountDefinition> IncomeAccounts =>
            this.Accounts.Where(x => x.Type == AccountDefinitionType.Income).ToList();

        public List<AccountDefinition> IncomeRemoteAccounts =>
            this.Accounts.Where(x => x.Type != AccountDefinitionType.Income).ToList();

        public List<AccountDefinition> ExpenseAccounts =>
            this.Accounts.Where(x => x.Type == AccountDefinitionType.Expense).ToList();

        public List<AccountDefinition> ExpenseRemoteAccounts =>
            this.Accounts.Where(x => x.Type != AccountDefinitionType.Expense).ToList();
#pragma warning restore S2365 // Properties should not make collection or array copies
    }
}
