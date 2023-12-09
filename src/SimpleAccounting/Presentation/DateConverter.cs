// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System;
using System.Globalization;
using System.Windows.Data;

/// <summary>
///     Implements a converter for <see cref="DateTime"/> into date only supporting <langword>null</langword>.
/// </summary>
public class DateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        const int firstYear = 1900;
        return value is not DateTime dateTime || dateTime.Year < firstYear
            ? null
            : dateTime.ToString("d", culture);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
