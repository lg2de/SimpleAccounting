// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.IntegrationTests.Presentation
{
    using System.Threading;
    using System.Threading.Tasks;
    using Caliburn.Micro;
    using FluentAssertions;
    using FluentAssertions.Extensions;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Infrastructure;
    using lg2de.SimpleAccounting.Presentation;
    using lg2de.SimpleAccounting.Properties;
    using lg2de.SimpleAccounting.Reports;
    using NSubstitute;
    using Xunit;

    public class ShellViewModelTests
    {
        [UIFact]
        public async Task InvokeLoadProjectFile_XXX()
        {
            IWindowManager windowManager = Substitute.For<IWindowManager>();
            IReportFactory reportFactory = Substitute.For<IReportFactory>();
            IApplicationUpdate applicationUpdate = Substitute.For<IApplicationUpdate>();
            IMessageBox messageBox = Substitute.For<IMessageBox>();
            IFileSystem fileSystem = Substitute.For<IFileSystem>();
            IProcess processApi = Substitute.For<IProcess>();
            var sut = new ShellViewModel(
                windowManager, reportFactory, applicationUpdate, messageBox, fileSystem, processApi)
            {
                Settings = new Settings()
            };
            long counter = 0;
            var tcs = new TaskCompletionSource<bool>();
            sut.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName != "IsBusy")
                {
                    return;
                }

                if (Interlocked.Increment(ref counter) == 2)
                {
                    tcs.SetResult(true);
                }
            };

            sut.InvokeLoadProjectFile("dummy");

            await tcs.Awaiting(x => x.Task).Should().CompleteWithinAsync(1.Seconds(), "IsBusy should change twice");
        }
    }
}
