// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Extensions
{
    using System;
    using System.Collections.Generic;

    internal static class CollectionExtensions
    {
        public static void ClearAndDispose<T>(this Stack<T> stack)
            where T : IDisposable
        {
            foreach (var item in stack)
            {
                item.Dispose();
            }

            stack.Clear();
        }
    }
}
