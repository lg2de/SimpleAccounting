// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

using System;
using System.Windows.Input;
using Caliburn.Micro;

namespace lg2de.SimpleAccounting.Presentation
{
    public class AccountViewModel : Screen
    {
        public ulong Identifier { get; set; }

        public string Name { get; set; }

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
