// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.IntegrationTests.Infrastructure;

using System;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Infrastructure;
using NSubstitute;
using Xunit;

public class ApplicationUpdateTests
{
    [Fact]
    public async Task GetAllReleasesAsync_ReturnKnownVersions()
    {
        var dialogs = Substitute.For<IDialogs>();
        var windowsManager = Substitute.For<IWindowManager>();
        var fileSystem = Substitute.For<IFileSystem>();
        var httpClient = Substitute.For<IHttpClient>();
        var processApi = Substitute.For<IProcess>();
        var sut = new ApplicationUpdate(dialogs, windowsManager, fileSystem, httpClient, processApi, null!);

        var task = sut.GetAllReleasesAsync();
        (await this.Awaiting(_ => task).Should().CompleteWithinAsync(10.Seconds()))
            .Which.Should().Contain(
                r => r.TagName == "2.0.0"
                     && r.Assets.Any(
                         a => a.BrowserDownloadUrl.EndsWith("SimpleAccounting.zip", StringComparison.Ordinal)));
    }
}
