// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Extensions
{
    /// <summary>
    ///     Implements extensions on <see cref="string"/>.
    /// </summary>
    internal static class StringExtensions
    {
        /// <summary>
        ///     Wraps the string according to specified max width.
        /// </summary>
        /// <param name="input">The string to be wrapped.</param>
        /// <param name="maxWidth">The maximum width per row.</param>
        /// <returns>The wrapped string.</returns>
        public static string Wrap(this string input, int maxWidth)
        {
            const double characterWidthFactor = 95.0 / 50;

            var result = input;
            var position = 0;
            
            while (position < result.Length)
            {
                var remainingText = result.Substring(position);
                if (remainingText.Length * characterWidthFactor <= maxWidth)
                {
                    return result;
                }

                var wrapPosition = (int)(maxWidth / characterWidthFactor);
                result = result.Insert(position + wrapPosition, "\n");
                position += wrapPosition + 1;
            }

            return result;
        }
    }
}
