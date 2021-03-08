// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Extensions
{
    using System.Drawing;
    using System.Windows.Forms;

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
        /// <param name="font"></param>
        /// <param name="printFactor"></param>
        /// <returns>The wrapped string.</returns>
        public static string Wrap(this string input, double maxWidth, Font font, double printFactor)
        {
            var result = input;
            var position = 0;
            
            while (position < result.Length)
            {
                var remainingText = result.Substring(position);
                if (GetWidth(remainingText) <= maxWidth)
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
                    var wrapPosition = remainingText.LastIndexOfAny(" -/".ToCharArray()) + 1;
                    if (wrapPosition <= 0)
                    {
                        // no wrap character available, just break in word
                        wrapPosition = remainingText.Length - 1;
                    }

                    if (wrapPosition > 1)
                    {
                        remainingText = remainingText.Substring(0, wrapPosition);
                        if (GetWidth(remainingText) > maxWidth)
                        {
                            remainingText = remainingText.Substring(0, wrapPosition - 1);
                            continue;
                        }
                    }

                    if (position + wrapPosition + 1 < result.Length)
                    {
                        result = result.Insert(position + wrapPosition, "\n");
                        position += wrapPosition + 1;
                    }
                    else
                    {
                        // ???
                        position = result.Length;
                    }

                    return;
                }
            }

            double GetWidth(string text)
            {
                var size = TextRenderer.MeasureText(text, font);
                var width = size.Width / printFactor;
                return width;
            }
        }
    }
}
