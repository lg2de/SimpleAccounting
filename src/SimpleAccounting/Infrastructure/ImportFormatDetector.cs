// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Infrastructure;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
///     Detects the culture of numbers and dates in import files.
/// </summary>
internal static partial class ImportFormatDetector
{
    /// <summary>
    ///     Gets the names of the cultures supported for import files.
    /// </summary>
    public static IReadOnlyList<string> SupportedCultures { get; } = ["de-DE", "en-US", "fr-FR"];

    /// <summary>
    ///     Detects the culture matching the specified dates and values.
    /// </summary>
    /// <param name="dates">The texts of the date column.</param>
    /// <param name="values">The texts of the value column.</param>
    /// <param name="preferredCulture">The culture preferred if the texts match several cultures.</param>
    /// <returns>The detected culture or <see langword="null" /> if the texts do not match any supported culture.</returns>
    public static CultureInfo? Detect(
        IReadOnlyCollection<string> dates, IEnumerable<string> values, CultureInfo preferredCulture)
    {
        // the decimal separator is the last separator followed by one or two digits only, e.g. "-1.234,56"
        var decimalSeparator = GetMostFrequent(values.Select(x => DecimalSeparatorRegex().Match(x.Trim())));

        // the date separator is relevant for day-first or month-first dates only, ISO dates match all cultures
        var dateSeparator = GetMostFrequent(dates.Select(x => DateSeparatorRegex().Match(x.Trim())));

        var nonEmptyDates = dates.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        return SupportedCultures
            .Select(CultureInfo.GetCultureInfo)
            .OrderByDescending(x =>
                x.TwoLetterISOLanguageName == preferredCulture.TwoLetterISOLanguageName)
            .FirstOrDefault(IsMatching);

        bool IsMatching(CultureInfo culture)
        {
            if (decimalSeparator != null && culture.NumberFormat.NumberDecimalSeparator != decimalSeparator)
            {
                return false;
            }

            if (dateSeparator != null && culture.DateTimeFormat.DateSeparator != dateSeparator)
            {
                return false;
            }

            return nonEmptyDates.TrueForAll(x => DateTime.TryParse(x, culture, DateTimeStyles.None, out _));
        }
    }

    private static string? GetMostFrequent(IEnumerable<Match> matches)
    {
        return matches
            .Where(x => x.Success)
            .GroupBy(x => x.Groups[1].Value)
            .OrderByDescending(x => x.Count())
            .Select(x => x.Key)
            .FirstOrDefault();
    }

    [GeneratedRegex(@"([.,])\d{1,2}$")]
    private static partial Regex DecimalSeparatorRegex();

    [GeneratedRegex(@"^\d{1,2}([./-])\d{1,2}\1\d{2,4}")]
    private static partial Regex DateSeparatorRegex();
}
