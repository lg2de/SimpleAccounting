// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using lg2de.SimpleAccounting.Presentation;
using Xunit;

public class JournalItemBaseViewModelTests
{
    [SuppressMessage(
        "Minor Code Smell", "S2094:Classes should not be empty", Justification = "Its for testing that way.")]
    private class TestJournalItemBaseViewModel : JournalItemBaseViewModel;

    [Fact]
    public void IdentifierText_DefaultIdentifier_Empty()
    {
        var sut = new TestJournalItemBaseViewModel { Identifier = 0 };

        sut.IdentifierText.Should().BeEmpty();
    }

    [Fact]
    public void IdentifierText_SampleIdentifier_Formatted()
    {
        var sut = new TestJournalItemBaseViewModel { Identifier = 42 };

        sut.IdentifierText.Should().Be("42");
    }
}
