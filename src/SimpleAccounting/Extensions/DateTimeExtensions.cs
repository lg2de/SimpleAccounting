// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Extensions
{
    using System;

    internal static class DateTimeExtensions
    {
        private const int YearFactor = 10000;
        private const int MonthFactor = 100;

        public static DateTime ToDateTime(this uint date)
        {
            return new DateTime(
                (int)date / YearFactor,
                (int)(date / MonthFactor) % MonthFactor,
                (int)date % MonthFactor);
        }

        public static uint ToAccountingDate(this DateTime date)
        {
            return (uint)(date.Year * YearFactor + date.Month * MonthFactor + date.Day);
        }
    }
}
