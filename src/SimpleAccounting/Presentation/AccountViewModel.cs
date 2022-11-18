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
    using lg2de.SimpleAccounting.Properties;

    /// <summary>
    ///     Implements the view model for a single account.
    /// </summary>
    internal class AccountViewModel : Screen
    {
        private bool isImportActive;

        static AccountViewModel()
        {
            ResetTypesLazy();
        }

        public static IDictionary<AccountDefinitionType, string> Types { get; } =
            new Dictionary<AccountDefinitionType, string>();

        public ulong Identifier { get; set; }

        public string Name { get; set; } = string.Empty;

        public IEnumerable<AccountingDataAccountGroup> Groups { get; set; }
            = Enumerable.Empty<AccountingDataAccountGroup>();

        public AccountingDataAccountGroup? Group { get; set; }

        public AccountDefinitionType Type { get; set; }

        public string TypeName => Types[this.Type];

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

        public IList<AccountDefinition> ImportRemoteAccounts { get; set; } = new List<AccountDefinition>();

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

                // check whether the identifier is valid (e.g. not duplicated)
                if (this.IsValidIdentifierFunc?.Invoke(this.Identifier) == false)
                {
                    return false;
                }

                if (!this.IsImportActive)
                {
                    // import is not active -> done
                    return true;
                }

                if (string.IsNullOrWhiteSpace(this.ImportDateSource))
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(this.ImportValueSource))
                {
                    return false;
                }

                return this.ImportPatterns.All(x => !string.IsNullOrWhiteSpace(x.Expression) && x.Account != null);
            });

        internal Func<ulong, bool>? IsValidIdentifierFunc { get; set; }

        internal static void ResetTypesLazy()
        {
            Types.Clear();
            foreach (var type in Enum.GetValues(typeof(AccountDefinitionType)).Cast<AccountDefinitionType>())
            {
                var localizedType = Resources.ResourceManager.GetString($"AccountType_{type}") ?? $"<{type}>";
                Types[type] = localizedType;
            }
        }

        internal AccountViewModel Clone()
        {
            return (AccountViewModel)this.MemberwiseClone();
        }
    }
}
