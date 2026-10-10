// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows.Data;
using lg2de.SimpleAccounting.Extensions;

/// <summary>
///     Formats a value with the optional currency symbol according to the culture.
/// </summary>
/// <remarks>
///     The first value is the value to be formatted.
///     The second value is the currency symbol, or <c>null</c> to format the value without currency.
///     With the parameter <see cref="HideZeroParameter" />, zero values are formatted as empty text.
/// </remarks>
public class CurrencyValueConverter : IMultiValueConverter
{
    public const string HideZeroParameter = "HideZero";

    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length == 0 || values[0] is not double value)
        {
            return string.Empty;
        }

        if (value == 0 && HideZeroParameter.Equals(parameter))
        {
            return string.Empty;
        }

        return values.Length > 1 && values[1] is string { Length: > 0 } currencySymbol
            ? value.FormatCurrency(currencySymbol, culture)
            : value.ToString("0.00", culture);
    }

    [ExcludeFromCodeCoverage(Justification = "This method is not used.")]
    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
