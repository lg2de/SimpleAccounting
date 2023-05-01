// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System;
using System.Text.RegularExpressions;
using lg2de.SimpleAccounting.Model;

/// <summary>
///     Implements the view model to visualize single pattern for semi-automatic booking import.
/// </summary>
public class ImportPatternViewModel
{
    private string expression = string.Empty;

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
                this.expression = (new Regex(value)).ToString();
            }
            catch
            {
                throw new ArgumentException("The expression must be a valid regular expression.");
            }
        }
    }

    public double? Value { get; set; }

    public ulong AccountId { get; set; }

    public AccountDefinition? Account { get; set; }
}
