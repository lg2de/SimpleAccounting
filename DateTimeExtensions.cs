// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

using System;

namespace lg2de.SimpleAccounting
{
    internal static class DateTimeExtensions
    {
        public static DateTime ToDateTime(this uint date)
        {
            return new DateTime((int)date / 10000, (int)(date / 100) % 100, (int)date % 100);
        }
    }
}
