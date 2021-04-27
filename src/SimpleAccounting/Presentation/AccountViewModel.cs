// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows.Input;
    using Caliburn.Micro;
    using lg2de.SimpleAccounting.Infrastructure;
    using lg2de.SimpleAccounting.Model;

    /// <summary>
    ///     Implements the view model for a single account.
    /// </summary>
    internal class AccountViewModel : Screen
    {
        private bool isImportActive;

        static AccountViewModel()
        {
            foreach (var type in Enum.GetValues(typeof(AccountDefinitionType)))
            {
                Types.Add((AccountDefinitionType)type!);
            }
        }

        public static IList<AccountDefinitionType> Types { get; } = new List<AccountDefinitionType>();

        public ulong Identifier { get; set; }

        public string Name { get; set; } = string.Empty;

        public IEnumerable<AccountingDataAccountGroup> Groups { get; set; }
            = Enumerable.Empty<AccountingDataAccountGroup>();

        public AccountingDataAccountGroup? Group { get; set; }

        public AccountDefinitionType Type { get; set; }

        public bool IsActivated { get; set; } = true;

        public bool IsImportActive
        {
            get => this.isImportActive;
            set
            {
                if (value == this.isImportActive)
                {
                    return;
                }

                this.isImportActive = value;
                this.NotifyOfPropertyChange();
                this.NotifyOfPropertyChange(nameof(this.SaveCommand));
            }
        }

        public string? ImportDateSource { get; set; }

        public string? ImportDateIgnorePattern { get; set; }

        public string? ImportNameSource { get; set; }

        public string? ImportNameIgnorePattern { get; set; }

        public string? ImportTextSource { get; set; }

        public string? ImportTextIgnorePattern { get; set; }

        public string? ImportValueSource { get; set; }

        public string? ImportValueIgnorePattern { get; set; }

        public ObservableCollection<ImportPatternViewModel> ImportPatterns { get; set; } =
            new ObservableCollection<ImportPatternViewModel>();

        public ICommand SaveCommand => new RelayCommand(
            _ => this.TryClose(true),
            _ =>
            {
                if (string.IsNullOrWhiteSpace(this.Name))
                {
                    return false;
                }

                if (this.IsImportActive
                    && (string.IsNullOrWhiteSpace(this.ImportDateSource) ||
                        string.IsNullOrWhiteSpace(this.ImportValueSource)))
                {
                    return false;
                }

                return this.IsValidIdentifierFunc?.Invoke(this.Identifier) ?? true;
            });

        internal Func<ulong, bool>? IsValidIdentifierFunc { get; set; }

        internal AccountViewModel Clone()
        {
            return (AccountViewModel)this.MemberwiseClone();
        }
    }
}
