// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Windows.Input;
using Caliburn.Micro;
using lg2de.SimpleAccounting.Model;

namespace lg2de.SimpleAccounting.Presentation
{
    public class AccountViewModel : Screen
    {
        static AccountViewModel()
        {
            foreach (var type in Enum.GetValues(typeof(AccountingDataAccountType)))
            {
                Types.Add((AccountingDataAccountType)type);
            }
        }

        public static List<AccountingDataAccountType> Types { get; } = new List<AccountingDataAccountType>();

        public ulong Identifier { get; set; }

        public string Name { get; set; }

        public AccountingDataAccountType Type { get; set; }

        internal Func<ulong, bool> IsAvalidIdentifierFunc { get; set; }

        public ICommand SaveCommand => new RelayCommand(
            _ => this.TryClose(true),
            _ => !string.IsNullOrWhiteSpace(this.Name)
            && (this.IsAvalidIdentifierFunc?.Invoke(this.Identifier) ?? true));

        internal AccountViewModel Clone()
        {
            return this.MemberwiseClone() as AccountViewModel;
        }
    }
}
