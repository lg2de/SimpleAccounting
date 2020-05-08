// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation
{
    using System;
    using System.Linq;
    using Caliburn.Micro;
    using FluentAssertions;
    using FluentAssertions.Execution;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Infrastructure;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Presentation;
    using lg2de.SimpleAccounting.Reports;
    using NSubstitute;
    using Xunit;

    public class EditBookingViewModelTests
    {
        private static readonly DateTime YearBegin = new DateTime(DateTime.Now.Year, 1, 1);
        private static readonly DateTime YearEnd = new DateTime(DateTime.Now.Year, 12, 31);

        [Fact]
        public void Accounts_AllAccountTypesAdded_AccountRelatedPropertiesNotEmpty()
        {
            var sut = new EditBookingViewModel(null, YearBegin, YearEnd);

            foreach (AccountDefinitionType type in Enum.GetValues(typeof(AccountDefinitionType)))
            {
                sut.Accounts.Add(new AccountDefinition { Name = type.ToString(), Type = type });
            }

            sut.Accounts.Should().NotBeEmpty();
            sut.IncomeAccounts.Should().NotBeEmpty();
            sut.IncomeRemoteAccounts.Should().NotBeEmpty();
            sut.ExpenseAccounts.Should().NotBeEmpty();
            sut.ExpenseRemoteAccounts.Should().NotBeEmpty();
        }

        [Fact]
        public void BookCommand_FirstBooking_BookingNumberIncremented()
        {
            var windowManager = Substitute.For<IWindowManager>();
            var reportFactory = Substitute.For<IReportFactory>();
            var applicationUpdate = Substitute.For<IApplicationUpdate>();
            var messageBox = Substitute.For<IMessageBox>();
            var fileSystem = Substitute.For<IFileSystem>();
            var processApi = Substitute.For<IProcess>();
            var parent = new ShellViewModel(
                windowManager, reportFactory, applicationUpdate, messageBox, fileSystem, processApi);
            parent.LoadProjectData(Samples.SampleProject);
            var sut = new EditBookingViewModel(parent, YearBegin, YearEnd);

            var oldNumber = sut.BookingIdentifier;
            sut.CreditAccount = 100;
            sut.DebitAccount = 990;

            using var monitor = sut.Monitor();
            sut.AddCommand.Execute(null);

            sut.BookingIdentifier.Should().Be(oldNumber + 1);
            monitor.Should().RaisePropertyChangeFor(m => m.BookingIdentifier);
        }

        [Fact]
        public void BookCommand_InvalidYear_CannotExecute()
        {
            var sut = new EditBookingViewModel(null, YearBegin, YearEnd)
            {
                BookingIdentifier = 1,
                BookingText = "abc",
                CreditIndex = 1,
                DebitIndex = 2,
                BookingValue = 42,
                Date = YearEnd + TimeSpan.FromDays(1)
            };

            sut.AddCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void BookCommand_MissingCredit_CannotExecute()
        {
            var sut = new EditBookingViewModel(null, YearBegin, YearEnd)
            {
                BookingIdentifier = 1,
                BookingText = "abc",
                DebitIndex = 2,
                BookingValue = 42
            };

            sut.AddCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void BookCommand_MissingDebit_CannotExecute()
        {
            var sut = new EditBookingViewModel(null, YearBegin, YearEnd)
            {
                BookingIdentifier = 1,
                BookingText = "abc",
                CreditIndex = 1,
                BookingValue = 42
            };

            sut.AddCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void BookCommand_MissingNumber_CannotExecute()
        {
            var sut = new EditBookingViewModel(null, YearBegin, YearEnd)
            {
                BookingText = "abc",
                CreditIndex = 1,
                DebitIndex = 2,
                BookingValue = 42
            };

            sut.AddCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void BookCommand_MissingText_CannotExecute()
        {
            var sut = new EditBookingViewModel(null, YearBegin, YearEnd)
            {
                BookingIdentifier = 1,
                CreditIndex = 1,
                DebitIndex = 2,
                BookingValue = 42
            };

            sut.AddCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void BookCommand_MissingValue_CannotExecute()
        {
            var sut = new EditBookingViewModel(null, YearBegin, YearEnd)
            {
                BookingIdentifier = 1,
                BookingText = "abc",
                CreditIndex = 1,
                DebitIndex = 2
            };

            sut.AddCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void BookCommand_SameAccount_CannotExecute()
        {
            var sut = new EditBookingViewModel(null, YearBegin, YearEnd)
            {
                BookingIdentifier = 1,
                BookingText = "abc",
                CreditIndex = 1,
                DebitIndex = 1,
                BookingValue = 42
            };

            sut.AddCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void BookCommand_ValidValues_CanExecute()
        {
            var sut = new EditBookingViewModel(null, YearBegin, YearEnd)
            {
                BookingIdentifier = 1,
                BookingText = "abc",
                CreditIndex = 1,
                DebitIndex = 2,
                BookingValue = 42
            };

            sut.AddCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void OnInitialize_Initialized()
        {
            var sut = new EditBookingViewModel(null, YearBegin, YearEnd);

            ((IActivate)sut).Activate();

            sut.DisplayName.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void SelectedTemplate_SetNull_PropertiesUnchanged()
        {
            var sut = new EditBookingViewModel(null, YearBegin, YearEnd)
            {
                CreditAccount = 1,
                DebitAccount = 2,
                BookingText = "default",
                BookingValue = 42
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
            var sut = new EditBookingViewModel(null, YearBegin, YearEnd)
            {
                CreditAccount = 1,
                DebitAccount = 2,
                BookingText = "default",
                BookingValue = 42
            };
            sut.Accounts.Add(new AccountDefinition { ID = 1 });
            sut.Accounts.Add(new AccountDefinition { ID = 2 });
            sut.Accounts.Add(new AccountDefinition { ID = 3 });
            sut.BindingTemplates.Add(new BookingTemplate { Credit = 3 });

            sut.SelectedTemplate = sut.BindingTemplates.Last();

            using var _ = new AssertionScope();
            sut.DebitAccount.Should().Be(2);
            sut.CreditAccount.Should().Be(3);
            sut.BookingValue.Should().Be(42);
            sut.BookingText.Should().Be("default");
        }

        [Fact]
        public void SelectedTemplate_SetTemplateWithDebit_DebitAccountSet()
        {
            var sut = new EditBookingViewModel(null, YearBegin, YearEnd)
            {
                CreditAccount = 1,
                DebitAccount = 2,
                BookingText = "default",
                BookingValue = 42
            };
            sut.Accounts.Add(new AccountDefinition { ID = 1 });
            sut.Accounts.Add(new AccountDefinition { ID = 2 });
            sut.Accounts.Add(new AccountDefinition { ID = 3 });
            sut.BindingTemplates.Add(new BookingTemplate { Debit = 3 });

            sut.SelectedTemplate = sut.BindingTemplates.Last();

            using var _ = new AssertionScope();
            sut.DebitAccount.Should().Be(3);
            sut.CreditAccount.Should().Be(1);
            sut.BookingValue.Should().Be(42);
            sut.BookingText.Should().Be("default");
        }

        [Fact]
        public void SelectedTemplate_SetTemplateWithValue_ValueSet()
        {
            var sut = new EditBookingViewModel(null, YearBegin, YearEnd)
            {
                CreditAccount = 1,
                DebitAccount = 2,
                BookingText = "default",
                BookingValue = 42
            };
            sut.Accounts.Add(new AccountDefinition { ID = 1 });
            sut.Accounts.Add(new AccountDefinition { ID = 2 });
            sut.Accounts.Add(new AccountDefinition { ID = 3 });
            sut.BindingTemplates.Add(new BookingTemplate { Value = 123 });

            sut.SelectedTemplate = sut.BindingTemplates.Last();

            using var _ = new AssertionScope();
            sut.DebitAccount.Should().Be(2);
            sut.CreditAccount.Should().Be(1);
            sut.BookingValue.Should().Be(123);
            sut.BookingText.Should().Be("default");
        }
    }
}
