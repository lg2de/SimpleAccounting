// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Extensions;
using lg2de.SimpleAccounting.Infrastructure;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Presentation;
using lg2de.SimpleAccounting.Properties;
using NSubstitute;
using Xunit;

public class ImportBookingsViewModelTests
{
    [Fact]
    public void Ctor_SampleData_AccountsFiltered()
    {
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        projectData.LoadData(Samples.SampleProject);
        projectData.Storage.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        var sut = new ImportBookingsViewModel(null!, null!, projectData);

        sut.ImportAccounts.Should().BeEquivalentTo([new { Name = "Bank account" }]);
    }

    [Fact]
    public void ImportStatus_NoImportAccount()
    {
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        projectData.Storage.Accounts =
        [
            new AccountingDataAccountGroup { Account = [new AccountDefinition { ID = 100, Name = "Bank" }] }
        ];
        var sut = new ImportBookingsViewModel(null!, null!, projectData);

        sut.IsImportPossible.Should().BeFalse();
        sut.IsImportBroken.Should().BeTrue();
    }

    [Fact]
    public void ImportStatus_AnyImportAccount()
    {
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        projectData.Storage.Accounts =
        [
            new AccountingDataAccountGroup
            {
                Account =
                [
                    new AccountDefinition { ID = 100, Name = "Bank", ImportMapping = Samples.SimpleImportConfiguration }
                ]
            }
        ];
        var sut = new ImportBookingsViewModel(null!, null!, projectData);

        sut.IsImportPossible.Should().BeTrue();
        sut.IsImportBroken.Should().BeFalse();
    }

