// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Windows.Input;
    using Caliburn.Micro;
    using CsvHelper;
    using CsvHelper.Configuration;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Model;

    internal class ImportBookingsViewModel : Screen
    {
        private readonly IMessageBox messageBox;
        private readonly ShellViewModel parent;
        private readonly List<AccountDefinition> accounts;
        private ulong selectedAccountNumber;

        public ImportBookingsViewModel(
            IMessageBox messageBox,
            ShellViewModel parent,
            IEnumerable<AccountDefinition> accounts)
        {
            this.messageBox = messageBox;
            this.parent = parent;
            this.accounts = accounts.ToList();

            this.DisplayName = "Import von Kontodaten";
        }

        public IEnumerable<AccountDefinition> ImportAccounts => this.accounts
            .Where(a => a.ImportMapping.Columns.Any(x => x.Target == AccountDefinitionImportMappingColumnTarget.Date) && a.ImportMapping.Columns.Any(x => x.Target == AccountDefinitionImportMappingColumnTarget.Value));

        public DateTime RangeMin { get; internal set; }

        public DateTime RangMax { get; internal set; }

        public AccountingDataJournal Journal { get; internal set; }

        public ulong BookingNumber { get; internal set; }

        public ulong SelectedAccountNumber
        {
            get => this.selectedAccountNumber;
            set
            {
                if (this.selectedAccountNumber == value)
                {
                    return;
                }

                this.selectedAccountNumber = value;
                this.NotifyOfPropertyChange();
            }
        }

        public AccountDefinition SelectedAccount { get; set; }

        public ObservableCollection<ImportEntryViewModel> ImportData { get; }
            = new ObservableCollection<ImportEntryViewModel>();

        public ICommand LoadDataCommand => new RelayCommand(_ =>
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = null;
            try
            {
                openFileDialog = new System.Windows.Forms.OpenFileDialog
                {
                    Filter = "Booking data files (*.csv)|*.csv",
                    RestoreDirectory = true
                };

                if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                this.ImportData.Clear();

                // note, the stream is disposed by the reader
                var stream = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using (var reader = new StreamReader(stream, Encoding.GetEncoding(1252)))
                {
                    this.ImportBookings(reader, new Configuration());
                }

                if (!this.ImportData.Any())
                {
                    this.messageBox.Show($"No relevant data found in {openFileDialog.FileName}.", "Import");
                }
            }
            catch (IOException e)
            {
                this.messageBox.Show($"Failed to load file '{openFileDialog.FileName}':\n{e.Message}", "Import");
            }
            finally
            {
                openFileDialog?.Dispose();
            }
        }, _ => this.SelectedAccount != null);

        public ICommand BookAllCommand => new RelayCommand(
            _ => this.ProcessData(),
            _ => this.ImportData.All(x => x.RemoteAccount != null));

        public ICommand BookMappedCommand => new RelayCommand(
            _ => this.ProcessData(),
            _ => this.ImportData.Any(x => x.RemoteAccount != null));

        internal void ImportBookings(TextReader reader, Configuration configuration)
        {
            var dateField = this.SelectedAccount.ImportMapping.Columns
                .FirstOrDefault(x => x.Target == AccountDefinitionImportMappingColumnTarget.Date)?.Source;
            var nameField = this.SelectedAccount.ImportMapping.Columns
                .FirstOrDefault(x => x.Target == AccountDefinitionImportMappingColumnTarget.Name)?.Source;
            var textField = this.SelectedAccount.ImportMapping.Columns
                .FirstOrDefault(x => x.Target == AccountDefinitionImportMappingColumnTarget.Text);
            var valueField = this.SelectedAccount.ImportMapping.Columns
                .FirstOrDefault(x => x.Target == AccountDefinitionImportMappingColumnTarget.Value)?.Source;

            if (this.Journal != null)
            {
                var lastEntry = this.Journal.Booking
                    .Where(x => x.Credit.Any(c => c.Account == this.SelectedAccountNumber) || x.Debit.Any(c => c.Account == this.SelectedAccountNumber))
                    .OrderBy(x => x.Date)
                    .LastOrDefault();
                if (lastEntry != null)
                {
                    this.RangeMin = lastEntry.Date.ToDateTime() + TimeSpan.FromDays(1);
                }
            }

            using (var csv = new CsvReader(reader, configuration))
            {
                csv.Read();
                if (!csv.ReadHeader())
                {
                    return;
                }

                while (csv.Read())
                {
                    csv.TryGetField(dateField, out DateTime date);
                    if (date < this.RangeMin || date > this.RangMax)
                    {
                        continue;
                    }

                    // date and value are checked by RelayCommand
                    // name and text may be empty
                    csv.TryGetField<double>(valueField, out var value);
                    string name = string.Empty;
                    string text = string.Empty;
                    if (nameField != null)
                    {
                        csv.TryGetField<string>(nameField, out name);
                    }

                    if (textField != null)
                    {
                        csv.TryGetField<string>(textField.Source, out text);
                        if (!string.IsNullOrEmpty(textField.IgnorePattern))
                        {
                            text = Regex.Replace(text, textField?.IgnorePattern, string.Empty);
                        }
                    }

                    var item = new ImportEntryViewModel
                    {
                        Date = date,
                        Accounts = this.accounts,
                        Identifier = this.BookingNumber++,
                        Name = name,
                        Text = text,
                        Value = value
                    };

                    var longValue = (long)(value * 100);
                    foreach (var importMapping in this.SelectedAccount.ImportMapping.Patterns)
                    {
                        if (!Regex.IsMatch(text, importMapping.Expression))
                        {
                            // mapping does not match
                            continue;
                        }

                        if (importMapping.ValueSpecified && longValue != importMapping.Value)
                        {
                            // mapping does not match
                            continue;
                        }

                        // use first match
                        item.RemoteAccount = this.accounts.SingleOrDefault(a => a.ID == importMapping.AccountID);
                        break;
                    }

                    this.ImportData.Add(item);
                }
            }
        }

        internal void ProcessData()
        {
            foreach (var importing in this.ImportData)
            {
                if (importing.RemoteAccount == null)
                {
                    // mapping missing - abort
                    break;
                }

                var newBooking = new AccountingDataJournalBooking
                {
                    Date = importing.Date.ToAccountingDate(),
                    ID = importing.Identifier
                };
                var creditValue = new BookingValue
                {
                    Value = (int)Math.Abs(Math.Round(importing.Value * 100))
                };

                // build booking text from name and/or text
                if (string.IsNullOrWhiteSpace(importing.Text))
                {
                    creditValue.Text = importing.Name;
                }
                else if (string.IsNullOrWhiteSpace(importing.Name))
                {
                    creditValue.Text = importing.Text;
                }
                else
                {
                    creditValue.Text = $"{importing.Name} - {importing.Text}";
                }

                // start debit with clone of credit
                var debitValue = creditValue.Clone();

                // set accounts according to the value
                if (importing.Value > 0)
                {
                    creditValue.Account = importing.RemoteAccount.ID;
                    debitValue.Account = this.SelectedAccountNumber;
                }
                else
                {
                    creditValue.Account = this.SelectedAccountNumber;
                    debitValue.Account = importing.RemoteAccount.ID;
                }

                newBooking.Credit = new List<BookingValue> { creditValue };
                newBooking.Debit = new List<BookingValue> { debitValue };
                this.parent.AddBooking(newBooking);
            }

            this.TryClose(null);
        }
    }
}
