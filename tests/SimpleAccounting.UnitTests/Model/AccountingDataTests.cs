// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace SimpleAccounting.UnitTests.Model
{
    using FluentAssertions;
    using lg2de.SimpleAccounting.Model;
    using Xunit;

    public class AccountingDataTests
    {
        [Fact]
        public void XsiSchemaLocation_DefaultContructor_DefaultValue()
        {
            var sut = new AccountingData();

            sut.xsiSchemaLocation.Should().Be(AccountingData.DefaultXsiSchemaLocation);
        }

        [Fact]
        public void XsiSchemaLocation_SetDifferentValue_DefaultValue()
        {
            var sut = new AccountingData
            {
                xsiSchemaLocation = "foo"
            };

            sut.xsiSchemaLocation.Should().Be(AccountingData.DefaultXsiSchemaLocation);
        }
    }
}
