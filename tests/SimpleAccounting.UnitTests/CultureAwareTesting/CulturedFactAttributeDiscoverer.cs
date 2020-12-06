// from https://github.com/xunit/xunit/tree/main/src/common.tests/CultureAwareTesting
// licensed by Apache 2.0 https://github.com/xunit/xunit/blob/main/LICENSE

// ReSharper disable once CheckNamespace
namespace Xunit
{
    using System.Collections.Generic;
    using System.Linq;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    internal class CulturedFactAttributeDiscoverer : IXunitTestCaseDiscoverer
	{
		readonly IMessageSink diagnosticMessageSink;

		public CulturedFactAttributeDiscoverer(IMessageSink diagnosticMessageSink)
		{
			this.diagnosticMessageSink = diagnosticMessageSink;
		}

		public IEnumerable<IXunitTestCase> Discover(
			ITestFrameworkDiscoveryOptions discoveryOptions,
			ITestMethod testMethod,
			IAttributeInfo factAttribute)
		{
			var ctorArgs = factAttribute.GetConstructorArguments().ToArray();
			var cultures = Reflector.ConvertArguments(ctorArgs, new[] { typeof(string[]) }).Cast<string[]>().Single();

			if (cultures == null || cultures.Length == 0)
				cultures = new[] { "en-US", "fr-FR" };

			var methodDisplay = discoveryOptions.MethodDisplayOrDefault();
			var methodDisplayOptions = discoveryOptions.MethodDisplayOptionsOrDefault();

			return
				cultures
					.Select(culture => new CulturedXunitTestCase(diagnosticMessageSink, methodDisplay, methodDisplayOptions, testMethod, culture))
					.ToList();
		}
	}
}