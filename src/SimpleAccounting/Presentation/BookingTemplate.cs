// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    internal class BookingTemplate
    {
        public string Text { get; set; } = string.Empty;

        public ulong Credit { get; set; }

        public ulong Debit { get; set; }

        public double Value { get; set; }
    }
}
