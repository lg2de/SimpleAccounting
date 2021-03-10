// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Input;
    using Caliburn.Micro;
    using lg2de.SimpleAccounting.Infrastructure;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Properties;

    public class CloseYearViewModel : Screen
    {
        private readonly AccountingDataJournal currentYear;

        public CloseYearViewModel(AccountingDataJournal currentYear)
        {
            this.currentYear = currentYear ?? throw new ArgumentNullException(nameof(currentYear));

            this.TextOptions = new List<TextOptionViewModel>
            {
                new TextOptionViewModel(1, "A"),
                new TextOptionViewModel(2, "B")
            };
            this.TextOption = this.TextOptions.First(); // TODO store selection in user settings
        }

        public string InstructionText { get; private set; } = string.Empty;

        public IList<AccountDefinition> Accounts { get; } = new List<AccountDefinition>();

        public AccountDefinition? RemoteAccount { get; set; }

        public IList<TextOptionViewModel> TextOptions { get; }

        public TextOptionViewModel TextOption { get; set; }

        public ICommand CloseYearCommand => new RelayCommand(
            _ => this.TryClose(true),
            _ => this.RemoteAccount != null);

        protected override void OnInitialize()
        {
            base.OnInitialize();

            this.DisplayName = Resources.Header_CloseYear;
            this.InstructionText = string.Format(
                CultureInfo.CurrentUICulture, Resources.Question_CloseYearX, this.currentYear.Year);

            this.RemoteAccount = this.Accounts.FirstOrDefault();
        }
    }

    public class TextOptionViewModel
    {
        public TextOptionViewModel(int option, string name)
        {
            this.Option = option;
            this.Name = name;
        }

        public int Option { get; }

        public string Name { get; }
    }
}
