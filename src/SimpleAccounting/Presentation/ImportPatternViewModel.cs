// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using System.Text.RegularExpressions;

    public class ImportPatternViewModel
    {
        private string expression;

        public ImportPatternViewModel(string expression)
        {
            this.expression = expression;
        }

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

        public ulong AccountNumber { get; set; }
    }
}
