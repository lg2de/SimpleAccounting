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

        public IEnumerable<AccountDefinition> IncomeAccounts =>
            this.Accounts.Where(x => x.Type == AccountDefinitionType.Income);

        public IEnumerable<AccountDefinition> IncomeRemoteAccounts =>
            this.Accounts.Where(x => x.Type != AccountDefinitionType.Income);

        public IEnumerable<AccountDefinition> ExpenseAccounts =>
            this.Accounts.Where(x => x.Type == AccountDefinitionType.Expense);

        public IEnumerable<AccountDefinition> ExpenseRemoteAccounts =>
            this.Accounts.Where(x => x.Type != AccountDefinitionType.Expense);

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

        public int DebitIndex { get; set; } = -1;

        public int CreditIndex { get; set; } = -1;

        public int IncomeIndex { get; set; } = -1;

        public int IncomeRemoteIndex { get; set; } = -1;

        public int ExpenseIndex { get; set; } = -1;

        public int ExpenseRemoteIndex { get; set; } = -1;

        public int PageIndex { get; set; }

        public ICommand AddCommand => new RelayCommand(
            _ =>
            {
                var newBooking = new AccountingDataJournalBooking
                {
                    Date = this.Date.ToAccountingDate(),
                    ID = this.BookingIdentifier
                };
                var creditValue = new BookingValue
                {
                    Account = this.CreditAccount,
                    Text = this.BookingText,
                    Value = (long)Math.Round(this.BookingValue * 100)
                };
                var debitValue = creditValue.Clone();
                debitValue.Account = this.DebitAccount;
                newBooking.Credit = new List<BookingValue> { creditValue };
                newBooking.Debit = new List<BookingValue> { debitValue };
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

        protected override void OnInitialize()
        {
            base.OnInitialize();

            this.DisplayName = "Neue Buchung erstellen";
        }

        [SuppressMessage("Major Code Smell", "S109:Magic numbers should not be used", Justification = "<Pending>")]
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

            switch (this.PageIndex)
            {
            case 0: // debit/credit
                return this.CreditIndex >= 0 && this.DebitIndex >= 0 && this.CreditIndex != this.DebitIndex;
            case 1: // income
                return this.IncomeIndex >= 0 && this.IncomeRemoteIndex >= 0;
            case 2: // expense
                return this.ExpenseIndex >= 0 && this.ExpenseRemoteIndex >= 0;

            default:
                throw new InvalidOperationException();
            }
        }
    }
}
