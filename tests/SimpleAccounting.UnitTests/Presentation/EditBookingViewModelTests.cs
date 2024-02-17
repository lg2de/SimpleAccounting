// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using System;
using System.Threading.Tasks;
using Caliburn.Micro;
using FluentAssertions;
using FluentAssertions.Execution;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Presentation;
using lg2de.SimpleAccounting.Properties;
using NSubstitute;
using Xunit;

public class EditBookingViewModelTests
{
    private static readonly DateTime YearBegin = new(DateTime.Now.Year, 1, 1, 0, 0, 0, DateTimeKind.Local);
    private static readonly DateTime YearEnd = new(DateTime.Now.Year, 12, 31, 0, 0, 0, DateTimeKind.Local);

    [Fact]
    public void Ctor_DateBeforeStart_DateLimited()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin - TimeSpan.FromDays(1));

        sut.Date.Should().Be(YearBegin);
    }

    [Fact]
    public void Ctor_DateAfterStart_DateCorrect()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin + TimeSpan.FromDays(1));

        sut.Date.Should().Be(YearBegin + TimeSpan.FromDays(1));
    }

    [Fact]
    public void Ctor_DateAfterEnd_DateLimited()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearEnd + TimeSpan.FromDays(1));

        sut.Date.Should().Be(YearEnd);
    }

    [Fact]
    public void AddCommand_FirstBooking_BookingNumberIncremented()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin);

        var oldNumber = sut.BookingIdentifier;
        sut.CreditAccount = 100;
        sut.DebitAccount = 990;

        using var monitor = sut.Monitor();
        sut.AddCommand.Execute(null);

        sut.BookingIdentifier.Should().Be(oldNumber + 1);
        monitor.Should().RaisePropertyChangeFor(m => m.BookingIdentifier);
    }

    [Fact]
    public void AddCommand_InvalidYear_CannotExecute()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            BookingText = "abc",
            CreditIndex = 1,
            DebitIndex = 2,
            BookingValue = 42,
            Date = YearEnd + TimeSpan.FromDays(1)
        };

        sut.AddCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void AddCommand_MissingCredit_CannotExecute()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            BookingText = "abc", DebitIndex = 2, BookingValue = 42
        };

        sut.AddCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void AddCommand_MissingDebit_CannotExecute()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            BookingText = "abc", CreditIndex = 1, BookingValue = 42
        };

        sut.AddCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void AddCommand_MissingNumber_CannotExecute()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            BookingIdentifier = 0,
            BookingText = "abc",
            CreditIndex = 1,
            DebitIndex = 2,
            BookingValue = 42
        };

        sut.AddCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void AddCommand_MissingText_CannotExecute()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            CreditIndex = 1, DebitIndex = 2, BookingValue = 42
        };

        sut.AddCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void AddCommand_MissingValue_CannotExecute()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            BookingText = "abc", CreditIndex = 1, DebitIndex = 2
        };

        sut.AddCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void AddCommand_SameAccount_CannotExecute()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            BookingText = "abc", CreditIndex = 1, DebitIndex = 1, BookingValue = 42
        };

        sut.AddCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void AddCommand_ValidValues_CanExecute()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            BookingText = "abc", CreditIndex = 1, DebitIndex = 2, BookingValue = 42
        };

        sut.AddCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void AddCommand_ConsistentCreditSplitBooking_CanExecute()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            BookingText = "abc",
            DebitIndex = 1,
            DebitAccount = 100,
            BookingValue = 3,
            CreditSplitEntries =
            {
                new SplitBookingViewModel
                {
                    BookingText = "X", BookingValue = 1, AccountIndex = 1, AccountNumber = 200
                },
                new SplitBookingViewModel
                {
                    BookingText = "Y", BookingValue = 2, AccountIndex = 2, AccountNumber = 300
                }
            }
        };

        sut.AddCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void AddCommand_ConsistentDebitSplitBooking_CanExecute()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            BookingText = "abc",
            CreditIndex = 1,
            CreditAccount = 100,
            BookingValue = 3,
            DebitSplitEntries =
            {
                new SplitBookingViewModel
                {
                    BookingText = "X", BookingValue = 1, AccountIndex = 1, AccountNumber = 200
                },
                new SplitBookingViewModel
                {
                    BookingText = "Y", BookingValue = 2, AccountIndex = 2, AccountNumber = 300
                }
            }
        };

        sut.AddCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void AddCommand_InconsistentDebitSplitBooking_CannotExecute()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            BookingText = "abc",
            CreditIndex = 1,
            CreditAccount = 100,
            BookingValue = 3,
            DebitSplitEntries =
            {
                new SplitBookingViewModel
                {
                    BookingText = "X", BookingValue = 0, AccountIndex = 1, AccountNumber = 200
                },
                new SplitBookingViewModel
                {
                    BookingText = "Y", BookingValue = 2, AccountIndex = 2, AccountNumber = 300
                }
            }
        };

        sut.AddCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void AddCommand_SplitBookingWithZeroValue_CannotExecute()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            BookingText = "abc",
            DebitIndex = 1,
            DebitAccount = 100,
            BookingValue = 3,
            CreditSplitEntries =
            {
                new SplitBookingViewModel
                {
                    BookingText = "X", BookingValue = 0, AccountIndex = 1, AccountNumber = 200
                },
                new SplitBookingViewModel
                {
                    BookingText = "Y", BookingValue = 1, AccountIndex = 2, AccountNumber = 300
                }
            }
        };

        sut.AddCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void AddCommand_SplitBookingMissingText_CannotExecute()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            BookingText = "abc",
            DebitIndex = 1,
            DebitAccount = 100,
            BookingValue = 3,
            CreditSplitEntries =
            {
                new SplitBookingViewModel
                {
                    BookingText = "", BookingValue = 1, AccountIndex = 1, AccountNumber = 200
                },
                new SplitBookingViewModel
                {
                    BookingText = "Y", BookingValue = 2, AccountIndex = 2, AccountNumber = 300
                }
            }
        };

        sut.AddCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void AddCommand_SplitBookingMissingAccount_CannotExecute()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            BookingText = "abc",
            DebitIndex = 1,
            DebitAccount = 100,
            BookingValue = 3,
            CreditSplitEntries =
            {
                new SplitBookingViewModel
                {
                    BookingText = "X", BookingValue = 1, AccountIndex = -1, AccountNumber = 200
                },
                new SplitBookingViewModel
                {
                    BookingText = "Y", BookingValue = 2, AccountIndex = 2, AccountNumber = 300
                }
            }
        };

        sut.AddCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void AddCommand_SplitBookingSameAccount_CannotExecute()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            BookingText = "abc",
            DebitIndex = 1,
            DebitAccount = 100,
            BookingValue = 3,
            CreditSplitEntries =
            {
                new SplitBookingViewModel
                {
                    BookingText = "X", BookingValue = 1, AccountIndex = 0, AccountNumber = 100
                },
                new SplitBookingViewModel
                {
                    BookingText = "Y", BookingValue = 2, AccountIndex = 2, AccountNumber = 300
                }
            }
        };

        sut.AddCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void AddCommand_SplitBookingNonMatchingValues_CannotExecute()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            BookingText = "abc",
            DebitIndex = 1,
            DebitAccount = 100,
            BookingValue = 4,
            CreditSplitEntries =
            {
                new SplitBookingViewModel
                {
                    BookingText = "X", BookingValue = 1, AccountIndex = 1, AccountNumber = 200
                },
                new SplitBookingViewModel
                {
                    BookingText = "Y", BookingValue = 2, AccountIndex = 2, AccountNumber = 300
                }
            }
        };

        sut.AddCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task OnInitialize_Initialized()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin);

        await ((IActivate)sut).ActivateAsync();

        sut.DisplayName.Should().NotBeNullOrWhiteSpace();
        sut.SelectedTemplate.Should().BeNull("not template should be selected by default");
    }

    [Fact]
    public void OnInitialize_AllAccountTypesAdded_AccountRelatedPropertiesNotEmpty()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin);
        foreach (AccountDefinitionType type in Enum.GetValues(typeof(AccountDefinitionType)))
        {
            sut.Accounts.Add(new AccountDefinition { Name = type.ToString(), Type = type });
        }

        ((IActivate)sut).ActivateAsync();

        sut.Accounts.Should().NotBeEmpty();
        sut.IncomeAccounts.Should().NotBeEmpty();
        sut.IncomeRemoteAccounts.Should().NotBeEmpty();
        sut.ExpenseAccounts.Should().NotBeEmpty();
        sut.ExpenseRemoteAccounts.Should().NotBeEmpty();
    }

    [Fact]
    public void SelectedTemplate_SetNull_PropertiesUnchanged()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            CreditAccount = 1, DebitAccount = 2, BookingText = "default", BookingValue = 42
        };
        sut.Accounts.Add(new AccountDefinition { ID = 1 });
        sut.Accounts.Add(new AccountDefinition { ID = 2 });
        sut.Accounts.Add(new AccountDefinition { ID = 3 });
        sut.BindingTemplates.Add(new BookingTemplate { Value = 123 });

        sut.SelectedTemplate = null;

        using var _ = new AssertionScope();
        sut.DebitAccount.Should().Be(2);
        sut.CreditAccount.Should().Be(1);
        sut.BookingValue.Should().Be(42);
        sut.BookingText.Should().Be("default");
    }

    [Fact]
    public void SelectedTemplate_SetTemplateWithCredit_CreditAccountSet()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            CreditAccount = 1, DebitAccount = 2, BookingText = "default", BookingValue = 42
        };
        sut.Accounts.Add(new AccountDefinition { ID = 1 });
        sut.Accounts.Add(new AccountDefinition { ID = 2 });
        sut.Accounts.Add(new AccountDefinition { ID = 3 });
        sut.BindingTemplates.Add(new BookingTemplate { Credit = 3 });

        sut.SelectedTemplate = sut.BindingTemplates[^1];

        using var _ = new AssertionScope();
        sut.DebitAccount.Should().Be(2);
        sut.CreditAccount.Should().Be(3);
        sut.BookingValue.Should().Be(42);
        sut.BookingText.Should().Be("default");
    }

    [Fact]
    public void SelectedTemplate_SetTemplateWithDebit_DebitAccountSet()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            CreditAccount = 1, DebitAccount = 2, BookingText = "default", BookingValue = 42
        };
        sut.Accounts.Add(new AccountDefinition { ID = 1 });
        sut.Accounts.Add(new AccountDefinition { ID = 2 });
        sut.Accounts.Add(new AccountDefinition { ID = 3 });
        sut.BindingTemplates.Add(new BookingTemplate { Debit = 3 });

        sut.SelectedTemplate = sut.BindingTemplates[^1];

        using var _ = new AssertionScope();
        sut.DebitAccount.Should().Be(3);
        sut.CreditAccount.Should().Be(1);
        sut.BookingValue.Should().Be(42);
        sut.BookingText.Should().Be("default");
    }

    [Fact]
    public void SelectedTemplate_SetTemplateWithValue_ValueSet()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            CreditAccount = 1,
            DebitAccount = 2,
            BookingText = "default",
            BookingValue = 42,
            Date = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Local)
        };
        sut.Accounts.Add(new AccountDefinition { ID = 1 });
        sut.Accounts.Add(new AccountDefinition { ID = 2 });
        sut.Accounts.Add(new AccountDefinition { ID = 3 });
        sut.BindingTemplates.Add(new BookingTemplate { Value = 123 });

        sut.SelectedTemplate = sut.BindingTemplates[^1];

        using var _ = new AssertionScope();
        sut.DebitAccount.Should().Be(2);
        sut.CreditAccount.Should().Be(1);
        sut.BookingValue.Should().Be(123);
        sut.BookingText.Should().Be("default");
    }

    [Fact]
    public void CreateJournalEntry_SplitCreditEntries_JournalEntryCorrect()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            BookingIdentifier = 42, Date = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Local)
        };
        sut.CreditSplitEntries.Add(
            new SplitBookingViewModel { BookingText = "Credit1", BookingValue = 10, AccountNumber = 100 });
        sut.CreditSplitEntries.Add(
            new SplitBookingViewModel { BookingText = "Credit2", BookingValue = 20, AccountNumber = 200 });
        sut.BookingText = "Debit";
        sut.BookingValue = 30;
        sut.DebitAccount = 300;

        var journalEntry = sut.CreateJournalEntry();

        journalEntry.Should().BeEquivalentTo(
            new AccountingDataJournalBooking
            {
                Date = 2020_0620,
                ID = 42,
                Credit =
                [
                    new BookingValue { Account = 100, Text = "Credit1", Value = 1000 },
                    new BookingValue { Account = 200, Text = "Credit2", Value = 2000 }
                ],
                Debit = [new BookingValue { Account = 300, Text = "Debit", Value = 3000 }]
            });
    }

    [Fact]
    public void CreateJournalEntry_SplitSingleCreditEntry_JournalEntryCorrect()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            BookingIdentifier = 42, Date = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Local)
        };
        sut.CreditSplitEntries.Add(
            new SplitBookingViewModel { BookingText = "Credit", BookingValue = 10, AccountNumber = 100 });
        sut.BookingText = "Overall";
        sut.BookingValue = 10;
        sut.DebitAccount = 300;

        var journalEntry = sut.CreateJournalEntry();

        journalEntry.Should().BeEquivalentTo(
            new AccountingDataJournalBooking
            {
                Date = 2020_0620,
                ID = 42,
                Credit = [new BookingValue { Account = 100, Text = "Overall", Value = 1000 }],
                Debit = [new BookingValue { Account = 300, Text = "Overall", Value = 1000 }]
            });
    }

    [Fact]
    public void CreateJournalEntry_SplitDebitEntries_JournalEntryCorrect()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            BookingIdentifier = 42, Date = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Local)
        };
        sut.DebitSplitEntries.Add(
            new SplitBookingViewModel { BookingText = "Debit1", BookingValue = 10, AccountNumber = 100 });
        sut.DebitSplitEntries.Add(
            new SplitBookingViewModel { BookingText = "Debit2", BookingValue = 20, AccountNumber = 200 });
        sut.BookingText = "Credit";
        sut.BookingValue = 30;
        sut.CreditAccount = 300;

        var journalEntry = sut.CreateJournalEntry();

        journalEntry.Should().BeEquivalentTo(
            new AccountingDataJournalBooking
            {
                Date = 2020_0620,
                ID = 42,
                Credit = [new BookingValue { Account = 300, Text = "Credit", Value = 3000 }],
                Debit =
                [
                    new BookingValue { Account = 100, Text = "Debit1", Value = 1000 },
                    new BookingValue { Account = 200, Text = "Debit2", Value = 2000 }
                ]
            });
    }

    [Fact]
    public void CreateJournalEntry_SplitSingleDebitEntry_JournalEntryCorrect()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin)
        {
            BookingIdentifier = 42, Date = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Local)
        };
        sut.DebitSplitEntries.Add(
            new SplitBookingViewModel { BookingText = "Debit", BookingValue = 10, AccountNumber = 100 });
        sut.BookingText = "Overall";
        sut.BookingValue = 10;
        sut.CreditAccount = 300;

        var journalEntry = sut.CreateJournalEntry();

        journalEntry.Should().BeEquivalentTo(
            new AccountingDataJournalBooking
            {
                Date = 2020_0620,
                ID = 42,
                Credit = [new BookingValue { Account = 300, Text = "Overall", Value = 1000 }],
                Debit = [new BookingValue { Account = 100, Text = "Overall", Value = 1000 }]
            });
    }

    [Fact]
    public void SaveCommand_Initialized_CannotExecute()
    {
        var windowManager = Substitute.For<IWindowManager>();
        var dialogs = Substitute.For<IDialogs>();
        var fileSystem = Substitute.For<IFileSystem>();
        var clock = Substitute.For<IClock>();
        var processApi = Substitute.For<IProcess>();
        var projectData = new ProjectData(new Settings(), windowManager, dialogs, fileSystem, clock, processApi);
        var sut = new EditBookingViewModel(projectData, YearBegin);

        sut.SaveCommand.CanExecute(null).Should().BeFalse();
    }
}
