// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Xml.Serialization;
    using lg2de.SimpleAccounting.Extensions;

    public partial class AccountingData
    {
        internal const string DefaultXsiSchemaLocation = DefaultSchemaNamespace + " " + DefaultSchemaLocation;

        private const string DefaultSchemaNamespace = "https://lg2.de/SimpleAccounting/AccountingSchema";
        private const string DefaultSchemaLocation = "https://lg2de.github.io/SimpleAccounting/AccountingData.xsd";

        private string schema = DefaultXsiSchemaLocation;

        [XmlAttribute("schemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        [SuppressMessage(
            "Minor Code Smell",
            "S100:Methods and properties should be named in PascalCase",
            Justification = "fixed name")]
        public string xsiSchemaLocation
        {
            get => this.schema;
            set
            {
                this.schema = value;
                if (this.schema != DefaultXsiSchemaLocation)
                {
                    this.schema = DefaultXsiSchemaLocation;
                }
            }
        }

        internal IEnumerable<AccountDefinition> AllAccounts =>
            this.Accounts?.SelectMany(g => g.Account) ?? Enumerable.Empty<AccountDefinition>();

        internal static AccountingData GetTemplateProject()
        {
            var year = (ushort)DateTime.Now.Year;
            var defaultAccounts = new List<AccountDefinition>
            {
                new AccountDefinition { ID = 100, Name = "Bank account", Type = AccountDefinitionType.Asset },
                new AccountDefinition { ID = 400, Name = "Salary", Type = AccountDefinitionType.Income },
                new AccountDefinition { ID = 600, Name = "Food", Type = AccountDefinitionType.Expense },
                new AccountDefinition { ID = 990, Name = "Carryforward", Type = AccountDefinitionType.Carryforward }
            };
            var accountJournal = new AccountingDataJournal
            {
                Year = year.ToString(CultureInfo.InvariantCulture),
                DateStart = (uint)year * 10000 + 101,
                DateEnd = (uint)year * 10000 + 1231,
                Booking = new List<AccountingDataJournalBooking>()
            };
            return new AccountingData
            {
                Accounts = new List<AccountingDataAccountGroup>
                {
                    new AccountingDataAccountGroup { Name = "Default", Account = defaultAccounts }
                },
                Journal = new List<AccountingDataJournal> { accountJournal }
            };
        }

        internal AccountingData Clone()
        {
            var xml = this.Serialize();
            return Deserialize(xml);
        }

        internal bool Migrate()
        {
            var result = false;
            result |= this.MergeYearsIntoJournal();
            result |= this.RemoveEmptyElements();
            return result;
        }

        internal AccountingDataJournal CloseYear(
            AccountingDataJournal currentModelJournal, AccountDefinition carryForwardAccount)
        {
            currentModelJournal.Closed = true;

            var newYearJournal = new AccountingDataJournal
            {
                DateStart = currentModelJournal.DateStart + 10000,
                DateEnd = currentModelJournal.DateEnd + 10000,
                Booking = new List<AccountingDataJournalBooking>()
            };
            newYearJournal.Year = newYearJournal.DateStart.ToDateTime().Year.ToString(CultureInfo.InvariantCulture);
            this.Journal.Add(newYearJournal);

            ulong bookingId = 1;

            // Asset Accounts (Bestandskonten), Credit and Debit Accounts
            var accounts = this.AllAccounts.Where(
                a =>
                    a.Type == AccountDefinitionType.Asset
                    || a.Type == AccountDefinitionType.Credit
                    || a.Type == AccountDefinitionType.Debit);
            foreach (var account in accounts)
            {
                if (currentModelJournal.Booking == null)
                {
                    continue;
                }

                var creditAmount = currentModelJournal.Booking
                    .SelectMany(b => b.Credit.Where(x => x.Account == account.ID))
                    .Sum(x => x.Value);
                var debitAmount = currentModelJournal.Booking
                    .SelectMany(b => b.Debit.Where(x => x.Account == account.ID))
                    .Sum(x => x.Value);

                if (creditAmount == 0 && debitAmount == 0 || creditAmount == debitAmount)
                {
                    // nothing to do
                    continue;
                }

                var newBooking = new AccountingDataJournalBooking
                {
                    Date = newYearJournal.DateStart,
                    ID = bookingId,
                    Debit = new List<BookingValue>(),
                    Credit = new List<BookingValue>(),
                    Opening = true
                };
                newYearJournal.Booking.Add(newBooking);
                var newDebit = new BookingValue
                {
                    Value = Math.Abs(creditAmount - debitAmount), Text = $"Eröffnungsbetrag {bookingId}"
                };
                newBooking.Debit.Add(newDebit);
                var newCredit = new BookingValue { Value = newDebit.Value, Text = newDebit.Text };
                newBooking.Credit.Add(newCredit);
                if (creditAmount > debitAmount)
                {
                    newCredit.Account = account.ID;
                    newDebit.Account = carryForwardAccount.ID;
                }
                else
                {
                    newDebit.Account = account.ID;
                    newCredit.Account = carryForwardAccount.ID;
                }

                bookingId++;
            }

            return newYearJournal;
        }

        internal string GetAccountName(BookingValue entry)
        {
            var account = this.AllAccounts.Single(a => a.ID == entry.Account);
            return account.FormatName();
        }

        private bool MergeYearsIntoJournal()
        {
            if (this.Years == null)
            {
                return false;
            }

            var result = false;
            foreach (var year in this.Years)
            {
                this.Journal ??= new List<AccountingDataJournal>();

                string oldYearName = year.Name.ToString(CultureInfo.InvariantCulture);
                var journal = this.Journal.SingleOrDefault(x => x.Year == oldYearName);
                if (journal == null)
                {
                    journal = new AccountingDataJournal { Year = oldYearName };
                    this.Journal.Add(journal);
                }

                journal.DateStart = year.DateStart;
                journal.DateEnd = year.DateEnd;
                journal.Closed = year.Closed;

                result = true;
            }

            this.Years = null;

            return result;
        }

        private bool RemoveEmptyElements()
        {
            if (this.Accounts == null)
            {
                return false;
            }

            var anyAccountFixed = false;
            var accountsWithMapping = this.Accounts.SelectMany(x => x.Account).Where(x => x.ImportMapping != null);
            foreach (var account in accountsWithMapping)
            {
                if ((account.ImportMapping?.Columns?.Any() ?? false)
                    || (account.ImportMapping?.Patterns?.Any() ?? false))
                {
                    continue;
                }

                account.ImportMapping = null;
                anyAccountFixed = true;
            }

            return anyAccountFixed;
        }
    }

    public partial class AccountingDataJournalBooking
    {
        internal bool ContainsAccount(ulong accountNumber)
        {
            return this.Debit.Any(x => x.Account == accountNumber)
                   || this.Credit.Any(x => x.Account == accountNumber);
        }
    }

    public partial class AccountDefinition
    {
        internal string FormatName()
        {
            return $"{this.ID} ({this.Name})";
        }
    }

    public partial class BookingValue
    {
        internal BookingValue Clone()
        {
            return (BookingValue)this.MemberwiseClone();
        }
    }
}
