// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Extensions;

using System;

/// <summary>
///     Implements extensions on <see cref="DateTime"/>.
/// </summary>
internal static class DateTimeExtensions
{
    private const int YearFactor = 10000;
    private const int MonthFactor = 100;

    public static DateTime ToDateTime(this uint date)
    {
        // converts from date format yyyymmdd (as uint) into Date(Time) instance
        return new DateTime(
            (int)date / YearFactor,
            (int)(date / MonthFactor) % MonthFactor,
            (int)date % MonthFactor);
    }

    public static uint ToAccountingDate(this DateTime date)
    {
        // converts from Date(Time) instance into date formatted as yyyymmdd (uint)
        return (uint)(date.Year * YearFactor + date.Month * MonthFactor + date.Day);
    }
}
