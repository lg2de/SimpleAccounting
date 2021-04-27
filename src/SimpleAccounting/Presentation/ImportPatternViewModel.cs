// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using lg2de.SimpleAccounting.Model;

    public class ImportPatternViewModel
    {
        private string expression;

        public ImportPatternViewModel(IList<AccountDefinition> accounts, string expression)
        {
            this.Accounts = accounts;
            this.Account = accounts.First();
            this.expression = expression;
        }

        public IList<AccountDefinition> Accounts { get; }

        public string Expression
        {
            get => this.expression;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("The expression must not be empty.");
                }

                try
                {
                    var _ = new Regex(value);
                }
                catch
                {
                    throw new ArgumentException("The expression must be a valid regular expression.");
                }

                this.expression = value;
            }
        }

        public double? Value { get; set; }

        public AccountDefinition Account { get; set; }
    }
}
