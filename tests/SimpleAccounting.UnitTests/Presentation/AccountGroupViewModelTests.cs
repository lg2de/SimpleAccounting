// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using System.ComponentModel;
using lg2de.SimpleAccounting.Model;
using lg2de.SimpleAccounting.Presentation;
using lg2de.SimpleAccounting.Properties;
using Xunit;

public class AccountGroupViewModelTests
{
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void IsValid_EmptyName_NameRequiredError(string name)
    {
        var sut = new AccountGroupViewModel(null, _ => true) { Name = name };
        IDataErrorInfo errorInfo = sut;

        using var _ = new AssertionScope();
        sut.IsValid.Should().BeFalse();
        errorInfo.Error.Should().Be(Resources.ProjectOptions_GroupNameRequired);
        errorInfo[nameof(sut.Name)].Should().Be(Resources.ProjectOptions_GroupNameRequired);
    }

    [Fact]
    public void Ctor_ExistingGroup_ValuesTakenFromModel()
    {
        var model = new AccountingDataAccountGroup
        {
            Name = "Assets", Account = [new AccountDefinition { ID = 1 }, new AccountDefinition { ID = 2 }]
        };

        var sut = new AccountGroupViewModel(model, _ => true);

        using var _ = new AssertionScope();
        sut.Name.Should().Be("Assets");
        sut.AccountCount.Should().Be(2);
        sut.Model.Should().BeSameAs(model);
    }

    [Fact]
    public void Ctor_NewGroup_EmptyValues()
    {
        var sut = new AccountGroupViewModel(null, _ => true);

        using var _ = new AssertionScope();
        sut.Name.Should().BeEmpty();
        sut.AccountCount.Should().Be(0);
        sut.Model.Should().BeNull();
    }

    [Fact]
    public void IsValid_UniqueName_NoErrors()
    {
        var sut = new AccountGroupViewModel(null, _ => true) { Name = "Assets" };
        IDataErrorInfo errorInfo = sut;

        using var _ = new AssertionScope();
        sut.IsValid.Should().BeTrue();
        errorInfo.Error.Should().BeEmpty();
        errorInfo[nameof(sut.Name)].Should().BeEmpty();
    }

    [Fact]
    public void IsValid_DuplicatedName_NameDuplicatedError()
    {
        AccountGroupViewModel checkedGroup = null;
        var sut = new AccountGroupViewModel(
            null,
            x =>
            {
                checkedGroup = x;
                return false;
            }) { Name = "Assets" };
        IDataErrorInfo errorInfo = sut;

        using var _ = new AssertionScope();
        sut.IsValid.Should().BeFalse();
        errorInfo.Error.Should().Be(Resources.ProjectOptions_GroupNameDuplicated);
        errorInfo[nameof(sut.Name)].Should().Be(Resources.ProjectOptions_GroupNameDuplicated);
        checkedGroup.Should().BeSameAs(sut, "the uniqueness check must exclude the group itself");
    }

    [Fact]
    public void IsValid_EmptyAndDuplicatedName_NameRequiredErrorOnly()
    {
        bool uniquenessChecked = false;
        var sut = new AccountGroupViewModel(
            null,
            _ =>
            {
                uniquenessChecked = true;
                return false;
            });
        IDataErrorInfo errorInfo = sut;

        using var _ = new AssertionScope();
        errorInfo.Error.Should().Be(Resources.ProjectOptions_GroupNameRequired);
        errorInfo[nameof(sut.Name)].Should().Be(Resources.ProjectOptions_GroupNameRequired);
        uniquenessChecked.Should().BeFalse("an empty name is reported before checking uniqueness");
    }

    [Fact]
    public void ErrorInfo_OtherProperty_NoError()
    {
        var sut = new AccountGroupViewModel(null, _ => false);
        IDataErrorInfo errorInfo = sut;
        
        errorInfo[nameof(sut.AccountCount)].Should().BeEmpty();
    }

    [Fact]
    public void RevalidateName_NameChangeNotified()
    {
        var sut = new AccountGroupViewModel(null, _ => true);
        using var monitor = sut.Monitor();

        sut.RevalidateName();

        monitor.Should().RaisePropertyChangeFor(x => x.Name);
    }
}
