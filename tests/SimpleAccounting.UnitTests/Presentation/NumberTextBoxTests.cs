// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using System.Globalization;
using FluentAssertions;
using FluentAssertions.Execution;
using lg2de.SimpleAccounting.Presentation;
using Xunit;

public class NumberTextBoxTests
{
    [WpfFact]
    public void IsNewTextValid_NewNumberAtFront_Accepted()
    {
        var sut = new NumberTextBox { Text = "1" };

        using var _ = new AssertionScope();
        sut.IsNewTextValid("2", 0, 0, out var newText).Should().BeTrue();
        newText.Should().Be("21");
    }

    [WpfFact]
    public void IsNewTextValid_NewNumberAtFrontWithTrailingZero_Accepted()
    {
        var sut = new NumberTextBox { Text = "10" };

        using var _ = new AssertionScope();
        sut.IsNewTextValid("2", 0, 0, out var newText).Should().BeTrue();
        newText.Should().Be("210");
    }

    [WpfFact]
    public void IsNewTextValid_SeparatorRightOfExistingOperator_Accepted()
    {
        var separator = CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator;
        var sut = new NumberTextBox { Scale = 2, Text = $"1{separator}2" };

        using var _ = new AssertionScope();
        sut.IsNewTextValid(separator, 1, 0, out var newText).Should().BeTrue();
        newText.Should().Be($"1{separator}2");
    }

    [WpfFact]
    public void IsNewTextValid_InsertFractional_Accepted()
    {
        var sut = new NumberTextBox { Scale = 2, Text = 1.23.ToString(CultureInfo.CurrentUICulture) };

        using var _ = new AssertionScope();
        sut.IsNewTextValid("5", 2, 0, out var newText).Should().BeTrue();
        newText.Should().Be(1.52.ToString(CultureInfo.CurrentUICulture));
    }

    [WpfFact]
    public void IsNewTextValid_Character_Rejected()
    {
        var sut = new NumberTextBox { Text = "1" };

        using var _ = new AssertionScope();
        sut.IsNewTextValid("a", 0, 0, out var newText).Should().BeFalse();
        newText.Should().Be("a1");
    }
}
