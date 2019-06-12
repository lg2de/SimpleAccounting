// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting
{
    internal class BookingTemplate
    {
        public string Text { get; set; }

        public ulong Credit { get; set; }

        public ulong Debit { get; set; }
    }
}
