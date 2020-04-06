﻿// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using Caliburn.Micro;
    using FluentAssertions;
    using FluentAssertions.Execution;
    using FluentAssertions.Extensions;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Presentation;
    using lg2de.SimpleAccounting.Properties;
    using lg2de.SimpleAccounting.Reports;
    using NSubstitute;
    using Octokit;
    using Xunit;

    public class ShellViewModelTests
    {
        [WpfFact]
        public async Task OnActivate_NewProject_ProjectLoadedAndAutoSaveActive()
        {
            var sut = CreateSut(out IFileSystem fileSystem);
            sut.AutoSaveInterval = 100.Milliseconds();
            sut.FileName = "new.project";
            var fileSaved = new TaskCompletionSource<bool>();
            fileSystem
                .When(x => x.WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>()))
                .Do(x => fileSaved.SetResult(true));

            ((IActivate)sut).Activate();
            sut.LoadingTask.Status.Should().Be(TaskStatus.RanToCompletion);
            sut.LoadProjectData(new AccountingData());
            sut.IsDocumentModified = true;
            await fileSaved.Awaiting(x => x.Task).Should().CompleteWithinAsync(1.Seconds());

            using var _ = new AssertionScope();
            sut.IsDocumentModified.Should().BeTrue();
            fileSystem.Received(1).WriteAllTextIntoFile("new.project~", Arg.Any<string>());
        }

        [WpfFact]
        public async Task OnActivate_RecentProject_ProjectLoadedAndAutoSaveActive()
        {
            var sut = CreateSut(out IFileSystem fileSystem);
            sut.AutoSaveInterval = 100.Milliseconds();
            sut.Settings.RecentProject = "recent.project";
            fileSystem.FileExists("recent.project").Returns(true);
            var sample = new AccountingData
            {
                Accounts = new List<AccountingDataAccountGroup>
                {
                    new AccountingDataAccountGroup
                    {
                        Account = new List<AccountDefinition>
                        {
                            new AccountDefinition { ID = 1, Name = "TheAccount" }
                        }
                    }
                }
            };
            fileSystem.ReadAllTextFromFile("recent.project").Returns(sample.Serialize());
            var fileSaved = new TaskCompletionSource<bool>();
            fileSystem
                .When(x => x.WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>()))
                .Do(x => fileSaved.SetResult(true));

            ((IActivate)sut).Activate();
            await sut.Awaiting(x => x.LoadingTask).Should().CompleteWithinAsync(1.Seconds());
            sut.IsDocumentModified = true;
            await fileSaved.Awaiting(x => x.Task).Should().CompleteWithinAsync(1.Seconds());

            using var _ = new AssertionScope();
            sut.IsDocumentModified.Should().BeTrue();
            sut.AccountList.Should().BeEquivalentTo(new { Name = "TheAccount" });
            fileSystem.Received(1).WriteAllTextIntoFile("recent.project~", Arg.Any<string>());
        }

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
            var sut = CreateSut();

            var result = ShellViewModel.GetNewRelease(currentVersion, CreateRelease(availableVersion));

            if (string.IsNullOrEmpty(expectedVersion))
            {
                result.Should().BeNull();
            }
            else
            {
                result.Should().BeEquivalentTo(new { TagName = expectedVersion });
            }
        }

        private static ShellViewModel CreateSut()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem)
            {
                Settings = new Settings()
            };
            return sut;
        }

        private static ShellViewModel CreateSut(out IWindowManager windowManager)
        {
            windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem)
            {
                Settings = new Settings()
            };
            return sut;
        }

        private static ShellViewModel CreateSut(out IReportFactory reportFactory)
        {
            var windowManager = Substitute.For<IWindowManager>();
            reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem)
            {
                Settings = new Settings()
            };
            return sut;
        }

        private static ShellViewModel CreateSut(out IMessageBox messageBox)
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem)
            {
                Settings = new Settings()
            };
            return sut;
        }

        private static ShellViewModel CreateSut(out IFileSystem fileSystem)
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem)
            {
                Settings = new Settings()
            };
            return sut;
        }

        private static ShellViewModel CreateSut(out IMessageBox messageBox, out IFileSystem fileSystem)
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            messageBox = Substitute.For<IMessageBox>();
            fileSystem = Substitute.For<IFileSystem>();
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem)
            {
                Settings = new Settings()
            };
            return sut;
        }

        private static IReadOnlyList<Release> CreateRelease(string tag, bool addAsset = true)
        {
            Type releaseType = typeof(Release);
            var tagProperty = releaseType.GetProperty(nameof(Release.TagName));
            var prereleaseProperty = releaseType.GetProperty(nameof(Release.Prerelease));
            var assetsProperty = releaseType.GetProperty(nameof(Release.Assets));
            var release = new Release();
            tagProperty.SetValue(release, tag);
            if (tag.Contains("beta"))
            {
                prereleaseProperty.SetValue(release, true);
            }

            if (addAsset)
            {
                assetsProperty.SetValue(release, new List<ReleaseAsset> { new ReleaseAsset() });
            }

            return new List<Release> { release };
        }

        [Fact]
        public void AccountJournalReportCommand_HappyPath_Completed()
        {
            var sut = CreateSut(out IReportFactory reportFactory);
            var accountJournalReport = Substitute.For<IAccountJournalReport>();
            reportFactory.CreateAccountJournal(
                Arg.Any<AccountingDataJournal>(),
                Arg.Any<IEnumerable<AccountDefinition>>(),
                Arg.Any<AccountingDataSetup>(), Arg.Any<CultureInfo>()).Returns(accountJournalReport);
            sut.LoadProjectData(Samples.SampleProject);

            sut.AccountJournalReportCommand.Execute(null);

            accountJournalReport.Received(1)
                .ShowPreview(Arg.Is<string>(document => !string.IsNullOrEmpty(document)));
        }

        [Fact]
        public void AccountJournalReportCommand_JournalWithEntries_CanExecute()
        {
            var sut = CreateSut();
            sut.FullJournal.Add(new FullJournalViewModel());

            sut.AccountJournalReportCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AccountJournalReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut();

            sut.AccountJournalReportCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void AccountSelectionCommand_SampleBookings_AccountJournalUpdated()
        {
            var sut = CreateSut();
            var project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            sut.LoadProjectData(project);

            sut.AccountSelectionCommand.Execute(sut.AccountList.Single(x => x.Identifier == 100));

            sut.AccountJournal.Should().BeEquivalentTo(
                new { Text = "Open 1", RemoteAccount = "990 (Carryforward)", CreditValue = 0, DebitValue = 1000 },
                new { Text = "Salary", RemoteAccount = "Diverse", CreditValue = 0, DebitValue = 200 },
                new { Text = "Credit rate", RemoteAccount = "5000 (Bank credit)", CreditValue = 400, DebitValue = 0 },
                new { Text = "Shoes", RemoteAccount = "Diverse", CreditValue = 50, DebitValue = 0 },
                new
                {
                    Text = "Rent to friend",
                    RemoteAccount = "6000 (Friends debit)",
                    CreditValue = 99,
                    DebitValue = 0
                },
                new { Text = "Summe", RemoteAccount = (string)null, CreditValue = 549, DebitValue = 1200 },
                new { Text = "Saldo", RemoteAccount = (string)null, CreditValue = 0, DebitValue = 651 });
        }

        [Fact]
        public void AddBooking_FirstBooking_JournalsUpdated()
        {
            var sut = CreateSut();
            sut.LoadProjectData(Samples.SampleProject);
            var booking = new AccountingDataJournalBooking
            {
                Date = Samples.BaseDate + 401,
                ID = 4567,
                Credit = new List<BookingValue> { new BookingValue { Account = 990, Text = "Init", Value = 42 } },
                Debit = new List<BookingValue> { new BookingValue { Account = 100, Text = "Init", Value = 42 } }
            };

            using var monitor = sut.Monitor();
            sut.AddBooking(booking);

            using var _ = new AssertionScope();
            sut.FullJournal.Should().BeEquivalentTo(
                new
                {
                    Identifier = 4567,
                    Date = new DateTime(DateTime.Now.Year, 4, 1),
                    Text = "Init",
                    Value = 0.42,
                    CreditAccount = "990 (Carryforward)",
                    DebitAccount = "100 (Bank account)"
                });
            monitor.Should().RaisePropertyChangeFor(x => x.SelectedFullJournalEntry);
            sut.SelectedFullJournalEntry.Should().BeEquivalentTo(new { Identifier = 4567 });
            sut.AccountJournal.Should().BeEquivalentTo(
                new
                {
                    Identifier = 4567,
                    Date = new DateTime(DateTime.Now.Year, 4, 1),
                    Text = "Init",
                    CreditValue = 0.0,
                    DebitValue = 0.42,
                    RemoteAccount = "990 (Carryforward)"
                },
                new { Text = "Summe", IsSummary = true, CreditValue = 0.0, DebitValue = 0.42 },
                new { Text = "Saldo", IsSummary = true, CreditValue = 0.0, DebitValue = 0.42 });
            monitor.Should().RaisePropertyChangeFor(x => x.SelectedAccountJournalEntry);
            sut.SelectedAccountJournalEntry.Should().BeEquivalentTo(new { Identifier = 4567 });
        }

        [Fact]
        public void AddBookingsCommand_ClosedYear_CannotExecute()
        {
            var sut = CreateSut();
            sut.LoadProjectData(Samples.SampleProject);
            sut.BookingYears.First().Command.Execute(null);

            sut.AddBookingsCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void AddBookingsCommand_HideInactiveAccounts_DialogInitialized()
        {
            var sut = CreateSut(out IWindowManager windowManager);
            AddBookingViewModel vm = null;
            windowManager.ShowDialog(Arg.Do<object>(model => vm = model as AddBookingViewModel));
            sut.LoadProjectData(Samples.SampleProject);
            sut.ShowInactiveAccounts = false;

            sut.AddBookingsCommand.Execute(null);

            using var _ = new AssertionScope();
            vm.BookingNumber.Should().Be(1);
            vm.Accounts.Should().BeEquivalentTo(Samples.SampleProject.AllAccounts.Where(x => x.Active));
        }

        [Fact]
        public void AddBookingsCommand_NoProject_CannotExecute()
        {
            var sut = CreateSut();

            sut.AddBookingsCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void AddBookingsCommand_OpenYear_CanExecute()
        {
            var sut = CreateSut();
            sut.LoadProjectData(Samples.SampleProject);
            sut.BookingYears.Last().Command.Execute(null);

            sut.AddBookingsCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AddBookingsCommand_ShowInactiveAccounts_DialogInitialized()
        {
            var sut = CreateSut(out IWindowManager windowManager);
            AddBookingViewModel vm = null;
            windowManager.ShowDialog(Arg.Do<object>(model => vm = model as AddBookingViewModel));
            sut.LoadProjectData(Samples.SampleProject);
            sut.ShowInactiveAccounts = true;

            sut.AddBookingsCommand.Execute(null);

            using var _ = new AssertionScope();
            vm.BookingNumber.Should().Be(1);
            vm.Accounts.Should().BeEquivalentTo(Samples.SampleProject.AllAccounts);
        }

        [Fact]
        public void AnnualBalanceReportCommand_HappyPath_Completed()
        {
            var sut = CreateSut(out IReportFactory reportFactory);
            var annualBalanceReport = Substitute.For<IAnnualBalanceReport>();
            reportFactory.CreateAnnualBalance(
                Arg.Any<AccountingDataJournal>(),
                Arg.Any<IEnumerable<AccountDefinition>>(),
                Arg.Any<AccountingDataSetup>(),
                Arg.Any<CultureInfo>()).Returns(annualBalanceReport);
            sut.LoadProjectData(Samples.SampleProject);

            sut.AnnualBalanceReportCommand.Execute(null);

            annualBalanceReport.Received(1)
                .ShowPreview(Arg.Is<string>(document => !string.IsNullOrEmpty(document)));
        }

        [Fact]
        public void AnnualBalanceReportCommand_JournalWithEntries_CanExecute()
        {
            var sut = CreateSut();
            sut.FullJournal.Add(new FullJournalViewModel());

            sut.AnnualBalanceReportCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AnnualBalanceReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut();

            sut.AnnualBalanceReportCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void AssetBalancesReportCommand_HappyPath_Completed()
        {
            var sut = CreateSut(out IReportFactory reportFactory);
            var assetBalancesReport = Substitute.For<ITotalsAndBalancesReport>();
            reportFactory.CreateTotalsAndBalances(
                Arg.Any<AccountingDataJournal>(),
                Arg.Any<IEnumerable<AccountingDataAccountGroup>>(),
                Arg.Any<AccountingDataSetup>(),
                Arg.Any<CultureInfo>()).Returns(assetBalancesReport);
            sut.LoadProjectData(Samples.SampleProject);

            sut.AssetBalancesReportCommand.Execute(null);

            assetBalancesReport.Received(1)
                .ShowPreview(Arg.Is<string>(document => !string.IsNullOrEmpty(document)));
        }

        [Fact]
        public void AssetBalancesReportCommand_JournalWithEntries_CanExecute()
        {
            var sut = CreateSut();
            sut.FullJournal.Add(new FullJournalViewModel());

            sut.AssetBalancesReportCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void AssetBalancesReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut();

            sut.AssetBalancesReportCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void CheckSaveProject_AnswerNo_NotSavedAndReturnsTrue()
        {
            var sut = CreateSut(out var messageBox, out var fileSystem);
            sut.IsDocumentModified = true;
            messageBox.Show(
                    Arg.Any<string>(), Arg.Any<string>(),
                    Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                    Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
                .Returns(MessageBoxResult.No);
            sut.LoadProjectData(Samples.SampleProject);

            sut.CheckSaveProject().Should().BeTrue();

            messageBox.Received(1).Show(
                Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
            fileSystem.DidNotReceive().WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public void CheckSaveProject_AnswerYes_SavedAndReturnsTrue()
        {
            var sut = CreateSut(out var messageBox, out var fileSystem);
            sut.IsDocumentModified = true;
            messageBox.Show(
                    Arg.Any<string>(), Arg.Any<string>(),
                    Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                    Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
                .Returns(MessageBoxResult.Yes);
            sut.LoadProjectData(Samples.SampleProject);

            sut.CheckSaveProject().Should().BeTrue();

            messageBox.Received(1).Show(
                Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
            fileSystem.Received(1).WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public void CheckSaveProject_Cancel_NotSavedAndReturnsFalse()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            messageBox.Show(
                    Arg.Any<string>(), Arg.Any<string>(),
                    Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                    Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
                .Returns(MessageBoxResult.Cancel);
            var sut = new ShellViewModel(windowManager, reportFactory, messageBox, fileSystem)
            {
                Settings = new Settings(), IsDocumentModified = true
            };
            sut.LoadProjectData(Samples.SampleProject);

            sut.CheckSaveProject().Should().BeFalse();

            messageBox.Received(1).Show(
                Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
            fileSystem.DidNotReceive().WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public void CheckSaveProject_NotModified_ReturnsTrue()
        {
            var sut = CreateSut(out IMessageBox messageBox);

            sut.CheckSaveProject().Should().BeTrue();

            messageBox.DidNotReceive().Show(
                Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>());
        }

        [Fact]
        public void CloseYearCommand_ActionAborted_YearsUnchanged()
        {
            var sut = CreateSut(out IMessageBox messageBox);
            messageBox.Show(
                Arg.Any<string>(),
                Arg.Any<string>(),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No).Returns(MessageBoxResult.No);
            sut.LoadProjectData(Samples.SampleProject);

            sut.CloseYearCommand.Execute(null);

            var thisYear = DateTime.Now.Year;
            sut.BookingYears.Select(x => x.Header).Should().Equal("2000", thisYear.ToString());
        }

        [Fact]
        public void CloseYearCommand_CurrentYearClosed_CannotExecute()
        {
            var sut = CreateSut(out IMessageBox messageBox);
            messageBox.Show(
                Arg.Any<string>(), Arg.Any<string>(), MessageBoxButton.YesNo, Arg.Any<MessageBoxImage>(),
                Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>()).Returns(MessageBoxResult.Yes);
            sut.LoadProjectData(Samples.SampleProject);
            sut.BookingYears.First().Command.Execute(null);

            sut.CloseYearCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void CloseYearCommand_DefaultProject_CanExecute()
        {
            var sut = CreateSut();
            sut.LoadProjectData(Samples.SampleProject);

            sut.CloseYearCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void CloseYearCommand_EmptyProject_CannotExecute()
        {
            var sut = CreateSut();

            sut.CloseYearCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void CloseYearCommand_HappyPath_YearClosedAndNewAdded()
        {
            var sut = CreateSut(out IWindowManager windowManager);
            windowManager.ShowDialog(
                Arg.Any<CloseYearViewModel>(),
                Arg.Any<object>(),
                Arg.Any<IDictionary<string, object>>()).Returns(
                info =>
                {
                    var vm = info.Arg<CloseYearViewModel>();
                    vm.RemoteAccount = vm.Accounts.First();
                    return true;
                });
            var project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            sut.LoadProjectData(project);

            sut.CloseYearCommand.Execute(null);

            windowManager.Received(1).ShowDialog(
                Arg.Any<CloseYearViewModel>(),
                Arg.Any<object>(),
                Arg.Any<IDictionary<string, object>>());
            var thisYear = DateTime.Now.Year;
            using var _ = new AssertionScope();
            sut.BookingYears.Select(x => x.Header).Should()
                .Equal("2000", thisYear.ToString(), (thisYear + 1).ToString());
            sut.FullJournal.Should().BeEquivalentTo(
                new
                {
                    Identifier = 1,
                    Date = new DateTime(thisYear + 1, 1, 1),
                    Text = "Eröffnungsbetrag 1",
                    Value = 651,
                    CreditAccount = "990 (Carryforward)",
                    DebitAccount = "100 (Bank account)"
                },
                new
                {
                    Identifier = 2,
                    Date = new DateTime(thisYear + 1, 1, 1),
                    Text = "Eröffnungsbetrag 2",
                    Value = 2600,
                    CreditAccount = "5000 (Bank credit)",
                    DebitAccount = "990 (Carryforward)"
                },
                new
                {
                    Identifier = 3,
                    Date = new DateTime(thisYear + 1, 1, 1),
                    Text = "Eröffnungsbetrag 3",
                    Value = 99,
                    CreditAccount = "990 (Carryforward)",
                    DebitAccount = "6000 (Friends debit)"
                });
            sut.AccountJournal.Should().BeEquivalentTo(
                new
                {
                    Identifier = 1,
                    Date = new DateTime(thisYear + 1, 1, 1),
                    Text = "Eröffnungsbetrag 1",
                    DebitValue = 651,
                    CreditValue = 0,
                    RemoteAccount = "990 (Carryforward)"
                },
                new { Text = "Summe", IsSummary = true, DebitValue = 651, CreditValue = 0 },
                new { Text = "Saldo", IsSummary = true, DebitValue = 651, CreditValue = 0 });
        }

        [Fact]
        public void CloseYearCommand_SecondCarryForwardAccount_OpeningsWithSelectedAccount()
        {
            var sut = CreateSut(out IWindowManager windowManager);
            windowManager.ShowDialog(
                Arg.Any<CloseYearViewModel>(),
                Arg.Any<object>(),
                Arg.Any<IDictionary<string, object>>()).Returns(
                info =>
                {
                    var vm = info.Arg<CloseYearViewModel>();
                    vm.RemoteAccount = vm.Accounts.Last();
                    return true;
                });
            var project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            project.Accounts.First().Account.Add(
                new AccountDefinition { ID = 999, Name = "MyCarryForward", Type = AccountDefinitionType.Carryforward });
            sut.LoadProjectData(project);

            sut.CloseYearCommand.Execute(null);

            windowManager.Received(1).ShowDialog(
                Arg.Any<CloseYearViewModel>(),
                Arg.Any<object>(),
                Arg.Any<IDictionary<string, object>>());
            var thisYear = DateTime.Now.Year;
            using var _ = new AssertionScope();
            sut.BookingYears.Select(x => x.Header).Should()
                .Equal("2000", thisYear.ToString(), (thisYear + 1).ToString());
            sut.FullJournal.Should().BeEquivalentTo(
                new
                {
                    Identifier = 1,
                    Date = new DateTime(thisYear + 1, 1, 1),
                    Text = "Eröffnungsbetrag 1",
                    Value = 651,
                    CreditAccount = "999 (MyCarryForward)",
                    DebitAccount = "100 (Bank account)"
                },
                new
                {
                    Identifier = 2,
                    Date = new DateTime(thisYear + 1, 1, 1),
                    Text = "Eröffnungsbetrag 2",
                    Value = 2600,
                    CreditAccount = "5000 (Bank credit)",
                    DebitAccount = "999 (MyCarryForward)"
                },
                new
                {
                    Identifier = 3,
                    Date = new DateTime(thisYear + 1, 1, 1),
                    Text = "Eröffnungsbetrag 3",
                    Value = 99,
                    CreditAccount = "999 (MyCarryForward)",
                    DebitAccount = "6000 (Friends debit)"
                });
            sut.AccountJournal.Should().BeEquivalentTo(
                new
                {
                    Identifier = 1,
                    Date = new DateTime(thisYear + 1, 1, 1),
                    Text = "Eröffnungsbetrag 1",
                    DebitValue = 651,
                    CreditValue = 0,
                    RemoteAccount = "999 (MyCarryForward)"
                },
                new { Text = "Summe", IsSummary = true, DebitValue = 651, CreditValue = 0 },
                new { Text = "Saldo", IsSummary = true, DebitValue = 651, CreditValue = 0 });
        }

        [Fact]
        public void EditAccountCommand_AllDataUpdated()
        {
            var sut = CreateSut(out IWindowManager windowManager);
            windowManager.ShowDialog(
                Arg.Do<object>(
                    model =>
                    {
                        var vm = model as AccountViewModel;
                        vm.Identifier += 1000;
                    })).Returns(true);
            sut.LoadProjectData(Samples.SampleProject);
            var booking = new AccountingDataJournalBooking
            {
                Date = DateTime.Now.ToAccountingDate(),
                ID = 1,
                Credit = new List<BookingValue> { new BookingValue { Account = 990, Text = "Init", Value = 42 } },
                Debit = new List<BookingValue> { new BookingValue { Account = 100, Text = "Init", Value = 42 } }
            };
            sut.AddBooking(booking);
            booking = new AccountingDataJournalBooking
            {
                Date = DateTime.Now.ToAccountingDate(),
                ID = 2,
                Credit = new List<BookingValue> { new BookingValue { Account = 100, Text = "Back", Value = 5 } },
                Debit = new List<BookingValue> { new BookingValue { Account = 990, Text = "Back", Value = 5 } }
            };
            sut.AddBooking(booking);
            sut.AccountSelectionCommand.Execute(sut.AccountList.FirstOrDefault(x => x.Identifier == 990));

            sut.EditAccountCommand.Execute(sut.AccountList.First());

            using (new AssertionScope())
            {
                sut.AccountList.Select(x => x.Name).Should().Equal(
                    "Salary", "Shoes", "Carryforward", "Bank account", "Bank credit", "Friends debit");
                sut.FullJournal.Should().BeEquivalentTo(
                    new { CreditAccount = "990 (Carryforward)", DebitAccount = "1100 (Bank account)" },
                    new { CreditAccount = "1100 (Bank account)", DebitAccount = "990 (Carryforward)" });
                sut.AccountJournal.Should().BeEquivalentTo(
                    new { RemoteAccount = "1100 (Bank account)" },
                    new { RemoteAccount = "1100 (Bank account)" },
                    new { Text = "Summe" },
                    new { Text = "Saldo" });
            }
        }

        [Fact]
        public void GetNewRelease_AssetNotAvailable_VersionIgnored()
        {
            var sut = CreateSut();

            var releases = CreateRelease("2.1.0", addAsset: false);
            var result = ShellViewModel.GetNewRelease("2.0.0", releases);

            result.Should().BeNull();
        }

        [Fact]
        public void ImportBookingsCommand_BookingNumberInitialized()
        {
            var sut = CreateSut(out IWindowManager windowManager);
            ImportBookingsViewModel vm = null;
            windowManager.ShowDialog(Arg.Do<object>(model => vm = model as ImportBookingsViewModel));
            sut.LoadProjectData(Samples.SampleProject);

            sut.ImportBookingsCommand.Execute(null);

            using (new AssertionScope())
            {
                vm.Should().BeEquivalentTo(
                    new
                    {
                        BookingNumber = 1,
                        RangeMin = new DateTime(DateTime.Now.Year, 1, 1),
                        RangMax = new DateTime(DateTime.Now.Year, 12, 31)
                    });
                vm.ImportAccounts.Should().NotBeEmpty();
            }
        }

        [Fact]
        public void ImportBookingsCommand_ClosedYear_CannotExecute()
        {
            var sut = CreateSut();
            sut.LoadProjectData(Samples.SampleProject);
            sut.BookingYears.First().Command.Execute(null);

            sut.ImportBookingsCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void ImportBookingsCommand_NoProject_CannotExecute()
        {
            var sut = CreateSut();

            sut.ImportBookingsCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void ImportBookingsCommand_OpenYear_CanExecute()
        {
            var sut = CreateSut();
            sut.LoadProjectData(Samples.SampleProject);
            sut.BookingYears.Last().Command.Execute(null);

            sut.ImportBookingsCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void LoadProjectFromFile_AutoSaveFileExistsNo_AutoSaveFileLoaded()
        {
            var sut = CreateSut(out var messageBox, out var fileSystem);
            messageBox.Show(
                    Arg.Any<string>(), Arg.Any<string>(),
                    MessageBoxButton.YesNo, MessageBoxImage.Question,
                    Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
                .Returns(MessageBoxResult.No);
            fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(new AccountingData().Serialize());
            fileSystem.FileExists("the.fileName~").Returns(true);

            sut.LoadProjectFromFile("the.fileName");

            using var _ = new AssertionScope();
            sut.FileName.Should().Be("the.fileName");
            sut.IsDocumentModified.Should().BeFalse();
            sut.Settings.RecentProject.Should().Be("the.fileName");
            sut.Settings.RecentProjects.OfType<string>().Should().Equal("the.fileName");
            fileSystem.Received(1).ReadAllTextFromFile("the.fileName");
        }

        [Fact]
        public void LoadProjectFromFile_AutoSaveFileExistsYes_AutoSaveFileLoaded()
        {
            var sut = CreateSut(out var messageBox, out var fileSystem);
            messageBox.Show(
                    Arg.Any<string>(), Arg.Any<string>(),
                    MessageBoxButton.YesNo, MessageBoxImage.Question,
                    Arg.Any<MessageBoxResult>(), Arg.Any<MessageBoxOptions>())
                .Returns(MessageBoxResult.Yes);
            fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(new AccountingData().Serialize());
            fileSystem.FileExists("the.fileName~").Returns(true);

            sut.LoadProjectFromFile("the.fileName");

            using var _ = new AssertionScope();
            sut.FileName.Should().Be("the.fileName");
            sut.IsDocumentModified.Should().BeTrue("changes are (still) not yet saved");
            sut.Settings.RecentProject.Should().Be("the.fileName");
            sut.Settings.RecentProjects.OfType<string>().Should().Equal("the.fileName");
            fileSystem.Received(1).ReadAllTextFromFile("the.fileName~");
        }

        [Fact]
        public void LoadProjectFromFile_FullRecentList_NewFileOnTop()
        {
            var sut = CreateSut(out IFileSystem fileSystem);
            sut.Settings.RecentProjects = new StringCollection
            {
                "A",
                "B",
                "C",
                "D",
                "E",
                "F",
                "G",
                "H",
                "I",
                "J"
            };
            fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(new AccountingData().Serialize());

            sut.LoadProjectFromFile("the.fileName");

            sut.Settings.RecentProjects.OfType<string>().Should()
                .Equal("the.fileName", "A", "B", "C", "D", "E", "F", "G", "H", "I");
        }

        [Fact]
        public void LoadProjectFromFile_HappyPath_FileLoaded()
        {
            var sut = CreateSut(out IFileSystem fileSystem);
            fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(new AccountingData().Serialize());

            sut.LoadProjectFromFile("the.fileName");

            using var _ = new AssertionScope();
            sut.FileName.Should().Be("the.fileName");
            sut.IsDocumentModified.Should().BeFalse();
            sut.Settings.RecentProject.Should().Be("the.fileName");
            sut.Settings.RecentProjects.OfType<string>().Should().Equal("the.fileName");
            fileSystem.Received(1).ReadAllTextFromFile("the.fileName");
        }

        [Fact]
        public void LoadProjectFromFile_MigrationRequired_ProjectModified()
        {
            var sut = CreateSut(out IFileSystem fileSystem);
            var accountingData = new AccountingData
            {
                Years = new List<AccountingDataYear> { new AccountingDataYear { Name = 2020 } }
            };
            fileSystem.ReadAllTextFromFile(Arg.Any<string>()).Returns(accountingData.Serialize());

            sut.LoadProjectFromFile("the.fileName");

            sut.IsDocumentModified.Should().BeTrue();
        }

        [Fact]
        public void NewAccountCommand_AccountCreatedAndSorted()
        {
            var sut = CreateSut(out IWindowManager windowManager);
            windowManager.ShowDialog(
                Arg.Do<object>(
                    model =>
                    {
                        var vm = model as AccountViewModel;
                        vm.Name = "New Account";
                        vm.Identifier = 500;
                    })).Returns(true);
            sut.LoadProjectData(Samples.SampleProject);

            sut.NewAccountCommand.Execute(null);

            sut.AccountList.Select(x => x.Name).Should().Equal(
                "Bank account", "Salary", "New Account", "Shoes", "Carryforward", "Bank credit", "Friends debit");
        }

        [Fact]
        public void NewProjectCommand_ProjectInitialized()
        {
            var sut = CreateSut();

            sut.NewProjectCommand.Execute(null);

            sut.AccountList.Should().NotBeEmpty();
            sut.FullJournal.Should().BeEmpty();
            sut.AccountJournal.Should().BeEmpty();
        }

        [Fact]
        public void OnActivate_SampleProject_JournalsUpdates()
        {
            var sut = CreateSut();
            var project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            sut.LoadProjectData(project);

            ((IActivate)sut).Activate();

            using var _ = new AssertionScope();
            sut.AccountList.Should().BeEquivalentTo(
                new { Name = "Bank account" },
                new { Name = "Salary" },
                new { Name = "Shoes" },
                new { Name = "Carryforward" },
                new { Name = "Bank credit" },
                new { Name = "Friends debit" });

            sut.FullJournal.Should().BeEquivalentTo(
                new { Text = "Open 1", CreditAccount = "990 (Carryforward)", DebitAccount = "100 (Bank account)" },
                new { Text = "Open 2", CreditAccount = "5000 (Bank credit)", DebitAccount = "990 (Carryforward)" },
                new { Text = "Salary", CreditAccount = (string)null, DebitAccount = "100 (Bank account)" },
                new { Text = "Salary1", CreditAccount = "400 (Salary)", DebitAccount = (string)null },
                new { Text = "Salary2", CreditAccount = "400 (Salary)", DebitAccount = (string)null },
                new { Text = "Credit rate", CreditAccount = "100 (Bank account)", DebitAccount = "5000 (Bank credit)" },
                new { Text = "Shoes1", CreditAccount = (string)null, DebitAccount = "600 (Shoes)" },
                new { Text = "Shoes2", CreditAccount = (string)null, DebitAccount = "600 (Shoes)" },
                new { Text = "Shoes", CreditAccount = "100 (Bank account)", DebitAccount = (string)null },
                new
                {
                    Text = "Rent to friend",
                    CreditAccount = "100 (Bank account)",
                    DebitAccount = "6000 (Friends debit)"
                });
            sut.AccountJournal.Should().BeEquivalentTo(
                new { Text = "Open 1", RemoteAccount = "990 (Carryforward)" },
                new { Text = "Salary", RemoteAccount = "Diverse" },
                new { Text = "Credit rate", RemoteAccount = "5000 (Bank credit)" },
                new { Text = "Shoes", RemoteAccount = "Diverse" },
                new { Text = "Rent to friend", RemoteAccount = "6000 (Friends debit)" },
                new { Text = "Summe" },
                new { Text = "Saldo" });
        }

        [Fact]
        public void OnActivate_TwoRecentProjectsOneExisting_ExistingProjectListed()
        {
            var sut = CreateSut(out IFileSystem fileSystem);
            sut.Settings = new Settings { RecentProjects = new StringCollection { "file1", "file2" } };
            fileSystem.FileExists(Arg.Is("file1")).Returns(true);

            ((IActivate)sut).Activate();

            sut.RecentProjects?.Select(x => x.Header).Should().Equal("file1");
        }

        [Fact]
        public void OnDeactivate_HappyPath_Completes()
        {
            var sut = CreateSut();
            ((IActivate)sut).Activate();

            var task = Task.Run(() => ((IDeactivate)sut).Deactivate(close: true));

            task.Awaiting(x => x).Should().CompleteWithin(1.Seconds());
        }

        [Fact]
        public void OnInitialize_NoProject_Initialized()
        {
            var sut = CreateSut();

            ((IActivate)sut).Activate();

            sut.DisplayName.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void SaveProject_AutoSaveExisting_AutoSaveFileDeleted()
        {
            var sut = CreateSut(out IFileSystem fileSystem);
            fileSystem.GetLastWriteTime(Arg.Any<string>()).Returns(new DateTime(2020, 2, 29, 18, 45, 56));
            var fileName = "project.name";
            fileSystem.FileExists(fileName + "~").Returns(true);
            sut.LoadProjectData(Samples.SampleProject);
            sut.FileName = fileName;

            sut.SaveProject();

            fileSystem.DidNotReceive().FileMove(Arg.Any<string>(), Arg.Any<string>());
            fileSystem.Received(1).WriteAllTextIntoFile(fileName, Arg.Any<string>());
            fileSystem.Received(1).FileDelete(fileName + "~");
        }

        [Fact]
        public void SaveProject_NotExisting_JustSaved()
        {
            var sut = CreateSut(out IFileSystem fileSystem);
            fileSystem.GetLastWriteTime(Arg.Any<string>()).Returns(new DateTime(2020, 2, 29, 18, 45, 56));
            sut.LoadProjectData(Samples.SampleProject);

            sut.SaveProject();

            fileSystem.DidNotReceive().FileMove(Arg.Any<string>(), Arg.Any<string>());
            fileSystem.Received(1).WriteAllTextIntoFile(Arg.Any<string>(), Arg.Any<string>());
            fileSystem.DidNotReceive().FileDelete(Arg.Any<string>());
        }

        [Fact]
        public void SaveProject_ProjectExisting_SavedAfterBackup()
        {
            var sut = CreateSut(out IFileSystem fileSystem);
            fileSystem.GetLastWriteTime(Arg.Any<string>()).Returns(new DateTime(2020, 2, 29, 18, 45, 56));
            var fileName = "project.name";
            fileSystem.FileExists(fileName).Returns(true);
            sut.LoadProjectData(Samples.SampleProject);
            sut.FileName = fileName;

            sut.SaveProject();

            fileSystem.Received(1).FileMove(fileName, fileName + ".20200229184556");
            fileSystem.Received(1).WriteAllTextIntoFile(fileName, Arg.Any<string>());
            fileSystem.DidNotReceive().FileDelete(Arg.Any<string>());
        }

        [Fact]
        public void SaveProjectCommand_DocumentModified_CanExecute()
        {
            var sut = CreateSut();
            sut.LoadProjectData(Samples.SampleProject);
            sut.IsDocumentModified = true;

            sut.SaveProjectCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void SaveProjectCommand_Initialized_CannotExecute()
        {
            var sut = CreateSut();

            sut.SaveProjectCommand.CanExecute(null).Should()
                .BeFalse("default instance does not contain modified document");
        }

        [Fact]
        public void ShowInactiveAccounts_SetTrue_InactiveAccountsGetVisible()
        {
            var sut = CreateSut();
            var project = Samples.SampleProject;
            project.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            sut.LoadProjectData(project);

            sut.ShowInactiveAccounts = true;

            using var _ = new AssertionScope();

            sut.AccountList.Should().BeEquivalentTo(
                new { Name = "Bank account" },
                new { Name = "Salary" },
                new { Name = "Shoes" },
                new { Name = "Carryforward" },
                new { Name = "Bank credit" },
                new { Name = "Friends debit" },
                new { Name = "Inactive" });
        }

        [Fact]
        public void TotalJournalReportCommand_HappyPath_Completed()
        {
            var sut = CreateSut(out IReportFactory reportFactory);
            var totalJournalReport = Substitute.For<ITotalJournalReport>();
            reportFactory.CreateTotalJournal(
                Arg.Any<AccountingDataJournal>(),
                Arg.Any<AccountingDataSetup>(),
                Arg.Any<CultureInfo>()).Returns(totalJournalReport);
            sut.LoadProjectData(Samples.SampleProject);

            sut.TotalJournalReportCommand.Execute(null);

            totalJournalReport.Received(1)
                .ShowPreview(Arg.Is<string>(document => !string.IsNullOrEmpty(document)));
        }

        [Fact]
        public void TotalJournalReportCommand_JournalWithEntries_CanExecute()
        {
            var sut = CreateSut();
            sut.FullJournal.Add(new FullJournalViewModel());

            sut.TotalJournalReportCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void TotalJournalReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut();

            sut.TotalJournalReportCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void TotalsAndBalancesReportCommand_HappyPath_Completed()
        {
            var sut = CreateSut(out IReportFactory reportFactory);
            var totalsAndBalancesReport = Substitute.For<ITotalsAndBalancesReport>();
            reportFactory.CreateTotalsAndBalances(
                Arg.Any<AccountingDataJournal>(),
                Arg.Any<IEnumerable<AccountingDataAccountGroup>>(),
                Arg.Any<AccountingDataSetup>(),
                Arg.Any<CultureInfo>()).Returns(totalsAndBalancesReport);
            sut.LoadProjectData(Samples.SampleProject);

            sut.TotalsAndBalancesReportCommand.Execute(null);

            totalsAndBalancesReport.Received(1)
                .ShowPreview(Arg.Is<string>(document => !string.IsNullOrEmpty(document)));
        }

        [Fact]
        public void TotalsAndBalancesReportCommand_JournalWithEntries_CanExecute()
        {
            var sut = CreateSut();
            sut.FullJournal.Add(new FullJournalViewModel());

            sut.TotalsAndBalancesReportCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void TotalsAndBalancesReportCommand_NoJournal_CannotExecute()
        {
            var sut = CreateSut();

            sut.TotalsAndBalancesReportCommand.CanExecute(null).Should().BeFalse();
        }
    }
}
