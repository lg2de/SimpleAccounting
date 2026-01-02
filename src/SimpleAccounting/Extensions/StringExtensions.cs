// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Extensions;

using System;
using System.Drawing;
using lg2de.SimpleAccounting.Abstractions;

/// <summary>
///     Implements extensions on <see cref="string"/>.
/// </summary>
internal static class StringExtensions
{
    private static readonly char[] WrapCharacters = " -/".ToCharArray();

    /// <param name="input">The string to be wrapped.</param>
    extension(string input)
    {
        /// <summary>
        ///     Wraps the string according to specified max width, font and device context.
        /// </summary>
        /// <param name="maxWidth">The maximum width per row.</param>
        /// <param name="font">The font to be used for size calculation.</param>
        /// <param name="graphics">The graphics providing rendering context.</param>
        /// <returns>The wrapped string.</returns>
        public string Wrap(double maxWidth, Font font, IGraphics graphics)
        {
            var result = input;
            var position = 0;

            while (position < result.Length)
            {
                var remainingText = result.Substring(position);
                var size = graphics.MeasureString(remainingText, font);
                if (size.Width <= maxWidth)
                {
                    // The remaining text fits into available space.
                    return result;
                }

                if (remainingText.Length == 1)
                {
                    // The last character must not be wrapped anymore.
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

        /// <summary>
        ///     Calculates the Levenshtein distance between two strings.
        /// </summary>
        /// <param name="target">The target string.</param>
        /// <returns>The Levenshtein distance between the two strings.</returns>
        public int LevenshteinDistance(string target)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.IsNullOrEmpty(target) ? 0 : target.Length;
            }

            if (string.IsNullOrEmpty(target))
            {
                return input.Length;
            }

            var sourceLength = input.Length;
            var targetLength = target.Length;
            var distance = new int[sourceLength + 1, targetLength + 1];

            for (var i = 0; i <= sourceLength; i++)
            {
                distance[i, 0] = i;
            }

            for (var j = 0; j <= targetLength; j++)
            {
                distance[0, j] = j;
            }

            for (var i = 1; i <= sourceLength; i++)
            {
                for (var j = 1; j <= targetLength; j++)
                {
                    var cost = target[j - 1] == input[i - 1] ? 0 : 1;
                    distance[i, j] = Math.Min(
                        Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                        distance[i - 1, j - 1] + cost);
                }
            }

            return distance[sourceLength, targetLength];
        }
    }
}
