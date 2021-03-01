// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Forms;
    using Caliburn.Micro;
    using FluentAssertions;
    using FluentAssertions.Execution;
    using FluentAssertions.Extensions;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Infrastructure;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Presentation;
    using lg2de.SimpleAccounting.Properties;
    using lg2de.SimpleAccounting.Reports;
    using NSubstitute;
    using Xunit;
    using MessageBoxOptions = System.Windows.MessageBoxOptions;

    public class MenuViewModelTests
    {
        [UIFact]
        public async Task OpenProjectCommand_HappyPath_BusyIndicatorChanged()
        {
            var settings = new Settings();
            var fileSystem = Substitute.For<IFileSystem>();
            var projectData = new ProjectData(settings, null!, null!, fileSystem, null!);
            var dialogs = Substitute.For<IDialogs>();
            var busy = new BusyControlModel();
            var sut = new MenuViewModel(settings, projectData, busy, null!, null!, dialogs);
            long counter = 0;
            var tcs = new TaskCompletionSource<bool>();
            dialogs.ShowOpenFileDialog(Arg.Any<string>()).Returns((DialogResult.OK, "dummy"));

            // Because awaiting "ExecuteUIThread" does not really await the action
            // we need to wait for two property changed events.
            var values = new List<bool>();
            busy.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName != "IsBusy")
                {
                    return;
                }

                values.Add(busy.IsBusy);
                if (Interlocked.Increment(ref counter) == 2)
                {
                    tcs.SetResult(true);
                }
            };

            sut.OpenProjectCommand.Execute(null);

            await tcs.Awaiting(x => x.Task).Should().CompleteWithinAsync(1.Seconds(), "IsBusy should change twice");
            values.Should().Equal(true, false);
        }

        [CulturedFact("en")]
        public void SwitchCultureCommand_DummyLanguage_MessageBoxShownAndConfigurationUpdated()
        {
            var sut = CreateSut(out IDialogs dialogs);

            sut.SwitchCultureCommand.Execute("dummy");

            dialogs.Received(1).ShowMessageBox(
                Arg.Is<string>(x => x.Contains("must restart the application")),
                Arg.Any<string>(),
                icon: MessageBoxImage.Information);
            sut.IsGermanCulture.Should().BeFalse();
            sut.IsEnglishCulture.Should().BeFalse();
            sut.IsSystemCulture.Should().BeFalse();
        }

        [Fact]
        public void NewProjectCommand_ModifiedProjectNoDiscard_ProjectRemains()
        {
            MenuViewModel sut = CreateSut(out ProjectData projectData);
            projectData.IsModified = true;
            var clone = projectData.Storage.Clone();

            sut.NewProjectCommand.Execute(null);

            var _ = new AssertionScope();
            projectData.IsModified.Should().BeTrue("project should still be modified");
            projectData.Storage.Should().BeEquivalentTo(clone);
        }

        [Fact]
        public void SaveProjectCommand_Initialized_CannotExecute()
        {
            var sut = CreateSut(out ProjectData _);

            sut.SaveProjectCommand.CanExecute(null).Should()
                .BeFalse("default instance does not contain modified document");
        }

        [Fact]
        public void SaveProjectCommand_DocumentModified_CanExecute()
        {
            var sut = CreateSut(out ProjectData projectData);
            projectData.Load(Samples.SampleProject);
            projectData.IsModified = true;

            sut.SaveProjectCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void CloseYearCommand_ActionAborted_YearsUnchanged()
        {
            var sut = CreateSut(out ProjectData projectData, out IDialogs dialogs, out _);
            dialogs.ShowMessageBox(
                Arg.Any<string>(),
                Arg.Any<string>(),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No).Returns(MessageBoxResult.No);
            projectData.Load(Samples.SampleProject);
            sut.OnDataLoaded();

            sut.CloseYearCommand.Execute(null);

            var thisYear = DateTime.Now.Year;
            sut.BookingYears.Select(x => x.Header).Should().Equal("2000", thisYear.ToString());
        }

        [Fact]
        public void CloseYearCommand_CurrentYearClosed_CannotExecute()
        {
            var sut = CreateSut(out ProjectData projectData, out IDialogs dialogs, out _);
            dialogs.ShowMessageBox(
                Arg.Any<string>(), Arg.Any<string>(), MessageBoxButton.YesNo, Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>()).Returns(MessageBoxResult.Yes);
            projectData.Load(Samples.SampleProject);
            sut.OnDataLoaded();
            sut.BookingYears.First().Command.Execute(null);

            sut.CloseYearCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void CloseYearCommand_DefaultProject_CanExecute()
        {
            var sut = CreateSut(out ProjectData projectData);
            projectData.Load(Samples.SampleProject);

            sut.CloseYearCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AccountJournalReportCommand_HappyPath_Completed()
        {
            var sut = CreateSut(out ProjectData projectData, out IReportFactory reportFactory);
            var accountJournalReport = Substitute.For<IAccountJournalReport>();
            reportFactory.CreateAccountJournal(projectData).Returns(accountJournalReport);
            projectData.Load(Samples.SampleProject);

            sut.AccountJournalReportCommand.Execute(null);

            accountJournalReport.Received(1)
                .ShowPreview(Arg.Is<string>(document => !string.IsNullOrEmpty(document)));
        }

        [Fact]
        public void AccountJournalReportCommand_JournalWithEntries_CanExecute()
        {
            var sut = CreateSut(out ProjectData projectData);
            projectData.CurrentYear.Booking.Add(new AccountingDataJournalBooking());

            sut.AccountJournalReportCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AccountJournalReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut(out ProjectData _);

            sut.AccountJournalReportCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void AnnualBalanceReportCommand_HappyPath_Completed()
        {
            var sut = CreateSut(out ProjectData projectData, out IReportFactory reportFactory);
            var annualBalanceReport = Substitute.For<IAnnualBalanceReport>();
            reportFactory.CreateAnnualBalance(projectData).Returns(annualBalanceReport);
            projectData.Load(Samples.SampleProject);

            sut.AnnualBalanceReportCommand.Execute(null);

            annualBalanceReport.Received(1)
                .ShowPreview(Arg.Is<string>(document => !string.IsNullOrEmpty(document)));
        }

        [Fact]
        public void AnnualBalanceReportCommand_JournalWithEntries_CanExecute()
        {
            var sut = CreateSut(out ProjectData projectData);
            projectData.CurrentYear.Booking.Add(new AccountingDataJournalBooking());

            sut.AnnualBalanceReportCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AnnualBalanceReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut(out ProjectData _);

            sut.AnnualBalanceReportCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void AssetBalancesReportCommand_HappyPath_Completed()
        {
            var sut = CreateSut(out ProjectData projectData, out IReportFactory reportFactory);
            var assetBalancesReport = Substitute.For<ITotalsAndBalancesReport>();
            reportFactory.CreateTotalsAndBalances(
                    projectData,
                    Arg.Any<IEnumerable<AccountingDataAccountGroup>>())
                .Returns(assetBalancesReport);
            var project = Samples.SampleProject;
            project.Accounts.Add(
                new AccountingDataAccountGroup { Name = "EMPTY", Account = new List<AccountDefinition>() });
            projectData.Load(project);

            sut.AssetBalancesReportCommand.Execute(null);

            assetBalancesReport.Received(1)
                .ShowPreview(Arg.Is<string>(document => !string.IsNullOrEmpty(document)));
            reportFactory.Received(1).CreateTotalsAndBalances(
                projectData,
                Arg.Is<IEnumerable<AccountingDataAccountGroup>>(x => x.ToList().All(y => y.Name != "EMPTY")));
        }

        [Fact]
        public void AssetBalancesReportCommand_JournalWithEntries_CanExecute()
        {
            var sut = CreateSut(out ProjectData projectData);
            projectData.CurrentYear.Booking.Add(new AccountingDataJournalBooking());

            sut.AssetBalancesReportCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AssetBalancesReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut(out ProjectData _);

            sut.AssetBalancesReportCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void TotalJournalReportCommand_HappyPath_Completed()
        {
            var sut = CreateSut(out ProjectData projectData, out IReportFactory reportFactory);
            var totalJournalReport = Substitute.For<ITotalJournalReport>();
            reportFactory.CreateTotalJournal(projectData).Returns(totalJournalReport);
            projectData.Load(Samples.SampleProject);

            sut.TotalJournalReportCommand.Execute(null);

            totalJournalReport.Received(1)
                .ShowPreview(Arg.Is<string>(document => !string.IsNullOrEmpty(document)));
        }

        [Fact]
        public void TotalJournalReportCommand_JournalWithEntries_CanExecute()
        {
            var sut = CreateSut(out ProjectData projectData);
            projectData.CurrentYear.Booking.Add(new AccountingDataJournalBooking());

            sut.TotalJournalReportCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void TotalJournalReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut(out ProjectData _);

            sut.TotalJournalReportCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void TotalsAndBalancesReportCommand_HappyPath_Completed()
        {
            var sut = CreateSut(out ProjectData projectData, out IReportFactory reportFactory);
            var totalsAndBalancesReport = Substitute.For<ITotalsAndBalancesReport>();
            reportFactory.CreateTotalsAndBalances(
                    projectData,
                    Arg.Any<IEnumerable<AccountingDataAccountGroup>>())
                .Returns(totalsAndBalancesReport);
            projectData.Load(Samples.SampleProject);

            sut.TotalsAndBalancesReportCommand.Execute(null);

            totalsAndBalancesReport.Received(1)
                .ShowPreview(Arg.Is<string>(document => !string.IsNullOrEmpty(document)));
        }

        [Fact]
        public void TotalsAndBalancesReportCommand_JournalWithEntries_CanExecute()
        {
            var sut = CreateSut(out ProjectData projectData);
            projectData.CurrentYear.Booking.Add(new AccountingDataJournalBooking());

            sut.TotalsAndBalancesReportCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void TotalsAndBalancesReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut(out ProjectData _);

            sut.TotalsAndBalancesReportCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void HelpAboutCommand_Execute_ShellProcessInvoked()
        {
            var sut = CreateSut(out IProcess processApi);

            sut.HelpAboutCommand.Execute(null);

            processApi.Received(1).ShellExecute(Arg.Any<string>());
        }

        [Fact]
        public void HelpFeedbackCommand_Execute_ShellProcessInvoked()
        {
            var sut = CreateSut(out IProcess processApi);

            sut.HelpFeedbackCommand.Execute(null);

            processApi.Received(1).ShellExecute(Arg.Any<string>());
        }

        private static MenuViewModel CreateSut(out ProjectData projectData)
        {
            var sut = CreateSut(out projectData, out IDialogs _, out IReportFactory _);
            return sut;
        }

        private static MenuViewModel CreateSut(out IDialogs dialogs)
        {
            var sut = CreateSut(out ProjectData _, out dialogs, out IReportFactory _);
            return sut;
        }

        private static MenuViewModel CreateSut(out ProjectData projectData, out IReportFactory reportFactory)
        {
            var sut = CreateSut(out projectData, out IDialogs _, out reportFactory);
            return sut;
        }

        private static MenuViewModel CreateSut(out ProjectData projectData, out IDialogs dialogs, out IReportFactory reportFactory)
        {
            var windowManager = Substitute.For<IWindowManager>();
            var fileSystem = Substitute.For<IFileSystem>();
            var processApi = Substitute.For<IProcess>();
            dialogs = Substitute.For<IDialogs>();
            var busy = Substitute.For<IBusy>();
            reportFactory = Substitute.For<IReportFactory>();
            var settings = new Settings();
            projectData = new ProjectData(settings, windowManager, dialogs, fileSystem, processApi);
            var sut = new MenuViewModel(settings, projectData, busy, reportFactory, processApi, dialogs);
            return sut;
        }

        private static MenuViewModel CreateSut(out IProcess processApi)
        {
            var windowManager = Substitute.For<IWindowManager>();
            var fileSystem = Substitute.For<IFileSystem>();
            processApi = Substitute.For<IProcess>();
            var dialogs = Substitute.For<IDialogs>();
            var busy = Substitute.For<IBusy>();
            var reportFactory = Substitute.For<IReportFactory>();
            var settings = new Settings();
            var projectData = new ProjectData(settings, windowManager, dialogs, fileSystem, processApi);
            var sut = new MenuViewModel(settings, projectData, busy, reportFactory, processApi, dialogs);
            return sut;
        }
    }
}
