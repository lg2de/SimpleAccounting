// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Presentation;

/// <summary>
///     Implements extensions on <see cref="SplitBookingViewModel"/>.
/// </summary>
internal static class SplitBookingViewModelExtensions
{
    private const double Tolerance = 0.001;

    public static bool IsConsistent(
        this IList<SplitBookingViewModel> list, (double ExpectedSum, ulong RemoteAccountNumber) booking)
    {
        if (!list.Any())
        {
            return true;
        }

        if (list.Any(
                x => string.IsNullOrWhiteSpace(x.BookingText)
                     || x.BookingValue <= 0
                     || x.AccountIndex < 0
                     || x.AccountNumber == booking.RemoteAccountNumber))
        {
            return false;
        }

        var splitSum = list.Sum(x => x.BookingValue);
        return Math.Abs(splitSum - booking.ExpectedSum) <= Tolerance;
    }

    public static BookingValue ToBooking(this SplitBookingViewModel viewModel)
    {
        return new BookingValue
        {
            Account = viewModel.AccountNumber,
            Text = viewModel.BookingText,
            Value = viewModel.BookingValue.ToModelValue()
        };
    }

    public static SplitBookingViewModel ToSplitModel(this BookingValue value)
    {
        return new SplitBookingViewModel
        {
            AccountNumber = value.Account, BookingText = value.Text, BookingValue = value.Value.ToViewModel()
        };
    }
}
