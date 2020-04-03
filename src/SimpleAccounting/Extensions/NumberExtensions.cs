// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Extensions
{
    using System;

    public static class NumberExtensions
    {
        private const double ConversionFactor = 100.0;

        public static string FormatCurrency(this double value, IFormatProvider formatProvider)
        {
            // formats the specified value as currency (without currency symbol)
            return value.ToString("0.00", formatProvider);
        }

        public static string FormatCurrency(this long value, IFormatProvider formatProvider)
        {
            return (value / ConversionFactor).FormatCurrency(formatProvider);
        }
    }
}
