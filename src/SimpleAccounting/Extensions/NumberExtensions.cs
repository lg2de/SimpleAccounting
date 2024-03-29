﻿// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Extensions;

using System;
using System.Globalization;

internal static class NumberExtensions
{
    private const double ConversionFactor = 100.0;

    public static string FormatCurrency(this double value)
    {
        // formats the specified value as currency (without currency symbol)
        return value.ToString("0.00", CultureInfo.CurrentCulture);
    }

    public static string FormatCurrency(this long value)
    {
        return (value / ConversionFactor).FormatCurrency();
    }

    public static long ToModelValue(this double value)
    {
        return (long)Math.Round(value * ConversionFactor);
    }

    public static double ToViewModel(this long value)
    {
        return value / ConversionFactor;
    }
}