    [CulturedFact(["en"])]
    public void SelectedAccountNumber_BankAccountSelected_ExistingBookingsSetUp()
    {
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        projectData.LoadData(Samples.SampleProject);
        var sut = new ImportBookingsViewModel(null!, null!, projectData);
        projectData.Storage.Journal[^1].Booking.AddRange(Samples.SampleBookings);

        sut.SelectedAccountNumber = 100;

        var year = Convert.ToInt32(Samples.SampleProject.Journal[^1].Year, CultureInfo.InvariantCulture);
        sut.ImportDataFiltered.Should().BeEmpty("start date should be set after last booking");
        sut.ExistingData.Should()
            .BeEquivalentTo(
            [
                new
                {
                    Date = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Local),
                    Identifier = 1,
                    Text = "Open 1",
                    Value = 1000,
                    RemoteAccount = new { ID = 990 }
                },
                new
                {
                    Date = new DateTime(year, 1, 28, 0, 0, 0, DateTimeKind.Local),
                    Identifier = 3,
                    Text = "Salary",
                    Value = 200,
                    RemoteAccount = new { ID = 400 }
                },
                new
                {
                    Date = new DateTime(year, 1, 29, 0, 0, 0, DateTimeKind.Local),
                    Identifier = 4,
                    Text = "Credit rate",
                    Value = -400,
                    RemoteAccount = new { ID = 5000 }
                },
                new
                {
                    Date = new DateTime(year, 2, 1, 0, 0, 0, DateTimeKind.Local),
                    Identifier = 5,
                    Text = "Shoes",
                    Value = -50,
                    RemoteAccount = new { ID = 600 }
                },
                new
                {
                    Date = new DateTime(year, 2, 5, 0, 0, 0, DateTimeKind.Local),
                    Identifier = 6,
                    Text = "Rent to friend",
                    Value = -99,
                    RemoteAccount = new { ID = 6000 }
                }
            ])
            .And.AllBeEquivalentTo(new { Name = "<already booked>", IsExisting = true });
    }

    [Fact]
    public void LoadDataCommand_NoAccountSelected_CannotExecute()
    {
        var dialogs = Substitute.For<IDialogs>();
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, dialogs, null!, clock, null!);
        var sut = new ImportBookingsViewModel(dialogs, null!, projectData);

        sut.LoadDataCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void LoadDataCommand_NoLastImportFolder_DefaultUsedAndSelectedStored()
    {
        var dialogs = Substitute.For<IDialogs>();
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, dialogs, null!, clock, null!);
        var fileSystem = Substitute.For<IFileSystem>();
        var sut = new ImportBookingsViewModel(dialogs, fileSystem, projectData) { SelectedAccountNumber = 100 };
        dialogs
            .ShowOpenFileDialog(Arg.Any<string>(), Arg.Any<string>())
            .Returns((DialogResult.OK, "D:\\MySelectedFolder\\import.csv"));

        sut.LoadDataCommand.Execute(null);

        projectData.Storage.Setup.Behavior.LastBookingImportFolder.Should().Be("D:\\MySelectedFolder");
        dialogs.Received(1).ShowOpenFileDialog(Arg.Any<string>(), Arg.Is<string>(x => x == null));
    }

    [Fact]
    public void LoadDataCommand_LoadFileCancelled_IsBusyReset()
    {
        var clock = Substitute.For<IClock>();
        var dialogs = Substitute.For<IDialogs>();
        var projectData = new ProjectData(new Settings(), null!, dialogs, null!, clock, null!);
        var sut = new ImportBookingsViewModel(dialogs, null!, projectData) { SelectedAccountNumber = 100 };
        dialogs
            .ShowOpenFileDialog(Arg.Any<string>(), Arg.Any<string>())
            .Returns((DialogResult.Cancel, string.Empty));

        sut.LoadDataCommand.Execute(null);

        projectData.Storage.Setup.Behavior.LastBookingImportFolder.Should()
            .BeNullOrEmpty("last folder should remain unchanged");
        sut.Busy.IsBusy.Should().BeFalse();
    }

    [Fact]
    public void LoadDataCommand_LastImportFolder_LastUsedAndNewStored()
    {
        var clock = Substitute.For<IClock>();
        var dialogs = Substitute.For<IDialogs>();
        var projectData = new ProjectData(new Settings(), null!, dialogs, null!, clock, null!);
        projectData.Storage.Setup.Behavior.LastBookingImportFolder = "E:\\MySelectedFolder";
        var fileSystem = Substitute.For<IFileSystem>();
        var sut = new ImportBookingsViewModel(dialogs, fileSystem, projectData) { SelectedAccountNumber = 100 };
        dialogs
            .ShowOpenFileDialog(Arg.Any<string>(), Arg.Any<string>())
            .Returns((DialogResult.OK, "F:\\MySelectedFolder\\import.csv"));

        sut.LoadDataCommand.Execute(null);

        projectData.Storage.Setup.Behavior.LastBookingImportFolder.Should().Be("F:\\MySelectedFolder");
        dialogs.Received(1).ShowOpenFileDialog(Arg.Any<string>(), Arg.Is<string>(x => x == "E:\\MySelectedFolder"));
    }

    [Fact]
    public void SetRemoteAccountsCommand_EntryNotMapped_CanExecute()
    {
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        projectData.LoadData(Samples.SampleProject);
        projectData.Storage.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        var accounts = projectData.Storage.AllAccounts.ToList();
        var sut = new ImportBookingsViewModel(null!, null!, projectData);
        var sampleDate = (Samples.BaseDate + 110).ToDateTime();
        sut.LoadedData.Add(
            new ImportEntryViewModel(accounts)
            {
                Date = sampleDate, RemoteAccount = accounts[0], IsSkip = false, IsExisting = false
            });
        sut.LoadedData.Add(
            new ImportEntryViewModel(accounts)
            {
                Date = sampleDate, RemoteAccount = null, IsSkip = false, IsExisting = false
            });

        sut.SetRemoteAccountsCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void SetRemoteAccountsCommand_EntriesMapped_CannotExecute()
    {
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        projectData.LoadData(Samples.SampleProject);
        projectData.Storage.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        var accounts = projectData.Storage.AllAccounts.ToList();
        var sut = new ImportBookingsViewModel(null!, null!, projectData);
        var sampleDate = (Samples.BaseDate + 110).ToDateTime();
        sut.LoadedData.Add(
            new ImportEntryViewModel(accounts)
            {
                Date = sampleDate, RemoteAccount = accounts[0], IsSkip = false, IsExisting = false
            });
        sut.LoadedData.Add(
            new ImportEntryViewModel(accounts)
            {
                Date = sampleDate, RemoteAccount = accounts[1], IsSkip = false, IsExisting = false
            });

        sut.SetRemoteAccountsCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void SetRemoteAccountsCommand_EntryNotMapped_AccountsSelected()
    {
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        projectData.LoadData(Samples.SampleProject);
        projectData.Storage.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        var accounts = projectData.Storage.AllAccounts.ToList();
        var sut = new ImportBookingsViewModel(null!, null!, projectData);
        var sampleDate = (Samples.BaseDate + 110).ToDateTime();
        sut.LoadedData.Add(new ImportEntryViewModel(accounts) { Date = sampleDate });
        sut.LoadedData.Add(
            new ImportEntryViewModel(accounts) { Date = sampleDate, Name = "irrelevant", RemoteAccount = accounts[0] });
        sut.LoadedData.Add(new ImportEntryViewModel(accounts) { Date = sampleDate, Name = "Shoes" });
        sut.LoadedData.Add(new ImportEntryViewModel(accounts) { Date = sampleDate, Name = "Frieds spend" });

        sut.SetRemoteAccountsCommand.Execute(null);

        sut.LoadedData.Should()
            .BeEquivalentTo(
                new object[]
                {
                    new { RemoteAccount = new { Name = "Bank account" } },
                    new { RemoteAccount = (object)null },
                    new { RemoteAccount = new { Name = "Shoes" } },
                    new { RemoteAccount = new { Name = "Friends debit" } }
                });
    }

    [Fact]
    public void BookMappedCommand_FirstEntryMapped_CanExecute()
    {
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        projectData.LoadData(Samples.SampleProject);
        projectData.Storage.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        var accounts = projectData.Storage.AllAccounts.ToList();
        var sut = new ImportBookingsViewModel(null!, null!, projectData);
        var sampleDate = (Samples.BaseDate + 110).ToDateTime();
        sut.LoadedData.Add(
            new ImportEntryViewModel(accounts)
            {
                Date = sampleDate, RemoteAccount = accounts[0], IsSkip = false, IsExisting = false
            });
        sut.LoadedData.Add(
            new ImportEntryViewModel(accounts)
            {
                Date = sampleDate, RemoteAccount = null, IsSkip = false, IsExisting = false
            });

        sut.BookMappedCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void BookMappedCommand_FirstEntrySkippedSecondMapped_CanExecute()
    {
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        projectData.LoadData(Samples.SampleProject);
        projectData.Storage.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        var accounts = projectData.Storage.AllAccounts.ToList();
        var sut = new ImportBookingsViewModel(null!, null!, projectData);
        var sampleDate = (Samples.BaseDate + 110).ToDateTime();
        sut.LoadedData.Add(
            new ImportEntryViewModel(accounts)
            {
                Date = sampleDate, RemoteAccount = null, IsSkip = true, IsExisting = false
            });
        sut.LoadedData.Add(
            new ImportEntryViewModel(accounts)
            {
                Date = sampleDate, RemoteAccount = accounts[0], IsSkip = false, IsExisting = false
            });

        sut.BookMappedCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void BookMappedCommand_FirstEntryExistingSecondMapped_CanExecute()
    {
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        projectData.LoadData(Samples.SampleProject);
        projectData.Storage.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        var accounts = projectData.Storage.AllAccounts.ToList();
        var sut = new ImportBookingsViewModel(null!, null!, projectData);
        var sampleDate = (Samples.BaseDate + 110).ToDateTime();
        sut.LoadedData.Add(
            new ImportEntryViewModel(accounts)
            {
                Date = sampleDate, RemoteAccount = null, IsSkip = false, IsExisting = true
            });
        sut.LoadedData.Add(
            new ImportEntryViewModel(accounts)
            {
                Date = sampleDate, RemoteAccount = accounts[0], IsSkip = false, IsExisting = false
            });

        sut.BookMappedCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void BookMappedCommand_UnmappedEntryBeforeStart_CanExecute()
    {
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        projectData.LoadData(Samples.SampleProject);
        projectData.Storage.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        var accounts = projectData.Storage.AllAccounts.ToList();
        var sampleDate = (Samples.BaseDate + 110).ToDateTime();
        var sut = new ImportBookingsViewModel(null!, null!, projectData) { StartDate = sampleDate };
        sut.LoadedData.Add(
            new ImportEntryViewModel(accounts)
            {
                Date = sampleDate - 1.Days(), RemoteAccount = null, IsSkip = false, IsExisting = false
            });
        sut.LoadedData.Add(
            new ImportEntryViewModel(accounts)
            {
                Date = sampleDate + 1.Days(), RemoteAccount = accounts[0], IsSkip = false, IsExisting = false
            });

        sut.BookMappedCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void BookMappedCommand_NoEntry_CannotExecute()
    {
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        projectData.LoadData(Samples.SampleProject);
        projectData.Storage.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        var sut = new ImportBookingsViewModel(null!, null!, projectData);

        sut.BookMappedCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void BookMappedCommand_FirstEntryNotMapped_CannotExecute()
    {
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        projectData.LoadData(Samples.SampleProject);
        projectData.Storage.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        var accounts = projectData.Storage.AllAccounts.ToList();
        var sut = new ImportBookingsViewModel(null!, null!, projectData);
        var sampleDate = (Samples.BaseDate + 110).ToDateTime();
        sut.LoadedData.Add(
            new ImportEntryViewModel(accounts)
            {
                Date = sampleDate, RemoteAccount = null, IsSkip = false, IsExisting = false
            });
        sut.LoadedData.Add(
            new ImportEntryViewModel(accounts)
            {
                Date = sampleDate, RemoteAccount = accounts[0], IsSkip = false, IsExisting = false
            });

        sut.BookMappedCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void BookMappedCommand_AllEntriesIgnored_CannotExecute()
    {
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        projectData.LoadData(Samples.SampleProject);
        projectData.Storage.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        var accounts = projectData.Storage.AllAccounts.ToList();
        var sut = new ImportBookingsViewModel(null!, null!, projectData);
        var sampleDate = (Samples.BaseDate + 110).ToDateTime();
        sut.LoadedData.Add(
            new ImportEntryViewModel(accounts)
            {
                Date = sampleDate, RemoteAccount = null, IsSkip = true, IsExisting = false
            });
        sut.LoadedData.Add(
            new ImportEntryViewModel(accounts)
            {
                Date = sampleDate, RemoteAccount = null, IsSkip = false, IsExisting = true
            });

        sut.BookMappedCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void BookAllCommand_EntryNotMapped_CannotExecute()
    {
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        projectData.LoadData(Samples.SampleProject);
        projectData.Storage.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        var accounts = projectData.Storage.AllAccounts.ToList();
        var sut = new ImportBookingsViewModel(null!, null!, projectData);
        var sampleDate = (Samples.BaseDate + 110).ToDateTime();
        sut.LoadedData.Add(
            new ImportEntryViewModel(accounts)
            {
                Date = sampleDate, RemoteAccount = null, IsSkip = false, IsExisting = false
            });
        sut.LoadedData.Add(
            new ImportEntryViewModel(accounts)
            {
                Date = sampleDate, RemoteAccount = accounts[0], IsSkip = false, IsExisting = false
            });

        sut.BookAllCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void BookAllCommand_EntryMapped_CanExecute()
    {
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        projectData.LoadData(Samples.SampleProject);
        projectData.Storage.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        var accounts = projectData.Storage.AllAccounts.ToList();
        var sut = new ImportBookingsViewModel(null!, null!, projectData);
        var sampleDate = (Samples.BaseDate + 110).ToDateTime();
        sut.LoadedData.Add(
            new ImportEntryViewModel(accounts)
            {
                Date = sampleDate, RemoteAccount = accounts[0], IsSkip = false, IsExisting = false
            });

        sut.BookAllCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void BookAllCommand_EntrySkipped_CanExecute()
    {
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        projectData.LoadData(Samples.SampleProject);
        projectData.Storage.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        var accounts = projectData.Storage.AllAccounts.ToList();
        var sut = new ImportBookingsViewModel(null!, null!, projectData);
        var sampleDate = (Samples.BaseDate + 110).ToDateTime();
        sut.LoadedData.Add(
            new ImportEntryViewModel(accounts)
            {
                Date = sampleDate, RemoteAccount = null, IsSkip = true, IsExisting = false
            });

        sut.BookAllCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void BookAllCommand_EntryExisting_CannotExecute()
    {
        var clock = Substitute.For<IClock>();
        var projectData = new ProjectData(new Settings(), null!, null!, null!, clock, null!);
        projectData.LoadData(Samples.SampleProject);
        projectData.Storage.Journal[^1].Booking.AddRange(Samples.SampleBookings);
        var accounts = projectData.Storage.AllAccounts.ToList();
        var sut = new ImportBookingsViewModel(null!, null!, projectData);
        var sampleDate = (Samples.BaseDate + 110).ToDateTime();
        sut.LoadedData.Add(
            new ImportEntryViewModel(accounts)
            {
                Date = sampleDate, RemoteAccount = null, IsSkip = false, IsExisting = true
            });

        sut.BookAllCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task BookAllCommand_SampleData_DataConvertedIntoJournal()
    {
        var projectData = Samples.SampleProjectData;
        var accountsViewModel = new AccountsViewModel(null!, projectData);
        var busy = Substitute.For<IBusy>();
        var clock = Substitute.For<IClock>();
        var parent = new ShellViewModel(
            projectData, busy,
            new MenuViewModel(projectData, busy, null!, clock, null!, null!), new FullJournalViewModel(projectData),
            new AccountJournalViewModel(projectData), accountsViewModel, null!, null!, null!, null!, null!);
        var accounts = projectData.Storage.AllAccounts.ToList();
        var bankAccount = accounts.Single(x => x.Name == "Bank account");
        var sut = new ImportBookingsViewModel(
            null!,
            null!,
            projectData) { SelectedAccount = bankAccount, SelectedAccountNumber = bankAccount.ID };
        var remoteAccount = accounts.Single(x => x.ID == 600);
        int year = DateTime.Today.Year;
        sut.StartDate = new DateTime(year, 1, 2, 0, 0, 0, DateTimeKind.Local);
        sut.LoadedData.AddRange(
        [
            new ImportEntryViewModel(accounts)
            {
                Date = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Local),
                Identifier = 101,
                Name = "Name",
                Text = "Text",
                Value = 1,
                RemoteAccount = remoteAccount
            },
            new ImportEntryViewModel(accounts)
            {
                Date = new DateTime(year, 1, 2, 0, 0, 0, DateTimeKind.Local),
                Identifier = 102,
                Text = "Text",
                Value = 2,
                RemoteAccount = remoteAccount,
                IsFollowup = true
            },
            new ImportEntryViewModel(accounts)
            {
                Date = new DateTime(year, 1, 3, 0, 0, 0, DateTimeKind.Local),
                Identifier = 103,
                Name = "Name",
                Value = -1,
                RemoteAccount = remoteAccount,
                IsSkip = true
            },
            new ImportEntryViewModel(accounts)
            {
                Date = new DateTime(year, 1, 3, 0, 0, 0, DateTimeKind.Local),
                Identifier = 104,
                Name = "Name",
                Value = -1,
                RemoteAccount = remoteAccount
            },
            new ImportEntryViewModel(accounts)
            {
                Date = new DateTime(year, 1, 3, 0, 0, 0, DateTimeKind.Local),
                Identifier = 105,
                Name = "Already booked",
                Value = -2,
                RemoteAccount = remoteAccount,
                IsExisting = true
            },
            new ImportEntryViewModel(accounts)
            {
                Date = new DateTime(year, 1, 3, 0, 0, 0, DateTimeKind.Local),
                Identifier = 106,
                Name = "Ignore",
                Value = -3,
                RemoteAccount = null
            },
            new ImportEntryViewModel(accounts)
            {
                Date = new DateTime(year, 1, 3, 0, 0, 0, DateTimeKind.Local),
                Identifier = 107,
                Name = "Ignore too",
                Value = -4,
                RemoteAccount = remoteAccount
            }
        ]);
        var projectDataMonitor = projectData.Monitor();

        await sut.BookAllCommand.ExecuteAsync(null);

        // 101 should be skipped because of selected start date
        // 103 should be skipped because it is configured to be skipped
        // 105 should be skipped because it is already existing
        // 106 should be skipped because it is not mapped
        // 107 should be skipped because it is valid entry AFTER unmapped entry => stopped
        parent.FullJournal.Items.Should().BeEquivalentTo(
            new object[]
            {
                new
                {
                    Identifier = 102,
                    Text = "Text",
                    Value = 2,
                    CreditAccount = "600 (Shoes)",
                    DebitAccount = "100 (Bank account)",
                    IsFollowup = true
                },
                new
                {
                    Identifier = 104,
                    Text = "Name",
                    Value = 1,
                    CreditAccount = "100 (Bank account)",
                    DebitAccount = "600 (Shoes)"
                }
            });
        projectDataMonitor.Should().Raise(nameof(projectData.JournalChanged)).Should().HaveCount(1);
    }
}
