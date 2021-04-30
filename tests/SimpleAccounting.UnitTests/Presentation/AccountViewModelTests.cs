// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation
{
    using System.Collections.ObjectModel;
    using FluentAssertions;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Presentation;
    using Xunit;

    public class AccountViewModelTests
    {
        [Fact]
        public void SaveCommand_MissingName_CannotExecute()
        {
            var sut = new AccountViewModel { Name = string.Empty };

            sut.SaveCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void SaveCommand_NoIdentifierCheck_CanExecute()
        {
            var sut = new AccountViewModel
            {
                Name = "AccountName", IsValidIdentifierFunc = null, IsImportActive = false
            };

            sut.SaveCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void SaveCommand_IdentifierCheckSucceed_CanExecute()
        {
            var sut = new AccountViewModel { Name = "AccountName", IsValidIdentifierFunc = id => true };

            sut.SaveCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void SaveCommand_IdentifierCheckFailed_CannotExecute()
        {
            var sut = new AccountViewModel { Name = "AccountName", IsValidIdentifierFunc = id => false };

            sut.SaveCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void SaveCommand_ImportMappingWithoutDate_CannotExecute()
        {
            var sut = new AccountViewModel
            {
                Name = "AccountName",
                IsImportActive = true,
                ImportDateSource = string.Empty,
                ImportValueSource = "X2",
                ImportNameSource = "X3",
                ImportTextSource = "X4"
            };

            sut.SaveCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void SaveCommand_ImportMappingWithoutValue_CannotExecute()
        {
            var sut = new AccountViewModel
            {
                Name = "AccountName",
                IsImportActive = true,
                ImportDateSource = "X1",
                ImportValueSource = string.Empty,
                ImportNameSource = "X3",
                ImportTextSource = "X4"
            };

            sut.SaveCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void SaveCommand_ImportMappingWithoutName_CanExecute()
        {
            var sut = new AccountViewModel
            {
                Name = "AccountName",
                IsImportActive = true,
                ImportDateSource = "X1",
                ImportValueSource = "X2",
                ImportNameSource = string.Empty,
                ImportTextSource = "X4"
            };

            sut.SaveCommand.CanExecute(null).Should().BeTrue("it is optional");
        }

        [Fact]
        public void SaveCommand_ImportMappingWithoutText_CanExecute()
        {
            var sut = new AccountViewModel
            {
                Name = "AccountName",
                IsImportActive = true,
                ImportDateSource = "X1",
                ImportValueSource = "X2",
                ImportNameSource = "X3",
                ImportTextSource = string.Empty
            };

            sut.SaveCommand.CanExecute(null).Should().BeTrue("it is optional");
        }

        [Fact]
        public void SaveCommand_ImportPatternMissingExpression_CanExecute()
        {
            var sut = new AccountViewModel
            {
                Name = "AccountName",
                IsImportActive = true,
                ImportDateSource = "X1",
                ImportValueSource = "X2",
                ImportPatterns = new ObservableCollection<ImportPatternViewModel>
                {
                    new ImportPatternViewModel { Account = new AccountDefinition() }
                }
            };

            sut.SaveCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void SaveCommand_ImportPatternMissingAccount_CanExecute()
        {
            var sut = new AccountViewModel
            {
                Name = "AccountName",
                IsImportActive = true,
                ImportDateSource = "X1",
                ImportValueSource = "X2",
                ImportPatterns = new ObservableCollection<ImportPatternViewModel>
                {
                    new ImportPatternViewModel { Expression = "RegEx", Account = null }
                }
            };

            sut.SaveCommand.CanExecute(null).Should().BeFalse();
        }
    }
}
