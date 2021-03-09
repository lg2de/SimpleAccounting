// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Extensions
{
    using System.Drawing;
    using lg2de.SimpleAccounting.Abstractions;

    /// <summary>
    ///     Implements extensions on <see cref="string"/>.
    /// </summary>
    internal static class StringExtensions
    {
        private static readonly char[] WrapCharacters = " -/".ToCharArray();

        /// <summary>
        ///     Wraps the string according to specified max width, font and device context.
        /// </summary>
        /// <param name="input">The string to be wrapped.</param>
        /// <param name="maxWidth">The maximum width per row.</param>
        /// <param name="font">The font to be used for size calculation.</param>
        /// <param name="graphics">The graphics providing rendering context.</param>
        /// <returns>The wrapped string.</returns>
        public static string Wrap(
            this string input, double maxWidth, Font font, IGraphics graphics)
        {
            var result = input;
            var position = 0;
            
            while (position < result.Length)
            {
                var remainingText = result.Substring(position);
                var size = graphics.MeasureString(remainingText, font);
                if (size.Width <= maxWidth)
                {
                    return result;
                }

                SearchPosition(remainingText);
            }

            return result;

            void SearchPosition(string remainingText)
            {
                while (true)
                {
                    var lastPosition = remainingText.Length - 1;
                    var latestWrapPosition = lastPosition - 1;
                    var wrapPosition = remainingText.LastIndexOfAny(WrapCharacters, latestWrapPosition) + 1;
                    if (wrapPosition <= 0)
                    {
                        // no wrap character available, just break in word
                        wrapPosition = remainingText.Length - 1;
                    }

                    if (wrapPosition > 1)
                    {
                        // wrap position identified
                        // check whether remaining text fits into available space
                        remainingText = remainingText.Substring(0, wrapPosition);
                        var size = graphics.MeasureString(remainingText, font);
                        if (size.Width > maxWidth)
                        {
                            // search for another wrap position
                            continue;
                        }
                    }

                    // remaining text fits
                    // insert line wrap at identified position
                    result = result.Insert(position + wrapPosition, "\n");
                    position += wrapPosition + 1;

                    return;
                }
            }
        }
    }
}
