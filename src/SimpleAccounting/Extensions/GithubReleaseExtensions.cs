// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Octokit;

    internal static class GithubReleaseExtensions
    {
        public static Release GetNewRelease(this IEnumerable<Release> releases, string currentVersion)
        {
            bool isPreRelease = currentVersion.Contains("beta", StringComparison.InvariantCultureIgnoreCase);
            var candidates = releases.Where(x => !x.Draft);
            if (!isPreRelease)
            {
                candidates = candidates.Where(x => !x.Prerelease);
            }

            return candidates.SingleOrDefault(
                x => x.Assets != null && x.Assets.Any() && IsGreater(x.TagName, currentVersion));

            static bool IsGreater(string tag, string current)
            {
                if (tag == current)
                {
                    return false;
                }

                var tagElements = tag.Split('-');
                var tagMain = tagElements[0];
                var tagBeta = tagElements.Length > 1 ? tagElements[1] : string.Empty;
                var currentElements = current.Split('-');
                var currentMain = currentElements[0];
                var currentBeta = currentElements.Length > 1 ? currentElements[1] : string.Empty;

                if (string.Compare(tagMain, currentMain, StringComparison.Ordinal) > 0)
                {
                    // new release
                    return true;
                }

                if (tagMain == currentMain)
                {
                    // same target version -> check beta
                    if (string.IsNullOrEmpty(tagBeta))
                    {
                        // update from beta to release?
                        return !string.IsNullOrEmpty(currentBeta);
                    }

                    return string.Compare(tagBeta, currentBeta, StringComparison.Ordinal) > 0;
                }

                // older release version
                return false;
            }
        }

    }
}
