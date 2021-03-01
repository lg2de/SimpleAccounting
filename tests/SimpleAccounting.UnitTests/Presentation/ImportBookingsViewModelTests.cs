// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation
{
    using System;
    using System.Linq;
    using FluentAssertions;
    using lg2de.SimpleAccounting.Infrastructure;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Presentation;
    using lg2de.SimpleAccounting.Properties;
    using NSubstitute;
    using Xunit;

    public class ImportBookingsViewModelTests
    {
        [CulturedFact("en")]
        public void SelectedAccountNumber_BankAccountSelected_ExistingBookingsSetUp()
        {
            var projectData = new ProjectData(new Settings(), null!, null!, null!, null!);
            projectData.Load(Samples.SampleProject);
            var sut = new ImportBookingsViewModel(null!, projectData);
            projectData.Storage.Journal.Last().Booking.AddRange(Samples.SampleBookings);

            sut.SelectedAccountNumber = 100;

            var year = Convert.ToInt32(Samples.SampleProject.Journal.Last().Year);
            sut.ImportDataFiltered.Should().BeEmpty("start date should be set after last booking");
            sut.ExistingData.Should().BeEquivalentTo(
                new
                {
                    Date = new DateTime(year, 1, 1),
                    Identifier = 1,
                    Name = "<already booked>",
                    Text = "Open 1",
                    Value = 1000,
                    RemoteAccount = new { ID = 990 }
                },
                new
                {
                    Date = new DateTime(year, 1, 28),
                    Identifier = 3,
                    Name = "<already booked>",
                    Text = "Salary",
                    Value = 200,
                    RemoteAccount = (AccountDefinition)null
                },
                new
                {
                    Date = new DateTime(year, 1, 29),
                    Identifier = 4,
                    Name = "<already booked>",
                    Text = "Credit rate",
                    Value = -400,
                    RemoteAccount = new { ID = 5000 }
                },
                new
                {
                    Date = new DateTime(year, 2, 1),
                    Identifier = 5,
                    Name = "<already booked>",
                    Text = "Shoes",
                    Value = -50,
                    RemoteAccount = (AccountDefinition)null
                },
                new
                {
                    Date = new DateTime(year, 2, 5),
                    Identifier = 6,
                    Name = "<already booked>",
                    Text = "Rent to friend",
                    Value = -99,
                    RemoteAccount = new { ID = 6000 }
                });
        }

        [Fact]
        public void ImportBookings_SampleData_AccountsFiltered()
        {
            var projectData = new ProjectData(new Settings(), null!, null!, null!, null!);
            projectData.Load(Samples.SampleProject);
            projectData.Storage.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            var sut = new ImportBookingsViewModel(null!, projectData);

            sut.ImportAccounts.Should().BeEquivalentTo(new { Name = "Bank account" });
        }

        [Fact]
        public void BookAllCommand_EntryNotMapped_CannotExecute()
        {
            var projectData = new ProjectData(new Settings(), null!, null!, null!, null!);
            projectData.Load(Samples.SampleProject);
            projectData.Storage.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            var accounts = projectData.Storage.AllAccounts.ToList();
            var sut = new ImportBookingsViewModel(null!, projectData);
            sut.LoadedData.Add(
                new ImportEntryViewModel(accounts) { RemoteAccount = null, IsSkip = false, IsExisting = false });

            sut.BookAllCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void BookAllCommand_EntryMapped_CanExecute()
        {
            var projectData = new ProjectData(new Settings(), null!, null!, null!, null!);
            projectData.Load(Samples.SampleProject);
            projectData.Storage.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            var accounts = projectData.Storage.AllAccounts.ToList();
            var sut = new ImportBookingsViewModel(null!, projectData);
            sut.LoadedData.Add(
                new ImportEntryViewModel(accounts)
                {
                    RemoteAccount = accounts.First(),
                    IsSkip = false,
                    IsExisting = false
                });

            sut.BookAllCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void BookAllCommand_EntrySkipped_CanExecute()
        {
            var projectData = new ProjectData(new Settings(), null!, null!, null!, null!);
            projectData.Load(Samples.SampleProject);
            projectData.Storage.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            var accounts = projectData.Storage.AllAccounts.ToList();
            var sut = new ImportBookingsViewModel(null!, projectData);
            sut.LoadedData.Add(
                new ImportEntryViewModel(accounts) { RemoteAccount = null, IsSkip = true, IsExisting = false });

            sut.BookAllCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void BookAllCommand_EntryExisting_CannotExecute()
        {
            var projectData = new ProjectData(new Settings(), null!, null!, null!, null!);
            projectData.Load(Samples.SampleProject);
            projectData.Storage.Journal.Last().Booking.AddRange(Samples.SampleBookings);
            var accounts = projectData.Storage.AllAccounts.ToList();
            var sut = new ImportBookingsViewModel(null!, projectData);
            sut.LoadedData.Add(
                new ImportEntryViewModel(accounts) { RemoteAccount = null, IsSkip = false, IsExisting = true });

            sut.BookAllCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void BookAllCommand_SampleData_DataConvertedIntoJournal()
        {
            var projectData = Samples.SampleProjectData;
            var accountsViewModel = new AccountsViewModel(null!, projectData);
            var busy = Substitute.For<IBusy>();
            var parent = new ShellViewModel(
                projectData, busy,
                new MenuViewModel(projectData, busy, null!, null!, null!), new FullJournalViewModel(projectData),
                new AccountJournalViewModel(projectData), accountsViewModel, null!);
            var accounts = projectData.Storage.AllAccounts.ToList();
            var bankAccount = accounts.Single(x => x.Name == "Bank account");
            var sut = new ImportBookingsViewModel(
                null!,
                projectData)
            { SelectedAccount = bankAccount, SelectedAccountNumber = bankAccount.ID };
            var remoteAccount = accounts.Single(x => x.ID == 600);
            int year = DateTime.Today.Year;
            sut.StartDate = new DateTime(year, 1, 2);
            sut.LoadedData.AddRange(
                new[]
                {
                    new ImportEntryViewModel(accounts)
                    {
                        Date = new DateTime(year, 1, 1),
                        Identifier = 101,
                        Name = "Name",
                        Text = "Text",
                        Value = 1,
                        RemoteAccount = remoteAccount
                    },
                    new ImportEntryViewModel(accounts)
                    {
                        Date = new DateTime(year, 1, 2),
                        Identifier = 102,
                        Text = "Text",
                        Value = 2,
                        RemoteAccount = remoteAccount
                    },
                    new ImportEntryViewModel(accounts)
                    {
                        Date = new DateTime(year, 1, 3),
                        Identifier = 103,
                        Name = "Name",
                        Value = -1,
                        RemoteAccount = remoteAccount,
                        IsSkip = true
                    },
                    new ImportEntryViewModel(accounts)
                    {
                        Date = new DateTime(year, 1, 3),
                        Identifier = 104,
                        Name = "Name",
                        Value = -1,
                        RemoteAccount = remoteAccount
                    },
                    new ImportEntryViewModel(accounts)
                    {
                        Date = new DateTime(year, 1, 3),
                        Identifier = 105,
                        Name = "Ignore",
                        Value = -2,
                        RemoteAccount = null
                    },
                    new ImportEntryViewModel(accounts)
                    {
                        Date = new DateTime(year, 1, 3),
                        Identifier = 106,
                        Name = "Ignore too",
                        Value = -3,
                        RemoteAccount = remoteAccount
                    }
                });

            sut.BookAllCommand.Execute(null);

            // 101 should be skipped because of selected start date
            // 103 should be skipped because it is configured to be skipped
            // 105 should be skipped because it is not mapped
            // 106 should be skipped because it is valid entry AFTER unmapped entry => stop
            parent.FullJournal.Items.Should().BeEquivalentTo(
                new
                {
                    Identifier = 102,
                    Text = "Text",
                    Value = 2,
                    CreditAccount = "600 (Shoes)",
                    DebitAccount = "100 (Bank account)"
                },
                new
                {
                    Identifier = 104,
                    Text = "Name",
                    Value = 1,
                    CreditAccount = "100 (Bank account)",
                    DebitAccount = "600 (Shoes)"
                });
        }
    }
}
