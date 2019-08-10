// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

using System;
using System.Xml;
using FluentAssertions;
using lg2de.SimpleAccounting.Extensions;
using Xunit;

namespace SimpleAccounting.UnitTests.Extensions
{
    public class XmlExtensionsTests
    {
        [Fact]
        public void SetAttribute_NullNode_ExceptionThrown()
        {
            XmlNode node = null;
            node.Invoking(n => n.SetAttribute("name", "value")).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void SetAttribute_NullName_ExceptionThrown()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<root><element/></root>");

            doc.DocumentElement.FirstChild.Invoking(node => node.SetAttribute(null, "value"))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void SetAttribute_HappyPath_ExceptionThrown()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<root><element/></root>");

            doc.DocumentElement.FirstChild.SetAttribute("name", "value");

            doc.DocumentElement.FirstChild.OuterXml.Should().Be("<element name=\"value\" />");
        }
    }
}
