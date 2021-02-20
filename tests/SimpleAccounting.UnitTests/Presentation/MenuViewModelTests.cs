// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using FluentAssertions;
    using FluentAssertions.Extensions;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Presentation;
    using lg2de.SimpleAccounting.Properties;
    using NSubstitute;
    using Xunit;

    public class MenuViewModelTests
    {
        [UIFact]
        public async Task OpenProjectCommand_HappyPath_BusyIndicatorChanged()
        {
            var settings = new Settings();
            var fileSystem = Substitute.For<IFileSystem>();
            var projectData = new ProjectData(settings, null!, null!, fileSystem, null!);
            var accounts = new AccountsViewModel(null!, projectData);
            var dialogs = Substitute.For<IDialogs>();
            var sut = new MenuViewModel(settings, projectData, accounts, null!, null!, dialogs);
            long counter = 0;
            var tcs = new TaskCompletionSource<bool>();
            dialogs.ShowOpenFileDialog(Arg.Any<string>()).Returns((DialogResult.OK, "dummy"));

            // Because awaiting "ExecuteUIThread" does not really await the action
            // we need to wait for two property changed events.
            var values = new List<bool>();
            sut.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName != "IsBusy")
                {
                    return;
                }

                values.Add(sut.IsBusy);
                if (Interlocked.Increment(ref counter) == 2)
                {
                    tcs.SetResult(true);
                }
            };

            sut.OpenProjectCommand.Execute(null);

            await tcs.Awaiting(x => x.Task).Should().CompleteWithinAsync(1.Seconds(), "IsBusy should change twice");
            values.Should().Equal(true, false);
        }

    }
}
