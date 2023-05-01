// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Extensions;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using lg2de.SimpleAccounting.Extensions;
using Octokit;
using Xunit;

public class GithubReleaseExtensionTests
{
    [Theory]
    [InlineData("2.0.0", "2.0.0", null)]
    [InlineData("2.0.0", "2.0.1", "2.0.1")]
    [InlineData("2.0.0", "2.1.0", "2.1.0")]
    [InlineData("2.1.0", "2.0.0", null)]
    [InlineData("2.0.0-beta1", "2.0.0-beta1", null)]
    [InlineData("2.0.0-beta1", "2.0.0-beta2", "2.0.0-beta2")]
    [InlineData("2.0.0-beta1", "2.0.1-beta1", "2.0.1-beta1")]
    [InlineData("2.0.0-beta2", "2.0.0-beta1", null)]
    [InlineData("2.0.0", "2.0.0-beta1", null)] // release is greater than beta
    [InlineData("2.0.0-beta1", "2.0.0", "2.0.0")] // update to release
    [InlineData("2.0.0", "2.0.1-beta1", null)] // do not update to beta
    public void GetNewRelease_TestScenarios(
        string currentVersion,
        string availableVersion,
        string expectedVersion)
    {
        var result = CreateRelease(availableVersion).GetNewRelease(currentVersion);

        if (string.IsNullOrEmpty(expectedVersion))
        {
            result.Should().BeNull();
        }
        else
        {
            result.Should().BeEquivalentTo(new { TagName = expectedVersion });
        }
    }

    [Fact]
    public void GetNewRelease_AssetNotAvailable_VersionIgnored()
    {
        var releases = CreateRelease("2.1.0", addAsset: false);
        var result = releases.GetNewRelease("2.0.0");

        result.Should().BeNull();
    }

    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    internal static IReadOnlyList<Release> CreateRelease(string tag, bool addAsset = true)
    {
        Type releaseType = typeof(Release);
        var tagProperty = releaseType.GetProperty(nameof(Release.TagName));
        var preReleaseProperty = releaseType.GetProperty(nameof(Release.Prerelease));
        var assetsProperty = releaseType.GetProperty(nameof(Release.Assets));
        var release = new Release();
        tagProperty.SetValue(release, tag);
        if (tag.Contains("beta", StringComparison.Ordinal))
        {
            preReleaseProperty.SetValue(release, true);
        }

        if (addAsset)
        {
            assetsProperty.SetValue(release, new List<ReleaseAsset> { new TestingRelease("x.zip") });
        }

        return new List<Release> { release };
    }

    private class TestingRelease : ReleaseAsset
    {
        public TestingRelease(string name)
            : base(
                url: string.Empty, id: 1, nodeId: string.Empty, name, label: string.Empty, state: string.Empty,
                contentType: string.Empty, size: 0, downloadCount: 0, createdAt: DateTimeOffset.MinValue,
                updatedAt: DateTimeOffset.MinValue, browserDownloadUrl: string.Empty, new Author())
        {
        }
    }
}
