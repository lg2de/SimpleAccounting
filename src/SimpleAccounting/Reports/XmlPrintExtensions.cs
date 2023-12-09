// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Reports;

using System;
using System.Text.RegularExpressions;
using lg2de.SimpleAccounting.Properties;

/// <summary>
///     Defines extensions for the <see cref="XmlPrinter"/>.
/// </summary>
internal static partial class XmlPrintExtensions
{
    private static readonly Regex ReferenceTextExpression = ReferenceTextRegex();

    public static string Translate(this string input)
    {
        var result = input;
        var match = ReferenceTextExpression.Match(result);
        while (match.Success)
        {
            string referenceText = match.Groups["ReferenceText"].Value;
            var translatedText = Resources.ResourceManager.GetString(referenceText) ?? referenceText;
            result = result.Replace(match.Value, translatedText, StringComparison.Ordinal);
            match = ReferenceTextExpression.Match(result);
        }

        return result;
    }

    [GeneratedRegex("@(?<ReferenceText>[a-zA-Z0-9_]+)@", RegexOptions.Compiled)]
    private static partial Regex ReferenceTextRegex();
}
