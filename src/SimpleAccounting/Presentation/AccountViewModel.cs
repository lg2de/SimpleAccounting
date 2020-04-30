// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using Caliburn.Micro;
    using lg2de.SimpleAccounting.Model;

    public class AccountViewModel : Screen
    {
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

        public ICommand SaveCommand => new RelayCommand(
            _ => this.TryClose(true),
            _ => !string.IsNullOrWhiteSpace(this.Name)
                 && (this.IsValidIdentifierFunc?.Invoke(this.Identifier) ?? true));

        internal Func<ulong, bool>? IsValidIdentifierFunc { get; set; }

        internal AccountViewModel Clone()
        {
            return (AccountViewModel)this.MemberwiseClone();
        }
    }
}
