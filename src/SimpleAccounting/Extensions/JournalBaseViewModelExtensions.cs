// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Extensions
{
    using System;
    using System.Collections.Generic;
    using JetBrains.Annotations;
    using lg2de.SimpleAccounting.Presentation;

    public static class JournalBaseViewModelExtensions
    {
        public static void UpdateRowHighlighting([NotNull] this IEnumerable<JournalBaseViewModel> journal)
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

                entry.IsEvenRow = isEven;
            }
        }
    }
}
