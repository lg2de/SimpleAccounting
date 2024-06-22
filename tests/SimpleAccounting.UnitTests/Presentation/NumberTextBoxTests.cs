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
    public void ProcessTextInput_NewNumberAtFront_Accepted()
    {
        var sut = new NumberTextBox { Text = "1", SelectionStart = 0, SelectionLength = 0 };

        sut.ProcessTextInput("2");

        using var _ = new AssertionScope();
        sut.Text.Should().Be("21");
        sut.SelectionStart.Should().Be(1);
    }

    [WpfFact]
    public void ProcessTextInput_NewNumberAtFrontWithTrailingZero_Accepted()
    {
        var sut = new NumberTextBox { Text = "10", SelectionStart = 0, SelectionLength = 0 };

        sut.ProcessTextInput("2");

        using var _ = new AssertionScope();
        sut.Text.Should().Be("210");
        sut.SelectionStart.Should().Be(1);
    }

    [WpfFact]
    public void ProcessTextInput_SeparatorAtCurrentSeparator_AcceptedAndDuplicateRemoved()
    {
        var separator = CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator;
        var sut = new NumberTextBox { Scale = 2, Text = $"1{separator}2", SelectionStart = 1, SelectionLength = 0 };

        sut.ProcessTextInput(separator);

        using var _ = new AssertionScope();
        sut.Text.Should().Be($"1{separator}2");
        sut.SelectionStart.Should().Be(2);
    }

    [WpfFact]
    public void ProcessTextInput_InsertFractional_AcceptedWithCuttingLowPrecision()
    {
        var sut = new NumberTextBox
        {
            Scale = 2, Text = 1.23.ToString(CultureInfo.CurrentUICulture), SelectionStart = 2, SelectionLength = 0
        };

        sut.ProcessTextInput("5");

        using var _ = new AssertionScope();
        sut.Text.Should().Be(1.52.ToString(CultureInfo.CurrentUICulture));
        sut.SelectionStart.Should().Be(3);
    }

    [WpfFact]
    public void ProcessTextInput_NumberReplacingLeadingZero_SelectionFixed()
    {
        var sut = new NumberTextBox
        {
            Scale = 2, Text = 0.12.ToString(CultureInfo.CurrentUICulture), SelectionStart = 1, SelectionLength = 0
        };

        sut.ProcessTextInput("8");

        using var _ = new AssertionScope();
        sut.Text.Should().Be(8.12.ToString(CultureInfo.CurrentUICulture));
        sut.SelectionStart.Should().Be(1);
    }

    [WpfFact]
    public void ProcessTextInput_Character_Rejected()
    {
        var sut = new NumberTextBox { Text = "1", SelectionStart = 0, SelectionLength = 0 };

        sut.ProcessTextInput("a");

        using var _ = new AssertionScope();
        sut.Text.Should().Be("1");
        sut.SelectionStart.Should().Be(0);
    }
}
