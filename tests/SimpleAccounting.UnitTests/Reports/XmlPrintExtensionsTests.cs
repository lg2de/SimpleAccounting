// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Reports;

using lg2de.SimpleAccounting.Properties;
using lg2de.SimpleAccounting.Reports;
using Xunit;

public class XmlPrintExtensionsTests
{
    [CulturedFact(["en"])]
    public void Translate_NoReferenceText_UnchangedStringReturned()
    {
        var result = "abc".Translate();

        result.Should().Be("abc");
    }

    [CulturedFact(["en"])]
    public void Translate_ReferenceText_TranslatedStringReturned()
    {
        var result = $"@{nameof(Resources.Word_AccountName)}@".Translate();

        result.Should().Be(Resources.Word_AccountName);
    }

    [CulturedFact(["en"])]
    public void Translate_TwoReferenceTexts_TranslatedStringsReturned()
    {
        var result = $"@{nameof(Resources.Word_AccountName)}@@{nameof(Resources.Word_BookingNumber)}@".Translate();

        result.Should().Be(Resources.Word_AccountName + Resources.Word_BookingNumber);
    }

    [CulturedFact(["en"])]
    public void Translate_UnbalancedKeyCharacters_UnchangedStringReturned()
    {
        var result = "@abc".Translate();

        result.Should().Be("@abc");
    }

    [CulturedFact(["en"])]
    public void Translate_UnknownReferenceText_ReferenceTextReturned()
    {
        var result = "@abc@".Translate();

        result.Should().Be("abc");
    }
}
