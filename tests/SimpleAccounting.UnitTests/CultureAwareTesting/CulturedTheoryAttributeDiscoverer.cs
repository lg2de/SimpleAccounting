// from https://github.com/xunit/xunit/tree/main/src/common.tests/CultureAwareTesting
// licensed by Apache 2.0 https://github.com/xunit/xunit/blob/main/LICENSE

#nullable enable
// ReSharper disable once CheckNamespace
namespace Xunit
{
    using System.Collections.Generic;
    using System.Linq;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    internal class CulturedTheoryAttributeDiscoverer : TheoryDiscoverer
	{
		public CulturedTheoryAttributeDiscoverer(IMessageSink diagnosticMessageSink)
			: base(diagnosticMessageSink) { }

		protected override IEnumerable<IXunitTestCase> CreateTestCasesForDataRow(
			ITestFrameworkDiscoveryOptions discoveryOptions,
			ITestMethod testMethod,
			IAttributeInfo theoryAttribute,
			object?[] dataRow)
		{
			var cultures = GetCultures(theoryAttribute);

			return cultures.Select(
				culture => new CulturedXunitTestCase(
                    this.DiagnosticMessageSink,
					discoveryOptions.MethodDisplayOrDefault(),
					discoveryOptions.MethodDisplayOptionsOrDefault(),
					testMethod,
					culture,
					dataRow
				)
			).ToList();
		}

		protected override IEnumerable<IXunitTestCase> CreateTestCasesForTheory(
			ITestFrameworkDiscoveryOptions discoveryOptions,
			ITestMethod testMethod,
			IAttributeInfo theoryAttribute)
		{
			var cultures = GetCultures(theoryAttribute);
			return cultures.Select(
				culture => new CulturedXunitTheoryTestCase(
                    this.DiagnosticMessageSink,
					discoveryOptions.MethodDisplayOrDefault(),
					discoveryOptions.MethodDisplayOptionsOrDefault(),
					testMethod,
					culture
				)
			).ToList();
		}

		static string[] GetCultures(IAttributeInfo culturedTheoryAttribute)
		{
			var ctorArgs = culturedTheoryAttribute.GetConstructorArguments().ToArray();
			var cultures = Reflector.ConvertArguments(ctorArgs, new[] { typeof(string[]) }).Cast<string[]>().Single();

            if (cultures == null || cultures.Length == 0)
            {
                cultures = new[] { "en-US", "fr-FR" };
            }

			return cultures;
		}
	}
}