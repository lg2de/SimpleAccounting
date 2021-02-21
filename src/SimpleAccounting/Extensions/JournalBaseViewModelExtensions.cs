// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Extensions
{
    using System;
    using System.Collections.Generic;
    using JetBrains.Annotations;
    using lg2de.SimpleAccounting.Presentation;

    /// <summary>
    ///     Implements extensions on <see cref="JournalItemBaseViewModel"/>.
    /// </summary>
    public static class JournalBaseViewModelExtensions
    {
        /// <summary>
        ///     Updates the alternating highlight based on the booking identifier.
        /// </summary>
        /// <param name="journal"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void UpdateRowHighlighting([NotNull] this IEnumerable<JournalItemBaseViewModel> journal)
        {
            if (journal == null)
            {
                throw new ArgumentNullException(nameof(journal));
            }

            bool isEven = true;
            ulong lastIdentifier = 0;
            foreach (var entry in journal)
            {
                if (entry.Identifier != lastIdentifier)
                {
                    isEven = !isEven;
                    lastIdentifier = entry.Identifier;
                }

                entry.IsEvenRow = entry.Identifier > 0 && isEven;
            }
        }
    }
}
